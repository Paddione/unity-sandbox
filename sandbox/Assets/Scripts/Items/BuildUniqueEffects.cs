using UnityEngine;

/// <summary>
/// Build uniques (cinderreach-build-uniques S2): solo effects of the two
/// class-bound weapons. Reads the live <see cref="Equipment"/> every call, so
/// unequipping ends the effect immediately and a reload keeps it only while
/// the weapon stays equipped (no saved state; the halo window is runtime-only).
///
/// Defensive vs #9 (SkillVariantTargeting, not on main): effects are
/// variant-independent and use the base skill geometry (cleave radius, halo
/// radius). When SkillVariantTargeting lands, S3 can pass variant geometry in.
/// Defensive vs #8 (SmithingService, not on main): both weapons are
/// <see cref="ItemRarity"/>.Unique with fixed affix sets, so the standing
/// rarity rule (uniques are never reforged) already excludes them.
/// </summary>
public static class BuildUniqueEffects
{
    public const string LancerWardId = ItemDatabase.LancerWardId;
    public const string WickOfHaloId = ItemDatabase.WickOfHaloId;

    /// <summary>Player-facing effect line for tooltips (empty for other items).</summary>
    public static string EffectText(string uniqueId)
    {
        if (uniqueId == LancerWardId)
            return "Cleave interrupts one charging Lancer (it recovers exhausted).";
        if (uniqueId == WickOfHaloId)
            return "Wisps strike prey inside your burning halo first.";
        return "";
    }

    public static string EffectTextFor(ItemInstance item) =>
        item != null && item.rarity == ItemRarity.Unique ? EffectText(item.uniqueId) : "";

    public static bool HasLancerWard(Equipment eq) =>
        eq != null && eq.weapon != null && eq.weapon.uniqueId == LancerWardId;

    public static bool HasWickOfHalo(Equipment eq) =>
        eq != null && eq.weapon != null && eq.weapon.uniqueId == WickOfHaloId;

    // ---------- lancer-ward: at most one interrupt per Cleave cast ----------

    /// <summary>
    /// Interrupt at most one lancer mid-charge/telegraph inside the cleave
    /// radius; it drops into the existing Exhausted recovery. Returns the
    /// interrupted lancer (null without the ward or without a valid target).
    /// </summary>
    public static AshLancer TryInterruptCharge(Transform hero, Equipment eq, float radius)
    {
        if (hero == null || !HasLancerWard(eq)) return null;
        AshLancer best = null;
        float bestD = radius;
        foreach (var lancer in Object.FindObjectsOfType<AshLancer>())
        {
            if (lancer == null) continue;
            if (lancer.Current != AshLancer.State.Charge && lancer.Current != AshLancer.State.Telegraph)
                continue;
            var d = lancer.transform.position - hero.position;
            d.y = 0f;
            if (d.magnitude <= bestD) { bestD = d.magnitude; best = lancer; }
        }
        if (best != null) best.InterruptToExhausted();
        return best;
    }

    // ---------- wick-of-the-halo: Wisp priority inside the halo ----------

    static Vector3 haloCenter;
    static float haloRadius;
    static float haloUntil = float.NegativeInfinity;

    /// <summary>Active halo window for Wisp priority (called by DoHalo).</summary>
    public static void NotifyHalo(Vector3 center, float radius, float duration)
    {
        haloCenter = center;
        haloRadius = radius;
        haloUntil = Time.time + duration;
    }

    /// <summary>True while a halo cast by the hero still burns (runtime-only: a reload clears it).</summary>
    public static bool HaloActiveAt(Vector3 pos, float now) =>
        now <= haloUntil && (pos - haloCenter).sqrMagnitude <= haloRadius * haloRadius + 0.0001f;

    public static bool AnyHaloActive(float now) => now <= haloUntil;

    /// <summary>Headless/test reset plus the reload semantic: no halo, no priority.</summary>
    public static void ResetRun()
    {
        haloUntil = float.NegativeInfinity;
    }

    /// <summary>
    /// Wisp target choice: with the lantern equipped and a burning halo, the
    /// nearest valid husk inside the halo wins (reach and line-of-sight rules
    /// unchanged — candidates are pre-filtered by the caller). Otherwise the
    /// caller's default (nearest) stands, returned as null.
    /// </summary>
    public static GameObject PreferHaloTarget(Equipment eq, System.Collections.Generic.List<GameObject> candidates, float now)
    {
        if (!HasWickOfHalo(eq) || !AnyHaloActive(now) || candidates == null) return null;
        GameObject best = null;
        float bestD = float.PositiveInfinity;
        foreach (var husk in candidates)
        {
            if (husk == null) continue;
            float d = (husk.transform.position - haloCenter).sqrMagnitude;
            if (!HaloActiveAt(husk.transform.position, now)) continue;
            if (d < bestD) { bestD = d; best = husk; }
        }
        return best;
    }
}
