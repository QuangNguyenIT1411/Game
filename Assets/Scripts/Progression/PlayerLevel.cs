using System;
using DungeonCrawler.Combat;
using DungeonCrawler.Stats;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Progression
{
    public class PlayerLevel : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int currentExp;
        [SerializeField] private int statPoints;

        private CharacterStats stats;
        private Health health;

        public event Action<PlayerLevel> Changed;

        public int Level => level;
        public int CurrentExp => currentExp;
        public int ExpToNext => level * 20;
        public int StatPoints => statPoints;

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

            if (keyboard.zKey != null && keyboard.zKey.wasPressedThisFrame)
            {
                Debug.Log("Z pressed - try add attack", this);
                SpendStatPoint(StatPointType.Attack);
            }

            if (keyboard.xKey != null && keyboard.xKey.wasPressedThisFrame)
            {
                Debug.Log("X pressed - try add defense", this);
                SpendStatPoint(StatPointType.Defense);
            }

            if (keyboard.cKey != null && keyboard.cKey.wasPressedThisFrame)
            {
                Debug.Log("C pressed - try add maxHP", this);
                SpendStatPoint(StatPointType.MaxHP);
            }
        }

        public void AddExp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentExp += amount;
            while (currentExp >= ExpToNext)
            {
                currentExp -= ExpToNext;
                level++;
                statPoints += 3;
                Debug.Log($"Level Up! Level: {level}", this);
            }

            Changed?.Invoke(this);
        }

        public void ResetProgress()
        {
            level = 1;
            currentExp = 0;
            statPoints = 3;
            Changed?.Invoke(this);
        }

        public void ConfigureProgress(int newLevel, int newCurrentExp, int newStatPoints)
        {
            level = Mathf.Max(1, newLevel);
            currentExp = Mathf.Max(0, newCurrentExp);
            statPoints = Mathf.Max(0, newStatPoints);
            Changed?.Invoke(this);
        }

        private void SpendStatPoint(StatPointType type)
        {
            if (statPoints <= 0)
            {
                Debug.Log("No stat points available", this);
                return;
            }

            if (stats == null)
            {
                stats = GetComponent<CharacterStats>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (stats == null)
            {
                Debug.LogWarning("Cannot spend stat point: CharacterStats is missing.", this);
                return;
            }

            int beforeAttack = stats.Attack;
            int beforeDefense = stats.Defense;
            int beforeMaxHP = stats.MaxHP;
            statPoints--;
            switch (type)
            {
                case StatPointType.Attack:
                    stats.AddAttack(1);
                    Debug.Log($"Attack stat: {beforeAttack} -> {stats.Attack}", this);
                    break;
                case StatPointType.Defense:
                    stats.AddDefense(1);
                    Debug.Log($"Defense stat: {beforeDefense} -> {stats.Defense}", this);
                    break;
                case StatPointType.MaxHP:
                    stats.AddMaxHP(10);
                    if (health != null)
                    {
                        health.SetMaxHealth(stats.MaxHP, true);
                    }
                    else
                    {
                        Debug.LogWarning("Cannot update maxHP: Health is missing.", this);
                    }

                    Debug.Log($"MaxHP stat: {beforeMaxHP} -> {stats.MaxHP}", this);
                    break;
            }

            Changed?.Invoke(this);
        }

        private enum StatPointType
        {
            Attack,
            Defense,
            MaxHP
        }
    }
}
