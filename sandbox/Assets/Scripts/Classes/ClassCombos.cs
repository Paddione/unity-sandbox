using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class-kit combos (cinderreach-class-kits-parity). One source for every
/// tuning value and pure combo formula of the Fenwalker and Rogue kits, so
/// the runtime, the character sheet and the tests read the same numbers.
///
/// Fenwalker (tank): Mire Hook, Bog Charge and Mire Ground leave husks
/// <b>mired</b>; Seismic Slam holds mired husks twice as long (Fen Shackle);
/// Cleave shatters stunned husks (Shatter) and releases the Iron Vow store
/// (Retribution).
///
/// Rogue (dps): Venom Vial stacks venom that the Powder Keg burns off
/// (Venom Burn); Shadow Roll arms an Ambush (next hit crits); Smoke Pot
/// blinds husks and Blade Flurry cuts blinded husks harder (Throat Cut);
/// a Quick Shot through a keg ignites it at once (Spark Shot).
/// </summary>
public static class ClassCombos
{
    // ---------------- Fenwalker ----------------
    public const int MireHookDamage = 3;
    public const float MireHookRange = 10f;
    public const float MireHookHitRadius = 0.9f;
    /// <summary>Hooked husks land this far in front of the tank (inside Cleave reach).</summary>
    public const float MireHookPullTo = 1.6f;
    public const float MireHookCooldown = 5f;

    public const int BogChargeDamage = 4;
    public const float BogChargeDistance = 6f;
    /// <summary>Half width of the charge lane.</summary>
    public const float BogChargeWidth = 1.3f;
    public const float BogChargeKnockback = 0.6f;
    public const float BogChargeCooldown = 8f;

    public const int MireGroundDamage = 2;
    public const float MireGroundRadius = 4f;
    public const float MireGroundDuration = 6f;
    public const float MireGroundTick = 1f;
    public const float MireGroundCooldown = 14f;

    /// <summary>Mired lifetime after a hook, charge or pool tick.</summary>
    public const float MiredSeconds = 3f;
    /// <summary>Mired husks walk at half speed.</summary>
    public const float MiredMoveFactor = 0.5f;

    public const float IronVowSeconds = 6f;
    public const float IronVowShield = 10f;
    public const float IronVowTauntRadius = 8f;
    public const float IronVowCooldown = 16f;

    /// <summary>Shatter: Cleave on a stunned husk.</summary>
    public const float ShatterFactor = 1.5f;
    /// <summary>Shatter with lancer-ward equipped.</summary>
    public const float LancerShatterFactor = 2f;
    /// <summary>Fen Shackle: Seismic Slam stun multiplier on mired husks.</summary>
    public const float MiredStunFactor = 2f;
    /// <summary>Retribution store cap (bonus damage per Cleave target).</summary>
    public const int RetributionCap = 20;
    /// <summary>Store lifetime after the last charge (vow end or blocked hit).</summary>
    public const float RetributionSeconds = 4f;

    // ---------------- Rogue ----------------
    public const float SmokeRange = 8f;
    public const float SmokeRadius = 3f;
    public const float SmokeDuration = 5f;
    public const float SmokeCooldown = 12f;
    /// <summary>Blind refresh per smoke tick (husks leaving the cloud see again quickly).</summary>
    public const float BlindRefresh = 0.6f;

    public const float ShadowRollDistance = 5f;
    public const float ShadowRollCooldown = 6f;
    public const float AmbushSeconds = 2.5f;
    /// <summary>Ambush window with keen-cadence.</summary>
    public const float KeenAmbushSeconds = 4f;

    public const int VenomImpactDamage = 1;
    public const float VenomRange = 9f;
    public const float VenomRadius = 2.5f;
    public const int VenomStacksPerVial = 2;
    public const int VenomMaxStacks = 5;
    public const float VenomSeconds = 6f;
    public const float VenomTick = 1f;
    /// <summary>Damage per venom stack per tick (never crits).</summary>
    public const int VenomDamagePerStack = 1;
    public const float VenomCooldown = 7f;

