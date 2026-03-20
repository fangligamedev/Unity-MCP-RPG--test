# NineKings V2 当前进度汇报

更新时间补充：Sprint 1 表现精修持续进行中，当前重点是战斗构图、projectile 可见度、战后暗场与 Greed run 稳定性。
更新时间：2026-03-19

## 1. 总体结论

`NineKingsPrototype V2` 目前已经从“只有数据底座和能跑起来的原型”推进到了“表现层方向基本正确、核心交互可用、战斗前后主流程可跑”的阶段。

按当前状态做一个保守估算：

- 表现层与可玩交互：约 `80%`
- 战斗列队与基础战斗表现：约 `82%`
- 完整玩法闭环与长局内容：约 `35%`
- 整体项目完成度：约 `68% - 72%`

当前最强的部分已经不是“底层能不能跑”，而是：

- 布局、镜头、拖拽、预览、地图显示、列队、调试视图这些用户第一眼能看到的东西，已经基本建立起来了
- 战斗已经不只是“会打”，而是开始具备可见 projectile、命中、死亡、掉落和战后过渡这些最小表现闭环
- `BattleDeploy / BattleRun` 的镜头与兵力条构图已继续精修一轮，战斗主体与底部兵力 UI 的重叠关系更稳定

当前最弱的部分仍然是：

- 完整内容闭环
- 长局验证
- 事件 / Final Battle / GM 接口的完整接入
- `Greed` 首个完整可玩 smoke run 之后的持续打磨

## 2. 已完成内容

### 2.1 V2 基础运行时

- `RunState / BoardSceneState / BattleSceneState / HandState / PlacementPreviewState` 已建立并可稳定驱动运行
- `ContentDatabase + ScriptableObject` 仍是内容真相源，没有引入第二套 authoring 体系
- `NineKingsV2GameController` 已承担主状态机职责，能完成：
  - 开局
  - 出牌
  - 弃井
  - 自动开战
  - 战斗
  - 战后奖励
  - 进入下一年
- `greed_mortgage` 即时牌执行链已接通：
  - 只能作用于非基地、已占用地块
  - 会清空目标地块
  - 会按目标等级返还金币
  - 会正确进入弃牌堆并从手牌移除

### 2.2 CardPhase 表现层与交互

- HUD 布局已经重构成固定锚点
  - 左上资源
  - 顶中年份时间线
  - 右上控速
  - 左侧井
  - 右侧状态区
  - 底部手牌
- `CardPhase` 镜头已拉远并适配拖牌/看格子
- 手牌支持：
  - hover 放大
  - drag 主交互
  - 合法格预览
  - 井弃牌预览
  - 非法区域释放回手
- 地图地块支持 hover tooltip，能显示运行时信息
- 战利品卡修复为“下一年起手 4 张之一”，不再延后一轮才进手牌

### 2.3 3x3 棋盘与摆放预览

- 初始只显示中间 `3x3` 解锁格
- 棋盘线改成唯一边段生成，不再是旧的散乱小白点
- 目前已经支持：
  - 仅在布局相关状态显示棋盘
  - 预选中时显示高亮格
  - `V` 键打开调试视图
- 调试视图目前可显示：
  - 友军列队参考轴线
  - 友军列队中心
  - 友军目标列队点
  - 敌军列队中心
  - 敌军目标列队点

### 2.4 地图表现与运行时规则

- 地图上的 troop source 不再只显示固定 `1-2` 个小图
- 地图显兵已经改成按运行时有效单位数显示
  - `1-6`：直接显示成员
  - `>6`：成员 + 数字角标
- 建筑等级颜色映射已接入表现层
  - 1 级：Yellow
  - 2 级：Red
  - 3 级：Purple
  - 4 级：Blue
  - 5 级：Black
- 年度相邻结算已落地
  - 目前已实现 `农场 -> 相邻单位地块` 的永久年度增兵
- 地块 tooltip 已能显示：
  - 等级
  - 有效单位数
  - 生命/伤害/攻速/范围/移速
  - 护盾
  - 附魔层数
  - 累计伤害/累计击杀
  - 相邻增兵说明

### 2.5 战斗列队与战斗显示

- 战斗前列队已从“直接跳到阵位”改成可见的 `BattleDeploy`
- 建筑在战斗态已经固定，不再跟列队逻辑一起乱跳
- 我方单位从地图上的真实显示位置出发进入战斗，不再从错误参考点起步
- 敌方从右下入口进场
- 友军 / 敌军都已经有明确列队目标点
- 我方、敌方列队目标点都已经接入同一条参考轴线
- 单位显示层已接入 animator 状态切换：
  - `Idle`
  - `Run`
  - `Attack`
  - `Shoot`
