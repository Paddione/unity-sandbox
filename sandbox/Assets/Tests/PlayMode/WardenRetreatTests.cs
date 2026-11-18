using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Fair-fights K1 (PlayMode): Bone Wardens stay in the Sunken Ossuary.
/// Leaving through the Ossuary gate breaks pursuit and the running windup;
/// the warden walks home and holds fire while away.
/// </summary>
public class WardenRetreatTests
{
    GameObject hero;
    GameObject warden;
    RangedCaster caster;
    Vector3 home;

    static readonly Vector3 FenArrival = new Vector3(0f, 1f, -4f);

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        RunProfile.Reset();
        ZoneGate.ResetRun();
        RangedCaster.ResetRun();
        DiabloProjectile.ResetRun();
        EnemyHealth.ResetRun();

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        hero.AddComponent<CharacterController>();
        hero.AddComponent<PlayerHealth>();

        home = KilnLayout.OssuaryOffset + new Vector3(7f, 0.05f, 0f);
        warden = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        warden.name = "Bone Warden";
        warden.tag = "Husk";
        warden.transform.position = home;
        warden.AddComponent<EnemyHealth>().SetTier(12, LootTier.Normal);
        caster = warden.AddComponent<RangedCaster>();
        warden.AddComponent<EnemyAttack>().damage = 1;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var g in Object.FindObjectsOfType<ZoneGate>())
            Object.DestroyImmediate(g.gameObject);
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>())
            Object.DestroyImmediate(p.gameObject);
        DiabloTestFixture.CleanupTestScene(hero);
        if (warden != null) Object.DestroyImmediate(warden);
        ZoneGate.ResetRun();
        RangedCaster.ResetRun();
        DiabloProjectile.ResetRun();
        EnemyHealth.ResetRun();
        RunProfile.Reset();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static float FlatDist(Vector3 a, Vector3 b)
    {
        var d = a - b;
        d.y = 0f;
        return d.magnitude;
    }

    GameObject GateToFen()
    {
        var go = new GameObject(ZoneGate.OssuaryGateName);
        var zg = go.AddComponent<ZoneGate>();
        zg.destination = FenArrival;
        zg.targetZone = ActProgress.Town;
        return go;
    }

    [UnityTest]
    public IEnumerator RetreatThroughGate_CancelsAttackAndWalksHome()
    {
        // Phase 1+2: hero holds 12 m — outside the hold band, inside aggro. The warden
        // closes in and starts its windup exactly at the outer band edge (8 m); entering
        // from above is deterministic, while the shared melee body would zeno-stall a
        // hero parked inside the band.
        DiabloTestFixture.TeleportHero(hero, home + new Vector3(12f, 0f, 0f));
        float t0 = Time.time;
        while (!caster.Telegraphing && Time.time - t0 < 8f) yield return null;
        Assert.IsTrue(caster.Telegraphing, "windup runs while the warden closes in");
        float closing = FlatDist(warden.transform.position, hero.transform.position);
        Assert.Less(closing, 12f, "warden pursued inside the zone");
        Assert.Greater(FlatDist(warden.transform.position, home), 0.5f, "warden left its post");

        // Phase 3: retreat through the gate to the Ashen Fen.
        var gate = GateToFen();
        gate.GetComponent<ZoneGate>().Teleport(hero);
        Assert.AreEqual(FenArrival, hero.transform.position);
        yield return null;
        Assert.IsFalse(caster.Telegraphing, "leaving the zone cancels the windup");
        Assert.AreEqual(0, RangedCaster.ShotsFired, "no shot after the retreat");

        float hp = hero.GetComponent<PlayerHealth>().CurrentHP;
        yield return DiabloTestFixture.WaitSeconds(4f); // 3 m/s over ~2-3 m, with margin
        Assert.IsFalse(caster.Returning, "warden arrived home");
        Assert.LessOrEqual(FlatDist(warden.transform.position, home), 0.5f, "warden is home");
        Assert.AreEqual(0, RangedCaster.ShotsFired);
        Assert.AreEqual(hp, hero.GetComponent<PlayerHealth>().CurrentHP, 0.001f);
    }
}
