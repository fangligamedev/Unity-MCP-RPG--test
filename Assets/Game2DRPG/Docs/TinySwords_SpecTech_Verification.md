# Tiny Swords Spec Tech: Verification

## 1. 文档目标
本文件定义双模式地图系统的验证策略、验收场景、性能预算、动画预算、MCP 执行链路和失败回退策略。

目标是把“怎么证明系统真的可用”写成明确清单，避免实现完成后再补验收标准。

## 2. 验证阶段
验证固定拆成 4 层：
- 静态资产验证
- 编辑器构建验证
- 运行时玩法验证
- 回归与性能验证

## 3. 测试矩阵
### 3.1 EditMode
重点验证资产、规则和生成器结构正确。

必须覆盖：
- `ResourceCatalogAsset` 能区分 `Static / Animated / Reactive Animated`
- `TileLayerRuleAsset` 包含静态层和动态图层
- `AmbientAnimationProfileAsset` 预算字段存在且合法
- `RoomTemplateAsset`、`RegionTemplateAsset` 最小模板可加载
- `MapBuilderWindow` 按钮存在，状态字段可序列化
- `RoomChainGenerator` 输出房间图合法
- `OpenWorldGenerator` 输出区域图合法
- SO -> JSON 导出可执行

### 3.2 PlayMode
重点验证场景可玩、动态对象激活合理、现有战斗可复用。

必须覆盖：
- `RoomChain` 样板场景可加载，玩家能移动
- `RoomChain` 中怪物点、召唤点、奖励点能正常工作
- `OpenWorld` 样板场景可加载，玩家能连续穿越 5 个区域
- `OpenWorld` 遭遇区能触发预置怪和召唤怪
- 动态水体、岸线、植被、AmbientProps、ReactiveFX 按规则激活
- `Torch` / `TNT` 在双模式场景中行为正常

### 3.3 MCP 验证
重点验证基于 Unity MCP 的工程操作链路。

必须覆盖：
- 文档中列出的 Editor 脚本能被 MCP 正确生成或更新
- 相关场景能通过 MCP 创建、保存、重新打开
- 相关资产能通过 MCP 刷新、查询和验证
- 测试程序集能通过 `tests-run` 执行

## 4. 验收场景
### 4.1 资源扫描验收
通过标准：
- 能扫描 `Assets/Tiny Swords`
- 生成 `ResourceCatalog.asset`
- 导出 `resource-catalog.json`
- 目录中动画资源被正确归类：
  - `Water Tile animated.asset`
  - `Bushes`
  - `Rocks in the Water`
  - `Fire`
  - `Explosion`
  - `Water Splash`
  - `Dust`
  - `Rubber Duck`

### 4.2 RoomChain 验收
通过标准：
- 能一键构建 `RoomChain` 样板场景
- 样板场景包含 7 个房间
- 主线和支线可达
- 至少 1 个主战斗房
- 至少 1 个奖励房
- 至少 1 个资源房
- 至少 1 段常驻水体/岸线动画
- 至少 1 组事件反馈动画
- 地图可保存为 `roomchain-save.json`
- 从 `roomchain-save.json` 能重建场景

### 4.3 OpenWorld 验收
通过标准：
- 能一键构建 `OpenWorld` 样板场景
- 样板场景包含 5 个连续区域
- 玩家可连续移动穿过全部区域
- 至少 2 个区域包含开放战斗
- 至少 1 个区域包含资源点聚集
- 至少 2 个区域包含触发式召唤区
- 大面积水体动画、植被动画和环境道具动画都能出现
- 地图可保存为 `openworld-save.json`
- 从 `openworld-save.json` 能重建场景

### 4.4 动态世界验收
通过标准：
- `AnimatedWater`、`AnimatedShoreline` 常驻
- `AnimatedVegetation`、`AmbientProps` 能按房间/区域或相机邻近激活
- `ReactiveFX` 只在交互/战斗/召唤触发时出现
- 动画不会挡住楼梯口、窄道入口、奖励点
- 动画不会误导可通行边界

