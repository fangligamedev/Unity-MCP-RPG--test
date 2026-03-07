# Tiny Swords 实现日志

## 项目目标
- 在当前 Unity 工程中落地 Tiny Swords 地图系统。
- 当前优先目标不是继续扩模式，而是把 `RoomChain showcase` 收成一张可直接对照官方宣传图和 tilemap guide 的固定参考图。
- 本轮交付重点：`BGColor / WaterFoam / FlatGround / Shadow / ElevatedGround / Stairs / 悬崖阻挡 / Animator 植被`。

## 当前阶段
- Phase 2B：RoomChain 固定参考图 showcase 收口

## 已完成
- `RoomChainGenerator.GenerateShowcase()` 已从旧的小岛测试例切换为固定手工参考图布局。
- 当前 fixed showcase 已具备以下构图元素：
  - 左上城堡高地
  - 中部高地桥面
  - 右侧村落区
  - 左侧塔楼区
  - 下方中心塔楼小岛
  - 右下出口高地区
  - 中央水域和外环 shoreline
- `WaterFoam` 已从错误的 `Water Tile animated.asset` 改成独立的 `Water Foam` tile 资产，不再把整片水误画成泡沫层。
- `Shadow` 继续按高地顶面 footprint 下移一格写入 `Shadow_L1` 图层；测试已改为直接验证阴影视觉层，而不是强行把阴影塞进单一地形语义。
- `MapSceneAssembler` 已调整：非 AlwaysOn 动画对象在编辑态不会被直接关掉，因此树木、灌木和水中石头在 Scene View 中可见。
- 树、灌木和水中岩石优先走原包 `AnimatorController`，不再用简化帧播放器冒充环境动画。
- 当前 fixed showcase 的主要美术摆件已接入：
  - Castle
  - Tower
  - House1 / House2 / House3 / Barracks
  - Blue guards (Warrior / Archer / Lancer)
  - Tree / Bush / Water Rocks
  - Sheep

## 进行中
- 继续把 fixed showcase 往宣传图构图靠近。
- 当前已经从“错误的矩形平铺地砖”回到 Tiny Swords 语法，但还没有做到逐像素级一比一复刻。
- 本轮保留 `OpenWorld` 现状，不并入这次视觉收口。

## 当前剩余差距
- 当前 fixed showcase 已接近宣传图的主要构图，但还存在这些差距：
  - 右上 logo / 招牌区尚未复刻
  - 房间轮廓仍偏规则矩形，后续还可继续做更细的半岛和缺口
  - 树木与建筑密度已经回升，但仍可继续向宣传图做更密的装饰层
  - 运行时 Game View 仍存在既存 HUD/翻转表现问题，这不是本轮地图生成规则主任务

## 下一步
- 继续收 fixed showcase：
  - 把地形轮廓做得更贴近宣传图
  - 继续补树群、碎石、小岛和水边装饰
  - 评估是否需要额外的 `WaterFoam` 变体 tile 提高 shoreline 自然度
- 在 fixed showcase 满意后，再把相同语法迁回 `RoomChain` 的随机模板。

## 风险 / 阻塞
- 目前 `RoomChain` 的 fixed showcase 已经进入正确方向，但若目标是“和宣传图几乎完全一致”，仍需要一轮纯视觉收口。
- `OpenWorld` 仍保留旧逻辑，会与新版 RoomChain 形成风格差。
- 当前工程里仍存在 `Assets/Game2DRPG/Scripts/Editor/` 下多 asmdef 的历史错误日志，但这轮脚本编译、生成和测试都已经实际跑通。

## 最近验证结果
- `MapBuilderController.BuildRoomChainShowcase()`：通过
- `TinySwords_RoomChain_Showcase.unity`：已重建
- `Game2DRPG.Map.EditMode.Tests`：7/7 通过
- `Game2DRPG.Map.PlayMode.Tests`：3/3 通过
- 人工核对结果：
  - `WaterFoam` 已回到 shoreline 边界层
  - `Shadow` 图层已存在并按高地 south offset 落地
  - 高地非楼梯边缘存在 `EdgeBarrier_*` 阻挡
  - 树木、灌木、水中石头在 Scene View 中可见
  - fixed showcase 已不再是旧的小岛测试图，而是固定参考图结构

## 里程碑
- [x] 底座编译通过
- [x] Catalog 扫描导出闭环
- [x] RoomChain Showcase 可生成
- [x] RoomChain PCG 可生成
- [x] OpenWorld Showcase 可生成
- [x] OpenWorld PCG 可生成
- [x] Save/Load 闭环通过
- [x] RoomChain 语义网格和 tile 规则表落地
- [x] RoomChain 占格碰撞落地
- [x] RoomChain Animator 优先路径落地
- [x] RoomChain fixed reference showcase 第一版落地
- [ ] RoomChain fixed showcase 继续视觉收口
- [ ] OpenWorld 延后重构
