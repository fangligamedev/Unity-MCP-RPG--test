# 9 Kings 复刻审阅包：技术架构文档

## 1. 文档目标
本文件定义 `NineKingsPrototype` 从当前原型状态升级到“数据驱动 + 同景自动战斗 + 战后暗场战利品”的总体技术架构。

它回答的是模块级问题：
- 内容数据库怎么组织
- 棋盘、战斗、战利品之间的数据如何流动
- 哪些系统保留规则骨架，哪些系统必须整体替换表现层

## 2. 模块边界
### 2.1 独立性
- `NineKingsPrototype` 保持完全独立模块
- 不依赖 `Game2DRPG` 运行时代码
- 允许复用 Unity 包、可复用美术资源和开发工具链

### 2.2 本轮架构目标
重点不是只做“视觉复刻”，而是形成一套可继续扩展王国与卡池的玩法 + 表现一体架构。

## 3. 内容数据库加载图
### 3.1 逻辑真相源
`ContentDatabase`
-> `KingDefinition`
-> `CardDefinition`
-> `CardCombatConfig`
-> `CardPresentationConfig`
-> `UnitArchetypeDefinition`
-> `SpawnPatternSpec`
-> `WeaponFXSpec`
-> `LootPoolDefinition`
-> `BattleCurveDefinition`

### 3.2 运行时流转
`ContentDatabase`
-> `RunState`
-> `BoardState / PlotState`
-> `BoardSceneState`
-> `BattleSceneState`
-> `PresentationSnapshot`

### 3.3 原则
- 内容数据库是只读运行时入口
- 控制器拿 ID 查配置，不直接硬编码资源路径
- 所有系统共享一套主键空间

## 4. 场景结构语义
主场景固定按以下语义层组织：
- `WorldBackground`
- `BoardGround`
- `BoardCells`
- `PlacedStructures`
- `BattleUnits`
- `WorldProps`
- `BattleFX`
- `HUD`
- `CardLayer`
- `OverlayModals`
- `DebugLayer`

### 4.1 WorldBackground
- 地面
- 墙体
- 庭院背景
- 大型静态环境轮廓

### 4.2 BoardGround
- 棋盘嵌入地面的区域基底

### 4.3 BoardCells
- 菱形格线
- 可用/不可用状态
- 悬停高亮
- 放置预览

### 4.4 PlacedStructures
- Base
- Building
- Tower
- 驻场 enchant 锚点表现

### 4.5 BattleUnits
- 友军单位
- 敌方单位
- 编队显示对象

### 4.6 WorldProps
- 井
- 石块
- 废墟
- 角落装饰

### 4.7 BattleFX
- 投射物
- 射线
- 近战命中特效
- 金币特效
- 附魔特效

### 4.8 HUD
- 金币
- 年份
- 时间线
- 右上控速

### 4.9 CardLayer
- 手牌带
- 拖拽中的卡
- 预览卡

### 4.10 OverlayModals
- 战利品界面
- 事件弹层
- 对局结束页

### 4.11 DebugLayer
- GM 面板
- 调试信息

## 5. 状态机
后续实现必须显式遵守以下状态机：
- `MainMenu`
- `RunIntro`
- `YearStart`
- `CardPhase`
- `PlacementPreview`
- `BattleDeploy`
- `BattleRun`
- `BattleResolve`
- `LootChoice`
- `EventModal`
- `RunOver`

### 状态切换原则
- `CardPhase` 与 `BattleRun` 是同一场景的不同表现态
- `LootChoice` 和 `EventModal` 是暗场覆盖层
- `BattleDeploy` 与 `BattleResolve` 都是独立状态，不能偷并进同一个函数

## 6. Board -> Combat -> Loot 数据流
### 6.1 Board
输入：
- `RunState`
- `CardHandState`
- `PlacementValidator`

输出：
- 更新后的 `PlotState`
- `BoardSceneState`

### 6.2 Combat
输入：
- `RunState`
- `PlotState[]`
- `CardCombatConfig`
- `BattleCurveDefinition`

输出：
- `BattleSceneState`
- `BattleResultSummary`
- `RunState` 的金币、生命、事件变化

### 6.3 Loot
输入：
- `BattleResultSummary`
- `LootPoolDefinition`
- 当前敌王与王国上下文

输出：
- 3 张候选卡
- 更新后的牌库 / 手牌 / 重掷成本

## 7. 表现层职责拆分
### 7.1 世界表现协调层
- 场景背景与棋盘空间关系
- 井、边界、装饰显隐

### 7.2 棋盘表现层
- 3x3 到 5x5 菱形格生成
- 可用格、锁定格、预览格高亮

