# Tiny Swords 地图系统 SDD

## 1. 项目目标
本设计文档定义一套基于 Tiny Swords 原始资源包的地图系统，用于替换当前固定单房间 Arena 的纯手工场景方案，形成：
- 可扫描的资源目录配置
- 可规则化构建的样板关卡
- 可随机生成的 `RoomChain` 多房间 roguelike 关卡
- 可随机生成的 `OpenWorld` 连续大地图关卡
- 可保存和可加载的地图配置
- 可最大化利用原包动态资源的地图世界

本阶段只做 SDD，不改代码，不改场景，不落地编辑器工具。

## 2. 当前工程现状
当前工程中已经存在一套可玩的战斗垂直切片：
- 玩家移动、冲刺、攻击
- Torch / TNT 两类敌人
- 波次推进、奖励点、胜负流程
- 顶部 HUD 与相机跟随

现有固定场景：
- `Assets/Game2DRPG/Scenes/TinySwordsArena.unity`

现有场景构建器：
- `Assets/Game2DRPG/Scripts/Editor/Game2DRPGBuilder.cs`

现状问题：
- 它构建的是单房间固定示例
- 它依赖的是 `Assets/Game2DRPG/Art/TinySwords` 的精简资源
- 它不是资源扫描器，也不是通用地图系统，更不是双模式 PCG 系统

因此新地图系统的职责不是“再做一个大房间”，而是把世界构建抽象出来，并支持两种地图模式。

## 3. 设计范围
### 3.1 本阶段进入范围
- 资源地图与动画资源图谱
- 官方 Tilemap 规则的项目化翻译
- `RoomChain` 样板关卡设计
- `OpenWorld` 样板世界设计
- 双模式 PCG 生成流程设计
- 配置文件设计
- 与现有战斗系统的对接边界

### 3.2 本阶段不进入范围
- Boss AI
- 元进度系统
- 商店系统
- 剧情与对话
- 经营 / 采集经济循环
- 完整战斗态存档
- 流式分块 Streaming

## 4. 核心设计原则
### 4.1 资源真相来源单一
- 所有地图资源真相以 `Assets/Tiny Swords` 为准
- 当前 `Assets/Game2DRPG/Art/TinySwords` 只视为玩法切片的筛选副本

### 4.2 地图采用 Tilemap 驱动
- 地图系统必须回归 Unity Tilemap
- 资源组织和分层关系必须匹配官方 Tilemap Guide

### 4.3 动画是地图规则的一部分
- 水体、泡沫、植被、环境点缀、战斗反馈动画都必须被写进规则
- 不允许把动画留到“实现时再说”

### 4.4 双模式并列
- `RoomChain` 和 `OpenWorld` 是并列模式
- 两者共用资源图谱、图层规则、战斗系统和保存体系
- 两者使用不同的布局生成器和样板结构

### 4.5 配置先于生成
- 先有资源图谱和规则资产
- 再有样板关卡与样板世界构建
- 最后再做 PCG

## 5. 系统概览
后续技术实现将由以下 7 个子系统组成。

### 5.1 Resource Catalog
职责：
- 扫描 `Assets/Tiny Swords`
- 建立资源家族、语义标签、动画标签和白名单
- 为构建器和双模式 PCG 提供可检索目录

设计对象：
- `ResourceCatalogAsset`
- `resource-catalog.json`
- `AnimatedVariantDefinition`

### 5.2 Tile Rule Library
职责：
- 定义静态 Tile 层顺序
- 定义动态图层 / 动画通道
- 定义高地、阴影、泡沫、水边规则
- 定义房间、区域与路径合法性约束

设计对象：
- `TileLayerRuleAsset`
- `AmbientAnimationProfileAsset`

### 5.3 RoomChain Builder
职责：
- 按固定拓扑构建 7 房间样板关卡
- 服务多房间 roguelike 模式

设计对象：
- `RoomTemplateAsset`
- `LevelLayoutAsset`
- `MapBuildRequest`

### 5.4 OpenWorld Builder
职责：
- 构建连续大地图样板世界
- 服务开放世界模式

设计对象：
- `RegionTemplateAsset`
- `OverworldLayoutAsset`

### 5.5 Dual PCG Generator
职责：
- `RoomChainGenerator`：生成多房间拓扑
- `OpenWorldGenerator`：生成区域式连续大地图
- 两者都输出可复现 seed 与保存配置

设计对象：
- `PCGProfileAsset`
- `RoomChainProfile`
- `OpenWorldProfile`

### 5.6 Save / Load Layer
职责：
- 保存关卡结构
- 支持重建与调试复现
- 不承担完整战斗态存档

设计对象：
- `MapSaveData`
- `OpenWorldSaveData`
- `EncounterDefinition`
- `RegionEncounterDefinition`

### 5.7 Runtime Bridge
职责：
- 把地图系统生成的点位与现有战斗系统绑定
- 驱动玩家出生、怪物布置、召唤、奖励点和出口交互
- 负责动画激活上下文与区域事件桥接

设计对象：
- `MapDefinition`
- `MapMode`
- `AnimationActivationContext`
- `WorldEventMarker`

