using System;

namespace DungeonCrawler.Items
{
    public enum EquipmentType
    {
        Sword,
        Armor
    }

    [Serializable]
    public class EquipmentItem
    {
        public EquipmentType Type;
        public string Name;
        public int AttackBonus;
        public int DefenseBonus;
        public int MaxHPBonus;

        public static EquipmentItem CreateSword()
        {
            return new EquipmentItem
            {
                Type = EquipmentType.Sword,
                Name = "Training Sword",
                AttackBonus = 5
            };
        }

        public static EquipmentItem CreateArmor()
        {
            return new EquipmentItem
            {
                Type = EquipmentType.Armor,
                Name = "Training Armor",
                DefenseBonus = 2,
                MaxHPBonus = 20
            };
        }
    }
}
