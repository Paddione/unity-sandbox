using System.Collections.Generic;

/// <summary>
/// Affix table (replayability S2, S3 adds CritRating + IncreasedArea, gear-slots S2 FlaskHealing): 17
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
    /// <summary>gear-slots S2: percent more healing from the flask (belts only).</summary>
    public const string FlaskHealing = "FlaskHealing";

    static readonly ItemSlot[] W = { ItemSlot.Weapon };
    static readonly ItemSlot[] H = { ItemSlot.Helmet };
    static readonly ItemSlot[] L = { ItemSlot.Belt };
    // gear-slots S1: gloves ride the offensive keys, boots the defensive and
    // movement keys, belts life, cooldown, area and the flask affix.
    static readonly ItemSlot[] WG = { ItemSlot.Weapon, ItemSlot.Gloves };
    static readonly ItemSlot[] WHG = { ItemSlot.Weapon, ItemSlot.Helmet, ItemSlot.Gloves };
    static readonly ItemSlot[] AB = { ItemSlot.Armor, ItemSlot.Boots };
    static readonly ItemSlot[] AHBL = { ItemSlot.Armor, ItemSlot.Helmet, ItemSlot.Boots, ItemSlot.Belt };
    static readonly ItemSlot[] AHG = { ItemSlot.Armor, ItemSlot.Helmet, ItemSlot.Gloves };
    static readonly ItemSlot[] AHB = { ItemSlot.Armor, ItemSlot.Helmet, ItemSlot.Boots };
    static readonly ItemSlot[] HL = { ItemSlot.Helmet, ItemSlot.Belt };
    static readonly ItemSlot[] AHL = { ItemSlot.Armor, ItemSlot.Helmet, ItemSlot.Belt };

    public static readonly AffixDef[] All =
    {
        new AffixDef(Damage, W, 1, 5, 4, 9, true),
        new AffixDef(AttackSpeed, WG, 5, 15, 12, 25, true),
        new AffixDef(Crit, WHG, 2, 10, 6, 15, true),
        new AffixDef(MaxHP, AHBL, 5, 20, 15, 35, true),
        new AffixDef(Armor, AB, 1, 3, 3, 6, true),
        new AffixDef(MoveSpeed, AB, 0.1f, 0.3f, 0.25f, 0.5f, false),
        new AffixDef(XPMul, H, 5, 15, 10, 25, true),
        new AffixDef(LifeSteal, W, 2, 6, 5, 10, true),
        new AffixDef(GoldFind, AHG, 10, 30, 25, 60, true),
        new AffixDef(SkillRank, H, 1, 1, 1, 2, true),
        new AffixDef(CooldownReduction, HL, 3, 8, 6, 15, true),
        new AffixDef(FireDamage, WG, 1, 4, 3, 8, true),
        new AffixDef(Thorns, AB, 1, 2, 2, 4, true),
        new AffixDef(MagicFind, AHB, 5, 20, 15, 40, true),
        // S3: CritRating rides the Crit slots at 10 rating = 1 crit point;
        // IncreasedArea is utility, so it stays off weapons to keep the weapon
        // pool damage-focused. Both integer, save keys are plain strings.
        new AffixDef(CritRating, WHG, 5, 20, 15, 40, true),
        new AffixDef(IncreasedArea, AHL, 5, 20, 15, 40, true),
        new AffixDef(FlaskHealing, L, 5, 15, 10, 25, true),
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

    /// <summary>Short stat name of an affix key ("MaxHP" → "Leben"), catalog <c>stat.&lt;key&gt;</c>.</summary>
    public static string StatName(string key) => Loc.T("stat." + (key ?? "").ToLowerInvariant());

    /// <summary>Player-facing text of one affix (falls back to "+v key").</summary>
    public static string Describe(Affix a)
    {
        var def = Get(a.key);
        return def != null ? def.Describe(a.value) : $"+{a.value:0.#} {a.key}";
    }
}
