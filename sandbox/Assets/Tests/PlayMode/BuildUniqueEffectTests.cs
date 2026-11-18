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
}
