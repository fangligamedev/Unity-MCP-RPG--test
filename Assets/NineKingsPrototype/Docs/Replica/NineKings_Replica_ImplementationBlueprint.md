# 9 Kings 复刻审阅包：实现蓝图

## 1. 文档目标
本文件定义 `NineKingsPrototype` 后续工程实现的完整技术蓝图。它不是代码设计细节清单，而是工程层、系统层、资源层、状态层的统一决策文档。

本文档要回答的问题是：
- 主场景如何组织
- 镜头如何固定
- 棋盘和拖拽如何工作
- 同景自动战斗如何拆成逻辑层与表现层
- 战后战利品如何叠加在原战场上
- 配置文件如何加载、校验和驱动表现

## 2. 总体架构结论
### 2.1 模块边界
- `NineKingsPrototype` 作为完全独立模块存在
- 不依赖 `Game2DRPG` 的运行时代码
- 允许复用 Unity 包、输入系统和可复用美术资源

### 2.2 数据驱动原则
- 运行时主控制器只调度状态，不存内容事实
- 内容事实来自 `ContentDatabase`
- 卡牌、单位、建筑、FX 都由配置驱动

### 2.3 表现驱动原则
- 棋盘格、建筑、兵力、战斗 FX、战利品页都属于场景表现，不属于纯 UGUI 面板
- 同景战斗必须保持世界层和 HUD 同时存在

## 3. 主场景与镜头方案
### 3.1 主场景
主场景固定为单场景：
- `NineKings_Main`

战斗、出牌、事件、战利品都发生在同一场景中，不切换战斗地图。

### 3.2 场景层级
- `WorldBackground`
- `BoardGround`
- `BoardCells`
- `PlacedStructures`
- `BattleUnits`
- `BattleFX`
- `HUD`
- `CardLayer`
- `OverlayModals`
- `DebugLayer`

### 3.3 镜头约束
- 高角度斜俯视
- 近似正交的 2.5D 视觉
- 16:9 为主设计基准
- 棋盘在镜头中居中偏下
- 左侧井和底部手牌必须常驻在可阅读区域

### 3.4 摄像机配置角色
- `BoardIdleCamera`：YearStart 与 CardPhase 主镜头
- `BattleCamera`：BattleDeploy / BattleRun 主镜头，同机位微调，不切镜头语言
- `OverlayCameraLock`：LootChoice / EventModal 时固定背景视图并压暗

## 4. 状态机蓝图
固定状态机：
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

### 4.1 状态职责表
| 状态 | 可见层 | 可交互层 | 进入条件 | 退出条件 | 需冻结/保留的数据 |
|---|---|---|---|---|---|
| MainMenu | HUD, OverlayModals | OverlayModals | 场景加载完成 | 开始新 run | 无 |
| RunIntro | WorldBackground, BoardGround, HUD | OverlayModals | 新局开始 | 初始化完成 | seed, playerKingId |
| YearStart | WorldBackground, BoardGround, BoardCells, HUD | HUD | 新一年开始 | 进入出牌阶段 | year state, event schedule |
| CardPhase | WorldBackground, BoardGround, BoardCells, PlacedStructures, HUD, CardLayer | BoardCells, CardLayer, HUD | 年开始处理完事件 | 手牌只剩 2 张或强制开战 | run state, board state |
| PlacementPreview | 同 CardPhase | BoardCells, CardLayer | 拖牌进入有效/无效目标 | 放置完成或取消 | drag session, preview state |
| BattleDeploy | 全部世界层 + HUD | HUD | 手牌锁定后 | 所有单位部署完成 | frozen hand, board -> battle snapshot |
| BattleRun | 全部世界层 + HUD | HUD 右上控速 | 部署完成 | 战斗胜负判定成立 | battle scene state |
| BattleResolve | WorldBackground, BoardGround, PlacedStructures, BattleUnits, BattleFX, HUD | 无 | 胜负判定成立 | 暗场与统计冻结完成 | battle result, survivors |
| LootChoice | WorldBackground, BoardGround, PlacedStructures, HUD, OverlayModals | OverlayModals | 战斗胜利且可拿牌 | 选牌或跳过完成 | loot candidates, reroll cost |
| EventModal | WorldBackground, BoardGround, PlacedStructures, HUD, OverlayModals | OverlayModals | 年度事件或特殊事件触发 | 玩家确认或选择完成 | event context |
| RunOver | WorldBackground, OverlayModals | OverlayModals | 生命归零或通关 | 新开局/退出 | final run summary |

## 5. 棋盘与拖拽系统
### 5.1 坐标系统
- 棋盘逻辑坐标统一使用 `BoardCoord`
- 数据层按逻辑方阵 `3x3 -> 5x5`
- 表现层投影成菱形格

### 5.2 拖拽系统子对象
- `CardHandState`
  - 维护当前手牌顺序、可用性、拖拽锁定状态
- `DragSession`
  - 维护当前拖拽卡、起点、屏幕位置、候选目标
- `PlacementValidator`
  - 判断放卡、升级、附魔、弃井、非法落点
- `PlacementPreviewState`
  - 管理高亮格、预览建筑、附魔边框、禁止图标

### 5.3 放置行为
#### 放卡
- 空格可接受 `Base / Troop / Tower / Building`
- 成功后在格子中生成或更新 `PlotState`

#### 升级
- 目标格已有同 `cardId` 的驻场卡时，拖入执行升级
- 等级上限以 `CardDefinition.maxLevel` 为准

