using System.Collections.Generic;

/// <summary>
/// Affix table (replayability S2, S3 adds CritRating + IncreasedArea): 16
/// affixes with tier-1 (Normal) and tier-2 (Kindled) spans. Keys stay strings
/// for save compatibility. Tier-1 spans of the seven P2 affixes keep their
/// old ranges.
/// Magic rolls 1–2 affixes, Rare 3–6, Unique items are fixed sets.
/// </summary>
public static class AffixLibrary
{
    public const string Damage = "Damage";
    public const string AttackSpeed = "AttackSpeed";
    public const string Crit = "Crit";
    public const string MaxHP = "MaxHP";
    public const string Armor = "Armor";
    public const string MoveSpeed = "MoveSpeed";
    public const string XPMul = "XPMul";
    public const string LifeSteal = "LifeSteal";
    public const string GoldFind = "GoldFind";
    public const string SkillRank = "SkillRank";
    public const string CooldownReduction = "CooldownReduction";
    public const string FireDamage = "FireDamage";
    public const string Thorns = "Thorns";
    public const string MagicFind = "MagicFind";
    public const string CritRating = "CritRating";
    public const string IncreasedArea = "IncreasedArea";

    static readonly ItemSlot[] W = { ItemSlot.Weapon };
    static readonly ItemSlot[] A = { ItemSlot.Armor };
    static readonly ItemSlot[] H = { ItemSlot.Helmet };
    static readonly ItemSlot[] WH = { ItemSlot.Weapon, ItemSlot.Helmet };
    static readonly ItemSlot[] AH = { ItemSlot.Armor, ItemSlot.Helmet };

    public static readonly AffixDef[] All =
    {
        new AffixDef(Damage, W, 1, 5, 4, 9, true, "+{0} Damage"),
        new AffixDef(AttackSpeed, W, 5, 15, 12, 25, true, "+{0}% Attack Speed"),
        new AffixDef(Crit, WH, 2, 10, 6, 15, true, "+{0}% Crit"),
        new AffixDef(MaxHP, AH, 5, 20, 15, 35, true, "+{0} Life"),
        new AffixDef(Armor, A, 1, 3, 3, 6, true, "+{0} Armor"),
        new AffixDef(MoveSpeed, A, 0.1f, 0.3f, 0.25f, 0.5f, false, "+{0} Move Speed"),
        new AffixDef(XPMul, H, 5, 15, 10, 25, true, "+{0}% Experience"),
        new AffixDef(LifeSteal, W, 2, 6, 5, 10, true, "{0}% Life Steal"),
        new AffixDef(GoldFind, AH, 10, 30, 25, 60, true, "+{0}% Gold Find"),
        new AffixDef(SkillRank, H, 1, 1, 1, 2, true, "+{0} to all Skills"),
        new AffixDef(CooldownReduction, H, 3, 8, 6, 15, true, "-{0}% Cooldowns"),
        new AffixDef(FireDamage, W, 1, 4, 3, 8, true, "+{0} Fire Damage"),
        new AffixDef(Thorns, A, 1, 2, 2, 4, true, "{0} Thorns"),
        new AffixDef(MagicFind, AH, 5, 20, 15, 40, true, "+{0}% Magic Find"),
        // S3: CritRating rides the Crit slots (WH) at 10 rating = 1 crit point;
        // IncreasedArea is utility, so it rides Armor/Helmet (AH) and stays off
        // weapons to keep the weapon pool damage-focused. Both integer, save
        // keys are plain strings like every other affix.
        new AffixDef(CritRating, WH, 5, 20, 15, 40, true, "+{0} Crit Rating"),
        new AffixDef(IncreasedArea, AH, 5, 20, 15, 40, true, "+{0}% Area"),
    };

    public static int MagicAffixCount(System.Random rng) => rng.Next(1, 3); // 1-2
    public static int RareAffixCount(System.Random rng) => rng.Next(3, 7); // 3-6

    public static AffixDef Get(string key)
    {
        foreach (var d in All)
            if (d.key == key) return d;
        return null;
    }

    public static List<string> PoolFor(ItemSlot slot)
    {
        var pool = new List<string>();
        foreach (var d in All)
            if (d.AllowedOn(slot)) pool.Add(d.key);
        return pool;
    }

    /// <summary>Tier-1 roll (P2 call sites).</summary>
    public static float RollValue(string key, System.Random rng) => RollValue(key, 1, rng);

    public static (float min, float max) GetRange(string key, int itemLevel, int affixTier)
    {
        var def = Get(key);
        if (def == null) return (0f, 0f);
        def.Range(affixTier, itemLevel, out float min, out float max);
        return (min, max);
    }

    public static float RollValue(string key, int tier, System.Random rng, int itemLevel = 0)
    {
        var def = Get(key);
        return def != null ? def.Roll(tier, itemLevel, rng) : 0f;
    }

    /// <summary>Tier comes from the run profile (Kindled = 2, wired in S4).</summary>
    public static List<Affix> RollAffixes(ItemSlot slot, ItemRarity rarity, System.Random rng) =>
        RollAffixes(slot, rarity, rng, 1);

    public static List<Affix> RollAffixes(ItemSlot slot, ItemRarity rarity, System.Random rng, int tier, int itemLevel = 0)
    {
        var result = new List<Affix>();
        if (rarity == ItemRarity.Unique) return result;
        int count = rarity == ItemRarity.Magic ? MagicAffixCount(rng) : RareAffixCount(rng);
        var pool = PoolFor(slot);
        for (int i = 0; i < count; i++)
        {
            string key = pool[rng.Next(pool.Count)];
            result.Add(new Affix(key, RollValue(key, tier, rng, itemLevel)));
        }
        return result;
    }

    /// <summary>Player-facing text of one affix (falls back to "+v key").</summary>
    public static string Describe(Affix a)
    {
        var def = Get(a.key);
        return def != null ? def.Describe(a.value) : $"+{a.value:0.#} {a.key}";
    }
}
