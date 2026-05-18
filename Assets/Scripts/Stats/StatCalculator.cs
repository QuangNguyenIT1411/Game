using UnityEngine;

namespace DungeonCrawler.Stats
{
    public static class StatCalculator
    {
        public static int CalculateDamage(CharacterStats attacker, CharacterStats target)
        {
            if (target != null && Random.value < target.DodgeRate)
            {
                Debug.Log("Dodged", target);
                return 0;
            }

            int attack = attacker != null ? attacker.Attack : 1;
            int defense = target != null ? target.Defense : 0;
            int baseDamage = Mathf.Max(1, attack - defense);

            if (attacker != null && Random.value < attacker.CritRate)
            {
                Debug.Log("Critical Hit", attacker);
                return Mathf.RoundToInt(baseDamage * attacker.CritDamage);
            }

            return baseDamage;
        }
    }
}
