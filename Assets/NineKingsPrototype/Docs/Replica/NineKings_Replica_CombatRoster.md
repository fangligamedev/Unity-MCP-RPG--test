# 9 Kings 复刻审阅包：战斗单位与建筑 Roster

## 1. 文档目标
本文件从“描述性 roster”升级为“可配置表驱动主文档”。后续工程实现阶段，`King of Greed` 与 `King of Nothing` 的每张卡都必须至少能映射到：
- `CardDefinition`
- `CardCombatConfig`
- `CardPresentationConfig`
- `SpawnPatternSpec`
- `WeaponFXSpec`

本文件区分两类信息：
- **公开可确认事实**：卡名、卡类、部分公开数值与效果语义
- **高复刻拟合设计**：战斗中显示方式、武器特技表现、L2/L3 数值扩展、编队显示与 FX 方案

## 2. 数值体系总则
### 2.1 目标
- 数值目标：`高复刻拟合`
- 效果系统：`半通用效果库`
- 数据权威：`ScriptableObject 为权威，JSON 做保存/导出/调试快照`
- 战斗时间模型：`固定步长实时`

### 2.2 通用字段口径
- `maxHp`：驻场建筑或单位原型的生命上限
- `armor`：固定减伤层
- `shield`：优先消耗护盾层
- `attackDamage`：单次命中伤害
- `attackInterval`：攻击间隔，单位秒
- `attackRange`：攻击距离，单位格
- `moveSpeed`：移动速度，单位格/秒
- `projectileSpeed`：投射速度，单位格/秒
- `splashRadius`：溅射半径，单位格
- `unitCount`：该卡默认生成的单位数量
- `goldYield`：年末收益或击杀收益
- `auraRadius`：光环半径，单位格

### 2.3 通用枚举建议
- `presenceType`：`none | structure | troop-source | enchantment-anchor`
- `combatRole`：`base | eco-structure | tower | aura-structure | melee-swarm | melee-bruiser | ranged-line | assassin | support | enchantment | tome-burst`
- `damageType`：`physical | pierce | beam | magic | execute | economic`
- `targetPriority`：`nearest-enemy | highest-hp | lowest-hp | structure-first | base-first | marked-target`
- `engageRule`：`hold-lane | chase-nearest | flank-dash | stationary-fire | aura-only | instant-resolve`
- `retargetRule`：`on-target-death | on-range-break | periodic-scan | never`
- `deathRule`：`corpse-fade | coin-burst | dissolve | explode | instant-remove`
- `survivorPersistence`：`clear-after-battle | keep-structure | keep-enchantment`

### 2.4 数量显示规则
- `single`：1-2 名单位，直接实例化
- `micro-squad`：3-4 名单位，显示 3 个主单位
- `small-squad`：5-8 名单位，显示 3 个主单位 + 队列残影
- `banner-stack`：9+ 名单位，显示 3 个主单位 + 数量角标
- `no-stack`：建筑或即时牌，不使用单位堆叠

## 3. King of Greed
### 3.1 王国战斗定位
`King of Greed` 的玩法核心不是“大兵团海量推进”，而是“财富驱动的精英小队 + 经济建筑 + 资源换战力”。其战斗观感必须具备：
- 建筑驻场显著可见
- 兵力偏少但质量高
- 金币、收益、掠夺、财富转化在战斗中有真实反馈
- `Palace` 不是纯被动底座，而是带“射线/处决式打击”存在感的核心建筑

### 3.2 Card Identity Table
| cardId | displayNameZh | displayNameEn | ownerKingId | cardType | rarity/sourceTag | maxLevel | targetRule | stackRule |
|---|---|---|---|---|---|---|---|---|
| greed-palace | 宫殿 | Palace | king-greed | Base | starter-core | 3 | place-on-empty-plot | none |
| greed-vault | 金库 | Vault | king-greed | Building | core-economy | 3 | place-on-empty-plot | none |
| greed-thief | 盗贼 | Thief | king-greed | Troop | core-troop | 3 | place-on-empty-plot | small-squad |
| greed-over-invest | 超额投资 | Over-invest | king-greed | Enchantment | risk-economy | 3 | attach-to-owned-plot | none |
| greed-mercenary | 佣兵 | Mercenary | king-greed | Troop | paid-troop | 3 | place-on-empty-plot | banner-stack |
| greed-dispenser | 分发器 | Dispenser | king-greed | Tower | utility-tower | 3 | place-on-empty-plot | none |
| greed-beacon | 信标 | Beacon | king-greed | Building | aura-economy | 3 | place-on-empty-plot | none |
| greed-midas-touch | 点金之手 | Midas Touch | king-greed | Enchantment | premium-enchant | 3 | attach-to-owned-structure | none |
| greed-mortgage | 抵押 | Mortgage | king-greed | Tome | instant-economy | 1 | choose-owned-plot-or-run | none |

