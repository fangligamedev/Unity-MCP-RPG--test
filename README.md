# Unity-MCP-RPG-test

一个基于 Unity 6 的 2D 俯视角单房间肉鸽 RPG 垂直切片示例工程，使用 Tiny Swords 资源集搭建，并通过 Unity MCP 完成资源配置、场景搭建、玩法脚本和自动化测试。

## 项目概览
- 引擎版本：Unity 6.3 LTS (`6000.3.10f1`)
- 主要场景：`Assets/Game2DRPG/Scenes/TinySwordsArena.unity`
- 玩法类型：2D 俯视角、单房间、波次战斗、奖励选择、胜负结算
- 资源来源：`Tiny Swords (Update 010)`

## 已实现内容
- 玩家移动、冲刺、近战攻击、受伤与死亡
- 三波固定敌人流程
- 近战敌人 Torch Goblin 与远程敌人 TNT Goblin
- TNT 炸药投掷与爆炸伤害
- 清怪后激活祝福矿点，提供二选一奖励
- HUD、胜利、失败与提示文本
- EditMode / PlayMode 自动化测试

## 操作方式
- `WASD` / 方向键：移动
- `Left Shift` 或 `Space`：冲刺
- 鼠标左键 / `Enter` / `J`：攻击
- `E`：交互
- `1` / `2`：奖励选择

## 目录结构
- `Assets/Game2DRPG/Art/TinySwords`：复制后的最小美术资源子集
- `Assets/Game2DRPG/Animations`：角色与特效动画资源
- `Assets/Game2DRPG/Prefabs`：玩家、敌人、炸药、爆炸、矿点 prefab
- `Assets/Game2DRPG/Scripts/Runtime`：核心运行时代码
- `Assets/Game2DRPG/Scripts/Editor`：场景/资源构建器
- `Assets/Game2DRPG/Tests`：EditMode 与 PlayMode 测试
- `Assets/Game2DRPG/Docs/GameplayReport.md`：详细交付说明

## 打开与运行
1. 用 Unity 6 打开本工程根目录。
2. 打开场景 `Assets/Game2DRPG/Scenes/TinySwordsArena.unity`。
3. 点击 Play 运行。

## 测试
可通过 Unity Test Runner 或 MCP `tests-run` 执行：
- `Game2DRPG.EditMode.Tests`
- `Game2DRPG.PlayMode.Tests`

当前工程验收结果：
- EditMode：5/5 通过
- PlayMode：5/5 通过

## 补充说明
- 详细玩法、资源清单、测试摘要见 `Assets/Game2DRPG/Docs/GameplayReport.md`。
- 工程已包含用于提交仓库的 Unity `.gitignore`，不会提交 `Library`、`Temp`、`Logs` 等生成目录。