## 6. `RoomChain` 样板关卡设计
样板关卡固定为 **7 房间串联+分支** 的拓扑，优先兼顾地形展示与可玩战斗。

### 6.1 拓扑图
```text
Start Room
   |
Narrow Pass
   |
Elevation Arena ---- Bridge Room ---- Reward Room
   |
Resource Court
   |
Exit / Boss Gate
```

说明：
- `Narrow Pass` 是主线收束口，负责从起点导向战斗核心区
- `Elevation Arena` 是主战斗房
- `Bridge Room -> Reward Room` 是支线奖励路径
- `Resource Court` 是资源语义房，同时承担节奏缓冲
- `Exit / Boss Gate` 是流程终点

### 6.2 房间规格
| 房间 | 建议尺寸 | 核心地形 | 核心玩法 |
|---|---:|---|---|
| Start Room | 14x12 | FlatGround | 出生、教学、轻装饰 |
| Narrow Pass | 12x8 | 水边窄路 | 路径引导、轻压迫 |
| Elevation Arena | 18x16 | 高地 + 阴影 + 楼梯 | 主战斗、走位与高差 |
| Bridge Room | 12x8 | 跨水连接 | 召唤战、过桥压迫 |
| Reward Room | 12x10 | 平地 + 地标 | 奖励与恢复 |
| Resource Court | 16x14 | 平地 + 资源点 + 建筑 | 轻战斗、资源语义展示 |
| Exit / Boss Gate | 14x10 | 平地 + 强地标 | 流程收束与出口 |

### 6.3 动态世界接入要求
- `Narrow Pass` 必须包含常驻水体动画和岸线动画
- `Elevation Arena` 必须包含至少 1 组战斗反馈动画
- `Resource Court` 必须包含至少 1 组区域环境动画
- `Reward Room` 可以克制，但需保留少量环境动效强化奖励感

## 7. `OpenWorld` 样板世界设计
这是本次新增模式，定义为 **单场景连续大地图**。

### 7.1 区域结构
开放世界固定为 5 个连续区域：
- `Spawn Meadow`
- `Wetland Belt`
- `Resource Forest`
- `Ruined Village`
- `High Plateau Citadel`

### 7.2 区域职责
#### Spawn Meadow
- 玩家出生与基础引导
- 平地、低密度装饰、轻量动态植被

#### Wetland Belt
- 大面积水域和岸线
- 常驻动态水体和泡沫
- 小规模开放战斗

#### Resource Forest
- 树木、木材、Sheep、资源点
- 高密度植被动画
- 预置遭遇与资源交互混合

#### Ruined Village
- House / Barracks / Monastery 组合
- 事件点、奖励点、召唤点
- 火焰、Dust、局部环境反馈

#### High Plateau Citadel
- 高地 + 城塞
- 强地标和终点区域
- 更克制的动画预算，强调终局感

### 7.3 开放世界玩法要求
- 可自由连续移动
- 存在开放区域战斗
- 存在预置怪物区、巡逻区、触发召唤区
- 存在资源点、奖励点、地标建筑
- 可保存和加载地图配置

## 8. 怪物与交互布置设计
当前工程可复用的战斗资产为：
- 玩家：现有 `TopDownPlayerController` + `PlayerCombat`
- 敌人：`Torch`、`TNT`
- 交互：`RewardShrine`

因此第一版地图系统采用以下设计：
- `Torch`：近战追击，适合 Narrow Pass、Combat Room、Bridge Room、Forest Encounters
- `TNT`：远程压迫，适合 Elevation Arena、Resource Court、Village / Citadel 遭遇
- `Summon Spawn`：触发式生成 1~3 个敌人，用于节奏变化
- `Reward Shrine`：奖励房、奖励区域或事件点固定交互物

布置规则：
- `RoomChain` 常规房默认 `2~4` 个怪物点
- `RoomChain` 主战斗房默认 `3~5` 个怪物点
- `OpenWorld` 每个核心区域至少 1 个预置遭遇区
- `OpenWorld` 至少 2 个区域拥有触发式召唤点
- 奖励点周边不放常驻怪

## 9. 双模式 PCG 设计
V1 PCG 目标不再是单一路径关卡，而是双模式生成。

### 9.1 输入
共享输入：
- `Seed`
- `ThemePalette`
- `EncounterBudget`
- `DecorationDensity`
- `AnimationDensity`
- `MapMode`

`RoomChain` 额外输入：
- `RoomCount`
- `BranchDepth`
- `AllowedRoomTypes`

`OpenWorld` 额外输入：
- `RegionScale`
- `WaterCoverage`
- `ElevationCoverage`
- `LandmarkDensity`
- `EncounterZoneCount`

### 9.2 输出
- 一张可玩的 `RoomChain` 或 `OpenWorld` 布局
- 一份可保存、可重建、可复现的地图配置

### 9.3 `RoomChainGenerator`
1. 生成房间图
2. 分配房型
3. 选择房间模板
4. 填充静态 Tile 层
5. 填充动画通道
6. 放置装饰、交互点、怪物点
7. 做可达性、净空、动画预算检查
8. 输出配置

