#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NineKingsPrototype.V2
{
    public sealed class NineKingsV2GameController : MonoBehaviour
    {
        [SerializeField] private ContentDatabase? _database;
        [SerializeField] private string _defaultPlayerKingId = "king_greed";
        [SerializeField] private bool _autoStartRun = true;

        private const float BattleDeployDuration = 1.25f;

        private CombatSimulation? _combatSimulation;
        private CombatPresentation? _combatPresentation;
        private NineKingsV2ScenePresenter? _scenePresenter;
        private float _battleDeployTimer;

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

            if (!PlacementValidator.TryApply(_database, RunState, cardId, coord))
            {
                return false;
            }

            HandState.cardIds.Remove(cardId);
            BuildBoardSceneState();
            ClearPreview();

            if (HandState.PlayableCount <= 2)
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
            if (HandState.PlayableCount <= 2)
            {
                EnterBattleDeploy();
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
        }

        public void TickBattle(float deltaTime)
        {
            if (RunState == null || IsPaused || _combatSimulation == null)
            {
                return;
            }

            if (RunState.phase == RunPhase.BattleDeploy)
            {
                _battleDeployTimer -= deltaTime;
                if (_battleDeployTimer <= 0f)
                {
                    RunState.phase = RunPhase.BattleRun;
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
                if (BattleSceneState.playerWon)
                {
                    RunState.gold += 9;
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
                RunState.deckCardIds.Add(rewardCardId);
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
            RefillHandToFour();
            BuildBoardSceneState();
            EnterCardPhase();
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

        private void SyncHandFromRun()
        {
            HandState = new CardHandState();
            if (RunState != null)
            {
                HandState.cardIds.AddRange(RunState.handCardIds);
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
