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
        internal readonly struct EnemyWaveProfile
        {
            public EnemyWaveProfile(int groupCount, int maxRangedGroups, bool allowDasher, float healthMultiplier, float damageMultiplier, int? fixedStackCount, int maxStackCount)
            {
                GroupCount = groupCount;
                MaxRangedGroups = maxRangedGroups;
                AllowDasher = allowDasher;
                HealthMultiplier = healthMultiplier;
                DamageMultiplier = damageMultiplier;
                FixedStackCount = fixedStackCount;
                MaxStackCount = maxStackCount;
            }

            public int GroupCount { get; }
            public int MaxRangedGroups { get; }
            public bool AllowDasher { get; }
            public float HealthMultiplier { get; }
            public float DamageMultiplier { get; }
            public int? FixedStackCount { get; }
            public int MaxStackCount { get; }
        }

        private const float PlayerBaseX = -5.10f;
        private const float PlayerBaseY = 2.72f;
        private const float EnemyEntryX = 7.10f;
        private const float EnemyEntryY = -3.36f;
        private const float DeployMoveSpeedMultiplier = 4.0f;
        private const float MinimumDeployMoveSpeed = 2.40f;

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

            var occupiedPlots = runState.GetUnlockedPlots()
                .Where(plot => !plot.IsEmpty)
                .OrderBy(plot => plot.coord.y)
                .ThenBy(plot => plot.coord.x)
                .ToList();

            var friendlyEntries = occupiedPlots
                .Select(plot => new
                {
                    Plot = plot,
                    Combat = _database.GetCombatConfig(plot.cardId),
                })
                .Where(entry => entry.Combat != null && entry.Combat.spawnsUnits && !string.IsNullOrEmpty(entry.Combat.unitArchetypeId))
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

            var waveProfile = ResolveEnemyWaveProfile(runState.year, runState.currentEnemyKingId, isFinalBattle);
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
                entity.worldX = Mathf.MoveTowards(entity.worldX, targetX, step);
                entity.worldY = Mathf.MoveTowards(entity.worldY, targetY, step);
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
                var target = enemies.OrderBy(enemy => Distance(entity, enemy)).FirstOrDefault();
                if (target == null)
                {
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
                var target = friendlies.OrderBy(friendly => Distance(entity, friendly)).FirstOrDefault();
                if (target == null)
                {
                    entity.worldX = Mathf.MoveTowards(entity.worldX, PlayerBaseX, entity.moveSpeed * _tickConfig.fixedDeltaTime);
                    entity.worldY = Mathf.MoveTowards(entity.worldY, PlayerBaseY, entity.moveSpeed * 0.72f * _tickConfig.fixedDeltaTime);
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

            if (state.entities.Any(entity => entity.isEnemy && !entity.isDead && entity.worldX <= PlayerBaseX - 0.25f))
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
            else if (friendlies.All(friendly => friendly.isDead))
            {
                state.isResolved = true;
                state.playerWon = false;
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

        private static float Distance(BattleEntityState a, BattleEntityState b)
        {
            var dx = a.worldX - b.worldX;
            var dy = a.worldY - b.worldY;
            return Mathf.Sqrt(dx * dx + dy * dy);
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
            var columns = 2;
            var xBase = isEnemy
                ? (ranged ? 3.48f : 2.34f)
                : (ranged ? -3.34f : -2.18f);
            var xStep = isEnemy ? 0.30f : -0.30f;
            var yBase = isEnemy ? -1.96f : 1.96f;
            var yStep = isEnemy ? 0.46f : -0.46f;

            for (var i = 0; i < count; i++)
            {
                var column = i % columns;
                var row = i / columns;
                slots.Add(new Vector2(xBase + column * xStep, yBase + row * yStep));
            }

            return slots;
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

        internal static EnemyWaveProfile ResolveEnemyWaveProfile(int year, string enemyKingId, bool isFinalBattle)
        {
            if (isFinalBattle)
            {
                return new EnemyWaveProfile(6, 3, true, 1f, 1f, null, int.MaxValue);
            }

            return year switch
            {
                <= 1 => new EnemyWaveProfile(2, 1, false, 0.70f, 0.70f, 1, 1),
                <= 2 => new EnemyWaveProfile(2, 1, false, 0.80f, 0.80f, 1, 1),
                <= 3 => new EnemyWaveProfile(3, 1, false, 0.90f, 0.90f, null, 2),
                <= 5 => new EnemyWaveProfile(4, 2, true, 0.95f, 0.95f, null, 2),
                <= 7 => new EnemyWaveProfile(5, 2, true, 1f, 1f, null, 3),
                _ => new EnemyWaveProfile(6, 3, true, 1f, 1f, null, int.MaxValue),
            };
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

            return Math.Max(1, Math.Min(baseUnitCount, profile.MaxStackCount));
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
