# 9 Kings 复刻审阅包：战斗技术架构文档

## 1. 文档目标
本文件定义后续把 `NineKingsPrototype` 的战斗表现重构成 9 Kings 式同景自动战斗时，必须遵守的战斗子系统技术架构。

重点不是写数值，而是写清：
- 战斗状态机
- `CombatSimulation` 与 `CombatPresentation` 的边界
- 建筑与兵力如何从棋盘态切到战斗态
- 单位如何显示、堆叠、命中、死亡和清理
- 战后如何进入暗场战利品

## 2. 战斗状态机
固定状态：
- `CardPhase`
- `PlacementPreview`
- `BattleDeploy`
- `BattleRun`
- `BattleResolve`
- `LootChoice`

### 2.1 CardPhase
- 允许拖牌、放牌、升级、附魔、弃井
- 棋盘为静态布局态
- 战斗单位层不可见

### 2.2 PlacementPreview
- 作为 `CardPhase` 子状态
- 负责目标格高亮、建筑 ghost、附魔边框、非法落点

### 2.3 BattleDeploy
- 锁定手牌与拖拽
- 把 `PlotState` 转成 `BattleSpawnRequest`
- 实例化驻场建筑
- 生成友军单位与敌军入口
- 初始化控速和 tick 时钟

### 2.4 BattleRun
- 固定步长实时推进
- 只开放控速、暂停和观战
- 不允许再出牌

### 2.5 BattleResolve
- 冻结 tick
- 汇总伤害、击杀、残兵、金币与战斗结果
- 处理残兵淡出、建筑保留、投射物清理
- 准备暗场和战利品候选

### 2.6 LootChoice
- 保留原战场背景
- 只开放战利品三选一
- 不再运行战斗模拟

## 3. CombatSimulation 与 CombatPresentation
### 3.1 CombatSimulation
职责：
- 维护战斗真相源
- 推进移动、索敌、攻击、伤害、死亡和胜负
- 产出 battle snapshot

输入：
- `RunState`
- `PlotState[]`
- `BattleCurveDefinition`
- `CardCombatConfig`
- `UnitArchetypeDefinition`
- `SpawnPatternSpec`

输出：
- `BattleSceneState`
- `BattleResultSummary`
- `KillRewardDelta`

### 3.2 CombatPresentation
职责：
- 把 `BattleSceneState` 转成世界内实例、动画、FX、音效与数量显示
- 绝不拥有战斗真相源

输入：
- `BattleSceneState`
- `CardPresentationConfig`
- `WeaponFXSpec`
- `StackDisplayRule`

输出：
- `BattleEntityView`
- `FXInstance`
- `HUD 战斗状态`

### 3.3 边界原则
- `CombatSimulation` 不直接操作 Sprite / Animator / Prefab
- `CombatPresentation` 不做伤害公式和胜负判定
- 两者通过 `BattleEntityState` 和 `BattleSceneState` 交接

## 4. 核心状态对象
### 4.1 BattleEntityState
表示单个战斗实体的逻辑状态。

建议字段：
- `entityId`
- `side`
- `sourceCardId`
- `unitArchetypeId`
- `worldRole`
- `currentHp`
- `currentShield`
- `currentArmor`
- `position`
- `velocity`
- `targetEntityId`
- `statusEffects`
- `stackCount`
- `isStructure`
- `isAlive`

### 4.2 BattleEntityView
表示单个逻辑实体在表现层的 view。

建议字段：
- `entityId`
- `prefabRef`
- `transformRef`
- `animatorRef`
- `stackBadgeRef`
- `selectionRef`
- `currentVisualState`

### 4.3 BattleSpawnRequest
表示从棋盘态进入战斗态时，需要生成的实体请求。

建议字段：
- `sourcePlotCoord`
- `sourceCardId`
- `spawnPatternId`
- `unitArchetypeId`
- `stackCount`
- `deployDelay`
- `team`

## 5. TickConfig
固定步长实时配置必须抽成独立对象，不能散在控制器里。

建议字段：
- `logicTickSeconds`
- `maxCatchupTicks`
- `speedMultiplierNormal`
- `speedMultiplierFast`
- `speedMultiplierUltra`
- `pauseAllowed`
- `snapshotInterval`

## 6. 目标选择与命中链路
### 6.1 RetargetPolicy
定义索敌与换目标规则。

建议字段：
- `policyId`
- `priorityMode`
- `rangeCheckMode`
- `reacquireInterval`
- `allowRetargetOnHit`
- `allowRetargetOnDeath`

### 6.2 OnHitEffectSpec
定义一次命中后挂载的效果。

建议字段：
- `effectId`
- `damageType`
- `bonusDamage`
- `applyDot`
- `applyGoldBurst`
- `applyArmorShred`
- `applySlow`
- `spawnImpactFxId`

