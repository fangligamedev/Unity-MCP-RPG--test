#nullable enable
using UnityEngine;

namespace NineKingsPrototype
{
    public static class NKChineseText
    {
        public static string Phase(NKRunPhase phase, bool paused)
        {
            var text = phase switch
            {
                NKRunPhase.Boot => "启动中",
                NKRunPhase.MainMenu => "主菜单",
                NKRunPhase.YearStart => "年初结算",
                NKRunPhase.Event => "事件处理中",
                NKRunPhase.CardPlay => "出牌阶段",
                NKRunPhase.Battle => "自动战斗",
                NKRunPhase.Reward => "奖励阶段",
                NKRunPhase.GameOver => "失败结算",
                NKRunPhase.Victory => "胜利结算",
                _ => phase.ToString(),
            };
            return paused ? text + "（已暂停）" : text;
        }

        public static string CardType(NKCardType type)
        {
            return type switch
            {
                NKCardType.Base => "基地",
                NKCardType.Troop => "兵种",
                NKCardType.Tower => "塔楼",
                NKCardType.Building => "建筑",
                NKCardType.Enchantment => "附魔",
                NKCardType.Tome => "法术",
                _ => type.ToString(),
            };
        }

        public static string Bool(bool value) => value ? "是" : "否";

        public static string CardName(NKCardDefinition? definition)
        {
            return definition == null ? string.Empty : CardName(definition.cardId, definition.displayName);
        }

        public static string CardName(string cardId, string fallback = "")
        {
            return cardId switch
            {
                "castle" => "城堡",
                "archer" => "弓箭手",
                "paladin" => "圣骑士",
                "soldier" => "士兵",
                "scout_tower" => "哨塔",
                "blacksmith" => "铁匠铺",
                "farm" => "农场",
                "steel_coat" => "钢铁披甲",
                "wildcard" => "百搭战术",
                "blood_raider" => "血袭者",
                "blood_archer" => "血弓手",
                "blood_war_camp" => "血战营地",
                "nature_guardian" => "自然守卫",
                "nature_archer" => "自然射手",
                "nature_grove" => "自然林地",
                _ => string.IsNullOrEmpty(fallback) ? cardId : fallback,
            };
        }

        public static string CardDescription(NKCardDefinition? definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            return definition.cardId switch
            {
                "castle" => "王国核心。敌军突破到这里时，你会失去生命。",
                "archer" => "远程兵种，能在敌军靠近前持续输出。",
                "paladin" => "高生存近战单位，适合稳住前线。",
                "soldier" => "廉价前排，适合配合农场和铁匠铺成长。",
                "scout_tower" => "固定防御塔，射程远、输出稳定。",
                "blacksmith" => "相邻兵种和塔楼每年都会获得更多伤害。",
                "farm" => "相邻兵种地块在每年开始时获得额外单位数。",
                "steel_coat" => "给目标兵种地块增加护盾层，可重复叠加。",
                "wildcard" => "即时将目标地块提升 1 级。",
                "blood_raider" => "血王国的近战冲锋单位，进攻压力高。",
                "blood_archer" => "血王国的远程压制单位，出手更快。",
                "blood_war_camp" => "血王国的进攻建筑，夺取后会强化进攻节奏。",
                "nature_guardian" => "自然王国的高韧性守卫，越往后越难处理。",
                "nature_archer" => "自然王国的稳定远程单位，持续消耗能力强。",
                "nature_grove" => "自然王国的成长建筑，夺取后偏向稳健发育。",
                _ => definition.description,
            };
        }

        public static string KingName(string kingId, string fallback = "")
        {
            return kingId switch
            {
                "king_nothing" => "虚无之王",
                "king_blood" => "鲜血之王",
                "king_nature" => "自然之王",
                _ => string.IsNullOrEmpty(fallback) ? kingId : fallback,
            };
        }

        public static string DecreeName(string decreeId, string fallback = "")
        {
            return decreeId switch
            {
                "golden_well" => "黄金之井",
                "tower_doctrine" => "塔楼教义",
                "martial_drill" => "军阵操演",
                "blessed_stone" => "祝圣之石",
                _ => string.IsNullOrEmpty(fallback) ? decreeId : fallback,
            };
        }

        public static string DecreeDescription(string decreeId, string fallback = "")
        {
            return decreeId switch
            {
                "golden_well" => "丢进井里的卡牌额外再获得 3 金币。",
                "tower_doctrine" => "塔楼在奖励与商人选择中的价值更高。",
                "martial_drill" => "前线兵种开战时额外获得 1 单位。",
                "blessed_stone" => "立即获得 1 点生命。",
                _ => fallback,
            };
        }

        public static string BlessingName(string blessingId, string fallback = "")
        {
            return blessingId switch
            {
                "growth_blessing" => "成长祝福",
                "mastery_blessing" => "精通祝福",
                _ => string.IsNullOrEmpty(fallback) ? blessingId : fallback,
            };
        }

        public static string BlessingDescription(string blessingId, string fallback = "")
        {
            return blessingId switch
            {
                "growth_blessing" => "所选地块在每场战斗中额外获得 1 单位。",
                "mastery_blessing" => "所选地块获得 25% 额外伤害。",
                _ => fallback,
            };
        }

        public static string MerchantName(string merchantId, string fallback = "")
        {
            return merchantId switch
            {
                "architect" => "建筑师",
                "sage" => "贤者",
                "warmonger" => "战狂",
                _ => string.IsNullOrEmpty(fallback) ? merchantId : fallback,
            };
        }

        public static string EventName(NKYearEventType eventType)
        {
            return eventType switch
            {
                NKYearEventType.RoyalCouncil => "王室议会",
                NKYearEventType.BlessingReveal => "祝福显现",
                NKYearEventType.BlessingResolve => "祝福生效",
                NKYearEventType.DiplomatWar => "外交：宣战",
                NKYearEventType.DiplomatPeace => "外交：议和",
                NKYearEventType.Merchant => "商人来访",
                NKYearEventType.TowerExpand => "塔楼扩地",
                NKYearEventType.FinalBattle => "最终决战",
                _ => "无事件",
            };
        }
    }
}