## 5. 性能与预算
本阶段只给预算和验证方法，不承诺平台级优化实现。

### 5.1 RoomChain 预算
- 常驻动画对象：建议 `<= 24`
- 同屏区域动画对象：建议 `<= 16`
- 单次 ReactiveFX 峰值：建议 `<= 8`

### 5.2 OpenWorld 预算
- 常驻动画对象：建议 `<= 40`
- 同屏区域动画对象：建议 `<= 24`
- 单次 ReactiveFX 峰值：建议 `<= 10`
- 同屏总动态对象：建议 `<= 64`

### 5.3 验证方式
- 通过运行时调试计数器输出当前激活动画对象数
- 在 PlayMode 测试中验证激活上限不被突破
- 对 `OpenWorld` 至少做一条“相机穿越 5 区域”的预算回归测试

## 6. 自动化测试清单
### 6.1 新增 EditMode 测试程序集
- `Game2DRPG.Map.EditMode.Tests`

建议测试类：
- `ResourceCatalogEditModeTests`
- `TileLayerRulesEditModeTests`
- `RoomChainGeneratorEditModeTests`
- `OpenWorldGeneratorEditModeTests`
- `JsonExportEditModeTests`
- `MapBuilderWindowEditModeTests`

### 6.2 新增 PlayMode 测试程序集
- `Game2DRPG.Map.PlayMode.Tests`

建议测试类：
- `RoomChainPlayModeTests`
- `OpenWorldPlayModeTests`
- `AnimationActivationPlayModeTests`
- `SaveLoadPlayModeTests`

## 7. Unity MCP 实施验证链路
### 7.1 文档到脚本
通过 MCP：
- `script-update-or-create` 生成或更新 Editor / Runtime 脚本
- `assets-refresh` 触发重新编译

### 7.2 资产与场景
通过 MCP：
- `scene-create`、`scene-open`、`scene-save`
- `assets-find`、`assets-get-data`
- `gameobject-create`、`gameobject-find`、`gameobject-modify`

### 7.3 验证
通过 MCP：
- `tests-run(EditMode)`
- `tests-run(PlayMode)`
- `scene-get-data`
- `screenshot-game-view`
- `screenshot-scene-view`

## 8. 失败回退策略
### 8.1 资源扫描失败
处理方式：
- 回退到上一次成功的 `ResourceCatalog.asset`
- 不允许继续生成地图

### 8.2 JSON 导出失败
处理方式：
- 保留 SO 作为权威
- 阻止保存成功提示
- 在窗口中输出详细错误

### 8.3 RoomChain 生成失败
处理方式：
- 清空未完成场景
- 保留上一次成功的样板场景
- 输出失败的 seed 和模板 ID

### 8.4 OpenWorld 生成失败
处理方式：
- 回退到上一次成功的 `OpenWorld` 样板场景
- 输出失败的 seed、区域模板和预算摘要

### 8.5 动画预算超标
处理方式：
- 优先裁减 `AnimatedVegetation`
- 再裁减 `AmbientProps`
- 不裁减 `AnimatedWater` 和 `AnimatedShoreline`

## 9. 最终验收顺序
实现完成后的正式验收顺序固定为：
1. `ResourceCatalog` 资产与 JSON 验证
2. `RoomChain` 样板构建验证
3. `RoomChain` PCG 验证
4. `OpenWorld` 样板构建验证
5. `OpenWorld` PCG 验证
6. 保存/加载闭环验证
7. 动画预算与导航不冲突验证
8. 现有战斗切片复用验证

## 10. 交付门槛
只有全部满足以下条件，才允许进入“完成实现”结论：
- EditMode 测试通过
- PlayMode 测试通过
- RoomChain 样板可玩
- OpenWorld 样板可玩
- 两类 JSON 可导出、可加载
- 动态世界规则生效
- 现有玩家与敌人逻辑在双模式中都正常
