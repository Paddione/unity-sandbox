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
    public const string NightfangId = ItemDatabase.NightfangId;
    public const string BulwarkCarrierId = ItemDatabase.BulwarkCarrierId;
    public const string SeismicCrownId = ItemDatabase.SeismicCrownId;
    public const string MercyShroudId = ItemDatabase.MercyShroudId;
    public const string DawnCircleId = ItemDatabase.DawnCircleId;
    public const string ShadowstrideVestId = ItemDatabase.ShadowstrideVestId;
    public const string KeenCadenceId = ItemDatabase.KeenCadenceId;

    /// <summary>Tank-stun bonus on the slam primary (S4): +2 s, primary only, no AoE creep.</summary>
    public const float SeismicCrownBonusStun = 2f;
    /// <summary>Mending-shield absorb granted by the Lanternbearer armor (S4).</summary>
    public const float MercyShieldAmount = 5f;
    /// <summary>Mending-shield lifetime in seconds (S4).</summary>
    public const float MercyShieldSeconds = 4f;
    /// <summary>Rogue-haste move factor granted by flurry hits (S4).</summary>
    public const float ShadowstrideHasteFactor = 1.3f;
    /// <summary>Rogue-haste lifetime in seconds, refreshed per flurry hit (S4).</summary>
    public const float ShadowstrideHasteSeconds = 3f;
    /// <summary>Rogue-helm cooldown cut on the first crit of a special cast (S4).</summary>
    public const float KeenCooldownCut = 2f;

    /// <summary>Player-facing effect line for tooltips (empty for other items).</summary>
    public static string EffectText(string uniqueId)
    {
        if (uniqueId == LancerWardId)
            return Loc.T("unique.effect.lancerward");
        if (uniqueId == WickOfHaloId)
            return Loc.T("unique.effect.wick");
        if (uniqueId == NightfangId)
            return Loc.T("unique.effect.nightfang");
        if (uniqueId == BulwarkCarrierId)
            return Loc.T("unique.effect.bulwark");
        if (uniqueId == SeismicCrownId)
            return Loc.T("unique.effect.seismiccrown");
        if (uniqueId == MercyShroudId)
            return Loc.T("unique.effect.mercyshroud");
        if (uniqueId == DawnCircleId)
            return Loc.T("unique.effect.dawncircle");
        if (uniqueId == ShadowstrideVestId)
            return Loc.T("unique.effect.shadowstride");
        if (uniqueId == KeenCadenceId)
            return Loc.T("unique.effect.keencadence");
        return GearUniqueEffects.EffectText(uniqueId);
    }

    public static string EffectTextFor(ItemInstance item) =>
        item != null && item.rarity == ItemRarity.Unique ? EffectText(item.uniqueId) : "";

    public static bool HasLancerWard(Equipment eq) =>
        eq != null && eq.weapon != null && eq.weapon.uniqueId == LancerWardId;

    public static bool HasWickOfHalo(Equipment eq) =>
        eq != null && eq.weapon != null && eq.weapon.uniqueId == WickOfHaloId;

    public static bool HasNightfang(Equipment eq) =>
        eq != null && eq.weapon != null && eq.weapon.uniqueId == NightfangId;

    public static bool HasBulwark(Equipment eq) =>
        eq != null && eq.armor != null && eq.armor.uniqueId == BulwarkCarrierId;

    public static bool HasSeismicCrown(Equipment eq) =>
        eq != null && eq.helmet != null && eq.helmet.uniqueId == SeismicCrownId;

    public static bool HasMercyShroud(Equipment eq) =>
        eq != null && eq.armor != null && eq.armor.uniqueId == MercyShroudId;

    public static bool HasDawnCircle(Equipment eq) =>
        eq != null && eq.helmet != null && eq.helmet.uniqueId == DawnCircleId;

    public static bool HasShadowstride(Equipment eq) =>
        eq != null && eq.armor != null && eq.armor.uniqueId == ShadowstrideVestId;

    public static bool HasKeenCadence(Equipment eq) =>
        eq != null && eq.helmet != null && eq.helmet.uniqueId == KeenCadenceId;

    // ---------- S4: slam primary, block threat, quick-shot pierce ----------

    /// <summary>
    /// Stun seconds for one slam target: base for the pack, base +
    /// <see cref="SeismicCrownBonusStun"/> for the primary (nearest) target
    /// while the crown is worn. Read live per cast: unequipping ends the bonus
    /// immediately, a reload keeps it only while the helm stays equipped.
    /// </summary>
    public static float SlamStunFor(Equipment eq, float baseSeconds, bool isPrimary) =>
        isPrimary && HasSeismicCrown(eq) ? baseSeconds + SeismicCrownBonusStun : baseSeconds;

    /// <summary>
    /// Quick Shot pierce bonus (S4): +1 with Nightfang, else 0. Base arrows
    /// never pierce; only the rogue specialty gains the extra target.
    /// </summary>
    public static int QuickShotPierce(Equipment eq) => HasNightfang(eq) ? 1 : 0;

    /// <summary>
    /// Bulwark threat (S4): books the slam-magnitude bonus
    /// (<see cref="ThreatTable.SlamThreatBonus"/>, 20) on the attacker's table
    /// with the blocking hero as source. Stateless per event: two wearers book
    /// their own threat on their own attackers, no shared state.
    /// </summary>
    public static void BookBulwarkThreat(GameObject attackerHusk, Transform hero)
    {
        if (attackerHusk == null || hero == null) return;
        ThreatTable.BookStun(attackerHusk, hero);
    }

    /// <summary>
    /// Weakest valid dawn-circle bonus target (S4): living, in radius, still
    /// injured after the circle — lowest current HP wins, ties go to the
    /// nearest. The healer herself qualifies when she is the weakest. Null
    /// when nobody is injured (no wasted impulse, never stacking: one bonus
    /// per cast by construction).
    /// </summary>
    public static PlayerHealth WeakestAlly(Transform caster, float radius)
    {
        if (caster == null) return null;
        PlayerHealth best = null;
        foreach (var hp in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (hp == null || hp.CurrentHP <= 0f || hp.CurrentHP >= hp.MaxHP) continue;
            var to = hp.transform.position - caster.position;
            to.y = 0f;
            if (to.magnitude > radius) continue;
            if (best == null || hp.CurrentHP < best.CurrentHP) best = hp;
        }
        return best;
    }

    // ---------- S4: rogue haste (wearer-bound, timed) ----------

    /// <summary>
    /// Grant the shadowstride haste on its wearer (S4): +30 % walk speed for
    /// <see cref="ShadowstrideHasteSeconds"/> s, refreshed per flurry hit.
    /// Per-wearer state lives on the hero's component (no static cross-owner
    /// state); the component ends the haste early when the vest is taken off
    /// and dies with the hero on reload. Returns false without a controller.
    /// Duration/reset rule: refresh on every flurry hit, restore base speed on
    /// expiry or unequip.
    /// </summary>
    public static bool GrantShadowstrideHaste(GameObject hero)
    {
        if (hero == null) return false;
        if (!HasShadowstride(hero.GetComponent<Equipment>())) return false;
        var haste = hero.GetComponent<ShadowstrideHaste>();
        if (haste != null) { haste.Refresh(); return true; }
        var controller = hero.GetComponent<DiabloClickController>();
        if (controller == null) return false;
        haste = hero.AddComponent<ShadowstrideHaste>();
        haste.Begin(controller);
        return true;
    }

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

