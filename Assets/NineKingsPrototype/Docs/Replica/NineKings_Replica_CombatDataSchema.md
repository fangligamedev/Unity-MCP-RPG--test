# 9 Kings 复刻审阅包：数据驱动战斗 Schema

## 1. 文档目标
本文件定义 `NineKingsPrototype` 后续工程实现所需的数据驱动框架。目标不是描述玩法体验，而是把“国家、卡牌、战斗、表现、战利品、运行态、持久化”拆成稳定的数据对象，使后续实现者可以在不改主控制器结构的前提下，持续扩展新国家与新卡牌。

本文档的结论是硬约束：
- 权威存储：`ScriptableObject`
- 导出与持久化：`JSON`
- 数据权威不落在控制器、不落在 UI、不落在场景对象
- 战斗配置与表现配置拆开，不允许混成一个大表

## 2. 数据总原则
### 2.1 Authoring 真相源
- 所有设计期内容都以 `ScriptableObject` 为真相源。
- `JSON` 只用于：
  - save / load
  - debug snapshot
  - 审阅快照
  - GM 导出

### 2.2 分层原则
数据分层固定为：
- `Identity`
- `Rules`
- `Combat`
- `Presentation`
- `Economy`
- `Events`

其中：
- `Identity` 只管身份，不管战斗数值
- `Combat` 只管模拟，不管 Sprite、Prefab、UI 色彩
- `Presentation` 只管可视、动画、FX、布局约束
- `RunState / PlotState / BattleSceneState` 属于运行态，不属于 authoring

### 2.3 依赖方向
唯一允许的依赖方向：

`KingDefinition / CardDefinition`
-> `CardCombatConfig / CardPresentationConfig`
-> `ContentDatabase`
-> `RunState / PlotState`
-> `BoardSceneState / BattleSceneState / PresentationSnapshot`

禁止：
- `Presentation` 反向依赖 `RunState`
- `RunState` 直接保存 Prefab / Sprite 引用
- `CardDefinition` 直接持有战斗模拟对象实例

## 3. 配置目录规范
根目录固定为：
- `Assets/NineKingsPrototype/Data/Definitions/`
- `Assets/NineKingsPrototype/Data/Presentation/`
- `Assets/NineKingsPrototype/Data/Curves/`
- `Assets/NineKingsPrototype/Data/Loot/`
- `Assets/NineKingsPrototype/Data/Events/`
- `Assets/NineKingsPrototype/Data/Databases/`
- `Assets/NineKingsPrototype/Data/DebugSnapshots/`

推荐二级目录：
- `Definitions/Kings`
- `Definitions/Cards`
- `Definitions/Units`
- `Definitions/Spawns`
- `Definitions/Rules`
- `Presentation/Cards`
- `Presentation/Buildings`
- `Presentation/Units`
- `Presentation/FX`
- `Presentation/Localization`

## 4. ID 命名规范
### 4.1 主键格式
- `kingId`：`king-greed`、`king-nothing`
- `cardId`：`greed-palace`、`nothing-soldier`
- `unitArchetypeId`：`greed-thief-scout`、`enemy-melee`
- `weaponFxId`：`fx-beam-palace`
- `spawnPatternId`：`spawn.front-arc-tight`
- `lootPoolId`：`loot.greedy-win-basic`

### 4.2 命名约束
- 全部小写
- 单词间用 `-`
- 类型域与值域分开
- 不允许同义别名
- 运行态 JSON 中只保存主键，不保存 Unity 对象路径

## 5. 核心 Authoring 对象
### 5.1 KingDefinition
职责：
- 王国身份、主题、可用卡池、敌对出场规则、基础战斗特性

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| kingId | string | 主键 |
| displayNameZh | string | 中文名 |
| displayNameEn | string | 英文名 |
| themeTag | string | 主题标签 |
| uiPaletteId | string | UI 色板引用 |
| cardIds | string[] | 可用卡池 |
| starterDeckCardIds | string[] | 起始牌组 |
| lootPoolIds | string[] | 战利品来源 |
| enemyWaveProfileId | string | 敌王刷怪规则 |
| specialRuleIds | string[] | 王国特性 |

### 5.2 CardDefinition
职责：
- 定义卡牌身份、目标规则、升级结构和描述

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| cardId | string | 主键 |
| ownerKingId | string | 所属王国 |
| displayNameZh | string | 中文名 |
| displayNameEn | string | 英文名 |
| cardType | enum | Base / Troop / Tower / Building / Enchantment / Tome |
| rarityTag | string | 牌池标签 |
| maxLevel | int | 最高等级 |
| targetRule | string | 放置 / 附着规则 |
| stackRuleId | string | 数量显示规则 |
| descriptionZh | string | 中文描述 |
| descriptionEn | string | 英文描述 |
| combatConfigId | string | 战斗配置引用 |
| presentationConfigId | string | 表现配置引用 |

