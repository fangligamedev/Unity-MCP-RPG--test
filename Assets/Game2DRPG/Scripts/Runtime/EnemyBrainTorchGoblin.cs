#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyBrainTorchGoblin : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.3f;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactCooldown = 0.7f;

        private Rigidbody2D? _rigidbody2D;
        private Animator? _animator;
        private SpriteRenderer? _spriteRenderer;
        private Transform? _player;
        private float _nextDamageTime;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            var player = FindAnyObjectByType<TopDownPlayerController>();
            _player = player != null ? player.transform : null;
        }

        private void Update()
        {
            if (_player == null || ArenaGameState.Instance?.State != RunState.Playing)
            {
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.linearVelocity = Vector2.zero;
                }
                return;
            }

            var direction = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = direction * moveSpeed;
            }

            if (_spriteRenderer != null && Mathf.Abs(direction.x) > 0.05f)
            {
                _spriteRenderer.flipX = direction.x < 0f;
            }

            if (_animator != null)
            {
                _animator.SetBool("IsMoving", true);
                _animator.SetFloat("MoveX", direction.x);
                _animator.SetFloat("MoveY", direction.y);
            }
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            if (Time.time < _nextDamageTime)
            {
                return;
            }

            var health = other.gameObject.GetComponentInParent<Health>();
            if (health == null || !health.IsPlayer)
            {
                return;
            }

            if (health.TakeDamage(contactDamage))
            {
                _nextDamageTime = Time.time + contactCooldown;
            }
        }
    }
}
