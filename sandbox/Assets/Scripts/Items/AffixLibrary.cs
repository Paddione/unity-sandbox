using System.Collections.Generic;

/// <summary>Affix pools per slot with roll ranges (P2 greybox tuning).</summary>
public static class AffixLibrary
{
    public static int MagicAffixCount(System.Random rng) => rng.Next(1, 3); // 1-2
    public static int RareAffixCount(System.Random rng) => rng.Next(3, 7); // 3-6

    public static List<string> PoolFor(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Weapon: return new List<string> { "Damage", "AttackSpeed", "Crit" };
            case ItemSlot.Armor: return new List<string> { "MaxHP", "Armor", "MoveSpeed" };
            default: return new List<string> { "XPMul", "Crit", "MaxHP" };
        }
    }

    public static float RollValue(string key, System.Random rng)
    {
        switch (key)
        {
            case "Damage": return rng.Next(1, 6);
            case "AttackSpeed": return rng.Next(5, 16);
            case "Crit": return rng.Next(2, 11);
            case "MaxHP": return rng.Next(5, 21);
            case "Armor": return rng.Next(1, 4);
            case "MoveSpeed": return (float)(rng.NextDouble() * 0.2 + 0.1);
            case "XPMul": return rng.Next(5, 16);
            default: return 0f;
        }
    }

    public static List<Affix> RollAffixes(ItemSlot slot, ItemRarity rarity, System.Random rng)
    {
        var result = new List<Affix>();
        if (rarity == ItemRarity.Unique) return result;
        int count = rarity == ItemRarity.Magic ? MagicAffixCount(rng) : RareAffixCount(rng);
        var pool = PoolFor(slot);
        for (int i = 0; i < count; i++)
        {
            string key = pool[rng.Next(pool.Count)];
            result.Add(new Affix(key, RollValue(key, rng)));
        }
        return result;
    }
}
