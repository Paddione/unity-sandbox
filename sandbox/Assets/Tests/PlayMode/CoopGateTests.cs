using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// S3 co-op world (cinderreach-coop-session): the host decides group travel
/// (all living heroes on the gate, or a 3 s host hold), every side teleports
/// its own hero with a spawn offset after a fade, and clients rebuild corpses
/// from the death broadcast. Pure gates are unit-tested here; delivery itself
/// is proven by the 2-process pair run (both sides reach zone 2).
/// </summary>
public class CoopGateTests
{
    GameObject gate;
    ZoneGate zg;

    [SetUp]
    public void Setup()
    {
        ZoneGate.ResetRun();
        DiabloCorpse.ResetRun();
        gate = new GameObject(ZoneGate.FenGateName);
        gate.transform.position = Vector3.zero;
        zg = gate.AddComponent<ZoneGate>();
        zg.destination = new Vector3(200f, 1f, 10f);
        zg.targetZone = 2;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var c in Object.FindObjectsByType<DiabloCorpse>(FindObjectsSortMode.None))
            Object.DestroyImmediate(c.gameObject);
        foreach (var h in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            if (h != null) Object.DestroyImmediate(h.gameObject);
        foreach (var g in Object.FindObjectsByType<ZoneGate>(FindObjectsSortMode.None))
            if (g != null && g.gameObject != gate) Object.DestroyImmediate(g.gameObject);
        if (gate != null) Object.DestroyImmediate(gate);
        ZoneGate.ResetRun();
        DiabloCorpse.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static GameObject Hero(string name, Vector3 at, float hp = 10f)
    {
        var go = new GameObject(name);
        go.transform.position = at;
        var health = go.AddComponent<PlayerHealth>();
        if (hp <= 0f) health.TakeDamage(9999f);
        return go;
    }

    [UnityTest]
    public IEnumerator GroupReady_AllLivingOnGate_Fires()
    {
        Assert.IsTrue(ZoneTravelRelay.GroupReady(allLivingOnGate: true, hostHoldTime: 0f, locked: false));
        Assert.IsTrue(ZoneTravelRelay.GroupReady(allLivingOnGate: true, hostHoldTime: 0f, locked: false),
            "idempotent");
        yield return null;
    }

    [UnityTest]
    public IEnumerator GroupReady_HostHoldPullsStragglers_LockedNeverFires()
    {
        Assert.IsFalse(ZoneTravelRelay.GroupReady(allLivingOnGate: false, hostHoldTime: 0f, locked: false),
            "partial party, no hold: wait");
        Assert.IsFalse(ZoneTravelRelay.GroupReady(allLivingOnGate: false,
            hostHoldTime: ZoneGate.HostHoldSeconds - 0.5f, locked: false), "hold too short");
        Assert.IsTrue(ZoneTravelRelay.GroupReady(allLivingOnGate: false,
            hostHoldTime: ZoneGate.HostHoldSeconds, locked: false), "3 s host hold pulls everyone");
        Assert.IsFalse(ZoneTravelRelay.GroupReady(allLivingOnGate: true,
            hostHoldTime: 99f, locked: true), "locked gates never broadcast");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SpawnOffset_HostStaysPut_FollowersFanOut()
    {
        Assert.AreEqual(Vector3.zero, ZoneTravelRelay.SpawnOffset(0));
        var seen = new System.Collections.Generic.HashSet<Vector3> { Vector3.zero };
        for (int i = 1; i <= 3; i++)
        {
            var off = ZoneTravelRelay.SpawnOffset(i);
            Assert.LessOrEqual(new Vector3(off.x, 0f, off.z).magnitude, 3.5f,
                $"client {i} stays near the gate");
            Assert.IsTrue(seen.Add(off), $"client {i} gets its own spot");
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator RemoteCorpse_RebuildsStrippedVisual()
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "RemoteHusk";
        husk.tag = "Husk";
        var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "RemoteModel";
        model.SetActive(false);
        var health = husk.AddComponent<EnemyHealth>();
        health.corpseModel = model;
        int before = DiabloCorpse.Spawned;

        var corpse = DiabloCorpse.SpawnFrom(husk, remote: true);

        Assert.IsNotNull(corpse, "remote path builds the visual without authority");
        Assert.AreEqual(before + 1, DiabloCorpse.Spawned);
        Assert.AreEqual("Corpse_RemoteHusk", corpse.name);
        Assert.AreEqual("Untagged", corpse.tag);
        Assert.IsNull(corpse.GetComponent<EnemyHealth>(), "no gameplay leftovers");
        Assert.IsNull(corpse.GetComponent<EnemyAttack>(), "no gameplay leftovers");
        Object.DestroyImmediate(model);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Fade_OverlayReachesBlackAndClears()
    {
        yield return ZoneTravelRelay.FadeTo(1f);
        Assert.AreEqual(1f, ZoneTravelRelay.FadeAlpha, 0.01f);
        yield return ZoneTravelRelay.FadeTo(0f);
        Assert.AreEqual(0f, ZoneTravelRelay.FadeAlpha, 0.01f);
    }

    [UnityTest]
    public IEnumerator HostHero_Offline_FallsBackToSolo()
    {
        var hero = Hero("Player", Vector3.zero);
        Assert.AreEqual(hero.transform, ZoneGate.HostHero());
        yield return null;
    }

    [UnityTest]
    public IEnumerator CoopTick_Offline_StaysLocal()
    {
        var hero = Hero("Player", Vector3.zero);
        Assert.IsFalse(ZoneGate.HasClientMirrors(), "offline: no mirrors, solo path owns travel");
        Assert.IsFalse(zg.CoopTick(10f), "no network session: the solo dwell path owns travel");
        Assert.AreEqual(0, ZoneGate.Teleports);
        Assert.AreNotEqual(zg.destination, hero.transform.position);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TeleportViaHost_Offline_FallsBackToLocal()
    {
        var hero = Hero("Player", Vector3.zero);
        Assert.IsFalse(zg.TeleportViaHost(hero), "offline: no broadcast, local travel");
        Assert.AreEqual(zg.destination, hero.transform.position);
        Assert.AreEqual(1, ZoneGate.Teleports);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ApplyTravel_MovesHeroAndCounts()
    {
        var hero = Hero("Player", Vector3.zero);
        zg.ApplyTravel(hero, zg.destination);
        Assert.AreEqual(zg.destination, hero.transform.position);
        Assert.AreEqual(1, ZoneGate.Teleports);
        yield return null;
    }
}