### 3.3 Board Presence Table
| cardId | presenceType | occupancyFootprint | placementAnchor | blocksPlacement | renderLayer |
|---|---|---|---|---|---|
| greed-palace | structure | 1x1 | center | true | placed-structures/base |
| greed-vault | structure | 1x1 | center | true | placed-structures/building |
| greed-thief | troop-source | 1x1 | center | true | board-ground/troop-slot |
| greed-over-invest | enchantment-anchor | 0x0 | overlay-on-plot | false | board-cells/enchantment |
| greed-mercenary | troop-source | 1x1 | center | true | board-ground/troop-slot |
| greed-dispenser | structure | 1x1 | center | true | placed-structures/tower |
| greed-beacon | structure | 1x1 | center | true | placed-structures/aura |
| greed-midas-touch | enchantment-anchor | 0x0 | overlay-on-structure | false | board-cells/enchantment |
| greed-mortgage | none | 0x0 | no-persistent-anchor | false | overlay-modals/instant |

### 3.4 Combat Spawn Table
| cardId | spawnsUnits | unitArchetypeId | spawnPatternId | unitCountBase | deployDelay | spawnCooldown | reinforceRule |
|---|---|---|---|---|---|---|---|
| greed-palace | false | none | none | 0 | 0.0 | 0.0 | none |
| greed-vault | false | none | none | 0 | 0.0 | 0.0 | none |
| greed-thief | true | greed-thief-scout | spawn-front-arc-tight | 2 | 0.25 | 0.0 | none |
| greed-over-invest | false | none | none | 0 | 0.0 | 0.0 | none |
| greed-mercenary | true | greed-mercenary-swordsman | spawn-front-arc-wide | 3 | 0.30 | 0.0 | none |
| greed-dispenser | false | none | none | 0 | 0.0 | 2.6 | periodic-shot |
| greed-beacon | false | none | none | 0 | 0.0 | 0.0 | aura-only |
| greed-midas-touch | false | none | none | 0 | 0.0 | 0.0 | on-owner-kill-burst |
| greed-mortgage | false | none | none | 0 | 0.0 | 0.0 | instant-gold-convert |

### 3.5 Level Stats Table
| cardId | maxHp L1/L2/L3 | armor L1/L2/L3 | shield L1/L2/L3 | attackDamage L1/L2/L3 | attackInterval L1/L2/L3 | attackRange L1/L2/L3 | moveSpeed L1/L2/L3 | projectileSpeed L1/L2/L3 | splashRadius L1/L2/L3 | unitCount L1/L2/L3 | goldYield L1/L2/L3 | auraRadius L1/L2/L3 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| greed-palace | 180 / 230 / 290 | 4 / 6 / 8 | 10 / 16 / 24 | 22 / 30 / 40 | 2.4 / 2.2 / 2.0 | 4.5 / 5.0 / 5.5 | 0 / 0 / 0 | 14 / 16 / 18 | 0 / 0 / 0 | 0 / 0 / 0 | 1 / 2 / 3 | 0 / 0 / 0 |
| greed-vault | 110 / 150 / 205 | 1 / 2 / 3 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 3 / 6 / 9 | 0 / 0 / 0 |
| greed-thief | 34 / 48 / 66 | 0 / 1 / 2 | 0 / 0 / 0 | 9 / 13 / 18 | 0.85 / 0.80 / 0.75 | 0.8 / 0.8 / 0.8 | 2.2 / 2.35 / 2.5 | 0 / 0 / 0 | 0 / 0 / 0 | 2 / 3 / 4 | 1 / 1 / 2 | 0 / 0 / 0 |
| greed-over-invest | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 2 / 4 / 7 | 0 / 0 / 0 |
| greed-mercenary | 44 / 60 / 82 | 1 / 2 / 3 | 0 / 0 / 0 | 11 / 15 / 21 | 1.00 / 0.95 / 0.90 | 1.0 / 1.0 / 1.0 | 1.7 / 1.8 / 1.9 | 0 / 0 / 0 | 0 / 0 / 0 | 3 / 4 / 5 | 0 / 0 / 0 | 0 / 0 / 0 |
| greed-dispenser | 85 / 118 / 164 | 1 / 2 / 3 | 0 / 0 / 0 | 8 / 14 / 24.5 | 1.8 / 1.7 / 1.55 | 4.2 / 4.7 / 5.2 | 0 / 0 / 0 | 12 / 13 / 14 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |
| greed-beacon | 80 / 110 / 150 | 1 / 2 / 3 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 2.0 / 2.5 / 3.0 |
| greed-midas-touch | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 2 / 4 / 6 | 0 / 0 / 0 |
| greed-mortgage | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 18 / 0 / 0 | 0 / 0 / 0 |

