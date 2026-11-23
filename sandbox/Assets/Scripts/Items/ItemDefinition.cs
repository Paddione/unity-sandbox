using System.Collections.Generic;

public enum ItemSlot { Armor, Weapon, Helmet, Consumable }

public enum ItemRarity { Magic, Rare, Unique }

/// <summary>Single stat modifier, e.g. Damage +3 or Crit +8%.</summary>
[System.Serializable]
public sealed class Affix
{
    public string key;
    public float value;

    public Affix(string key, float value)
    {
        this.key = key;
        this.value = value;
    }
}

/// <summary>
/// Runtime item instance (P2: local data, no network sync yet).
/// Serializable so builder-equipped gear (starter sword) survives in scenes.
/// </summary>
[System.Serializable]
public sealed class ItemInstance
{
    public ItemSlot slot;
    public ItemRarity rarity;
    public string baseType;
    public List<Affix> affixes = new List<Affix>();
    public string uniqueId; // only set for Unique
    /// <summary>
    /// Affix-Stufe der ursprünglichen Beute (1 = Normal, 2 = Kindled).
    /// Wird am Erzeugungspunkt (Drop/Vendor) erfasst; alte Einträge
    /// migriert die Save-Migration auf 1. Annahme: historische Kindled-Funde
    /// sind nicht mehr unterscheidbar und zählen als Tier 1.
    /// </summary>
    public int affixTier = 1;
    /// <summary>
    /// Umschmiedeter Affix-Slot + 1 (0 = ungebunden). Pro Gegenstand ist
    /// dauerhaft genau ein Slot wählbar, der Rest bleibt erhalten.
    /// </summary>
    public int reforgeIndexPlusOne;

    /// <summary>Source level, or zero for historical and unrolled items.</summary>
    public int itemLevel;
    public string instanceId;

    public void EnsureIdentity()
    {
        if (string.IsNullOrEmpty(instanceId)) instanceId = System.Guid.NewGuid().ToString("N");
    }

    public float GetAffix(string key)
    {
        float total = 0f;
        foreach (var a in affixes)
            if (a.key == key) total += a.value;
        return total;
    }
}

/// <summary>Display names of item enums (cinderreach-deutsch-story); enum names stay save/net ids.</summary>
public static class ItemText
{
    public static string Rarity(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Magic: return Loc.T("item.rarity.magic");
            case ItemRarity.Rare: return Loc.T("item.rarity.rare");
            default: return Loc.T("item.rarity.unique");
        }
    }

    public static string Slot(ItemSlot s)
    {
        switch (s)
        {
            case ItemSlot.Armor: return Loc.T("item.slot.armor");
            case ItemSlot.Weapon: return Loc.T("item.slot.weapon");
            case ItemSlot.Helmet: return Loc.T("item.slot.helmet");
            default: return Loc.T("item.slot.consumable");
        }
    }
}
