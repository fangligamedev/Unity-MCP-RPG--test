# Tiny Swords 资源地图图谱

## 1. 文档目标
本文件定义 `Assets/Tiny Swords` 的资源图谱，作为后续地图系统、样板关卡、开放世界关卡和 PCG 关卡的资源真相来源。

本版本相较上一版有两个关键升级：
- 资源不再只按“静态地图素材”理解，而是明确区分 `Static / Animated / Reactive Animated`
- 地图用途不再只服务 `RoomChain` 多房间模式，而是同时服务 `RoomChain` 与 `OpenWorld`

## 2. 扫描基线
- 工程路径：`/Users/brucef/Documents/UnityProject/test/4-up-2d-rpg`
- 扫描源：`Assets/Tiny Swords`
- 扫描日期：2026-03-06
- 设计参考：
  - [Tiny Swords 主页](https://pixelfrog-assets.itch.io/tiny-swords)
  - [Tilemap Guide](https://pixelfrog-assets.itch.io/tiny-swords/devlog/1138989/tilemap-guide)

### 2.1 顶层资源统计
| 分类 | PNG | Anim | Controller | Prefab | 说明 |
|---|---:|---:|---:|---:|---|
| `Terrain` | 36 | 9 | 9 | 1 | 地图核心资源，含 Tileset、Decorations、Tilemap Settings |
| `Buildings` | 40 | 0 | 0 | 0 | 5 套配色，每套 8 类建筑 |
| `Pawn and Resources` | 131 | 125 | 17 | 0 | 资源节点、Sheep、Pawn、工具、木材等 |
| `Units` | 118 | 116 | 21 | 0 | 6 个配色阵营、4 类单位、额外箭矢/治疗效果 |
| `Particle FX` | 8 | 8 | 8 | 0 | Dust、Explosion、Fire、Water Splash |
| `UI Elements` | 81 | 0 | 0 | 0 | Banner、Button、Bar、Icon 等 |

## 3. 资源家族总览
地图系统不按单张图片平铺使用资源，而按“资源家族 + 语义标签 + 动画类型”组织。

### 3.1 Tile Family
用途标签：
- 地表
- 水体
- 泡沫
- 阴影
- 高地
- 楼梯
- 边缘连接件

来源：
- `Assets/Tiny Swords/Terrain/Tileset/Tilemap_color1.png`
- `Assets/Tiny Swords/Terrain/Tileset/Tilemap_color2.png`
- `Assets/Tiny Swords/Terrain/Tileset/Tilemap_color3.png`
- `Assets/Tiny Swords/Terrain/Tileset/Tilemap_color4.png`
- `Assets/Tiny Swords/Terrain/Tileset/Tilemap_color5.png`
- `Assets/Tiny Swords/Terrain/Tileset/Water Background color.png`
- `Assets/Tiny Swords/Terrain/Tileset/Water Foam.png`
- `Assets/Tiny Swords/Terrain/Tileset/Shadow.png`
- `Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/*`

动画归类：
- `Static`：大多数地表、阴影、高地切片
- `Animated`：`Tilemap Settings/Water Tile animated.asset`

设计结论：
- 这是 V1 地图系统的核心资源家族。
- 后续世界构建必须以 Tilemap 为核心，不再沿用当前 `Game2DRPGBuilder` 的大块 Sprite 拼场景方式。
- `Tilemap Settings` 下已有 `Water Tile animated.asset`、`Shadow.asset` 和大批 `Sliced Tiles`，是后续 Rule Tile、Animated Tile、切片 Tile 的事实来源。

### 3.2 Decoration Family
用途标签：
- 阻挡物
- 环境装饰
- 路径视觉引导
- 水边点缀
- 开放世界区域氛围强化

来源：
- `Terrain/Decorations/Bushes`
- `Terrain/Decorations/Clouds`
- `Terrain/Decorations/Rocks`
- `Terrain/Decorations/Rocks in the Water`
- `Terrain/Decorations/Rubber Duck`

动画归类：
- `Static`：`Clouds`、`Rocks`
- `Animated`：`Bushes/* Animation`、`Rocks in the Water/* Animation`
- `Reactive Animated`：`Rubber Duck` 归入趣味动画道具，优先作为事件或彩蛋激活

设计结论：
- `Bushes`、`Rocks`、`Rocks in the Water` 进入 V1 白名单。
- `Bushes` 和 `Rocks in the Water` 在新 SDD 中必须作为动态图层资源对待，而不是普通装饰。
- `Clouds` 保留为远景静态资源。
- `Rubber Duck` 归为彩蛋/趣味资源，不进主流程，但保留为开放世界事件点候选。

### 3.3 Landmark / Building Family
用途标签：
- 地标
- 房间主题锚点
- 区域主题锚点
- 视觉叙事
- 阻挡体
- 奖励房 / 资源房 / 出口房识别物

来源：
- `Buildings/Black Buildings`
- `Buildings/Blue Buildings`
- `Buildings/Purple Buildings`
- `Buildings/Red Buildings`
- `Buildings/Yellow Buildings`

每套固定 8 个建筑：
- `Archery`
- `Barracks`
- `Castle`
- `House1`
- `House2`
- `House3`
- `Monastery`
- `Tower`

动画归类：
- `Static`

设计结论：
- 建筑不作为经营系统对象，只作为地图语义资源。
- `RoomChain` 推荐用途：
  - `Castle` / `Tower`：出口或 Boss Gate 地标
  - `Monastery`：奖励房、祭坛房
  - `Barracks`：战斗房或精英房背景地标
  - `Archery`：远程怪主题房装饰
  - `House1~3`：资源房、过渡房、起始房点缀
- `OpenWorld` 推荐用途：
  - `Village Cluster`：废墟村落、资源点聚落
  - `Citadel Core`：高原城塞或终点区域
  - `Field Landmark`：远距离导航锚点

### 3.4 Resource Node Family
用途标签：
- 资源点
- 可交互点
- 奖励代理
- 召唤代理
- 条件触发物
- 区域语义点

来源：
- `Pawn and Resources/Gold`
- `Pawn and Resources/Meat`
- `Pawn and Resources/Pawn`
- `Pawn and Resources/Tools`
- `Pawn and Resources/Wood`

关键子集：
- `Gold Resource`、`Gold Stones`
- `Meat Resource`、`Sheep`
- `Tool_01~04`
- `Trees`、`Wood Resource`
- 各色 `Pawn`

动画归类：
- `Static`：Gold、Tool、Wood Resource、大部分 Trees
- `Animated`：Sheep、各色 Pawn 原包自带动作资源
- `Reactive Animated`：资源采集、事件对话、世界事件触发时激活动画

设计结论：
- V1 不实现采集经济循环。
- 这些资源在地图系统中的语义为：
  - `Gold`：资源点、奖励点、可争夺区域地标
  - `Wood/Trees`：阻挡、路径分割、区域边界
  - `Sheep/Meat`：开放世界中立生物、资源房装饰、轻交互点
  - `Tools`：工坊房、剧情占位、世界交互提示
  - `Pawn`：NPC 占位、事件房、商人/工人语义占位

### 3.5 Combat Unit Family
用途标签：
- 玩家候选
- 友军 / NPC 候选
- 敌方皮肤扩展候选
- 技能特效

原始目录来源：
- `Units/Black Units`
- `Units/Blue Units`
- `Units/Purple Units`
- `Units/Red Units`
- `Units/Yellow Units`
- `Units/Extra`
- `Units/Enemy Pack - Promo`

每个阵营固定 4 类单位：
- `Archer`
- `Lancer`
- `Monk`
- `Warrior`

额外战斗资源：
- `Extra/Arrow`
- `Extra/Heal Effect`

动画归类：
- `Animated`：各单位 `Idle / Run / Attack / Shoot / Heal / Guard / Defence`
- `Reactive Animated`：受击、施法、远程发射、召唤事件

重要说明：
- 当前工程实际可玩的战斗敌人 `Torch` / `TNT` 并 **不在** `Assets/Tiny Swords` 原始目录中。
- 它们来自当前玩法切片使用的项目扩展资产，位于 `Assets/Game2DRPG/Art/TinySwords`。
- SDD 中必须明确区分：
  - 原始资源扫描真相：`Assets/Tiny Swords`
  - 当前项目扩展战斗资产：`Torch/TNT`

设计结论：
- V1 地图系统使用 `Units` 作为未来战斗扩展角色库。
- V1 实际可玩关卡继续复用现有 `Torch/TNT` 战斗切片。
- 技术实现阶段应将 `Torch/TNT` 注册为“项目扩展敌人包”，而不是写进原始 Tiny Swords 资源扫描结果。

### 3.6 FX Family
用途标签：
- 命中特效
- 环境特效
- 水边特效
- 爆炸与火焰
- 动态世界反馈

来源：
- `Particle FX/Dust_01.png`
- `Particle FX/Dust_02.png`
- `Particle FX/Explosion_01.png`
- `Particle FX/Explosion_02.png`
- `Particle FX/Fire_01.png`
- `Particle FX/Fire_02.png`
- `Particle FX/Fire_03.png`
- `Particle FX/Water Splash.png`

动画归类：
- `Animated`：Fire、Water Splash
- `Reactive Animated`：Explosion、Dust

设计结论：
- `Explosion`、`Fire`、`Water Splash` 进入 V1 白名单。
- `Dust` 作为移动反馈、生成反馈、区域动态效果候选。

### 3.7 UI Family
用途标签：
- 地图编辑器按钮
- 关卡预览卡片
- 小地图占位
- 区域标签
- 调试界面

来源：
- `UI Elements/Banners`
- `UI Elements/Bars`
- `UI Elements/Buttons`
- `UI Elements/Cursors`
- `UI Elements/Human Avatars`
- `UI Elements/Icons`
- `UI Elements/Papers`
- `UI Elements/Ribbons`
- `UI Elements/Swords`
- `UI Elements/Wood Table`

动画归类：
- `Static`

设计结论：
- 这些资源不参与 Tilemap 地图绘制。
- 它们用于 V1 的编辑器按钮、关卡说明、调试面板与 HUD 扩展。

## 4. 动画资源图谱
本章节是本次修订新增内容，用于支撑“资源最大化利用的动态地图世界”。

### 4.1 动画资源分类
| 分类 | 含义 | 代表资源 | 激活策略 |
|---|---|---|---|
| `Static` | 无循环动画，纯静态展示 | Buildings、Clouds、Rocks | 常驻 |
| `Animated` | 原生自带循环动画，可作为环境动态 | Water Tile、Bushes、Water Rocks、Fire | 常驻或区域激活 |
| `Reactive Animated` | 按交互、战斗、事件触发 | Explosion、Dust、Rubber Duck、部分单位行为 | 事件激活 |

### 4.2 可直接复用的动态资源清单
#### 常驻环境动画
- `Terrain/Tileset/Tilemap Settings/Water Tile animated.asset`
- `Terrain/Decorations/Bushes/Bush 1~4 Animation/*`
- `Terrain/Decorations/Rocks in the Water/Rock 1~4 Animation/*`

#### 区域环境动画
- `Particle FX/Fire 1~3 Animation/*`
- `Rubber Duck` 动画
- `Pawn and Resources/Meat/Sheep/*`
- `Pawn and Resources/Pawn/*` 的 Idle / Run / Interact 序列

#### 交互 / 战斗反馈动画
- `Particle FX/Explosion 1~2 Animation/*`
- `Particle FX/Dust 1~2 Animation/*`
- `Particle FX/Water Splash/*`
- `Units/*` 的技能、跑动、施法、远程发射动作

### 4.3 动态世界的资源使用原则
- 水体和岸线动画是地图世界的基础动效，必须优先接入。
- 植被与浮水岩动画用于提升区域呼吸感，默认不全图同时激活。
- 战斗与交互反馈动画只在事件发生时激活，避免资源浪费。
- 没有原生动画的资源不强行伪造循环动画，避免偏离资源包原貌。

## 5. V1 地图系统资源白名单
以下资源进入 V1 地图构建白名单。

### 5.1 必选
- `Terrain/Tileset/*`
- `Terrain/Tileset/Tilemap Settings/*`
- `Terrain/Decorations/Bushes/*`
- `Terrain/Decorations/Rocks/*`
- `Terrain/Decorations/Rocks in the Water/*`
- `Buildings/*`
- `Pawn and Resources/Gold/*`
- `Pawn and Resources/Wood/*`
- `Pawn and Resources/Meat/Sheep/*`
- `Particle FX/Explosion*`
- `Particle FX/Water Splash*`
- `Particle FX/Fire*`
- `UI Elements/Banners/*`
- `UI Elements/Buttons/*`

### 5.2 条件启用
- `Pawn and Resources/Pawn/*`
- `Pawn and Resources/Tools/*`
- `Units/*`
- `Particle FX/Dust*`
- `Terrain/Decorations/Rubber Duck/*`

### 5.3 暂不进入 V1 主流程
- `UI Elements/Human Avatars/*`
- `UI Elements/Cursors/*`
- `UI Elements/Swords/*`
- `Units/Enemy Pack - Promo/*`

## 6. 资源语义映射表
| 资源家族 | 地图职责 | 动画类型 | 是否进入 PCG | 备注 |
|---|---|---|---|---|
| Tile Family | 地形主结构 | Static / Animated | 是 | 核心 |
| Decoration Family | 边界、装饰、阻挡 | Static / Animated / Reactive Animated | 是 | 支撑动态世界 |
| Building Family | 地标、房间主题、区域主题 | Static | 是 | 不做经营 |
| Resource Node Family | 交互点、资源点、NPC 点 | Static / Animated / Reactive Animated | 是 | 不做经济循环 |
| Combat Unit Family | 战斗参与者 | Animated / Reactive Animated | 是 | 原包与扩展敌人分离 |
| FX Family | 环境与战斗反馈 | Animated / Reactive Animated | 是 | 动态世界反馈层 |
| UI Family | 编辑器与 HUD | Static | 否 | 不进入世界 Tilemap |

## 7. 模式适配建议
### 7.1 `RoomChain` 模式
推荐覆盖资源组合：
- 平地 + 水边 + 泡沫 + 高地 + 阴影
- 树木 + 岩石 + 浮水岩
- 至少 3 类建筑地标
- 至少 2 类资源节点
- 至少 1 类常驻环境动画
- 至少 1 类战斗反馈动画

### 7.2 `OpenWorld` 模式
推荐覆盖资源组合：
- 连续水域与湿地区
- 大面积植被动画带
- 至少 4 个区域地标建筑簇
- 资源点链路和开放区域遭遇点
- 远距离导航用的静态大建筑
- 多层级环境动画：水体、植被、火焰、事件反馈

## 8. 与当前工程的衔接结论
- 当前工程已有战斗切片：玩家、Torch、TNT、爆炸、奖励、HUD、波次。
- 后续地图系统应复用这套运行时系统，而不是重新设计战斗逻辑。
- 原始 Tiny Swords 目录提供地图、地标、资源语义、环境动画和未来可扩展角色素材。
- 当前项目扩展敌人 `Torch/TNT` 需要在未来资源配置中以“外部扩展战斗包”形式登记。

## 9. 后续技术文档输入项
下一阶段 `Spec Tech` 必须细化以下对象：
- `ResourceCatalogAsset`
- `resource-catalog.json`
- `TileFamilyDefinition`
- `DecorationFamilyDefinition`
- `AnimatedVariantDefinition`
- `LandmarkDefinition`
- `ResourceNodeDefinition`
- `CombatAssetRegistry`
- `AmbientAnimationProfileAsset`

这些对象的职责是把“目录里有什么”转成“系统能怎么用，何时激活，在哪种地图模式中使用”。