    public const int PowderKegDamage = 6;
    public const float PowderKegRange = 7f;
    public const float PowderKegFuse = 1.5f;
    public const float PowderKegRadius = 3.5f;
    public const float PowderKegCooldown = 10f;
    /// <summary>Venom Burn: bonus blast damage per consumed venom stack.</summary>
    public const int VenomBurnPerStack = 2;
    /// <summary>Spark Shot: early ignition by a Quick Shot scales the blast.</summary>
    public const float IgniteFactor = 1.5f;
    /// <summary>A Quick Shot passing this close to a keg ignites it.</summary>
    public const float IgniteRadius = 1.0f;
    /// <summary>Throat Cut: Blade Flurry on a blinded husk.</summary>
    public const float ThroatCutFactor = 1.5f;

    // ---------------- pure formulas (tests + runtime) ----------------

    /// <summary>Cleave blow on one target: Shatter on stunned husks, plus the Retribution store.</summary>
    public static int CleaveDamageOn(int blow, bool stunned, bool lancerWard, int retribution)
    {
        float f = stunned ? (lancerWard ? LancerShatterFactor : ShatterFactor) : 1f;
        return Mathf.RoundToInt(blow * f) + Mathf.Clamp(retribution, 0, RetributionCap);
    }

    /// <summary>Seismic Slam stun on one target: Fen Shackle doubles it on mired husks.</summary>
    public static float SlamStunOn(float baseSeconds, bool mired) =>
        mired ? baseSeconds * MiredStunFactor : baseSeconds;

    /// <summary>Blade Flurry blow on one target: Throat Cut on blinded husks.</summary>
    public static int FlurryDamageOn(int blow, bool blinded) =>
        blinded ? Mathf.RoundToInt(blow * ThroatCutFactor) : blow;

    /// <summary>Powder Keg blast on one target: Spark Shot scales the blast, Venom Burn adds per stack.</summary>
    public static int KegDamageOn(int blast, int venomStacks, bool ignited)
    {
        float f = ignited ? IgniteFactor : 1f;
        return Mathf.RoundToInt(blast * f) + Mathf.Clamp(venomStacks, 0, VenomMaxStacks) * VenomBurnPerStack;
    }

    /// <summary>New venom count after adding stacks (capped).</summary>
    public static int AddStacks(int current, int add) => Mathf.Clamp(current + add, 0, VenomMaxStacks);

    /// <summary>Ambush window for a wearer (keen-cadence lengthens it).</summary>
    public static float AmbushWindow(Equipment eq) =>
        BuildUniqueEffects.HasKeenCadence(eq) ? KeenAmbushSeconds : AmbushSeconds;

    // ---------------- shared geometry ----------------

    /// <summary>Planar distance of p to the segment a→b, and the position along it (0..1).</summary>
    public static float SegmentDistance(Vector3 a, Vector3 b, Vector3 p, out float t)
    {
        a.y = b.y = p.y = 0f;
        var ab = b - a;
        float len2 = ab.sqrMagnitude;
        t = len2 < 0.000001f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        return (a + ab * t - p).magnitude;
    }

    /// <summary>True for boss-class husks (never pulled or blinded, combos still deal damage).</summary>
    public static bool IsBossLike(GameObject husk)
    {
        if (husk == null) return false;
        var hp = husk.GetComponent<EnemyHealth>();
        return hp != null && (hp.tier == LootTier.Boss || hp.tier == LootTier.ActBoss);
    }

