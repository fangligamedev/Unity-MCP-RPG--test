#nullable enable
using System;
using System.Collections.Generic;

namespace NineKingsPrototype.V2
{
    [Serializable]
    public sealed class BattleEntityState
    {
        public string entityId = string.Empty;
        public string sourceCardId = string.Empty;
        public string unitArchetypeId = string.Empty;
        public bool isEnemy;
        public int level = 1;
        public int maxHp = 1;
        public int currentHp = 1;
        public int attackDamage = 1;
        public float attackInterval = 1f;
        public float attackRange = 1f;
        public float moveSpeed = 1f;
        public int stackCount = 1;
        public BoardCoord sourceCoord;
        public float worldX;
        public float worldY;
        public float timeSinceLastAttack;
        public bool isDead;
    }

    [Serializable]
    public sealed class BattleSceneState
    {
        public int year;
        public string enemyKingId = string.Empty;
        public bool isFinalBattle;
        public bool isResolved;
        public bool playerWon;
        public List<BattleEntityState> entities = new();
    }

    [Serializable]
    public sealed class PresentationSnapshot
    {
        public RunState runState = new();
        public BoardSceneState boardSceneState = new();
        public BattleSceneState battleSceneState = new();
    }
}