### 3.6 Battle Behavior Table
| cardId | combatRole | damageType | targetPriority | engageRule | retargetRule | deathRule | survivorPersistence |
|---|---|---|---|---|---|---|---|
| greed-palace | base | beam | lowest-hp | stationary-fire | periodic-scan | coin-burst | keep-structure |
| greed-vault | eco-structure | economic | none | aura-only | never | dissolve | keep-structure |
| greed-thief | assassin | physical | lowest-hp | flank-dash | on-target-death | coin-burst | clear-after-battle |
| greed-over-invest | enchantment | economic | none | aura-only | never | instant-remove | keep-enchantment |
| greed-mercenary | melee-bruiser | physical | nearest-enemy | chase-nearest | on-target-death | corpse-fade | clear-after-battle |
| greed-dispenser | tower | pierce | nearest-enemy | stationary-fire | periodic-scan | dissolve | keep-structure |
| greed-beacon | aura-structure | magic | none | aura-only | never | dissolve | keep-structure |
| greed-midas-touch | enchantment | economic | marked-target | instant-resolve | never | coin-burst | keep-enchantment |
| greed-mortgage | tome-burst | economic | structure-first | instant-resolve | never | instant-remove | clear-after-battle |

### 3.7 Presentation & FX Table
| cardId | worldObjectType | unitVisualType | stackDisplayRule | weaponFxId | hitFxId | deathFxId | lootFxId | audioCueGroup |
|---|---|---|---|---|---|---|---|---|
| greed-palace | palace-large | none | no-stack | fx-beam-palace | fx-hit-gold-burn | fx-death-structure-collapse | fx-loot-gold-burst | sfx-greed-palace |
| greed-vault | vault-compact | none | no-stack | fx-none | fx-hit-dust-small | fx-death-wood-break | fx-loot-coin-pop | sfx-greed-vault |
| greed-thief | none | thief-sprite-set | micro-squad | fx-melee-dagger-slash | fx-hit-coin-spark | fx-death-fade-coins | fx-loot-steal-gold | sfx-greed-thief |
| greed-over-invest | enchant-overlay-gold | none | no-stack | fx-enchant-risk-pulse | fx-hit-gold-pulse | fx-none | fx-loot-invest-return | sfx-greed-invest |
| greed-mercenary | none | mercenary-sprite-set | small-squad | fx-melee-sword-impact | fx-hit-metal-clang | fx-death-fade-dust | fx-loot-bounty | sfx-greed-mercenary |
| greed-dispenser | dispenser-turret | none | no-stack | fx-projectile-gold-bolt | fx-hit-gold-spark | fx-death-device-shatter | fx-none | sfx-greed-dispenser |
| greed-beacon | beacon-obelisk | none | no-stack | fx-aura-ring-gold | fx-hit-aura-mark | fx-death-light-fade | fx-none | sfx-greed-beacon |
| greed-midas-touch | enchant-overlay-midas | none | no-stack | fx-midas-convert | fx-hit-gold-transmute | fx-none | fx-loot-gold-rain | sfx-greed-midas |
| greed-mortgage | tome-scroll-contract | none | no-stack | fx-contract-siphon | fx-hit-economy-drain | fx-none | fx-loot-loan-burst | sfx-greed-mortgage |

