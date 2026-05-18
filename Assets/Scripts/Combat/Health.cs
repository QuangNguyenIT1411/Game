using System;
using DungeonCrawler.Stats;
using UnityEngine;

namespace DungeonCrawler.Combat
{
    /// <summary>
    /// Reusable health component for actors that can take damage and die.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        public event Action<Health> Died;
        public event Action<Health> OnDied;
        public event Action<Health, int> Damaged;
        public event Action<Health> Changed;
        public event Action<Health> OnHealthChanged;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float Normalized => maxHealth <= 0 ? 0f : currentHealth / (float)maxHealth;
        public bool IsDead => currentHealth <= 0;

        private void Awake()
        {
            CharacterStats stats = GetComponent<CharacterStats>();
            if (stats != null)
            {
                maxHealth = stats.MaxHP;
                currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
            }

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public void Configure(int newMaxHealth, int newCurrentHealth)
        {
            maxHealth = Mathf.Max(1, newMaxHealth);
            currentHealth = Mathf.Clamp(newCurrentHealth, 0, maxHealth);
            Changed?.Invoke(this);
            OnHealthChanged?.Invoke(this);
        }

        public void SetMaxHealth(int newMaxHealth, bool addDifferenceToCurrent)
        {
            int previousMaxHealth = maxHealth;
            maxHealth = Mathf.Max(1, newMaxHealth);
            if (addDifferenceToCurrent)
            {
                currentHealth += maxHealth - previousMaxHealth;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            Changed?.Invoke(this);
            OnHealthChanged?.Invoke(this);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            if (IsDead)
            {
                return;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
            Debug.Log($"[Health] {gameObject.name} took {damage} damage: {previousHealth} -> {currentHealth}", this);
            Damaged?.Invoke(this, damage);
            Changed?.Invoke(this);
            OnHealthChanged?.Invoke(this);

            if (IsDead)
            {
                Debug.Log($"{gameObject.name} died", this);
                Died?.Invoke(this);
                OnDied?.Invoke(this);
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
            Changed?.Invoke(this);
            OnHealthChanged?.Invoke(this);
        }

        public void ReviveFull()
        {
            currentHealth = maxHealth;
            Changed?.Invoke(this);
            OnHealthChanged?.Invoke(this);
        }
    }
}
