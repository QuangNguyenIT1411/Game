using DungeonCrawler.Items;
using UnityEngine;

namespace DungeonCrawler.Village
{
    public class ShopManager : MonoBehaviour
    {
        public const int PotionCost = 5;
        public const int MaterialSellValue = 3;

        public static ShopManager Instance { get; private set; }

        [SerializeField] private Inventory inventory;

        private void Awake()
        {
            Instance = this;
        }

        public void Bind(Inventory targetInventory)
        {
            inventory = targetInventory;
        }

        public void BuyPotion()
        {
            Inventory targetInventory = GetInventory();
            if (targetInventory == null || !targetInventory.TrySpendGold(PotionCost))
            {
                Debug.Log("Not enough Gold", this);
                return;
            }

            targetInventory.Add(ItemType.Potion);
            Debug.Log("Bought Potion", this);
        }

        public void SellMaterial()
        {
            Inventory targetInventory = GetInventory();
            if (targetInventory == null || !targetInventory.TryRemoveMaterial())
            {
                Debug.Log("No Material to sell", this);
                return;
            }

            targetInventory.Add(ItemType.Gold, MaterialSellValue);
            Debug.Log("Sold Material", this);
        }

        private Inventory GetInventory()
        {
            if (inventory != null)
            {
                return inventory;
            }

            GameObject player = GameObject.Find("Player");
            inventory = player != null ? player.GetComponent<Inventory>() : null;
            return inventory;
        }
    }
}
