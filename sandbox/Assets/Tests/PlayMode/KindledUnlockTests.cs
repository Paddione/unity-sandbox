using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>replayability S4 / cinderreach-team-autoplay K1: the Thewes kill unlocks Kindled (saved), the Abbot no longer does; Kindled scales enemies.</summary>
public class KindledUnlockTests
{
    GameObject player;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        BossController.ResetRun();
        SessionComplete.ResetRun();
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
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
        foreach (var b in GameObject.FindObjectsOfType<BossController>())
            Object.DestroyImmediate(b.gameObject);
        foreach (var s in GameObject.FindObjectsOfType<SessionComplete>())
            if (s.gameObject != player) Object.DestroyImmediate(s.gameObject);
        DiabloTestFixture.CleanupTestScene(player);
        BossController.ResetRun();
        SessionComplete.ResetRun();
        EnemyHealth.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
        RunProfile.Reset();
    }

    [UnityTest]
    public IEnumerator AbbotKill_NoLongerUnlocksKindled()
    {
        var boss = new GameObject(BossController.BossName);
        boss.tag = "Husk";
        boss.AddComponent<BossController>().OnBossDeath(player);
        Assert.IsFalse(RunProfile.DifficultyUnlocked);
        Assert.IsFalse(SessionComplete.KindledUnlockedThisRun);
        StringAssert.DoesNotContain(SessionComplete.KindledLine, SessionComplete.LastSummary);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ThewesKill_UnlocksKindledAndSavesIt()
    {
        var boss = new GameObject(BossController.BossName);
        boss.tag = "Husk";
        var ctrl = boss.AddComponent<BossController>();
        Assert.IsFalse(RunProfile.DifficultyUnlocked);

        StoryDirector.BookKill(StoryIds.Thewes);
        SaveService.SaveNow(player);
        ctrl.OnBossDeath(player);
        Assert.IsTrue(RunProfile.DifficultyUnlocked);
        Assert.IsTrue(SessionComplete.KindledUnlockedThisRun);
        StringAssert.Contains(SessionComplete.KindledLine, SessionComplete.LastSummary);
        Assert.AreEqual(RunProfile.Normal, RunProfile.Difficulty, "unlock does not switch the current run");

        Assert.IsTrue(SaveService.HasSave());
        RunProfile.Reset();
        var fresh = new GameObject("Fresh");
        Assert.IsTrue(SaveService.TryLoad(fresh));
        Object.DestroyImmediate(fresh);
        Assert.IsTrue(RunProfile.DifficultyUnlocked, "unlock persists through the save");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SecondKill_DoesNotRepeatTheUnlockLine()
    {
        RunProfile.DifficultyUnlocked = true;
        var boss = new GameObject(BossController.BossName);
        boss.AddComponent<BossController>().OnBossDeath(player);
        Assert.IsFalse(SessionComplete.KindledUnlockedThisRun);
        StringAssert.DoesNotContain(SessionComplete.KindledLine, SessionComplete.LastSummary);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Kindled_ScalesRuntimeEnemyHealthAndHits()
    {
        var normal = new GameObject("NormalHusk");
        normal.AddComponent<EnemyHealth>().SetTier(5, LootTier.Normal);
        Assert.AreEqual(5, normal.GetComponent<EnemyHealth>().maxHP);

        RunProfile.DifficultyUnlocked = true;
        DifficultyProfile.Select(RunProfile.Kindled);
        var kindled = new GameObject("KindledHusk");
        kindled.AddComponent<EnemyHealth>().SetTier(5, LootTier.Normal);
        Assert.AreEqual(9, kindled.GetComponent<EnemyHealth>().maxHP);
        Assert.AreEqual(2, DifficultyProfile.Current.ScaleDamage(1));

        Object.DestroyImmediate(normal);
        Object.DestroyImmediate(kindled);
        yield return null;
    }
}
