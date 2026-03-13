# NineKings Prototype Implementation Log

## V2 第一里程碑
- 已建立 `NineKingsPrototype/V2` 并行实现子树，不覆盖旧原型。
- 已完成第一阶段“数据底座优先”：
  - 新增 V2 运行时、编辑器、EditMode、PlayMode 四个独立程序集。
  - 新增 `ContentDatabase`、`KingDefinition`、`CardDefinition`、`CardCombatConfig`、`CardPresentationConfig`、`UnitArchetypeDefinition`、`SpawnPatternSpec`、`WeaponFXSpec`、`StackDisplayRule`、`LootPoolDefinition`、`BattleCurveDefinition`。
  - 新增运行态对象：`RunState`、`PlotState`、`BoardCoord`、`BoardSceneState`、`BattleSceneState`、`PresentationSnapshot`。
  - 新增拖拽与放置骨架：`CardHandState`、`DragSession`、`PlacementPreviewState`、`PlacementValidator`。
  - 新增战斗骨架：`CombatSimulation`、`CombatPresentation`。
  - 新增 `NineKingsV2GameController`，已能跑通：`MainMenu/RunIntro -> YearStart -> CardPhase -> BattleDeploy -> BattleRun -> BattleResolve/LootChoice` 的最小闭环。
  - 新增 `NineKingsPrototypeV2Bootstrapper`，可一键生成：
    - `Assets/NineKingsPrototype/V2/Data/Definitions/NineKingsV2ContentDatabase.asset`
    - `Assets/NineKingsPrototype/V2/Scenes/NineKings_Main_V2.unity`
  - 默认内容库已接入两套玩家王国：
    - `King of Greed`
    - `King of Nothing`
  - 默认内容库已接入通用敌军 archetype 与 33 年事件表。

## 当前阶段
- 已从旧原型维护阶段切换到 `V2 并行重构 + 数据驱动底座` 阶段。

## 本轮完成
- 建立 `NineKingsPrototype/V2` 目录树与四个独立程序集。
- 写入 V2 第一批核心脚本，并通过 Unity 编译。
- 生成默认 V2 内容数据库资产与 `NineKings_Main_V2` 主场景。
- 完成 V2 基线测试：
  - 内容数据库校验
  - 新局初始化
  - 放卡 / 升级 / 附魔判定
  - JSON round-trip
  - 自动进入战斗
  - 战斗结算到 `LootChoice`
- 运行时主界面汉化：顶栏、地块详情、战斗日志、弹窗、奖励、商人、议会、外交、祝福。
- 新增 `NKChineseText` 中文映射词典，用于卡牌、敌王、王令、祝福与阶段文本。
- 修复 `F1` 打开的 GM 面板显示不全问题，重新布局锚点与 pivot。
- GM 面板增加中文标题、中文日志、中文提示、快捷按钮与输入框回车执行。
- 生成 3 份中文 MD 文档：新手说明、卡牌作用总表、敌方王国说明。

## 最近验证结果
- `NineKingsPrototype.V2.Tests.EditMode`：待本轮重跑确认。
- `NineKingsPrototype.V2.Tests.PlayMode`：2/2 通过。
- `NineKingsPrototype.Tests.EditMode`：3/3 通过。
- `NineKingsPrototype.Tests.PlayMode`：3/3 通过。
- 新增回归：`GmPanel_WhenShown_RemainsInsideScreenBounds`。

## 下一步
- 重跑并确认 `NineKingsPrototype.V2.Tests.EditMode` 全绿。
- 在 V2 上继续实现棋盘世界层、拖拽预览与菱形格表现。
- 补齐 `CombatSimulation / CombatPresentation` 的建筑驻场、兵力堆叠、远程 FX 和战利品暗场。
- 再接入 `King of Greed` 的完整可玩闭环，再用 `King of Nothing` 验证第二王国扩展能力。
