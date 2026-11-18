using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Fair-fights K3 (PlayMode): the Cinder Kiln arrival is safe in every seeded
/// layout. Through the gate and through the unlocked waypoint the hero can
/// stand, orient, and only triggers the first fight by advancing.
/// </summary>
public class KilnArrivalTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        RunProfile.Reset(); // also re-arms the plaza latch
        ZoneGate.ResetRun();
        ZoneTravel.ResetRun();
        EnemyHealth.ResetRun();
        AshLancer.ResetRun();
        ThrallBlast.ResetRun();
        KilnmotherController.ResetRun();
        DiabloProjectile.ResetRun();
        RunProfile.Act.Unlock(ActProgress.Kiln);

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        hero.AddComponent<CharacterController>();
        hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var g in Object.FindObjectsOfType<ZoneGate>())
            Object.DestroyImmediate(g.gameObject);
        foreach (var s in Object.FindObjectsOfType<WaypointShrine>())
            Object.DestroyImmediate(s.gameObject);
        foreach (var b in Object.FindObjectsOfType<ThrallBlast>())
            Object.DestroyImmediate(b.gameObject);
        DiabloTestFixture.CleanupTestScene(hero);
        ZoneGate.ResetRun();
        ZoneTravel.ResetRun();
        EnemyHealth.ResetRun();
        AshLancer.ResetRun();
        ThrallBlast.ResetRun();
        KilnmotherController.ResetRun();
        DiabloProjectile.ResetRun();
        RunProfile.Reset();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static GameObject SpawnThrall(Vector3 pos, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.tag = "Husk";
        go.transform.position = pos;
        go.AddComponent<EnemyHealth>().SetTier(6, LootTier.Normal);
        go.AddComponent<EnemyAttack>().damage = 1;
        go.AddComponent<CinderThrall>();
        return go;
    }

    static GameObject SpawnLancer(Vector3 pos, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.tag = "Husk";
        go.transform.position = pos;
        go.AddComponent<EnemyHealth>().SetTier(10, LootTier.Normal);
        go.AddComponent<AshLancer>();
        return go;
    }

    static GameObject SpawnMother()
    {
        var go = new GameObject(KilnLayout.KilnmotherName);
        go.tag = "Husk";
        go.transform.position = KilnLayout.KilnmotherPos;
        go.AddComponent<EnemyHealth>().SetTier(KilnmotherController.MaxHP, LootTier.ActBoss);
        go.AddComponent<KilnmotherController>();
        return go;
    }

    static List<GameObject> SpawnVariant(int v)
    {
        var all = new List<GameObject>();
        for (int i = 0; i < KilnLayout.ThrallCount; i++)
            all.Add(SpawnThrall(KilnLayout.ThrallAnchor(v, i), $"T_{v}_{i}"));
        for (int i = 0; i < KilnLayout.LancerCount; i++)
            all.Add(SpawnLancer(KilnLayout.LancerAnchor(v, i), $"L_{v}_{i}"));
        for (int i = 0; i < KilnLayout.EliteCount; i++)
            all.Add(SpawnThrall(KilnLayout.EliteAnchor(v, i), $"E_{v}_{i}"));
        all.Add(SpawnMother());
        return all;
    }

    static void Despawn(List<GameObject> gos)
    {
        foreach (var go in gos)
            if (go != null) Object.DestroyImmediate(go.gameObject);
        gos.Clear();
    }

    static Dictionary<GameObject, Vector3> Snapshot(List<GameObject> gos)
    {
        var map = new Dictionary<GameObject, Vector3>();
        foreach (var go in gos) map[go] = go.transform.position;
        return map;
    }

    static float MaxDrift(Dictionary<GameObject, Vector3> before)
    {
        float max = 0f;
        foreach (var kv in before)
        {
            if (kv.Key == null) continue;
            var d = kv.Key.transform.position - kv.Value;
            d.y = 0f;
            max = Mathf.Max(max, d.magnitude);
        }
        return max;
    }

    static float FlatDist(Vector3 a, Vector3 b)
    {
        var d = a - b;
        d.y = 0f;
        return d.magnitude;
    }

    GameObject KilnGate()
    {
        var go = new GameObject(ZoneGate.KilnGateName);
        var zg = go.AddComponent<ZoneGate>();
        zg.destination = KilnLayout.KilnArrival;
        zg.targetZone = ActProgress.Kiln;
        return go;
    }

    GameObject KilnShrine()
    {
        var (id, name, zone, pos) = KilnLayout.Shrines[3];
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "WaypointShrine_Kiln";
        go.transform.position = pos;
        var ws = go.AddComponent<WaypointShrine>();
        ws.shrineId = id;
        ws.displayName = name;
        ws.zone = zone;
        return go;
    }

    [UnityTest]
    public IEnumerator GateArrival_AllVariants_StandSafeThenAdvanceTriggers()
    {
        var gate = KilnGate();
        gate.GetComponent<ZoneGate>().Teleport(hero);
        Assert.AreEqual(KilnLayout.KilnArrival, hero.transform.position);
        Assert.IsFalse(KilnLayout.PlazaBreached, "entry re-arms the plaza");

        for (int v = 0; v < KilnLayout.Variants; v++)
        {
            KilnLayout.ResetPlaza();
            DiabloTestFixture.TeleportHero(hero, KilnLayout.KilnArrival);
            AshLancer.ResetRun();
            KilnmotherController.ResetRun();
            var hp = hero.GetComponent<PlayerHealth>();
            hp.Heal(hp.MaxHP);
            var pack = SpawnVariant(v);
            var before = Snapshot(pack);

            yield return DiabloTestFixture.WaitSeconds(3f);
            Assert.AreEqual(hp.MaxHP, hp.CurrentHP, 0.001f, $"variant {v}: standing is safe");
            Assert.Less(MaxDrift(before), 0.5f, $"variant {v}: packs hold");
            Assert.AreEqual(0, AshLancer.Charges, $"variant {v}: no lancer charges");
            Assert.AreEqual(0, KilnmotherController.WavesFired, $"variant {v}: no mother waves");
            Assert.IsFalse(KilnLayout.PlazaBreached, $"variant {v}: still inside");

            // Advance out of the plaza: the first fight starts.
            DiabloTestFixture.TeleportHero(hero, KilnLayout.KilnArrival + new Vector3(0f, 0f, -8.5f));
            yield return DiabloTestFixture.WaitSeconds(2.5f);
            Assert.IsTrue(KilnLayout.PlazaBreached, $"variant {v}: advance breaches the plaza");
            bool triggered = KilnmotherController.WavesFired > 0
                || AshLancer.Charges > 0
                || MaxDrift(before) > 1f;
            Assert.IsTrue(triggered, $"variant {v}: advance triggers the fight");
            Despawn(pack);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator WaypointArrival_IsSafeAndAdvancingTriggers()
    {
        RunProfile.Act.Discover("Kiln");
        var shrine = KilnShrine();
        var ws = shrine.GetComponent<WaypointShrine>();
        Assert.IsTrue(WaypointShrine.Travel("Kiln", hero), "waypoint travels");
        Assert.AreEqual(ws.Arrival, hero.transform.position);
        Assert.LessOrEqual(FlatDist(hero.transform.position, KilnLayout.WaypointArrival), 0.01f);
        Assert.IsTrue(KilnLayout.PlazaSafe(hero.transform.position), "waypoint lands inside the plaza");
        Assert.IsFalse(KilnLayout.PlazaBreached);

        var pack = SpawnVariant(0);
        var hp = hero.GetComponent<PlayerHealth>();
        yield return DiabloTestFixture.WaitSeconds(2.5f);
        Assert.AreEqual(hp.MaxHP, hp.CurrentHP, 0.001f, "waypoint arrival is safe");
        Assert.AreEqual(0, AshLancer.Charges);
        Assert.AreEqual(0, KilnmotherController.WavesFired);

        DiabloTestFixture.TeleportHero(hero, KilnLayout.WaypointArrival + new Vector3(0f, 0f, -6.5f));
        yield return DiabloTestFixture.WaitSeconds(2.5f);
        Assert.IsTrue(KilnLayout.PlazaBreached, "advancing from the waypoint breaches the plaza");
        bool triggered = KilnmotherController.WavesFired > 0
            || AshLancer.Charges > 0;
        Assert.IsTrue(triggered, "advancing from the waypoint triggers the fight");
        Despawn(pack);
        yield return null;
    }
}
