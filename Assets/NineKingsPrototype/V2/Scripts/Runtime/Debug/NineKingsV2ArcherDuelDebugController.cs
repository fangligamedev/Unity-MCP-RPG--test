#nullable enable
using UnityEngine;

namespace NineKingsPrototype.V2
{
    public sealed class NineKingsV2ArcherDuelDebugController : MonoBehaviour
    {
        [SerializeField] private NineKingsV2GameController? _controller;
        [SerializeField] private float _battleSpeed = 1f;
        [SerializeField] private int _friendlyHp = 96;
        [SerializeField] private int _enemyHp = 96;
        [SerializeField] private int _friendlyDamage = 2;
        [SerializeField] private int _enemyDamage = 2;
        [SerializeField] private float _friendlyAttackInterval = 1.05f;
        [SerializeField] private float _enemyAttackInterval = 1.05f;
        [SerializeField] private float _friendlyRange = 2.9f;
        [SerializeField] private float _enemyRange = 2.9f;
        [SerializeField] private float _friendlyMoveSpeed = 0.8f;
        [SerializeField] private float _enemyMoveSpeed = 0.8f;

        private bool _initialized;

        private void Start()
        {
            if (_initialized)
            {
                return;
            }

            _controller ??= GetComponent<NineKingsV2GameController>();
            if (_controller == null)
            {
                Debug.LogError("NineKingsV2ArcherDuelDebugController 缺少 NineKingsV2GameController。");
                return;
            }

            if (_controller.Database == null)
            {
                _controller.SetDatabase(NineKingsV2SampleContentFactory.CreateInMemoryDatabase());
            }

            _controller.SetAutoBattleEnabled(true);
            _controller.SetBattleSpeedMultiplier(_battleSpeed);
            _controller.EnterDebugBattle(CreateBattleScene());
            _initialized = true;
        }

        internal BattleSceneState CreateBattleScene()
        {
            var battle = new BattleSceneState
            {
                year = 1,
                enemyKingId = "king_blood",
                isResolved = false,
                playerWon = false,
            };

            var friendlyPosition = new Vector2(-2.35f, 1.08f);
            var enemyPosition = new Vector2(2.35f, -1.08f);

            battle.entities.Add(new BattleEntityState
            {
                entityId = "archer-duel-friendly",
                sourceCardId = "nothing_archer",
                unitArchetypeId = "nothing-archer",
                isEnemy = false,
                level = 3,
                maxHp = _friendlyHp,
                currentHp = _friendlyHp,
                attackDamage = _friendlyDamage,
                attackInterval = _friendlyAttackInterval,
                attackRange = _friendlyRange,
                moveSpeed = _friendlyMoveSpeed,
                stackCount = 1,
                sourceCoord = new BoardCoord(1, 2),
                deployStartX = friendlyPosition.x,
                deployStartY = friendlyPosition.y,
                deployTargetX = friendlyPosition.x,
                deployTargetY = friendlyPosition.y,
                worldX = friendlyPosition.x,
                worldY = friendlyPosition.y,
            });

            battle.entities.Add(new BattleEntityState
            {
                entityId = "archer-duel-enemy",
                sourceCardId = "enemy-ranged",
                unitArchetypeId = "enemy-ranged",
                isEnemy = true,
                level = 3,
                maxHp = _enemyHp,
                currentHp = _enemyHp,
                attackDamage = _enemyDamage,
                attackInterval = _enemyAttackInterval,
                attackRange = _enemyRange,
                moveSpeed = _enemyMoveSpeed,
                stackCount = 1,
                sourceCoord = new BoardCoord(3, 2),
                deployStartX = enemyPosition.x,
                deployStartY = enemyPosition.y,
                deployTargetX = enemyPosition.x,
                deployTargetY = enemyPosition.y,
                worldX = enemyPosition.x,
                worldY = enemyPosition.y,
            });

            return battle;
        }
    }
}
