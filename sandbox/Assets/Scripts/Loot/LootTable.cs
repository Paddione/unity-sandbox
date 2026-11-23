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
            roll.slot = (ItemSlot)rng.Next(3);
            return roll;
        }
        WeightsWithMagicFind(tier, magicFind, out float magic, out float rare, out float unique);
        unique *= uniqueMul > 0f ? uniqueMul : 1f;
        double pick = rng.NextDouble() * (magic + rare + unique);
        roll.rarity = pick < magic ? ItemRarity.Magic
            : pick < magic + rare ? ItemRarity.Rare : ItemRarity.Unique;
        roll.slot = (ItemSlot)rng.Next(3);
        return roll;
    }
}
