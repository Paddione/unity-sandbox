using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>act-one S4: Kilnmother phase two, death completes Act I and persists.</summary>
public class KilnmotherLoopTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        KilnmotherController.ResetRun();
        SessionComplete.ResetRun();
        EnemyHealth.ResetRun();
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.transform.position = new Vector3(0f, 1f, 40f); // out of aggro
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var k in Object.FindObjectsOfType<KilnmotherController>()) Object.DestroyImmediate(k.gameObject);
        foreach (var s in Object.FindObjectsOfType<SessionComplete>()) if (s.gameObject != hero) Object.DestroyImmediate(s.gameObject);
        DiabloTestFixture.CleanupTestScene(hero);
        foreach (var b in Object.FindObjectsOfType<ThrallBlast>()) Object.DestroyImmediate(b.gameObject);
        if (SaveService.HasSave()) SaveService.DeleteSave();
        KilnmotherController.ResetRun();
        SessionComplete.ResetRun();
        RunProfile.Reset();
    }

    GameObject Mother()
    {
        var go = new GameObject(KilnLayout.KilnmotherName);
        go.tag = "Husk";
        go.AddComponent<EnemyHealth>().SetTier(KilnmotherController.MaxHP, LootTier.ActBoss);
        go.AddComponent<KilnmotherController>();
        return go;
    }

    [UnityTest]
    public IEnumerator PhaseTwo_AtHalfHp_SummonsFourOnce()
    {
        var go = Mother();
        var health = go.GetComponent<EnemyHealth>();
        health.TakeDamage(KilnmotherController.MaxHP / 2);
        yield return null;
        var ctrl = go.GetComponent<KilnmotherController>();
        Assert.IsTrue(ctrl.PhaseTwo);
        ctrl.EnterPhaseTwo();
        Assert.AreEqual(1, KilnmotherController.PhaseTwoEntries, "summons once");
        int summons = 0;
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
            if (h.name.StartsWith("Kiln Summon_")) summons++;
        Assert.AreEqual(KilnmotherController.SummonCount, summons);
    }

    [UnityTest]
    public IEnumerator Death_CompletesActOneSavesAndShowsTheActScreen()
    {
        RunProfile.Act.Unlock(ActProgress.Kiln);
        var go = Mother();
        go.GetComponent<EnemyHealth>().TakeDamage(KilnmotherController.MaxHP);
        yield return null;
        Assert.AreEqual(1, KilnmotherController.Kills);
        Assert.IsTrue(RunProfile.Act.ActOneComplete);
        Assert.IsTrue(SessionComplete.ActOneMode);
        Assert.AreEqual(1, SessionComplete.Shown);
        Assert.IsTrue(SaveService.HasSave());
        RunProfile.Reset();
        var fresh = new GameObject("Fresh");
        Assert.IsTrue(SaveService.TryLoad(fresh));
        Object.DestroyImmediate(fresh);
        Assert.IsTrue(RunProfile.Act.ActOneComplete, "act completion persists");
    }
}