### 7.3 卡牌表现层
- 手牌带
- 拖拽、缩放、预览、回弹
- 战斗态弱化或收起

### 7.4 战斗表现层
- 单位入场
- 编队显示
- 攻击、投射物、受击、击杀
- 控速按钮状态

### 7.5 覆盖界面层
- 战利品页
- 事件弹层
- 对局结束页

## 8. 数据与接口层约束
### RunPresentationSpec
- 当前年份显示规则
- 顶栏信息优先级
- 各阶段主视图配置

### BoardPresentationSpec
- 棋盘尺寸
- 菱形透视
- 可用格与锁定格视觉差

### CardViewSpec
- 手牌卡面
- 拖拽缩放
- 预览态
- 战利品卡面

### BattlePresentationSpec
- 战斗镜头
- 单位缩放
- 友军/敌军出场方向
- 控速按钮行为

### LootScreenSpec
- 暗场程度
- 标题排版
- 三卡间距
- 选中反馈

### TimelineHUDSpec
- 年份
- 节点位置
- 事件图标

### LocalizationLayoutSpec
- 英文版长度边界
- 中文版长度边界
- 标题和正文换行策略

### ScreenStateDefinition
- 各状态的可见层
- 各状态的可交互层
- 弱化规则

## 9. 现有系统的保留与替换
### 9.1 可保留为规则骨架
- 现有 `RunState` 概念
- 年份推进和事件入口
- GM 入口

### 9.2 必须整体替换的表现层
- 当前平面化棋盘
- 当前 UGUI 主导的主战场表现
- 当前战斗显示层
- 当前战后页面表现

### 9.3 半保留半替换
- `NineKingsBattleController`
  - 保留：胜负概念、波次概念
  - 替换：表现层、同景单位层、部署和清场过渡
- `NineKingsRuntimeUI`
  - 保留：顶栏字段概念
  - 替换：布局、层级、卡牌带、战斗态与暗场战利品

## 10. 审阅结论标准
通过标准：
- 可以清楚看出 `ContentDatabase` 在运行时的位置
- 可以清楚看出 `Board -> Combat -> Loot` 的完整数据流
- 可以判断当前原型哪些逻辑骨架还能复用，哪些表现层必须推倒重做
- 可以明确知道后续实现不需要再自己发明数据边界、加载顺序和状态切换规则

## 7. 现有实现需要替换或重做的部分
### 7.1 NineKingsRuntimeUI
当前问题：
- 偏纯 UGUI 面板布局
- 世界层与 UI 层关系未分离
- 顶栏、侧栏、底栏更像工具界面

后续方向：
- 拆成世界层、HUD 层、卡牌层、覆盖层
- 不再用面板式左右分栏作为主布局

### 7.2 NineKingsBattleController
当前问题：
- 战斗逻辑骨架存在，但画面呈现不符合 9 Kings 的战场语法
- 单位和建筑的视觉关系、推进关系和战斗观感不足

后续方向：
- 保留规则骨架
- 重做表现层与战斗态镜头组织

### 7.3 棋盘显示方式
当前问题：
- 更像 UI 网格
- 与世界背景脱节

后续方向：
- 重构为场景地面中的菱形棋盘
- 让棋盘成为环境的一部分，而不是漂浮于 UI

### 7.4 GM 系统
当前结论：
- 保留为开发辅助层
- 不纳入本轮复刻审阅范围
- 在正式体验稿中只作为 DebugLayer 存在

## 8. 数据流与交互流
### 数据流
- 内容数据库提供卡牌、王国、事件与文本
- 对局状态驱动当前可见屏幕与世界布局
- HUD 和战斗表现共同消费对局状态

### 交互流
- 手牌拖拽驱动棋盘预览
- 棋盘确认后驱动状态更新
- 状态更新驱动世界表现刷新
- 手数达到阈值后驱动战斗阶段切换
- 战斗结果驱动战利品覆盖层

## 9. 实现阶段的顺序建议
后续真正进入实现时，应按以下顺序推进：
1. 主场景表现重构
2. 菱形棋盘与井
3. 底部手牌与拖拽预览
4. 顶部 HUD 与右上控速
5. 自动战斗表现层
6. 战后战利品页
7. 事件弹层统一皮肤

## 10. 审阅结论标准
这份技术文档通过的标准是：
- 后续实现者不需要再猜场景层次怎么拆
- 不需要再猜哪些现有系统要保留、哪些要重做
- 表现层和状态机之间的关系已经明确
- 复刻目标已经从“感觉像”变成“结构上必须像”
