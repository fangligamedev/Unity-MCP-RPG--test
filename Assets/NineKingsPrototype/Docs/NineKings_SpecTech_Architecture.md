# 9 Kings Prototype Spec Tech: Architecture

## 1. 模块目标
`NineKingsPrototype` 是当前 Unity 工程中的独立策略自动战斗模块。
- 不依赖 `Game2DRPG` 运行时代码
- 仅共享 Unity 项目依赖与可复用美术资源
- 目标是完整跑通 33 年单王国 Alpha
- 运行时内建 GM 面板与命令系统

## 2. 工程边界
目录：
- `Assets/NineKingsPrototype/Scenes`
- `Assets/NineKingsPrototype/Data`
- `Assets/NineKingsPrototype/Scripts/Runtime`
- `Assets/NineKingsPrototype/Scripts/Editor`
- `Assets/NineKingsPrototype/UI`
- `Assets/NineKingsPrototype/Tests`

程序集：
- `NineKingsPrototype.Runtime`
- `NineKingsPrototype.Editor`
- `NineKingsPrototype.Tests.EditMode`
- `NineKingsPrototype.Tests.PlayMode`

## 3. 运行时系统分层
### 3.1 Content Layer
负责加载王国、卡牌、事件、商人和数值曲线配置。

### 3.2 State Layer
负责维护：
- 年份
- 生命
- 金币
- 牌库 / 手牌 / 弃牌
- 版图状态
- 事件状态
- 敌王状态

### 3.3 Flow Layer
负责驱动：
- 年开始
- 事件阶段
- 出牌阶段
- 自动战斗
- 战后选卡
- 进入下一年

### 3.4 Battle Layer
负责：
- 友军生成
- 敌军波次生成
- 同图自动战斗
- Base 失命
- 最终胜负判定

### 3.5 Presentation Layer
负责：
- 棋盘与手牌 UI
- Plot 信息面板
- 事件弹窗
- 战后选卡界面
- GM 调试面板

## 4. 场景结构
主场景固定为 `NineKings_Main.unity`。
根节点固定为：
- `WorldRoot`
- `BoardRoot`
- `CombatRoot`
- `UIRoot`
- `CameraRoot`
- `DebugRoot`

## 5. 年度状态流
`Boot -> MainMenu -> NewRun/LoadRun -> YearStart -> EventPhase -> CardPhase -> BattlePhase -> RewardPhase -> YearAdvance -> EndRun`

## 6. GM 系统位置
GM 系统属于运行时模块的一部分。
- 入口：`F1`
- 只在 Editor 与 Development Build 启用
- 包括面板按钮和文本命令解释器
- 所有 GM 操作都必须经过统一服务，而不是散落在 UI 中

## 7. 实现阶段顺序
1. 数据对象与内容数据库
2. 棋盘、Plot、手牌与 UI 交互
3. 年度流转与事件日历
4. 自动战斗
5. 33 年全流程与 Final Battle
6. GM 调试系统
7. Save / Load 与测试
