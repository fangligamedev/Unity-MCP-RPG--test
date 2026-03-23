#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NineKingsPrototype.V2
{
    [Serializable]
    public sealed class CombatTickConfig
    {
        public float fixedDeltaTime = 0.1f;
    }

    public sealed class CombatSimulation
    {
        internal readonly struct FormationDebugAxis
        {
            public FormationDebugAxis(Vector2 start, Vector2 end, Vector2 friendlyCenter, Vector2 forward, Vector2 lateral)
            {
                Start = start;
                End = end;
                FriendlyCenter = friendlyCenter;
                Forward = forward;
                Lateral = lateral;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public Vector2 FriendlyCenter { get; }
            public Vector2 Forward { get; }
            public Vector2 Lateral { get; }
        }

        internal readonly struct EnemyWaveProfile
        {
            public EnemyWaveProfile(int groupCount, int maxRangedGroups, bool allowDasher, float healthMultiplier, float damageMultiplier, int? fixedStackCount, int maxStackCount, float stackMultiplier)
            {
                GroupCount = groupCount;
                MaxRangedGroups = maxRangedGroups;
                AllowDasher = allowDasher;
                HealthMultiplier = healthMultiplier;
                DamageMultiplier = damageMultiplier;
                FixedStackCount = fixedStackCount;
                MaxStackCount = maxStackCount;
                StackMultiplier = stackMultiplier;
            }

            public int GroupCount { get; }
            public int MaxRangedGroups { get; }
            public bool AllowDasher { get; }
            public float HealthMultiplier { get; }
            public float DamageMultiplier { get; }
            public int? FixedStackCount { get; }
            public int MaxStackCount { get; }
            public float StackMultiplier { get; }
        }

        private const float DefaultPlayerBaseX = -5.10f;
        private const float DefaultPlayerBaseY = 2.72f;
        private const int MinimumBaseBattleHp = 40;
        private const int BaseBattleHpScale = 6;
        private const float EnemyEntryX = 7.10f;
        private const float EnemyEntryY = -3.36f;
        private const float DeployMoveSpeedMultiplier = 4.0f;
        private const float MinimumDeployMoveSpeed = 2.40f;
        private const float SiegeMoveSpeedMultiplier = 3.6f;
        private const float MinimumSiegeMoveSpeed = 2.4f;
        private const float BaseContactDistance = 0.34f;

        private readonly ContentDatabase _database;
        private readonly CombatTickConfig _tickConfig;
        private float _accumulator;

        public CombatSimulation(ContentDatabase database, CombatTickConfig? tickConfig = null)
        {
            _database = database;
            _tickConfig = tickConfig ?? new CombatTickConfig();
        }

        public BattleSceneState CreateBattleScene(RunState runState, bool isFinalBattle = false)
        {
            var battle = new BattleSceneState
            {
                year = runState.year,
                enemyKingId = runState.currentEnemyKingId,
                isFinalBattle = isFinalBattle,
            };

            InitializePlayerBaseBattleState(runState, battle);

            var occupiedPlots = runState.GetUnlockedPlots()
                .Where(plot => !plot.IsEmpty)
                .OrderBy(plot => plot.coord.y)
                .ThenBy(plot => plot.coord.x)
                .ToList();

            var occupiedEntries = occupiedPlots
                .Select(plot => new
                {
                    Plot = plot,
                    Combat = _database.GetCombatConfig(plot.cardId),
                    Presentation = _database.GetPresentationConfig(plot.cardId),
                })
                .Where(entry => entry.Combat != null)
                .ToList();

            var friendlyEntries = occupiedEntries
                .Where(entry => entry.Combat!.spawnsUnits && !string.IsNullOrEmpty(entry.Combat.unitArchetypeId))
                .OrderBy(entry => GetFormationRolePriority(entry.Combat!.combatRole))
                .ThenBy(entry => entry.Plot.coord.y)
                .ThenBy(entry => entry.Plot.coord.x)
                .ToList();

            var friendlyMelee = friendlyEntries.Where(entry => entry.Combat!.combatRole != CombatRole.Ranged).ToList();
            var friendlyRanged = friendlyEntries.Where(entry => entry.Combat!.combatRole == CombatRole.Ranged).ToList();
            var friendlyMeleeSlots = ResolveFormationSlots(false, CombatRole.Melee, friendlyMelee.Count);
            var friendlyRangedSlots = ResolveFormationSlots(false, CombatRole.Ranged, friendlyRanged.Count);

            for (var i = 0; i < friendlyMelee.Count; i++)
            {
                var plot = friendlyMelee[i].Plot;
                var combat = friendlyMelee[i].Combat!;
                var level = combat.levels.FirstOrDefault(item => item.level == Math.Max(1, plot.level)) ?? combat.levels.Last();
                var runtimeStats = NineKingsV2GameController.ResolvePlotRuntimeStats(_database, runState, plot);
                var formationPosition = friendlyMeleeSlots[i];
                var deployStart = NineKingsV2ScenePresenter.ResolveMapUnitDisplayAnchor(plot.coord, runtimeStats.EffectiveUnitCount, false);
                battle.entities.Add(new BattleEntityState
                {
                    entityId = $"friendly-{plot.coord.x}-{plot.coord.y}",
                    sourceCardId = plot.cardId,
                    unitArchetypeId = combat.unitArchetypeId,
                    isEnemy = false,
                    level = Math.Max(1, plot.level),
                    maxHp = level.maxHp,
                    currentHp = level.maxHp,
                    attackDamage = level.attackDamage,
                    attackInterval = Math.Max(0.35f, level.attackInterval),
                    attackRange = Math.Max(0.85f, level.attackRange),
                    moveSpeed = Math.Max(0.50f, level.moveSpeed),
                    stackCount = Math.Max(1, runtimeStats.EffectiveUnitCount),
                    sourceCoord = plot.coord,
                    deployStartX = deployStart.x,
                    deployStartY = deployStart.y,
                    deployTargetX = formationPosition.x,
                    deployTargetY = formationPosition.y,
                    worldX = deployStart.x,
                    worldY = deployStart.y,
                });
            }

            for (var i = 0; i < friendlyRanged.Count; i++)
            {
                var plot = friendlyRanged[i].Plot;
                var combat = friendlyRanged[i].Combat!;
                var level = combat.levels.FirstOrDefault(item => item.level == Math.Max(1, plot.level)) ?? combat.levels.Last();
                var runtimeStats = NineKingsV2GameController.ResolvePlotRuntimeStats(_database, runState, plot);
                var formationPosition = friendlyRangedSlots[i];
                var deployStart = NineKingsV2ScenePresenter.ResolveMapUnitDisplayAnchor(plot.coord, runtimeStats.EffectiveUnitCount, true);
                battle.entities.Add(new BattleEntityState
                {
                    entityId = $"friendly-{plot.coord.x}-{plot.coord.y}",
                    sourceCardId = plot.cardId,
                    unitArchetypeId = combat.unitArchetypeId,
                    isEnemy = false,
                    level = Math.Max(1, plot.level),
                    maxHp = level.maxHp,
                    currentHp = level.maxHp,
                    attackDamage = level.attackDamage,
                    attackInterval = Math.Max(0.35f, level.attackInterval),
                    attackRange = Math.Max(0.85f, level.attackRange),
                    moveSpeed = Math.Max(0.50f, level.moveSpeed),
                    stackCount = Math.Max(1, runtimeStats.EffectiveUnitCount),
                    sourceCoord = plot.coord,
                    deployStartX = deployStart.x,
                    deployStartY = deployStart.y,
                    deployTargetX = formationPosition.x,
                    deployTargetY = formationPosition.y,
                    worldX = deployStart.x,
                    worldY = deployStart.y,
                });
            }

            var friendlyStaticAttackers = occupiedEntries
                .Where(entry =>
                {
                    var combat = entry.Combat!;
                    if (combat.spawnsUnits || combat.presenceType != PresenceType.Structure)
                    {
                        return false;
                    }

                    var level = combat.levels.FirstOrDefault(item => item.level == Math.Max(1, entry.Plot.level)) ?? combat.levels.LastOrDefault();
                    return level != null && level.attackDamage > 0 && level.attackRange > 0f;
                })
                .OrderBy(entry => entry.Plot.coord.y)
                .ThenBy(entry => entry.Plot.coord.x)
                .ToList();

            foreach (var entry in friendlyStaticAttackers)
            {
                var plot = entry.Plot;
                var combat = entry.Combat!;
                var level = combat.levels.FirstOrDefault(item => item.level == Math.Max(1, plot.level)) ?? combat.levels.Last();
                var worldObjectType = entry.Presentation?.worldObjectType ?? WorldObjectType.Tower;
                var structureAnchor = NineKingsV2ScenePresenter.ResolveBattleStructureAnchor(plot.cardId, worldObjectType, plot.coord).Position;
                battle.entities.Add(new BattleEntityState
                {
                    entityId = $"friendly-structure-{plot.coord.x}-{plot.coord.y}",
                    sourceCardId = plot.cardId,
                    unitArchetypeId = string.IsNullOrEmpty(combat.unitArchetypeId) ? plot.cardId : combat.unitArchetypeId,
                    isEnemy = false,
                    level = Math.Max(1, plot.level),
                    maxHp = level.maxHp,
                    currentHp = level.maxHp,
                    attackDamage = level.attackDamage,
                    attackInterval = Math.Max(0.35f, level.attackInterval),
                    attackRange = Math.Max(1.6f, level.attackRange),
                    moveSpeed = 0f,
                    stackCount = 1,
                    sourceCoord = plot.coord,
                    deployStartX = structureAnchor.x,
                    deployStartY = structureAnchor.y,
                    deployTargetX = structureAnchor.x,
                    deployTargetY = structureAnchor.y,
                    worldX = structureAnchor.x,
                    worldY = structureAnchor.y,
                });
            }

            var waveProfile = ResolveEnemyWaveProfile(runState.year, runState.currentEnemyKingId, isFinalBattle, _database.battleCurve);
            var enemyArchetypeIds = BuildEnemyWaveArchetypes(runState.currentEnemyKingId, runState.year, isFinalBattle);
            var enemyEntries = new List<(string ArchetypeId, UnitArchetypeDefinition Archetype)>();
            for (var i = 0; i < enemyArchetypeIds.Count; i++)
            {
                var archetype = _database.unitArchetypes.FirstOrDefault(item => string.Equals(item.unitArchetypeId, enemyArchetypeIds[i], StringComparison.Ordinal));
                if (archetype == null)
                {
                    continue;
                }

                enemyEntries.Add((enemyArchetypeIds[i], archetype));
            }

            enemyEntries = enemyEntries
                .OrderBy(entry => GetFormationRolePriority(entry.Archetype.combatRole))
                .ThenBy(entry => entry.ArchetypeId, StringComparer.Ordinal)
                .ToList();

            var enemyMelee = enemyEntries.Where(entry => entry.Archetype.combatRole != CombatRole.Ranged).ToList();
            var enemyRanged = enemyEntries.Where(entry => entry.Archetype.combatRole == CombatRole.Ranged).ToList();
            var enemyMeleeSlots = ResolveFormationSlots(true, CombatRole.Melee, enemyMelee.Count);
            var enemyRangedSlots = ResolveFormationSlots(true, CombatRole.Ranged, enemyRanged.Count);

            for (var i = 0; i < enemyMelee.Count; i++)
            {
                var archetype = enemyMelee[i].Archetype;
                var level = archetype.levels.FirstOrDefault() ?? new LevelStatBlock
                {
                    maxHp = 8,
                    attackDamage = 2,
                    attackInterval = 1f,
                    attackRange = 1f,
                    moveSpeed = 0.9f,
                    unitCount = 1,
                };
                var wavePosition = enemyMeleeSlots[i];
                battle.entities.Add(new BattleEntityState
                {
                    entityId = $"enemy-melee-{i}",
                    sourceCardId = archetype.unitArchetypeId,
                    unitArchetypeId = archetype.unitArchetypeId,
                    isEnemy = true,
                    level = 1,
                    maxHp = ResolveEnemyMaxHp(level.maxHp, waveProfile),
                    currentHp = ResolveEnemyMaxHp(level.maxHp, waveProfile),
                    attackDamage = ResolveEnemyDamage(level.attackDamage, waveProfile),
                    attackInterval = Math.Max(0.45f, level.attackInterval),
                    attackRange = Math.Max(0.85f, level.attackRange),
                    moveSpeed = Math.Max(0.50f, level.moveSpeed),
                    stackCount = ResolveEnemyStackCount(level.unitCount, waveProfile),
                    sourceCoord = new BoardCoord(5, i % 5),
                    deployStartX = EnemyEntryX,
                    deployStartY = EnemyEntryY,
                    deployTargetX = wavePosition.x,
                    deployTargetY = wavePosition.y,
                    worldX = EnemyEntryX,
                    worldY = EnemyEntryY,
                });
            }

            for (var i = 0; i < enemyRanged.Count; i++)
            {
                var archetype = enemyRanged[i].Archetype;
                var level = archetype.levels.FirstOrDefault() ?? new LevelStatBlock
                {
                    maxHp = 8,
                    attackDamage = 2,
                    attackInterval = 1f,
                    attackRange = 1f,
                    moveSpeed = 0.9f,
                    unitCount = 1,
                };
                var wavePosition = enemyRangedSlots[i];
                battle.entities.Add(new BattleEntityState
                {
                    entityId = $"enemy-ranged-{i}",
                    sourceCardId = archetype.unitArchetypeId,
                    unitArchetypeId = archetype.unitArchetypeId,
                    isEnemy = true,
                    level = 1,
                    maxHp = ResolveEnemyMaxHp(level.maxHp, waveProfile),
                    currentHp = ResolveEnemyMaxHp(level.maxHp, waveProfile),
                    attackDamage = ResolveEnemyDamage(level.attackDamage, waveProfile),
                    attackInterval = Math.Max(0.45f, level.attackInterval),
                    attackRange = Math.Max(0.85f, level.attackRange),
                    moveSpeed = Math.Max(0.50f, level.moveSpeed),
                    stackCount = ResolveEnemyStackCount(level.unitCount, waveProfile),
                    sourceCoord = new BoardCoord(5, i % 5),
                    deployStartX = EnemyEntryX,
                    deployStartY = EnemyEntryY,
                    deployTargetX = wavePosition.x,
                    deployTargetY = wavePosition.y,
                    worldX = EnemyEntryX,
                    worldY = EnemyEntryY,
                });
            }

            return battle;
        }

        public bool AdvanceDeployFormation(BattleSceneState state, float deltaTime)
        {
            var allArrived = true;
            for (var i = 0; i < state.entities.Count; i++)
            {
                var entity = state.entities[i];
                var targetX = entity.deployTargetX;
                var targetY = entity.deployTargetY;
                var current = new Vector2(entity.worldX, entity.worldY);
                var target = new Vector2(targetX, targetY);
                var distance = Vector2.Distance(current, target);
                if (distance <= 0.02f)
                {
                    entity.worldX = targetX;
                    entity.worldY = targetY;
                    continue;
                }

                allArrived = false;
                var moveSpeed = Math.Max(MinimumDeployMoveSpeed, entity.moveSpeed * DeployMoveSpeedMultiplier);
                var step = moveSpeed * deltaTime;
                var moved = Vector2.MoveTowards(current, target, step);
                entity.worldX = moved.x;
                entity.worldY = moved.y;
            }

            return allArrived;
        }

        public void Advance(BattleSceneState state, float deltaTime)
        {
            if (state.isResolved)
            {
                return;
            }

            _accumulator += deltaTime;
            while (_accumulator >= _tickConfig.fixedDeltaTime)
            {
                _accumulator -= _tickConfig.fixedDeltaTime;
                Tick(state);
            }
        }

        private void Tick(BattleSceneState state)
        {
            var friendlies = state.entities.Where(entity => !entity.isEnemy && !entity.isDead).ToList();
            var enemies = state.entities.Where(entity => entity.isEnemy && !entity.isDead).ToList();

            foreach (var entity in friendlies)
            {
                entity.timeSinceLastAttack += _tickConfig.fixedDeltaTime;
                var target = state.entities
                    .Where(enemy => enemy.isEnemy && !enemy.isDead)
                    .OrderBy(enemy => Distance(entity, enemy))
                    .FirstOrDefault();
                if (target == null)
                {
                    continue;
                }

                var distance = Distance(entity, target);
                if (entity.attackRange < 1.6f)
                {
                    var engageDistance = ResolveEngageDistance(entity);
                    if (distance > engageDistance && entity.moveSpeed > 0f)
                    {
                        var contactPoint = ResolveMeleeContactPoint(entity, target);
                        entity.worldX = Mathf.MoveTowards(entity.worldX, contactPoint.x, entity.moveSpeed * _tickConfig.fixedDeltaTime);
                        entity.worldY = Mathf.MoveTowards(entity.worldY, contactPoint.y, entity.moveSpeed * 0.82f * _tickConfig.fixedDeltaTime);
                    }
                }
                else
                {
                    if (distance > entity.attackRange * 0.95f && entity.moveSpeed > 0f)
                    {
                        entity.worldX = Mathf.MoveTowards(entity.worldX, target.worldX - 1.55f, entity.moveSpeed * 0.18f * _tickConfig.fixedDeltaTime);
                        entity.worldY = Mathf.MoveTowards(entity.worldY, target.worldY + 0.22f, entity.moveSpeed * 0.26f * _tickConfig.fixedDeltaTime);
                    }
                }

                distance = Distance(entity, target);
                if (distance <= ResolveAttackTriggerDistance(entity) && entity.timeSinceLastAttack >= entity.attackInterval)
                {
                    entity.timeSinceLastAttack = 0f;
                    ApplyDamage(target, entity);
                }
            }

            foreach (var entity in enemies)
            {
                entity.timeSinceLastAttack += _tickConfig.fixedDeltaTime;
                var hasAliveFriendlyTroops = state.entities.Any(friendly => !friendly.isEnemy && !friendly.isDead && IsTroopSourceEntity(friendly));
                if (!hasAliveFriendlyTroops)
                {
                    AdvanceEnemyTowardPlayerBase(entity, state);
                    TryAttackPlayerBase(entity, state);
                    continue;
                }

                var target = state.entities
                    .Where(friendly => !friendly.isEnemy && !friendly.isDead && IsTroopSourceEntity(friendly))
                    .OrderBy(friendly => Distance(entity, friendly))
                    .FirstOrDefault();
                if (target == null)
                {
                    AdvanceEnemyTowardPlayerBase(entity, state);
                    TryAttackPlayerBase(entity, state);
                    continue;
                }

                var distance = Distance(entity, target);
                if (entity.attackRange < 1.6f)
                {
                    var engageDistance = ResolveEngageDistance(entity);
                    if (distance > engageDistance)
                    {
                        var contactPoint = ResolveMeleeContactPoint(entity, target);
                        entity.worldX = Mathf.MoveTowards(entity.worldX, contactPoint.x, entity.moveSpeed * _tickConfig.fixedDeltaTime);
                        entity.worldY = Mathf.MoveTowards(entity.worldY, contactPoint.y, entity.moveSpeed * 0.82f * _tickConfig.fixedDeltaTime);
                    }
                }
                else
                {
                    if (distance > entity.attackRange * 0.95f)
                    {
                        entity.worldX = Mathf.MoveTowards(entity.worldX, target.worldX + 1.45f, entity.moveSpeed * 0.16f * _tickConfig.fixedDeltaTime);
                        entity.worldY = Mathf.MoveTowards(entity.worldY, target.worldY - 0.20f, entity.moveSpeed * 0.24f * _tickConfig.fixedDeltaTime);
                    }
                }

                distance = Distance(entity, target);
                if (distance <= ResolveAttackTriggerDistance(entity) && entity.timeSinceLastAttack >= entity.attackInterval)
                {
                    entity.timeSinceLastAttack = 0f;
                    ApplyDamage(target, entity);
                }
            }

            if (state.playerBaseCurrentHp <= 0)
            {
                state.isResolved = true;
                state.playerWon = false;
                return;
            }

            if (state.entities.Any(entity =>
                    entity.isEnemy &&
                    !entity.isDead &&
                    DistanceToPlayerBase(entity, state) <= BaseContactDistance))
            {
                state.isResolved = true;
                state.playerWon = false;
                return;
            }

            if (enemies.All(enemy => enemy.isDead))
            {
                state.isResolved = true;
                state.playerWon = true;
            }
        }

        private static void ApplyDamage(BattleEntityState target, BattleEntityState attacker)
        {
            var totalDamage = Math.Max(1, attacker.attackDamage + Math.Max(0, attacker.stackCount - 1));
            target.currentHp -= totalDamage;
            if (target.currentHp <= 0)
            {
                target.currentHp = 0;
                target.isDead = true;
            }
        }

        private void InitializePlayerBaseBattleState(RunState runState, BattleSceneState battle)
        {
            var playerKing = _database.GetKing(runState.playerKingId);
            var fallbackCoord = new BoardCoord(2, 2);
            var basePlot = runState.GetUnlockedPlots()
                .FirstOrDefault(plot =>
                    !plot.IsEmpty &&
                    (string.Equals(plot.cardId, playerKing?.baseCardId, StringComparison.Ordinal) ||
                     _database.GetCard(plot.cardId)?.cardType == CardType.Base));

            var baseCardId = basePlot?.cardId ?? playerKing?.baseCardId ?? string.Empty;
            var baseCoord = basePlot?.coord ?? fallbackCoord;
            var presentation = !string.IsNullOrEmpty(baseCardId) ? _database.GetPresentationConfig(baseCardId) : null;
            var worldObjectType = presentation?.worldObjectType ?? WorldObjectType.Palace;
            var anchor = NineKingsV2ScenePresenter.ResolveBattleStructureAnchor(baseCardId, worldObjectType, baseCoord).Position;

            var baseCombat = !string.IsNullOrEmpty(baseCardId) ? _database.GetCombatConfig(baseCardId) : null;
            var baseLevel = baseCombat?.levels
                .FirstOrDefault(level => level.level == Math.Max(1, basePlot?.level ?? 1))
                ?? baseCombat?.levels.LastOrDefault();
            var baseHp = Math.Max(MinimumBaseBattleHp, (baseLevel?.maxHp ?? 10) * BaseBattleHpScale);

            battle.playerBaseCardId = baseCardId;
            battle.playerBaseCoord = baseCoord;
            battle.playerBaseWorldX = anchor.x;
            battle.playerBaseWorldY = anchor.y;
            battle.playerBaseMaxHp = baseHp;
            battle.playerBaseCurrentHp = baseHp;

            if (string.IsNullOrEmpty(baseCardId))
            {
                battle.playerBaseWorldX = DefaultPlayerBaseX;
                battle.playerBaseWorldY = DefaultPlayerBaseY;
            }
        }

        private void AdvanceEnemyTowardPlayerBase(BattleEntityState entity, BattleSceneState state)
        {
            var basePoint = state.playerBaseMaxHp > 0
                ? new Vector2(state.playerBaseWorldX, state.playerBaseWorldY)
                : new Vector2(DefaultPlayerBaseX, DefaultPlayerBaseY);
            var current = new Vector2(entity.worldX, entity.worldY);
            var distance = Vector2.Distance(current, basePoint);
            if (distance <= BaseContactDistance || entity.moveSpeed <= 0f)
            {
                return;
            }

            var moveStep = Mathf.Max(entity.moveSpeed * SiegeMoveSpeedMultiplier, MinimumSiegeMoveSpeed) * _tickConfig.fixedDeltaTime;
            var moved = Vector2.MoveTowards(current, basePoint, moveStep);
            entity.worldX = moved.x;
            entity.worldY = moved.y;
        }

        private static void TryAttackPlayerBase(BattleEntityState enemy, BattleSceneState state)
        {
            if (state.playerBaseCurrentHp <= 0)
            {
                return;
            }

            if (DistanceToPlayerBase(enemy, state) > BaseContactDistance || enemy.timeSinceLastAttack < enemy.attackInterval)
            {
                return;
            }

            enemy.timeSinceLastAttack = 0f;
            var totalDamage = Math.Max(1, enemy.attackDamage + Math.Max(0, enemy.stackCount - 1));
            state.playerBaseCurrentHp = Math.Max(0, state.playerBaseCurrentHp - totalDamage);
        }

        private static float DistanceToPlayerBase(BattleEntityState entity, BattleSceneState state)
        {
            var baseX = state.playerBaseMaxHp > 0 ? state.playerBaseWorldX : DefaultPlayerBaseX;
            var baseY = state.playerBaseMaxHp > 0 ? state.playerBaseWorldY : DefaultPlayerBaseY;
            var dx = entity.worldX - baseX;
            var dy = entity.worldY - baseY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float Distance(BattleEntityState a, BattleEntityState b)
        {
            var dx = a.worldX - b.worldX;
            var dy = a.worldY - b.worldY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private bool IsTroopSourceEntity(BattleEntityState entity)
        {
            var combat = _database.GetCombatConfig(entity.sourceCardId);
            if (combat != null)
            {
                return combat.spawnsUnits;
            }

            return entity.moveSpeed > 0.01f;
        }

        internal static float ResolveEngageDistance(BattleEntityState entity)
        {
            return entity.attackRange < 1.6f
                ? 0.20f
                : Math.Max(1.05f, entity.attackRange * 0.95f);
        }

        internal static float ResolveAttackTriggerDistance(BattleEntityState entity)
        {
            return entity.attackRange < 1.6f
                ? 0.26f
                : entity.attackRange + 0.12f;
        }

        private static Vector2 ResolveMeleeContactPoint(BattleEntityState entity, BattleEntityState target)
        {
            return entity.isEnemy
                ? new Vector2(target.worldX + 0.12f, target.worldY - 0.02f)
                : new Vector2(target.worldX - 0.12f, target.worldY + 0.02f);
        }

        internal static Vector2 GetEnemyDeployEntryPoint()
        {
            return new Vector2(EnemyEntryX, EnemyEntryY);
        }

        internal static IReadOnlyList<Vector2> ResolveFormationSlots(bool isEnemy, CombatRole role, int count)
        {
            var slots = new List<Vector2>(Math.Max(0, count));
            if (count <= 0)
            {
                return slots;
            }

            var ranged = role == CombatRole.Ranged;

            for (var i = 0; i < count; i++)
            {
                var centeredIndex = i - (count - 1) * 0.5f;
                if (isEnemy)
                {
                    var enemyAxis = ResolveFriendlyFormationDebugAxis();
                    var enemyCenter = ResolveEnemyFormationDebugCenter();
                    var enemyLayerOffset = ranged ? 0.22f : -0.22f;
                    var enemyBaseAnchor = enemyCenter + enemyAxis.Forward * enemyLayerOffset;
                    slots.Add(enemyBaseAnchor - enemyAxis.Forward * (centeredIndex * 0.44f));
                    continue;
                }

                var axis = ResolveFriendlyFormationDebugAxis();
                var layerOffset = ranged ? -0.22f : 0.22f;
                var baseAnchor = axis.FriendlyCenter + axis.Forward * layerOffset;
                slots.Add(baseAnchor + axis.Forward * (centeredIndex * 0.44f));
            }

            return slots;
        }

        internal static FormationDebugAxis ResolveFriendlyFormationDebugAxis()
        {
            var start = (Vector2)NineKingsV2ScenePresenter.ResolveMapPlotAnchor(new BoardCoord(2, 2));
            var forward = new Vector2(16f, -9f).normalized;
            var end = start + forward * 9.5f;
            var lateral = new Vector2(forward.y, -forward.x).normalized;
            var friendlyCenter = start + forward * 1.95f;
            return new FormationDebugAxis(start, end, friendlyCenter, forward, lateral);
        }

        internal static Vector2 ResolveEnemyFormationDebugCenter()
        {
            var axis = ResolveFriendlyFormationDebugAxis();
            return axis.Start + axis.Forward * 7.10f;
        }

        private static int GetFormationRolePriority(CombatRole role)
        {
            return role switch
            {
                CombatRole.Melee => 0,
                CombatRole.Ranged => 1,
                _ => 2,
            };
        }

        internal static List<string> BuildEnemyWaveArchetypes(string enemyKingId, int year, bool isFinalBattle)
        {
            var profile = ResolveEnemyWaveProfile(year, enemyKingId, isFinalBattle);
            var fullPattern = new List<string>();
            if (string.Equals(enemyKingId, "king_nature", StringComparison.Ordinal))
            {
                fullPattern.AddRange(new[] { "enemy-ranged", "enemy-melee", "enemy-ranged", "enemy-dasher", "enemy-melee", "enemy-ranged" });
            }
            else
            {
                fullPattern.AddRange(new[] { "enemy-melee", "enemy-ranged", "enemy-melee", "enemy-dasher", "enemy-melee", "enemy-dasher" });
            }

            var result = new List<string>();
            var rangedCount = 0;
            foreach (var archetypeId in fullPattern)
            {
                if (!profile.AllowDasher && string.Equals(archetypeId, "enemy-dasher", StringComparison.Ordinal))
                {
                    continue;
                }

                var ranged = string.Equals(archetypeId, "enemy-ranged", StringComparison.Ordinal);
                if (ranged && rangedCount >= profile.MaxRangedGroups)
                {
                    continue;
                }

                result.Add(archetypeId);
                if (ranged)
                {
                    rangedCount += 1;
                }

                if (result.Count >= profile.GroupCount)
                {
                    break;
                }
            }

            while (result.Count < profile.GroupCount)
            {
                result.Add("enemy-melee");
            }

            if (isFinalBattle)
            {
                result.Add("enemy-elite");
                result.Add("enemy-boss");
            }

            return result;
        }

        internal static EnemyWaveProfile ResolveEnemyWaveProfile(int year, string enemyKingId, bool isFinalBattle, BattleCurveDefinition? battleCurve = null)
        {
            var curve = battleCurve ?? new BattleCurveDefinition();
            if (isFinalBattle)
            {
                return new EnemyWaveProfile(
                    6,
                    3,
                    true,
                    Mathf.Max(1f, curve.finalBattleHealthMultiplier),
                    Mathf.Max(1f, curve.finalBattleAttackMultiplier),
                    null,
                    12,
                    Mathf.Max(1f, curve.unitCountMultiplier * 2.4f));
            }

            var baseProfile = year switch
            {
                <= 1 => new EnemyWaveProfile(2, 1, false, 0.70f, 0.70f, 1, 1, 1f),
                <= 2 => new EnemyWaveProfile(2, 1, false, 0.80f, 0.80f, 1, 1, 1f),
                <= 3 => new EnemyWaveProfile(3, 1, false, 0.90f, 0.90f, null, 2, 1f),
                <= 5 => new EnemyWaveProfile(4, 2, true, 0.95f, 0.95f, null, 2, 1f),
                <= 7 => new EnemyWaveProfile(5, 2, true, 1f, 1f, null, 3, 1f),
                _ => new EnemyWaveProfile(6, 2, true, 1f, 1f, null, 3, 1f),
            };

            if (year <= 7)
            {
                return baseProfile;
            }

            var pressureYears = Mathf.Max(0, year - 7);
            var healthGrowth = Mathf.Pow(Mathf.Max(1.01f, curve.yearlyHealthMultiplier), pressureYears * 1.15f);
            var damageGrowth = Mathf.Pow(Mathf.Max(1.01f, curve.yearlyAttackMultiplier), pressureYears * 1.10f);
            var stackGrowth = Mathf.Pow(Mathf.Max(1.01f, curve.unitCountMultiplier), pressureYears * 4.5f);
            var rangedGroups = pressureYears >= 5 ? 3 : 2;
            var maxStack = Mathf.Clamp(3 + (pressureYears / 3), 3, 10);

            return new EnemyWaveProfile(
                baseProfile.GroupCount,
                rangedGroups,
                true,
                baseProfile.HealthMultiplier * healthGrowth,
                baseProfile.DamageMultiplier * damageGrowth,
                null,
                maxStack,
                stackGrowth);
        }

        internal static int ResolveEnemyWaveCountForYear(int year, bool isFinalBattle)
        {
            return ResolveEnemyWaveProfile(year, string.Empty, isFinalBattle).GroupCount;
        }

        internal static int ResolveEnemyStackCountForYear(int baseUnitCount, int year, bool isFinalBattle)
        {
            return ResolveEnemyStackCount(baseUnitCount, ResolveEnemyWaveProfile(year, string.Empty, isFinalBattle));
        }

        private static int ResolveEnemyMaxHp(int baseHp, EnemyWaveProfile profile)
        {
            return Math.Max(1, Mathf.RoundToInt(baseHp * profile.HealthMultiplier));
        }

        private static int ResolveEnemyDamage(int baseDamage, EnemyWaveProfile profile)
        {
            return Math.Max(1, Mathf.RoundToInt(baseDamage * profile.DamageMultiplier));
        }

        private static int ResolveEnemyStackCount(int baseUnitCount, EnemyWaveProfile profile)
        {
            if (profile.FixedStackCount.HasValue)
            {
                return Math.Max(1, profile.FixedStackCount.Value);
            }

            var scaledUnitCount = Mathf.RoundToInt(Math.Max(1, baseUnitCount) * Mathf.Max(1f, profile.StackMultiplier));
            return Math.Max(1, Math.Min(scaledUnitCount, profile.MaxStackCount));
        }
    }

    public sealed class CombatPresentation
    {
        public BattleSceneState CurrentState { get; private set; } = new();

        public void Bind(BattleSceneState state)
        {
            CurrentState = state;
        }

        public IReadOnlyList<BattleEntityState> GetVisibleEntities()
        {
            return CurrentState.entities.Where(entity => !entity.isDead).ToList();
        }
    }
}