### 5.3 CardCombatConfig
职责：
- 定义该卡的战斗数值、出兵、攻击方式与战斗行为

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| combatConfigId | string | 主键 |
| cardId | string | 所属卡牌 |
| spawnsUnits | bool | 是否出兵 |
| unitArchetypeId | string | 单位原型 |
| spawnPatternId | string | 出兵模式 |
| deployDelay | float | 部署延迟 |
| spawnCooldown | float | 周期刷出或周期攻击 |
| reinforceRuleId | string | 增援规则 |
| levelStatBlocks | list | L1/L2/L3 数值块 |
| behaviorProfileId | string | 战斗行为配置 |
| onHitEffectIds | string[] | 命中特效/逻辑 |
| onKillEffectIds | string[] | 击杀效果 |
| auraEmitterId | string | 光环配置 |

### 5.4 CardPresentationConfig
职责：
- 定义棋盘实体、战斗实体、Prefab、Sprite、动画、双语布局

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| presentationConfigId | string | 主键 |
| cardId | string | 所属卡牌 |
| worldObjectType | string | 棋盘实体类型 |
| unitVisualType | string | 单位视觉类型 |
| boardPrefab | ObjectRef | 棋盘态 prefab |
| battlePrefab | ObjectRef | 战斗态 prefab |
| iconSprite | ObjectRef | 卡面 icon |
| illustrationSprite | ObjectRef | 卡面图 |
| animationProfileId | string | 动画配置 |
| weaponFxId | string | 武器特效 |
| hitFxId | string | 受击特效 |
| deathFxId | string | 死亡特效 |
| lootFxId | string | 战利品特效 |
| localizationLayoutId | string | 双语布局限制 |

### 5.5 UnitArchetypeDefinition
职责：
- 定义通用单位原型，供卡牌复用

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| unitArchetypeId | string | 主键 |
| combatRole | enum | 单位定位 |
| sizeClass | string | tiny / small / medium / large |
| moveProfileId | string | 移动模型 |
| attackProfileId | string | 攻击模型 |
| baseStats | struct | 默认单位数值 |
| defaultStackRuleId | string | 数量显示规则 |
| visualTypeId | string | 视觉类型 |

### 5.6 SpawnPatternSpec
职责：
- 定义单位如何从格子、建筑、外圈入口生成到战场

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| spawnPatternId | string | 主键 |
| anchorType | string | plot-center / plot-front / edge-entry |
| formationType | string | line / arc / wedge / swarm |
| slotOffsets | vector[] | 局部偏移 |
| deploymentFacing | string | toward-center / toward-enemy |
| staggerDelay | float | 队列出场间隔 |

### 5.7 WeaponFXSpec
职责：
- 把战斗武器特效从逻辑中解耦

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| weaponFxId | string | 主键 |
| fxType | string | melee / projectile / beam / aura / burst |
| projectilePrefab | ObjectRef | 投射物 |
| trailPrefab | ObjectRef | 拖尾 |
| impactPrefab | ObjectRef | 命中 |
| soundGroupId | string | 音效组 |
| screenShakeProfileId | string | 震屏 |
| timingProfileId | string | 延迟与节奏 |

### 5.8 StackDisplayRule
职责：
- 定义兵力数量如何从“真实数量”映射到“可视显示”

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| stackRuleId | string | 主键 |
| oneUnitThreshold | int | 单体阈值 |
| visibleActorCap | int | 最多展示多少主单位 |
| useResidualGhosts | bool | 是否显示残影 |
| showCountBadgeAt | int | 角标阈值 |
| badgeFormat | string | xN / N |

### 5.9 BattleCurveDefinition
职责：
- 统一年增长曲线、Final Battle 修正和建筑/单位成长倍率

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| curveId | string | 主键 |
| yearMultiplierTable | float[] | 年份倍率 |
| eliteMultiplier | float | 精英倍率 |
| bossMultiplier | float | Boss 倍率 |
| finalBattleModifierId | string | 终局修正 |
| economyInflationPerYear | float | 经济膨胀 |

### 5.10 LootPoolDefinition
职责：
- 定义战后 3 选 1 的候选来源和权重

建议字段：
| 字段 | 类型 | 说明 |
|---|---|---|
| lootPoolId | string | 主键 |
| sourceKingIds | string[] | 来源王国 |
| allowedCardTypes | string[] | 允许牌类 |
| excludeCardIds | string[] | 排除项 |
| weightEntries | list | 权重表 |
| rerollCostBase | int | 重掷基础价 |

## 6. 运行态对象
### 6.1 RunState
职责：
- 表示一局 run 的逻辑真相源

