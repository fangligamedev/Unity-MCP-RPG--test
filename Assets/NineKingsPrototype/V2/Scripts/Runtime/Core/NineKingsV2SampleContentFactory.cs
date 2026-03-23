#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NineKingsPrototype.V2
{
    public static class NineKingsV2SampleContentFactory
    {
        private const string GreedBeaconId = "greed_beacon";
        private const string GreedDispenserId = "greed_dispenser";
        private const string NothingScoutTowerId = "nothing_scout_tower";
        private const string NothingArcherId = "nothing_archer";
        private const string FxBoltId = "fx-bolt";
        private const string GreedBeaconBoltArchetypeId = "greed-beacon-bolt";

        public static void Populate(ContentDatabase database)
        {
            database.kings = BuildKings();
            database.cards = BuildCards();
            database.combatConfigs = BuildCombatConfigs();
            database.presentationConfigs = BuildPresentationConfigs();
            database.unitArchetypes = BuildUnitArchetypes();
            database.spawnPatterns = BuildSpawnPatterns();
            database.weaponFx = BuildWeaponFx();
            database.stackDisplayRules = BuildStackDisplayRules();
            database.lootPools = BuildLootPools();
            database.yearEvents = BuildYearEvents();
            database.battleCurve = new BattleCurveDefinition();
            database.RebuildIndexes();
        }

        public static void ApplyRuntimeHotfixes(ContentDatabase database)
        {
            if (database == null)
            {
                return;
            }

            PatchTowerCombat(database, GreedDispenserId, "greed-dispenser-bolt", 9, 6.8f, 1.05f);
            PatchTowerCombat(database, GreedBeaconId, GreedBeaconBoltArchetypeId, 7, 6.4f, 1.10f);
            PatchTowerCombat(database, NothingScoutTowerId, "nothing-scout-bolt", 7, 6.2f, 0.95f);
            PatchWeaponFx(database, GreedDispenserId, FxBoltId);
            PatchWeaponFx(database, GreedBeaconId, FxBoltId);
            PatchWeaponFx(database, NothingScoutTowerId, FxBoltId);
            PatchWeaponFx(database, NothingArcherId, FxBoltId);
            EnsureArchetype(database, GreedBeaconBoltArchetypeId, "灯塔", "Beacon", CombatRole.Ranged, false, 10, 7, 1.10f, 6.4f, 0f, 1);
            PatchEnemyRangedArchetype(database);
            database.RebuildIndexes();
        }

        private static void PatchTowerCombat(ContentDatabase database, string cardId, string archetypeId, int damage, float range, float interval)
        {
            var config = database.GetCombatConfig(cardId);
            if (config == null)
            {
                return;
            }

            config.presenceType = PresenceType.Structure;
            config.combatRole = CombatRole.Ranged;
            config.engageRule = EngageRule.HoldPosition;
            config.spawnsUnits = false;
            config.unitArchetypeId = archetypeId;
            config.targetPriority = TargetPriority.Nearest;
            if (config.levels == null || config.levels.Count == 0)
            {
                config.levels = new List<LevelStatBlock> { new LevelStatBlock { level = 1 } };
            }

            for (var i = 0; i < config.levels.Count; i++)
            {
                var level = config.levels[i];
                level.attackDamage = Math.Max(damage + i, level.attackDamage);
                level.attackRange = Math.Max(range + (i * 0.2f), level.attackRange);
                level.attackInterval = Mathf.Min(interval, Mathf.Max(0.35f, level.attackInterval > 0f ? level.attackInterval : interval));
                level.moveSpeed = 0f;
                level.unitCount = 1;
            }
        }

        private static void PatchWeaponFx(ContentDatabase database, string cardId, string weaponFxId)
        {
            var presentation = database.GetPresentationConfig(cardId);
            if (presentation == null)
            {
                return;
            }

            presentation.weaponFxId = weaponFxId;
        }

        private static void EnsureArchetype(ContentDatabase database, string archetypeId, string zh, string en, CombatRole role, bool isEnemy, int hp, int damage, float interval, float range, float speed, int units)
        {
            var existing = database.unitArchetypes.Find(item => string.Equals(item.unitArchetypeId, archetypeId, StringComparison.Ordinal));
            if (existing != null)
            {
                return;
            }

            var archetype = new UnitArchetypeDefinition
            {
                unitArchetypeId = archetypeId,
                displayName = new LocalizedText { zh = zh, en = en },
                combatRole = role,
                isEnemy = isEnemy,
                levels = new List<LevelStatBlock>
                {
                    new() { level = 1, maxHp = hp, attackDamage = damage, attackInterval = interval, attackRange = range, moveSpeed = speed, unitCount = units },
                    new() { level = 2, maxHp = hp + 3, attackDamage = damage + 1, attackInterval = interval, attackRange = range + 0.2f, moveSpeed = speed, unitCount = units },
                    new() { level = 3, maxHp = hp + 6, attackDamage = damage + 2, attackInterval = interval, attackRange = range + 0.4f, moveSpeed = speed, unitCount = units },
                },
            };

            database.unitArchetypes.Add(archetype);
        }

        private static void PatchEnemyRangedArchetype(ContentDatabase database)
        {
            var ranged = database.unitArchetypes.Find(item => string.Equals(item.unitArchetypeId, "enemy-ranged", StringComparison.Ordinal));
            if (ranged == null || ranged.levels == null)
            {
                return;
            }

            foreach (var level in ranged.levels)
            {
                level.moveSpeed = Mathf.Max(level.moveSpeed, 1.15f);
            }
        }

        public static ContentDatabase CreateInMemoryDatabase()
        {
            var database = ScriptableObject.CreateInstance<ContentDatabase>();
            Populate(database);
            return database;
        }

        private static List<KingDefinition> BuildKings()
        {
            return new List<KingDefinition>
            {
                new()
                {
                    kingId = "king_greed",
                    displayName = new LocalizedText { zh = "贪婪之王", en = "King of Greed" },
                    factionType = KingFactionType.Player,
                    themeColor = new Color(0.87f, 0.63f, 0.16f),
                    baseCardId = "greed_palace",
                    lootPoolId = "loot_greed",
                    cardIds = new List<string>
                    {
                        "greed_palace","greed_vault","greed_thief","greed_over_invest","greed_mercenary",
                        "greed_dispenser","greed_beacon","greed_midas_touch","greed_mortgage",
                    },
                    enemyKingIds = new List<string> { "king_blood", "king_nature" },
                },
                new()
                {
                    kingId = "king_nothing",
                    displayName = new LocalizedText { zh = "虚无之王", en = "King of Nothing" },
                    factionType = KingFactionType.Player,
                    themeColor = new Color(0.64f, 0.72f, 0.86f),
                    baseCardId = "nothing_castle",
                    lootPoolId = "loot_nothing",
                    cardIds = new List<string>
                    {
                        "nothing_castle","nothing_archer","nothing_paladin","nothing_soldier","nothing_scout_tower",
                        "nothing_blacksmith","nothing_farm","nothing_steel_coat","nothing_wildcard",
                    },
                    enemyKingIds = new List<string> { "king_blood", "king_nature" },
                },
                new()
                {
                    kingId = "king_blood",
                    displayName = new LocalizedText { zh = "鲜血之王", en = "King of Blood" },
                    factionType = KingFactionType.Enemy,
                    themeColor = new Color(0.76f, 0.2f, 0.24f),
                    lootPoolId = "loot_blood",
                },
                new()
                {
                    kingId = "king_nature",
                    displayName = new LocalizedText { zh = "自然之王", en = "King of Nature" },
                    factionType = KingFactionType.Enemy,
                    themeColor = new Color(0.31f, 0.66f, 0.34f),
                    lootPoolId = "loot_nature",
                },
            };
        }

        private static List<CardDefinition> BuildCards()
        {
            return new List<CardDefinition>
            {
                Card("greed_palace","宫殿","Palace","king_greed",CardType.Base,TargetRule.EmptyPlot),
                Card("greed_vault","金库","Vault","king_greed",CardType.Building,TargetRule.EmptyPlot),
                Card("greed_thief","盗贼","Thief","king_greed",CardType.Troop,TargetRule.EmptyPlot),
                Card("greed_over_invest","过度投资","Over-invest","king_greed",CardType.Enchantment,TargetRule.EnchantmentAnchor),
                Card("greed_mercenary","雇佣兵","Mercenary","king_greed",CardType.Troop,TargetRule.EmptyPlot),
                Card("greed_dispenser","分发塔","Dispenser","king_greed",CardType.Tower,TargetRule.EmptyPlot),
                Card("greed_beacon","灯塔","Beacon","king_greed",CardType.Building,TargetRule.EmptyPlot),
                Card("greed_midas_touch","迈达斯之触","Midas Touch","king_greed",CardType.Enchantment,TargetRule.EnchantmentAnchor),
                Card("greed_mortgage","抵押","Mortgage","king_greed",CardType.Tome,TargetRule.None, 1, false),

                Card("nothing_castle","城堡","Castle","king_nothing",CardType.Base,TargetRule.EmptyPlot),
                Card("nothing_archer","弓箭手","Archer","king_nothing",CardType.Troop,TargetRule.EmptyPlot),
                Card("nothing_paladin","圣骑士","Paladin","king_nothing",CardType.Troop,TargetRule.EmptyPlot),
                Card("nothing_soldier","士兵","Soldier","king_nothing",CardType.Troop,TargetRule.EmptyPlot),
                Card("nothing_scout_tower","侦察塔","Scout Tower","king_nothing",CardType.Tower,TargetRule.EmptyPlot),
                Card("nothing_blacksmith","铁匠铺","Blacksmith","king_nothing",CardType.Building,TargetRule.EmptyPlot),
                Card("nothing_farm","农场","Farm","king_nothing",CardType.Building,TargetRule.EmptyPlot),
                Card("nothing_steel_coat","钢铁披风","Steel Coat","king_nothing",CardType.Enchantment,TargetRule.EnchantmentAnchor),
                Card("nothing_wildcard","万能牌","Wildcard","king_nothing",CardType.Tome,TargetRule.None, 1, false),
            };

            static CardDefinition Card(string id, string zh, string en, string owner, CardType type, TargetRule targetRule, int maxLevel = 3, bool upgradeable = true)
            {
                return new CardDefinition
                {
                    cardId = id,
                    displayName = new LocalizedText { zh = zh, en = en },
                    ownerKingId = owner,
                    cardType = type,
                    targetRule = targetRule,
                    maxLevel = maxLevel,
                    upgradeable = upgradeable,
                    description = new LocalizedText { zh = zh, en = en },
                };
            }
        }

        private static List<CardCombatConfig> BuildCombatConfigs()
        {
            return new List<CardCombatConfig>
            {
                Troop("greed_palace", PresenceType.Structure, CombatRole.Base, "greed-palace-guard", false, 0, 12, 0f, 5, 1f, 6f),
                Structure("greed_vault"),
                Troop("greed_thief", PresenceType.TroopSource, CombatRole.Melee, "greed-thief", true, 3, 5, 1.15f, 3, 0.62f, 1.28f),
                Enchantment("greed_over_invest"),
                Troop("greed_mercenary", PresenceType.TroopSource, CombatRole.Melee, "greed-mercenary", true, 2, 16, 1.25f, 6, 1.05f, 0.92f),
                Tower("greed_dispenser", "greed-dispenser-bolt", 9, 6.8f, 1.05f),
                Tower("greed_beacon", "greed-beacon-bolt", 7, 6.4f, 1.10f),
                Enchantment("greed_midas_touch"),
                Tome("greed_mortgage"),

                Troop("nothing_castle", PresenceType.Structure, CombatRole.Base, "nothing-castle-guard", false, 0, 14, 0f, 5, 1f, 6f),
                Troop("nothing_archer", PresenceType.TroopSource, CombatRole.Ranged, "nothing-archer", true, 2, 6, 4.2f, 2, 1.35f, 0.78f),
                Troop("nothing_paladin", PresenceType.TroopSource, CombatRole.Melee, "nothing-paladin", true, 1, 22, 1.10f, 8, 1.20f, 0.82f),
                Troop("nothing_soldier", PresenceType.TroopSource, CombatRole.Melee, "nothing-soldier", true, 4, 9, 1.05f, 2, 0.78f, 1.05f),
                Tower("nothing_scout_tower", "nothing-scout-bolt", 7, 6.2f, 0.95f),
                Structure("nothing_blacksmith", 10, 0, 2f),
                Structure("nothing_farm", 8, 0, 2f),
                Enchantment("nothing_steel_coat"),
                Tome("nothing_wildcard"),
            };

            static CardCombatConfig Troop(string cardId, PresenceType presenceType, CombatRole role, string unitArchetypeId, bool spawnsUnits, int unitCountBase, int maxHp, float attackRange, int attackDamage, float attackInterval, float moveSpeed)
            {
                return new CardCombatConfig
                {
                    cardId = cardId,
                    presenceType = presenceType,
                    combatRole = role,
                    unitArchetypeId = unitArchetypeId,
                    spawnsUnits = spawnsUnits,
                    spawnPatternId = "spawn-plot-line",
                    unitCountBase = unitCountBase,
                    targetPriority = TargetPriority.Nearest,
                    engageRule = role == CombatRole.Ranged ? EngageRule.HoldPosition : EngageRule.AdvanceToNearest,
                    levels = BuildLevels(maxHp, attackDamage, attackInterval, attackRange, moveSpeed, unitCountBase),
                };
            }

            static CardCombatConfig Structure(string cardId, int maxHp = 9, int attackDamage = 0, float auraRadius = 0f)
            {
                return new CardCombatConfig
                {
                    cardId = cardId,
                    presenceType = PresenceType.Structure,
                    combatRole = attackDamage > 0 ? CombatRole.Support : CombatRole.Passive,
                    engageRule = EngageRule.HoldPosition,
                    levels = BuildLevels(maxHp, attackDamage, 1.2f, 0f, 0f, 0, auraRadius),
                };
            }

            static CardCombatConfig Tower(string cardId, string unitArchetypeId, int attackDamage, float attackRange, float attackInterval)
            {
                return new CardCombatConfig
                {
                    cardId = cardId,
                    presenceType = PresenceType.Structure,
                    combatRole = CombatRole.Ranged,
                    unitArchetypeId = unitArchetypeId,
                    targetPriority = TargetPriority.Nearest,
                    engageRule = EngageRule.HoldPosition,
                    levels = BuildLevels(10, attackDamage, attackInterval, attackRange, 0f, 0),
                };
            }

            static CardCombatConfig Enchantment(string cardId)
            {
                return new CardCombatConfig
                {
                    cardId = cardId,
                    presenceType = PresenceType.EnchantmentAnchor,
                    combatRole = CombatRole.Support,
                    blocksPlacement = false,
                    levels = BuildLevels(1, 0, 0f, 0f, 0f, 0),
                };
            }

            static CardCombatConfig Tome(string cardId)
            {
                return new CardCombatConfig
                {
                    cardId = cardId,
                    presenceType = PresenceType.None,
                    combatRole = CombatRole.None,
                    blocksPlacement = false,
                    levels = BuildLevels(1, 0, 0f, 0f, 0f, 0),
                };
            }

            static List<LevelStatBlock> BuildLevels(int hp, int damage, float interval, float range, float speed, int units, float auraRadius = 0f)
            {
                return new List<LevelStatBlock>
                {
                    new() { level = 1, maxHp = hp, attackDamage = damage, attackInterval = interval, attackRange = range, moveSpeed = speed, unitCount = Math.Max(1, units), auraRadius = auraRadius },
                    new() { level = 2, maxHp = hp + 3, attackDamage = damage + 1, attackInterval = Math.Max(0.6f, interval - 0.05f), attackRange = range, moveSpeed = speed, unitCount = Math.Max(1, units + (units > 0 ? 1 : 0)), auraRadius = auraRadius + 0.3f },
                    new() { level = 3, maxHp = hp + 6, attackDamage = damage + 2, attackInterval = Math.Max(0.5f, interval - 0.1f), attackRange = range + 0.2f, moveSpeed = speed, unitCount = Math.Max(1, units + (units > 0 ? 2 : 0)), auraRadius = auraRadius + 0.6f },
                };
            }
        }

        private static List<CardPresentationConfig> BuildPresentationConfigs()
        {
            var result = new List<CardPresentationConfig>();
            foreach (var cardId in new[]
                     {
                         "greed_palace","greed_vault","greed_thief","greed_over_invest","greed_mercenary","greed_dispenser","greed_beacon","greed_midas_touch","greed_mortgage",
                         "nothing_castle","nothing_archer","nothing_paladin","nothing_soldier","nothing_scout_tower","nothing_blacksmith","nothing_farm","nothing_steel_coat","nothing_wildcard",
                     })
            {
                result.Add(new CardPresentationConfig
                {
                    cardId = cardId,
                    worldObjectType = cardId.Contains("tower") || cardId.Contains("dispenser") ? WorldObjectType.Tower : cardId.Contains("castle") || cardId.Contains("palace") || cardId.Contains("vault") || cardId.Contains("beacon") || cardId.Contains("blacksmith") || cardId.Contains("farm") ? WorldObjectType.Building : cardId.Contains("touch") || cardId.Contains("invest") || cardId.Contains("wildcard") || cardId.Contains("mortgage") ? WorldObjectType.AuraEmitter : WorldObjectType.UnitSource,
                    unitVisualType = cardId.Contains("archer") || cardId.Contains("thief") || cardId.Contains("mercenary") || cardId.Contains("soldier") || cardId.Contains("paladin") ? UnitVisualType.SmallSquad : UnitVisualType.Single,
                    stackDisplayRuleId = cardId.Contains("archer") || cardId.Contains("soldier") || cardId.Contains("thief") || cardId.Contains("mercenary") ? "stack-squad" : "stack-single",
                    weaponFxId = cardId.Contains("archer") || cardId.Contains("tower") || cardId.Contains("dispenser") || cardId.Contains("beacon") ? "fx-bolt" : cardId.Contains("midas") || cardId.Contains("palace") ? "fx-ray" : "fx-slash",
                    hitFxId = "fx-hit",
                    deathFxId = "fx-fade",
                    lootFxId = "fx-gold",
                    audioCueGroup = cardId.StartsWith("greed") ? AudioCueGroup.Greed : AudioCueGroup.Nothing,
                });
            }

            return result;
        }

        private static List<UnitArchetypeDefinition> BuildUnitArchetypes()
        {
            return new List<UnitArchetypeDefinition>
            {
                Unit("greed-palace-guard","宫廷卫兵","Palace Guard",CombatRole.Base,false,14,5,1.0f,6f,1f,1),
                Unit("greed-thief","盗贼小队","Thief",CombatRole.Melee,false,5,3,0.62f,1.15f,1.28f,3),
                Unit("greed-mercenary","雇佣兵","Mercenary",CombatRole.Melee,false,16,6,1.05f,1.25f,0.92f,2),
                Unit("greed-dispenser-bolt","分发塔","Dispenser",CombatRole.Ranged,false,10,9,1.05f,6.8f,0f,1),
                Unit("greed-beacon-bolt","灯塔","Beacon",CombatRole.Ranged,false,10,7,1.10f,6.4f,0f,1),
                Unit("nothing-castle-guard","城堡卫兵","Castle Guard",CombatRole.Base,false,16,5,1.0f,6f,1f,1),
                Unit("nothing-archer","弓箭手","Archer",CombatRole.Ranged,false,6,2,1.35f,4.2f,0.78f,2),
                Unit("nothing-paladin","圣骑士","Paladin",CombatRole.Melee,false,22,8,1.20f,1.10f,0.82f,1),
                Unit("nothing-soldier","士兵小队","Soldier",CombatRole.Melee,false,9,2,0.78f,1.05f,1.05f,4),
                Unit("nothing-scout-bolt","侦察塔","Scout Bolt",CombatRole.Ranged,false,10,7,0.95f,6.2f,0f,1),
                Unit("enemy-melee","敌方步兵","Enemy Melee",CombatRole.Melee,true,8,2,1.0f,1f,0.9f,3),
                Unit("enemy-ranged","敌方弓手","Enemy Ranged",CombatRole.Ranged,true,6,2,1.2f,2.6f,1.15f,2),
                Unit("enemy-dasher","敌方突进兵","Enemy Dasher",CombatRole.Melee,true,5,3,0.7f,0.8f,1.3f,2),
                Unit("enemy-elite","敌方精英","Enemy Elite",CombatRole.Elite,true,16,5,1.0f,1.3f,0.9f,1),
                Unit("enemy-boss","敌方首领","Enemy Boss",CombatRole.Boss,true,30,8,1.2f,1.5f,0.8f,1),
            };

            static UnitArchetypeDefinition Unit(string id, string zh, string en, CombatRole role, bool isEnemy, int hp, int damage, float interval, float range, float speed, int units)
            {
                return new UnitArchetypeDefinition
                {
                    unitArchetypeId = id,
                    displayName = new LocalizedText { zh = zh, en = en },
                    combatRole = role,
                    isEnemy = isEnemy,
                    levels = new List<LevelStatBlock>
                    {
                        new() { level = 1, maxHp = hp, attackDamage = damage, attackInterval = interval, attackRange = range, moveSpeed = speed, unitCount = units },
                        new() { level = 2, maxHp = hp + 3, attackDamage = damage + 1, attackInterval = interval, attackRange = range, moveSpeed = speed, unitCount = units + (units > 1 ? 1 : 0) },
                        new() { level = 3, maxHp = hp + 6, attackDamage = damage + 2, attackInterval = interval, attackRange = range + 0.2f, moveSpeed = speed, unitCount = units + (units > 1 ? 2 : 0) },
                    },
                };
            }
        }

        private static List<SpawnPatternSpec> BuildSpawnPatterns()
        {
            return new List<SpawnPatternSpec>
            {
                new() { spawnPatternId = "spawn-plot-line", description = "从地块前沿生成一排单位", boardAnchor = "plot-front", rowSpacing = 0.4f, columns = 2 },
                new() { spawnPatternId = "spawn-edge-lane", description = "从战场边缘生成敌军", boardAnchor = "edge-lane", rowSpacing = 0.35f, columns = 3 },
            };
        }

        private static List<WeaponFXSpec> BuildWeaponFx()
        {
            return new List<WeaponFXSpec>
            {
                new() { weaponFxId = "fx-slash", description = "近战斩击", usesProjectile = false, tint = Color.white },
                new() { weaponFxId = "fx-bolt", description = "远程箭矢", usesProjectile = true, projectileSpeed = 8f, tint = new Color(1f, 0.86f, 0.35f) },
                new() { weaponFxId = "fx-ray", description = "处决射线", usesProjectile = false, tint = new Color(1f, 0.78f, 0.22f) },
                new() { weaponFxId = "fx-hit", description = "命中特效", usesProjectile = false, tint = Color.white },
                new() { weaponFxId = "fx-fade", description = "死亡淡出", usesProjectile = false, tint = Color.gray },
                new() { weaponFxId = "fx-gold", description = "金币掉落", usesProjectile = false, tint = new Color(1f, 0.83f, 0.18f) },
            };
        }

        private static List<StackDisplayRule> BuildStackDisplayRules()
        {
            return new List<StackDisplayRule>
            {
                new() { stackDisplayRuleId = "stack-single", mode = StackDisplayMode.SingleOnly },
                new() { stackDisplayRuleId = "stack-squad", mode = StackDisplayMode.SpritePlusCount, duplicateSpriteThreshold = 2, overlayCountThreshold = 3, formationGridThreshold = 6 },
            };
        }

        private static List<LootPoolDefinition> BuildLootPools()
        {
            return new List<LootPoolDefinition>
            {
                new() { lootPoolId = "loot_greed", sourceKingId = "king_greed", rewardCardIds = new List<string> { "greed_vault", "greed_thief", "greed_mercenary", "greed_dispenser", "greed_beacon" } },
                new() { lootPoolId = "loot_nothing", sourceKingId = "king_nothing", rewardCardIds = new List<string> { "nothing_archer", "nothing_paladin", "nothing_soldier", "nothing_scout_tower", "nothing_blacksmith" } },
                new() { lootPoolId = "loot_blood", sourceKingId = "king_blood", rewardCardIds = new List<string> { "greed_mercenary", "greed_midas_touch", "nothing_paladin" } },
                new() { lootPoolId = "loot_nature", sourceKingId = "king_nature", rewardCardIds = new List<string> { "nothing_farm", "nothing_steel_coat", "greed_beacon" } },
            };
        }

        private static List<YearEventDefinition> BuildYearEvents()
        {
            return new List<YearEventDefinition>
            {
                Event(4, YearEventType.RoyalCouncil),
                Event(6, YearEventType.BlessingReveal),
                Event(8, YearEventType.DiplomatWar),
                Event(10, YearEventType.Merchant),
                Event(12, YearEventType.TowerExpand),
                Event(14, YearEventType.RoyalCouncil),
                Event(16, YearEventType.BlessingResolve),
                Event(19, YearEventType.TowerExpand),
                Event(21, YearEventType.RoyalCouncil),
                Event(23, YearEventType.DiplomatPeace),
                Event(25, YearEventType.Merchant),
                Event(27, YearEventType.TowerExpand),
                Event(29, YearEventType.RoyalCouncil),
                Event(31, YearEventType.Merchant),
                Event(33, YearEventType.FinalBattle),
            };

            static YearEventDefinition Event(int year, params YearEventType[] events)
            {
                return new YearEventDefinition { year = year, events = new List<YearEventType>(events) };
            }
        }
    }
}
