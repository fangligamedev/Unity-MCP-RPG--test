# Tiny Swords 当前模式总结

## 1. 当前激活模式
当前工程的主展示模式已经收口为 `RoomChain Fixed Reference Showcase`。

这不是随机 PCG 图，也不是旧的小岛测试图，而是一张固定手工布局的参考展示地图。目标是直接对齐 Tiny Swords 官方宣传图和 tilemap guide 的视觉语法，作为后续继续收口和回迁 PCG 规则的基准版本。

## 2. 当前模式的定位
当前模式用于解决两个核心问题：
- 先把地图生成结果从错误的“整图矩形平铺地砖”拉回 Tiny Swords 正确语法。
- 先固定一张可审阅、可运行、可测试的参考图，再把同样的规则迁回随机生成。

因此，这一版的重点不是地图数量，而是规则正确性和视觉结构正确性。

## 3. 当前地图结构
当前 fixed showcase 已经采用单张固定大岛结构，主要由以下区域组成：
- 左上城堡高地
- 中部横向高地桥面
- 右上林地区高地
- 右中村落区
- 左侧塔楼区
- 下方中心塔楼小岛
- 右下出口高地区
- 中央水域与外沿 shoreline

整体结构已经不再是抽象测试岛，而是明确向宣传图那种“多个高低地块 + 中央水域 + 建筑群 + 守卫摆位”的构图靠近。

## 4. 当前地图规则
### 4.1 图层顺序
当前模式严格采用 Tiny Swords guide 的核心层级：
1. `BGColor`
2. `WaterFoam`
3. `FlatGround`
4. `Shadow_L1`
5. `ElevatedGround_L1`
6. `Shadow_L2`
7. `ElevatedGround_L2`

当前 fixed showcase 主要使用到 `L1`，但系统保留 `L2` 扩展能力。

### 4.2 水与泡沫
- `BGColor` 负责整个水域底色。
- `WaterFoam` 只贴 shoreline 边界，不再把整片水错误当成泡沫层。
- `WaterFoam` 已从错误复用 `Water Tile animated.asset` 改为独立 tile 资产，来源是 `Water Foam.png`。

### 4.3 高地与阴影
- 高地通过 `ElevatedGround_L1` 表达。
- 阴影通过 `Shadow_L1` 独立图层表达。
- 阴影放置规则为：对每个高地顶面格，在其下方一格生成 shadow tile。
- 这符合官方“128x128 shadow 放在 64x64 网格，并整体下移一格”的视觉语法。

### 4.4 楼梯与悬崖
- 高地只能通过楼梯跨层。
- 高地非楼梯边缘会自动生成 `EdgeBarrier_*` 薄碰撞。
- 玩家和敌人不能再从悬崖正面或平台侧边直接走上去。

## 5. 当前装饰与动画策略
### 5.1 建筑
当前 fixed showcase 已接入：
- Castle
- Tower
- House1
- House2
- House3
- Barracks

### 5.2 单位摆件
当前已把蓝方单位作为场景装饰摆件加入：
- Warrior Guard
- Archer Idle
- Lancer Defence

### 5.3 环境资源
当前已接入：
- 树
- 灌木
- 水中岩石
- 羊

### 5.4 动画路径
当前环境动画优先复用 Tiny Swords 原包的 AnimatorController：
- Tree 1..4
- Bush 1..4
- Water Rocks 1..4

Scene View 中这些对象现在可见；运行态仍按 `ByCameraProximity` 激活。

## 6. 当前碰撞模型
当前模式已经从“按整张 sprite 外接框碰撞”切换到“按语义和占格建碰撞”：
- `Water / Cliff / BlockedDecoration`：不可通行
- `FlatGround / ElevatedTop / Stairs`：可通行
- 树木、塔楼、建筑：按占格写入 `OccupancyCellData`
- 高地非楼梯边缘：额外生成 `EdgeBarrier_*`

这意味着当前地图的通行规则已经开始和视觉语义一致，而不是表面看起来像高地、实际却能从侧边直接穿上去。

## 7. 当前可玩的运行状态
当前 fixed showcase 仍然保持可玩：
- 有 `player_start`
- 有 `reward_shrine`
- 有 `exit_gate`
- 有基础 encounter 区
- 玩家可以在地图上移动
- 战斗系统仍沿用现有切片系统复用

因此，这一版不是纯静态展示图，而是一张可进入、可运行、可继续调试的参考场景。

## 8. 当前测试状态
最近一次验证结果：
- `Game2DRPG.Map.EditMode.Tests`：7/7 通过
- `Game2DRPG.Map.PlayMode.Tests`：3/3 通过

覆盖点包括：
- 资源扫描和导出
- RoomChain 生成的 guide 图层
- 阴影层偏移
- cliff / water / walkable 语义一致性
- 树木与灌木 Animator 路径
- Scene Assembler 生成 Occupancy 和 EdgeBarrier
- 动画激活服务在 PlayMode 下正常工作

## 9. 当前已知差距
虽然 fixed showcase 已经明显优于旧版本，但仍有这些差距：
- 还没有逐像素达到宣传图的一比一复刻程度
- 右上 logo / 招牌区尚未实现
- 地形边缘和建筑密度还可以继续向宣传图收口
- OpenWorld 仍保留旧风格逻辑，尚未按这一版规则回收
- 运行态 HUD / 画面翻转仍有既存显示层问题，不属于当前地图模式本体

## 10. 当前结论
当前工程的地图主模式可以定义为：

`RoomChain Fixed Reference Showcase`

它的特点是：
- 固定布局
- 可运行
- 可测试
- 符合 Tiny Swords guide 的基本图层和碰撞语法
- 用于后续继续做视觉收口和规则回迁 PCG

这意味着项目已经从“错误生成地图阶段”进入“有正确参考底图、可继续精修阶段”。
