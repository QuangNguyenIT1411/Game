using System;
using System.Collections.Generic;
using DungeonCrawler.Combat;
using DungeonCrawler.Stats;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Items
{
    public class EquipmentInventory : MonoBehaviour
    {
        private readonly List<EquipmentItem> swords = new List<EquipmentItem>();
        private readonly List<EquipmentItem> armors = new List<EquipmentItem>();

        private EquipmentItem equippedSword;
        private EquipmentItem equippedArmor;
        private CharacterStats stats;
        private Health health;

        public event Action<EquipmentInventory> Changed;

        public int SwordCount => swords.Count;
        public int ArmorCount => armors.Count;
        public string EquippedSwordName => equippedSword != null ? equippedSword.Name : "None";
        public string EquippedArmorName => equippedArmor != null ? equippedArmor.Name : "None";
        public int EquippedSwordAttackBonus => equippedSword != null ? equippedSword.AttackBonus : 0;
        public int EquippedArmorDefenseBonus => equippedArmor != null ? equippedArmor.DefenseBonus : 0;
        public int EquippedArmorMaxHPBonus => equippedArmor != null ? equippedArmor.MaxHPBonus : 0;

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key != null && keyboard.digit1Key.wasPressedThisFrame)
            {
                EquipFirstSword();
            }

            if (keyboard.digit2Key != null && keyboard.digit2Key.wasPressedThisFrame)
            {
                EquipFirstArmor();
            }
        }

        public void Add(EquipmentItem item)
        {
            if (item == null)
            {
                return;
            }

            if (item.Type == EquipmentType.Sword)
            {
                swords.Add(item);
            }
            else if (item.Type == EquipmentType.Armor)
            {
                armors.Add(item);
            }

            Changed?.Invoke(this);
        }

        public void EquipFirstSword()
        {
            if (swords.Count <= 0)
            {
                return;
            }

            equippedSword = swords[0];
            ApplyEquipmentBonuses(false);
            Debug.Log("Equipped Sword", this);
        }

        public void EquipFirstArmor()
        {
            if (armors.Count <= 0)
            {
                return;
            }

            equippedArmor = armors[0];
            ApplyEquipmentBonuses(true);
            Debug.Log("Equipped Armor", this);
        }

        public void ConfigureSaved(
            int swordOwned,
            int armorOwned,
            string equippedSwordName,
            string equippedArmorName,
            int equippedSwordAttackBonus,
            int equippedArmorDefenseBonus,
            int equippedArmorMaxHpBonus)
        {
            swords.Clear();
            armors.Clear();
            equippedSword = null;
            equippedArmor = null;

            for (int i = 0; i < Mathf.Max(0, swordOwned); i++)
            {
                swords.Add(EquipmentItem.CreateSword());
            }

            for (int i = 0; i < Mathf.Max(0, armorOwned); i++)
            {
                armors.Add(EquipmentItem.CreateArmor());
            }

            if (!string.IsNullOrEmpty(equippedSwordName) && equippedSwordName != "None")
            {
                equippedSword = new EquipmentItem
                {
                    Type = EquipmentType.Sword,
                    Name = equippedSwordName,
                    AttackBonus = Mathf.Max(0, equippedSwordAttackBonus)
                };
                if (swords.Count == 0)
                {
                    swords.Add(equippedSword);
                }
            }

            if (!string.IsNullOrEmpty(equippedArmorName) && equippedArmorName != "None")
            {
                equippedArmor = new EquipmentItem
                {
                    Type = EquipmentType.Armor,
                    Name = equippedArmorName,
                    DefenseBonus = Mathf.Max(0, equippedArmorDefenseBonus),
                    MaxHPBonus = Mathf.Max(0, equippedArmorMaxHpBonus)
                };
                if (armors.Count == 0)
                {
                    armors.Add(equippedArmor);
                }
            }

            ApplyEquipmentBonuses(true);
        }

        private void ApplyEquipmentBonuses(bool adjustCurrentHealth)
        {
            if (stats == null)
            {
                stats = GetComponent<CharacterStats>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            int attackBonus = equippedSword != null ? equippedSword.AttackBonus : 0;
            int defenseBonus = equippedArmor != null ? equippedArmor.DefenseBonus : 0;
            int maxHPBonus = equippedArmor != null ? equippedArmor.MaxHPBonus : 0;

            if (stats != null)
            {
                stats.SetEquipmentBonuses(attackBonus, defenseBonus, maxHPBonus);
            }

            if (health != null && stats != null)
            {
                health.SetMaxHealth(stats.MaxHP, adjustCurrentHealth);
            }

            Changed?.Invoke(this);
        }
    }
}