- 近战攻击距离已经收紧，不再过早隔空出手

### 2.6 战斗表现收口 Sprint 1 已完成部分

本轮已经把“开战到结算到选奖励”的最小表现闭环补上：

- 远程单位在 `Shoot` 动画窗口内会生成可见 projectile
- ranged projectile 从攻击者当前世界坐标飞向目标当前世界坐标
- `fx-bolt` 已明确切到 Tiny Swords 箭矢资源路径
- melee 不生成 projectile，只保留接敌后的攻击动画和命中反馈
- 命中时会生成最小 `hit FX`
- 单位死亡时会生成最小 `death FX`
- 敌方死亡时会生成最小 `gold drop FX`
- `BattleResolve` 不再直接跳 `LootChoice`
- `BattleResolve` 会先冻结战场、保留残兵和结果静帧
- `LootChoice` 改成保留战场背景的暗场叠层，不再是突兀的功能切换
- 右上控速按钮在 `BattleResolve` 和 `LootChoice` 下会统一弱化

这部分的实现仍然是“最小反馈闭环”，不是最终豪华版本，但已经满足 Sprint 1 对战斗表现的最低目标。

### 2.7 已完成的测试保障

当前已经长期在维护 `EditMode` 与 `PlayMode` 回归。

最近一轮稳定通过并保留的回归范围包括：

- 布局 snapshot
- 相机 preset
- 初始 `3x3` 解锁格
- 战利品进手牌
- 敌军前期难度曲线
- 相邻增兵
- 地图显兵
- 建筑颜色
- 拖拽放置
- BattleDeploy 起点/目标点
- 我方/敌方列队轴线约束
- ranged projectile 生成条件
- `BattleResolve -> LootChoice` overlay 可见性与保留战场背景的 smoke 测试
- projectile / hit / death / gold drop 的最小表现链 smoke 测试
- `King of Greed` 从“开局 -> 放牌 -> 战斗 -> 奖励 -> 下一年继续”的基础 smoke run 用例
- `greed_mortgage` 的 EditMode / PlayMode 可用性与金币返还回归

### 2.8 Sprint 1 / Sprint 2 当前状态

按当前主计划拆分：

- `Sprint 1：战斗表现收口`
  - 已完成
- `Sprint 2：King of Greed 首个完整基础 run`
  - 代码与 PlayMode smoke run 用例已补上
  - 已在打开中的 Unity 编辑器 Test Runner 内完成 `EditMode / PlayMode` 验证
  - 当前 batchmode 仍会被“同项目 Unity 实例占用”挡住，因此正式结论以编辑器内测试结果为准

也就是说，当前代码状态已经从“准备进入 Sprint 2”推进到“`Sprint 2` 的主验证链已经写进测试并在编辑器内稳定通过”。

## 2.9 最新回归结果

在当前打开中的 Unity 编辑器 Test Runner 内，最新一轮 `NineKings V2` 命名空间测试结果为：

- `EditMode / NineKingsPrototype.V2.Tests.EditMode`：`37 passed, 0 failed`
- `PlayMode / NineKingsPrototype.V2.Tests.PlayMode`：`19 passed, 0 failed`

这一轮新增稳定结论：

- `greed_mortgage` 已可在运行时正确使用，不再因为即时牌校验或自动开战时机导致不可用
- ranged projectile / hit / death / gold drop 的最小表现链继续保持通过

## 3. 当前还没完成的工作

### 3.1 表现层仍需继续收口

- `3x3` 格线虽然已经能正确显示，但视觉风格还可以继续往原版压
- 战斗镜头与 `CardPhase` 镜头虽然能切，但还可以继续加强构图美感
- 当前调试视图是为定位问题临时加入的，后续要决定：
  - 保留为开发工具
  - 还是做成开发开关并默认关闭

### 3.2 战斗表现还差明显一截

目前战斗“能看懂，也有最小反馈”，但还没有到 9 Kings 原版那种完成度。

还缺：

- 更完整的 projectile 类型与差异化表现
- 更丰富的 hit / death / drop FX
- 更完整的近战碰撞感
- 更自然的兵团推进和接敌节奏
- 战斗结果统计、冻结和切换的更完整动效

