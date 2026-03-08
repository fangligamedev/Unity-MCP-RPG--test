#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NineKingsPrototype
{
    public sealed class NineKingsBattleController : MonoBehaviour
    {
        private sealed class NKBattleStack
        {
            public string id = string.Empty;
            public bool friendly;
            public bool isTower;
            public bool isBase;
            public bool isRanged;
            public string sourceCardId = string.Empty;
            public int x;
            public int y;
            public float unitHealth;
            public float totalHealth;
            public float attack;
            public float attackInterval;
            public float attackTimer;
            public int range;
            public int displayedUnits;
            public int shieldCharges;
            public Sprite? sprite;
            public GameObject? view;
            public Text? viewText;
            public float moveTimer;
        }

        private readonly List<NKBattleStack> _friendlyStacks = new();
        private readonly List<NKBattleStack> _enemyStacks = new();
        private readonly System.Random _random = new();

        private NineKingsGameController? _controller;
        private NineKingsRuntimeUI? _ui;
        private NineKingsContentDatabase? _database;
        private NKRunState? _state;
        private Action<bool>? _onFinished;
        private bool _running;
        private bool _finalBattle;
        private Vector2Int _baseCell = new(2, 2);

        public bool IsRunning => _running;

        public void BeginBattle(NineKingsGameController controller, NineKingsRuntimeUI ui, NineKingsContentDatabase database, NKRunState state, bool finalBattle, Action<bool> onFinished)
        {
            _controller = controller;
            _ui = ui;
            _database = database;
            _state = state;
            _onFinished = onFinished;
            _finalBattle = finalBattle;
            _running = true;

            _ui.ClearBattleViews();
            _friendlyStacks.Clear();
            _enemyStacks.Clear();

            CreateFriendlyStacks();
            CreateEnemyStacks();
            RefreshViews();
        }

        public void ForceFinish(bool victory)
        {
            if (!_running)
            {
                return;
            }

            FinishBattle(victory);
        }

        private void Update()
        {
            if (!_running || _controller == null || _ui == null || _controller.IsPaused)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            TickStacks(_friendlyStacks, _enemyStacks, deltaTime, true);
            TickStacks(_enemyStacks, _friendlyStacks, deltaTime, false);
            CleanupDeadStacks();
            RefreshViews();

            if (_enemyStacks.Count == 0)
            {
                FinishBattle(true);
            }
            else if (_controller.Lives <= 0)
            {
                FinishBattle(false);
            }
        }

        private void CreateFriendlyStacks()
        {
            if (_database == null || _state == null || _ui == null)
            {
                return;
            }

            foreach (var plot in _state.GetUnlockedPlots())
            {
                if (plot.IsEmpty)
                {
                    continue;
                }

                var definition = _database.GetCard(plot.cardId);
                if (definition == null)
                {
                    continue;
                }

                var levelStats = definition.GetLevel(Mathf.Max(1, plot.level));
                if (definition.cardType == NKCardType.Base)
                {
                    _baseCell = new Vector2Int(plot.x, plot.y);
                    _friendlyStacks.Add(CreateStack("base", true, plot.x, plot.y, definition, levelStats, plot, true));
                }
                else if (definition.cardType == NKCardType.Troop)
                {
                    _friendlyStacks.Add(CreateStack(definition.cardId, true, plot.x, plot.y, definition, levelStats, plot, false));
                }
                else if (definition.cardType == NKCardType.Tower)
                {
                    var tower = CreateStack(definition.cardId, true, plot.x, plot.y, definition, levelStats, plot, false);
                    tower.isTower = true;
                    tower.isRanged = true;
                    _friendlyStacks.Add(tower);
                }
            }
        }

        private void CreateEnemyStacks()
        {
            if (_database == null || _state == null)
            {
                return;
            }

            var enemyDefinition = _database.GetOpponentKing(_state.CurrentEnemyKingId);
            if (enemyDefinition == null || enemyDefinition.enemyUnitCardIds.Count == 0)
            {
                return;
            }

            var spawnPoints = GetSpawnPoints();
            var stackCount = Mathf.Clamp(2 + (_state.Year / 3) + enemyDefinition.yearlyStackBonus, 2, 10);
            if (_finalBattle)
            {
                stackCount += _database.battleCurve.finalBattleBonusStacks;
            }

            for (var i = 0; i < stackCount; i++)
            {
                var point = spawnPoints[i % spawnPoints.Count];
                var enemyCard = _database.GetCard(enemyDefinition.enemyUnitCardIds[i % enemyDefinition.enemyUnitCardIds.Count]);
                if (enemyCard == null)
                {
                    continue;
                }

                var stats = enemyCard.GetLevel(Mathf.Clamp(1 + (_state.Year / 12), 1, enemyCard.maxLevel));
                var tempPlot = new NKPlotState { x = point.x, y = point.y, level = Mathf.Clamp(stats.level, 1, 3), damageMultiplier = 1f };
                var enemy = CreateStack(enemyCard.cardId + i, false, point.x, point.y, enemyCard, stats, tempPlot, false);
                enemy.totalHealth *= 1f + ((_state.Year - 1) * enemyDefinition.yearlyHealthMultiplier * _database.battleCurve.enemyHealthGrowthPerYear);
                enemy.attack *= 1f + ((_state.Year - 1) * enemyDefinition.yearlyAttackMultiplier * _database.battleCurve.enemyAttackGrowthPerYear);
                if (_finalBattle)
                {
                    enemy.totalHealth *= _database.battleCurve.finalBattleBonusHealthMultiplier;
                    enemy.attack *= _database.battleCurve.finalBattleBonusAttackMultiplier;
                }
                _enemyStacks.Add(enemy);
            }
        }

        private List<Vector2Int> GetSpawnPoints()
        {
            if (_state == null)
            {
                return new List<Vector2Int> { new(0, 0) };
            }

            var border = _state.GetUnlockedPlots()
                .Where(plot => plot.x == 0 || plot.y == 0 || plot.x == 4 || plot.y == 4)
                .Select(plot => new Vector2Int(plot.x, plot.y))
                .ToList();

            if (border.Count == 0)
            {
                border.Add(new Vector2Int(2, 0));
                border.Add(new Vector2Int(0, 2));
                border.Add(new Vector2Int(4, 2));
            }

            return border;
        }

        private NKBattleStack CreateStack(string id, bool friendly, int x, int y, NKCardDefinition definition, NKCardLevelStats stats, NKPlotState plot, bool isBase)
        {
            var stack = new NKBattleStack
            {
                id = id,
                friendly = friendly,
                x = x,
                y = y,
                unitHealth = Mathf.Max(1f, stats.health),
                totalHealth = Mathf.Max(1f, stats.health * Mathf.Max(1, stats.units + plot.unitBonus)),
                attack = Mathf.Max(0.5f, stats.attack * Mathf.Max(0.1f, plot.damageMultiplier)),
                attackInterval = Mathf.Max(0.2f, stats.attackInterval),
                range = Mathf.Max(1, stats.range),
                displayedUnits = Mathf.Max(1, stats.units + plot.unitBonus),
                shieldCharges = plot.shieldCharges,
                sprite = definition.unitSprite != null ? definition.unitSprite : definition.plotSprite,
                sourceCardId = definition.cardId,
                isBase = isBase,
                isRanged = definition.battleBehavior == NKBattleBehavior.Ranged || definition.cardType == NKCardType.Base,
            };

            if (isBase)
            {
                stack.isRanged = true;
                stack.range = Mathf.Max(stack.range, 3);
            }

            if (definition.cardType == NKCardType.Tower)
            {
                stack.range = Mathf.Max(stack.range, 3);
            }

            stack.view = _ui!.CreateBattleView(stack.displayedUnits.ToString(), stack.sprite, friendly ? new Color(0.35f, 0.65f, 1f, 0.9f) : new Color(1f, 0.35f, 0.35f, 0.9f), _ui.GetPlotAnchoredPosition(x, y));
            stack.viewText = stack.view.GetComponentInChildren<Text>();
            return stack;
        }

        private void TickStacks(List<NKBattleStack> actingStacks, List<NKBattleStack> opposingStacks, float deltaTime, bool friendly)
        {
            foreach (var stack in actingStacks)
            {
                stack.attackTimer += deltaTime;
                stack.moveTimer += deltaTime;

                var target = FindNearestTarget(stack, opposingStacks);
                if (target != null && Distance(stack, target) <= stack.range)
                {
                    if (stack.attackTimer >= stack.attackInterval)
                    {
                        stack.attackTimer = 0f;
                        ApplyDamage(target, stack.attack, stack.sourceCardId);
                    }

                    continue;
                }

                if (stack.isTower)
                {
                    continue;
                }

                if (stack.moveTimer >= 0.55f)
                {
                    stack.moveTimer = 0f;
                    if (friendly)
                    {
                        MoveFriendlyStack(stack, target);
                    }
                    else
                    {
                        MoveEnemyStack(stack);
                    }
                }
            }
        }

        private NKBattleStack? FindNearestTarget(NKBattleStack source, List<NKBattleStack> targets)
        {
            NKBattleStack? result = null;
            var bestDistance = int.MaxValue;
            foreach (var target in targets)
            {
                var distance = Distance(source, target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    result = target;
                }
            }

            return result;
        }

        private static int Distance(NKBattleStack a, NKBattleStack b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private void MoveFriendlyStack(NKBattleStack stack, NKBattleStack? target)
        {
            if (target == null)
            {
                return;
            }

            if (stack.isRanged)
            {
                return;
            }

            StepTowards(stack, new Vector2Int(target.x, target.y));
        }

        private void MoveEnemyStack(NKBattleStack stack)
        {
            StepTowards(stack, _baseCell);
            if (stack.x == _baseCell.x && stack.y == _baseCell.y)
            {
                if (_controller != null)
                {
                    _controller.OnBaseBreached(_finalBattle);
                }
                stack.totalHealth = 0f;
            }
        }

        private void StepTowards(NKBattleStack stack, Vector2Int destination)
        {
            if (stack.x < destination.x)
            {
                stack.x += 1;
            }
            else if (stack.x > destination.x)
            {
                stack.x -= 1;
            }
            else if (stack.y < destination.y)
            {
                stack.y += 1;
            }
            else if (stack.y > destination.y)
            {
                stack.y -= 1;
            }
        }

        private void ApplyDamage(NKBattleStack target, float damage, string sourceId)
        {
            if (target.shieldCharges > 0)
            {
                target.shieldCharges -= 1;
                return;
            }

            target.totalHealth -= damage;
            target.displayedUnits = Mathf.Max(0, Mathf.CeilToInt(target.totalHealth / Mathf.Max(1f, target.unitHealth)));
            if (_state != null && target.friendly == false)
            {
                var basePlot = _state.Plots.FirstOrDefault(plot => plot.HasCard(sourceId));
                if (basePlot != null)
                {
                    basePlot.totalDamage += Mathf.RoundToInt(damage);
                    if (target.totalHealth <= 0f)
                    {
                        basePlot.totalKills += 1;
                    }
                }
            }
        }

        private void CleanupDeadStacks()
        {
            RemoveDead(_friendlyStacks);
            RemoveDead(_enemyStacks);
        }

        private void RemoveDead(List<NKBattleStack> stacks)
        {
            for (var i = stacks.Count - 1; i >= 0; i--)
            {
                if (stacks[i].totalHealth > 0f)
                {
                    continue;
                }

                if (stacks[i].view != null)
                {
                    Destroy(stacks[i].view);
                }
                stacks.RemoveAt(i);
            }
        }

        private void RefreshViews()
        {
            RefreshStackViews(_friendlyStacks);
            RefreshStackViews(_enemyStacks);
        }

        private void RefreshStackViews(List<NKBattleStack> stacks)
        {
            foreach (var stack in stacks)
            {
                if (stack.view == null)
                {
                    continue;
                }

                var rect = stack.view.GetComponent<RectTransform>();
                rect.anchoredPosition = _ui!.GetPlotAnchoredPosition(stack.x, stack.y);
                if (stack.viewText != null)
                {
                    stack.viewText.text = stack.displayedUnits > 0 ? stack.displayedUnits.ToString() : "0";
                }
            }
        }

        private void FinishBattle(bool victory)
        {
            _running = false;
            _onFinished?.Invoke(victory);
        }
    }
}
