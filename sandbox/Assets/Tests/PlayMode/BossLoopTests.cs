using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>S5 loop: the Fen Gate teleports and autosaves zone 2, and the
/// Hollow Abbot's death opens the session-complete overlay.</summary>
public class BossLoopTests
{
    GameObject player;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloProgression.ResetRun();
        ZoneGate.ResetRun();
        BossController.ResetRun();
        SessionComplete.ResetRun();
        RangedCaster.ResetRun();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 11f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloProgression>();
        player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        player.AddComponent<Wallet>();
        player.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var g in GameObject.FindObjectsOfType<ZoneGate>())
            Object.DestroyImmediate(g.gameObject);
        foreach (var b in GameObject.FindObjectsOfType<BossController>())
            Object.DestroyImmediate(b.gameObject);
        foreach (var s in GameObject.FindObjectsOfType<SessionComplete>())
            if (s.gameObject != player) Object.DestroyImmediate(s.gameObject);
        DiabloTestFixture.CleanupTestScene(player);
        ZoneGate.ResetRun();
        BossController.ResetRun();
        SessionComplete.ResetRun();
        RangedCaster.ResetRun();
        EnemyHealth.ResetRun();
        DiabloProgression.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    [UnityTest]
    public IEnumerator Gate_TeleportsAndSavesZoneTwo()
    {
        var gate = new GameObject(ZoneGate.FenGateName);
        gate.transform.position = new Vector3(0f, 1f, -6f);
        var zg = gate.AddComponent<ZoneGate>();
        zg.destination = new Vector3(200f, 1f, 10f);
        zg.targetZone = 2;

        zg.Teleport(player);
        Assert.AreEqual(zg.destination, player.transform.position);
        Assert.AreEqual(1, ZoneGate.Teleports);
        Assert.IsTrue(SaveService.HasSave());

        var fresh = new GameObject("Fresh");
        Assert.IsTrue(SaveService.TryLoad(fresh));
        Assert.AreEqual(2, SaveService.LoadedZoneReached);
        Object.DestroyImmediate(fresh);

        // Return gate brings the hero back to town (zone 1).
        var ret = new GameObject(ZoneGate.OssuaryGateName);
        var zr = ret.AddComponent<ZoneGate>();
        zr.destination = new Vector3(0f, 1.05f, 11f);
        zr.targetZone = 1;
        zr.Teleport(player);
        Assert.AreEqual(2, ZoneGate.Teleports);
        Assert.AreEqual(zr.destination, player.transform.position);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Boss_DeathOpensSessionComplete()
    {
        var boss = new GameObject(BossController.BossName);
        boss.tag = "Husk";
        boss.transform.position = new Vector3(200f, 0.05f, -10f);
        var health = boss.AddComponent<EnemyHealth>();
        health.SetTier(BossController.BossMaxHP, LootTier.Boss);
        var ctrl = boss.AddComponent<BossController>();
        player.GetComponent<Wallet>().Add(40);

        health.TakeDamage(BossController.BossMaxHP); // EnemyHealth owns the death
        yield return null; // deferred destroy runs
        ctrl.OnBossDeath(player); // same-frame poll the scene Update performs

        Assert.AreEqual(1, BossController.BossesKilled);
        Assert.AreEqual(1, SessionComplete.Shown);
        StringAssert.Contains("Kills", SessionComplete.LastSummary);
        StringAssert.Contains("Gold", SessionComplete.LastSummary);
        StringAssert.Contains("40", SessionComplete.LastSummary);
        yield return null;
    }
}
