#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineKingsPrototype
{
    public static class NineKingsPrototypeBootstrapper
    {
        private const string RootPath = "Assets/NineKingsPrototype";
        private const string DocsPath = RootPath + "/Docs";
        private const string ScenesPath = RootPath + "/Scenes";
        private const string DataPath = RootPath + "/Data";
        private const string ResourcesPath = DataPath + "/Resources";
        private const string DbAssetPath = ResourcesPath + "/NineKingsContentDatabase.asset";
        private const string SceneAssetPath = ScenesPath + "/NineKings_Main.unity";

        [MenuItem("Tools/NineKings/Build All")]
        public static void BuildAll()
        {
            EnsureFolders();
            BuildContentDatabaseInternal();
            BuildMainSceneInternal();
            WriteImplementationLog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("NineKingsPrototype", "内容资产、主场景和实现日志已生成。", "OK");
        }

        [MenuItem("Tools/NineKings/Build Content Database")]
        public static void BuildContentDatabase()
        {
            EnsureFolders();
            BuildContentDatabaseInternal();
            WriteImplementationLog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/NineKings/Build Main Scene")]
        public static void BuildMainScene()
        {
            EnsureFolders();
            BuildMainSceneInternal();
            WriteImplementationLog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(DocsPath);
            Directory.CreateDirectory(ScenesPath);
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(ResourcesPath);
        }

        private static NineKingsContentDatabase BuildContentDatabaseInternal()
        {
            var database = AssetDatabase.LoadAssetAtPath<NineKingsContentDatabase>(DbAssetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<NineKingsContentDatabase>();
                AssetDatabase.CreateAsset(database, DbAssetPath);
            }

            database.playerKing = BuildPlayerKing();
            database.opponentKings = BuildOpponentKings();
            database.cards = BuildCards();
            database.royalDecrees = BuildRoyalDecrees();
            database.blessings = BuildBlessings();
            database.merchants = BuildMerchants();
            database.yearEvents = BuildYearEvents();
            database.battleCurve = new NKBattleStatCurveDefinition
            {
                enemyHealthGrowthPerYear = 0.08f,
                enemyAttackGrowthPerYear = 0.06f,
                enemyStackGrowthEveryYears = 3,
                finalBattleBonusStacks = 5,
                finalBattleBonusHealthMultiplier = 1.65f,
                finalBattleBonusAttackMultiplier = 1.45f,
            };

            EditorUtility.SetDirty(database);
            return database;
        }

        private static void BuildMainSceneInternal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "NineKings_Main";

            var root = new GameObject("NineKingsRoot");
            root.AddComponent<NineKingsGameController>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.1f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            EditorSceneManager.SaveScene(scene, SceneAssetPath);
        }

        private static NKKingDefinition BuildPlayerKing()
        {
            return new NKKingDefinition
            {
                kingId = "king_nothing",
                displayName = "King of Nothing",
                themeColor = new Color(0.42f, 0.67f, 0.98f, 1f),
                baseCardId = "castle",
                cardIds = new List<string>
                {
                    "castle",
                    "archer",
                    "paladin",
                    "soldier",
                    "scout_tower",
                    "blacksmith",
                    "farm",
                    "steel_coat",
                    "wildcard",
                },
                royalDecreeIds = new List<string>
                {
                    "golden_well",
                    "tower_doctrine",
                    "martial_drill",
                    "blessed_stone",
                },
                blessingIds = new List<string>
                {
                    "growth_blessing",
                    "mastery_blessing",
                },
            };
        }

        private static List<NKOpponentKingDefinition> BuildOpponentKings()
        {
            return new List<NKOpponentKingDefinition>
            {
                new()
                {
                    enemyKingId = NKEnemyKingId.Blood,
                    kingId = "king_blood",
                    displayName = "King of Blood",
                    themeColor = new Color(0.84f, 0.28f, 0.26f, 1f),
                    rewardPoolIds = new List<string> { "blood_raider", "blood_archer", "blood_war_camp" },
                    enemyUnitCardIds = new List<string> { "blood_raider", "blood_archer", "blood_raider", "blood_war_camp" },
                    yearlyHealthMultiplier = 1.05f,
                    yearlyAttackMultiplier = 1.08f,
                    yearlyStackBonus = 1,
                },
                new()
                {
                    enemyKingId = NKEnemyKingId.Nature,
                    kingId = "king_nature",
                    displayName = "King of Nature",
                    themeColor = new Color(0.34f, 0.76f, 0.38f, 1f),
                    rewardPoolIds = new List<string> { "nature_guardian", "nature_archer", "nature_grove" },
                    enemyUnitCardIds = new List<string> { "nature_guardian", "nature_archer", "nature_guardian", "nature_grove" },
                    yearlyHealthMultiplier = 1.08f,
                    yearlyAttackMultiplier = 1.04f,
                    yearlyStackBonus = 0,
                },
            };
        }

        private static List<NKCardDefinition> BuildCards()
        {
            return new List<NKCardDefinition>
            {
                CreateCard(
                    "castle", "Castle", "king_nothing", NKCardType.Base, NKTargetRule.EmptyPlot, NKBattleBehavior.Base,
                    "Base of the kingdom. If breached, you lose a life.",
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/Castle.png"),
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/Castle.png"),
                    Stats(1, 60f, 2f, 1.2f, 1, 3),
                    Stats(2, 90f, 3f, 1.1f, 1, 3),
                    Stats(3, 130f, 4f, 1f, 1, 4)),
                CreateCard(
                    "archer", "Archer", "king_nothing", NKCardType.Troop, NKTargetRule.EmptyPlot, NKBattleBehavior.Ranged,
                    "Ranged troop that starts shooting before the enemy reaches the line.",
                    LoadSprite("Assets/Tiny Swords/Units/Blue Units/Archer/Archer_Idle.png"),
                    LoadSprite("Assets/Tiny Swords/Units/Blue Units/Archer/Archer_Idle.png"),
                    Stats(1, 8f, 2f, 1f, 3, 3),
                    Stats(2, 10f, 2.8f, 0.95f, 4, 3),
                    Stats(3, 12f, 3.6f, 0.9f, 5, 4)),
                CreateCard(
                    "paladin", "Paladin", "king_nothing", NKCardType.Troop, NKTargetRule.EmptyPlot, NKBattleBehavior.Melee,
                    "Durable melee troop for holding the center line.",
                    LoadSprite("Assets/Tiny Swords/Units/Blue Units/Lancer/Lancer_Idle.png"),
                    LoadSprite("Assets/Tiny Swords/Units/Blue Units/Lancer/Lancer_Idle.png"),
                    Stats(1, 18f, 3f, 1.2f, 1, 1),
                    Stats(2, 24f, 4.2f, 1.15f, 1, 1),
                    Stats(3, 32f, 5.6f, 1.05f, 2, 1)),
                CreateCard(
                    "soldier", "Soldier", "king_nothing", NKCardType.Troop, NKTargetRule.EmptyPlot, NKBattleBehavior.Melee,
                    "Cheap frontline troop that scales well with farms and blacksmiths.",
                    LoadSprite("Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle.png"),
                    LoadSprite("Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle.png"),
                    Stats(1, 10f, 2.2f, 1f, 2, 1),
                    Stats(2, 12f, 3f, 0.95f, 3, 1),
                    Stats(3, 14f, 3.8f, 0.9f, 4, 1)),
                CreateCard(
                    "scout_tower", "Scout Tower", "king_nothing", NKCardType.Tower, NKTargetRule.EmptyPlot, NKBattleBehavior.Ranged,
                    "Static defense tower with long range and stable pressure.",
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/Tower.png"),
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/Tower.png"),
                    Stats(1, 18f, 2.5f, 0.9f, 1, 4),
                    Stats(2, 22f, 3.2f, 0.85f, 1, 4),
                    Stats(3, 28f, 4.2f, 0.8f, 1, 5)),
                CreateCard(
                    "blacksmith", "Blacksmith", "king_nothing", NKCardType.Building, NKTargetRule.EmptyPlot, NKBattleBehavior.Passive,
                    "Adjacent troops and towers deal more damage every year.",
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/Barracks.png"),
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/Barracks.png"),
                    Stats(1, 12f, 0f, 1f, 1, 1),
                    Stats(2, 16f, 0f, 1f, 1, 1),
                    Stats(3, 20f, 0f, 1f, 1, 1)),
                CreateCard(
                    "farm", "Farm", "king_nothing", NKCardType.Building, NKTargetRule.EmptyPlot, NKBattleBehavior.Passive,
                    "Adjacent troop plots gain extra units at the start of each year.",
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/House1.png"),
                    LoadSprite("Assets/Tiny Swords/Buildings/Blue Buildings/House1.png"),
                    Stats(1, 10f, 0f, 1f, 1, 1),
                    Stats(2, 14f, 0f, 1f, 1, 1),
                    Stats(3, 18f, 0f, 1f, 1, 1)),
                CreateCard(
                    "steel_coat", "Steel Coat", "king_nothing", NKCardType.Enchantment, NKTargetRule.TroopPlot, NKBattleBehavior.Support,
                    "Adds shield to a troop plot. Can be stacked.",
                    LoadSprite("Assets/Tiny Swords/Pawn and Resources/Tools/Shields.png"),
                    LoadSprite("Assets/Tiny Swords/Pawn and Resources/Tools/Shields.png"),
                    Stats(1, 1f, 0f, 1f, 1, 1),
                    Stats(2, 1f, 0f, 1f, 1, 1),
                    Stats(3, 1f, 0f, 1f, 1, 1),
                    new NKCardEffectDefinition
                    {
                        effectType = NKEffectType.AddShield,
                        amount = 1f,
                        description = "Grant 1 shield charge.",
                    }),
                CreateCard(
                    "wildcard", "Wildcard", "king_nothing", NKCardType.Tome, NKTargetRule.OccupiedPlot, NKBattleBehavior.Support,
                    "Instantly upgrades a plot by 1 level.",
                    LoadSprite("Assets/Tiny Swords/UI/Icons/Scroll.png", "Assets/Tiny Swords/UI/Buttons/Button_Blue.png"),
                    LoadSprite("Assets/Tiny Swords/UI/Icons/Scroll.png", "Assets/Tiny Swords/UI/Buttons/Button_Blue.png"),
                    Stats(1, 1f, 0f, 1f, 1, 1),
                    Stats(2, 1f, 0f, 1f, 1, 1),
                    Stats(3, 1f, 0f, 1f, 1, 1),
                    new NKCardEffectDefinition
                    {
                        effectType = NKEffectType.UpgradePlot,
                        amount = 1f,
                        description = "Upgrade target plot by 1.",
                    }),
                CreateCard(
                    "blood_raider", "Blood Raider", "king_blood", NKCardType.Troop, NKTargetRule.EmptyPlot, NKBattleBehavior.Melee,
                    "Aggressive raider from the Blood kingdom.",
                    LoadSprite("Assets/Tiny Swords/Units/Red Units/Warrior/Warrior_Idle.png", "Assets/Tiny Swords/Units/Red Units/Monk/Idle.png"),
                    LoadSprite("Assets/Tiny Swords/Units/Red Units/Warrior/Warrior_Idle.png", "Assets/Tiny Swords/Units/Red Units/Monk/Idle.png"),
                    Stats(1, 9f, 2.4f, 0.95f, 2, 1),
                    Stats(2, 11f, 3.2f, 0.9f, 3, 1),
                    Stats(3, 13f, 4.2f, 0.85f, 4, 1)),
                CreateCard(
                    "blood_archer", "Blood Archer", "king_blood", NKCardType.Troop, NKTargetRule.EmptyPlot, NKBattleBehavior.Ranged,
                    "Fast ranged pressure from the Blood kingdom.",
                    LoadSprite("Assets/Tiny Swords/Units/Red Units/Archer/Archer_Idle.png"),
                    LoadSprite("Assets/Tiny Swords/Units/Red Units/Archer/Archer_Idle.png"),
                    Stats(1, 7f, 2.3f, 0.95f, 3, 3),
                    Stats(2, 9f, 3.0f, 0.9f, 4, 3),
                    Stats(3, 11f, 3.8f, 0.85f, 5, 4)),
                CreateCard(
                    "blood_war_camp", "Blood War Camp", "king_blood", NKCardType.Building, NKTargetRule.EmptyPlot, NKBattleBehavior.Passive,
                    "Enemy camp that rewards an aggressive board when stolen.",
                    LoadSprite("Assets/Tiny Swords/Buildings/Red Buildings/Barracks.png", "Assets/Tiny Swords/Buildings/Red Buildings/House1.png"),
                    LoadSprite("Assets/Tiny Swords/Buildings/Red Buildings/Barracks.png", "Assets/Tiny Swords/Buildings/Red Buildings/House1.png"),
                    Stats(1, 10f, 0f, 1f, 1, 1),
                    Stats(2, 14f, 0f, 1f, 1, 1),
                    Stats(3, 18f, 0f, 1f, 1, 1)),
                CreateCard(
                    "nature_guardian", "Nature Guardian", "king_nature", NKCardType.Troop, NKTargetRule.EmptyPlot, NKBattleBehavior.Melee,
                    "Sturdy guardian that scales with time.",
                    LoadSprite("Assets/Tiny Swords/Units/Yellow Units/Lancer/Lancer_Idle.png", "Assets/Tiny Swords/Units/Blue Units/Lancer/Lancer_Idle.png"),
                    LoadSprite("Assets/Tiny Swords/Units/Yellow Units/Lancer/Lancer_Idle.png", "Assets/Tiny Swords/Units/Blue Units/Lancer/Lancer_Idle.png"),
                    Stats(1, 12f, 2.2f, 1.05f, 2, 1),
                    Stats(2, 15f, 3.0f, 1f, 3, 1),
                    Stats(3, 18f, 4.0f, 0.95f, 4, 1)),
                CreateCard(
                    "nature_archer", "Nature Archer", "king_nature", NKCardType.Troop, NKTargetRule.EmptyPlot, NKBattleBehavior.Ranged,
                    "Nature kingdom ranged unit with steady pressure.",
                    LoadSprite("Assets/Tiny Swords/Units/Yellow Units/Archer/Archer_Idle.png", "Assets/Tiny Swords/Units/Blue Units/Archer/Archer_Idle.png"),
                    LoadSprite("Assets/Tiny Swords/Units/Yellow Units/Archer/Archer_Idle.png", "Assets/Tiny Swords/Units/Blue Units/Archer/Archer_Idle.png"),
                    Stats(1, 8f, 2.0f, 0.95f, 3, 3),
                    Stats(2, 10f, 2.7f, 0.9f, 4, 3),
                    Stats(3, 12f, 3.5f, 0.85f, 5, 4)),
                CreateCard(
                    "nature_grove", "Nature Grove", "king_nature", NKCardType.Building, NKTargetRule.EmptyPlot, NKBattleBehavior.Passive,
                    "Recovered grove that supports broader unit growth.",
                    LoadSprite("Assets/Tiny Swords/Buildings/Green Buildings/House1.png", "Assets/Tiny Swords/Buildings/Yellow Buildings/House1.png", "Assets/Tiny Swords/Buildings/Blue Buildings/House2.png"),
                    LoadSprite("Assets/Tiny Swords/Buildings/Green Buildings/House1.png", "Assets/Tiny Swords/Buildings/Yellow Buildings/House1.png", "Assets/Tiny Swords/Buildings/Blue Buildings/House2.png"),
                    Stats(1, 10f, 0f, 1f, 1, 1),
                    Stats(2, 14f, 0f, 1f, 1, 1),
                    Stats(3, 18f, 0f, 1f, 1, 1)),
            };
        }

        private static List<NKRoyalDecreeDefinition> BuildRoyalDecrees()
        {
            return new List<NKRoyalDecreeDefinition>
            {
                new()
                {
                    decreeId = "golden_well",
                    displayName = "Golden Well",
                    description = "Cards discarded into the well grant +3 extra gold.",
                    effects = new List<NKCardEffectDefinition>
                    {
                        new() { effectType = NKEffectType.AddGold, amount = 3f, description = "Discarded cards grant +3 gold." },
                    },
                },
                new()
                {
                    decreeId = "tower_doctrine",
                    displayName = "Tower Doctrine",
                    description = "Towers feel more valuable in merchant and reward choices.",
                    effects = new List<NKCardEffectDefinition>
                    {
                        new() { effectType = NKEffectType.AddDamagePercent, amount = 0.1f, description = "Abstract tower-support rule." },
                    },
                },
                new()
                {
                    decreeId = "martial_drill",
                    displayName = "Martial Drill",
                    description = "Frontline troops start battles slightly stronger.",
                    effects = new List<NKCardEffectDefinition>
                    {
                        new() { effectType = NKEffectType.AddUnits, amount = 1f, description = "Frontline troop plots gain +1 unit." },
                    },
                },
                new()
                {
                    decreeId = "blessed_stone",
                    displayName = "Blessed Stone",
                    description = "Gain 1 life immediately.",
                    effects = new List<NKCardEffectDefinition>
                    {
                        new() { effectType = NKEffectType.AddLives, amount = 1f, description = "Gain 1 life." },
                    },
                },
            };
        }

        private static List<NKBlessingDefinition> BuildBlessings()
        {
            return new List<NKBlessingDefinition>
            {
                new()
                {
                    blessingId = "growth_blessing",
                    displayName = "Blessing of Growth",
                    description = "Chosen plot gains +1 extra unit each battle.",
                    effect = new NKCardEffectDefinition
                    {
                        effectType = NKEffectType.AddUnits,
                        amount = 1f,
                        description = "Add 1 unit.",
                    },
                },
                new()
                {
                    blessingId = "mastery_blessing",
                    displayName = "Blessing of Mastery",
                    description = "Chosen plot gains +25% damage.",
                    effect = new NKCardEffectDefinition
                    {
                        effectType = NKEffectType.AddDamagePercent,
                        amount = 0.25f,
                        description = "Increase damage by 25%.",
                    },
                },
            };
        }

        private static List<NKMerchantDefinition> BuildMerchants()
        {
            return new List<NKMerchantDefinition>
            {
                new()
                {
                    merchantId = "architect",
                    displayName = "Architect",
                    supportedCardTypes = new List<NKCardType> { NKCardType.Building, NKCardType.Tower },
                    stockCount = 3,
                    baseBuyCost = 30,
                    additionalBuyCost = 15,
                    baseRerollCost = 10,
                    rerollCostStep = 10,
                },
                new()
                {
                    merchantId = "sage",
                    displayName = "Sage",
                    supportedCardTypes = new List<NKCardType> { NKCardType.Enchantment, NKCardType.Tome, NKCardType.Building },
                    stockCount = 3,
                    baseBuyCost = 30,
                    additionalBuyCost = 15,
                    baseRerollCost = 10,
                    rerollCostStep = 10,
                },
                new()
                {
                    merchantId = "warmonger",
                    displayName = "Warmonger",
                    supportedCardTypes = new List<NKCardType> { NKCardType.Troop, NKCardType.Tower },
                    stockCount = 3,
                    baseBuyCost = 30,
                    additionalBuyCost = 15,
                    baseRerollCost = 10,
                    rerollCostStep = 10,
                },
            };
        }

        private static List<NKYearEventDefinition> BuildYearEvents()
        {
            return new List<NKYearEventDefinition>
            {
                Event(4, NKYearEventType.RoyalCouncil),
                Event(6, NKYearEventType.BlessingReveal),
                Event(8, NKYearEventType.DiplomatWar),
                Event(10, NKYearEventType.Merchant),
                Event(12, NKYearEventType.TowerExpand),
                Event(14, NKYearEventType.RoyalCouncil),
                Event(16, NKYearEventType.BlessingResolve),
                Event(19, NKYearEventType.TowerExpand),
                Event(21, NKYearEventType.RoyalCouncil),
                Event(23, NKYearEventType.DiplomatPeace),
                Event(25, NKYearEventType.Merchant),
                Event(27, NKYearEventType.TowerExpand),
                Event(29, NKYearEventType.RoyalCouncil),
                Event(31, NKYearEventType.Merchant),
                Event(33, NKYearEventType.FinalBattle),
            };
        }

        private static NKYearEventDefinition Event(int year, NKYearEventType eventType)
        {
            return new NKYearEventDefinition
            {
                year = year,
                eventType = eventType,
            };
        }

        private static NKCardDefinition CreateCard(
            string cardId,
            string displayName,
            string ownerKingId,
            NKCardType cardType,
            NKTargetRule targetRule,
            NKBattleBehavior behavior,
            string description,
            Sprite? plotSprite,
            Sprite? unitSprite,
            NKCardLevelStats level1,
            NKCardLevelStats level2,
            NKCardLevelStats level3,
            params NKCardEffectDefinition[] effects)
        {
            return new NKCardDefinition
            {
                cardId = cardId,
                displayName = displayName,
                ownerKingId = ownerKingId,
                cardType = cardType,
                targetRule = targetRule,
                battleBehavior = behavior,
                maxLevel = 3,
                upgradeable = cardType != NKCardType.Enchantment && cardType != NKCardType.Tome,
                infiniteStack = cardType == NKCardType.Enchantment,
                description = description,
                plotSprite = plotSprite,
                unitSprite = unitSprite,
                levels = new List<NKCardLevelStats> { level1, level2, level3 },
                effects = new List<NKCardEffectDefinition>(effects),
            };
        }

        private static NKCardLevelStats Stats(int level, float health, float attack, float attackInterval, int units, int range)
        {
            return new NKCardLevelStats
            {
                level = level,
                health = health,
                attack = attack,
                attackInterval = attackInterval,
                units = units,
                range = range,
            };
        }

        private static Sprite? LoadSprite(params string[] paths)
        {
            foreach (var path in paths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static void WriteImplementationLog()
        {
            var path = Path.Combine(DocsPath, "NineKings_ImplementationLog.md");
            var content = string.Join(
                "\n",
                "# NineKings Prototype Implementation Log",
                "",
                "## 当前阶段",
                "- 已进入 `33 年 Alpha + GM 调试` 的工程实现阶段。",
                "",
                "## 已完成",
                "- 建立 `Assets/NineKingsPrototype/` 独立模块目录。",
                "- 落地 SDD 与 Spec Tech 文档。",
                "- 建立运行时核心骨架：RunState、BattleController、GameController、GM、运行时 UI。",
                "- 建立默认内容数据库生成器。",
                "- 建立 `NineKings_Main.unity` 主场景生成器。",
                "- 接入 `King of Nothing` 9 张首版卡池。",
                "- 接入 `King of Blood` 与 `King of Nature` 两个敌王配置。",
                "- 接入 33 年完整事件日历。",
                "",
                "## 进行中",
                "- 完整 Alpha 的测试与交互验证。",
                "- GM 指令的 PlayMode 闭环验证。",
                "",
                "## 下一步",
                "- 跑 EditMode / PlayMode 测试。",
                "- 修正运行态交互与数值问题。",
                "- 补强存档、重开和 Final Battle 验收。",
                "",
                "## 风险与阻塞",
                "- 当前为高还原 Alpha，不是最终数值定稿版本。",
                "- 仍需通过场景实际运行来收敛拖牌、战斗节奏和弹窗流程。",
                "",
                "## 最近验证结果",
                "- 本次生成器已能重建默认内容数据库与主场景。",
                "- 待下一轮测试确认运行态闭环。",
                "");
            File.WriteAllText(path, content);
        }
    }
}
