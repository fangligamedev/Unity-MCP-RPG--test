# Tiny Swords 单房间肉鸽 RPG 项目说明

## 1. 项目概述
本项目基于 Unity 6 LTS 在现有工程 `4-up-2d-rpg` 中实现，目标是构建一个参考《Hades》战斗节奏的 2D 俯视角单房间动作 RPG 垂直切片。

当前版本已经具备以下完整可玩能力：
- 玩家可进行移动、冲刺、近战攻击与受击反馈
- 房间内按波次刷新近战与投掷两类敌人
- 清空全部波次后出现可交互奖励点
- 奖励二选一后进入胜利状态
- 玩家死亡后进入失败状态
- HUD、提示文本、相机跟随、自动化测试已接通

## 2. 工程信息
- Unity 版本：Unity 6.3 LTS `6000.3.10f1`
- 工程路径：`/Users/brucef/Documents/UnityProject/test/4-up-2d-rpg`
- 主场景：`Assets/Game2DRPG/Scenes/TinySwordsArena.unity`
- 主文档：`Assets/Game2DRPG/Docs/GameplayReport.md`

## 3. 资源来源
### 3.1 美术资源
本项目当前使用的主要美术资源来自 Pixel Frog 的 Tiny Swords 资源包：
- 网站主页：https://pixelfrog-assets.itch.io/tiny-swords
- Tilemap 指南：https://pixelfrog-assets.itch.io/tiny-swords/devlog/797694/tilemap-guide

### 3.2 使用策略
原始资源包保留在：
- `Assets/Tiny Swords (Update 010)`

项目实际引用的资源经过复制和整理，放在：
- `Assets/Game2DRPG/Art/TinySwords`

这样做的目的有两个：
- 保持原始资源包不被工程逻辑直接污染，便于后续升级或替换
- 只保留本玩法切片真正需要的资源，减少依赖范围和后续维护成本

### 3.3 当前使用的核心资源
- 玩家：Knight Warrior Blue
- 近战敌人：Torch Goblin Red
- 远程敌人：TNT Goblin Red
- 投掷物：Dynamite
- 爆炸特效：Explosion
- 地形：Flat / Elevation / Water / Foam / Rocks / Tree
- 奖励交互物：Gold Mine Active
- UI：Carved Banner / Button 9-Slice

## 4. 目录结构
当前核心目录如下：

```text
Assets/Game2DRPG
├── Animations
├── Art
│   └── TinySwords
├── Docs
│   └── GameplayReport.md
├── Prefabs
├── Scenes
│   └── TinySwordsArena.unity
├── Scripts
│   ├── Editor
│   └── Runtime
├── Tests
│   ├── EditMode
│   └── PlayMode
└── UI
```

其中：
- `Scripts/Runtime` 保存实际运行逻辑
- `Scripts/Editor` 保存构建和场景生成辅助逻辑
- `Tests` 保存 EditMode 与 PlayMode 自动化测试
- `Scenes` 保存主场景资源
- `Docs` 保存项目说明与交付说明

## 5. 场景设计
### 5.1 主场景
主场景为单房间竞技场：
- 中央为战斗区域
- 外圈为水域边界
- 上方存在高地和装饰树木
- 场景右上区域保留奖励点位置
- 玩家开局出生在房间中下区域

### 5.2 视觉层次
场景按 2D 俯视角进行组织，重点是：
- 使用环境块快速拼出战斗空间
- 用树木、岩石、高地形成视觉区分和阻挡边界
- 使用屏幕顶部 HUD，避免遮挡玩家战斗视野

## 6. 核心玩法系统
### 6.1 玩家系统
玩家角色为 Knight Warrior Blue，支持：
- `WASD` / 方向键移动
- `Left Shift` 或 `Space` 冲刺
- `鼠标左键` / `Enter` / `J` 攻击
- `E` 交互
- `1` / `2` 选择奖励

玩家逻辑包含：
- 角色移动与朝向记录
- 冲刺位移和短时机动能力
- 近战攻击判定和冷却
- 受伤与死亡
- 与 HUD、奖励和关卡流程联动

### 6.2 敌人系统
当前敌人分为两类：

#### Torch Goblin
- 近战追击型敌人
- 主动靠近玩家
- 接触或近距离造成伤害
- 用于构成基础走位压力

#### TNT Goblin
- 远程投掷型敌人
- 与玩家保持一定距离
- 投出 `Dynamite` 后延迟爆炸
- 爆炸带来范围伤害，增加战场节奏变化

