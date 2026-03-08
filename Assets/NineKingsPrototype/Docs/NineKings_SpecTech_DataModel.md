# 9 Kings Prototype Spec Tech: Data Model

## 1. 数据权威
首版以 ScriptableObject 内容库为权威数据源。
运行时 save 使用 JSON。

## 2. 内容对象
必须包含：
- `KingDefinition`
- `OpponentKingDefinition`
- `CardDefinition`
- `CardEffectDefinition`
- `RoyalDecreeDefinition`
- `BlessingDefinition`
- `MerchantDefinition`
- `YearEventScheduleDefinition`
- `BattleStatCurveDefinition`

## 3. 运行态对象
必须包含：
- `RunState`
- `PlotState`
- `BattleUnitState`
- `SaveGameState`
- `RunDebugSnapshot`

## 4. KingDefinition 关键字段
- `KingId`
- `DisplayName`
- `ThemeColor`
- `CardIds`
- `RewardPoolIds`
- `BaseCardId`
- `RoyalDecreeIds`
- `BlessingIds`

## 5. CardDefinition 关键字段
- `CardId`
- `DisplayName`
- `CardType`
- `OwnerKingId`
- `TargetRule`
- `MaxLevel`
- `Upgradeable`
- `InfiniteStack`
- `PlotSprite`
- `UnitSprite`
- `Description`
- `LevelStats`
- `Effects`

## 6. RunState 关键字段
- `Year`
- `Lives`
- `Gold`
- `CurrentEnemyKingId`
- `RemainingEnemyKingIds`
- `HandCardIds`
- `DeckCardIds`
- `DiscardCardIds`
- `SelectedDecreeIds`
- `PendingBlessing`
- `BoardPlots`
- `RewardRerollCost`
- `MerchantRerollCost`

## 7. Save 范围
首版保存：
- 年份
- 生命
- 金币
- 版图
- 手牌 / 牌库 / 弃牌
- 事件状态
- 当前敌王
- 重掷成本
不保存逐帧战斗现场，只保存年阶段边界状态。