    /// <summary>Living husks inside a planar radius.</summary>
    public static List<GameObject> HusksInRadius(Vector3 center, float radius)
    {
        var list = new List<GameObject>();
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            var d = husk.transform.position - center;
            d.y = 0f;
            if (d.magnitude <= radius) list.Add(husk);
        }
        return list;
    }

    /// <summary>Clamp a throw target to a max planar range from the hero.</summary>
    public static Vector3 ThrowPoint(Vector3 from, Vector3 dir, float aimDistance, float maxRange)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();
        float d = aimDistance > 0f ? Mathf.Min(aimDistance, maxRange) : maxRange;
        var p = from + dir * d;
        p.y = from.y;
        return p;
    }

    // ---------------- run counters (autoplay proof) ----------------

    public const string Shatter = "Shatter";
    public const string FenShackle = "FenShackle";
    public const string Retribution = "Retribution";
    public const string VenomBurn = "VenomBurn";
    public const string Ambush = "Ambush";
    public const string ThroatCut = "ThroatCut";
    public const string SparkShot = "SparkShot";

    static readonly Dictionary<string, int> triggered = new Dictionary<string, int>();

    /// <summary>Count one combo payoff (solo/host side, where it applies).</summary>
    public static void Note(string combo) =>
        triggered[combo] = (triggered.TryGetValue(combo, out int n) ? n : 0) + 1;

    public static int Triggered(string combo) => triggered.TryGetValue(combo, out int n) ? n : 0;

    /// <summary>"Name=count" lines, sorted (autoplay result).</summary>
    public static List<string> TriggeredLines()
    {
        var keys = new List<string>(triggered.Keys);
        keys.Sort(System.StringComparer.Ordinal);
        var list = new List<string>();
        foreach (var k in keys) list.Add($"{k}={triggered[k]}");
        return list;
    }

    /// <summary>Headless teardown for every combo registry.</summary>
    public static void ResetRun()
    {
        triggered.Clear();
        MireZone.Active.Clear();
        SmokeCloud.Active.Clear();
        PowderKeg.Active.Clear();
    }
}

/// <summary>
/// Combo state on a husk (class-kits-parity): mired, blinded and venom. Added
/// lazily by the first combo that touches the husk; runs on whoever applies
/// damage (solo/host), so co-op clients only ever see the result.
/// </summary>
public sealed class EnemyStatus : MonoBehaviour
{
    float miredUntil = float.NegativeInfinity;
    float blindedUntil = float.NegativeInfinity;
    float venomUntil = float.NegativeInfinity;
    float nextVenomTick;
    int venom;
    Transform venomOwner;
    GameObject venomFx;

    public bool Mired => Time.time < miredUntil;
    public bool Blinded => Time.time < blindedUntil;
    public int VenomStacks => Time.time < venomUntil ? venom : 0;

    static EnemyStatus Get(GameObject husk, bool create)
    {
        if (husk == null) return null;
        var s = husk.GetComponent<EnemyStatus>();
        if (s == null && create) s = husk.AddComponent<EnemyStatus>();
        return s;
    }

    public static void Mire(GameObject husk, float seconds)
    {
        var s = Get(husk, true);
        if (s == null || seconds <= 0f) return;
        s.miredUntil = Mathf.Max(s.miredUntil, Time.time + seconds);
    }

    public static void Blind(GameObject husk, float seconds)
    {
        if (ClassCombos.IsBossLike(husk)) return;
        var s = Get(husk, true);
        if (s == null || seconds <= 0f) return;
        s.blindedUntil = Mathf.Max(s.blindedUntil, Time.time + seconds);
    }

    public static bool IsMired(GameObject husk) { var s = Get(husk, false); return s != null && s.Mired; }
    public static bool IsBlinded(GameObject husk) { var s = Get(husk, false); return s != null && s.Blinded; }
    public static int Venom(GameObject husk) { var s = Get(husk, false); return s != null ? s.VenomStacks : 0; }

    /// <summary>Walk-speed factor for <see cref="EnemyAttack"/> (mired halves it).</summary>
    public static float MoveFactor(GameObject husk) => IsMired(husk) ? ClassCombos.MiredMoveFactor : 1f;

    /// <summary>Add venom stacks (capped, refreshes the timer). Returns the new count.</summary>
    public static int AddVenom(GameObject husk, Transform owner, int stacks)
    {
        var s = Get(husk, true);
        if (s == null || stacks <= 0) return Venom(husk);
        bool fresh = s.VenomStacks == 0;
        s.venom = ClassCombos.AddStacks(s.VenomStacks, stacks);
        s.venomUntil = Time.time + ClassCombos.VenomSeconds;
        if (fresh) s.nextVenomTick = Time.time + ClassCombos.VenomTick;
        if (owner != null) s.venomOwner = owner;
        s.ShowVenom(true);
        return s.venom;
    }

    /// <summary>Remove and return every venom stack (Venom Burn).</summary>
    public static int ConsumeVenom(GameObject husk)
    {
        var s = Get(husk, false);
        if (s == null) return 0;
        int n = s.VenomStacks;
        s.venom = 0;
        s.venomUntil = float.NegativeInfinity;
        s.ShowVenom(false);
        return n;
    }

