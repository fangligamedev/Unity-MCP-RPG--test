# Tiny Swords 地图规则文档

## 1. 规则来源
本规则基于官方 Tilemap Guide：
- [Tilemap Guide](https://pixelfrog-assets.itch.io/tiny-swords/devlog/1138989/tilemap-guide)

官方规则中的硬约束：
- 工程 tile size 为 `64x64`
- 静态地形图层顺序固定为 `BG Color -> Water Foam -> Flat Ground`
- 之后按需要重复 `Shadow -> Elevated Ground`
- `Shadow` 精灵是 `128x128`，但放置逻辑仍按 `64x64` 网格执行，并向下偏移一格制造高度错觉
- `Water Foam` 沿水边接触面铺设，建议错开动画起始帧

本次修订新增两个核心约束：
- 地图规则必须显式覆盖动画信息，而不是把动画当成运行时附赠效果
- 地图模式必须同时覆盖 `RoomChain` 与 `OpenWorld`

## 2. 世界坐标默认值
- `1 tile = 64x64 px`
- 技术实现默认导入为 `64 PPU`
- 运行时默认 `1 cell = 1 Unity unit`
- 所有地图合法性规则都以 cell 为基础，不以像素为基础

## 3. 图层模型
地图规则拆成两部分：
- `静态渲染层`
- `动态图层 / 动画通道`

### 3.1 静态渲染层
| 层名 | 顺序 | 数据类型 | 可通行性 | 说明 |
|---|---:|---|---|---|
| `BGColor` | 0 | Tilemap | 不可通行 | 地图外背景、水体底色 |
| `WaterFoam` | 1 | Tilemap | 不可通行 | 水边泡沫底层，和动态图层配合 |
| `FlatGround` | 2 | Tilemap | 可通行 | 最低可行走地面 |
| `Shadow_L1` | 3 | Tilemap | 可通行 | 第一层高地阴影 |
| `ElevatedGround_L1` | 4 | Tilemap | 部分可通行 | 第一层高地上表面与悬崖体 |
| `Shadow_L2` | 5 | Tilemap | 可通行 | 第二层高地阴影 |
| `ElevatedGround_L2` | 6 | Tilemap | 部分可通行 | 第二层高地上表面与悬崖体 |
| `Decorations` | 7 | Tilemap / Placed Objects | 视对象而定 | 树、岩石、灌木、云等 |
| `Interactives` | 8 | Placed Objects | 视对象而定 | 奖励点、资源点、祭坛、出口等 |
| `SpawnMarkers` | 9 | Markers | 可通行 | 玩家出生点、怪物预置点、召唤点 |
| `NavigationMask` | 10 | Data Layer | 不渲染 | 路径、阻挡、危险区、事件区语义层 |

### 3.2 动态图层 / 动画通道
| 通道名 | 默认来源 | 激活方式 | 说明 |
|---|---|---|---|
| `AnimatedWater` | `Water Tile animated.asset` | 常驻 | 大面积水体循环动画 |
| `AnimatedShoreline` | Water Foam 相关表现 | 常驻 | 水边接触动画或岸线波动感 |
| `AnimatedVegetation` | `Bushes/* Animation` | 房间/区域激活 | 植被呼吸感与环境生命感 |
| `AmbientProps` | `Rocks in the Water`、`Fire`、`Rubber Duck` | 区域激活 | 场景氛围动态物 |
| `ReactiveFX` | `Dust`、`Explosion`、`Water Splash` | 事件触发 | 交互、战斗、召唤、入场反馈 |

## 4. 图层放置与动画规则

### 4.1 BGColor
- 必须覆盖整张地图所有未被可行走地表填充区域
- 默认承担水域底色职责
- 不允许玩家、敌人、奖励点直接生成在 BGColor 上

### 4.2 WaterFoam + AnimatedShoreline
- `WaterFoam` 只允许放在 FlatGround / Elevated cliff 与 BGColor 相邻的位置
- 逻辑上属于水边装饰，不产生碰撞
- 按 `64x64` 网格对齐
- 若同一段岸线可动画，则优先挂接到 `AnimatedShoreline`
- 岸线动画必须支持随机起始帧偏移，避免整圈同步闪动

### 4.3 FlatGround
- 是所有低地房间、路径和开放区域的基础可走层
- 所有出生点、怪物点、资源点、奖励点默认优先落在 FlatGround 上
- 平地边缘只允许直接接水或接 Shadow / Stair，不允许出现悬空断裂

### 4.4 Shadow_L1 / Shadow_L2
- 不参与碰撞
- 必须与对应的 ElevatedGround 层一一对应
- 视觉上向下偏移 1 格
- 禁止出现无来源的孤立阴影块

### 4.5 ElevatedGround_L1 / ElevatedGround_L2
- 上表面是可行走区域
- 悬崖体不是可行走区域
- 每个高地模块必须有合法楼梯或坡道连接到低地

### 4.6 Decorations + AnimatedVegetation + AmbientProps
- Decoration 分成两类：
  - `Soft Decoration`：不挡路，只参与视觉丰富
  - `Hard Decoration`：有阻挡语义，如树、石头、木堆
- `AnimatedVegetation` 默认只能挂在 Soft / 半阻挡装饰上
- `AmbientProps` 可挂在浮水岩、火焰、彩蛋装饰、区域地标周边
- 动态环境对象不能遮挡关键导航入口、楼梯口和窄道入口

### 4.7 Interactives
- 包括奖励、资源点、事件物、出口门、祭坛、召唤器
- 必须落在可达格上
- 与怪物出生点至少保持 2 格以上净空
- 与房间入口至少保持 3 格以上净空

### 4.8 SpawnMarkers
- 分为：
  - `PlayerStart`
  - `EnemySpawn`
  - `SummonSpawn`
  - `EliteSpawn`
  - `RewardSpawn`
  - `ExitSpawn`
  - `PatrolAnchor`
  - `EventTrigger`
- 标记层不渲染，但必须能与 Tilemap 数据绑定

### 4.9 NavigationMask
- 是规则系统的真相层，而不是渲染层
- 至少记录：
  - `Walkable`
  - `Blocked`
  - `Water`
  - `Cliff`
  - `Stair`
  - `Hazard`
  - `SpawnReserved`
  - `InteractionReserved`
  - `AnimationReserved`

## 5. 动画激活规则
本章节是本次修订新增内容。

### 5.1 常驻动画
以下动画默认整图常驻：
- `AnimatedWater`
- `AnimatedShoreline`

原因：
- 它们定义世界是否“活着”
- 视觉收益高
- 与地图结构强绑定

### 5.2 房间 / 区域激活动画
以下动画默认按房间、区域或摄像机邻近激活：
- `AnimatedVegetation`
- `AmbientProps`

适用资源：
- Bushes
- Water Rocks
- 区域火焰
- 彩蛋物
- Sheep / Pawn 这类区域动态元素

### 5.3 事件触发动画
以下动画按交互或战斗状态触发：
- `ReactiveFX`

适用资源：
- Dust
- Explosion
- Water Splash
- 召唤入场特效
- 战斗开始 / 结束反馈

### 5.4 动画预算规则
- 大地图模式中，同屏激活的 `AnimatedVegetation + AmbientProps` 默认必须受预算控制
- 动画对象不能误导玩家对可通行边界的判断
- 动画对象不能把入口、窄道、楼梯、奖励点完全遮住

## 6. 地图合法性约束

### 6.1 通用可达性
- 玩家起点必须能到达主战斗区、奖励点和最终出口
- 所有主路径必须是 4 邻接连续路径
- 不允许仅靠装饰缝隙穿越

### 6.2 水域封边
- 所有贴近 BGColor 的 walkable 边缘，必须匹配 WaterFoam 或 cliff 逻辑
- 不允许出现平地直接接背景色但无泡沫/无 cliff 的裸边
- 地图外围至少保留 1 圈 BGColor 缓冲带

### 6.3 动画合法性
- 动画对象不能遮挡关键导航信息
- 动画对象不能误导可通行边界
- 动画对象不能与交互点、刷怪点、奖励点重叠
- 大地图模式下必须限制同屏动画预算

### 6.4 装饰密度
- Hard Decoration 占可移动区域外缘面积比例建议在 `8% ~ 18%`
- Soft Decoration 占总面积比例建议在 `5% ~ 15%`
- 任何地块都不得因装饰导致主路径宽度低于 3 格

### 6.5 奖励与交互点距离
- 奖励点与最近怪物点距离 `>= 4` 格
- 出口与奖励点距离 `>= 5` 格
- 资源点与房间入口或区域入口距离 `>= 3` 格
- 召唤点与奖励点不得同格或相邻格

## 7. `RoomChain` 模式规则
这是原有多房间 roguelike 模式，保留并继续作为样板关卡基线。

### 7.1 房间最小尺寸
- `Start Room`：`14x12`
- `Combat Room`：`16x14`
- `Elite Room`：`18x16`
- `Reward Room`：`12x10`
- `Resource Room`：`14x12`
- `Connector Room / Pass`：最窄处宽度 `>= 3`

### 7.2 战斗净空
- 常规战斗房净空可移动区域不得小于 `10x8`
- 精英房净空可移动区域不得小于 `12x10`
- 近战敌人出生点与玩家出生点初始距离至少 `6` 格
- 远程敌人出生点与玩家出生点初始距离至少 `8` 格

### 7.3 房型池
- `Start`
- `Combat`
- `Resource`
- `Reward`
- `Connector`
- `Elite`
- `Exit`

### 7.4 样板关卡必须覆盖
- 平地接水
- 高地带阴影
- 水边窄道
- 至少 1 个跨水连接区
- 至少 1 个资源点房
- 至少 1 个高地主战斗房
- 至少 1 组常驻动画
- 至少 1 组事件反馈动画

## 8. `OpenWorld` 模式规则
这是本次新增的连续大地图模式，定义为单场景连续开放世界，不做流式分块。

### 8.1 基本结构
开放世界固定由 5 个连续区域组成：
- `Spawn Meadow`
- `Wetland Belt`
- `Resource Forest`
- `Ruined Village`
- `High Plateau Citadel`

### 8.2 区域规则
- 每个区域都必须有清晰地形身份和视觉锚点
- 区域之间必须通过连续路径、桥、坡道或门廊连接
- 至少 2 个区域包含开放战斗
- 至少 1 个区域包含资源点聚集
- 至少 1 个区域包含触发式召唤遭遇

### 8.3 大地图可达性
- 玩家从 `Spawn Meadow` 必须能到达其余 4 个区域
- 主线到 `High Plateau Citadel` 必须连续可达
- 所有地标点、奖励点、主要遭遇区都必须可达

### 8.4 大地图动画规则
- `Wetland Belt` 默认是水体和岸线动画最密集区域
- `Resource Forest` 默认是植被动画最密集区域
- `Ruined Village` 可使用火焰、Dust、环境事件反馈
- `High Plateau Citadel` 以静态大地标为主，只保留少量重点动态元素

## 9. PCG 规则
V1 PCG 不再是单一路径生成，而是拆成双模式生成。

### 9.1 `RoomChainGenerator`
固定流程：
1. 生成房间拓扑图
2. 选定主线与支线
3. 给每个房间分配房型
4. 根据房型选择房间模板
5. 填充静态 Tile 层
6. 填充动画通道
7. 放置装饰、交互点、怪物点
8. 执行合法性检查
9. 生成可保存配置和可复现 seed

### 9.2 `OpenWorldGenerator`
固定流程：
1. 生成区域图和主线路
2. 生成连续地形块与水域边界
3. 铺设高地、森林、废墟、资源区
4. 放置建筑和资源语义点
5. 放置预置敌人区、巡逻区、召唤区
6. 布局动态环境动画
7. 执行可达性和动画预算检查
8. 输出可保存配置

### 9.3 通用合法性检查
- 起点到出口或终点区域可达
- 奖励点可达
- 所有战斗区净空达标
- 所有交互点可达
- 不存在被装饰封死的主线路
- 动画对象不与导航和交互冲突

## 10. 当前工程复用规则
- 地图系统负责决定“在哪里战斗”
- 当前运行时系统负责“如何战斗”

复用对象：
- `TopDownPlayerController`
- `PlayerCombat`
- `EnemyBrainTorchGoblin`
- `EnemyBrainTntGoblin`
- `WaveDirector`
- `ArenaGameState`
- `RewardShrine`

设计约束：
- 第一版只保证新地图系统能承载现有战斗切片
- 不在本阶段重做战斗摄像机、伤害公式、UI 系统

## 11. 技术实现前置要求
Tech Spec 阶段必须把以下规则落成结构化数据：
- 静态图层顺序与排序值
- 动态通道与激活策略
- Tile 语义与导航语义
- 房型模板参数
- 区域模板参数
- 房间 / 区域合法性检查参数
- 保存/加载数据结构
- Editor 工具按钮与模式切换约束
