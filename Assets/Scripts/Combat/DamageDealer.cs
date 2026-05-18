using DungeonCrawler.Stats;
using UnityEngine;

namespace DungeonCrawler.Combat
{
    /// <summary>
    /// Small reusable helper for cooldown-gated damage.
    /// </summary>
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private float cooldown = 1f;

        private float nextAttackTime;

        public int Damage
        {
            get => damage;
            set => damage = Mathf.Max(0, value);
        }

        public float Cooldown
        {
            get => cooldown;
            set => cooldown = Mathf.Max(0f, value);
        }

        public bool CanDealDamage => Time.time >= nextAttackTime;

        public bool TryDealDamage(Health targetHealth)
        {
            if (targetHealth == null || !CanDealDamage)
            {
                return false;
            }

            CharacterStats attackerStats = GetComponent<CharacterStats>();
            CharacterStats targetStats = targetHealth.GetComponent<CharacterStats>();
            int finalDamage = StatCalculator.CalculateDamage(attackerStats, targetStats);
            if (finalDamage > 0)
            {
                targetHealth.TakeDamage(finalDamage);
            }

            nextAttackTime = Time.time + cooldown;
            return true;
        }

        public void StartCooldown()
        {
            nextAttackTime = Time.time + cooldown;
        }
    }
}
