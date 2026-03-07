# Tiny Swords Spec Tech: Architecture

## 1. 文档目标
本文件定义 Tiny Swords 双模式地图系统的技术架构，用于指导后续 Unity 工程实现。

本文件回答以下问题：
- 系统要拆成哪些模块
- 每个模块在 Editor、Runtime、数据层分别负责什么
- `RoomChain` 与 `OpenWorld` 共用什么、分开什么
- 地图、动画、遭遇、保存加载如何串起来
- 实施阶段如何通过 Unity MCP 和自定义编辑器脚本落地

## 2. 技术基线
- Unity 版本：`6000.3.10f1`
- 工程路径：`/Users/brucef/Documents/UnityProject/test/4-up-2d-rpg`
- SDD 输入：
  - `TinySwords_ResourceAtlas.md`
  - `TinySwords_MapRules.md`
  - `TinySwords_MapSystem_SDD.md`
- 资源真相源：`Assets/Tiny Swords`
- 现有可复用运行时：
  - `TopDownPlayerController`
  - `PlayerCombat`
  - `EnemyBrainTorchGoblin`
  - `EnemyBrainTntGoblin`
  - `ArenaGameState`
  - `RewardShrine`
  - `HudPresenter`
  - `CameraFollow2D`

## 3. 实施顺序
实现顺序固定为 3 段，不允许并行打散：

### 3.1 Phase 1: 共享底座
- 资源扫描
- 资源图谱 ScriptableObject
- JSON 导出与导入
- Tile 规则与动画激活规则资产
- Map Builder 编辑器窗口骨架
- 场景构建公共服务

### 3.2 Phase 2: RoomChain
- 7 房间样板关卡构建
- 房间模板与 RoomChain 布局资产
- `RoomChainGenerator`
- `RoomChain` 保存/加载
- `RoomChain` EditMode / PlayMode 验证

### 3.3 Phase 3: OpenWorld
- 5 区域样板世界构建
- 区域模板与 Overworld 布局资产
- `OpenWorldGenerator`
- 区域遭遇与动画预算
- `OpenWorld` 保存/加载
- `OpenWorld` EditMode / PlayMode 验证

## 4. 模块划分
系统分成 7 个模块，目录和程序集都按这个边界落地。

### 4.1 Resource Catalog
职责：
- 扫描 `Assets/Tiny Swords`
- 识别静态资源、环境动画、战斗反馈动画
- 建立统一资源语义与白名单
- 导出 `resource-catalog.json`

主要类型：
- `ResourceCatalogAsset`
- `ResourceFamilyDefinition`
- `ResourceEntryDefinition`
- `AnimatedVariantDefinition`
- `CombatAssetRegistry`

### 4.2 Tile Rule Library
职责：
- 定义静态图层和动态图层
- 定义 Tile 语义、导航语义、动画通道
- 定义房间和区域合法性约束

主要类型：
- `TileLayerRuleAsset`
- `AmbientAnimationProfileAsset`
- `TileSemantic`
- `AnimationChannel`
- `ActivationPolicy`

### 4.3 Scene Build Core
职责：
- 在 Scene 内创建 Grid、Tilemap、Tilemap Renderer、Collider、Marker Root
- 负责统一的层级结构、Sorting Layer、Object 命名
- 负责构建和清空地图

主要类型：
- `MapSceneAssembler`
- `TilemapLayerHandle`
- `MapBuildContext`
- `MapSceneRoots`

### 4.4 Mode-Specific Builders
职责：
- `RoomChainBuilder`：按房间拓扑生成地图
- `OpenWorldBuilder`：按区域布局生成连续大地图

主要类型：
- `RoomChainGenerator`
- `OpenWorldGenerator`
- `RoomTemplateAsset`
- `RegionTemplateAsset`
- `LevelLayoutAsset`
- `OverworldLayoutAsset`

### 4.5 Encounter & Marker Bridge
职责：
- 把地图系统产生的 `SpawnMarkers`、事件区、资源点、奖励点映射到现有运行时
- 负责遭遇区、召唤区、巡逻区的生成定义

主要类型：
- `EncounterDefinition`
- `RegionEncounterDefinition`
- `WorldEventMarker`
- `MapRuntimeBinder`

### 4.6 Save / Load
职责：
- SO -> JSON 导出
- JSON -> Scene 重建
- 保存当前地图实例
- 加载历史地图配置