### 6.3 统一命中链路
战斗内一次攻击固定流程：
1. 目标检查
2. 命中方式判定
3. 基础伤害
4. 护甲 / 护盾结算
5. `OnHitEffectSpec`
6. 击杀判定
7. `KillRewardRule`

## 7. AuraEmitterSpec
定义驻场建筑或单位释放光环的结构。

建议字段：
- `auraEmitterId`
- `radius`
- `targetFilter`
- `effectIds`
- `pulseInterval`
- `visualFxId`

应用对象：
- `Beacon`
- `Blacksmith`
- 未来的 buff 建筑和特殊单位

## 8. KillRewardRule
定义击杀后产生的经济或表现反馈。

建议字段：
- `ruleId`
- `goldOnKill`
- `spawnLootFxId`
- `applyToOwnerCard`
- `applyToWholeRun`
- `requiresMarkedTarget`

典型应用：
- `Thief`
- `Midas Touch`
- `Palace`

## 9. FinalBattleModifier
定义 Final Battle 的倍率和附加规则。

建议字段：
- `modifierId`
- `enemyHpMultiplier`
- `enemyDamageMultiplier`
- `enemySpawnDensityMultiplier`
- `specialBossRuleIds`
- `lootSuppression`

## 10. 场景层与表现职责
### WorldBackground
- 战斗背景与空间感

### BoardGround / BoardCells
- 维持棋盘在战斗中的可读性

### PlacedStructures
- Base、Tower、Building、Aura 物体

### BattleUnits
- 友军和敌军战斗实体

### BattleFX
- 投射物、射线、金币、爆点、附魔高亮

### HUD
- 年份、金币、速度控制、战斗结果文案

### OverlayModals
- 战后战利品、事件弹层

## 11. 建筑与兵力生成规则
### 11.1 建筑生成
每个 `structure / tower / base` 类卡牌必须定义：
- 棋盘态 prefab
- 战斗态是否沿用同 prefab
- 是否拥有开火点、光环点或受击点

### 11.2 兵力生成
每个 `troop-source` 类卡牌必须定义：
- 出兵位置
- 编队显示策略
- 战斗中如何与建筑共存

### 11.3 敌军生成
敌军必须：
- 从外圈或边缘入口生成
- 与友军共享世界战场
- 不可退化为纯 UI 数字

## 12. 现有系统的保留与替换边界
### 12.1 NineKingsBattleController
保留：
- 战斗开始/结束入口
- 胜负概念
- 敌军波次概念

替换：
- 单位显示层
- 建筑与兵力事实落地
- 投射物 / 射线 / 命中 / 死亡表现
- 暗场战利品过渡

### 12.2 NineKingsRuntimeUI
保留：
- 顶栏字段概念
- 基本状态组织概念

替换：
- 纯面板式棋盘
- 纯面板式战斗显示
- 非同景的战利品表现

## 13. GM 调试接口边界
GM 只能：
- 读取 `RunState`
- 读取 `BoardSceneState`
- 读取 `BattleSceneState`
- 通过受控入口注入状态修改

GM 不得：
- 直接修改 `BattleEntityView`
- 直接操作动画或 Prefab 层
- 绕过 `CombatSimulation`

## 14. 审阅结论标准
通过标准：
- 可以清楚区分模拟层和表现层
- 可以明确知道建筑和兵力如何从棋盘态转成战斗态
- 可以明确知道命中、击杀、光环、终局修正分别挂在哪一层
- GM 如何观察与改写状态而不污染主架构已经明确
- 投射物与特技反馈层

### 7.2 NineKingsRuntimeUI
保留：
- 文本与数据绑定方向
- GM 层可作为 DebugLayer 延续

替换：
- 纯 UGUI 的战斗主视图
- 棋盘显示方式
- 战利品层
- 控速按钮组表现

## 8. 战后到战利品的过渡架构
### BattleResolve
必须负责：
- 统计冻结
- 背景状态冻结或减速
- 清理不必要的实时战斗逻辑
- 准备暗场

### LootChoice
必须负责：
- 压暗战场
- 居中战利品卡
- 结果标题
- 与原战场的视觉连续性

要求：
- 不切场
- 不加载另一套战利品背景
- 不用纯 UI 全屏替换世界

## 9. 双王国并行架构
后续实现时必须支持：
- `King of Greed` 与 `King of Nothing` 共用一套战斗表现框架
- 但两者可通过 roster、render spec 和 visual language 做差异化

即：
- 架构统一
- 表现参数可配置

## 10. 审阅结论标准
通过标准：
- 任何实现者都能看清战斗表现层如何分层
- 都能看清建筑和兵力的生成接口应该怎样定义
- 都能看清当前实现哪些可以保留、哪些必须重做
- 都能看清战斗到战利品的过渡为何必须是同景暗场，而不是切页面
