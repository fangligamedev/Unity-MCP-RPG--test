#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Runtime
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ExplosionDamage : MonoBehaviour
    {
        [SerializeField] private float radius = 0.9f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifetime = 0.3f;
        [SerializeField] private bool damagePlayerOnly = true;

        private bool _hasExploded;

        public void Configure(float newRadius, int newDamage, bool playerOnly)
        {
            radius = newRadius;
            damage = newDamage;
            damagePlayerOnly = playerOnly;
        }

        private void OnEnable()
        {
            if (_hasExploded)
            {
                return;
            }

            _hasExploded = true;
            var colliders = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var collider in colliders)
            {
                var health = collider.GetComponentInParent<Health>();
                if (health == null)
                {
                    continue;
                }

                if (damagePlayerOnly && !health.IsPlayer)
                {
                    continue;
                }

                if (!damagePlayerOnly && health.IsPlayer)
                {
                    continue;
                }

                health.TakeDamage(damage);
            }

            Destroy(gameObject, lifetime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
