#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Runtime
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DynamiteProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float fuseTime = 1.05f;
        [SerializeField] private GameObject? explosionPrefab;
        [SerializeField] private int damage = 1;
        [SerializeField] private float explosionRadius = 0.9f;

        private Rigidbody2D? _rigidbody2D;
        private Vector2 _velocity;
        private float _explodeAt;
        private bool _launched;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (_rigidbody2D != null)
            {
                _rigidbody2D.gravityScale = 0f;
                _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
                _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }

        public void Launch(Vector2 direction)
        {
            var normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _velocity = normalizedDirection * speed;
            _explodeAt = Time.time + fuseTime;
            _launched = true;
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _velocity;
            }
        }

        private void FixedUpdate()
        {
            if (!_launched || _rigidbody2D == null)
            {
                return;
            }

            _rigidbody2D.linearVelocity = _velocity;
        }

        private void Update()
        {
            if (!_launched)
            {
                return;
            }

            if (Time.time >= _explodeAt)
            {
                Explode();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_launched)
            {
                return;
            }

            if (other.GetComponentInParent<Health>()?.IsPlayer == true)
            {
                Explode();
            }
        }

        private void OnDisable()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private void Explode()
        {
            if (!_launched)
            {
                return;
            }

            _launched = false;
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            if (explosionPrefab != null)
            {
                var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                var explosionDamage = explosion.GetComponent<ExplosionDamage>();
                if (explosionDamage != null)
                {
                    explosionDamage.Configure(explosionRadius, damage, playerOnly: true);
                }
            }

            Destroy(gameObject);
        }
    }
}