/// <summary>Enemy tiers for drop rolls (P2).</summary>
public enum LootTier { Normal, Elite, Chest, Boss }

/// <summary>Loot roll result: rarity + slot (drops=false means no loot).</summary>
public struct LootRoll
{
    public bool drops;
    public ItemRarity rarity;
    public ItemSlot slot;
}

/// <summary>Weighted drop tables per tier, seeded RNG for tests.</summary>
public static class LootTable
{
    // Weights sum to 100 per tier (pinned by DiabloP2ConfigTests).
    public static void WeightsFor(LootTier tier, out int magic, out int rare, out int unique)
    {
        switch (tier)
        {
            case LootTier.Boss:
                magic = 0; rare = 70; unique = 30; return;
            case LootTier.Elite:
                magic = 50; rare = 40; unique = 10; return;
            case LootTier.Chest:
                magic = 0; rare = 80; unique = 20; return;
            default:
                magic = 80; rare = 18; unique = 2; return;
        }
    }

    public static int DropChanceFor(LootTier tier)
    {
        return tier == LootTier.Normal ? 70 : 100; // Boss always drops (Rare+)
    }

    public static LootRoll Roll(LootTier tier, System.Random rng)
    {
        var roll = new LootRoll();
        if (rng.Next(100) >= DropChanceFor(tier)) return roll;
        roll.drops = true;
        WeightsFor(tier, out int magic, out int rare, out int unique);
        int pick = rng.Next(100);
        roll.rarity = pick < magic ? ItemRarity.Magic
            : pick < magic + rare ? ItemRarity.Rare : ItemRarity.Unique;
        roll.slot = (ItemSlot)rng.Next(3);
        return roll;
    }
}
