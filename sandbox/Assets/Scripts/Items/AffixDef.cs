/// <summary>
/// One affix row (replayability S2): string key (save-compatible with
/// <see cref="Affix.key"/>), allowed slots and two roll tiers. Tier 1 rolls on
/// Normal, tier 2 on Kindled (wired in S4). Integer affixes roll whole numbers.
/// </summary>
public sealed class AffixDef
{
    public readonly string key;
    public readonly ItemSlot[] slots;
    public readonly float t1Min, t1Max, t2Min, t2Max;
    public readonly bool integer;
    /// <summary>Player-facing line, {0} = value (e.g. "+{0} Damage").</summary>
    public readonly string format;

    public AffixDef(string key, ItemSlot[] slots, float t1Min, float t1Max, float t2Min, float t2Max,
        bool integer, string format)
    {
        this.key = key;
        this.slots = slots;
        this.t1Min = t1Min;
        this.t1Max = t1Max;
        this.t2Min = t2Min;
        this.t2Max = t2Max;
        this.integer = integer;
        this.format = format;
    }

    public bool AllowedOn(ItemSlot slot) => System.Array.IndexOf(slots, slot) >= 0;

    public void Range(int tier, out float min, out float max)
    {
        if (tier >= 2) { min = t2Min; max = t2Max; }
        else { min = t1Min; max = t1Max; }
    }

    /// <summary>Roll a value inside the tier span (inclusive for integers).</summary>
    public float Roll(int tier, System.Random rng)
    {
        Range(tier, out float min, out float max);
        if (integer) return rng.Next((int)min, (int)max + 1);
        return (float)(min + rng.NextDouble() * (max - min));
    }

    public string Describe(float value) =>
        string.Format(format, integer ? value.ToString("0") : value.ToString("0.##"));
}
