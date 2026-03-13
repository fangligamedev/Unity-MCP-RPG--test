#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NineKingsPrototype.V2
{
    public enum KingFactionType
    {
        Player,
        Enemy,
        Neutral,
    }

    public enum CardType
    {
        Base,
        Troop,
        Tower,
        Building,
        Enchantment,
        Tome,
    }

    public enum TargetRule
    {
        None,
        EmptyPlot,
        OccupiedPlot,
        SameCard,
        TroopPlot,
        AnyPlot,
        EnchantmentAnchor,
    }

    public enum PresenceType
    {
        None,
        Structure,
        TroopSource,
        EnchantmentAnchor,
    }

    public enum CombatRole
    {
        None,
        Base,
        Melee,
        Ranged,
        Support,
        Passive,
        Elite,
        Boss,
    }

    public enum DamageType
    {
        Physical,
        Magic,
        True,
        Economic,
    }

    public enum TargetPriority
    {
        Nearest,
        LowestHealth,
        HighestThreat,
        StructureFirst,
        BaseFirst,
    }

    public enum EngageRule
    {
        HoldPosition,
        AdvanceToNearest,
        AdvanceToBase,
        SupportRearline,
    }

    public enum RetargetRule
    {
        Never,
        OnTargetDeath,
        OnTargetOutOfRange,
        EveryAttack,
    }

    public enum DeathRule
    {
        RemoveImmediately,
        FadeOut,
        LeaveRubble,
    }

    public enum SurvivorPersistence
    {
        None,
        PersistAsStructure,
        PersistAsDamagedStructure,
    }

    public enum WorldObjectType
    {
        None,
        Palace,
        Tower,
        Building,
        UnitSource,
        AuraEmitter,
    }

    public enum UnitVisualType
    {
        None,
        Single,
        SmallSquad,
        Formation,
        LargeFormation,
        Boss,
    }

    public enum StackDisplayMode
    {
        SingleOnly,
        DuplicateSprites,
        SpritePlusCount,
        FormationGrid,
    }

    public enum AudioCueGroup
    {
        None,
        Greed,
        Nothing,
        Enemy,
        Loot,
    }

    public enum RunPhase
    {
        MainMenu,
        RunIntro,
        YearStart,
        CardPhase,
        PlacementPreview,
        BattleDeploy,
        BattleRun,
        BattleResolve,
        LootChoice,
        EventModal,
        RunOver,
    }

    public enum YearEventType
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

    [Serializable]
    public sealed class LocalizedText
    {
        public string zh = string.Empty;
        public string en = string.Empty;

        public string Get(bool chinese)
        {
            return chinese ? (string.IsNullOrEmpty(zh) ? en : zh) : (string.IsNullOrEmpty(en) ? zh : en);
        }
    }

    [Serializable]
    public sealed class LevelStatBlock
    {
        public int level = 1;
        public int maxHp = 1;
        public int armor;
        public int shield;
        public int attackDamage = 1;
        public float attackInterval = 1f;
        public float attackRange = 1f;
        public float moveSpeed = 1f;
        public float projectileSpeed = 10f;
        public float splashRadius;
        public int unitCount = 1;
        public int goldYield;
        public float auraRadius;
    }

    [Serializable]
    public sealed class CardDefinition
    {
        public string cardId = string.Empty;
        public LocalizedText displayName = new();
        public LocalizedText description = new();
        public string ownerKingId = string.Empty;
        public CardType cardType;
        public string rarity = "common";
        public int maxLevel = 3;
        public TargetRule targetRule;
        public string stackRule = "default";
        public bool upgradeable = true;
    }

    [Serializable]
    public sealed class CardCombatConfig
    {
        public string cardId = string.Empty;
        public PresenceType presenceType;
        public string occupancyFootprint = "1x1";
        public string placementAnchor = "center";
        public bool blocksPlacement = true;
        public string renderLayer = "PlacedStructures";
        public bool spawnsUnits;
        public string unitArchetypeId = string.Empty;
        public string spawnPatternId = string.Empty;
        public int unitCountBase = 1;
        public float deployDelay;
        public float spawnCooldown;
        public string reinforceRule = "none";
        public CombatRole combatRole;
        public DamageType damageType = DamageType.Physical;
        public TargetPriority targetPriority = TargetPriority.Nearest;
        public EngageRule engageRule = EngageRule.AdvanceToNearest;
        public RetargetRule retargetRule = RetargetRule.OnTargetDeath;
        public DeathRule deathRule = DeathRule.FadeOut;
        public SurvivorPersistence survivorPersistence = SurvivorPersistence.None;
        public List<LevelStatBlock> levels = new();
    }

    [Serializable]
    public sealed class CardPresentationConfig
    {
        public string cardId = string.Empty;
        public WorldObjectType worldObjectType;
        public UnitVisualType unitVisualType;
        public string stackDisplayRuleId = string.Empty;
        public string weaponFxId = string.Empty;
        public string hitFxId = string.Empty;
        public string deathFxId = string.Empty;
        public string lootFxId = string.Empty;
        public AudioCueGroup audioCueGroup;
        public Sprite? cardIllustration;
        public Sprite? boardSprite;
        public Sprite? unitSprite;
        public GameObject? structurePrefab;
        public GameObject? unitPrefab;
    }

    [Serializable]
    public sealed class UnitArchetypeDefinition
    {
        public string unitArchetypeId = string.Empty;
        public LocalizedText displayName = new();
        public CombatRole combatRole;
        public DamageType damageType = DamageType.Physical;
        public bool isEnemy;
        public List<LevelStatBlock> levels = new();
    }

    [Serializable]
    public sealed class SpawnPatternSpec
    {
        public string spawnPatternId = string.Empty;
        public string description = string.Empty;
        public string boardAnchor = "plot-center";
        public Vector2 offset = Vector2.zero;
        public float rowSpacing = 0.4f;
        public int columns = 1;
    }

    [Serializable]
    public sealed class WeaponFXSpec
    {
        public string weaponFxId = string.Empty;
        public string description = string.Empty;
        public bool usesProjectile;
        public float projectileSpeed = 8f;
        public Color tint = Color.white;
        public Sprite? projectileSprite;
    }

    [Serializable]
    public sealed class StackDisplayRule
    {
        public string stackDisplayRuleId = string.Empty;
        public StackDisplayMode mode = StackDisplayMode.SingleOnly;
        public int duplicateSpriteThreshold = 3;
        public int overlayCountThreshold = 4;
        public int formationGridThreshold = 8;
    }

    [Serializable]
    public sealed class BattleCurveDefinition
    {
        public string curveId = "default";
        public float yearlyHealthMultiplier = 1.06f;
        public float yearlyAttackMultiplier = 1.05f;
        public float structureHealthMultiplier = 1.03f;
        public float unitCountMultiplier = 1.02f;
        public float finalBattleHealthMultiplier = 1.5f;
        public float finalBattleAttackMultiplier = 1.35f;
    }

    [Serializable]
    public sealed class LootPoolDefinition
    {
        public string lootPoolId = string.Empty;
        public string sourceKingId = string.Empty;
        public List<string> rewardCardIds = new();
        public int draftCount = 3;
    }

    [Serializable]
    public sealed class KingDefinition
    {
        public string kingId = string.Empty;
        public LocalizedText displayName = new();
        public KingFactionType factionType = KingFactionType.Player;
        public Color themeColor = Color.white;
        public string baseCardId = string.Empty;
        public string lootPoolId = string.Empty;
        public List<string> cardIds = new();
        public List<YearEventType> preferredEvents = new();
        public List<string> enemyKingIds = new();
    }
}
