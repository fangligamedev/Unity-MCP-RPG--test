# 9 Kings 竞品拆解与数值基线

## 1. 文档目的
这份文档只做两件事：
- 用官方公开资料拆解 9 Kings 的核心战斗玩法、卡牌机制、战斗模式与关键数值。
- 为本项目的单王国 Alpha 提供高还原但可实现的设计基线。

本文件不讨论实现类、代码结构、程序集或脚本接口，只讨论玩法事实、设计规律和对本项目的启发。

## 2. 研究来源
本分析只以官方公开资料为事实基础：
- Steam 商店页：https://store.steampowered.com/app/2784470/9_Kings/
- 官方 Wiki 主循环：https://wiki.hoodedhorse.com/9_Kings/Run
- 官方 Wiki 卡牌：https://wiki.hoodedhorse.com/9_Kings/Cards
- 官方 Wiki 地块：https://wiki.hoodedhorse.com/9_Kings/Plots
- 官方 Wiki 王令：https://wiki.hoodedhorse.com/9_Kings/Royal_Council
- 官方 Wiki 商人：https://wiki.hoodedhorse.com/9_Kings/Merchants
- 官方 Wiki 祝福：https://wiki.hoodedhorse.com/9_Kings/Blessings
- 官方 Wiki 国王与初始卡池：https://wiki.hoodedhorse.com/9_Kings/Kings

## 3. 游戏定位拆解
9 Kings 不是单一品类游戏，而是四种结构的叠加：
- 回合制构筑：每年先做出牌与弃牌决策。
- 自动战斗：一旦进入战斗，玩家不再直接操作单位移动与攻击。
- 王国城建：卡牌放在地块上形成版图、邻接和成长。
- 事件经济：金币、商人、王令、祝福、外交与扩地共同构成中长期节奏。

因此它的关键乐趣不在即时微操，而在于：
- 你如何构筑一个会自动滚雪球的王国。
- 你如何在有限年份内把资源转成版图强度。
- 你如何通过跨王国抢牌，逐步把局内构筑打碎再重组。

## 4. 核心循环拆解
### 4.1 单局长度
- 一局固定为 33 年。
- 第 33 年是 Final Battle。
- 通关后可以继续 Endless，但本项目首版不把 Endless 作为目标。

### 4.2 生命系统
- 每局有 3 条命。
- 任何敌方单位接触到王国 Base 就会失去 1 条命。
- Final Battle 中一旦失命，整局直接失败，不管还剩多少命。
- 官方资料中唯一明确的失命恢复方式是 Rebirth 王令。

### 4.3 开局状态
- 初始王国为 3x3，共 9 个空地块。
- 初始手牌 4 张：1 张 Base 卡 + 3 张所选王国卡。
- 第一张必定是 Base 卡，且必须放在空地块上。

### 4.4 年度回合
每个年份的玩家行为分成 3 段：
1. 查看本年攻击的敌对王国与事件。
2. 打牌或弃牌，直到手里只剩 2 张牌。
3. 自动战斗并结算。

这个结构非常重要。它意味着：
- 手牌不是越多越好，手牌本身是要被转化成版图或金币的资源。
- 玩家每年都有出牌截止线，不存在无限拖回合。
- 自动战斗是构筑结果的检验，而不是手操补救阶段。

### 4.5 战后收益
- 每战结束无论输赢固定获得 9 Gold。
- 若战斗胜利，从被击败敌王牌池拿 1 张牌。
- 若战斗失败，从自己王国牌池拿 1 张牌。

这个设计带来的影响：
- 失败不是纯惩罚，而是提供自家体系修复机会。
- 胜利是扩展构筑边界的主要来源。
- 打谁、和谁开战，直接决定你能从谁身上偷到什么体系部件。

## 5. 年度事件节奏
官方 Run 页给出的 33 年节奏非常清晰，几乎就是整局 pacing 的骨架。