主要类型：
- `MapSaveData`
- `OpenWorldSaveData`
- `MapJsonExporter`
- `MapJsonImporter`
- `MapPersistenceService`

### 4.7 Editor Tools
职责：
- 提供 `Map Builder` 窗口
- 承载一键扫描、一键样板构建、一键 PCG、一键保存/加载
- 显示配置状态、最后一次生成摘要、最近一次验证结果

主要类型：
- `MapBuilderWindow`
- `MapBuilderController`
- `MapBuilderState`

## 5. 目录结构
实现阶段固定新增以下目录结构。

```text
Assets/Game2DRPG
├── Data
│   ├── Catalog
│   ├── Rules
│   ├── Templates
│   │   ├── RoomChain
│   │   └── OpenWorld
│   ├── Layouts
│   └── Saves
├── Docs
├── Scenes
│   ├── TinySwordsArena.unity
│   ├── TinySwords_RoomChain_Showcase.unity
│   └── TinySwords_OpenWorld_Showcase.unity
├── Scripts
│   ├── Editor
│   │   ├── Catalog
│   │   ├── Rules
│   │   ├── Builders
│   │   ├── Persistence
│   │   └── Windows
│   └── Runtime
│       ├── Map
│       ├── Encounter
│       ├── Animation
│       └── Persistence
└── Tests
    ├── EditMode
    └── PlayMode
```

## 6. 程序集划分
新增程序集固定为：
- `Game2DRPG.Map.Editor`
- `Game2DRPG.Map.Runtime`
- `Game2DRPG.Map.EditMode.Tests`
- `Game2DRPG.Map.PlayMode.Tests`

依赖关系固定为：
- `Game2DRPG.Map.Editor` 依赖 `Game2DRPG.Runtime`
- `Game2DRPG.Map.Runtime` 依赖 `Game2DRPG.Runtime`
- 编辑器测试依赖 Editor 和 Runtime 两个地图程序集
- PlayMode 测试依赖 Runtime 地图程序集和现有运行时程序集

## 7. 场景层级结构
所有地图场景固定采用以下根层级：

```text
SceneRoot
├── MapRoot
│   ├── GridRoot
│   │   ├── BGColor
│   │   ├── WaterFoam
│   │   ├── FlatGround
│   │   ├── Shadow_L1
│   │   ├── ElevatedGround_L1
│   │   ├── Shadow_L2
│   │   ├── ElevatedGround_L2
│   │   ├── AnimatedWater
│   │   ├── AnimatedShoreline
│   │   ├── AnimatedVegetation
│   │   └── NavigationMask
│   ├── DecorationRoot
│   ├── InteractiveRoot
│   ├── MarkerRoot
│   └── AmbientFxRoot
├── GameplayRoot
│   ├── PlayerRoot
│   ├── EncounterRoot
│   ├── RuntimeBinder
│   └── RewardRoot
├── CameraRoot
└── UIRoot
```

说明：
- Tilemap 层统一放在 `GridRoot`
- 非 Tilemap 动态物件和交互物件统一放在单独 Root 下
- 所有生成器都必须遵循同一层级，不允许模式间各自起名

## 8. 编辑器窗口设计
### 8.1 窗口入口
- 菜单：`Tools/Game2DRPG/Map Builder`
- 类型：`EditorWindow`

### 8.2 窗口固定区域
- `Catalog`
  - 扫描按钮
  - 最近扫描时间
  - 目录摘要
- `Rules`
  - 当前规则资产引用
  - 动画预算摘要
- `Showcase`
  - `Build RoomChain Showcase`
  - `Build OpenWorld Showcase`
- `PCG`
  - Mode 选择
  - Seed 输入
  - `Generate RoomChain`
  - `Generate OpenWorld`
- `Persistence`
  - `Save Current Level Config`
  - `Load Level Config`
- `Verification`
  - 最近一次测试摘要

### 8.3 窗口状态模型
窗口必须缓存以下状态：
- 当前模式
- 最近使用 seed
- 当前 Rule Asset
- 当前 Catalog Asset
- 最近生成场景路径
- 最近导出 JSON 路径

## 9. 运行时桥接架构
### 9.1 Bridge 职责
`MapRuntimeBinder` 在场景生成完成后负责：
- 放置玩家出生点
- 绑定 `WaveDirector` 或开放世界区域遭遇
- 绑定奖励点、资源点、事件点
- 绑定 Camera 跟随目标
- 初始化动画激活服务

