using System.IO;
using DungeonCrawler.Combat;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Items;
using DungeonCrawler.Progression;
using DungeonCrawler.Stats;
using DungeonCrawler.UI;
using UnityEngine;

namespace DungeonCrawler.Save
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        private void Awake()
        {
            Instance = this;
        }

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public void SaveGame()
        {
            GameObject player = GameObject.Find("Player");
            SaveData data = new SaveData();

            Inventory inventory = player != null ? player.GetComponent<Inventory>() : null;
            if (inventory != null)
            {
                data.gold = inventory.Gold;
                data.potion = inventory.Potion;
                data.material = inventory.Material;
            }

            EquipmentInventory equipment = player != null ? player.GetComponent<EquipmentInventory>() : null;
            if (equipment != null)
            {
                data.swordOwned = equipment.SwordCount;
                data.armorOwned = equipment.ArmorCount;
                data.equippedSwordName = equipment.EquippedSwordName;
                data.equippedArmorName = equipment.EquippedArmorName;
                data.equippedSwordAttackBonus = equipment.EquippedSwordAttackBonus;
                data.equippedArmorDefenseBonus = equipment.EquippedArmorDefenseBonus;
                data.equippedArmorMaxHpBonus = equipment.EquippedArmorMaxHPBonus;
            }

            PlayerLevel playerLevel = player != null ? player.GetComponent<PlayerLevel>() : null;
            if (playerLevel != null)
            {
                data.level = playerLevel.Level;
                data.currentExp = playerLevel.CurrentExp;
                data.statPoints = playerLevel.StatPoints;
            }

            CharacterStats stats = player != null ? player.GetComponent<CharacterStats>() : null;
            if (stats != null)
            {
                data.baseMaxHP = stats.BaseMaxHP;
                data.baseAttack = stats.BaseAttack;
                data.baseDefense = stats.BaseDefense;
                data.critRate = stats.CritRate;
                data.critDamage = stats.CritDamage;
                data.dodgeRate = stats.DodgeRate;
                data.moveSpeed = stats.MoveSpeed;
            }

            FloorManager floorManager = FloorManager.Instance;
            if (floorManager != null)
            {
                data.highestUnlockedFloor = floorManager.HighestUnlockedFloor;
                data.checkpointFloor = floorManager.CheckpointFloor;
                data.unlockedCheckpointFloors = new System.Collections.Generic.List<int>(floorManager.UnlockedCheckpointFloors);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Save path: {SavePath}", this);
            Debug.Log("Game Saved", this);
        }

        public void LoadGame()
        {
            if (!HasSave())
            {
                Debug.Log("No save file found", this);
                return;
            }

            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            GameObject player = GameObject.Find("Player");

            CharacterStats stats = player != null ? player.GetComponent<CharacterStats>() : null;
            if (stats != null)
            {
                stats.Configure(data.baseMaxHP, data.baseAttack, data.baseDefense, data.critRate, data.critDamage, data.dodgeRate, data.moveSpeed);
            }

            Inventory inventory = player != null ? player.GetComponent<Inventory>() : null;
            if (inventory != null)
            {
                inventory.SetCounts(data.gold, data.potion, data.material);
            }

            EquipmentInventory equipment = player != null ? player.GetComponent<EquipmentInventory>() : null;
            if (equipment != null)
            {
                equipment.ConfigureSaved(
                    data.swordOwned,
                    data.armorOwned,
                    data.equippedSwordName,
                    data.equippedArmorName,
                    data.equippedSwordAttackBonus,
                    data.equippedArmorDefenseBonus,
                    data.equippedArmorMaxHpBonus);
            }

            PlayerLevel playerLevel = player != null ? player.GetComponent<PlayerLevel>() : null;
            if (playerLevel != null)
            {
                playerLevel.ConfigureProgress(data.level, data.currentExp, data.statPoints);
            }

            Health health = player != null ? player.GetComponent<Health>() : null;
            if (health != null && stats != null)
            {
                health.SetMaxHealth(stats.MaxHP, true);
            }

            FloorManager floorManager = FloorManager.Instance;
            if (floorManager != null)
            {
                floorManager.ConfigureProgression(data.highestUnlockedFloor, data.checkpointFloor, data.unlockedCheckpointFloors);
            }

            RefreshKnownUI();
            Debug.Log("Game Loaded", this);
        }

        public void DeleteSave()
        {
            if (HasSave())
            {
                File.Delete(SavePath);
            }

            Debug.Log("Save Deleted", this);
        }

        private static void RefreshKnownUI()
        {
            foreach (InventoryDebugUI ui in FindObjectsByType<InventoryDebugUI>(FindObjectsInactive.Include))
            {
                ui.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
            }

            FloorSelectUI floorSelectUI = FindAnyObjectByType<FloorSelectUI>(FindObjectsInactive.Include);
            if (floorSelectUI != null)
            {
                floorSelectUI.Refresh();
            }
        }
    }
}
