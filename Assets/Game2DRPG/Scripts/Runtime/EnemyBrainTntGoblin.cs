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
    public sealed class EnemyBrainTntGoblin : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float preferredMinRange = 2f;
        [SerializeField] private float preferredMaxRange = 3.5f;
        [SerializeField] private float throwCooldown = 1.75f;
        [SerializeField] private GameObject? dynamitePrefab;
        [SerializeField] private Transform? throwOrigin;

        private Rigidbody2D? _rigidbody2D;
        private Animator? _animator;
        private SpriteRenderer? _spriteRenderer;
        private Transform? _player;
        private float _nextThrowTime;

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

            var toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            var distance = toPlayer.magnitude;
            var direction = distance > 0.001f ? toPlayer / distance : Vector2.zero;
            var velocity = Vector2.zero;
            var canThrow = Time.time >= _nextThrowTime && distance <= preferredMaxRange + 0.15f && direction.sqrMagnitude > 0.001f;

            if (canThrow)
            {
                ThrowDynamite(direction);
                _nextThrowTime = Time.time + throwCooldown;
            }

            if (distance > preferredMaxRange)
            {
                velocity = direction * moveSpeed;
            }
            else if (distance < preferredMinRange)
            {
                velocity = -direction * moveSpeed * 0.85f;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = velocity;
            }

            if (_spriteRenderer != null && Mathf.Abs(direction.x) > 0.05f)
            {
                _spriteRenderer.flipX = direction.x < 0f;
            }

            if (_animator != null)
            {
                _animator.SetBool("IsMoving", velocity.sqrMagnitude > 0.01f);
                _animator.SetFloat("MoveX", direction.x);
                _animator.SetFloat("MoveY", direction.y);
            }
        }

        private void ThrowDynamite(Vector2 direction)
        {
            if (dynamitePrefab == null)
            {
                return;
            }

            var origin = throwOrigin != null ? throwOrigin.position : transform.position;
            var projectileObject = Instantiate(dynamitePrefab, origin, Quaternion.identity);
            var projectile = projectileObject.GetComponent<DynamiteProjectile>();
            projectile?.Launch(direction);
            _animator?.SetTrigger("Attack");
        }
    }
}