### 5.1 固定事件表
- 第 4、14、21、29 年：Royal Council
- 第 6 年：Blessing reveal
- 第 16 年：Blessing effect
- 第 8 年：Diplomat War
- 第 23 年：Diplomat Peace
- 第 10、25、31 年：Merchant
- 第 12、19、27 年：Tower 扩地
- 第 33 年：Final Battle

### 5.2 事件设计含义
- 第 4 年就给王令，说明游戏很早就开始要求 build direction。
- 第 6 到第 16 年的 Blessing 是延迟兑现机制，迫使玩家预留格子或规划卡位。
- 第 8 与第 23 年的外交事件会改变后续可偷牌的敌王来源，属于局内 meta 决策。
- 第 12、19、27 年的扩地把版图成长和年份推进严格绑定，避免玩家过快膨胀。
- 第 25 与第 31 年商人出现在后半局，意味着金币在中后期会持续有高价值出口。

## 6. 版图与地块规则
### 6.1 Plot 基础规则
- 初始 9 格，为 3x3。
- 最多解锁外圈 16 格，扩成 5x5，共 25 格。
- 每个 Plot 只能承载一个基础对象，但可以再叠 Tome 或 Enchantment 类效果。

### 6.2 卡牌落格规则
- Base、Troop、Building、Tower 放在空 Plot 上会激活该 Plot。
- 放在同类型已占 Plot 上会升级。
- Tome 打在已占 Plot 上会立刻触发效果。
- Enchantment 打在 Troop 上会给予可叠加的增强。

### 6.3 扩地规则
官方资料明确列出几种扩地来源：
- 固定年份的 Tower 扩地事件。
- Exploration、Wilderness 王令。
- Earthworks 卡。
- Endless 模式下永久存在的 Tower 买地。

对首版 Alpha 的启发是：
- 扩地必须既有固定节奏，也有少数卡牌/王令打破节奏。
- 版图的稀缺性是前中期的主要张力来源之一。

## 7. 卡牌分类与成长机制
### 7.1 六类卡牌
官方资料明确把卡牌分成六类：
- Base
- Troop
- Tower
- Building
- Enchantment
- Tome

其中 Base、Troop、Tower、Building 是占地与升级的主类；Enchantment 与 Tome 是 build 放大器。

### 7.2 升级规则
- Base、Troop、Tower、Building 默认最高 3 级。
- Enchantment 无限叠加。
- Tome 是一次性立即生效。
- 少数特殊规则可以突破常规上限，例如某些 perk 或卡效果。

### 7.3 设计启发
这套分类结构决定了游戏不是传统牌库构筑，而是：
- 卡牌就是地块内容。
- 升级就是空间上的叠放与替换。
- 真正的 build 深度来自邻接、升级、叠加和触发次数，而不是复杂的费用系统。

## 8. 战斗模式分析
### 8.1 战斗不是独立关卡
战斗就在同一张王国地图上进行，不切到独立战斗场景。

### 8.2 战斗结构
- 敌军从王国边界或外部入口压入。
- Base 是最后防线。
- Troop 从其所处 Plot 出战并推进或接战。
- Tower 在固定位置持续输出。
- Building 更多是年触发、战斗触发或邻接增益来源。

### 8.3 为什么是 auto battler + tower defense
它并不是传统横向自动战棋，而更接近：
- 你先用卡牌定义一个会产兵、会增益、会发射、会爆发的防御系统。
- 然后系统自动演算敌我接触、远程输出、召唤、叠层和 base pressure。

因此本项目在高还原时，必须把战斗理解为“版图状态的年度结算器”，而不是独立的战斗小游戏。

## 9. 经济数值基线
### 9.1 固定金币
- 每战后固定得到 9 Gold。
- 丢牌进井得到 9 Gold。

这是一个非常鲜明的设计语言：
- 9 是主题数字，也被反复当作基础经济单位。
- 玩家会很快形成对 9、18、27、30、45 的价值直觉。

