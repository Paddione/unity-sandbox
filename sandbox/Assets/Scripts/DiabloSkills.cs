using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Hero skills: Fireball (Q, blast damage), Bow (E, fast single target
/// arrow) and Cleave (R, 360° melee arc, S4). Right-click repeats the last
/// used skill. Ranged shots fire toward the cursor aim of
/// <see cref="DiabloClickController"/>; Cleave hits every husk in radius.
/// Damage runs locally solo/host and through a range-validated ServerRpc as
/// a netcode client. Skill ranks (+20 % damage/rank) and focus live on the
/// <see cref="CharacterSheet"/> and scale every cast.
/// </summary>
public sealed class DiabloSkills : MonoBehaviour
{
    /// <summary>
    /// Explicit values: co-op sends skills as int (DiabloNetworkPlayer), so
    /// existing values never move; Lanternbearer skills append (second-class S1),
    /// party-role skills append after them (party-roles S2: SeismicSlam, S3: Rogue + Healer).
    /// </summary>
    public enum Skill { Fireball = 0, Arrow = 1, Cleave = 2, LanternBolt = 3, Wisps = 4, SearingHalo = 5, VeilStep = 6, SeismicSlam = 7, QuickShot = 8, BladeFlurry = 9, MendingLight = 10, RenewingCircle = 11 }

    [Header("Fireball (Q)")]
    public int fireballDamage = 3;
    public float fireballBlastRadius = 2.5f;
    public float fireballSpeed = 11f;
    public float fireballRange = 16f;
    public float fireballCooldown = 1.0f;

    [Header("Bow (E)")]
    public int arrowDamage = 2;
    public float arrowSpeed = 24f;
    public float arrowRange = 20f;
    public float arrowCooldown = 0.35f;

    [Header("Cleave (R)")]
    public int cleaveDamage = 4;
    public float cleaveBaseRadius = 2.6f;
    public float cleaveRadiusPerRank = 0.4f;
    public float cleaveCooldown = 6f;
    public float cleaveKnockback = 1.2f;

    [Header("Lanternbearer (second-class S2)")]
    public int lanternBoltDamage = 4;
    public float lanternBoltSpeed = 16f;
    public float lanternBoltRange = 18f;
    public float lanternBoltCooldown = 1.2f;
    public int lanternBoltPierce = 1;
    public int wispBaseCount = 2;
    public int wispDamage = 1;
    public float wispDuration = 20f;
    public float wispCooldown = 14f;
    public int haloDamage = 3;
    public float haloRadius = 3.5f;
    /// <summary>Same pop as the melee contract (d2-combat-feel: 0.6 m on normal husks).</summary>
    public float haloKnockback = 0.6f;
    public float haloCooldown = 7f;
    public float veilDistance = 6f;
    public float veilCooldown = 5f;
    public const float VeilHeroRadius = 0.35f;

    [Header("Tank slam (party-roles S2, Fenwalker)")]
    public int slamDamage = 3;
    public float slamRadius = 3.5f;
    public float slamCooldown = 8f;
    /// <summary>How long slammed husks hold still (bosses use their own controller, unaffected).</summary>
    public float slamStunSeconds = 2f;
    /// <summary>No knockback: the tank holds the line, slammed husks stay in reach.</summary>
    public float slamKnockback = 0f;

    [Header("Rogue (party-roles S3)")]
    public int quickShotDamage = 2;
    public float quickShotSpeed = 24f;
    public float quickShotRange = 20f;
    public float quickShotCooldown = 0.3f;
    public int flurryDamage = 2;
    public float flurryRadius = 2.4f;
    public float flurryCooldown = 4f;
    /// <summary>Flurry pops husks back a step so the rogue keeps its distance (halo pop).</summary>
    public float flurryKnockback = 0.6f;

    [Header("Healer (party-roles S3, Lanternbearer)")]
    public int mendHeal = 8;
    public const float TargetHealBaseRange = 12f;
    public float targetHealRange = TargetHealBaseRange;
    public const float MendBaseCooldown = 5f;
    public float mendCooldown = MendBaseCooldown;
    public int renewHeal = 5;
    public const float GroupHealBaseRange = 6f;
    public float groupHealRadius = GroupHealBaseRange;
    public const float RenewBaseCooldown = 12f;
    public float renewCooldown = RenewBaseCooldown;

    /// <summary>
    /// Burning-halo window for the wick-of-the-halo Wisp priority
    /// (cinderreach-build-uniques S2): matches the halo visual lifetime.
    /// </summary>
    public const float HaloVisualSeconds = 0.6f;

    /// <summary>Launch height above the hero pivot (chest).</summary>
    public const float LaunchHeight = 1.0f;
    /// <summary>Launch offset in front of the hero (clears its own collider).</summary>
    public const float LaunchForward = 0.8f;

    public Skill lastUsed = Skill.Fireball;

    float lastFireball = float.NegativeInfinity;
    float lastArrow = float.NegativeInfinity;
    float lastCleave = float.NegativeInfinity;
    readonly System.Collections.Generic.Dictionary<Skill, float> lastLantern =
        new System.Collections.Generic.Dictionary<Skill, float>();

    /// <summary>
    /// S4 keen-cadence: pending special cast awaiting its first crit. Set at
    /// cast start (sync skills arm around their loop, Quick Shot arms at
    /// launch and its projectile impacts consume). Per-instance state: two
    /// wearers never share it, a reload rebuilds the component.
    /// Duration/reset rule: one cut per cast — the first consumed crit
    /// disarms until the next cast arms again.
    /// </summary>
    bool keenPending;
    Skill keenPendingSkill;

    void ArmKeen(Skill skill) { keenPending = true; keenPendingSkill = skill; }

    /// <summary>
    /// S4 keen-cadence: the first crit of a special cast cuts its remaining
    /// cooldown by <see cref="BuildUniqueEffects.KeenCooldownCut"/> s, once
    /// per cast. Base skills never qualify, even with the helm on.
    /// </summary>
    public bool TryKeenReduction(Skill skill, bool crit)
    {
        if (!crit || !keenPending || !keenPendingSkill.Equals(skill)) return false;
        if (!IsPartySkill(skill)) return false;
        if (!BuildUniqueEffects.HasKeenCadence(GetComponent<Equipment>())) return false;
        keenPending = false;
        ReduceCooldown(skill, BuildUniqueEffects.KeenCooldownCut);
        return true;
    }

    /// <summary>Move a skill's last-cast timestamp back (floored at ready).</summary>
    public void ReduceCooldown(Skill skill, float seconds)
    {
        if (seconds <= 0f) return;
        float floor = Time.time - EffectiveCooldown(skill);
        if (skill == Skill.Fireball) lastFireball = Mathf.Max(lastFireball - seconds, floor);
        else if (skill == Skill.Arrow) lastArrow = Mathf.Max(lastArrow - seconds, floor);
        else if (skill == Skill.Cleave) lastCleave = Mathf.Max(lastCleave - seconds, floor);
        else if (lastLantern.ContainsKey(skill)) lastLantern[skill] = Mathf.Max(lastLantern[skill] - seconds, floor);
        else lastLantern[skill] = floor;
    }

    /// <summary>
    /// Class override for AI drivers: a companion casts as its own class,
    /// not the player's. Null on heroes and mirrors (run profile rules).
    /// </summary>
    public ClassDefinition classOverride;

    /// <summary>Class of the local hero (run profile); mirrors only render casts.</summary>
    public ClassDefinition ActiveClass => classOverride ?? RunProfile.Class;

    /// <summary>
    /// Active skill variant (skill-variants S2). Unknown picks fall back to
    /// the legacy default; co-op sessions always run the base skill path.
    /// </summary>
    public string ActiveVariantId(Skill s)
    {
        string def = SkillVariantCatalog.DefaultVariantId(ActiveClass.id, s);
        if (def == null) return null;
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        if (netPlayer != null && netPlayer.IsSpawned) return def;
        return RunProfile.GetSkillVariant(ActiveClass.id, s);
    }

    /// <summary>Wisps for a rank: base 2, a third from rank 3.</summary>
    public int WispCountFor(int rank) => wispBaseCount + (rank >= 3 ? 1 : 0);

    /// <summary>Heals cast this run (party-roles S6 autoplay proof).</summary>
    public static int HealsCast;
    /// <summary>HP restored by heals this run.</summary>
    public static float HPRestored;

    /// <summary>Reset run counters (scene load / headless tests).</summary>
    public static void ResetRun() { HealsCast = 0; HPRestored = 0f; }
    DiabloClickController controller;
    DiabloNetworkPlayer netPlayer;
    Text hud;
    Transform slotsContainer;
    string hudBuiltFor = "";
    readonly System.Collections.Generic.List<(Skill skill, Image frame, Image veil, Text cdText)> hudSlots =
        new System.Collections.Generic.List<(Skill, Image, Image, Text)>();

    void Awake()
    {
        controller = GetComponent<DiabloClickController>();
        netPlayer = GetComponent<DiabloNetworkPlayer>();
    }

    bool IsRemoteMirror => netPlayer != null && netPlayer.IsSpawned && !netPlayer.IsOwner;

    // ---------- held weapon swap (bow while shooting, sword otherwise) ----------

    /// <summary>Seconds the bow stays drawn after the last arrow.</summary>
    public const float BowStanceSeconds = 2.0f;
    /// <summary>Scene/prefab name of the builder-attached melee weapon.</summary>
    public const string SwordName = "StarterSword";

    float bowUntil = float.NegativeInfinity;
    GameObject heldBow;
    Renderer[] swordRenderers;

    /// <summary>True while the hero holds the bow instead of the sword.</summary>
    public bool BowDrawn => heldBow != null && heldBow.activeSelf;

    /// <summary>Draw the bow (hide the sword) for BowStanceSeconds.</summary>
    public void EnterBowStance()
    {
        bowUntil = Time.time + BowStanceSeconds;
        if (heldBow == null) heldBow = CreateHeldBow();
        if (heldBow != null) heldBow.SetActive(true);
        SetSwordVisible(false);
    }

    /// <summary>Put the bow away and show the sword again (melee strike or timeout).</summary>
    public void ExitBowStance()
    {
        bowUntil = float.NegativeInfinity;
        if (heldBow != null) heldBow.SetActive(false);
        SetSwordVisible(true);
    }

    GameObject CreateHeldBow()
    {
        var prefab = Resources.Load<GameObject>("SkillArt/HeroBow");
        if (prefab == null) return null;
        // Bows sit in the off hand; KayKit rigs expose a "handslot.l" mount.
        var hand = FindDeep(transform, "handslot.l") ?? FindDeep(transform, "hand.l");
        var bow = Instantiate(prefab);
        bow.name = "HeldBow";
        if (hand != null)
        {
            bow.transform.SetParent(hand, false);
            bow.transform.localPosition = Vector3.zero;
            bow.transform.localRotation = HandMountRotation(bow);
        }
        else
        {
            bow.transform.SetParent(transform, false);
            bow.transform.localPosition = new Vector3(-0.35f, 1.0f, 0.25f);
            bow.transform.localRotation = Quaternion.identity;
        }
        return bow;
    }

    /// <summary>
    /// KayKit's bow on handslot.l faces its string toward the target and its
    /// arc toward the archer (seen in the shotcheck). Turn it 180° about its
    /// own long axis so the arc leads and the string sits on the archer's side.
    /// </summary>
    public static Quaternion HandMountRotation(GameObject bow) =>
        Quaternion.AngleAxis(180f, LongAxis(bow));

    /// <summary>Longest mesh-bounds axis in the bow root's local space (limb to limb).</summary>
    public static Vector3 LongAxis(GameObject bow)
    {
        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        foreach (var r in bow.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null;
            if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
            else
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }
            if (mesh == null) continue;
            var mb = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var local = bow.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }
        if (float.IsInfinity(min.x)) return Vector3.up;
        var size = max - min;
        if (size.x >= size.y && size.x >= size.z) return Vector3.right;
        return size.y >= size.z ? Vector3.up : Vector3.forward;
    }

    void SetSwordVisible(bool visible)
    {
        if (swordRenderers == null)
        {
            var sword = FindDeep(transform, SwordName);
            swordRenderers = sword != null ? sword.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }
        foreach (var r in swordRenderers)
            if (r != null) r.enabled = visible;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindDeep(t.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    /// <summary>Base cooldown of a skill (before focus reduction).</summary>
    public float BaseCooldown(Skill s)
    {
        switch (s)
        {
            case Skill.Fireball: return fireballCooldown;
            case Skill.Arrow: return arrowCooldown;
            case Skill.LanternBolt: return lanternBoltCooldown;
            case Skill.Wisps: return wispCooldown;
            case Skill.SearingHalo: return haloCooldown;
            case Skill.VeilStep: return veilCooldown;
            case Skill.SeismicSlam: return slamCooldown;
            case Skill.QuickShot: return quickShotCooldown;
            case Skill.BladeFlurry: return flurryCooldown;
            case Skill.MendingLight: return mendCooldown;
            case Skill.RenewingCircle: return renewCooldown;
            default: return cleaveCooldown;
        }
    }

    /// <summary>Effective cooldown: base × focus factor (S4, floored at half).</summary>
    public float EffectiveCooldown(Skill s)
    {
        var sheet = CharacterSheet.For(gameObject);
        float factor = sheet != null ? sheet.CooldownMultiplier() : 1f;
        var eq = GetComponent<Equipment>();
        if (eq != null) factor *= eq.CooldownFactor();
        return BaseCooldown(s) * factor;
    }

    /// <summary>Seconds until the skill is ready again (0 when ready).</summary>
    public float CooldownLeft(Skill s)
    {
        float last = s == Skill.Fireball ? lastFireball
            : s == Skill.Arrow ? lastArrow
            : s == Skill.Cleave ? lastCleave
            : lastLantern.TryGetValue(s, out float l) ? l : float.NegativeInfinity;
        return Mathf.Max(0f, EffectiveCooldown(s) - (Time.time - last));
    }

    /// <summary>Current cleave radius: base + per-rank bonus (S4).</summary>
    public float CleaveRadius
    {
        get
        {
            var sheet = CharacterSheet.For(gameObject);
            return sheet != null
                ? sheet.CleaveRadius(cleaveBaseRadius, cleaveRadiusPerRank)
                : cleaveBaseRadius;
        }
    }

    /// <summary>
    /// S3 area scaling: rank/variant radius through the wearer's
    /// <see cref="Equipment.ScaleArea"/>. One call site per skill, shared by the
    /// hit loop and every visual, so both see the identical radius.
    /// </summary>
    public float ScaledArea(float baseRadius)
    {
        var eq = GetComponent<Equipment>();
        return eq != null ? eq.ScaleArea(baseRadius) : baseRadius;
    }

    /// <summary>True for skills appended by party-roles (SeismicSlam and later).</summary>
    public static bool IsPartySkill(Skill s) => s >= Skill.SeismicSlam;

    /// <summary>
    /// Damage of one cast: base (+ weapon bonus for Bow/Cleave) × rank × focus.
    /// Party-role specialty skills scale with the class specialty factor on
    /// top (S3 ×1.5 contract); pre-party skills keep their legacy scaling
    /// untouched. Pure entry point for tests (uses the live sheet, no cooldown gate).
    /// </summary>
    public int EffectiveDamage(Skill s)
    {
        int baseDmg = BaseDamage(s);
        if (baseDmg <= 0) return 0; // Veil Step and the heals deal no damage
        var sheet = CharacterSheet.For(gameObject);
        var eq = GetComponent<Equipment>();
        int gearRanks = eq != null ? eq.SkillRankBonus() : 0;
        float mul;
        if (sheet != null)
            mul = sheet.SkillMultiplier(s, gearRanks, ActiveClass.focusSkillFactor);
        else mul = 1f + CharacterSheet.RankDamageBonus * gearRanks;
        if (IsPartySkill(s) && ActiveClass.IsSpecialty(s))
            mul *= ActiveClass.specialtyFactor;
        return Mathf.Max(1, Mathf.RoundToInt(baseDmg * mul));
    }

    /// <summary>
    /// Heal of one cast (party-roles S3): base × rank × focus(class) ×
    /// specialty factor. Pure entry point for tests (no cooldown gate).
    /// </summary>
    public int EffectiveHeal(Skill s)
    {
        int baseHeal = s == Skill.MendingLight ? mendHeal : s == Skill.RenewingCircle ? renewHeal : 0;
        if (baseHeal <= 0) return 0;
        var sheet = CharacterSheet.For(gameObject);
        var eq = GetComponent<Equipment>();
        int gearRanks = eq != null ? eq.SkillRankBonus() : 0;
        float mul = sheet != null ? sheet.SpecialtySkillMultiplier(s, ActiveClass, gearRanks) : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseHeal * mul));
    }

    /// <summary>Unscaled damage of a skill (weapon bonus for Bow/Cleave).</summary>
    public int BaseDamage(Skill s)
    {
        switch (s)
        {
            case Skill.Fireball: return fireballDamage;
            case Skill.Arrow: return arrowDamage + WeaponBonus();
            case Skill.Cleave: return cleaveDamage + WeaponBonus();
            case Skill.LanternBolt: return lanternBoltDamage;
            case Skill.Wisps: return wispDamage;
            case Skill.SearingHalo: return haloDamage;
            case Skill.SeismicSlam: return slamDamage;
            case Skill.QuickShot: return quickShotDamage;
            case Skill.BladeFlurry: return flurryDamage;
            default: return 0;
        }
    }

    /// <summary>Cast toward the current aim. False while on cooldown.</summary>
    public bool Cast(Skill s)
    {
        if (controller == null) controller = GetComponent<DiabloClickController>();
        var dir = controller != null ? controller.AimDirection : transform.forward;
        return CastToward(s, dir);
    }

    /// <summary>
    /// Cast in a world direction (tests + external drivers). The host passes
    /// the sender's class explicitly (S3 intent path); everyone else casts
    /// with the local run profile.
    /// </summary>
    public bool CastToward(Skill s, Vector3 dir, ClassDefinition classOverride = null)
    {
        var casterClass = classOverride ?? ActiveClass;
        if (!casterClass.CanCast(s)) return false; // base set for all, specialty only for its class (party-roles S1)
        if (CooldownLeft(s) > 0f) return false;
        if (s == Skill.MendingLight && FindHealTarget(targetHealRange) == null) return false; // no cooldown wasted
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        if (s == Skill.Fireball) lastFireball = Time.time;
        else if (s == Skill.Arrow) lastArrow = Time.time;
        else if (s == Skill.Cleave) lastCleave = Time.time;
        else lastLantern[s] = Time.time;
        lastUsed = s;

        // S3 co-op intent: a networked client never applies damage itself. It
        // sends skill + anchor + facing (never a damage number); the host
        // validates and executes authoritatively. Locally only the cosmetic
        // half plays, so nothing applies twice.
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var intentNet = NetworkManager.Singleton;
        if (netPlayer != null && netPlayer.IsSpawned && intentNet != null
            && intentNet.IsListening && !intentNet.IsServer)
        {
            netPlayer.SubmitSkillCastServerRpc((int)s, transform.position, dir);
            PlayCosmetic(s, dir);
            return true;
        }

        if (s == Skill.Wisps || s == Skill.SearingHalo || s == Skill.VeilStep)
        {
            var at = transform.position + Vector3.up * LaunchHeight;
            if (s == Skill.Wisps) SummonWisps(cosmetic: false);
            else if (s == Skill.SearingHalo) DoHalo(cosmetic: false);
            else DoVeilStep(dir);
            BroadcastIfNetworked(s, at, dir);
            return true;
        }

        if (s == Skill.Cleave)
        {
            DoCleave(dir, cosmetic: false);
            var origin = transform.position + Vector3.up * LaunchHeight + dir * LaunchForward;
            // Co-op: mirrors render the arc visual (no damage there).
            if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
            var relay = NetworkManager.Singleton;
            if (netPlayer != null && netPlayer.IsSpawned && relay != null && relay.IsListening)
                netPlayer.BroadcastCast((int)s, origin, dir);
            return true;
        }

        if (s == Skill.SeismicSlam)
        {
            DoSlam(cosmetic: false);
            var at = transform.position + Vector3.up * LaunchHeight;
            BroadcastIfNetworked(s, at, dir);
            return true;
        }

        if (s == Skill.BladeFlurry)
        {
            DoFlurry(dir, cosmetic: false);
            var fat = transform.position + Vector3.up * LaunchHeight;
            BroadcastIfNetworked(s, fat, dir);
            return true;
        }

        if (s == Skill.MendingLight)
        {
            DoTargetHeal(cosmetic: false);
            var mat = transform.position + Vector3.up * LaunchHeight;
            BroadcastIfNetworked(s, mat, dir);
            return true;
        }

        if (s == Skill.RenewingCircle)
        {
            DoGroupHeal(cosmetic: false);
            var rat = transform.position + Vector3.up * LaunchHeight;
            BroadcastIfNetworked(s, rat, dir);
            return true;
        }

        var origin2 = transform.position + Vector3.up * LaunchHeight + dir * LaunchForward;
        Launch(s, origin2, dir, cosmetic: false);

        // Co-op: every other client renders a visual-only copy of this cast.
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var nm = NetworkManager.Singleton;
        if (netPlayer != null && netPlayer.IsSpawned && nm != null && nm.IsListening)
            netPlayer.BroadcastCast((int)s, origin2, dir);
        return true;
    }

    /// <summary>S3: cosmetic half of a cast for intent-sending clients (no damage, no cooldown).</summary>
    public void PlayCosmetic(Skill s, Vector3 dir)
    {
        switch (s)
        {
            case Skill.Cleave: DoCleave(dir, cosmetic: true); break;
            case Skill.SearingHalo: DoHalo(cosmetic: true); break;
            case Skill.SeismicSlam: DoSlam(cosmetic: true); break;
            case Skill.BladeFlurry: DoFlurry(dir, cosmetic: true); break;
            case Skill.MendingLight: DoTargetHeal(cosmetic: true); break;
            case Skill.RenewingCircle: DoGroupHeal(cosmetic: true); break;
            case Skill.Wisps: SummonWisps(cosmetic: true); break;
            case Skill.VeilStep: DoVeilStep(dir); break; // movement stays client-side
            default:
                var origin = transform.position + Vector3.up * LaunchHeight + dir * LaunchForward;
                Launch(s, origin, dir, cosmetic: true);
                break;
        }
    }

    /// <summary>
    /// S3 host-authoritative execution: runs the sender mirror's skill from S2
    /// gear + authoritative cooldown state after
    /// <see cref="DiabloNetworkPlayer.ValidateSkillIntent"/> passed. A damage
    /// number from the client is never consulted, so the HitValidator input cap
    /// (MaxDamage) only gates legacy client-sent hits — a legitimate
    /// server-rolled 2x crit may exceed it.
    /// </summary>
    public bool ServerExecuteSkill(Skill s, Vector3 dir, ClassDefinition senderClass) =>
        CastToward(s, dir, senderClass);

    void BroadcastIfNetworked(Skill s, Vector3 origin, Vector3 dir)
    {
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var nm = NetworkManager.Singleton;
        if (netPlayer != null && netPlayer.IsSpawned && nm != null && nm.IsListening)
            netPlayer.BroadcastCast((int)s, origin, dir);
    }

    // ---------- Lanternbearer (second-class S2) ----------

    /// <summary>Wisps (E): companions for <see cref="wispDuration"/> s; +1 from rank 3. Replaces live wisps.</summary>
    public System.Collections.Generic.List<Wisp> SummonWisps(bool cosmetic)
    {
        foreach (var old in Wisp.Active.ToArray())
            if (old != null && old.owner == transform)
            {
                if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
            }
        var sheet = CharacterSheet.For(gameObject);
        int count = WispCountFor(sheet != null ? sheet.GetRank(Skill.Wisps) : 0);
        int dmg = cosmetic ? 0 : EffectiveDamage(Skill.Wisps);
        var list = new System.Collections.Generic.List<Wisp>();
        for (int i = 0; i < count; i++)
            list.Add(Wisp.Spawn(transform, i, count, dmg, wispDuration, cosmetic));
        if (!cosmetic) DiabloAudio.PlayLantern();
        return list;
    }

    /// <summary>Searing Halo (R): ring around the hero, every husk inside takes the blow and a small pop.</summary>
    public int DoHalo(bool cosmetic)
    {
        int hits = 0;
        bool ward = !cosmetic && ActiveVariantId(Skill.SearingHalo) == SkillVariantCatalog.HaloWardId;
        if (!cosmetic) HaloZone.ClearFor(transform); // recasts replace the old ward
        float radius = ScaledArea(haloRadius);
        if (!cosmetic)
        {
            int blow = EffectiveDamage(Skill.SearingHalo);
            foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            {
                if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
                var to = husk.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > radius) continue;
                ApplyHit(husk, blow, transform.position, haloKnockback, transform);
                hits++;
            }
            if (ward)
                HaloZone.Spawn(transform, transform.position, blow, radius,
                    SkillVariantCatalog.HaloWardDuration, SkillVariantCatalog.HaloTickInterval, haloKnockback);
            BuildUniqueEffects.NotifyHalo(transform.position, radius, HaloVisualSeconds);
            DiabloAudio.PlayHalo();
            var driver = GetComponent<DiabloAnimator>();
            if (driver != null) driver.OnStrike();
        }
        SpawnHaloVisual(radius);
        PresentationVfx.CleaveArc(transform.position, radius);
        return hits;
    }

    void SpawnHaloVisual(float radius)
    {
        if (!Application.isPlaying) return;
        var prefab = Resources.Load<GameObject>("SkillArt/CleaveArc");
        GameObject ring;
        if (prefab != null)
        {
            ring = Instantiate(prefab, transform.position, Quaternion.identity);
            ring.transform.localScale = Vector3.one * (radius / cleaveBaseRadius);
            foreach (var r in ring.GetComponentsInChildren<Renderer>())
                r.material.color = new Color(1f, 0.85f, 0.45f, r.material.color.a);
        }
        else ring = new GameObject("SearingHalo");
        ring.name = "SearingHalo";
        // Explicit check: GetComponent returns a Unity fake-null in the editor, so "??" would not fall back.
        var light = ring.GetComponent<Light>();
        if (light == null) light = ring.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.range = radius * 2f;
        light.intensity = 4f;
        Destroy(ring, 0.6f);
    }

    /// <summary>
    /// Seismic Slam (tank specialty, party-roles S2): shockwave around the
    /// tank — every husk inside <see cref="slamRadius"/> takes the blow, holds
    /// still for <see cref="slamStunSeconds"/> and books
    /// <see cref="ThreatTable.SlamThreatBonus"/> threat on top of the damage,
    /// so aggro swings to the tank. No knockback: the tank holds the line.
    /// Cosmetic mirrors only spawn the visual, never damage, stun or threat.
    /// </summary>
    public int DoSlam(bool cosmetic)
    {
        int hits = 0;
        float radius = ScaledArea(slamRadius);
        if (!cosmetic)
        {
            ExitBowStance();
            var driver = GetComponent<DiabloAnimator>();
            if (driver != null) driver.OnStrike();
            int blow = EffectiveDamage(Skill.SeismicSlam);
            var slamEq = GetComponent<Equipment>();
            var slamTargets = new System.Collections.Generic.List<GameObject>();
            foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            {
                if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
                var to = husk.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > radius) continue;
                slamTargets.Add(husk);
            }
            // S4 seismic-crown: the primary (nearest) target holds longer.
            GameObject primary = null;
            float bestD = float.MaxValue;
            foreach (var t in slamTargets)
            {
                var d = t.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude < bestD) { bestD = d.magnitude; primary = t; }
            }
            ArmKeen(Skill.SeismicSlam);
            foreach (var husk in slamTargets)
            {
                bool crit = ApplyHit(husk, blow, transform.position, slamKnockback, transform);
                TryKeenReduction(Skill.SeismicSlam, crit);
                ThreatTable.BookStun(husk, transform);
                var atk = husk.GetComponent<EnemyAttack>();
                if (atk != null) atk.Stun(BuildUniqueEffects.SlamStunFor(slamEq, slamStunSeconds, husk == primary));
                hits++;
            }
            DiabloAudio.PlayWhoosh();
        }
        SpawnSlamVisual(radius);
        PresentationVfx.CleaveArc(transform.position, radius);
        return hits;
    }

    void SpawnSlamVisual(float radius)
    {
        if (!Application.isPlaying) return;
        var go = new GameObject("SeismicSlam");
        go.transform.position = transform.position + Vector3.up * 0.2f;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.7f, 0.6f, 0.5f);
        light.range = radius * 2f;
        light.intensity = 3f;
        Destroy(go, 0.4f);
    }

    // ---------- Rogue + Healer (party-roles S3) ----------

    /// <summary>
    /// Blade Flurry (rogue specialty): quick blades through the frontal melee
    /// cone (<see cref="DiabloMelee.ArcHalfAngle"/>) — every husk inside
    /// <see cref="flurryRadius"/> takes the blow with a small pop.
    /// Cosmetic mirrors only spawn the visual, never damage.
    /// </summary>
    public int DoFlurry(Vector3 dir, bool cosmetic)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        int hits = 0;
        if (!cosmetic)
        {
            ExitBowStance();
            var driver = GetComponent<DiabloAnimator>();
            if (driver != null) driver.OnStrike();
            int blow = EffectiveDamage(Skill.BladeFlurry);
            float radius = ScaledArea(flurryRadius);
            ArmKeen(Skill.BladeFlurry);
            foreach (var husk in DiabloMelee.FindArcTargets(transform, dir, radius, DiabloMelee.ArcHalfAngle))
            {
                if (husk == null) continue;
                bool crit = ApplyHit(husk, blow, transform.position, flurryKnockback, transform);
                TryKeenReduction(Skill.BladeFlurry, crit);
                hits++;
            }
            // S4 shadowstride: flurry hits grant the wearer brief haste.
            if (hits > 0) BuildUniqueEffects.GrantShadowstrideHaste(gameObject);
            DiabloAudio.PlayWhoosh();
            PresentationVfx.CleaveArc(transform.position, radius);
            return hits;
        }
        PresentationVfx.CleaveArc(transform.position, ScaledArea(flurryRadius));
        return hits;
    }

    /// <summary>Nearest injured living ally (not self) inside range, or null.</summary>
    public PlayerHealth FindHealTarget(float range)
    {
        PlayerHealth best = null;
        float bestD = float.MaxValue;
        foreach (var hp in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (hp == null || hp.transform == transform) continue;
            if (hp.CurrentHP <= 0f || hp.CurrentHP >= hp.MaxHP) continue;
            var to = hp.transform.position - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            if (d <= range && d < bestD) { bestD = d; best = hp; }
        }
        return best;
    }

    /// <summary>
    /// Mending Light (healer specialty): mends the nearest injured ally in
    /// <see cref="targetHealRange"/> for <see cref="EffectiveHeal"/> and books
    /// the restored HP as threat. Returns the HP actually restored.
    /// Cosmetic mirrors only spawn the visual, never healing or threat.
    /// </summary>
    public float DoTargetHeal(bool cosmetic)
    {
        float restored = 0f;
        var target = FindHealTarget(targetHealRange);
        if (!cosmetic && target != null)
        {
            restored = target.Heal(EffectiveHeal(Skill.MendingLight));
            ThreatTable.BookHeal(transform, restored);
            RelayHeal(target.gameObject, restored, Skill.MendingLight);
            // S4 mercy-shroud: the healed ally keeps a short absorb shield.
            if (BuildUniqueEffects.HasMercyShroud(GetComponent<Equipment>()))
                target.GrantShield(BuildUniqueEffects.MercyShieldAmount, BuildUniqueEffects.MercyShieldSeconds);
            if (restored > 0f) { HealsCast++; HPRestored += restored; }
        }
        SpawnHealVisual(targetHealRange);
        return restored;
    }

    /// <summary>
    /// Renewing Circle (healer specialty): mends the healer and every living
    /// ally inside <see cref="groupHealRadius"/> for <see cref="EffectiveHeal"/>
    /// and books the restored HP as threat. Returns the HP actually restored.
    /// Cosmetic mirrors only spawn the visual, never healing or threat.
    /// </summary>
    public float DoGroupHeal(bool cosmetic)
    {
        float restored = 0f;
        float radius = ScaledArea(groupHealRadius);
        if (!cosmetic)
        {
            int amount = EffectiveHeal(Skill.RenewingCircle);
            var healed = new System.Collections.Generic.List<GameObject>();
            foreach (var hp in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                if (hp == null) continue;
                if (hp.CurrentHP <= 0f) continue; // the dead respawn at camp, they are not mended
                var to = hp.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > radius) continue;
                float got = hp.Heal(amount);
                restored += got;
                if (got > 0f) healed.Add(hp.gameObject);
            }
            ThreatTable.BookHeal(transform, restored);
            foreach (var go in healed) RelayHeal(go, amount, Skill.RenewingCircle);
            // S4 dawn-circle: the weakest injured ally in radius is mended once more.
            if (BuildUniqueEffects.HasDawnCircle(GetComponent<Equipment>()))
            {
                var weakest = BuildUniqueEffects.WeakestAlly(transform, radius);
                if (weakest != null)
                {
                    float bonus = weakest.Heal(amount);
                    restored += bonus;
                    ThreatTable.BookHeal(transform, bonus);
                    RelayHeal(weakest.gameObject, bonus, Skill.RenewingCircle);
                }
            }
            if (restored > 0f) { HealsCast++; HPRestored += restored; }
        }
        SpawnHealVisual(radius);
        return restored;
    }

    /// <summary>
    /// Forward a locally-applied heal on a remote player's hero to its owner
    /// (party-roles S5). Solo, self-heals and host-local targets need no
    /// relay: the local apply already stands. Hosts fan out straight to the
    /// owner, clients ask the server first (validated there).
    /// </summary>
    void RelayHeal(GameObject target, float amount, Skill kind)
    {
        if (amount <= 0f || target == null) return;
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var nm = NetworkManager.Singleton;
        if (netPlayer == null || !netPlayer.IsSpawned || nm == null || !nm.IsListening) return;
        var targetNb = target.GetComponent<NetworkObject>();
        if (targetNb == null || !targetNb.IsPlayerObject || targetNb.IsOwner) return;
        if (nm.IsServer) netPlayer.HealTargetClientRpc(targetNb.NetworkObjectId, amount, nm.LocalClientId);
        else netPlayer.SubmitHealServerRpc(targetNb.NetworkObjectId, amount, (int)kind);
    }

    void SpawnHealVisual(float radius)
    {
        PresentationVfx.CleaveArc(transform.position, radius);
        if (!Application.isPlaying) return;
        var go = new GameObject("MendingLight");
        go.transform.position = transform.position + Vector3.up * 1.2f;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.5f, 1f, 0.6f);
        light.range = radius;
        light.intensity = 2.5f;
        Destroy(go, 0.5f);
    }

    /// <summary>
    /// Veil Step (Space): blink up to <see cref="veilDistance"/> along dir,
    /// stopping in front of the first solid collider. Returns the distance moved.
    /// </summary>
    public float DoVeilStep(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        float dist = VeilClearDistance(transform.position, dir, veilDistance, transform);
        var from = transform.position;
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = from + dir * dist;
        transform.rotation = Quaternion.LookRotation(dir);
        if (cc != null) cc.enabled = true;
        SpawnVeilPuff(from);
        SpawnVeilPuff(transform.position);
        DiabloAudio.PlayVeil();
        return dist;
    }

    /// <summary>Free distance for a hero-sized capsule along dir (husks and triggers ignored).</summary>
    public static float VeilClearDistance(Vector3 from, Vector3 dir, float max, Transform self)
    {
        var p0 = from + Vector3.up * (0.4f + VeilHeroRadius);
        var p1 = from + Vector3.up * 1.6f;
        float best = max;
        foreach (var h in Physics.CapsuleCastAll(p0, p1, VeilHeroRadius, dir, max, ~0, QueryTriggerInteraction.Ignore))
        {
            var t = h.collider.transform;
            if (self != null && (t == self || t.IsChildOf(self))) continue;
            if (h.collider.CompareTag("Husk") || h.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (h.distance <= 0f) continue; // already overlapping: ignore (start inside)
            best = Mathf.Min(best, Mathf.Max(0f, h.distance - 0.1f));
        }
        return best;
    }

    static void SpawnVeilPuff(Vector3 at)
    {
        PresentationVfx.VeilPuff(at);
        if (!Application.isPlaying) return;
        var go = new GameObject("VeilPuff");
        go.transform.position = at + Vector3.up;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.75f, 0.8f, 1f);
        light.range = 4f;
        light.intensity = 3f;
        Destroy(go, 0.3f);
    }

    /// <summary>
    /// Cleave (R, S4): instant 360° melee arc — every husk inside
    /// <see cref="CleaveRadius"/> takes the ranked blow with knockback.
    /// The frontal variant (skill-variants S2) trades the back half for a
    /// 90° cone at 1.5× range; damage and cooldown paths stay the same.
    /// Cosmetic mirrors only spawn the arc visual, never damage.
    /// </summary>
    public void DoCleave(Vector3 dir, bool cosmetic)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        string variant = ActiveVariantId(Skill.Cleave);
        float radius = ScaledArea(SkillVariantCatalog.CleaveRadiusFor(this, variant));
        float arc = SkillVariantCatalog.CleaveArcFor(variant);
        if (!cosmetic)
        {
            ExitBowStance();
            var driver = GetComponent<DiabloAnimator>();
            if (driver != null) driver.OnStrike();
            int blow = EffectiveDamage(Skill.Cleave);
            foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            {
                if (husk == null) continue;
                if (!SkillVariantTargeting.InCleaveArea(transform.position, dir,
                    husk.transform.position, radius, arc)) continue;
                ApplyHit(husk, blow, transform.position, cleaveKnockback, transform);
            }
            // Build unique (lancer-ward S2): at most one interrupt per cast, variant-independent.
            BuildUniqueEffects.TryInterruptCharge(transform, GetComponent<Equipment>(), radius);
            DiabloAudio.PlayWhoosh();
        }
        SpawnCleaveVisual(radius);
    }

    void SpawnCleaveVisual(float radius)
    {
        PresentationVfx.CleaveArc(transform.position, radius);
        var prefab = Resources.Load<GameObject>("SkillArt/CleaveArc");
        if (prefab == null) return;
        var arc = Instantiate(prefab, transform.position, Quaternion.identity);
        arc.transform.localScale = Vector3.one * (radius / cleaveBaseRadius);
        Destroy(arc, 0.6f);
    }

    /// <summary>
    /// Render another player's cast on this client: same flight and impact
    /// art, no damage (the caster's own client applies it). No cooldown gate.
    /// </summary>
    public DiabloProjectile ShowRemoteCast(Skill s, Vector3 origin, Vector3 dir)
    {
        if (s == Skill.Cleave)
        {
            SpawnCleaveVisual(CleaveRadius);
            return null;
        }
        if (s == Skill.SearingHalo) { DoHalo(cosmetic: true); return null; }
        if (s == Skill.SeismicSlam) { DoSlam(cosmetic: true); return null; }
        if (s == Skill.Wisps) { SummonWisps(cosmetic: true); return null; }
        if (s == Skill.BladeFlurry) { DoFlurry(dir, cosmetic: true); return null; }
        if (s == Skill.MendingLight) { DoTargetHeal(cosmetic: true); return null; }
        if (s == Skill.RenewingCircle) { DoGroupHeal(cosmetic: true); return null; }
        if (s == Skill.VeilStep) { SpawnVeilPuff(origin); return null; }
        return Launch(s, origin, dir, cosmetic: true);
    }

    DiabloProjectile Launch(Skill s, Vector3 origin, Vector3 dir, bool cosmetic)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        // S3 VFX hooks only: flight art, no damage or cooldown change.
        if (s == Skill.Fireball || s == Skill.LanternBolt) PresentationVfx.FireballTrail(origin);
        else if (s == Skill.Arrow) PresentationVfx.ArrowTrail(origin);
        if (s == Skill.LanternBolt)
        {
            var bolt = DiabloProjectile.Spawn(s, origin, dir, transform, EffectiveDamage(s),
                lanternBoltSpeed, lanternBoltRange, 0f, cosmetic);
            bolt.pierce = lanternBoltPierce;
            var d = GetComponent<DiabloAnimator>();
            if (d != null) d.OnStrike();
            DiabloAudio.PlayLantern();
            return bolt;
        }
        if (s == Skill.QuickShot)
        {
            var q = DiabloProjectile.Spawn(s, origin, dir, transform, EffectiveDamage(s),
                quickShotSpeed, quickShotRange, 0f, cosmetic);
            // S4 nightfang: the rogue specialty pierces one extra target.
            q.pierce = BuildUniqueEffects.QuickShotPierce(GetComponent<Equipment>());
            if (!cosmetic) ArmKeen(Skill.QuickShot);
            var qd = GetComponent<DiabloAnimator>();
            EnterBowStance();
            if (qd != null) qd.OnBowShot();
            if (!cosmetic) DiabloAudio.PlayTwang();
            return q;
        }
        var p = s == Skill.Fireball
            ? DiabloProjectile.Spawn(s, origin, dir, transform, EffectiveDamage(s),
                fireballSpeed, fireballRange, ScaledArea(fireballBlastRadius), cosmetic)
            : DiabloProjectile.Spawn(s, origin, dir, transform, EffectiveDamage(s),
                arrowSpeed, arrowRange, 0f, cosmetic);
        var driver = GetComponent<DiabloAnimator>();
        if (s == Skill.Arrow)
        {
            EnterBowStance();
            if (driver != null) driver.OnBowShot();
        }
        else if (driver != null) driver.OnStrike();
        if (s == Skill.Arrow) DiabloAudio.PlayTwang();
        else DiabloAudio.PlayWhoosh();
        return p;
    }

    /// <summary>Weapon Damage affix scales arrows like melee swings.</summary>
    int WeaponBonus()
    {
        var eq = GetComponent<Equipment>();
        return eq != null ? Mathf.Max(0, Mathf.RoundToInt(eq.GetDamage() - Equipment.BaseDamage)) : 0;
    }

    /// <summary>
    /// Apply one projectile hit: direct damage solo/host, ServerRpc relay as a
    /// netcode client (the server re-validates range + cooldown + alive + damage
    /// via HitValidator, S2 host-authoritative).
    /// Exactly one crit roll per direct hit (S3); periodic ticks, thorns and
    /// heals pass <c>canCrit: false</c> and keep their normal result. Healing
    /// never rolls (this path only runs for damage).
    /// </summary>
    public static bool ApplyHit(GameObject target, int damage, Vector3 from, float knockback,
        Transform attacker = null, bool canCrit = true)
    {
        if (target == null) return false;
        var health = target.GetComponent<EnemyHealth>();
        if (health == null) return false;
        bool crit = false;
        if (canCrit) damage = DiabloJuice.RollCrit(attacker, damage, out crit);
        // Party threat (S2): local + client-prediction booking; the server books
        // authoritative threat when it applies damage (HitValidator, S5 wiring).
        ThreatTable.BookDamage(target, attacker, damage);
        var nm = NetworkManager.Singleton;
        var netObj = target.GetComponent<NetworkObject>();
        if (nm != null && nm.IsListening && !nm.IsServer && netObj != null && netObj.IsSpawned)
        {
            var player = nm.SpawnManager.GetLocalPlayerObject();
            var relay = player != null ? player.GetComponent<DiabloNetworkPlayer>() : null;
            if (relay != null) relay.SubmitRangedHitServerRpc(netObj.NetworkObjectId, damage);
            DiabloJuice.OnEnemyHit(target.transform.position, damage, crit);
            return crit;
        }
        health.TakeDamage(damage, crit, from);
        Equipment.ApplyLifeSteal(attacker, damage);
        if (health != null)
            health.ApplyKnockback(from, health.tier == LootTier.Normal ? knockback : knockback * 0.3f);
        DiabloAudio.PlayHit();
        DiabloMelee.LastStruck = target;
        DiabloMelee.LastStruckTime = Time.time;
        return crit;
    }

    void Update()
    {
        // Runs on mirrors too: remote casts draw the bow there as well.
        if (BowDrawn && Time.time > bowUntil) ExitBowStance();
        if (IsRemoteMirror) return;
        if (controller == null || !controller.isActiveAndEnabled) return;
        if (DiabloClickController.ExternalDrive) { UpdateHud(); return; }
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null && mouse == null) return; // headless/tests: API only
        if (DiabloClickController.ShouldBlockClicks(GetComponent<DiabloInventoryUI>()))
            return;
        var charUI = GetComponent<DiabloCharacterUI>();
        if (charUI != null && charUI.IsOpen) return;

        // Keys come from the class: Q/E/R(/Space/F/G) map to its skill slots.
        // RMB is the melee outreach thrust (DiabloClickController), never a skill.
        var skills = ActiveClass.skills;
        if (kb != null && kb.qKey.wasPressedThisFrame && skills.Length > 0) Cast(skills[0]);
        else if (kb != null && kb.eKey.wasPressedThisFrame && skills.Length > 1) Cast(skills[1]);
        else if (kb != null && kb.rKey.wasPressedThisFrame && skills.Length > 2) Cast(skills[2]);
        else if (kb != null && kb.spaceKey.wasPressedThisFrame && skills.Length > 3) Cast(skills[3]);
        else if (kb != null && kb.fKey.wasPressedThisFrame && skills.Length > 4) Cast(skills[4]);
        else if (kb != null && kb.gKey.wasPressedThisFrame && skills.Length > 5) Cast(skills[5]);

        UpdateHud();
    }

    void UpdateHud()
    {
        if (hud == null) BuildHud();
        if (hudBuiltFor != ActiveClass.id) RebuildHudSlots();
        hud.text = HudLine();
        foreach (var (skill, frame, veil, cdText) in hudSlots)
        {
            float left = CooldownLeft(skill);
            bool onCd = left > 0f;
            if (veil != null) veil.enabled = onCd;
            if (cdText != null)
            {
                cdText.enabled = onCd;
                if (onCd) cdText.text = $"{left:0.0}";
            }
            if (frame != null)
                frame.color = skill == lastUsed ? DiabloPalette.GoldBright : Color.white;
        }
    }

    /// <summary>Skill bar text from the class (Fenwalker keeps "Fireball / Bow / Cleave").</summary>
    public string HudLine()
    {
        var cls = ActiveClass;
        var parts = new System.Collections.Generic.List<string>();
        for (int i = 0; i < cls.skills.Length; i++)
            parts.Add(Slot(cls.keys[i], SkillName(cls.skills[i]), cls.skills[i]));
        return string.Join("     ", parts.ToArray()) + Loc.T("hud.skillbar.thrust");
    }

    public static string SkillName(Skill s)
    {
        switch (s)
        {
            case Skill.Fireball: return Loc.T("name.skill.fireball");
            case Skill.Arrow: return Loc.T("name.skill.arrow");
            case Skill.Cleave: return Loc.T("name.skill.cleave");
            case Skill.LanternBolt: return Loc.T("name.skill.lanternbolt");
            case Skill.Wisps: return Loc.T("name.skill.wisps");
            case Skill.SearingHalo: return Loc.T("name.skill.searinghalo");
            case Skill.SeismicSlam: return Loc.T("name.skill.seismicslam");
            case Skill.QuickShot: return Loc.T("name.skill.quickshot");
            case Skill.BladeFlurry: return Loc.T("name.skill.bladeflurry");
            case Skill.MendingLight: return Loc.T("name.skill.mendinglight");
            case Skill.RenewingCircle: return Loc.T("name.skill.renewingcircle");
            default: return Loc.T("name.skill.veilstep");
        }
    }

    string Slot(string key, string name, Skill s)
    {
        float left = CooldownLeft(s);
        return left > 0f ? $"[{key}] {name} {left:0.0}s" : $"[{key}] {name}";
    }

    void BuildHud()
    {
        var canvasGo = new GameObject("SkillBarCanvas");
        canvasGo.transform.SetParent(transform, false);
        DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 5, raycaster: false);

        // Bronze HUD plate under skills, key labels and the XP bar (XpCanvas sorts above at 6),
        // so the bar reads against bright town paving and dark fen alike.
        var plateGo = new GameObject("HudPlate", typeof(RectTransform));
        plateGo.transform.SetParent(canvasGo.transform, false);
        var plate = plateGo.AddComponent<Image>();
        plate.sprite = DiabloMenuArt.PanelBackground();
        plate.type = Image.Type.Sliced;
        plate.color = new Color(1f, 1f, 1f, 0.92f);
        plate.raycastTarget = false;
        var prt = plate.rectTransform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0f);
        prt.sizeDelta = new Vector2(880f, 100f);
        prt.anchoredPosition = new Vector2(0f, -6f);

        var rowGo = new GameObject("SkillSlotsRow", typeof(RectTransform));
        rowGo.transform.SetParent(canvasGo.transform, false);
        var rrt = rowGo.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0f);
        rrt.sizeDelta = new Vector2(340f, 52f);
        rrt.anchoredPosition = new Vector2(0f, 20f);
        var hz = rowGo.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = 10f;
        hz.childAlignment = TextAnchor.MiddleCenter;
        hz.childControlWidth = false;
        hz.childControlHeight = false;
        slotsContainer = rowGo.transform;

        var go = new GameObject("SkillBar");
        go.transform.SetParent(canvasGo.transform, false);
        hud = go.AddComponent<Text>();
        hud.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hud.fontSize = 14;
        hud.alignment = TextAnchor.MiddleCenter;
        hud.color = DiabloPalette.Parchment;
        hud.horizontalOverflow = HorizontalWrapMode.Overflow;
        hud.raycastTarget = false;
        go.AddComponent<Shadow>();
        var r = hud.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(920f, 20f);
        r.anchoredPosition = new Vector2(0f, 2f);

        RebuildHudSlots();
    }

    void RebuildHudSlots()
    {
        if (slotsContainer == null) return;
        foreach (Transform c in slotsContainer)
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        hudSlots.Clear();

        var cls = ActiveClass;
        for (int i = 0; i < cls.skills.Length; i++)
        {
            var sk = cls.skills[i];
            string key = cls.keys[i] == "Space" ? Loc.T("key.space.short") : cls.keys[i];

            var slot = new GameObject($"SkillSlot_{sk}", typeof(RectTransform));
            slot.transform.SetParent(slotsContainer, false);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(50f, 50f);
            var frame = slot.AddComponent<Image>();
            frame.sprite = DiabloMenuArt.SlotBackground(sk == lastUsed ? ItemRarity.Rare : (ItemRarity?)null, 64);
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;

            var spr = DiabloMenuArt.SkillIcon(sk);
            if (spr != null)
            {
                var iGo = new GameObject("Icon", typeof(RectTransform));
                iGo.transform.SetParent(slot.transform, false);
                var iImg = iGo.AddComponent<Image>();
                iImg.sprite = spr;
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
                var ir = iGo.GetComponent<RectTransform>();
                ir.anchorMin = Vector2.zero;
                ir.anchorMax = Vector2.one;
                ir.offsetMin = new Vector2(4f, 4f);
                ir.offsetMax = new Vector2(-4f, -4f);
            }

            var veilGo = new GameObject("CooldownVeil", typeof(RectTransform));
            veilGo.transform.SetParent(slot.transform, false);
            var veil = veilGo.AddComponent<Image>();
            veil.color = new Color(0.02f, 0.02f, 0.04f, 0.72f);
            veil.raycastTarget = false;
            var vr = veilGo.GetComponent<RectTransform>();
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.offsetMin = vr.offsetMax = Vector2.zero;
            veil.enabled = false;

            var cd = DiabloUIFactory.MakeText(slot.transform, "CdText", "", 15, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
            cd.fontStyle = FontStyle.Bold;
            var cdr = cd.rectTransform;
            cdr.anchorMin = Vector2.zero;
            cdr.anchorMax = Vector2.one;
            cdr.offsetMin = cdr.offsetMax = Vector2.zero;
            cd.enabled = false;

            var keyLbl = DiabloUIFactory.MakeText(slot.transform, "KeyBadge", key, 11, DiabloPalette.TrimGold, TextAnchor.LowerRight);
            keyLbl.fontStyle = FontStyle.Bold;
            var kr = keyLbl.rectTransform;
            kr.anchorMin = Vector2.zero;
            kr.anchorMax = Vector2.one;
            kr.offsetMin = new Vector2(2f, 2f);
            kr.offsetMax = new Vector2(-4f, -2f);

            hudSlots.Add((sk, frame, veil, cd));
        }
        hudBuiltFor = cls.id;
    }
}
