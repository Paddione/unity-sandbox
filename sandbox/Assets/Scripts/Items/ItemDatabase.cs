using System.Collections.Generic;

/// <summary>Base types per slot + the 3 fixed uniques (P2).</summary>
public static class ItemDatabase
{
    // second-class S4: weapon bases per class. Armor and helmets fit every class.
    public static readonly string[] FenwalkerWeapons = { "ShortSword", "HeavyAxe", "Dagger" };
    public static readonly string[] LanternbearerWeapons = { "Lantern", "HoodedLantern", "EmberCenser" };
    public const string StarterSwordBase = "Worn Short Sword";
    public const string StarterLanternBase = "Worn Lantern";

    /// <summary>Class that may equip a base type; "" = every class.</summary>
    public static string ClassRestriction(string baseType)
    {
        if (string.IsNullOrEmpty(baseType)) return "";
        if (baseType == StarterSwordBase || System.Array.IndexOf(FenwalkerWeapons, baseType) >= 0)
            return ClassCatalog.FenwalkerId;
        if (baseType == StarterLanternBase || System.Array.IndexOf(LanternbearerWeapons, baseType) >= 0)
            return ClassCatalog.LanternbearerId;
        return "";
    }

    /// <summary>Base types of a slot for one class (weapons differ, the rest is shared).</summary>
    public static List<string> GetBaseTypes(ItemSlot slot, string classId)
    {
        if (slot != ItemSlot.Weapon) return GetBaseTypes(slot);
        return new List<string>(classId == ClassCatalog.LanternbearerId ? LanternbearerWeapons : FenwalkerWeapons);
    }

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
            case "Lantern of the Hold":
                return new ItemInstance
                {
                    slot = ItemSlot.Weapon,
                    rarity = ItemRarity.Unique,
                    baseType = "Lantern",
                    uniqueId = "Lantern of the Hold",
                    affixes = new List<Affix> { new Affix("SkillRank", 1f), new Affix("CooldownReduction", 10f), new Affix("MaxHP", 10f) },
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

    /// <summary>Healing potion (S3 vendor consumable, slot Consumable, drunk with key 1).</summary>
    public const int PotionPrice = 10;

    /// <summary>HP restored by one healing potion.</summary>
    public const float PotionHeal = 10f;

    /// <summary>Stackable vendor potion: bagged like loot, never equipped.</summary>
    public static ItemInstance CreateHealingPotion()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Consumable,
            rarity = ItemRarity.Magic,
            baseType = "Healing Potion",
            affixes = new List<Affix>(),
        };
    }

    /// <summary>Fixed starter sword, auto-equipped at spawn (Ashen Fen).</summary>
    public static ItemInstance CreateStarterSword()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Magic,
            baseType = StarterSwordBase,
            affixes = new List<Affix> { new Affix("Damage", 2f) },
        };
    }

    /// <summary>Lanternbearer starter weapon (same +2 Damage as the sword).</summary>
    public static ItemInstance CreateStarterLantern()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Magic,
            baseType = StarterLanternBase,
            affixes = new List<Affix> { new Affix("Damage", 2f) },
        };
    }

    /// <summary>Kindled-finale reward weapon (S2): class-compatible Rare with Kindled affixes.</summary>
    public static ItemInstance CreateKindledFinaleWeapon(string classId)
    {
        string id = ClassCatalog.Get(classId).id;
        string baseType = id == ClassCatalog.LanternbearerId ? LanternbearerWeapons[0] : FenwalkerWeapons[0];
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = baseType,
            affixes = new List<Affix>
            {
                new Affix("Damage", 6f),
                new Affix("Crit", 12f),
                new Affix("MaxHP", 10f),
            },
        };
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