### 6.3 波次系统
当前房间固定 3 波敌人：
- Wave 1：3 个 Torch Goblin
- Wave 2：2 个 Torch Goblin + 1 个 TNT Goblin
- Wave 3：3 个 Torch Goblin + 2 个 TNT Goblin

波次逻辑：
- 开局自动启动第一波
- 清空当前波次敌人后，进入下一波
- 清空全部 3 波后，激活奖励点

### 6.4 奖励系统
清空全部敌人后，右上角奖励点被激活。玩家靠近并按 `E` 交互后可进行二选一：
- `1`：攻击力 +1
- `2`：回复 2 点生命并提升最大生命 +1

选择任意奖励后直接进入胜利状态。

### 6.5 胜负状态
- 玩家清空 3 波并完成奖励选择后：`Victory`
- 玩家生命归零后：`Defeat`

## 7. UI 与交互说明
### 7.1 HUD 内容
HUD 当前包含：
- 玩家生命值
- 当前波次
- 当前剩余敌人数
- 状态提示文本
- 奖励选择面板
- 胜利 / 失败提示

### 7.2 HUD 布局修复记录
本项目在迭代过程中曾出现一个明显 UI 缺陷：HUD 被渲染到屏幕中间，遮挡玩家和敌人视线。

问题根因：
- `UIRoot/Canvas/HUD` 使用的是普通 `Transform`
- 其子对象虽然设置了顶部锚点，但由于父节点不是标准 UI 布局根节点，导致布局参考系错误
- 结果是整组 HUD 漂移到画面中部

修复措施：
- 将 `HUD` 根节点改为铺满画布的 `RectTransform`
- 重新整理顶部状态条与文本布局，使其固定贴在屏幕最上方
- 重绑 `Bootstrap`、`ArenaGameState`、`WaveDirector` 对新的 `HudPresenter` 引用
- 同步修改编辑器构建脚本，避免后续场景重建时回退到错误布局
- 增加 EditMode 回归测试，确保 HUD 根节点必须为 `RectTransform`

修复结果：
- HUD 已不再遮挡战斗主视野
- 布局规则稳定，可通过自动化测试回归验证

## 8. 动画排查与修复经验
### 8.1 角色闪烁问题根因
本项目在角色动画接入阶段，先后出现了三类典型问题：
- 主角待机和移动时闪烁
- 主角在没有输入时看起来像在自己触发攻击动画
- Torch Goblin 与 TNT Goblin 在循环播放时会突然闪一下

根因不是单一问题，而是两层错误叠加：

第一层错误是精灵排序方式错误：
- 最初动画片段是按字符串排序精灵名取帧
- 这会导致 `0, 1, 10, 11...` 排在 `2, 3...` 前面
- 结果是同一动作里混入错误帧，表现为明显闪烁和假动作

第二层错误是切片策略错误：
- Tiny Swords 角色图集本质上是规则网格帧表
- 如果用透明区域自动切片，剑光、火焰、爆炸边缘等独立效果会被切成小碎片 Sprite
- 动画片段一旦按索引取帧，就可能把完整角色和局部碎片混在一起
- 即使索引连续，也仍然会出现角色闪烁、局部消失、看起来像空帧的问题

### 8.2 这次最终采用的修复策略
为彻底解决问题，这次统一采用以下方案：

- 玩家图集 `Warrior_Blue.png` 改为规则网格切片：`6 x 8`
- Torch 图集 `Torch_Red.png` 改为规则网格切片：`7 x 5`
- TNT 图集 `TNT_Red.png` 改为规则网格切片：`7 x 3`
- 动画片段只允许从完整网格帧中取值，不再依赖透明轮廓切片结果
- 加入帧序回归测试，固定关键动画的精灵名顺序

### 8.3 最终确认过的空帧
在规则网格切片后，继续做了逐格有效像素排查，最终确认以下帧是完全透明空帧，必须从动画中排除：

- `Torch_Red_13`
- `Torch_Red_20`
- `Torch_Red_34`
- `TNT_Red_6`
- `TNT_Red_13`

这些帧如果被放入循环动画，就会出现用户肉眼可见的“闪一下”。

### 8.4 当前稳定的关键动画区间
当前工程中经过验证的关键动画区间如下：