### 9.2 商人价格
官方 Merchant 页给出的规则：
- 商人库存 6 张牌。
- 第一张购买价 30 Gold。
- 同一商人每多买一张，价格 +15 Gold。
- 初始重掷价 10 Gold。
- 每次重掷再 +10 Gold。
- 新商人的购牌价格会重置到 30，但重掷价格不会重置。

### 9.3 战后重掷
Cards 页明确写到：
- 战后拿牌也能重掷。
- 战后重掷同样按 +10 Gold 递增。

### 9.4 经济含义
这意味着金币不是单纯消费资源，而是：
- 用来修正构筑方向。
- 用来赌更好的卡池结果。
- 用来在年份中期弥补坏手牌和事件压力。

## 10. King of Nothing 典型卡牌数值
官方 Wiki 给出了 King of Nothing 的完整 9 卡池，这是最适合作为首版可玩王国的起点。

### 10.1 九张牌清单
- Castle
- Archer
- Paladin
- Soldier
- Scout Tower
- Blacksmith
- Farm
- Steel Coat
- Wildcard

### 10.2 代表性数值
Castle
- 1 级伤害 10
- 2 级伤害 12.5
- 3 级伤害 15.63

Archer
- 1 级：12 HP，2.3 Damage，0.58 Hits/s，5 Crit，9 Units
- 2 级：15 HP，2.88 Damage，0.73 Hits/s，6 Crit，18 Units
- 3 级：18.75 HP，3.59 Damage，0.91 Hits/s，7 Crit，27 Units

Paladin
- 1 级：37 HP，8 Damage，0.25 Hits/s，3 Units
- 2 级：55.5 HP，12 Damage，0.25 Hits/s，6 Units
- 3 级：83.25 HP，18 Damage，0.25 Hits/s，9 Units

Soldier
- 1 级：23 HP，3 Damage，0.5 Hits/s，9 Units
- 2 级：28.75 HP，3.75 Damage，0.63 Hits/s，18 Units
- 3 级：35.94 HP，4.69 Damage，0.78 Hits/s，27 Units

Scout Tower
- 1 级：25 Damage，0.5 Hits/s，20 Crit
- 2 级：37.5 Damage，0.63 Hits/s，20 Crit
- 3 级：56.25 Damage，0.78 Hits/s，20 Crit

Blacksmith
- 每年给相邻 troop 与 building 增加伤害
- 1 级 +2%
- 2 级 +4%
- 3 级 +6%

Farm
- 每年给一个相邻 troop 增加单位数
- 1 级 +1
- 2 级 +2
- 3 级 +3

Steel Coat
- 每场战斗取消目标单位要承受的第一击
- 可无限叠加

Wildcard
- 目标 Plot 直接升 1 级

### 10.3 王国风格总结
King of Nothing 的风格非常适合做首版原因如下：
- 直观，容易教会玩家。
- 有 troop、tower、building、enchantment、tome 的完整代表。
- 有邻接增益，也有即时升级。
- 没有过度依赖异常复杂的召唤和规则变形。

## 11. 设计启发：首版必须还原什么
首版必须还原：
- 33 年年度循环。
- 3 条命与 Base 失命逻辑。
- 3x3 起步、5x5 封顶的 Plot 版图。
- 六类卡牌的功能分工。
- 打牌/弃牌到剩 2 张再开战。
- 自动战斗。
- 战后 9 Gold、战后选卡。
- 商人、王令、祝福、扩地、外交这些节奏事件。
- 单王国完整卡池 + 敌王掉牌。

可以延后：
- 完整 9 王国
- 全量 perk 解锁体系
- Endless
- 高级难度
- 复杂视觉特效与高保真美术
- 完整成就与进度系统

## 12. 对本项目的直接结论
如果要做一个高还原 Alpha，本项目不应该从动作战斗切入，而应该从这四件事切入：
- Plot 版图系统
- 六类卡牌的数据与交互
- 年度循环与事件日历
- 同图自动战斗结算

换句话说，9 Kings 的最小可玩版本不是“先做战斗角色”，而是“先做会自己运转的王国”。
