/// <summary>Enemy tiers for drop rolls (P2).</summary>
public enum LootTier { Normal, Elite, Chest, Boss, ActBoss }

/// <summary>Loot roll result: rarity + slot (drops=false means no loot).</summary>
public struct LootRoll
{
    public bool drops;
    public ItemRarity rarity;
    public ItemSlot slot;
    /// <summary>Affix-Stufe des Funds (1 = Normal, 2 = Kindled).</summary>
    public int affixTier;
}

/// <summary>Weighted drop tables per tier, seeded RNG for tests.</summary>
public static class LootTable
{
    // Weights sum to 100 per tier (pinned by DiabloP2ConfigTests). Playtest
    // feedback 2026-11-23: no blue (Magic) drops any more; the old Magic share
    // folded into Rare, Unique odds unchanged. The magic out-param stays 0 so
    // callers and the Magic Find formula keep their shape.
    public static void WeightsFor(LootTier tier, out int magic, out int rare, out int unique)
    {
        switch (tier)
        {
            case LootTier.Boss:
                magic = 0; rare = 70; unique = 30; return;
            case LootTier.ActBoss:
                // act-one S4: Kilnmother, Rare+ with a heavier unique roll.
                magic = 0; rare = 60; unique = 40; return;
            case LootTier.Elite:
                magic = 0; rare = 90; unique = 10; return;
            case LootTier.Chest:
                magic = 0; rare = 80; unique = 20; return;
            default:
                magic = 0; rare = 98; unique = 2; return;
        }
    }

    public static int DropChanceFor(LootTier tier)
    {
        return tier == LootTier.Normal ? 70 : 100; // Boss always drops (Rare+)
    }

    public static LootRoll Roll(LootTier tier, System.Random rng) => Roll(tier, rng, 0f);

    /// <summary>
    /// Magic Find (replayability S2) scales the unique weight by (1 + mf/100);
    /// rare is the floor since Magic was retired (2026-11-23) and keeps its
    /// weight, so Magic Find still shifts drops toward Uniques. The pick runs
    /// over the new total. mf = 0 is exactly the base table.
    /// </summary>
    public static void WeightsWithMagicFind(LootTier tier, float magicFind,
        out float magic, out float rare, out float unique)
    {
        WeightsFor(tier, out int m, out int r, out int u);
        float mf = magicFind > 0f ? magicFind : 0f;
        magic = m;
        rare = r;
        unique = u * (1f + mf / 100f);
    }

    public static LootRoll Roll(LootTier tier, System.Random rng, float magicFind) =>
        Roll(tier, rng, magicFind, 1f);

    /// <summary>Full roll: Magic Find plus the difficulty unique multiplier (Kindled ×2).</summary>
    public static LootRoll Roll(LootTier tier, System.Random rng, float magicFind, float uniqueMul)
    {
        var roll = new LootRoll();
        // Affix-Stufe am Erzeugungspunkt erfassen (smithing S1).
        roll.affixTier = DifficultyProfile.Current.affixTier;
        if (rng.Next(100) >= DropChanceFor(tier)) return roll;
        roll.drops = true;
        if (magicFind <= 0f && uniqueMul == 1f)
        {
            // Exact P2 path (same RNG draws), so seeded tests and drops stay stable.
            WeightsFor(tier, out int m, out int r, out _);
            int ipick = rng.Next(100);
            roll.rarity = ipick < m ? ItemRarity.Magic : ipick < m + r ? ItemRarity.Rare : ItemRarity.Unique;
            roll.slot = PickSlot(rng);
            return roll;
        }
        WeightsWithMagicFind(tier, magicFind, out float magic, out float rare, out float unique);
        unique *= uniqueMul > 0f ? uniqueMul : 1f;
        double pick = rng.NextDouble() * (magic + rare + unique);
        roll.rarity = pick < magic ? ItemRarity.Magic
            : pick < magic + rare ? ItemRarity.Rare : ItemRarity.Unique;
        roll.slot = PickSlot(rng);
        return roll;
    }

    /// <summary>
    /// gear-slots S6 slot weights (sum 100): weapon 25, armor 20, helmet 15,
    /// gloves 15, boots 15, belt 10. The rarity weights above stay untouched,
    /// so six slots never thin out the per-tier Unique share.
    /// </summary>
    public static readonly (ItemSlot slot, int weight)[] SlotWeights =
    {
        (ItemSlot.Weapon, 25), (ItemSlot.Armor, 20), (ItemSlot.Helmet, 15),
        (ItemSlot.Gloves, 15), (ItemSlot.Boots, 15), (ItemSlot.Belt, 10),
    };

    public static ItemSlot PickSlot(System.Random rng)
    {
        int pick = rng.Next(100);
        foreach (var (slot, weight) in SlotWeights)
        {
            if (pick < weight) return slot;
            pick -= weight;
        }
        return ItemSlot.Weapon;
    }

    /// <summary>Rare drops in a row after which the next item drop is forced Unique.</summary>
    public const int PityThreshold = 40;

    /// <summary>
    /// Bad-luck protection (gear-slots S6): <paramref name="rareStreak"/> counts
    /// item drops since the last Unique. Once it reaches
    /// <see cref="PityThreshold"/> the next drop becomes Unique; any Unique
    /// resets it. No-drop rolls leave it alone. Magic Find and Kindled never
    /// touch the counter, they only shape the roll that comes in.
    /// </summary>
    public static LootRoll ApplyPity(LootRoll roll, ref int rareStreak)
    {
        if (!roll.drops) return roll;
        if (roll.rarity != ItemRarity.Unique && rareStreak >= PityThreshold)
            roll.rarity = ItemRarity.Unique;
        if (roll.rarity == ItemRarity.Unique) rareStreak = 0;
        else rareStreak++;
        return roll;
    }
}

/// <summary>
/// Hero-wide pity counter (gear-slots S6, save v15 <c>uniquePity</c>). Loot
/// rolls on the host for the local hero, like Magic Find; the counter rides
/// the local save and resets on load of a fresh hero.
/// </summary>
public static class UniquePity
{
    public static int Current;

    public static LootRoll Apply(LootRoll roll) => LootTable.ApplyPity(roll, ref Current);
}