### 3.8 Data Mapping Table
| cardId | CardDefinition | CardCombatConfig | CardPresentationConfig | SpawnPatternSpec | WeaponFXSpec |
|---|---|---|---|---|---|
| greed-palace | `carddef.greed-palace` | `combat.greed-palace` | `present.greed-palace` | `spawn.none` | `fx.beam-palace` |
| greed-vault | `carddef.greed-vault` | `combat.greed-vault` | `present.greed-vault` | `spawn.none` | `fx.none` |
| greed-thief | `carddef.greed-thief` | `combat.greed-thief` | `present.greed-thief` | `spawn.front-arc-tight` | `fx.melee-dagger` |
| greed-over-invest | `carddef.greed-over-invest` | `combat.greed-over-invest` | `present.greed-over-invest` | `spawn.none` | `fx.enchant-risk` |
| greed-mercenary | `carddef.greed-mercenary` | `combat.greed-mercenary` | `present.greed-mercenary` | `spawn.front-arc-wide` | `fx.melee-sword` |
| greed-dispenser | `carddef.greed-dispenser` | `combat.greed-dispenser` | `present.greed-dispenser` | `spawn.none` | `fx.gold-bolt` |
| greed-beacon | `carddef.greed-beacon` | `combat.greed-beacon` | `present.greed-beacon` | `spawn.none` | `fx.aura-ring` |
| greed-midas-touch | `carddef.greed-midas-touch` | `combat.greed-midas-touch` | `present.greed-midas-touch` | `spawn.none` | `fx.midas-convert` |
| greed-mortgage | `carddef.greed-mortgage` | `combat.greed-mortgage` | `present.greed-mortgage` | `spawn.none` | `fx.contract-siphon` |

### 3.9 Greed 王国战斗显示规则总结
- Base 与财富建筑必须强可见，不可退化成 UI 图标。
- `Thief` 与 `Mercenary` 必须以真实战斗单位进场，而不是文字结算。
- `Dispenser` 与 `Beacon` 是典型的“驻场可见、战斗效果可读”的建筑。
- 金币、掠夺、财富转化是战斗中的一等反馈，不是战后文字描述。

## 4. King of Nothing
### 4.1 王国战斗定位
`King of Nothing` 更接近朴素王国与基础军团：兵种更直观、建筑职责更清晰、战斗观感偏“防线 + 步射协同 + 简洁成长”。它是第二王国，但必须能通过与 `Greed` 相同的数据框架驱动。

### 4.2 Card Identity Table
| cardId | displayNameZh | displayNameEn | ownerKingId | cardType | rarity/sourceTag | maxLevel | targetRule | stackRule |
|---|---|---|---|---|---|---|---|---|
| nothing-castle | 城堡 | Castle | king-nothing | Base | starter-core | 3 | place-on-empty-plot | none |
| nothing-soldier | 士兵 | Soldier | king-nothing | Troop | core-troop | 3 | place-on-empty-plot | banner-stack |
| nothing-archer | 弓箭手 | Archer | king-nothing | Troop | core-ranged | 3 | place-on-empty-plot | small-squad |
| nothing-paladin | 圣骑士 | Paladin | king-nothing | Troop | elite-frontline | 3 | place-on-empty-plot | micro-squad |
| nothing-scout-tower | 哨戒塔 | Scout Tower | king-nothing | Tower | core-defense | 3 | place-on-empty-plot | none |
| nothing-blacksmith | 铁匠铺 | Blacksmith | king-nothing | Building | upgrade-support | 3 | place-on-empty-plot | none |
| nothing-farm | 农场 | Farm | king-nothing | Building | economy-growth | 3 | place-on-empty-plot | none |
| nothing-steel-coat | 钢铁外衣 | Steel Coat | king-nothing | Enchantment | defense-enchant | 3 | attach-to-owned-plot | none |
| nothing-wildcard | 万用牌 | Wildcard | king-nothing | Tome | utility-tome | 1 | choose-valid-target | none |

### 4.3 Board Presence Table
| cardId | presenceType | occupancyFootprint | placementAnchor | blocksPlacement | renderLayer |
|---|---|---|---|---|---|
| nothing-castle | structure | 1x1 | center | true | placed-structures/base |
| nothing-soldier | troop-source | 1x1 | center | true | board-ground/troop-slot |
| nothing-archer | troop-source | 1x1 | center | true | board-ground/troop-slot |
| nothing-paladin | troop-source | 1x1 | center | true | board-ground/troop-slot |
| nothing-scout-tower | structure | 1x1 | center | true | placed-structures/tower |
| nothing-blacksmith | structure | 1x1 | center | true | placed-structures/building |
| nothing-farm | structure | 1x1 | center | true | placed-structures/building |
| nothing-steel-coat | enchantment-anchor | 0x0 | overlay-on-plot | false | board-cells/enchantment |
| nothing-wildcard | none | 0x0 | no-persistent-anchor | false | overlay-modals/instant |