- 玩家 Idle：`Warrior_Blue_0 ~ 5`
- 玩家 Move：`Warrior_Blue_6 ~ 11`
- 玩家 Attack：`Warrior_Blue_12 ~ 17`
- Torch Idle：`Torch_Red_0 ~ 6`
- Torch Move：`Torch_Red_7 ~ 12`
- Torch Attack：`Torch_Red_14 ~ 19`
- Torch Death：`Torch_Red_28 ~ 33`
- TNT Idle：`TNT_Red_0 ~ 5`
- TNT Move：`TNT_Red_7 ~ 12`
- TNT Attack：`TNT_Red_14 ~ 20`
- TNT Death：`TNT_Red_18 ~ 20`

### 8.5 可复用经验
这次修复沉淀出几条可复用经验：

1. 对像素角色图集，先判断它是“规则网格帧表”还是“透明岛切片素材”，不要默认自动切片。
2. 动画片段不要只看帧名连续，必须确认每一帧是不是完整角色帧。
3. 如果角色循环动画中“总有一帧会闪”，优先排查是否存在透明空帧或局部碎片帧被选入。
4. 在 EditMode 测试里直接断言关键动画片段的精灵顺序，能有效避免后续构建器回归。

## 9. 核心代码结构
以下是当前主要运行时脚本职责概览：
- `TopDownPlayerController`：玩家移动、朝向、冲刺与基础状态控制
- `PlayerCombat`：近战攻击、攻击冷却、伤害输出
- `Health`：通用生命值与死亡事件
- `EnemyBrainTorchGoblin`：近战敌人追击逻辑
- `EnemyBrainTntGoblin`：投掷敌人逻辑
- `DynamiteProjectile`：炸药投掷与爆炸触发
- `ExplosionDamage`：范围伤害判定
- `WaveDirector`：波次生成、剩余敌人统计、流程推进
- `RewardShrine`：奖励交互逻辑
- `ArenaGameState`：胜利 / 失败 / 奖励状态切换
- `CameraFollow2D`：相机跟随
- `HudPresenter`：HUD 文本与提示更新
- `Bootstrap`：启动时完成引用组装与流程初始化

编辑器脚本：
- `Game2DRPGBuilder`：用于构建和修复场景内容，避免重复手工搭建

## 10. 自动化测试
### 9.1 EditMode 测试
当前 EditMode 测试覆盖以下内容：
- 主场景可打开
- 核心 prefab / 场景对象存在并具备正确组件
- HUD 根节点是 `RectTransform`
- 关键引用完整
- 场景基础结构可用

### 9.2 PlayMode 测试
当前 PlayMode 测试覆盖以下内容：
- 玩家能够移动，冲刺位移大于普通移动
- 玩家攻击可击杀敌人并推进波次
- TNT 敌人可生成炸药并造成爆炸伤害
- 三波敌人清空后可交互奖励并进入胜利
- 玩家死亡后进入失败

### 10.3 最新测试结果
最近一次验证结果如下：
- `Game2DRPG.EditMode.Tests`：6 / 6 通过
- `Game2DRPG.PlayMode.Tests`：6 / 6 通过

这意味着当前版本在核心玩法、HUD 修复以及角色/敌人动画修复之后仍保持稳定可玩。

## 11. 当前已实现范围
当前已经实现：
- 单房间 2D 俯视角战斗场景
- 玩家移动、冲刺、近战攻击
- 两类敌人行为差异
- 三波固定战斗流程
- 奖励点交互与二选一奖励
- 胜利与失败状态
- 顶部 HUD 与提示系统
- 自动化测试覆盖主要可玩路径
- 玩家与敌人动画的规则网格切片与空帧剔除

## 12. 当前未实现范围
当前版本仍然没有实现以下内容：
- 多房间推进
- 程序化地图生成
- Boss 战
- 掉落系统与构筑分支成长
- 技能树或武器流派系统
- 音效与背景音乐完善
- 存档 / 读档
- 更复杂的动画状态机和击退反馈
- 更完整的 UI 美术包装

## 13. 后续建议
建议后续优先级如下：
1. 优化 HUD 美术样式与排版层级，减少纯文字重叠感
2. 增加玩家受击无敌闪烁、击退与屏幕反馈
3. 扩展第二个房间与简单房间切换流程
4. 引入随机奖励池，把当前二选一变成更接近肉鸽构筑的选择体验
5. 增加 Boss 房，形成完整一轮战斗闭环
6. 增加音效与配乐，提升游戏反馈强度

## 14. 当前结论
截至当前版本，本项目已经从资源导入和场景搭建阶段，推进到了“可实际运行、可战斗、可胜负、可测试”的最小可玩垂直切片。

它仍然不是完整的肉鸽 RPG，但已经具备继续扩展为多房间、更多敌人和更完整 Build 系统的稳定基础。