    /// <summary>Test seam: apply one venom tick now, returns the damage dealt.</summary>
    public int TickVenomNow() => TickVenom();

    void Update()
    {
        if (VenomStacks > 0 && Time.time >= nextVenomTick)
        {
            TickVenom();
            nextVenomTick += ClassCombos.VenomTick;
        }
        else if (VenomStacks == 0 && venomFx != null) ShowVenom(false);
    }

    int TickVenom()
    {
        int n = VenomStacks;
        if (n <= 0) return 0;
        int dmg = n * ClassCombos.VenomDamagePerStack;
        DiabloSkills.ApplyHit(gameObject, dmg, transform.position, 0f, venomOwner, canCrit: false);
        return dmg;
    }

    void ShowVenom(bool on)
    {
        if (!Application.isPlaying) return;
        if (on && venomFx == null)
        {
            venomFx = new GameObject("VenomGlow");
            venomFx.transform.SetParent(transform, false);
            venomFx.transform.localPosition = Vector3.up * 1.2f;
            var l = venomFx.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.45f, 1f, 0.3f);
            l.range = 2.2f;
            l.intensity = 1.6f;
        }
        else if (!on && venomFx != null)
        {
            Destroy(venomFx);
            venomFx = null;
        }
    }
}

/// <summary>
/// Per-hero combo state (class-kits-parity): Iron Vow window, Retribution
/// store and the Ambush crit. One component per hero, no static cross-owner
/// state; a reload rebuilds the hero without it.
/// </summary>
public sealed class HeroComboState : MonoBehaviour
{
    float vowUntil = float.NegativeInfinity;
    float retribution;
    float retributionUntil = float.NegativeInfinity;
    float ambushUntil = float.NegativeInfinity;

    public bool VowActive => Time.time < vowUntil;
    public int Retribution => Time.time < Mathf.Max(retributionUntil, vowUntil)
        ? Mathf.Min(ClassCombos.RetributionCap, Mathf.RoundToInt(retribution)) : 0;
    public bool AmbushArmed => Time.time < ambushUntil;

    public static HeroComboState For(GameObject hero, bool create = true)
    {
        if (hero == null) return null;
        var s = hero.GetComponent<HeroComboState>();
        if (s == null && create) s = hero.AddComponent<HeroComboState>();
        return s;
    }

    public void BeginVow(float seconds)
    {
        vowUntil = Time.time + seconds;
        retributionUntil = vowUntil + ClassCombos.RetributionSeconds;
    }

    /// <summary>Charge the store (vow hits, bulwark blocks); keeps it alive a while.</summary>
    public void AddRetribution(float amount)
    {
        if (amount <= 0f) return;
        if (Retribution == 0) retribution = 0f;
        retribution = Mathf.Min(ClassCombos.RetributionCap, retribution + amount);
        retributionUntil = Mathf.Max(retributionUntil, Time.time + ClassCombos.RetributionSeconds);
    }

    /// <summary>Spend the whole store (one Cleave releases it).</summary>
    public int ConsumeRetribution()
    {
        int r = Retribution;
        retribution = 0f;
        retributionUntil = float.NegativeInfinity;
        return r;
    }

    public void ArmAmbush(float seconds) => ambushUntil = Time.time + seconds;

    /// <summary>True once per armed ambush: the next damaging hit crits.</summary>
    public bool ConsumeAmbush()
    {
        if (!AmbushArmed) return false;
        ambushUntil = float.NegativeInfinity;
        ClassCombos.Note(ClassCombos.Ambush);
        return true;
    }

    /// <summary>Hero took damage (pre-shield): the vow turns it into Retribution.</summary>
    public static void OnHeroDamaged(GameObject hero, float amount)
    {
        var s = For(hero, false);
        if (s != null && s.VowActive) s.AddRetribution(amount);
    }

    /// <summary>Crit hook for <see cref="DiabloJuice.RollCrit"/>: an armed ambush forces the crit.</summary>
    public static bool TryAmbush(Transform attacker)
    {
        if (attacker == null) return false;
        var s = attacker.GetComponent<HeroComboState>();
        return s != null && s.ConsumeAmbush();
    }
}