### 4.4 Combat Spawn Table
| cardId | spawnsUnits | unitArchetypeId | spawnPatternId | unitCountBase | deployDelay | spawnCooldown | reinforceRule |
|---|---|---|---|---|---|---|---|
| nothing-castle | false | none | none | 0 | 0.0 | 0.0 | none |
| nothing-soldier | true | nothing-soldier-line | spawn-front-arc-wide | 3 | 0.25 | 0.0 | none |
| nothing-archer | true | nothing-archer-line | spawn-backline-arc | 2 | 0.35 | 0.0 | none |
| nothing-paladin | true | nothing-paladin-elite | spawn-front-center-heavy | 1 | 0.20 | 0.0 | none |
| nothing-scout-tower | false | none | none | 0 | 0.0 | 1.9 | periodic-shot |
| nothing-blacksmith | false | none | none | 0 | 0.0 | 0.0 | aura-only |
| nothing-farm | false | none | none | 0 | 0.0 | 0.0 | economy-only |
| nothing-steel-coat | false | none | none | 0 | 0.0 | 0.0 | on-owner-hit-buffer |
| nothing-wildcard | false | none | none | 0 | 0.0 | 0.0 | instant-special |

### 4.5 Level Stats Table
| cardId | maxHp L1/L2/L3 | armor L1/L2/L3 | shield L1/L2/L3 | attackDamage L1/L2/L3 | attackInterval L1/L2/L3 | attackRange L1/L2/L3 | moveSpeed L1/L2/L3 | projectileSpeed L1/L2/L3 | splashRadius L1/L2/L3 | unitCount L1/L2/L3 | goldYield L1/L2/L3 | auraRadius L1/L2/L3 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| nothing-castle | 200 / 255 / 320 | 5 / 7 / 9 | 12 / 18 / 26 | 18 / 24 / 31 | 2.5 / 2.3 / 2.1 | 4.5 / 4.8 / 5.0 | 0 / 0 / 0 | 14 / 15 / 16 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |
| nothing-soldier | 42 / 58 / 78 | 1 / 2 / 3 | 0 / 0 / 0 | 10 / 14 / 19 | 1.05 / 1.00 / 0.95 | 1.0 / 1.0 / 1.0 | 1.55 / 1.65 / 1.75 | 0 / 0 / 0 | 0 / 0 / 0 | 3 / 4 / 5 | 0 / 0 / 0 | 0 / 0 / 0 |
| nothing-archer | 28 / 40 / 55 | 0 / 1 / 1 | 0 / 0 / 0 | 8 / 12 / 16 | 1.15 / 1.05 / 0.95 | 4.8 / 5.2 / 5.6 | 1.35 / 1.40 / 1.45 | 13 / 14 / 15 | 0 / 0 / 0 | 2 / 3 / 4 | 0 / 0 / 0 | 0 / 0 / 0 |
| nothing-paladin | 70 / 96 / 128 | 3 / 4 / 6 | 6 / 10 / 14 | 16 / 22 / 30 | 1.20 / 1.10 / 1.00 | 1.0 / 1.0 / 1.0 | 1.35 / 1.45 / 1.55 | 0 / 0 / 0 | 0 / 0 / 0 | 1 / 2 / 2 | 0 / 0 / 0 | 0 / 0 / 0 |
| nothing-scout-tower | 90 / 125 / 170 | 1 / 2 / 3 | 0 / 0 / 0 | 9 / 13 / 18 | 1.85 / 1.75 / 1.60 | 4.6 / 5.1 / 5.5 | 0 / 0 / 0 | 13 / 14 / 15 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |
| nothing-blacksmith | 95 / 130 / 175 | 1 / 2 / 3 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 1.5 / 2.0 / 2.5 |
| nothing-farm | 85 / 115 / 150 | 0 / 1 / 2 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 3 / 5 / 7 | 0 / 0 / 0 |
| nothing-steel-coat | 0 / 0 / 0 | 2 / 4 / 6 | 4 / 8 / 12 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |
| nothing-wildcard | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |

### 4.6 Battle Behavior Table
| cardId | combatRole | damageType | targetPriority | engageRule | retargetRule | deathRule | survivorPersistence |
|---|---|---|---|---|---|---|---|
| nothing-castle | base | beam | nearest-enemy | stationary-fire | periodic-scan | dissolve | keep-structure |
| nothing-soldier | melee-swarm | physical | nearest-enemy | chase-nearest | on-target-death | corpse-fade | clear-after-battle |
| nothing-archer | ranged-line | pierce | nearest-enemy | hold-lane | on-range-break | corpse-fade | clear-after-battle |
| nothing-paladin | melee-bruiser | physical | highest-hp | chase-nearest | on-target-death | corpse-fade | clear-after-battle |
| nothing-scout-tower | tower | pierce | nearest-enemy | stationary-fire | periodic-scan | dissolve | keep-structure |
| nothing-blacksmith | support | magic | none | aura-only | never | dissolve | keep-structure |
| nothing-farm | eco-structure | economic | none | aura-only | never | dissolve | keep-structure |
| nothing-steel-coat | enchantment | magic | none | aura-only | never | instant-remove | keep-enchantment |
| nothing-wildcard | tome-burst | magic | marked-target | instant-resolve | never | instant-remove | clear-after-battle |

### 4.7 Presentation & FX Table
| cardId | worldObjectType | unitVisualType | stackDisplayRule | weaponFxId | hitFxId | deathFxId | lootFxId | audioCueGroup |
|---|---|---|---|---|---|---|---|---|
| nothing-castle | castle-stone | none | no-stack | fx-beam-castle | fx-hit-stone-burst | fx-death-structure-collapse | fx-none | sfx-nothing-castle |
| nothing-soldier | none | soldier-blue-set | small-squad | fx-melee-sword-basic | fx-hit-metal-clang | fx-death-dust-small | fx-none | sfx-nothing-soldier |
| nothing-archer | none | archer-blue-set | small-squad | fx-arrow-basic | fx-hit-arrow-stick | fx-death-dust-small | fx-none | sfx-nothing-archer |
| nothing-paladin | none | paladin-blue-set | micro-squad | fx-melee-heavy-strike | fx-hit-heavy-armor | fx-death-dust-heavy | fx-none | sfx-nothing-paladin |
| nothing-scout-tower | tower-wood-stone | none | no-stack | fx-bolt-watchtower | fx-hit-wood-burst | fx-death-wood-break | fx-none | sfx-nothing-tower |
| nothing-blacksmith | blacksmith-forge | none | no-stack | fx-aura-forge-heat | fx-hit-buff-spark | fx-death-wood-break | fx-none | sfx-nothing-blacksmith |
| nothing-farm | farm-crops | none | no-stack | fx-none | fx-hit-dust-small | fx-death-wood-break | fx-loot-harvest | sfx-nothing-farm |
| nothing-steel-coat | enchant-overlay-steel | none | no-stack | fx-enchant-armor-shell | fx-hit-shield-shimmer | fx-none | fx-none | sfx-nothing-steelcoat |
| nothing-wildcard | tome-wildcard-scroll | none | no-stack | fx-wildcard-burst | fx-hit-magic-pop | fx-none | fx-none | sfx-nothing-wildcard |

### 4.8 Data Mapping Table
| cardId | CardDefinition | CardCombatConfig | CardPresentationConfig | SpawnPatternSpec | WeaponFXSpec |
|---|---|---|---|---|---|
| nothing-castle | `carddef.nothing-castle` | `combat.nothing-castle` | `present.nothing-castle` | `spawn.none` | `fx.beam-castle` |
| nothing-soldier | `carddef.nothing-soldier` | `combat.nothing-soldier` | `present.nothing-soldier` | `spawn.front-arc-wide` | `fx.melee-basic` |
| nothing-archer | `carddef.nothing-archer` | `combat.nothing-archer` | `present.nothing-archer` | `spawn.backline-arc` | `fx.arrow-basic` |
| nothing-paladin | `carddef.nothing-paladin` | `combat.nothing-paladin` | `present.nothing-paladin` | `spawn.front-center-heavy` | `fx.melee-heavy` |
| nothing-scout-tower | `carddef.nothing-scout-tower` | `combat.nothing-scout-tower` | `present.nothing-scout-tower` | `spawn.none` | `fx.bolt-watchtower` |
| nothing-blacksmith | `carddef.nothing-blacksmith` | `combat.nothing-blacksmith` | `present.nothing-blacksmith` | `spawn.none` | `fx.aura-forge` |
| nothing-farm | `carddef.nothing-farm` | `combat.nothing-farm` | `present.nothing-farm` | `spawn.none` | `fx.none` |
| nothing-steel-coat | `carddef.nothing-steel-coat` | `combat.nothing-steel-coat` | `present.nothing-steel-coat` | `spawn.none` | `fx.enchant-armor` |
| nothing-wildcard | `carddef.nothing-wildcard` | `combat.nothing-wildcard` | `present.nothing-wildcard` | `spawn.none` | `fx.wildcard-burst` |

