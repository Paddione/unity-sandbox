using System.Collections.Generic;

/// <summary>Base types per slot + the 3 fixed uniques (P2).</summary>
public static class ItemDatabase
{
    public static List<string> GetBaseTypes(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Weapon:
                return new List<string> { "ShortSword", "HeavyAxe", "Dagger" };
            case ItemSlot.Armor:
                return new List<string> { "ClothTunic", "LeatherVest", "Breastplate" };
            default:
                return new List<string> { "Hood", "Helm", "Circlet" };
        }
    }

    public static ItemInstance CreateUnique(string uniqueId)
    {
        switch (uniqueId)
        {
            case "Emberbrand Blade":
                return new ItemInstance
                {
                    slot = ItemSlot.Weapon,
                    rarity = ItemRarity.Unique,
                    baseType = "ShortSword",
                    uniqueId = "Emberbrand Blade",
                    affixes = new List<Affix> { new Affix("Damage", 6f), new Affix("Crit", 10f) },
                };
            case "Cryptwarden Plate":
                return new ItemInstance
                {
                    slot = ItemSlot.Armor,
                    rarity = ItemRarity.Unique,
                    baseType = "Breastplate",
                    uniqueId = "Cryptwarden Plate",
                    affixes = new List<Affix> { new Affix("MaxHP", 30f), new Affix("Armor", 4f) },
                };
            default:
                return new ItemInstance
                {
                    slot = ItemSlot.Helmet,
                    rarity = ItemRarity.Unique,
                    baseType = "Circlet",
                    uniqueId = "Seer's Circlet",
                    affixes = new List<Affix> { new Affix("XPMul", 20f), new Affix("Crit", 8f) },
                };
        }
    }

    public static List<ItemInstance> GetUniques()
    {
        return new List<ItemInstance>
        {
            CreateUnique("Emberbrand Blade"),
            CreateUnique("Cryptwarden Plate"),
            CreateUnique("Seer's Circlet"),
        };
    }
}
