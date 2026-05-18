using UnityEngine;
using System;

namespace DungeonCrawler.Stats
{
    public class CharacterStats : MonoBehaviour
    {
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int attack = 10;
        [SerializeField] private int defense = 2;
        [SerializeField] private float critRate = 0.1f;
        [SerializeField] private float critDamage = 1.5f;
        [SerializeField] private float dodgeRate = 0.05f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private int equipmentAttackBonus;
        [SerializeField] private int equipmentDefenseBonus;
        [SerializeField] private int equipmentMaxHPBonus;

        public event Action<CharacterStats> Changed;

        public int MaxHP => maxHP + equipmentMaxHPBonus;
        public int BaseMaxHP => maxHP;
        public int Attack => attack + equipmentAttackBonus;
        public int BaseAttack => attack;
        public int Defense => defense + equipmentDefenseBonus;
        public int BaseDefense => defense;
        public float CritRate => critRate;
        public float CritDamage => critDamage;
        public float DodgeRate => dodgeRate;
        public float MoveSpeed => moveSpeed;

        public void Configure(int newMaxHP, int newAttack, int newDefense, float newCritRate, float newCritDamage, float newDodgeRate, float newMoveSpeed)
        {
            maxHP = Mathf.Max(1, newMaxHP);
            attack = Mathf.Max(0, newAttack);
            defense = Mathf.Max(0, newDefense);
            critRate = Mathf.Clamp01(newCritRate);
            critDamage = Mathf.Max(1f, newCritDamage);
            dodgeRate = Mathf.Clamp01(newDodgeRate);
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
            Changed?.Invoke(this);
        }

        public void ConfigurePlayerDefaults()
        {
            Configure(100, 10, 2, 0.1f, 1.5f, 0.05f, 5f);
        }

        public void ConfigureEnemyDefaults(int debugMaxHP = 10)
        {
            Configure(debugMaxHP, 8, 0, 0.05f, 1.5f, 0f, 3f);
        }

        public void SetEquipmentBonuses(int attackBonus, int defenseBonus, int maxHPBonus)
        {
            equipmentAttackBonus = Mathf.Max(0, attackBonus);
            equipmentDefenseBonus = Mathf.Max(0, defenseBonus);
            equipmentMaxHPBonus = Mathf.Max(0, maxHPBonus);
            Changed?.Invoke(this);
        }

        public void AddAttack(int amount)
        {
            attack = Mathf.Max(0, attack + amount);
            Changed?.Invoke(this);
        }

        public void AddDefense(int amount)
        {
            defense = Mathf.Max(0, defense + amount);
            Changed?.Invoke(this);
        }

        public void AddMaxHP(int amount)
        {
            maxHP = Mathf.Max(1, maxHP + amount);
            Changed?.Invoke(this);
        }
    }
}
