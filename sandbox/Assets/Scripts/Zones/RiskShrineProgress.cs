/// <summary>
/// Risk-shrine choice state (cinderreach-risk-shrines S1). One choice per run:
/// instant heal to max HP, or an empowered elite fight for a guaranteed rare.
/// Older saves start <see cref="RiskShrineState.Unchosen"/>; a new run calls
/// <see cref="Reset"/>. The payout is atomic: the item and
/// <see cref="RiskShrineState.RewardClaimed"/> persist together, a write
/// failure rolls the bag insertion back and keeps the claim open, and a full
/// bag keeps the claim open too.
/// </summary>
public enum RiskShrineState
{
    Unchosen,
    Healed,
    ChallengeActive,
    RewardAvailable,
    RewardClaimed,
}

public sealed class RiskShrineProgress
{
    public RiskShrineState State;
    public ItemInstance PendingReward;
    public int ChallengeSeed = -1;

    public bool CanChoose => State == RiskShrineState.Unchosen;

    /// <summary>Take the heal path. False unless the shrine is still unchosen.</summary>
    public bool ChooseHeal()
    {
        if (!CanChoose) return false;
        State = RiskShrineState.Healed;
        return true;
    }

    /// <summary>Take the challenge path, seeded for the deterministic rare. False unless unchosen.</summary>
    public bool ChooseChallenge(int runSeed)
    {
        if (!CanChoose) return false;
        State = RiskShrineState.ChallengeActive;
        ChallengeSeed = runSeed;
        return true;
    }

    /// <summary>Elite down: the rolled rare waits at the shrine. No-op unless the challenge is active.</summary>
    public void NotifyEliteKilled(ItemInstance reward)
    {
        if (State != RiskShrineState.ChallengeActive) return;
        State = RiskShrineState.RewardAvailable;
        PendingReward = reward;
    }

    /// <summary>
    /// Claim the waiting rare into the hero's bag and persist item + claim
    /// together. False when nothing waits, the bag is full (claim stays open),
    /// or the save write fails (bag insertion is rolled back, claim stays open).
    /// </summary>
    public bool TryClaim(UnityEngine.GameObject hero)
    {
        return TryClaim(hero, () => SaveService.SaveNow(hero));
    }

    /// <summary>Test seam: persist step is injectable so write failures are provable.</summary>
    public bool TryClaim(UnityEngine.GameObject hero, System.Action persist)
    {
        if (State != RiskShrineState.RewardAvailable || PendingReward == null) return false;
        if (hero == null) return false;
        var inv = hero.GetComponent<Inventory>();
        if (inv == null) return false;
        if (!inv.Add(PendingReward)) return false; // full bag: claim stays open
        // Flip first so the persisted snapshot already carries the claim;
        // a write failure below undoes both the bag and the flip.
        var reward = PendingReward;
        PendingReward = null;
        State = RiskShrineState.RewardClaimed;
        try
        {
            persist?.Invoke();
        }
        catch (System.Exception)
        {
            State = RiskShrineState.RewardAvailable;
            PendingReward = reward;
            inv.RemoveAt(inv.Count - 1); // roll back the provisional insert
            return false;
        }
        return true;
    }

    /// <summary>New run: the shrine offers its choice again.</summary>
    public void Reset()
    {
        State = RiskShrineState.Unchosen;
        PendingReward = null;
        ChallengeSeed = -1;
    }

    /// <summary>
    /// Deterministic rare for a run seed (reward side, no scene needed).
    /// Same seed always yields the same slot, base and affixes.
    /// </summary>
    public static ItemInstance RollRare(int runSeed)
    {
        var rng = new System.Random(runSeed ^ 0x51ab);
        var slots = new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Helmet };
        var slot = slots[rng.Next(slots.Length)];
        var bases = ItemDatabase.GetBaseTypes(slot);
        var item = new ItemInstance
        {
            slot = slot,
            rarity = ItemRarity.Rare,
            baseType = bases[rng.Next(bases.Count)],
            affixes = new System.Collections.Generic.List<Affix>(),
        };
        string[] pool = { "Damage", "MaxHP", "Armor", "Crit" };
        string first = pool[rng.Next(pool.Length)];
        string second = pool[rng.Next(pool.Length)];
        item.affixes.Add(new Affix(first, 1f + (float)rng.NextDouble() * 9f));
        item.affixes.Add(new Affix(second, 1f + (float)rng.NextDouble() * 9f));
        return item;
    }
}