/// <summary>Mire Ground pool (Fenwalker F): ticks damage and keeps husks mired.</summary>
public sealed class MireZone : MonoBehaviour
{
    public static readonly List<MireZone> Active = new List<MireZone>();
    public Transform owner;
    public int damage;
    public float radius;
    float endTime, nextTick;

    public static MireZone Spawn(Transform owner, Vector3 at, int damage, float radius, float duration)
    {
        if (owner != null)
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] == null) Active.RemoveAt(i);
                else if (Active[i].owner == owner) Active[i].Kill();
        var go = new GameObject("MireGround");
        go.transform.position = at;
        var z = go.AddComponent<MireZone>();
        z.owner = owner;
        z.damage = damage;
        z.radius = radius;
        z.endTime = Time.time + duration;
        z.nextTick = Time.time;
        Active.Add(z);
        ClassAbilityVfx.AttachMireGround(go.transform, radius, duration);
        return z;
    }

    /// <summary>Test seam: one tick now, returns husks touched.</summary>
    public int TickNow() => Tick();

    void Update()
    {
        if (owner == null || Time.time >= endTime) { Kill(); return; }
        if (Time.time >= nextTick) { Tick(); nextTick += ClassCombos.MireGroundTick; }
    }

    int Tick()
    {
        var hits = ClassCombos.HusksInRadius(transform.position, radius);
        foreach (var husk in hits)
        {
            EnemyStatus.Mire(husk, ClassCombos.MireGroundTick + 0.5f);
            DiabloSkills.ApplyHit(husk, damage, transform.position, 0f, owner, canCrit: false);
        }
        return hits.Count;
    }

    public void Kill()
    {
        Active.Remove(this);
        if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
    }

    void OnDestroy() => Active.Remove(this);
}

/// <summary>Smoke Pot cloud (Rogue R): blinds husks inside (no strikes).</summary>
public sealed class SmokeCloud : MonoBehaviour
{
    public static readonly List<SmokeCloud> Active = new List<SmokeCloud>();
    public Transform owner;
    public float radius;
    float endTime, nextTick;

    public static SmokeCloud Spawn(Transform owner, Vector3 at, float radius, float duration)
    {
        var go = new GameObject("SmokeCloud");
        go.transform.position = at;
        var c = go.AddComponent<SmokeCloud>();
        c.owner = owner;
        c.radius = radius;
        c.endTime = Time.time + duration;
        c.nextTick = Time.time;
        Active.Add(c);
        c.Tick();
        ClassAbilityVfx.AttachSmokePot(go.transform, radius, duration);
        return c;
    }

    /// <summary>True when a point sits inside any live cloud.</summary>
    public static bool Covers(Vector3 p)
    {
        foreach (var c in Active)
        {
            if (c == null || Time.time >= c.endTime) continue;
            var d = p - c.transform.position;
            d.y = 0f;
            if (d.magnitude <= c.radius) return true;
        }
        return false;
    }

    public int TickNow() => Tick();

    void Update()
    {
        if (Time.time >= endTime) { Kill(); return; }
        if (Time.time >= nextTick) { Tick(); nextTick += ClassCombos.BlindRefresh * 0.5f; }
    }

    int Tick()
    {
        var hits = ClassCombos.HusksInRadius(transform.position, radius);
        foreach (var husk in hits) EnemyStatus.Blind(husk, ClassCombos.BlindRefresh);
        return hits.Count;
    }

    public void Kill()
    {
        Active.Remove(this);
        if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
    }

    void OnDestroy() => Active.Remove(this);
}

/// <summary>
/// Powder Keg (Rogue G): fuse, then a blast that burns off venom. A Quick
/// Shot through the keg ignites it early (Spark Shot); smugglers-fuse sets
/// it off at once inside smoke with a wider blast and fresh venom.
/// </summary>
public sealed class PowderKeg : MonoBehaviour
{
    public static readonly List<PowderKeg> Active = new List<PowderKeg>();
    public Transform owner;
    public int blast;
    public float radius;
    public bool exploded;
    float fuseEnd;

