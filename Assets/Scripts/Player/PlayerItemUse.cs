using DungeonCrawler.Combat;
using DungeonCrawler.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Player
{
    public class PlayerItemUse : MonoBehaviour
    {
        private const int PotionHealAmount = 30;

        private Inventory inventory;
        private Health health;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || keyboard.qKey == null || !keyboard.qKey.wasPressedThisFrame)
            {
                return;
            }

            UsePotion();
        }

        private void UsePotion()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (inventory == null || !inventory.TryUsePotion())
            {
                Debug.Log("No potion available", this);
                return;
            }

            if (health != null)
            {
                health.Heal(PotionHealAmount);
            }

            Debug.Log("Used Potion: +30 HP", this);
        }
    }
}
