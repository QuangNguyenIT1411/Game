using System;
using System.Collections.Generic;

namespace DungeonCrawler.Save
{
    [Serializable]
    public class SaveData
    {
        public int gold;
        public int potion;
        public int material;

        public int swordOwned;
        public int armorOwned;
        public string equippedSwordName;
        public string equippedArmorName;
        public int equippedSwordAttackBonus;
        public int equippedArmorDefenseBonus;
        public int equippedArmorMaxHpBonus;

        public int level;
        public int currentExp;
        public int statPoints;

        public int baseMaxHP;
        public int baseAttack;
        public int baseDefense;
        public float critRate;
        public float critDamage;
        public float dodgeRate;
        public float moveSpeed;

        public int highestUnlockedFloor;
        public int checkpointFloor;
        public List<int> unlockedCheckpointFloors = new List<int>();
    }
}
