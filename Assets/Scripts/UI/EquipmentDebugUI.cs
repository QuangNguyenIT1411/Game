using DungeonCrawler.Items;
using DungeonCrawler.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class EquipmentDebugUI : MonoBehaviour
    {
        [SerializeField] private EquipmentInventory equipmentInventory;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private Text equipmentText;

        private void Awake()
        {
            FindTargetsIfMissing();
            if (equipmentText == null)
            {
                equipmentText = GetComponent<Text>();
            }
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

        public void Bind(EquipmentInventory targetInventory, CharacterStats targetStats, Text targetText)
        {
            Unsubscribe();
            equipmentInventory = targetInventory;
            stats = targetStats;
            equipmentText = targetText;
            Subscribe();
            Refresh();
        }

        private void FindTargetsIfMissing()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return;
            }

            if (equipmentInventory == null)
            {
                equipmentInventory = player.GetComponent<EquipmentInventory>();
            }

            if (stats == null)
            {
                stats = player.GetComponent<CharacterStats>();
            }
        }

        private void Subscribe()
        {
            if (equipmentInventory != null)
            {
                equipmentInventory.Changed += OnEquipmentChanged;
            }

            if (stats != null)
            {
                stats.Changed += OnStatsChanged;
            }
        }

        private void Unsubscribe()
        {
            if (equipmentInventory != null)
            {
                equipmentInventory.Changed -= OnEquipmentChanged;
            }

            if (stats != null)
            {
                stats.Changed -= OnStatsChanged;
            }
        }

        private void OnEquipmentChanged(EquipmentInventory changedInventory)
        {
            Refresh();
        }

        private void OnStatsChanged(CharacterStats changedStats)
        {
            Refresh();
        }

        public void UpdateUI()
        {
            Refresh();
        }

        private void Refresh()
        {
            FindTargetsIfMissing();
            if (equipmentInventory == null || equipmentText == null)
            {
                return;
            }

            equipmentText.text =
                $"Sword owned: {equipmentInventory.SwordCount}\n" +
                $"Armor owned: {equipmentInventory.ArmorCount}\n" +
                $"Equipped Sword: {equipmentInventory.EquippedSwordName}\n" +
                $"Equipped Armor: {equipmentInventory.EquippedArmorName}" +
                (stats != null ? $"\nATK: {stats.Attack} DEF: {stats.Defense} MaxHP: {stats.MaxHP}" : string.Empty);
        }
    }
}
