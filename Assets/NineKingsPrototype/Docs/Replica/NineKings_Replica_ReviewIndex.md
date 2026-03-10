# 9 Kings 复刻审阅包索引

## 1. 本地文档
### 1.1 视觉与流程
- `NineKings_Replica_VisualDesign.md`
- `NineKings_Replica_GameplayFlow.md`
- `NineKings_Replica_UIInventory.md`

### 1.2 战斗与单位
- `NineKings_Replica_CombatDesign.md`
- `NineKings_Replica_CombatRoster.md`
- `NineKings_Replica_CombatTechArchitecture.md`

### 1.3 数据驱动与实现蓝图
- `NineKings_Replica_CombatDataSchema.md`
- `NineKings_Replica_ImplementationBlueprint.md`
- `NineKings_Replica_TechArchitecture.md`

## 2. Figma / FigJam 审阅稿
### 2.1 结构与流程稿
### Screen Map
- `https://www.figma.com/online-whiteboard/create-diagram/d2ad83fa-ee3a-4ab8-a25a-18d51ff7f641?utm_source=other&utm_content=edit_in_figjam&oai_id=&request_id=e37e21ba-4654-487a-bd12-94be268ddde0`

### Core Loop Flow
- `https://www.figma.com/online-whiteboard/create-diagram/6da48598-0154-4ea8-9507-4a6b2f530dad?utm_source=other&utm_content=edit_in_figjam&oai_id=&request_id=43573875-f2fc-4f78-b08e-5cb07c9e3b87`

### Battle + UI Layout
- `https://www.figma.com/online-whiteboard/create-diagram/42db1def-87a2-4dfb-a588-dc366fe917ec?utm_source=other&utm_content=edit_in_figjam&oai_id=&request_id=7de30a99-e82d-4179-a82a-8546fc288c6e`

### 2.2 第二阶段 Figma 设计稿
- `Nine Kings Replica Review Pack`：`https://www.figma.com/design/n169WQGG9OQ9E0kmo4ecZM`
- `Visual Language Board`：`https://www.figma.com/design/n169WQGG9OQ9E0kmo4ecZM`
- `Battlefield Keyframes`：`https://www.figma.com/design/n169WQGG9OQ9E0kmo4ecZM?node-id=4-2`
- `Combat Unit Staging`：`https://www.figma.com/design/n169WQGG9OQ9E0kmo4ecZM?node-id=3-2`
- `UI Component Sheet`：`https://www.figma.com/design/n169WQGG9OQ9E0kmo4ecZM?node-id=5-2`
- `Localization Layout Board`：`https://www.figma.com/design/n169WQGG9OQ9E0kmo4ecZM?node-id=2-2`

## 3. 审阅建议顺序
1. 先读 `NineKings_Replica_VisualDesign.md`，确认镜头、视角、世界层和双语布局目标。
2. 再读 `NineKings_Replica_CombatDesign.md` 与 `NineKings_Replica_CombatRoster.md`，确认两王国卡牌、兵力、建筑、武器和特技的事实落地方式。
3. 再读 `NineKings_Replica_CombatDataSchema.md`，确认后续数据驱动 authoring 对象、ID 体系、SO/JSON 边界与引用方向。
4. 再读 `NineKings_Replica_GameplayFlow.md`，确认发牌、拖牌、剩两张自动开战、同景自动战斗和战后战利品闭环。
5. 再读 `NineKings_Replica_ImplementationBlueprint.md` 与 `NineKings_Replica_TechArchitecture.md`，确认场景结构、状态机、拖拽、战斗模拟、资源系统和内容数据库方案。
6. 最后读 `NineKings_Replica_UIInventory.md` 并结合 Figma 页面，确认组件清单、状态显隐和静态关键帧是否足够指导实现。

## 4. 本轮交付边界
- 本轮只产出审阅文档和 Figma 审阅稿，不包含 Unity 实现改动。
- 第一阶段补齐了视觉、玩法流程、技术架构和 UI 清单。
- 第二阶段补齐了战斗设计文档、战斗技术文档、兵力/建筑 roster，以及静态关键帧设计稿。
- 第三阶段补齐了数据驱动战斗 schema、`King of Greed` 与 `King of Nothing` 两王国卡牌数值配置表、完整实现蓝图，以及内容数据库与配置系统方案。
- 本轮不承诺最终商业级像素稿，但当前交付已足够直接指导后续工程和代码实现。
