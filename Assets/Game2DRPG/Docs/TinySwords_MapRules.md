# Tiny Swords Map Rules

## 1. 当前执行范围
- 当前只对 `RoomChain showcase` 做 fixed reference pass。
- `OpenWorld` 暂不并入本轮视觉收口。
- 规则目标不是继续容忍抽象小岛图，而是让 showcase 直接对齐官方宣传图和 tilemap guide 的视觉语法。

## 2. 基础单位
- Tile Size: `64x64`
- 技术坐标: `1 cell = 1 Unity unit`
- 所有 tile、碰撞、占格、保存和重建都以 cell 为真相源。

## 3. 基础图层顺序
严格遵循 Tiny Swords 官方顺序：
1. `BGColor`
2. `WaterFoam`
3. `FlatGround`
4. `Shadow_L1`
5. `ElevatedGround_L1`
6. `Shadow_L2`
7. `ElevatedGround_L2`

说明：
- 当前 showcase 实际只用到 `L1`，但系统保留 `L2` 扩展位。
- `BGColor` 是整张图的水底背景色，不再额外拿 `Water Tile animated.asset` 去铺满整片水面。

## 4. 地形语义
- `Water`
- `FlatGround`
- `ElevatedTopL1`
- `ElevatedTopL2`
- `CliffToGroundL1`
- `CliffToWaterL1`
- `CliffToGroundL2`
- `CliffToWaterL2`
- `StairsL1`
- `StairsL2`
- `ShadowL1`
- `ShadowL2`
- `BlockedDecoration`

## 5. Tile 规则
### 5.1 Flat Ground
- 使用 Tiny Swords 官方 `Tilemap_color1_*` flat top 16 宫格。
- 所有平地和岛边都先生成语义，再映射 tile，不允许再用单块中心 tile 平铺整图。

### 5.2 Elevated Ground
- 使用同一套 `Tilemap_color1_*` 中的 elevated top 16 宫格。
- 当前 showcase 的高地主要服务以下视觉结构：
  - 左上城堡高地
  - 中部桥面高地
  - 右上林地区高地
  - 右下出口高地

### 5.3 Cliff
- 继续使用官方 cliff rows：
  - upper: `34,35,36,37`
  - lower: `40,41,42,43`
- 当前 fixed showcase 的 cliff 重点是 south-facing cliff，因为这是当前语义生成链里最稳定、也最接近参考图俯视表现的方向。

### 5.4 Stairs
- stairs 使用 `32,33,38,39`
- 高地只允许通过楼梯跨层，不允许侧边直接上台。
- 所有高地 south face 如果不是楼梯口，必须被 `EdgeBarrier` 封住。

## 6. Water Foam 规则
### 6.1 正确语义
`WaterFoam` 是 shoreline 边界层，不是底层水面，也不是整片水域动画填充层。

### 6.2 当前实现规则
- `BGColor` 负责整个水域底色。
- `WaterFoam` 只出现在“接触 BGColor 的边缘水格”。
- 参与 shoreline 的陆地语义包括：
  - `FlatGround`
  - `ElevatedTop`
  - `Stairs`
  - `Cliff`
- `BlockedDecoration` 不参与 shoreline 计算，避免树和建筑把泡沫边界算乱。

### 6.3 当前资产路径
- 当前不再复用错误的 `Water Tile animated.asset`。
- 已改成独立的 `Water Foam Tile.asset`，其源 sprite 来自：
  - `Assets/Tiny Swords/Terrain/Tileset/Water Foam.png`

## 7. Shadow 规则
### 7.1 官方语义
- Shadow 是独立图层。
- 它跟随高地顶面 footprint，下移一个 `64x64` cell，用来制造高度错觉。

### 7.2 当前实现规则
- 对每个 `ElevatedTopL1` cell：
  - 在 `Shadow_L1` 图层写入 `cell + (0, -1)`
- 注意：
  - 阴影是独立视觉层，不强行占据地形语义主槽位
  - 同一位置下方如果本身还是 walkable flat，也允许阴影图层和 walkable 语义并存

## 8. 碰撞与阻挡规则
### 8.1 可通行
- `FlatGround`
- `ElevatedTopL1/L2`
- `StairsL1/L2`

### 8.2 不可通行
- `Water`
- `CliffToGround*`
- `CliffToWater*`
- `BlockedDecoration`

### 8.3 高地边缘阻挡
- 对每个 `ElevatedTop` cell：
  - 若相邻 cell 不是同层高地，也不是同层 stairs
  - 则生成 `EdgeBarrier_*` 薄碰撞
- 这样玩家和敌人只能通过 stairs 上下高地。

### 8.4 Decoration 占格
- 树木、建筑、塔楼不依赖 sprite.bounds 作为主碰撞。
- 统一按 `OccupancyCellData` 写占格，再由 Scene Assembler 生成 collider。

## 9. 植被与环境动画规则
### 9.1 优先路径
树、灌木、水中石头优先使用原包 AnimatorController：
- Tree 1..4
- Bush 1..4
- Water Rocks 1..4

### 9.2 激活策略
- 当前策略仍保留：`ByCameraProximity`
- 但在编辑态构建场景时，不再先把这些对象强行关掉，所以 Scene View 可见。

### 9.3 current showcase 目标
- 右上林地区、右下出口区、左侧高地下缘必须能看到树群
- 灌木和水中石头用于打散大块平地和水面边缘

## 10. Fixed Reference Showcase 规则
当前 showcase 不再是“抽象测试岛”，而是固定参考图构图，最少必须具备：
- 左上城堡高地
- 中部高地桥面
- 右侧村落区
- 左侧塔楼区
- 下方中心塔楼小岛
- 右下出口高地区
- 中央水域和 shoreline
- 守卫、羊、树木、灌木、水中石头等装饰层

## 11. 当前验收标准
- 地图不能再是“整图单一地砖 + 几个装饰”的错误结果
- 必须一眼看出：水域、泡沫、高地、悬崖、楼梯、建筑聚落、树群
- 玩家不能从高地侧边直接走上去
- 树木、灌木必须优先走 Animator 路线
- fixed showcase 必须比旧的小岛测试例更接近宣传图的大岛构图