#### 附魔
- `Enchantment` 仅允许附着到合法目标
- 不直接占用棋盘主卡槽

#### 弃井
- 左侧井始终存在为世界内固定交互点
- 丢牌进井进入经济结算，不创建棋盘实体

#### 非法落点
- 表现层必须出现：
  - 红色非法边框
  - 卡牌回弹
  - 不改变 `RunState`

### 5.4 自动锁定到剩 2 张牌
- 当 `CardHandState` 中剩余可处理手牌数为 `2` 时
- `CardPhase` 进入锁定结算
- 触发 `BattleDeploy`

## 6. 战斗系统技术方案
### 6.1 时间模型
- 固定步长实时
- 推荐逻辑步长：`0.1s`
- 视觉插值与逻辑 tick 解耦

### 6.2 分层结构
- `CombatSimulation`
  - 维护战斗真相源
  - 负责移动、索敌、攻击、伤害、死亡、胜负
- `CombatPresentation`
  - 读取战斗快照
  - 负责实例化单位、播放动画、特效、音效、角标、镜头反馈

### 6.3 战斗最小循环
每个 tick 固定按以下顺序：
1. `deploy`
2. `move`
3. `acquire target`
4. `attack`
5. `resolve damage`
6. `death cleanup`
7. `win/loss check`

### 6.4 单位与建筑共存规则
- 建筑始终保留在原始棋盘格位置
- 单位从对应格或边界入口生成
- 战斗中同一格允许“建筑 + 该格出兵的单位源”同时存在
- 建筑是驻场实体，单位是战斗实体

### 6.5 BattleDeploy
职责：
- 冻结手牌层
- 把 `BoardState` 转成 `BattleSceneState`
- 从每个 `troop-source` 生成友军单位
- 从敌王年度波次生成外圈敌军
- 激活控速按钮组

### 6.6 BattleResolve
职责：
- 冻结 combat tick
- 汇总胜负、伤害、击杀、金币、事件触发
- 处理残兵淡出、保留建筑、清理投射物
- 压暗背景并准备 `LootChoice`

## 7. 战利品界面方案
- `LootChoice` 是战场上的暗场叠层，不切场景
- 背景保留但整体降亮度和饱和度
- 居中显示 3 张候选卡
- 支持中英双语长度约束
- 候选卡完全由 `LootPoolDefinition + CardPresentationConfig` 驱动

## 8. 资源管理模式
### 8.1 第一版原则
- 不使用 Addressables 作为第一版硬依赖
- 第一版采用：
  - `ScriptableObject + Prefab + Sprite/Animation 引用`

### 8.2 目录拆分
- `Assets/NineKingsPrototype/Data/Definitions`
- `Assets/NineKingsPrototype/Data/Presentation`
- `Assets/NineKingsPrototype/Prefabs/Board`
- `Assets/NineKingsPrototype/Prefabs/Units`
- `Assets/NineKingsPrototype/Prefabs/Structures`
- `Assets/NineKingsPrototype/Prefabs/UI`
- `Assets/NineKingsPrototype/Art/Shared`
- `Assets/NineKingsPrototype/Art/NineKings`

### 8.3 资源引用规则
- 所有卡牌、单位、建筑、FX 都必须通过配置引用
- 控制器里禁止硬编码 Prefab 路径、Sprite 路径
- `ContentDatabase` 作为运行时统一入口

## 9. 配置文件系统方案
### 9.1 ContentDatabase
职责：
- 统一加载所有 authoring 配置
- 在运行时建立 ID -> 配置对象 索引
- 对外提供只读查询

### 9.2 配置分层
- `Identity`
- `Rules`
- `Combat`
- `Presentation`
- `Economy`
- `Events`

### 9.3 主键体系
- `kingId`
- `cardId`
- `unitArchetypeId`
- `weaponFxId`
- `spawnPatternId`
- `lootPoolId`

### 9.4 加载顺序
1. `Rules`
2. `Combat`
3. `Presentation`
4. `Identity`
5. `Economy`
6. `Events`
7. `ContentDatabase finalize`

### 9.5 校验顺序
1. 主键唯一性
2. 外键引用完整性
3. 等级块一致性
4. 资源引用完整性
5. 卡牌与棋盘目标规则一致性
6. 战利品池可解析性

### 9.6 Fallback 策略
- 缺少视觉资源：允许回退到占位 prefab
- 缺少战斗配置：禁止该卡进入运行时
- 缺少本地化：回退英文

## 10. 现有模块的保留与替换
### 10.1 可保留的规则骨架
- `RunState` 年份推进概念
- `BattleController` 的胜负入口概念
- `GM` 的调试入口

### 10.2 必须整体替换的表现层
- 当前纯 UGUI 棋盘
- 当前纯面板式 `RuntimeUI`
- 当前单位/建筑表现不足的战斗层
- 当前战利品弹窗的非暗场形式

## 11. 文档与实现的对应关系
- `CombatRoster`：决定卡牌级数值和显示规则
- `CombatDataSchema`：决定配置对象边界
- `CombatTechArchitecture`：决定战斗子系统边界
- `TechArchitecture`：决定模块总图
- `ImplementationBlueprint`：决定后续工程落地顺序与集成方案

## 12. 审阅结论标准
文档审阅通过时，必须能直接判断：
- 如何用一套配置对象驱动两个王国
- 如何从棋盘态切到同景战斗态
- 如何让建筑和兵力真实出现在战场里
- 如何让战利品页保持战场暗场叠层
- 如何在不引入 Addressables 的前提下，保持配置、资源和运行态结构清晰
