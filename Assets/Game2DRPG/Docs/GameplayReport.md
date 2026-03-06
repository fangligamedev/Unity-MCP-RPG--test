# Tiny Swords 单房间肉鸽 RPG 交付说明

## 工程与资源来源
- Unity 工程：`/Users/brucef/Documents/UnityProject/test/4-up-2d-rpg`
- 主场景：`Assets/Game2DRPG/Scenes/TinySwordsArena.unity`
- 美术资源来源：`Assets/Tiny Swords (Update 010)`
- 参考页面：<https://pixelfrog-assets.itch.io/tiny-swords>
- Tilemap 指南：<https://pixelfrog-assets.itch.io/tiny-swords/devlog/797694/tilemap-guide>

本次实现只使用 Tiny Swords 资源，不再引用 `mystic_woods_free_2.2`。原始资源包保持不改，实际工程中使用的是复制到 `Assets/Game2DRPG/Art/TinySwords` 的子集。

## 复制资源清单
- 玩家：`Warrior_Blue.png`
- 近战敌人：`Torch_Red.png`
- 远程敌人：`TNT_Red.png`、`Dynamite.png`
- 特效：`Explosions.png`、`Fire.png`
- 地形与装饰：`Tilemap_Flat.png`、`Tilemap_Elevation.png`、`Shadows.png`、`Water.png`、`Foam.png`、`Rocks_01.png`、`Rocks_02.png`、`Rocks_03.png`、`Rocks_04.png`、`Tree.png`、`GoldMine_Active.png`
- UI：`UI_Carved_Regular.png`、`UI_Carved_9Slides.png`、`UI_Button_Blue_9Slides.png`、`UI_Button_Hover_9Slides.png`、`UI_Button_Red_9Slides.png`

## 场景结构
场景采用单房间 2D 俯视角竞技场，基于 64 PPU 的 Tiny Swords 资源搭建。

根对象结构：
- `Environment`：水面、地面、边界装饰、树木、岩石与碰撞障碍
- `Systems`：`ArenaGameState`、`WaveDirector`、`Bootstrap`
- `Spawns`：4 个刷怪点
- `UIRoot`：Canvas、HUD、奖励面板、胜负面板、EventSystem
- `Player`：玩家角色与战斗组件
- `RewardShrine`：波次完成后的祝福矿点
- `Main Camera`：正交相机与跟随逻辑

## 角色与敌人设计
- 玩家角色：`Knight Warrior Blue`
- 近战敌人：`Torch Goblin Red`
- 远程敌人：`TNT Goblin Red`
- 远程攻击物：`Dynamite`
- 爆炸特效：`Explosion`
- 奖励交互物：`GoldMine_Active` 作为祝福矿点

运行时脚本包括：
- `TopDownPlayerController`
- `PlayerCombat`
- `Health`
- `EnemyBrainTorchGoblin`
- `EnemyBrainTntGoblin`
- `DynamiteProjectile`
- `ExplosionDamage`
- `WaveDirector`
- `RewardShrine`
- `ArenaGameState`
- `CameraFollow2D`
- `HudPresenter`
- `Bootstrap`

## 操作方式
- `WASD` / 方向键：移动
- `Left Shift` 或 `Space`：冲刺
- 鼠标左键 / `Enter` / `J`：攻击
- `E`：与祝福矿点交互
- `1` / `2`：选择奖励

## 已实现玩法范围
- 单房间 2D 俯视角战斗切片
- 玩家移动、冲刺、近战攻击、受击与死亡
- 三波固定敌人流程
  - Wave 1：3 个 Torch Goblin
  - Wave 2：2 个 Torch Goblin + 1 个 TNT Goblin
  - Wave 3：3 个 Torch Goblin + 2 个 TNT Goblin
- TNT Goblin 会投掷炸药，炸药可对玩家造成爆炸伤害
- 全部清波后激活祝福矿点
- 奖励二选一
  - `1`：攻击力 +1
  - `2`：回复 2 点生命并提升最大生命 +1
- 胜利、失败、提示文本与基础 HUD
- 可用于测试的伪输入注入接口 `IPlayerInputSource`

## 未实现范围
- 多房间推进
- 随机房间或程序化地图
- Build 流派系统
- Boss 房
- 掉落装备与局外成长
- NPC、对话、商店与存档
- 更复杂的动画状态机与音效系统

## 测试结果摘要
测试日期：2026-03-06

执行顺序：
1. `tests-run(EditMode, testAssembly=Game2DRPG.EditMode.Tests)`
2. `tests-run(PlayMode, testAssembly=Game2DRPG.PlayMode.Tests)`

结果：
- EditMode：目标用例 4 个全部通过，关键检查覆盖场景存在、玩家 prefab、敌人与矿点 prefab、HUD 与刷怪点结构
- PlayMode：5 个用例全部通过，覆盖移动与冲刺、玩家攻击推进波次、TNT 投掷与爆炸伤害、清波后交互胜利、玩家死亡失败

PlayMode 通过用例：
- `ClearingAllWavesAndChoosingReward_TriggersVictory`
- `Player_AttackCanClearWaveAndAdvanceProgress`
- `Player_CanMoveAndDashFurtherThanNormalMove`
- `PlayerHealthZero_TriggersDefeat`
- `TntGoblin_CanThrowDynamiteAndDealDamage`

## 本次修正说明
- 修复了 `WaveDirector` 场景实例上的敌人 prefab 空引用，确保第二波和第三波能生成 TNT 敌人
- 给 `DynamiteProjectile` 增加 `Rigidbody2D` 与稳定的 2D 触发逻辑，提升炸药命中可靠性
- 调整 `EnemyBrainTntGoblin` 的投掷判定，使其在近距离后撤时仍能稳定投掷
- 为 PlayMode 测试补充更明确的失败提示，便于后续定位

## 后续扩展建议
- 扩展为多房间路线选择与清图推进
- 把奖励升级为真正的 Build 选择，例如暴击、吸血、冲刺伤害、投射强化
- 增加 Boss 房和精英怪
- 引入掉落、拾取物和局内经济
- 接入音效、屏幕震动与更完整的战斗反馈
- 进一步把地形升级为完整 tile palette 工作流
