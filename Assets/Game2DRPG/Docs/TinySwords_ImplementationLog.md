# Tiny Swords 地图系统实现日志

## 项目目标
- 在当前 Unity 工程中落地 Tiny Swords 双模式地图系统。
- 按固定顺序推进：共享底座 -> RoomChain -> OpenWorld。
- 保留现有单房间 Arena 作为回归场景，新增地图系统专用构建器、场景、配置资产和测试。

## 当前阶段
- Phase 2：RoomChain / OpenWorld Showcase 与 PCG 基线打通

## 已完成
- 共享底座已经落地：
  - `Assets/Game2DRPG/Data/{Catalog,Rules,Templates,Layouts,Profiles,Saves}` 目录已建立。
  - 地图运行时数据模型、动画激活、区域遭遇、运行时绑定器已接入工程。
  - `Map Builder` 编辑器窗口已接入 `Tools/Game2DRPG/Map Builder`。
- 资源扫描闭环已打通：
  - 可扫描 `Assets/Tiny Swords` 并生成 `ResourceCatalog.asset`。
  - 可导出 `resource-catalog.json`。
  - 已登记 `Torch/TNT` 项目扩展战斗资产。
- RoomChain / OpenWorld 样板场景已可生成：
  - `Assets/Game2DRPG/Scenes/TinySwords_RoomChain_Showcase.unity`
  - `Assets/Game2DRPG/Scenes/TinySwords_OpenWorld_Showcase.unity`
- RoomChain / OpenWorld 随机生成链路已可执行：
  - 已生成随机 `roomchain-save.json`
  - 已生成随机 `openworld-save.json`
- Save / Load 基线已验证：
  - 当前场景地图配置可导出为 JSON。
  - 可从 JSON 重新构建 OpenWorld 场景。
- 地图测试基线已补齐：
  - EditMode 已覆盖基础资产、扫描、随机生成、JSON 回读、场景重建。
  - PlayMode 已覆盖动画激活服务配置和相机邻近激活逻辑。
- HUD 顶部布局的编辑态结构已稳定：
  - `Canvas/HUD/TopPanel/PromptText` 锚点和位置已固定在顶部。
  - `MapSceneAssembler` 生成的新场景不再把 HUD 放在屏幕中间。

## 进行中
- OpenWorld 运行态的表现层收口：
  - 地图、主角、奖励点已经能在运行时实例化。
  - HUD 在编辑态结构正常，但当前在 Metal Game View 抓图中仍出现文字倒置/底部显示异常，正在作为表现层问题继续排查。
- 双模式玩法整合深化：
  - RoomChain 目前已经接入房间遭遇和奖励点。
  - OpenWorld 已接入区域遭遇控制器，但还需要进一步做开放世界战斗节奏和区域引导的可玩性收口。

## 下一步
- 继续排查并修正 OpenWorld 运行态 HUD 的显示异常。
- 补 `RoomChainPlayModeTests`、`OpenWorldPlayModeTests`、`SaveLoadPlayModeTests` 的真实场景级验证。
- 补 `MapBuilderWindow` 的按钮级 EditMode 覆盖，确保窗口入口和控制器调用一致。
- 开始把当前基线从 Showcase 推进到更完整的战斗可玩切片。

## 风险/阻塞
- Unity 6 + Metal 下的 UGUI 抓图结果与编辑态 RectTransform 数据不完全一致，说明还存在运行时表现层问题，不能把当前 HUD 视为最终稳定版本。
- 当前地图系统代码先并入现有 `Game2DRPG.Editor` / `Game2DRPG.Runtime` 程序集运行，后续如果要彻底独立出地图程序集，需要重新整理 asmdef 结构。
- OpenWorld 目前已是单场景连续世界，但还没有进入性能优化阶段；后续动画预算与大地图可视密度需要专门验证。

## 最近验证结果
- 资源扫描、样板构建、随机生成、保存与加载链路已在 2026-03-07 重新执行通过。
- 生成的保存文件：
  - `Assets/Game2DRPG/Data/Saves/resource-catalog.json`
  - `Assets/Game2DRPG/Data/Saves/roomchain-save.json`
  - `Assets/Game2DRPG/Data/Saves/openworld-save.json`
- EditMode：`Game2DRPG.Map.EditMode.Tests` 6/6 通过
- PlayMode：`Game2DRPG.Map.PlayMode.Tests` 2/2 通过
- 运行态验收：
  - OpenWorld 样板场景进入 PlayMode 成功
  - 主角与奖励点成功实例化
  - 当前仍观测到 HUD 在 Game View 抓图中的倒置/位置异常，列为下一步处理项

## 里程碑
- [x] 底座编译通过
- [x] Catalog 扫描导出闭环
- [x] RoomChain Showcase 可生成
- [x] RoomChain PCG 可生成
- [x] OpenWorld Showcase 可生成
- [x] OpenWorld PCG 可生成
- [x] Save/Load 闭环通过
- [ ] OpenWorld 运行态 HUD 表现收口
- [ ] 场景级 PlayMode 验收补全
