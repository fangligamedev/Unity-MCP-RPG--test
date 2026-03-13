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
        private const float PlayerBaseX = -4.30f;
        private const float PlayerBaseY = 2.24f;
        private const float EnemyEntryX = 5.05f;
        private const float EnemyEntryY = -2.18f;

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

            var friendlyUnitIndex = 0;
            foreach (var plot in occupiedPlots)
            {
                var combat = _database.GetCombatConfig(plot.cardId);
                if (combat == null || !combat.spawnsUnits || string.IsNullOrEmpty(combat.unitArchetypeId))
                {
                    continue;
                }

                var level = combat.levels.FirstOrDefault(item => item.level == Math.Max(1, plot.level)) ?? combat.levels.Last();
                var formationPosition = GetFriendlyFormationPosition(friendlyUnitIndex, combat.combatRole);
                friendlyUnitIndex++;
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
                    stackCount = Math.Max(1, level.unitCount),
                    sourceCoord = plot.coord,
                    worldX = formationPosition.x,
                    worldY = formationPosition.y,
                });
            }

            var enemyArchetypeIds = BuildEnemyWaveArchetypes(runState.currentEnemyKingId, isFinalBattle);
            for (var i = 0; i < enemyArchetypeIds.Count; i++)
            {
                var archetype = _database.unitArchetypes.FirstOrDefault(item => string.Equals(item.unitArchetypeId, enemyArchetypeIds[i], StringComparison.Ordinal));
                if (archetype == null)
                {
                    continue;
                }

                var level = archetype.levels.FirstOrDefault() ?? new LevelStatBlock
                {
                    maxHp = 8,
                    attackDamage = 2,
                    attackInterval = 1f,
                    attackRange = 1f,
                    moveSpeed = 0.9f,
                    unitCount = 1,
                };
                var wavePosition = GetEnemyFormationPosition(i, archetype.combatRole);
                battle.entities.Add(new BattleEntityState
                {
                    entityId = $"enemy-{i}",
                    sourceCardId = archetype.unitArchetypeId,
                    unitArchetypeId = archetype.unitArchetypeId,
                    isEnemy = true,
                    level = 1,
                    maxHp = level.maxHp,
                    currentHp = level.maxHp,
                    attackDamage = level.attackDamage,
                    attackInterval = Math.Max(0.45f, level.attackInterval),
                    attackRange = Math.Max(0.85f, level.attackRange),
                    moveSpeed = Math.Max(0.50f, level.moveSpeed),
                    stackCount = Math.Max(1, level.unitCount),
                    sourceCoord = new BoardCoord(5, i % 5),
                    worldX = wavePosition.x,
                    worldY = wavePosition.y,
                });
            }

            return battle;
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
                    if (distance > entity.attackRange + 0.08f)
                    {
                        entity.worldX = Mathf.MoveTowards(entity.worldX, target.worldX - 0.42f, entity.moveSpeed * _tickConfig.fixedDeltaTime);
                        entity.worldY = Mathf.MoveTowards(entity.worldY, target.worldY + 0.06f, entity.moveSpeed * 0.70f * _tickConfig.fixedDeltaTime);
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

                if (distance <= entity.attackRange + 0.12f && entity.timeSinceLastAttack >= entity.attackInterval)
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
                    if (distance > entity.attackRange + 0.08f)
                    {
                        entity.worldX = Mathf.MoveTowards(entity.worldX, target.worldX + 0.42f, entity.moveSpeed * _tickConfig.fixedDeltaTime);
                        entity.worldY = Mathf.MoveTowards(entity.worldY, target.worldY - 0.06f, entity.moveSpeed * 0.70f * _tickConfig.fixedDeltaTime);
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

                if (distance <= entity.attackRange + 0.12f && entity.timeSinceLastAttack >= entity.attackInterval)
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

        private static Vector2 GetFriendlyFormationPosition(int index, CombatRole role)
        {
            var lane = index % 3;
            var row = index / 3;
            if (role == CombatRole.Ranged)
            {
                return new Vector2(-4.88f - row * 0.30f, 2.42f - lane * 0.46f);
            }

            return new Vector2(-3.76f - row * 0.28f, 1.90f - lane * 0.50f);
        }

        private static Vector2 GetEnemyFormationPosition(int index, CombatRole role)
        {
            var lane = index % 4;
            var row = index / 4;
            if (role == CombatRole.Ranged)
            {
                return new Vector2(EnemyEntryX + row * 0.18f, EnemyEntryY - 0.24f + lane * 0.42f);
            }

            return new Vector2(EnemyEntryX - 0.96f + row * 0.20f, EnemyEntryY + 0.16f + lane * 0.44f);
        }

        private static List<string> BuildEnemyWaveArchetypes(string enemyKingId, bool isFinalBattle)
        {
            var result = new List<string>();
            if (string.Equals(enemyKingId, "king_nature", StringComparison.Ordinal))
            {
                result.AddRange(new[] { "enemy-ranged", "enemy-ranged", "enemy-melee", "enemy-dasher", "enemy-melee", "enemy-ranged" });
            }
            else
            {
                result.AddRange(new[] { "enemy-melee", "enemy-melee", "enemy-dasher", "enemy-ranged", "enemy-melee", "enemy-dasher" });
            }

            if (isFinalBattle)
            {
                result.Add("enemy-elite");
                result.Add("enemy-boss");
            }

            return result;
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
