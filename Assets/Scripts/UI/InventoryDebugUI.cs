using DungeonCrawler.Items;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// Lightweight HUD text showing current inventory counts.
    /// </summary>
    public class InventoryDebugUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private Text inventoryText; // Fallback
        [SerializeField] private Text goldText;
        [SerializeField] private Text potionText;
        [SerializeField] private Text materialText;

        private void Awake()
        {
            FindInventoryIfMissing();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(Inventory targetInventory, Text targetText)
        {
            Unsubscribe();
            inventory = targetInventory;
            inventoryText = targetText;
            Subscribe();
            Refresh();
        }

        private void FindInventoryIfMissing()
        {
            if (inventory != null)
            {
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                inventory = player.GetComponent<Inventory>();
            }
        }

        private void Subscribe()
        {
            if (inventory != null)
            {
                inventory.Changed += OnInventoryChanged;
            }
        }

        private void Unsubscribe()
        {
            if (inventory != null)
            {
                inventory.Changed -= OnInventoryChanged;
            }
        }

        private void OnInventoryChanged(Inventory changedInventory)
        {
            Refresh();
        }

        public void UpdateUI()
        {
            Refresh();
        }

        private void Refresh()
        {
            FindInventoryIfMissing();
            if (inventory == null)
            {
                return;
            }

            if (goldText != null) goldText.text = inventory.Gold.ToString();
            if (potionText != null) potionText.text = inventory.Potion.ToString();
            if (materialText != null) materialText.text = inventory.Material.ToString();

            // Legacy fallback
            if (inventoryText != null && goldText == null)
            {
                inventoryText.text = $"Gold: {inventory.Gold}\nPotion: {inventory.Potion}\nMaterial: {inventory.Material}";
            }
        }
    }
}