/// <summary>
/// Shadowstride haste carrier (S4): lives on the hasted hero, multiplies its
/// <see cref="DiabloClickController.walkSpeed"/> for
/// <see cref="BuildUniqueEffects.ShadowstrideHasteSeconds"/> s and restores
/// the base speed on expiry, on vest unequip, or on destroy (reload).
/// One component per hero: two wearers never share state.
/// </summary>
public sealed class ShadowstrideHaste : MonoBehaviour
{
    float until = float.NegativeInfinity;
    float baseWalk;
    bool armed;

    /// <summary>Start the haste clock (captures the current walk speed as base).</summary>
    public void Begin(DiabloClickController controller)
    {
        if (controller == null) { Destroy(this); return; }
        baseWalk = controller.walkSpeed;
        controller.walkSpeed = baseWalk * BuildUniqueEffects.ShadowstrideHasteFactor;
        until = Time.time + BuildUniqueEffects.ShadowstrideHasteSeconds;
        armed = true;
    }

    /// <summary>Refresh the 3 s window (per flurry hit, keeps the same base).</summary>
    public void Refresh() => until = Time.time + BuildUniqueEffects.ShadowstrideHasteSeconds;

    /// <summary>Seconds left on the haste (0 when expired).</summary>
    public float SecondsLeft => armed ? Mathf.Max(0f, until - Time.time) : 0f;

    void Update()
    {
        var controller = GetComponent<DiabloClickController>();
        var eq = GetComponent<Equipment>();
        // Unequip ends the effect early: the vest must stay on to keep running.
        if (controller == null || !BuildUniqueEffects.HasShadowstride(eq) || Time.time >= until)
        {
            if (controller != null && armed) controller.walkSpeed = baseWalk;
            armed = false;
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        // Reload/scene safety: never leak a boosted speed onto a fresh hero.
        if (!armed) return;
        var controller = GetComponent<DiabloClickController>();
        if (controller != null && Mathf.Approximately(controller.walkSpeed, baseWalk * BuildUniqueEffects.ShadowstrideHasteFactor))
            controller.walkSpeed = baseWalk;
        armed = false;
    }
}