### 3.3 完整玩法闭环仍未打通

目前还是“基础可玩闭环”，还不是“完整 run”。

主要缺口：

- `King of Greed` 的完整首个 smoke run 之后的成长曲线与长期平衡打磨
- `King of Nothing` 按同一架构稳定完整可玩
- 事件、商人、祝福、外交、扩地
- 完整 `33` 年流程
- 敌王轮换
- Final Battle
- GM 对 `run / board / battle` 的稳定改写入口

### 3.4 长局平衡还不够

虽然前期敌军已经削弱过，但目前仍然是“原型平衡”而不是“长局平衡”。

还需要继续调：

- 前 1-5 年敌军成长曲线
- 友军成长速度
- 建筑和 troop source 的收益差
- 升级收益
- 相邻规则强度

## 4. 当前剩余工作量评估

如果按你之前确认过的四大阶段来算：

### 阶段 1：表现层止血

- 完成度：约 `90%`
- 剩余：少量视觉微调、局部构图继续修饰

### 阶段 2：战斗编队与单位可视化

- 完成度：约 `84%`
- 已完成：
  - 列队
  - 地图显兵
  - battle deploy
  - 目标点/调试视图
  - 动画状态切换
  - 最小 projectile / hit / death / gold drop 反馈
- 剩余：
  - 更丰富的战斗观感
  - 远程与近战表现差异继续拉开

### 阶段 3：战后暗场与完整战斗节奏

- 完成度：约 `65%`
- 已完成：
  - 战斗结束后进入奖励流程
  - `BattleResolve` 停顿窗口
  - 暗场 overlay
  - `LootChoice` 保留战场背景
- 剩余：
  - 更完整的战斗统计冻结
  - 过渡动画
  - 更强的战后节奏感

### 阶段 4：王国接入与长局验证

- 完成度：约 `22%`
- 这一阶段仍然是当前最大的工作量来源

## 5. 下一步最建议做什么

如果继续按“先解决用户当前能直接看到的问题，再补完整系统”的原则推进，建议优先级如下：

1. 收 `BattleDeploy / BattleRun` 的最终构图与观感
2. 继续加强 projectile / hit / death / gold drop 的可见度
3. 收战后暗场和奖励过渡的最终观感
4. 继续稳住 `Greed` 基础可玩 run 与成长曲线
5. 再推进 `Nothing` 的第二王国验证，以及事件、商人、祝福、外交、`33` 年流程和 Final Battle

## 5.1 最新测试结论

在 2026-03-19 这轮同步中，已通过打开中的 Unity 编辑器 Test Runner 运行：

- `EditMode / NineKingsPrototype.V2.Tests.EditMode`：`35 passed, 0 failed`
- `PlayMode / NineKingsPrototype.V2.Tests.PlayMode`：`18 passed, 0 failed`

因此当前可以稳定确认：

- Sprint 1 的战斗表现收口已通过当前回归
- `Greed` 基础 smoke run 已进入稳定通过状态
- 当前后续开发可以正式切到“战斗表现继续精修 + Greed 基础 run 打磨”

本轮精修已落地：

- `BattleRun` 镜头进一步拉远并略微上提，战斗主体与兵力条构图更稳定
- projectile、hit FX、death FX、gold drop FX 的尺寸、时长与可见度已增强，`fx-bolt` 已切到 Tiny Swords 箭矢资源路径
- 已新增一条 EditMode 回归，锁定 projectile / hit / death / gold drop 的尺寸、速度下限与时长阈值，避免后续被改弱
- `BattleResolve` 停顿时长和暗场强度已上调，`LootChoice` 暗场遮罩更明确
- `NineKingsV2_Progress_Management.md` 已作为状态报告配套方法文档落盘并保持可维护结构

## 6. 结论

当前 `NineKings V2` 已经跨过了“只是原型”的阶段，进入了“可持续打磨的正式版本骨架”阶段。

最关键的判断是：

- 表现层方向已经基本跑对了
- 列队、拖拽、地图显兵、tooltip、相邻结算这些核心基础已经立起来了
- 战斗表现收口 Sprint 1 的主体已经完成了最小闭环
- 后续主要不是“重写系统”，而是“继续把完整 run、长局内容和更丰富的战斗表现补齐”

从工程视角看，现在最值得继续投入的是：

- `Greed` 首个完整基础 run 验证之后的持续打磨
- `Nothing` 与共用框架验证
- 长局系统闭环
- 战斗表现的第二轮精修