必须字段：
- `runId`
- `seed`
- `currentYear`
- `lives`
- `gold`
- `rerollCost`
- `playerKingId`
- `activeEnemyKingId`
- `drawPileCardIds`
- `discardPileCardIds`
- `handCardInstanceIds`
- `selectedDecreeIds`
- `eventFlags`
- `boardState`

### 6.2 PlotState
职责：
- 表示一个棋盘格的逻辑状态

必须字段：
- `boardCoord`
- `isUnlocked`
- `cardId`
- `cardLevel`
- `enchantmentIds`
- `temporaryEffectIds`
- `occupancyTag`
- `battleCache`

### 6.3 BoardSceneState
职责：
- 作为逻辑层到表现层的棋盘快照

必须字段：
- `visiblePlots`
- `highlightedCoord`
- `previewCardId`
- `previewValidity`
- `placedStructureInstances`
- `unitSourceMarkers`

### 6.4 BattleSceneState
职责：
- 当前战斗的表现快照

必须字段：
- `battleId`
- `phase`
- `friendlyEntities`
- `enemyEntities`
- `projectileEntities`
- `activeFxIds`
- `speedMode`
- `winner`

### 6.5 PresentationSnapshot
职责：
- 用于 GM、回放、战后冻结与截图

必须字段：
- `runStateJson`
- `boardSceneJson`
- `battleSceneJson`
- `timestamp`
- `cameraProfileId`

## 7. 配置引用关系
### 7.1 强引用
- `KingDefinition -> CardDefinition[]`
- `CardDefinition -> CardCombatConfig`
- `CardDefinition -> CardPresentationConfig`
- `CardCombatConfig -> UnitArchetypeDefinition`
- `CardCombatConfig -> SpawnPatternSpec`
- `CardPresentationConfig -> WeaponFXSpec`
- `CardDefinition / KingDefinition -> LootPoolDefinition`

### 7.2 弱引用
- `RunState` 只存 ID，不存 Unity 引用
- `PlotState` 只存 ID 与运行态数值
- `BoardSceneState / BattleSceneState` 只存表现快照 ID 与轻量可视状态

### 7.3 禁止循环依赖
禁止：
- `CardPresentationConfig -> RunState`
- `WeaponFXSpec -> CardDefinition`
- `SpawnPatternSpec -> CardCombatConfig`

允许：
- `ContentDatabase` 在加载后建立索引缓存，但不反写 authoring 资产

## 8. JSON 导出与保存范围
### 8.1 正式存档
正式存档 JSON 只保存：
- `RunState`
- `PlotState[]`
- 当前事件上下文
- 当前敌王
- 当前重掷成本
- 已持有卡牌实例状态

### 8.2 调试快照
调试快照 JSON 额外保存：
- `BoardSceneState`
- `BattleSceneState`
- `PresentationSnapshot`

### 8.3 不进入 JSON 的内容
- Prefab 路径
- Sprite / Animation 引用
- 运行时 view instance id
- 任何 UnityEngine.Object 直接引用

## 9. 加载顺序
固定加载顺序：
1. `BattleCurveDefinition`
2. `StackDisplayRule`
3. `SpawnPatternSpec`
4. `WeaponFXSpec`
5. `UnitArchetypeDefinition`
6. `CardCombatConfig`
7. `CardPresentationConfig`
8. `CardDefinition`
9. `KingDefinition`
10. `LootPoolDefinition`
11. `ContentDatabase`

原因：
- 先有底层战斗和表现基础规则，再加载卡牌与王国

## 10. 校验顺序
固定校验顺序：
1. 主键唯一性
2. 必填字段完整性
3. 外键可解析性
4. 等级块数量一致性
5. `cardType` 与 `presenceType` 兼容性
6. `spawnsUnits` 与 `unitArchetypeId` 对齐
7. `weaponFxId` 与 `damageType` 对齐
8. `stackRuleId` 与 `unitCountBase` 对齐
9. `LootPoolDefinition` 不引用不存在卡牌

## 11. Fallback 规则
当配置缺失时：
- `WeaponFXSpec` 缺失：回退到 `fx.none`
- `StackDisplayRule` 缺失：回退到 `single`
- `CardPresentationConfig` 缺失：阻止进入运行态
- `CardCombatConfig` 缺失且该卡是 `Tome`：允许以即时牌降级
- `CardCombatConfig` 缺失且该卡是 `Troop / Tower / Building`：直接校验失败

## 12. 审阅结论标准
文档审阅通过时，应能直接确认：
- 哪些对象是 authoring 真相源
- 哪些对象只存在于运行态
- 卡牌、战斗、表现和战利品如何通过统一 schema 驱动
- SO 与 JSON 的边界是否明确
- 内容库是否具备扩展第二、第三区王国的能力