### 4.9 Nothing 王国战斗显示规则总结
- `Soldier`、`Archer`、`Paladin` 是最标准的兵力事实落地对象，必须在格子与同景战斗中真实生成。
- `Scout Tower`、`Farm`、`Blacksmith` 是最标准的驻场建筑表现对象。
- `Steel Coat` 这种强化牌不能只写在数值面板里，必须有附着高亮与受击反馈。

## 5. 通用敌军 Unit Archetype Table
| unitArchetypeId | displayNameZh | combatRole | maxHp | armor | shield | attackDamage | attackInterval | attackRange | moveSpeed | projectileSpeed | splashRadius | targetPriority | engageRule | stackDisplayRule | worldVisualRule |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|---|
| enemy-melee | 敌方近战 | melee-swarm | 24 | 1 | 0 | 8 | 0.95 | 1.0 | 1.45 | 0 | 0 | nearest-enemy | chase-nearest | small-squad | 单体或 3 人小队 |
| enemy-ranged | 敌方远程 | ranged-line | 16 | 0 | 0 | 7 | 1.10 | 4.4 | 1.15 | 12 | 0 | nearest-enemy | hold-lane | small-squad | 后排射手小队 |
| enemy-dasher | 敌方突进 | assassin | 18 | 0 | 0 | 11 | 0.90 | 1.0 | 2.15 | 0 | 0 | lowest-hp | flank-dash | micro-squad | 小体量高速单位 |
| enemy-elite | 敌方精英 | melee-bruiser | 58 | 3 | 4 | 18 | 1.25 | 1.2 | 1.35 | 0 | 0 | highest-hp | chase-nearest | micro-squad | 单体精英或双体 |
| enemy-boss | 敌方 Boss | melee-bruiser | 180 | 6 | 10 | 28 | 1.45 | 1.4 | 1.10 | 0 | 0.8 | structure-first | chase-nearest | single | 单体大型单位 |

## 6. 半通用效果库建议
后续实现阶段优先做成可配置效果库，而不是每张卡单独写死逻辑：
- `gain-gold-on-year-end`
- `gain-gold-on-kill`
- `spawn-unit-squad`
- `periodic-projectile-shot`
- `emit-aura-buff`
- `beam-execute`
- `apply-armor-buff`
- `convert-kill-to-gold`
- `temporary-invest-bonus`
- `instant-gold-conversion`

## 7. 审阅结论标准
文档审阅通过时，应能直接回答：
- 两个国家每张卡的身份、落位、是否出兵、是否驻场、是否附魔
- L1/L2/L3 的核心数值如何增长
- 武器和特技如何表现
- 数量如何显示，哪些走单体、哪些走编队、哪些走角标
- 后续工程实现中，这些卡将各自绑定到哪些配置对象

### 5.4 精英敌军
- 更高体量
- 更强轮廓
- 允许带特殊地面圈或 aura

### 5.5 Boss / Champion
- 必须是战场中的显著实体
- 不可只用大数字表示
- 可带独立特效或专属标题反馈

## 6. 数量显示统一规则
- 近战 troop：优先用单体 + 编队残影
- 远程 troop：优先用稀疏编队
- 雇佣军或盗贼群：可用更密集队形
- 精英与 Boss：始终以单个显著实体为主

## 7. 中英双语显示规范
每张卡在审阅包中都必须兼容：
- 英文名：用于对齐原作
- 中文名：用于后续本地化
- 名称长度必须提前考虑在卡牌头部和战利品页中

## 8. 审阅结论标准
通过标准：
- 你能直接看出每张卡是建筑、部队还是效果
- 你能判断每张卡是否应该在战场里有实体
- 你能看出 Greed 与 Nothing 两个王国在战斗观感上的区别
- 后续实现者无需再猜“这张牌到底该不该出兵、该怎么显示”