### 9.2 动画激活服务
运行时新增 `AnimationActivationService`，负责：
- 常驻通道常开：`AnimatedWater`、`AnimatedShoreline`
- 近邻通道：`AnimatedVegetation`、`AmbientProps`
- 事件通道：`ReactiveFX`

激活源：
- 摄像机位置
- 玩家所在房间 / 区域
- 遭遇状态
- 交互状态

### 9.3 Encounter Bridge
- `RoomChain` 下，遭遇以房间为单位触发
- `OpenWorld` 下，遭遇以区域内子区块触发
- 现有 `Torch` / `TNT` 生成仍通过预制体注册，不改敌人行为逻辑

## 10. 双模式生成流水线
### 10.1 共享底座流水线
1. 扫描资源目录
2. 生成或更新 `ResourceCatalogAsset`
3. 校验 `TileLayerRuleAsset`
4. 选择模式
5. 创建或清空目标场景
6. 初始化 `SceneRoot`

### 10.2 RoomChain 流水线
1. 选择 `LevelLayoutAsset` 或 seed
2. 生成房间图
3. 分配房型与模板
4. 绘制 Tilemap
5. 布置动画对象
6. 布置怪物点、召唤点、奖励点
7. 运行合法性检查
8. 绑定运行时
9. 保存场景与配置

### 10.3 OpenWorld 流水线
1. 选择 `OverworldLayoutAsset` 或 seed
2. 生成区域图
3. 铺设连续地形
4. 布置区域资源、建筑和装饰
5. 布置动画对象
6. 布置预置遭遇区、巡逻区、召唤区
7. 运行可达性和动画预算检查
8. 绑定运行时
9. 保存场景与配置

## 11. Unity MCP 落地路径
实现阶段优先使用现有 Unity MCP 能力，无法直接表达的部分由自定义编辑器脚本承接。

### 11.1 直接可用的 MCP 能力
- 资产搜索：`assets-find`
- 资产复制/移动/刷新：`assets-copy`、`assets-move`、`assets-refresh`
- 场景操作：`scene-create`、`scene-open`、`scene-save`、`scene-get-data`
- GameObject 操作：`gameobject-create`、`gameobject-find`、`gameobject-modify`
- 脚本生成：`script-update-or-create`
- 测试：`tests-run`

### 11.2 必须通过 Unity 侧自定义脚本补齐的能力
- 资源目录扫描与语义标注
- Rule Asset 生成
- EditorWindow UI
- 双模式生成器
- SO -> JSON 导出和 JSON -> 地图重建
- 动画激活运行时服务

### 11.3 MCP 实施原则
- MCP 负责驱动工程修改、脚本生成、场景创建和验证
- 复杂生成逻辑必须沉淀为 Unity Editor C# 脚本，而不是每次靠 MCP 临时拼装场景

## 12. 风险与约束
- `OpenWorld` 第一版不支持流式分块，因此地图尺寸和同屏动画预算必须受控
- `Torch/TNT` 不在原始 Tiny Swords 目录内，必须作为扩展战斗资产注册
- 现有 `WaveDirector` 面向单房间，开放世界实现时需要区域遭遇桥接，而不是直接复用整套波次控制
- Tilemap 与动态装饰混合后，层级、Sorting、Collider 和可达性检查必须一起验证

## 13. 输出物
实现完成后必须形成以下实体：
- 共享规则资产
- RoomChain 样板场景
- OpenWorld 样板场景
- 双模式生成器
- 双模式保存/加载
- 自动化测试与说明文档

## 16. 决策记录：RoomChain-first Compliance Pass
- 时间：2026-03-07
- 决策：暂停继续扩写 OpenWorld，先对 RoomChain 做规范化重构。
- 原因：旧生成器虽然已经能导出和重建场景，但视觉结果仍是“矩形平铺单 tile + 静态装饰”，和 Tiny Swords 官方 tilemap guide 的语法不一致。
- 本次重构固定优先级：
  1. 语义网格
  2. Tile 规则表
  3. Water Foam / Shadow / Cliff / Stairs
  4. 占格碰撞
  5. Animator 优先的环境动画
- 影响：
  - RoomChain 成为当前视觉和规则的权威实现
  - OpenWorld 暂时只保留兼容，不作为当前验收目标
