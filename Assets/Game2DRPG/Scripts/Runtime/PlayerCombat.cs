#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Runtime
{
    [RequireComponent(typeof(TopDownPlayerController))]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private int attackDamage = 2;
        [SerializeField] private float attackRange = 0.9f;
        [SerializeField] private float attackRadius = 0.55f;
        [SerializeField] private float attackCooldown = 0.35f;

        private TopDownPlayerController? _controller;
        private Animator? _animator;
        private float _nextAttackTime;

        public int AttackDamage => attackDamage;

        private void Awake()
        {
            _controller = GetComponent<TopDownPlayerController>();
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (_controller == null || ArenaGameState.Instance?.State != RunState.Playing)
            {
                return;
            }

            if (_controller.CurrentInput.AttackPressed && Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + attackCooldown;
                PerformAttack();
            }

            if (_animator != null)
            {
                _animator.SetBool("IsMoving", _controller.CurrentInput.Move.sqrMagnitude > 0.01f);
                _animator.SetFloat("MoveX", _controller.FacingDirection.x);
                _animator.SetFloat("MoveY", _controller.FacingDirection.y);
            }
        }

        public void IncreaseAttackPower(int amount)
        {
            attackDamage = Mathf.Max(1, attackDamage + amount);
        }

        private void PerformAttack()
        {
            var direction = _controller != null && _controller.FacingDirection.sqrMagnitude > 0.01f
                ? _controller.FacingDirection.normalized
                : Vector2.right;
            var center = (Vector2)transform.position + direction * attackRange;
            var hits = Physics2D.OverlapCircleAll(center, attackRadius);
            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
                {
                    continue;
                }

                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsPlayer)
                {
                    continue;
                }

                health.TakeDamage(attackDamage);
            }

            _animator?.SetTrigger("Attack");
        }

        private void OnDrawGizmosSelected()
        {
            var controller = Application.isPlaying ? _controller : GetComponent<TopDownPlayerController>();
            var direction = controller != null && controller.FacingDirection.sqrMagnitude > 0.01f ? controller.FacingDirection : Vector2.right;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)transform.position + direction.normalized * attackRange, attackRadius);
        }
    }
}
