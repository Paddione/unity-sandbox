using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-build-uniques S2: lancer-ward Cleave interrupt and
/// wick-of-the-halo Wisp priority. Effects are variant-independent (#9
/// SkillVariantTargeting is not on main: base skill geometry is used);
/// unequipping ends effects, halo expiry and reload-equivalent reset end the
/// Wisp priority.
/// </summary>
public class BuildUniqueEffectTests
{
    GameObject player;
    Equipment eq;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        HitStop.Enabled = false;
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        BuildUniqueEffects.ResetRun();
        AshLancer.ResetRun();
        Wisp.ResetRun();
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloClickController>();
        eq = player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        skills = player.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var w in Object.FindObjectsOfType<Wisp>())
            Object.DestroyImmediate(w.gameObject);
        foreach (var l in Object.FindObjectsOfType<AshLancer>())
            Object.DestroyImmediate(l.gameObject);
        DiabloTestFixture.CleanupTestScene(player);
        BuildUniqueEffects.ResetRun();
        AshLancer.ResetRun();
        Wisp.ResetRun();
        RunProfile.Reset();
        HitStop.Enabled = true;
    }

    static GameObject SpawnHusk(Vector3 pos, int hp = 30)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>().SetTier(hp, LootTier.Normal);
        return husk;
    }

    AshLancer SpawnChargingLancer(Vector3 pos)
    {
        var go = new GameObject("TestLancer");
        go.transform.position = pos;
        var lancer = go.AddComponent<AshLancer>();
        float t0 = Time.time;
        lancer.Tick(t0, 0.02f, player.transform);
        Assert.AreEqual(AshLancer.State.Telegraph, lancer.Current, "lancer in range must telegraph");
        lancer.Tick(t0 + AshLancer.Telegraph + 0.1f, 0.02f, player.transform);
        Assert.AreEqual(AshLancer.State.Charge, lancer.Current, "lancer must charge after the telegraph");
        return lancer;
    }

    void EquipWard() => eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.LancerWardId));

    [Test]
    public void Cleave_WithWard_InterruptsAtMostOneChargingLancer()
    {
        EquipWard();
        var first = SpawnChargingLancer(new Vector3(2f, 0f, 0f));
        var second = SpawnChargingLancer(new Vector3(-2f, 0f, 0f));
        skills.DoCleave(Vector3.forward, cosmetic: false);
        int exhausted = (first.Current == AshLancer.State.Exhausted ? 1 : 0)
            + (second.Current == AshLancer.State.Exhausted ? 1 : 0);
        Assert.AreEqual(1, exhausted, "one Cleave interrupts at most one lancer");
    }

    [Test]
    public void Cleave_WithoutWard_InterruptsNothing()
    {
        var first = SpawnChargingLancer(new Vector3(2f, 0f, 0f));
        var second = SpawnChargingLancer(new Vector3(-2f, 0f, 0f));
        skills.DoCleave(Vector3.forward, cosmetic: false);
        Assert.AreEqual(AshLancer.State.Charge, first.Current);
        Assert.AreEqual(AshLancer.State.Charge, second.Current);
    }

    [Test]
    public void Cleave_InterruptedLancer_UsesExistingRecovery()
    {
        EquipWard();
        var lancer = SpawnChargingLancer(new Vector3(2f, 0f, 0f));
        skills.DoCleave(Vector3.forward, cosmetic: false);
        Assert.AreEqual(AshLancer.State.Exhausted, lancer.Current);
        lancer.Tick(Time.time + AshLancer.Exhaust + 0.5f, 0.02f, player.transform);
        Assert.AreEqual(AshLancer.State.Approach, lancer.Current, "interrupted lancer recovers like a spent charge");
    }

    [Test]
    public void Cleave_UnequippedWard_EndsEffect()
    {
        EquipWard();
        var lancer = SpawnChargingLancer(new Vector3(2f, 0f, 0f));
        Assert.IsNotNull(BuildUniqueEffects.TryInterruptCharge(
            player.transform, eq, skills.CleaveRadius));
        eq.Equip(ItemDatabase.CreateUnique("Emberbrand Blade"));
        var other = SpawnChargingLancer(new Vector3(-2f, 0f, 0f));
        Assert.IsNull(BuildUniqueEffects.TryInterruptCharge(
            player.transform, eq, skills.CleaveRadius));
        Assert.AreEqual(AshLancer.State.Charge, other.Current);
        Assert.AreEqual(AshLancer.State.Exhausted, lancer.Current);
    }

    Wisp SpawnWisp()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.WickOfHaloId));
        var wisp = Wisp.Spawn(player.transform, 0, 1, 1, 60f, cosmetic: true);
        wisp.reach = 6f;
        return wisp;
    }

    [Test]
    public void Wisp_WithHalo_PrefersTargetInsideHalo()
    {
        var wisp = SpawnWisp();
        var near = SpawnHusk(new Vector3(1f, 1.05f, 0f));
        var inHalo = SpawnHusk(new Vector3(4.5f, 1.05f, 0f));
        BuildUniqueEffects.NotifyHalo(new Vector3(4f, 0f, 0f), 2f, 60f);
        float now = Time.time;
        Assert.IsTrue(BuildUniqueEffects.HaloActiveAt(inHalo.transform.position, now));
        Assert.IsFalse(BuildUniqueEffects.HaloActiveAt(near.transform.position, now));
        Assert.AreEqual(inHalo, wisp.TryStrike(now), "halo target wins over the nearer husk");
    }

    [Test]
    public void Wisp_WithoutHalo_PicksNearest()
    {
        var wisp = SpawnWisp();
        var near = SpawnHusk(new Vector3(1f, 1.05f, 0f));
        SpawnHusk(new Vector3(4.5f, 1.05f, 0f));
        Assert.AreEqual(near, wisp.TryStrike(Time.time));
    }

    [Test]
    public void Wisp_WithoutLantern_PicksNearestDespiteHalo()
    {
        var wisp = SpawnWisp();
        eq.Equip(ItemDatabase.CreateUnique("Lantern of the Hold"));
        var near = SpawnHusk(new Vector3(1f, 1.05f, 0f));
        var inHalo = SpawnHusk(new Vector3(4.5f, 1.05f, 0f));
        BuildUniqueEffects.NotifyHalo(new Vector3(4f, 0f, 0f), 2f, 60f);
        Assert.AreEqual(near, wisp.TryStrike(Time.time));
        Assert.IsNotNull(inHalo);
    }

    [UnityTest]
    public IEnumerator Wisp_HaloExpiry_EndsPriority()
    {
        var wisp = SpawnWisp();
        var near = SpawnHusk(new Vector3(1f, 1.05f, 0f));
        var inHalo = SpawnHusk(new Vector3(4.5f, 1.05f, 0f));
        BuildUniqueEffects.NotifyHalo(new Vector3(4f, 0f, 0f), 2f, 0.05f);
        Assert.AreEqual(inHalo, wisp.TryStrike(Time.time), "burning halo prioritizes");
        yield return DiabloTestFixture.WaitSeconds(0.3f);
        var wisp2 = Wisp.Spawn(player.transform, 0, 1, 1, 60f, cosmetic: true);
        wisp2.reach = 6f;
        Assert.AreEqual(near, wisp2.TryStrike(Time.time), "expired halo falls back to nearest");
    }

    [Test]
    public void Wisp_ReloadClearsHalo_PicksNearest()
    {
        var wisp = SpawnWisp();
        var near = SpawnHusk(new Vector3(1f, 1.05f, 0f));
        SpawnHusk(new Vector3(4.5f, 1.05f, 0f));
        BuildUniqueEffects.NotifyHalo(new Vector3(4f, 0f, 0f), 2f, 60f);
        BuildUniqueEffects.ResetRun(); // reload semantic: halo state is runtime-only
        Assert.AreEqual(near, wisp.TryStrike(Time.time));
    }

    // ---------------- S4 role uniques (cinderreach-itemization S4) ----------------
    //
    // Per effect: equip, trigger, unequip, reload (+ two wearers without state
    // leaks). Durations and reset rules, pinned here:
    // - seismic-crown: +2 s stun on the slam primary only, read live per cast.
    // - bulwark-carrier: blocked frontal hit books SlamThreatBonus (20) threat.
    // - mercy-shroud: 5 HP shield for 4 s on the mended ally, refresh per cast.
    // - dawn-circle: weakest injured ally in radius mended once more, one bonus
    //   per cast, never stacked.
    // - shadowstride-vest: +30 % walk speed for 3 s, refreshed per flurry hit,
    //   ends early on unequip, per-wearer state on the hero.
    // - keen-cadence: first crit of a special cast cuts its cooldown by 2 s,
    //   once per cast, per-hero pending state.
    // - nightfang: Quick Shot pierces one extra target (base arrows never pierce).

    readonly System.Collections.Generic.List<GameObject> s4allies =
        new System.Collections.Generic.List<GameObject>();

    GameObject SpawnAlly(Vector3 pos, float damage)
    {
        var ally = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ally.name = "TestAlly";
        ally.transform.position = pos;
        ally.AddComponent<Equipment>();
        var hp = ally.AddComponent<PlayerHealth>();
        Assert.AreEqual(0f, hp.ShieldAmount, "fresh hero spawns without a shield");
        hp.TakeDamage(damage);
        s4allies.Add(ally);
        return ally;
    }

    void EquipHelmet(string id) => eq.Equip(ItemDatabase.CreateUnique(id));

    [TearDown]
    public void S4TearDown()
    {
        foreach (var a in s4allies)
            if (a != null) Object.DestroyImmediate(a);
        s4allies.Clear();
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>())
            if (p != null) Object.DestroyImmediate(p.gameObject);
    }

    [Test]
    public void SlamStun_PrimaryBonus_LivePerCast()
    {
        Assert.AreEqual(2f, BuildUniqueEffects.SlamStunFor(eq, 2f, true), "no crown, no bonus");
        EquipHelmet(ItemDatabase.SeismicCrownId);
        Assert.AreEqual(4f, BuildUniqueEffects.SlamStunFor(eq, 2f, true), "crown holds the primary +2 s");
        Assert.AreEqual(2f, BuildUniqueEffects.SlamStunFor(eq, 2f, false), "pack keeps the base stun");
        eq.Equip(ItemDatabase.CreateUnique("Seer's Circlet")); // unequip
        Assert.AreEqual(2f, BuildUniqueEffects.SlamStunFor(eq, 2f, true), "unequipped crown ends the bonus");
    }

    [UnityTest]
    public IEnumerator Slam_WithCrown_HoldsPrimaryPastBaseStun()
    {
        var husk = SpawnHusk(new Vector3(1.5f, 1.05f, 0f));
        husk.AddComponent<EnemyAttack>();
        var atk = husk.GetComponent<EnemyAttack>();
        EquipHelmet(ItemDatabase.SeismicCrownId);
        skills.DoSlam(cosmetic: false);
        Assert.IsTrue(atk.IsStunned, "crowned slam stuns");
        yield return DiabloTestFixture.WaitSeconds(skills.slamStunSeconds + 0.6f);
        Assert.IsTrue(atk.IsStunned, "crown holds the primary past the base stun (+2 s)");
    }

    [UnityTest]
    public IEnumerator Slam_WithoutCrown_BaseStunExpires()
    {
        var husk = SpawnHusk(new Vector3(1.5f, 1.05f, 0f));
        husk.AddComponent<EnemyAttack>();
        var atk = husk.GetComponent<EnemyAttack>();
        skills.DoSlam(cosmetic: false);
        Assert.IsTrue(atk.IsStunned, "slam stuns without the crown too");
        yield return DiabloTestFixture.WaitSeconds(skills.slamStunSeconds + 0.6f);
        Assert.IsFalse(atk.IsStunned, "base stun expires without the crown");
    }

    [Test]
    public void Slam_TwoWearers_NoSharedState()
    {
        var other = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        other.transform.position = new Vector3(10f, 1.05f, 0f);
        other.AddComponent<DiabloClickController>();
        var otherEq = other.AddComponent<Equipment>();
        s4allies.Add(other);
        EquipHelmet(ItemDatabase.SeismicCrownId);
        otherEq.Equip(ItemDatabase.CreateUnique(ItemDatabase.SeismicCrownId));
        Assert.AreEqual(4f, BuildUniqueEffects.SlamStunFor(eq, 2f, true));
        Assert.AreEqual(4f, BuildUniqueEffects.SlamStunFor(otherEq, 2f, true));
        eq.Equip(ItemDatabase.CreateUnique("Seer's Circlet"));
        Assert.AreEqual(2f, BuildUniqueEffects.SlamStunFor(eq, 2f, true), "unequip ends it here");
        Assert.AreEqual(4f, BuildUniqueEffects.SlamStunFor(otherEq, 2f, true), "other wearer keeps it");
    }

    [Test]
    public void Block_WithBulwark_BooksThreat_WithoutArmor_Nothing()
    {
        player.GetComponent<DiabloBlock>().Held = true; // controller owns the block (Awake)
        player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        var hp = player.AddComponent<PlayerHealth>();
        var attacker = SpawnHusk(new Vector3(0f, 1.05f, 0.5f));
        var blk = player.GetComponent<DiabloBlock>();
        Assert.IsTrue(blk.BlocksFrom(attacker.transform.position),
            $"held={blk.Held} fwd={player.transform.forward} hero={player.transform.position} atk={attacker.transform.position}");
        float before = hp.CurrentHP;
        hp.TakeDamage(10f, attacker.transform.position);
        Assert.AreEqual(before - 5f, hp.CurrentHP, 0.001f, "blocked frontal hit lands halved");
        var table = attacker.GetComponent<ThreatTable>();
        Assert.IsNull(table, "no bulwark, no threat booked");
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.BulwarkCarrierId));
        hp.TakeDamage(10f, attacker.transform.position);
        table = attacker.GetComponent<ThreatTable>();
        Assert.IsNotNull(table, "bulwark books threat on the attacker's table");
        Assert.AreEqual(ThreatTable.SlamThreatBonus, table.ThreatOf(player.transform),
            "bulwark threat matches the slam-magnitude bonus");
    }

    [Test]
    public void Mercy_TargetHeal_GrantsShield_AbsorbsPointForPoint()
    {
        var ally = SpawnAlly(new Vector3(2f, 1.05f, 0f), 10f);
        var allyHp = ally.GetComponent<PlayerHealth>();
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.MercyShroudId));
        Assert.Greater(skills.DoTargetHeal(cosmetic: false), 0f, "mending restores HP");
        Assert.AreEqual(BuildUniqueEffects.MercyShieldAmount, allyHp.ShieldAmount,
            "mended ally keeps a 5 HP shield");
        float healed = allyHp.CurrentHP;
        allyHp.TakeDamage(3f);
        Assert.AreEqual(healed, allyHp.CurrentHP, 0.001f, "small hit eats the shield first (HP holds)");
        Assert.AreEqual(2f, allyHp.ShieldAmount, 0.001f, "shield absorbs point for point");
        allyHp.TakeDamage(10f);
        Assert.AreEqual(0f, allyHp.ShieldAmount, 0.001f, "shield spent");
        Assert.AreEqual(healed - 8f, allyHp.CurrentHP, 0.001f, "overflow lands on HP");
    }

    [Test]
    public void Mercy_Unequip_EndsGrant_ReloadStartsClean()
    {
        var ally = SpawnAlly(new Vector3(2f, 1.05f, 0f), 10f);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.MercyShroudId));
        skills.DoTargetHeal(cosmetic: false);
        Assert.Greater(ally.GetComponent<PlayerHealth>().ShieldAmount, 0f);
        eq.Equip(ItemDatabase.CreateUnique("Cryptwarden Plate")); // unequip
        var other = SpawnAlly(new Vector3(-2f, 1.05f, 0f), 10f);
        skills.DoTargetHeal(cosmetic: false);
        Assert.AreEqual(0f, other.GetComponent<PlayerHealth>().ShieldAmount,
            "without the shroud no shield is granted");
    }

    [UnityTest]
    public IEnumerator Mercy_ShieldExpires_AfterFourSeconds()
    {
        var ally = SpawnAlly(new Vector3(2f, 1.05f, 0f), 10f);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.MercyShroudId));
        skills.DoTargetHeal(cosmetic: false);
        Assert.Greater(ally.GetComponent<PlayerHealth>().ShieldAmount, 0f);
        yield return DiabloTestFixture.WaitSeconds(
            BuildUniqueEffects.MercyShieldSeconds + 0.3f);
        Assert.AreEqual(0f, ally.GetComponent<PlayerHealth>().ShieldAmount,
            "4 s shield expires (duration + reset rule: refresh per cast, never stacked)");
    }

    [Test]
    public void DawnCircle_WeakestAlly_MendedTwice_OthersOnce()
    {
        var weak = SpawnAlly(new Vector3(2f, 1.05f, 0f), 16f);
        var hurt = SpawnAlly(new Vector3(-2f, 1.05f, 0f), 8f);
        var weakHp = weak.GetComponent<PlayerHealth>();
        var hurtHp = hurt.GetComponent<PlayerHealth>();
        float weakBefore = weakHp.CurrentHP, hurtBefore = hurtHp.CurrentHP;
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.DawnCircleId));
        Assert.Greater(skills.DoGroupHeal(cosmetic: false), 0f);
        float weakRestored = weakHp.CurrentHP - weakBefore;
        float hurtRestored = hurtHp.CurrentHP - hurtBefore;
        Assert.Greater(weakRestored, 0f);
        Assert.AreEqual(2f * hurtRestored, weakRestored, 0.001f,
            "weakest ally is mended once more (one bonus per cast)");
    }

    [Test]
    public void DawnCircle_WithoutHelm_NoBonus_NobodyToppedTwice()
    {
        var weak = SpawnAlly(new Vector3(2f, 1.05f, 0f), 16f);
        var hurt = SpawnAlly(new Vector3(-2f, 1.05f, 0f), 8f);
        var weakHp = weak.GetComponent<PlayerHealth>();
        var hurtHp = hurt.GetComponent<PlayerHealth>();
        float weakBefore = weakHp.CurrentHP, hurtBefore = hurtHp.CurrentHP;
        Assert.Greater(skills.DoGroupHeal(cosmetic: false), 0f);
        Assert.AreEqual(hurtHp.CurrentHP - hurtBefore, weakHp.CurrentHP - weakBefore, 0.001f,
            "without the helm every ally is mended exactly once");
    }

    [Test]
    public void Shadowstride_FlurryHit_GrantsHaste_UnequipEndsIt()
    {
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 1.5f));
        var controller = player.GetComponent<DiabloClickController>();
        float baseWalk = controller.walkSpeed;
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.ShadowstrideVestId));
        Assert.Greater(skills.DoFlurry(Vector3.forward, cosmetic: false), 0,
            "flurry must hit to grant haste");
        var haste = player.GetComponent<ShadowstrideHaste>();
        Assert.IsNotNull(haste, "flurry hit grants the vest haste on its wearer");
        Assert.AreEqual(baseWalk * BuildUniqueEffects.ShadowstrideHasteFactor,
            controller.walkSpeed, 0.001f, "+30 % move speed while hasted");
        Assert.IsNotNull(husk);
    }

    [UnityTest]
    public IEnumerator Shadowstride_Unequip_EndsHasteEarly()
    {
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 1.5f));
        var controller = player.GetComponent<DiabloClickController>();
        float baseWalk = controller.walkSpeed;
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.ShadowstrideVestId));
        Assert.Greater(skills.DoFlurry(Vector3.forward, cosmetic: false), 0);
        Assert.IsNotNull(player.GetComponent<ShadowstrideHaste>());
        eq.Equip(ItemDatabase.CreateUnique("Cryptwarden Plate")); // unequip mid-haste
        yield return null;
        yield return null;
        Assert.IsNull(player.GetComponent<ShadowstrideHaste>(), "vest off ends the haste early");
        Assert.AreEqual(baseWalk, controller.walkSpeed, 0.001f, "base speed restored on unequip");
        Assert.IsNotNull(husk);
    }

    [UnityTest]
    public IEnumerator Shadowstride_HasteExpires_AfterThreeSeconds()
    {
        SpawnHusk(new Vector3(0f, 1.05f, 1.5f));
        var controller = player.GetComponent<DiabloClickController>();
        float baseWalk = controller.walkSpeed;
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.ShadowstrideVestId));
        Assert.Greater(skills.DoFlurry(Vector3.forward, cosmetic: false), 0);
        yield return DiabloTestFixture.WaitSeconds(
            BuildUniqueEffects.ShadowstrideHasteSeconds + 0.5f);
        Assert.IsNull(player.GetComponent<ShadowstrideHaste>(), "3 s haste expires");
        Assert.AreEqual(baseWalk, controller.walkSpeed, 0.001f, "base speed restored on expiry");
    }

    [Test]
    public void Shadowstride_TwoWearers_IndependentHaste()
    {
        SpawnHusk(new Vector3(0f, 1.05f, 1.5f));
        var other = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        other.transform.position = new Vector3(10f, 1.05f, 0f);
        other.AddComponent<DiabloClickController>();
        var otherEq = other.AddComponent<Equipment>();
        s4allies.Add(other);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.ShadowstrideVestId));
        otherEq.Equip(ItemDatabase.CreateUnique(ItemDatabase.ShadowstrideVestId));
        Assert.IsTrue(BuildUniqueEffects.GrantShadowstrideHaste(player));
        Assert.IsTrue(BuildUniqueEffects.GrantShadowstrideHaste(other));
        Assert.IsNotNull(player.GetComponent<ShadowstrideHaste>());
        Assert.IsNotNull(other.GetComponent<ShadowstrideHaste>());
        Assert.AreNotSame(player.GetComponent<ShadowstrideHaste>(),
            other.GetComponent<ShadowstrideHaste>(), "per-wearer state, never shared");
    }

    [Test]
    public void Keen_FirstCritCutsCooldown_OncePerCast()
    {
        EquipHelmet(ItemDatabase.KeenCadenceId);
        skills.DoSlam(cosmetic: false); // arms the pending special cast
        Assert.IsTrue(skills.TryKeenReduction(DiabloSkills.Skill.SeismicSlam, true),
            "first crit of the cast cuts the cooldown");
        Assert.IsFalse(skills.TryKeenReduction(DiabloSkills.Skill.SeismicSlam, true),
            "second crit of the same cast cuts nothing (once per cast)");
        skills.DoSlam(cosmetic: false); // next cast arms again
        Assert.IsFalse(skills.TryKeenReduction(DiabloSkills.Skill.Fireball, true),
            "base skills never qualify, even with the helm on");
        Assert.IsTrue(skills.TryKeenReduction(DiabloSkills.Skill.SeismicSlam, true),
            "pending special crit still cuts after a base-skill attempt");
    }

    [Test]
    public void Keen_WithoutHelm_NoCut()
    {
        skills.DoSlam(cosmetic: false);
        Assert.IsFalse(skills.TryKeenReduction(DiabloSkills.Skill.SeismicSlam, true),
            "no helm, no cooldown cut");
    }

    [Test]
    public void Keen_CastCooldown_ShortenedByTwoSeconds()
    {
        EquipHelmet(ItemDatabase.KeenCadenceId);
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.SeismicSlam, Vector3.forward),
            "tank casts its specialty slam");
        float before = skills.CooldownLeft(DiabloSkills.Skill.SeismicSlam);
        Assert.Greater(before, 2f, "slam sits on cooldown");
        Assert.IsTrue(skills.TryKeenReduction(DiabloSkills.Skill.SeismicSlam, true));
        float after = skills.CooldownLeft(DiabloSkills.Skill.SeismicSlam);
        Assert.AreEqual(2f, before - after, 0.05f, "first crit cuts the rest by 2 s");
    }

    [Test]
    public void Nightfang_QuickShot_PiercesOneExtraTarget()
    {
        Assert.AreEqual(0, BuildUniqueEffects.QuickShotPierce(eq), "base arrows never pierce");
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.NightfangId));
        Assert.AreEqual(1, BuildUniqueEffects.QuickShotPierce(eq), "nightfang pierces one extra");
        var rogue = ClassCatalog.Get(ClassCatalog.RogueId);
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward, rogue),
            "rogue specialty cast fires");
        DiabloProjectile shot = null;
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>())
            if (p.kind == DiabloSkills.Skill.QuickShot) shot = p;
        Assert.IsNotNull(shot, "quick shot launched a projectile");
        Assert.AreEqual(1, shot.pierce, "trigger wires the +1 pierce onto the shot");
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.RogueKeepsakeId));
        Assert.AreEqual(0, BuildUniqueEffects.QuickShotPierce(eq), "legacy rogue weapon pierces nothing");
    }
}
