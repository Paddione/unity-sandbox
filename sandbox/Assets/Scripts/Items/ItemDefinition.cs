using System.Collections.Generic;

public enum ItemSlot { Armor, Weapon, Helmet }

public enum ItemRarity { Magic, Rare, Unique }

/// <summary>Single stat modifier, e.g. Damage +3 or Crit +8%.</summary>
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

/// <summary>Runtime item instance (P2: local data, no network sync yet).</summary>
public sealed class ItemInstance
{
    public ItemSlot slot;
    public ItemRarity rarity;
    public string baseType;
    public List<Affix> affixes = new List<Affix>();
    public string uniqueId; // only set for Unique

    public float GetAffix(string key)
    {
        float total = 0f;
        foreach (var a in affixes)
            if (a.key == key) total += a.value;
        return total;
    }
}