    public static PowderKeg Place(Transform owner, Vector3 at, int blast, float radius, float fuse)
    {
        var go = new GameObject("PowderKeg");
        go.transform.position = at;
        var k = go.AddComponent<PowderKeg>();
        k.owner = owner;
        k.blast = blast;
        k.radius = radius;
        k.fuseEnd = Time.time + fuse;
        Active.Add(k);
        var body = ClassAbilityVfx.AttachPowderKeg(go.transform);
        if (body == null && Application.isPlaying) ComboVfx.Keg(go.transform);
        return k;
    }

    /// <summary>First live keg within <paramref name="reach"/> of the segment a→b (Spark Shot).</summary>
    public static PowderKeg OnSegment(Vector3 a, Vector3 b, float reach)
    {
        foreach (var k in Active)
        {
            if (k == null || k.exploded) continue;
            if (ClassCombos.SegmentDistance(a, b, k.transform.position, out _) <= reach) return k;
        }
        return null;
    }

    void Update()
    {
        if (!exploded && Time.time >= fuseEnd) Detonate(false);
    }

    /// <summary>Blow up now. Returns the total damage dealt (test seam).</summary>
    public int Detonate(bool ignited)
    {
        if (exploded) return 0;
        exploded = true;
        if (ignited) ClassCombos.Note(ClassCombos.SparkShot);
        var eq = owner != null ? owner.GetComponent<Equipment>() : null;
        bool fuse = BuildUniqueEffects.HasSmugglersFuse(eq) && SmokeCloud.Covers(transform.position);
        float r = fuse ? radius * BuildUniqueEffects.FuseRadiusFactor : radius;
        int total = 0;
        bool burnedVenom = false;
        foreach (var husk in ClassCombos.HusksInRadius(transform.position, r))
        {
            int stacks = EnemyStatus.ConsumeVenom(husk);
            if (stacks > 0) { burnedVenom = true; ClassCombos.Note(ClassCombos.VenomBurn); }
            int dmg = ClassCombos.KegDamageOn(blast, stacks, ignited);
            DiabloSkills.ApplyHit(husk, dmg, transform.position, 0.8f, owner);
            if (fuse) EnemyStatus.AddVenom(husk, owner, BuildUniqueEffects.FuseVenomStacks);
            total += dmg;
        }
        DiabloAudio.PlayPowderKeg();
        ClassAbilityVfx.SpawnPowderKegBlast(transform.position, r, burnedVenom);
        Active.Remove(this);
        if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        return total;
    }

    void OnDestroy() => Active.Remove(this);
}

/// <summary>Light-and-ring visuals for the combo skills (play mode only).</summary>
public static class ComboVfx
{
    public static void Pool(Vector3 at, float radius, float seconds, Color color)
    {
        PresentationVfx.CleaveArc(at, radius);
        var prefab = Res.Get<GameObject>("SkillArt/CleaveArc");
        GameObject ring;
        if (prefab != null)
        {
            ring = Object.Instantiate(prefab, at, Quaternion.identity);
            ring.transform.localScale = Vector3.one * (radius / 2.6f);
            foreach (var r in ring.GetComponentsInChildren<Renderer>())
                r.material.color = new Color(color.r, color.g, color.b, r.material.color.a);
        }
        else ring = new GameObject("ComboPool");
        var light = ring.GetComponent<Light>();
        if (light == null) light = ring.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = radius * 2f;
        light.intensity = 2.5f;
        Object.Destroy(ring, seconds);
    }

    public static void Flash(Vector3 at, Color color, float range, float seconds)
    {
        var go = new GameObject("ComboFlash");
        go.transform.position = at + Vector3.up;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = 3.5f;
        Object.Destroy(go, seconds);
    }

    public static void Blast(Vector3 at, float radius)
    {
        PresentationVfx.CleaveArc(at, radius);
        Flash(at, new Color(1f, 0.55f, 0.2f), radius * 2.5f, 0.45f);
    }

    public static void Keg(Transform keg)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "KegBody";
        Object.Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(keg, false);
        body.transform.localPosition = Vector3.up * 0.35f;
        body.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
        var r = body.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(0.35f, 0.22f, 0.12f);
        var spark = new GameObject("Fuse");
        spark.transform.SetParent(keg, false);
        spark.transform.localPosition = Vector3.up * 0.8f;
        var l = spark.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.6f, 0.2f);
        l.range = 2f;
        l.intensity = 2f;
    }
}