### 9.4 `OpenWorldGenerator`
1. 生成区域图和主线路
2. 生成连续地形块与水域边界
3. 铺设高地、森林、废墟、资源区
4. 放置建筑和资源语义点
5. 放置预置敌人区、巡逻区、召唤区
6. 布局动态环境动画
7. 执行可达性和动画预算检查
8. 输出可保存配置

### 9.5 生成原则
- `RoomChain` 更强调房间节奏与波次战斗
- `OpenWorld` 更强调连续探索、区域身份与开放遭遇
- 两种模式都必须最大化利用原包内已有动态资源
- 不为静态资源发明不存在的循环动画

## 10. 配置文件设计
本阶段只定义职责，不定义代码细节。

### 10.1 `MapMode`
记录：
- `RoomChain`
- `OpenWorld`

### 10.2 `ResourceCatalogAsset` / `resource-catalog.json`
记录：
- 资源家族
- 资源路径
- 语义标签
- 动画标签
- 默认图层归属
- 是否进入白名单

### 10.3 `AnimatedVariantDefinition`
记录：
- 动画来源资源
- 动画分类
- 默认激活策略
- 所属动态图层通道

### 10.4 `AmbientAnimationProfileAsset`
记录：
- 常驻动画预算
- 区域动画预算
- 触发动画预算
- 摄像机邻近激活参数

### 10.5 `TileLayerRuleAsset`
记录：
- 图层名
- 排序
- 可放资源类型
- 动画通道映射
- 导航语义
- 覆盖 / 依赖关系

### 10.6 `RoomTemplateAsset`
记录：
- 房型
- 房间尺寸范围
- 地形结构类型
- 允许装饰密度
- 允许刷怪点数量
- 允许交互点数量
- 只服务 `RoomChain`

### 10.7 `RegionTemplateAsset`
记录：
- 区域类型
- 区域规模范围
- 资源与建筑主题
- 动画密度
- 遭遇区数量
- 只服务 `OpenWorld`

### 10.8 `OverworldLayoutAsset`
记录：
- 区域图
- 区域连接关系
- 主线路与支线路
- 每个区域的模板选择

### 10.9 `PCGProfileAsset`
记录：
- 通用参数
- `RoomChainProfile`
- `OpenWorldProfile`

### 10.10 `GeneratedLevelSaveData`
记录：
- seed
- 模式
- 最终 Tile 数据
- 怪物点和交互点
- 可用于加载重建

### 10.11 `OpenWorldSaveData`
记录：
- 区域图
- 动画布局
- 区域遭遇定义
- 世界事件标记

## 11. 编辑器工具设计
V1 工具入口固定为 Unity Editor 窗口，不设计运行时按钮。

按钮固定为：
- `Scan Tiny Swords Catalog`
- `Build Showcase Level`
- `Generate PCG Level`
- `Save Current Level Config`
- `Load Level Config`

工具交互修订：
- `Build Showcase Level` 必须支持模式切换：
  - `Build RoomChain Showcase`
  - `Build OpenWorld Showcase`
- `Generate PCG Level` 必须支持：
  - `Generate RoomChain`
  - `Generate OpenWorld`

## 12. 与当前运行时的对接边界
复用现有系统：
- `TopDownPlayerController`
- `PlayerCombat`
- `EnemyBrainTorchGoblin`
- `EnemyBrainTntGoblin`
- `WaveDirector`
- `ArenaGameState`
- `RewardShrine`

新增设计边界：
- `MapDefinition`
- `MapMode`
- `EncounterDefinition`
- `RegionEncounterDefinition`
- `MapBuildRequest`
- `MapSaveData`
- `AnimationActivationContext`
- `WorldEventMarker`

边界分工：
- 地图系统负责构建场地、区域、点位和动画激活上下文
- 战斗系统负责角色行为和结算

## 13. 审阅标准
本 SDD 审阅通过应满足：
- 资源图谱已区分静态、常驻动画、条件动画、战斗反馈动画
- Tilemap 分层规则和动态图层规则都可落地
- `RoomChain` 样板关卡结构清楚
- `OpenWorld` 样板世界结构清楚
- 双模式 PCG 输入输出清楚
- 配置资产职责清楚
- 与当前战斗切片的边界清楚

## 14. 下阶段 Tech Spec 输入项
下一阶段技术文档必须细化：
- ScriptableObject 与 JSON 的字段结构
- Tilemap 构建流水线
- 动态通道与激活系统
- `RoomChain` / `OpenWorld` 两套生成器接口
- 地图保存/加载的数据格式
- 运行时桥接组件
- 自动化测试策略

## 15. 最终验收目标
最终实现完成后必须满足以下结果：
- 能扫描原始 Tiny Swords 目录并生成资源配置
- 能扫描和登记动画资源及其激活策略
- 能一键构建 `RoomChain` 样板关卡
- 能一键构建 `OpenWorld` 样板世界
- 能一键随机生成双模式关卡
- 能保存并重新加载双模式地图配置
- 主角能在生成地图中移动、战斗、遭遇和触发召唤怪
- 动画对象与导航、遭遇、交互规则不冲突
