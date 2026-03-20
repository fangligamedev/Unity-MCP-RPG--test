#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NineKingsPrototype.V2
{
    [Serializable]
    public sealed class CardHandState
    {
        public List<string> cardIds = new();
        public bool isLocked;

        public int PlayableCount => cardIds.Count;
    }

    [Serializable]
    public sealed class DragSession
    {
        public bool isActive;
        public string cardId = string.Empty;
        public BoardCoord? hoveredCoord;
        public bool hoveringWell;
    }

    [Serializable]
    public sealed class PlacementPreviewState
    {
        public BoardCoord? targetCoord;
        public bool isValid;
        public string reason = string.Empty;
        public bool isUpgrade;
        public bool isEnchantment;
        public bool isDiscardToWell;
    }

    public readonly struct PlacementResult
    {
        public PlacementResult(bool isValid, string reason, bool isUpgrade = false, bool isEnchantment = false, bool isDiscardToWell = false)
        {
            IsValid = isValid;
            Reason = reason;
            IsUpgrade = isUpgrade;
            IsEnchantment = isEnchantment;
            IsDiscardToWell = isDiscardToWell;
        }

        public bool IsValid { get; }
        public string Reason { get; }
        public bool IsUpgrade { get; }
        public bool IsEnchantment { get; }
        public bool IsDiscardToWell { get; }
    }

    public static class PlacementValidator
    {
        public static PlacementResult ValidateWellDiscard(CardDefinition? card)
        {
            return card == null
                ? new PlacementResult(false, "缺少卡牌定义")
                : new PlacementResult(true, "可弃入井", isDiscardToWell: true);
        }

        public static PlacementResult ValidatePlotPlacement(ContentDatabase database, RunState runState, string cardId, BoardCoord coord)
        {
            var card = database.GetCard(cardId);
            if (card == null)
            {
                return new PlacementResult(false, $"未知卡牌: {cardId}");
            }

            var plot = runState.GetPlot(coord);
            if (!plot.unlocked)
            {
                return new PlacementResult(false, "地块未解锁");
            }

            switch (card.cardType)
            {
                case CardType.Tome:
                    if (string.Equals(cardId, "greed_mortgage", StringComparison.Ordinal))
                    {
                        if (plot.IsEmpty)
                        {
                            return new PlacementResult(false, "抵押需要已有地块");
                        }

                        if (string.Equals(plot.cardId, "greed_palace", StringComparison.Ordinal) ||
                            string.Equals(plot.cardId, "nothing_castle", StringComparison.Ordinal))
                        {
                            return new PlacementResult(false, "不能抵押基地");
                        }

                        return new PlacementResult(true, "可抵押");
                    }

                    return new PlacementResult(false, "即时牌不能放到棋盘");
                case CardType.Enchantment:
                    return plot.IsEmpty
                        ? new PlacementResult(false, "附魔需要目标")
                        : new PlacementResult(true, "可附魔", isEnchantment: true);
            }

            if (plot.IsEmpty)
            {
                return card.targetRule is TargetRule.EmptyPlot or TargetRule.AnyPlot
                    ? new PlacementResult(true, "可放置")
                    : new PlacementResult(false, "目标必须已有卡牌");
            }

            if (string.Equals(plot.cardId, cardId, StringComparison.Ordinal) && card.upgradeable && plot.level < card.maxLevel)
            {
                return new PlacementResult(true, "可升级", isUpgrade: true);
            }

            if (card.targetRule == TargetRule.OccupiedPlot || card.targetRule == TargetRule.AnyPlot)
            {
                return new PlacementResult(true, "可覆盖");
            }

            return new PlacementResult(false, "目标不合法");
        }

        public static bool TryApply(ContentDatabase database, RunState runState, string cardId, BoardCoord coord)
        {
            var result = ValidatePlotPlacement(database, runState, cardId, coord);
            if (!result.IsValid)
            {
                return false;
            }

            var card = database.GetCard(cardId)!;
            var plot = runState.GetPlot(coord);
            if (result.IsEnchantment)
            {
                plot.enchantmentStacks += 1;
            }
            else if (result.IsUpgrade)
            {
                plot.level += 1;
            }
            else
            {
                plot.cardId = cardId;
                plot.level = 1;
            }

            runState.handCardIds.Remove(cardId);
            return true;
        }
    }
}
