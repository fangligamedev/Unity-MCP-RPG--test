#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using UnityEngine;

namespace Game2DRPG.Runtime
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 6;
        [SerializeField] private float invulnerabilityDuration = 0.15f;
        [SerializeField] private bool isPlayer;
        [SerializeField] private bool destroyOnDeath = true;

        private float _lastDamageTime = -999f;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsPlayer => isPlayer;

        public event Action<Health>? Changed;
        public event Action<Health>? Died;
        public event Action<Health, int>? Damaged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void Configure(int newMaxHealth, bool playerOwned, bool shouldDestroyOnDeath)
        {
            maxHealth = Mathf.Max(1, newMaxHealth);
            isPlayer = playerOwned;
            destroyOnDeath = shouldDestroyOnDeath;
            CurrentHealth = Mathf.Clamp(CurrentHealth <= 0 ? maxHealth : CurrentHealth, 0, maxHealth);
            Changed?.Invoke(this);
        }

        public bool TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return false;
            }

            if (Time.time < _lastDamageTime + invulnerabilityDuration)
            {
                return false;
            }

            _lastDamageTime = Time.time;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Damaged?.Invoke(this, amount);
            Changed?.Invoke(this);

            if (CurrentHealth == 0)
            {
                IsDead = true;
                Died?.Invoke(this);
                if (destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }

            return true;
        }

        public void Restore(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);
            Changed?.Invoke(this);
        }

        public void IncreaseMaxHealth(int amount, bool alsoHeal)
        {
            if (amount <= 0)
            {
                return;
            }

            maxHealth += amount;
            if (alsoHeal)
            {
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            }
            Changed?.Invoke(this);
        }

        public void ResetHealth()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            _lastDamageTime = -999f;
            Changed?.Invoke(this);
        }
    }
}
