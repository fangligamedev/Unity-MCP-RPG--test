#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NineKingsPrototype
{
    public enum NKCardType
    {
        Base,
        Troop,
        Tower,
        Building,
        Enchantment,
        Tome,
    }

    public enum NKTargetRule
    {
        None,
        EmptyPlot,
        OccupiedPlot,
        SameCard,
        TroopPlot,
        AnyPlot,
    }

    public enum NKEffectType
    {
        None,
        UpgradePlot,
        AddGold,
        AddLives,
        AddUnits,
        AddDamagePercent,
        AddShield,
        AddAttackPercent,
        UnlockExpansionTier,
        BlessPlot,
        DrawCards,
    }

    public enum NKYearEventType
    {
        None,
        RoyalCouncil,
        BlessingReveal,
        BlessingResolve,
        DiplomatWar,
        DiplomatPeace,
        Merchant,
        TowerExpand,
        FinalBattle,
    }

    public enum NKBattleBehavior
    {
        None,
        Base,
        Melee,
        Ranged,
        Support,
        Passive,
    }

    public enum NKEnemyKingId
    {
        None,
        Blood,
        Nature,
        Rebels,
    }

    public enum NKRunPhase
    {
        Boot,
        MainMenu,
        YearStart,
        Event,
        CardPlay,
        Battle,
        Reward,
        GameOver,
        Victory,
    }

    [Serializable]
    public sealed class NKCardLevelStats
    {
        public int level = 1;
        public float health = 10f;
        public float attack = 1f;
        public float attackInterval = 1f;
        public int units = 1;
        public int range = 1;
        public float critChance;
    }

    [Serializable]
    public sealed class NKCardEffectDefinition
    {
        public NKEffectType effectType;
        public float amount;
        public string targetFilter = string.Empty;
        public string description = string.Empty;
    }

    [Serializable]
    public sealed class NKCardDefinition
    {
        public string cardId = string.Empty;
        public string displayName = string.Empty;
        public string ownerKingId = string.Empty;
        public NKCardType cardType;
        public NKTargetRule targetRule;
        public int maxLevel = 3;
        public bool upgradeable = true;
        public bool infiniteStack;
        public string description = string.Empty;
        public Sprite? plotSprite;
        public Sprite? unitSprite;
        public NKBattleBehavior battleBehavior;
        public List<NKCardLevelStats> levels = new();
        public List<NKCardEffectDefinition> effects = new();

        public NKCardLevelStats GetLevel(int level)
        {
            if (levels.Count == 0)
            {
                return new NKCardLevelStats { level = Mathf.Max(1, level) };
            }

            var found = levels.FirstOrDefault(item => item.level == level);
            if (found != null)
            {
                return found;
            }

            return levels[Mathf.Clamp(level - 1, 0, levels.Count - 1)];
        }
    }

    [Serializable]
    public sealed class NKKingDefinition
    {
        public string kingId = string.Empty;
        public string displayName = string.Empty;
        public Color themeColor = Color.white;
        public string baseCardId = string.Empty;
        public List<string> cardIds = new();
        public List<string> royalDecreeIds = new();
        public List<string> blessingIds = new();
    }

    [Serializable]
    public sealed class NKOpponentKingDefinition
    {
        public NKEnemyKingId enemyKingId;
        public string kingId = string.Empty;
        public string displayName = string.Empty;
        public Color themeColor = Color.white;
        public List<string> rewardPoolIds = new();
        public List<string> enemyUnitCardIds = new();
        public float yearlyHealthMultiplier = 1f;
        public float yearlyAttackMultiplier = 1f;
        public int yearlyStackBonus;
    }

    [Serializable]
    public sealed class NKRoyalDecreeDefinition
    {
        public string decreeId = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public List<NKCardEffectDefinition> effects = new();
    }

    [Serializable]
    public sealed class NKBlessingDefinition
    {
        public string blessingId = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public NKCardEffectDefinition effect = new();
    }

    [Serializable]
    public sealed class NKMerchantDefinition
    {
        public string merchantId = string.Empty;
        public string displayName = string.Empty;
        public List<NKCardType> supportedCardTypes = new();
        public int stockCount = 3;
        public int baseBuyCost = 30;
        public int additionalBuyCost = 15;
        public int baseRerollCost = 10;
        public int rerollCostStep = 10;
    }

    [Serializable]
    public sealed class NKYearEventDefinition
    {
        public int year;
        public NKYearEventType eventType;
    }

    [Serializable]
    public sealed class NKBattleStatCurveDefinition
    {
        public float enemyHealthGrowthPerYear = 0.06f;
        public float enemyAttackGrowthPerYear = 0.05f;
        public int enemyStackGrowthEveryYears = 3;
        public int finalBattleBonusStacks = 4;
        public float finalBattleBonusHealthMultiplier = 1.5f;
        public float finalBattleBonusAttackMultiplier = 1.4f;
    }

    [CreateAssetMenu(menuName = "NineKings/Content Database", fileName = "NineKingsContentDatabase")]
    public sealed class NineKingsContentDatabase : ScriptableObject
    {
        public NKKingDefinition playerKing = new();
        public List<NKOpponentKingDefinition> opponentKings = new();
        public List<NKCardDefinition> cards = new();
        public List<NKRoyalDecreeDefinition> royalDecrees = new();
        public List<NKBlessingDefinition> blessings = new();
        public List<NKMerchantDefinition> merchants = new();
        public List<NKYearEventDefinition> yearEvents = new();
        public NKBattleStatCurveDefinition battleCurve = new();

        public NKCardDefinition? GetCard(string cardId)
        {
            return cards.FirstOrDefault(card => string.Equals(card.cardId, cardId, StringComparison.Ordinal));
        }

        public NKOpponentKingDefinition? GetOpponentKing(string kingId)
        {
            return opponentKings.FirstOrDefault(king => string.Equals(king.kingId, kingId, StringComparison.Ordinal));
        }

        public NKRoyalDecreeDefinition? GetDecree(string decreeId)
        {
            return royalDecrees.FirstOrDefault(item => string.Equals(item.decreeId, decreeId, StringComparison.Ordinal));
        }

        public NKBlessingDefinition? GetBlessing(string blessingId)
        {
            return blessings.FirstOrDefault(item => string.Equals(item.blessingId, blessingId, StringComparison.Ordinal));
        }

        public NKMerchantDefinition? GetMerchant(string merchantId)
        {
            return merchants.FirstOrDefault(item => string.Equals(item.merchantId, merchantId, StringComparison.Ordinal));
        }

        public List<NKYearEventType> GetEventsForYear(int year)
        {
            var results = new List<NKYearEventType>();
            foreach (var definition in yearEvents)
            {
                if (definition.year == year)
                {
                    results.Add(definition.eventType);
                }
            }

            return results;
        }
    }
}
