using System;
using UnityEngine;

namespace DungeonCrawler.Items
{
    /// <summary>
    /// Minimal player inventory counts for early loot testing.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private int potion;
        [SerializeField] private int material;

        public event Action<Inventory> Changed;

        public int Gold => gold;
        public int Potion => potion;
        public int Material => material;

        public void SetCounts(int newGold, int newPotion, int newMaterial)
        {
            gold = Mathf.Max(0, newGold);
            potion = Mathf.Max(0, newPotion);
            material = Mathf.Max(0, newMaterial);
            Changed?.Invoke(this);
        }

        public void Add(ItemType itemType, int amount = 1)
        {
            amount = Mathf.Max(1, amount);
            switch (itemType)
            {
                case ItemType.Gold:
                    gold += amount;
                    break;
                case ItemType.Potion:
                    potion += amount;
                    break;
                case ItemType.Material:
                    material += amount;
                    break;
            }

            Changed?.Invoke(this);
        }

        public bool TryUsePotion()
        {
            if (potion <= 0)
            {
                return false;
            }

            potion--;
            Changed?.Invoke(this);
            return true;
        }

        public bool TrySpendGold(int amount)
        {
            amount = Mathf.Max(1, amount);
            if (gold < amount)
            {
                return false;
            }

            gold -= amount;
            Changed?.Invoke(this);
            return true;
        }

        public bool TryRemoveMaterial(int amount = 1)
        {
            amount = Mathf.Max(1, amount);
            if (material < amount)
            {
                return false;
            }

            material -= amount;
            Changed?.Invoke(this);
            return true;
        }

        public void ResetCounts()
        {
            gold = 0;
            potion = 0;
            material = 0;
            Changed?.Invoke(this);
        }
    }

    public enum ItemType
    {
        Gold,
        Potion,
        Material,
        Sword,
        Armor
    }
}
