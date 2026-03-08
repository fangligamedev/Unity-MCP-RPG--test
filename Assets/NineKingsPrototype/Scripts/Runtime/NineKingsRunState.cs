#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NineKingsPrototype
{
    [Serializable]
    public sealed class NKPlotState
    {
        public int x;
        public int y;
        public bool unlocked;
        public string cardId = string.Empty;
        public int level;
        public int shieldCharges;
        public int unitBonus;
        public float damageMultiplier = 1f;
        public bool blessingMarked;
        public int totalDamage;
        public int totalKills;

        public bool IsEmpty => string.IsNullOrEmpty(cardId);
        public bool HasCard(string id) => string.Equals(cardId, id, StringComparison.Ordinal);
    }

    [Serializable]
    public sealed class NKBlessingState
    {
        public string blessingId = string.Empty;
        public int targetX = -1;
        public int targetY = -1;
        public bool isResolved;
    }

    [Serializable]
    public sealed class NKRunStateData
    {
        public int year = 1;
        public int lives = 3;
        public int gold;
        public int merchantRerollCost = 10;
        public int rewardRerollCost = 10;
        public int expansionTier;
        public string currentEnemyKingId = string.Empty;
        public List<string> remainingEnemyKingIds = new();
        public List<string> handCardIds = new();
        public List<string> discardedCardIds = new();
        public List<string> selectedDecreeIds = new();
        public NKBlessingState? pendingBlessing;
        public List<NKPlotState> plots = new();
    }

    [Serializable]
    public sealed class NKSaveGameState
    {
        public NKRunStateData runState = new();
        public NKRunPhase phase;
        public bool battleWasFinal;
        public string saveVersion = "1";
    }

    [Serializable]
    public sealed class NKRunDebugSnapshot
    {
        public int year;
        public int lives;
        public int gold;
        public int handCount;
        public int unlockedPlots;
        public string currentEnemyKingId = string.Empty;
        public string phase = string.Empty;
    }

    public sealed class NKRunState
    {
        private readonly Dictionary<Vector2Int, NKPlotState> _plotLookup = new();

        public NKRunStateData Data { get; }

        public NKRunState(NKRunStateData data)
        {
            Data = data;
            RebuildLookup();
        }

        public static NKRunState CreateNew(NineKingsContentDatabase database)
        {
            var data = new NKRunStateData();
            data.remainingEnemyKingIds = database.opponentKings.Select(item => item.kingId).ToList();
            if (database.opponentKings.Count > 0)
            {
                data.currentEnemyKingId = database.opponentKings[0].kingId;
            }

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    var plot = new NKPlotState
                    {
                        x = x,
                        y = y,
                        unlocked = x >= 1 && x <= 3 && y >= 1 && y <= 3,
                    };
                    data.plots.Add(plot);
                }
            }

            if (!string.IsNullOrEmpty(database.playerKing.baseCardId))
            {
                data.handCardIds.Add(database.playerKing.baseCardId);
            }

            foreach (var cardId in database.playerKing.cardIds)
            {
                if (data.handCardIds.Count >= 4)
                {
                    break;
                }

                if (!string.Equals(cardId, database.playerKing.baseCardId, StringComparison.Ordinal))
                {
                    data.handCardIds.Add(cardId);
                }
            }

            return new NKRunState(data);
        }

        public int Year
        {
            get => Data.year;
            set => Data.year = Mathf.Clamp(value, 1, 33);
        }

        public int Lives
        {
            get => Data.lives;
            set => Data.lives = Mathf.Max(0, value);
        }

        public int Gold
        {
            get => Data.gold;
            set => Data.gold = Mathf.Max(0, value);
        }

        public int MerchantRerollCost
        {
            get => Data.merchantRerollCost;
            set => Data.merchantRerollCost = Mathf.Max(0, value);
        }

        public int RewardRerollCost
        {
            get => Data.rewardRerollCost;
            set => Data.rewardRerollCost = Mathf.Max(0, value);
        }

        public int ExpansionTier
        {
            get => Data.expansionTier;
            set => Data.expansionTier = Mathf.Clamp(value, 0, 3);
        }

        public string CurrentEnemyKingId
        {
            get => Data.currentEnemyKingId;
            set => Data.currentEnemyKingId = value;
        }

        public IReadOnlyList<string> HandCardIds => Data.handCardIds;
        public IReadOnlyList<string> RemainingEnemyKingIds => Data.remainingEnemyKingIds;
        public IReadOnlyCollection<NKPlotState> Plots => _plotLookup.Values;

        public NKPlotState GetPlot(int x, int y)
        {
            return _plotLookup[new Vector2Int(x, y)];
        }

        public IEnumerable<NKPlotState> GetUnlockedPlots()
        {
            return _plotLookup.Values.Where(plot => plot.unlocked);
        }

        public IEnumerable<NKPlotState> GetAdjacentPlots(int x, int y)
        {
            var directions = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
            };

            foreach (var direction in directions)
            {
                var key = new Vector2Int(x, y) + direction;
                if (_plotLookup.TryGetValue(key, out var plot))
                {
                    yield return plot;
                }
            }
        }

        public bool TryUnlockNextExpansionTier()
        {
            if (ExpansionTier >= 3)
            {
                return false;
            }

            ExpansionTier += 1;
            var cells = ExpansionTier switch
            {
                1 => new[]
                {
                    new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(4, 2), new Vector2Int(2, 4),
                },
                2 => new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(0, 3), new Vector2Int(1, 0), new Vector2Int(3, 0),
                    new Vector2Int(4, 1), new Vector2Int(4, 3), new Vector2Int(1, 4), new Vector2Int(3, 4),
                },
                3 => new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(0, 4), new Vector2Int(4, 0), new Vector2Int(4, 4),
                },
                _ => Array.Empty<Vector2Int>(),
            };

            foreach (var cell in cells)
            {
                if (_plotLookup.TryGetValue(cell, out var plot))
                {
                    plot.unlocked = true;
                }
            }

            return true;
        }

        public bool TryPlayCardToPlot(string cardId, NKCardDefinition definition, NKPlotState targetPlot)
        {
            if (!Data.handCardIds.Contains(cardId))
            {
                return false;
            }

            switch (definition.targetRule)
            {
                case NKTargetRule.EmptyPlot:
                    if (!targetPlot.unlocked)
                    {
                        return false;
                    }

                    if (targetPlot.IsEmpty)
                    {
                        targetPlot.cardId = cardId;
                        targetPlot.level = 1;
                        break;
                    }

                    if (!definition.upgradeable || !targetPlot.HasCard(cardId))
                    {
                        return false;
                    }

                    targetPlot.level = Mathf.Clamp(targetPlot.level + 1, 1, definition.maxLevel);
                    break;
                case NKTargetRule.SameCard:
                    if (!targetPlot.unlocked || targetPlot.IsEmpty || !targetPlot.HasCard(cardId))
                    {
                        return false;
                    }

                    targetPlot.level = Mathf.Clamp(targetPlot.level + 1, 1, definition.maxLevel);
                    break;
                case NKTargetRule.TroopPlot:
                    if (!targetPlot.unlocked || targetPlot.IsEmpty)
                    {
                        return false;
                    }

                    targetPlot.shieldCharges += 1;
                    break;
                case NKTargetRule.OccupiedPlot:
                case NKTargetRule.AnyPlot:
                    if (!targetPlot.unlocked)
                    {
                        return false;
                    }

                    if (definition.cardType == NKCardType.Tome)
                    {
                        targetPlot.level = Mathf.Clamp(targetPlot.level + 1, 1, 3);
                    }
                    else
                    {
                        if (!targetPlot.IsEmpty)
                        {
                            return false;
                        }

                        targetPlot.cardId = cardId;
                        targetPlot.level = 1;
                    }

                    break;
                default:
                    return false;
            }

            Data.handCardIds.Remove(cardId);
            return true;
        }

        public bool DiscardCard(string cardId)
        {
            if (!Data.handCardIds.Remove(cardId))
            {
                return false;
            }

            Data.discardedCardIds.Add(cardId);
            Gold += 9;
            return true;
        }

        public void AddCardToHand(string cardId)
        {
            Data.handCardIds.Add(cardId);
        }

        public bool RemoveCardFromHand(string cardId)
        {
            return Data.handCardIds.Remove(cardId);
        }

        public void SetAvailableEnemies(IEnumerable<string> enemyIds)
        {
            Data.remainingEnemyKingIds = enemyIds.Distinct().ToList();
        }

        public void SetPendingBlessing(string blessingId, int x, int y)
        {
            Data.pendingBlessing = new NKBlessingState
            {
                blessingId = blessingId,
                targetX = x,
                targetY = y,
                isResolved = false,
            };

            GetPlot(x, y).blessingMarked = true;
        }

        public void ResolvePendingBlessing()
        {
            if (Data.pendingBlessing == null)
            {
                return;
            }

            Data.pendingBlessing.isResolved = true;
        }

        public NKRunDebugSnapshot CreateDebugSnapshot(NKRunPhase phase)
        {
            return new NKRunDebugSnapshot
            {
                year = Year,
                lives = Lives,
                gold = Gold,
                handCount = HandCardIds.Count,
                unlockedPlots = GetUnlockedPlots().Count(),
                currentEnemyKingId = CurrentEnemyKingId,
                phase = phase.ToString(),
            };
        }

        public NKSaveGameState CreateSave(NKRunPhase phase, bool battleWasFinal)
        {
            return new NKSaveGameState
            {
                runState = Data,
                phase = phase,
                battleWasFinal = battleWasFinal,
            };
        }

        public void RebuildLookup()
        {
            _plotLookup.Clear();
            foreach (var plot in Data.plots)
            {
                _plotLookup[new Vector2Int(plot.x, plot.y)] = plot;
            }
        }
    }
}
