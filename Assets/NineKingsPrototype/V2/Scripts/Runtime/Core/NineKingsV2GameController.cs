#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NineKingsPrototype.V2
{
    internal readonly struct PlotRuntimeStats
    {
        public PlotRuntimeStats(
            int level,
            int effectiveUnitCount,
            int cumulativeBonusUnitCount,
            int annualAdjacencyBonus,
            int effectiveMaxHp,
            int effectiveDamage,
            float attackInterval,
            float attackRange,
            float moveSpeed,
            int shield,
            int enchantmentStacks,
            int totalDamage,
            int totalKills,
            bool isUnitSource)
        {
            Level = level;
            EffectiveUnitCount = effectiveUnitCount;
            CumulativeBonusUnitCount = cumulativeBonusUnitCount;
            AnnualAdjacencyBonus = annualAdjacencyBonus;
            EffectiveMaxHp = effectiveMaxHp;
            EffectiveDamage = effectiveDamage;
            AttackInterval = attackInterval;
            AttackRange = attackRange;
            MoveSpeed = moveSpeed;
            Shield = shield;
            EnchantmentStacks = enchantmentStacks;
            TotalDamage = totalDamage;
            TotalKills = totalKills;
            IsUnitSource = isUnitSource;
        }

        public int Level { get; }
        public int EffectiveUnitCount { get; }
        public int CumulativeBonusUnitCount { get; }
        public int AnnualAdjacencyBonus { get; }
        public int EffectiveMaxHp { get; }
        public int EffectiveDamage { get; }
        public float AttackInterval { get; }
        public float AttackRange { get; }
        public float MoveSpeed { get; }
        public int Shield { get; }
        public int EnchantmentStacks { get; }
        public int TotalDamage { get; }
        public int TotalKills { get; }
        public bool IsUnitSource { get; }
    }

    public sealed class NineKingsV2GameController : MonoBehaviour
    {
        [SerializeField] private ContentDatabase? _database;
        [SerializeField] private string _defaultPlayerKingId = "king_greed";
        [SerializeField] private bool _autoStartRun = true;

        private const float BattleDeployDuration = 2.75f;
        private const float BattleDeployCameraLeadDuration = 0.55f;
        private const float BattleResolveDuration = 1.60f;

        private CombatSimulation? _combatSimulation;
        private CombatPresentation? _combatPresentation;
        private NineKingsV2ScenePresenter? _scenePresenter;
        private float _battleDeployTimer;
        private float _battleDeployCameraLeadTimer;
        private float _battleResolveTimer;
        private int _lastResolvedYearlyBoardEffectsYear;

        public RunState? RunState { get; private set; }
        public BoardSceneState BoardSceneState { get; private set; } = new();
        public BattleSceneState BattleSceneState { get; private set; } = new();
        public CardHandState HandState { get; private set; } = new();
        public DragSession DragSession { get; private set; } = new();
        public PlacementPreviewState PlacementPreviewState { get; private set; } = new();
        public bool IsPaused { get; private set; }
        public bool AutoBattleEnabled { get; private set; } = true;
        public float BattleSpeedMultiplier { get; private set; } = 1f;

        public ContentDatabase? Database => _database;
        public CombatPresentation? CombatPresentation => _combatPresentation;
        internal float BattleDeployTimeRemaining => _battleDeployTimer;
        internal float BattleDeployCameraLeadTimeRemaining => _battleDeployCameraLeadTimer;
        internal float BattleResolveTimeRemaining => _battleResolveTimer;
        internal bool IsBattleDeployCameraLeading => RunState?.phase == RunPhase.BattleDeploy && _battleDeployCameraLeadTimer > 0f;

        public void SetDatabase(ContentDatabase database)
        {
            _database = database;
            _database.RebuildIndexes();
            _combatSimulation = new CombatSimulation(_database);
            _combatPresentation = new CombatPresentation();
        }

        public void StartNewRun(string playerKingId = "king_greed")
        {
            EnsureDatabase();
            if (_database == null)
            {
                throw new InvalidOperationException("Missing V2 content database.");
            }

            RunState = RunState.CreateNew(_database, playerKingId);
            SyncHandFromRun();
            RunState.phase = RunPhase.YearStart;
            BuildBoardSceneState();
            BattleSceneState = new BattleSceneState();
            _lastResolvedYearlyBoardEffectsYear = 0;
            _battleResolveTimer = 0f;
            ClearPreview();
        }

        public void EnterCardPhase()
        {
            if (RunState == null)
            {
                return;
            }

            RunState.phase = RunPhase.CardPhase;
            HandState.isLocked = false;
            ClearPreview();
        }

        public bool TryPlayCard(string cardId, BoardCoord coord)
        {
            if (_database == null || RunState == null || RunState.phase != RunPhase.CardPhase)
            {
                return false;
            }

            var card = _database.GetCard(cardId);
            if (card?.cardType == CardType.Tome)
            {
                if (!TryApplyTomeCard(cardId, coord))
                {
                    return false;
                }
            }
            else
            {
                if (!PlacementValidator.TryApply(_database, RunState, cardId, coord))
                {
                    return false;
                }

                HandState.cardIds.Remove(cardId);
            }

            BuildBoardSceneState();
            ClearPreview();

            if (ShouldAutoEnterBattleDeploy())
            {
                EnterBattleDeploy();
            }

            return true;
        }

        public bool TryDiscardToWell(string cardId)
        {
            if (RunState == null || !RunState.handCardIds.Remove(cardId))
            {
                return false;
            }

            RunState.discardCardIds.Add(cardId);
            RunState.gold += 9;
            HandState.cardIds.Remove(cardId);
            ClearPreview();
            if (ShouldAutoEnterBattleDeploy())
            {
                EnterBattleDeploy();
            }

            return true;
        }

        private bool TryApplyTomeCard(string cardId, BoardCoord coord)
        {
            if (_database == null || RunState == null)
            {
                return false;
            }

            if (!string.Equals(cardId, "greed_mortgage", StringComparison.Ordinal))
            {
                return false;
            }

            var result = PlacementValidator.ValidatePlotPlacement(_database, RunState, cardId, coord);
            if (!result.IsValid)
            {
                return false;
            }

            var plot = RunState.GetPlot(coord);
            RunState.gold += Mathf.Max(1, plot.level) * 30;
            ClearPlot(plot);
            RunState.handCardIds.Remove(cardId);
            RunState.discardCardIds.Add(cardId);
            HandState.cardIds.Remove(cardId);
            return true;
        }

        private static void ClearPlot(PlotState plot)
        {
            plot.cardId = string.Empty;
            plot.level = 0;
            plot.bonusUnitCount = 0;
            plot.enchantmentStacks = 0;
            plot.shield = 0;
            plot.damageMultiplier = 1f;
            plot.blessingMarked = false;
            plot.totalDamage = 0;
            plot.totalKills = 0;
        }

        private bool ShouldAutoEnterBattleDeploy()
        {
            if (_database == null || RunState == null)
            {
                return false;
            }

            if (HandState.PlayableCount > 2)
            {
                return false;
            }

            foreach (var handCardId in HandState.cardIds)
            {
                var handCard = _database.GetCard(handCardId);
                if (handCard?.cardType == CardType.Tome)
                {
                    return false;
                }
            }

            return true;
        }

        public void SetPreview(string cardId, BoardCoord? coord, bool overWell)
        {
            if (_database == null || RunState == null)
            {
                return;
            }

            DragSession.isActive = true;
            DragSession.cardId = cardId;
            DragSession.hoveredCoord = coord;
            DragSession.hoveringWell = overWell;

            if (overWell)
            {
                var discardResult = PlacementValidator.ValidateWellDiscard(_database.GetCard(cardId));
                PlacementPreviewState = new PlacementPreviewState
                {
                    isValid = discardResult.IsValid,
                    reason = discardResult.Reason,
                    isDiscardToWell = discardResult.IsDiscardToWell,
                };
                return;
            }

            if (coord.HasValue)
            {
                var placementResult = PlacementValidator.ValidatePlotPlacement(_database, RunState, cardId, coord.Value);
                PlacementPreviewState = new PlacementPreviewState
                {
                    targetCoord = coord.Value,
                    isValid = placementResult.IsValid,
                    reason = placementResult.Reason,
                    isUpgrade = placementResult.IsUpgrade,
                    isEnchantment = placementResult.IsEnchantment,
                };
            }
            else
            {
                PlacementPreviewState = new PlacementPreviewState();
            }
        }

        public void ClearPreview()
        {
            DragSession = new DragSession();
            PlacementPreviewState = new PlacementPreviewState();
        }

        public void EnterBattleDeploy()
        {
            if (RunState == null || _database == null || _combatSimulation == null || _combatPresentation == null)
            {
                return;
            }

            RunState.phase = RunPhase.BattleDeploy;
            HandState.isLocked = true;
            BattleSceneState = _combatSimulation.CreateBattleScene(RunState);
            _combatPresentation.Bind(BattleSceneState);
            _battleDeployTimer = BattleDeployDuration;
            _battleDeployCameraLeadTimer = BattleDeployCameraLeadDuration;
            _battleResolveTimer = 0f;
        }

        internal void EnterDebugBattle(BattleSceneState battleSceneState, string playerKingId = "king_nothing", string enemyKingId = "king_blood")
        {
            EnsureDatabase();
            if (_database == null || _combatPresentation == null)
            {
                throw new InvalidOperationException("Missing V2 content database.");
            }

            RunState = new RunState
            {
                playerKingId = playerKingId,
                currentEnemyKingId = enemyKingId,
                phase = RunPhase.BattleRun,
            };

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    RunState.plots.Add(new PlotState
                    {
                        coord = new BoardCoord(x, y),
                        unlocked = x >= 1 && x <= 3 && y >= 1 && y <= 3,
                    });
                }
            }

            RunState.RebuildLookup();
            SyncHandFromRun();
            BuildBoardSceneState();
            ClearPreview();
            HandState.isLocked = true;
            BattleSceneState = battleSceneState;
            _combatPresentation.Bind(BattleSceneState);
            _battleDeployTimer = 0f;
            _battleDeployCameraLeadTimer = 0f;
            _battleResolveTimer = 0f;
            _lastResolvedYearlyBoardEffectsYear = 0;
        }

        public void TickBattle(float deltaTime)
        {
            if (RunState == null || IsPaused || _combatSimulation == null)
            {
                return;
            }

            if (RunState.phase == RunPhase.BattleDeploy)
            {
                var deployArrived = false;
                if (_battleDeployCameraLeadTimer > 0f)
                {
                    _battleDeployCameraLeadTimer = Mathf.Max(0f, _battleDeployCameraLeadTimer - deltaTime);
                }
                else
                {
                    deployArrived = _combatSimulation.AdvanceDeployFormation(BattleSceneState, deltaTime);
                }

                _battleDeployTimer -= deltaTime;
                if (_battleDeployTimer <= 0f && _battleDeployCameraLeadTimer <= 0f && deployArrived)
                {
                    RunState.phase = RunPhase.BattleRun;
                }

                return;
            }

            if (RunState.phase == RunPhase.BattleResolve)
            {
                _battleResolveTimer = Mathf.Max(0f, _battleResolveTimer - deltaTime);
                if (_battleResolveTimer > 0f)
                {
                    return;
                }

                if (BattleSceneState.playerWon)
                {
                    RunState.phase = RunPhase.LootChoice;
                }
                else
                {
                    RunState.lives = Math.Max(0, RunState.lives - 1);
                    if (RunState.lives > 0)
                    {
                        BeginNextYear();
                    }
                    else
                    {
                        RunState.phase = RunPhase.RunOver;
                    }
                }

                return;
            }

            if (RunState.phase != RunPhase.BattleRun || !AutoBattleEnabled)
            {
                return;
            }

            _combatSimulation.Advance(BattleSceneState, deltaTime * BattleSpeedMultiplier);
            if (BattleSceneState.isResolved)
            {
                RunState.phase = RunPhase.BattleResolve;
                _battleResolveTimer = BattleResolveDuration;
                if (BattleSceneState.playerWon)
                {
                    RunState.gold += 9;
                }
            }
        }

        public void ResolveLootChoice(string rewardCardId)
        {
            if (RunState == null || RunState.phase != RunPhase.LootChoice)
            {
                return;
            }

            if (!string.IsNullOrEmpty(rewardCardId))
            {
                AddRewardCardToNextHandOrDeckTop(rewardCardId);
            }

            BeginNextYear();
        }

        public IReadOnlyList<string> GetLootChoices()
        {
            if (_database == null || RunState == null)
            {
                return Array.Empty<string>();
            }

            var lootPoolId = _database.GetKing(RunState.currentEnemyKingId)?.lootPoolId;
            if (string.IsNullOrEmpty(lootPoolId))
            {
                lootPoolId = _database.GetKing(RunState.playerKingId)?.lootPoolId;
            }

            var pool = _database.lootPools.Find(item => string.Equals(item.lootPoolId, lootPoolId, StringComparison.Ordinal));
            if (pool == null || pool.rewardCardIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            for (var i = 0; i < Math.Min(pool.draftCount, pool.rewardCardIds.Count); i++)
            {
                result.Add(pool.rewardCardIds[i]);
            }

            return result;
        }

        public void SetPausedState(bool paused)
        {
            IsPaused = paused;
        }

        public void SetBattleSpeedMultiplier(float multiplier)
        {
            BattleSpeedMultiplier = Mathf.Clamp(multiplier, 1f, 4f);
        }

        public void SetAutoBattleEnabled(bool enabled)
        {
            AutoBattleEnabled = enabled;
        }

        public void AdvanceYear()
        {
            BeginNextYear();
        }

        internal PlotRuntimeStats ResolvePlotRuntimeStats(PlotState plot)
        {
            return ResolvePlotRuntimeStats(_database, RunState, plot);
        }

        public PresentationSnapshot CreateSnapshot()
        {
            return new PresentationSnapshot
            {
                runState = RunState ?? new RunState(),
                boardSceneState = BoardSceneState,
                battleSceneState = BattleSceneState,
            };
        }

        public IReadOnlyList<BattleEntityState> GetVisibleBattleEntities()
        {
            return _combatPresentation?.GetVisibleEntities() ?? Array.Empty<BattleEntityState>();
        }

        private void Awake()
        {
            CleanupLegacyDontDestroyOnLoad();
            EnsureDatabase();
            EnsurePresenter();
        }

        private void Start()
        {
            if (_autoStartRun && RunState == null)
            {
                StartNewRun(_defaultPlayerKingId);
                EnterCardPhase();
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SetPausedState(!IsPaused);
            }

            TickBattle(Time.deltaTime);
        }

        private void BeginNextYear()
        {
            if (RunState == null)
            {
                return;
            }

            RunState.year = Math.Min(33, RunState.year + 1);
            RunState.currentEnemyKingId = NextEnemyKingId();
            RunState.phase = RunPhase.YearStart;
            BattleSceneState = new BattleSceneState();
            ResolveCurrentYearBoardEffects();
            RefillHandToFour();
            BuildBoardSceneState();
            EnterCardPhase();
        }

        private void ResolveCurrentYearBoardEffects()
        {
            if (RunState == null || _database == null)
            {
                return;
            }

            if (_lastResolvedYearlyBoardEffectsYear == RunState.year)
            {
                return;
            }

            ResolveYearlyAdjacencyEffects(_database, RunState);
            _lastResolvedYearlyBoardEffectsYear = RunState.year;
        }

        private string NextEnemyKingId()
        {
            if (_database == null || RunState == null)
            {
                return string.Empty;
            }

            var playerKing = _database.GetKing(RunState.playerKingId);
            if (playerKing == null || playerKing.enemyKingIds.Count == 0)
            {
                return _database.GetDefaultEnemyKingId(RunState.playerKingId);
            }

            var index = Math.Max(0, RunState.year - 1) % playerKing.enemyKingIds.Count;
            return playerKing.enemyKingIds[index];
        }

        private void RefillHandToFour()
        {
            if (RunState == null)
            {
                return;
            }

            while (RunState.handCardIds.Count < 4)
            {
                if (RunState.deckCardIds.Count == 0)
                {
                    if (RunState.discardCardIds.Count == 0)
                    {
                        break;
                    }

                    RunState.deckCardIds.AddRange(RunState.discardCardIds);
                    RunState.discardCardIds.Clear();
                }

                var nextCardId = RunState.deckCardIds[0];
                RunState.deckCardIds.RemoveAt(0);
                RunState.handCardIds.Add(nextCardId);
            }

            SyncHandFromRun();
        }

        private void AddRewardCardToNextHandOrDeckTop(string rewardCardId)
        {
            if (RunState == null || string.IsNullOrEmpty(rewardCardId))
            {
                return;
            }

            if (RunState.handCardIds.Count < 4)
            {
                RunState.handCardIds.Add(rewardCardId);
            }
            else
            {
                RunState.deckCardIds.Insert(0, rewardCardId);
            }

            SyncHandFromRun();
        }

        private void SyncHandFromRun()
        {
            HandState = new CardHandState();
            if (RunState != null)
            {
                HandState.cardIds.AddRange(RunState.handCardIds);
            }
        }

        internal static IReadOnlyList<BoardCoord> GetOrthogonalNeighbors(BoardCoord coord)
        {
            return new[]
            {
                new BoardCoord(coord.x + 1, coord.y),
                new BoardCoord(coord.x - 1, coord.y),
                new BoardCoord(coord.x, coord.y + 1),
                new BoardCoord(coord.x, coord.y - 1),
            };
        }

        internal static PlotRuntimeStats ResolvePlotRuntimeStats(ContentDatabase? database, RunState? runState, PlotState? plot)
        {
            if (database == null || runState == null || plot == null || plot.IsEmpty)
            {
                return default;
            }

            var level = Math.Max(1, plot.level);
            var combat = database.GetCombatConfig(plot.cardId);
            var levelStats = combat?.levels
                .OrderBy(item => item.level)
                .FirstOrDefault(item => item.level == level) ?? combat?.levels.LastOrDefault();
            var isUnitSource = combat != null && combat.spawnsUnits && combat.presenceType == PresenceType.TroopSource;
            var annualAdjacencyBonus = ResolveCurrentAdjacencyBonus(database, runState, plot);
            var effectiveUnitCount = isUnitSource
                ? Math.Max(0, (levelStats?.unitCount ?? 0) + Math.Max(0, plot.bonusUnitCount))
                : 0;
            var effectiveDamage = levelStats == null
                ? 0
                : Mathf.Max(0, Mathf.RoundToInt(levelStats.attackDamage * Mathf.Max(0f, plot.damageMultiplier)));

            return new PlotRuntimeStats(
                level,
                effectiveUnitCount,
                Math.Max(0, plot.bonusUnitCount),
                annualAdjacencyBonus,
                levelStats?.maxHp ?? 0,
                effectiveDamage,
                levelStats?.attackInterval ?? 0f,
                levelStats?.attackRange ?? 0f,
                levelStats?.moveSpeed ?? 0f,
                plot.shield,
                plot.enchantmentStacks,
                plot.totalDamage,
                plot.totalKills,
                isUnitSource);
        }

        internal static int ResolveCurrentAdjacencyBonus(ContentDatabase? database, RunState? runState, PlotState? targetPlot)
        {
            if (database == null || runState == null || targetPlot == null || targetPlot.IsEmpty)
            {
                return 0;
            }

            var targetCombat = database.GetCombatConfig(targetPlot.cardId);
            if (targetCombat == null || !targetCombat.spawnsUnits || targetCombat.presenceType != PresenceType.TroopSource)
            {
                return 0;
            }

            var adjacencyBonus = 0;
            foreach (var neighborCoord in GetOrthogonalNeighbors(targetPlot.coord))
            {
                if (neighborCoord.x < 0 || neighborCoord.x > 4 || neighborCoord.y < 0 || neighborCoord.y > 4)
                {
                    continue;
                }

                var sourcePlot = runState.GetPlot(neighborCoord);
                if (!sourcePlot.unlocked || sourcePlot.IsEmpty)
                {
                    continue;
                }

                if (string.Equals(sourcePlot.cardId, "nothing_farm", StringComparison.Ordinal))
                {
                    adjacencyBonus += Mathf.Clamp(sourcePlot.level, 1, 5);
                }
            }

            return adjacencyBonus;
        }

        internal static void ResolveYearlyAdjacencyEffects(ContentDatabase? database, RunState? runState)
        {
            if (database == null || runState == null)
            {
                return;
            }

            foreach (var targetPlot in runState.plots)
            {
                if (!targetPlot.unlocked || targetPlot.IsEmpty)
                {
                    continue;
                }

                var adjacencyBonus = ResolveCurrentAdjacencyBonus(database, runState, targetPlot);
                if (adjacencyBonus <= 0)
                {
                    continue;
                }

                targetPlot.bonusUnitCount += adjacencyBonus;
            }
        }

        private void EnsureDatabase()
        {
            if (_database != null)
            {
                SetDatabase(_database);
                return;
            }

            _database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            SetDatabase(_database);
        }

        private void EnsurePresenter()
        {
            _scenePresenter = GetComponent<NineKingsV2ScenePresenter>();
            if (_scenePresenter == null)
            {
                _scenePresenter = gameObject.AddComponent<NineKingsV2ScenePresenter>();
            }

            _scenePresenter.SetController(this);
        }

        private static void CleanupLegacyDontDestroyOnLoad()
        {
            var dontDestroyScene = SceneManager.GetSceneByName("DontDestroyOnLoad");
            if (!dontDestroyScene.IsValid() || !dontDestroyScene.isLoaded)
            {
                return;
            }

            foreach (var root in dontDestroyScene.GetRootGameObjects())
            {
                var remove = root.name.Contains("NineKings", StringComparison.Ordinal);
                if (!remove)
                {
                    var components = root.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        var fullName = component.GetType().FullName ?? string.Empty;
                        if (fullName.StartsWith("NineKingsPrototype.", StringComparison.Ordinal) &&
                            !fullName.StartsWith("NineKingsPrototype.V2.", StringComparison.Ordinal))
                        {
                            remove = true;
                            break;
                        }
                    }
                }

                if (remove)
                {
                    root.SetActive(false);
                    Destroy(root);
                }
            }
        }

        private void BuildBoardSceneState()
        {
            BoardSceneState = new BoardSceneState();
            if (RunState == null)
            {
                return;
            }

            foreach (var plot in RunState.plots)
            {
                BoardSceneState.plots.Add(new BoardScenePlotState
                {
                    coord = plot.coord,
                    boardSpriteId = plot.cardId,
                    selected = false,
                    highlighted = plot.unlocked,
                    illegal = !plot.unlocked,
                });
            }
        }
    }
}
