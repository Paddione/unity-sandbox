using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>act-one S1: Act I unlock chain and save v4.</summary>
public class ActProgressTests
{
    string path;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        BossController.ResetRun();
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-act-{System.Guid.NewGuid():N}.json");
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var f in new[] { path, path + ".tmp", SaveMigrations.BackupPath(path, 3) })
            if (File.Exists(f)) File.Delete(f);
        if (SaveService.HasSave()) SaveService.DeleteSave();
        BossController.ResetRun();
        RunProfile.Reset();
    }

    [Test]
    public void Chain_OpensInOrder()
    {
        var act = new ActProgress();
        Assert.IsTrue(act.IsUnlocked(ActProgress.Town));
        Assert.IsTrue(act.IsUnlocked(ActProgress.Ossuary), "Ossuary open from the start, as before");
        Assert.IsFalse(act.IsUnlocked(ActProgress.Kiln));
        Assert.IsFalse(act.CompleteAct(), "no act completion with the Kiln locked");
        Assert.IsFalse(act.Unlock(4), "no zone 4 in Act I");
        Assert.IsTrue(act.Unlock(ActProgress.Kiln));
        Assert.IsFalse(act.Unlock(ActProgress.Kiln), "second unlock is a no-op");
        Assert.IsTrue(act.CompleteAct());
        Assert.IsFalse(act.CompleteAct());
        Assert.IsTrue(act.ActOneComplete);
    }

    [Test]
    public void Waypoints_AreDiscoveredOnce()
    {
        var act = new ActProgress();
        Assert.IsTrue(act.Discover("Town"));
        Assert.IsFalse(act.Discover("Town"));
        Assert.IsFalse(act.Discover(""));
        CollectionAssert.AreEqual(new[] { "Town" }, act.Waypoints);
    }

    [Test]
    public void AbbotKill_UnlocksKilnAndSaves()
    {
        var hero = new GameObject("Player");
        var boss = new GameObject(BossController.BossName).AddComponent<BossController>();
        Assert.IsFalse(RunProfile.Act.IsUnlocked(ActProgress.Kiln));
        boss.OnBossDeath(hero);
        Assert.IsTrue(RunProfile.Act.IsUnlocked(ActProgress.Kiln));
        Assert.IsTrue(SaveService.HasSave());
        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(hero));
        Assert.IsTrue(RunProfile.Act.IsUnlocked(ActProgress.Kiln), "unlock persists");
        Object.DestroyImmediate(boss.gameObject);
        Object.DestroyImmediate(hero);
        foreach (var s in Object.FindObjectsByType<SessionComplete>(FindObjectsSortMode.None))
            Object.DestroyImmediate(s.gameObject);
    }

    [Test]
    public void SaveV4_RoundtripsActState()
    {
        var hero = new GameObject("Player");
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();
        RunProfile.Act.Discover("Kiln");
        SaveService.SaveNow(hero, path: path);
        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.IsTrue(RunProfile.Act.ActOneComplete);
        CollectionAssert.AreEqual(new[] { "Kiln" }, RunProfile.Act.Waypoints);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void V3Migration_OpensKilnOnlyForAbbotSlayers()
    {
        Assert.AreEqual(SaveMigrations.Outcome.Migrated,
            SaveMigrations.Migrate("{\"v\":3,\"level\":5,\"difficultyUnlocked\":false}", out SaveGame fresh, out _));
        Assert.AreEqual(SaveGame.CurrentVersion, fresh.v);
        Assert.AreEqual(ActProgress.Ossuary, fresh.highestUnlockedZone);
        Assert.IsFalse(fresh.actOneComplete);
        Assert.IsNotNull(fresh.waypoints);

        SaveMigrations.Migrate("{\"v\":3,\"level\":8,\"difficultyUnlocked\":true}", out SaveGame slayer, out _);
        Assert.AreEqual(ActProgress.Kiln, slayer.highestUnlockedZone);

        File.WriteAllText(path, "{\"v\":3,\"level\":8,\"difficultyUnlocked\":true,\"classId\":\"lanternbearer\"}");
        var hero = new GameObject("Player");
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.IsTrue(File.Exists(SaveMigrations.BackupPath(path, 3)));
        Assert.IsTrue(RunProfile.Act.IsUnlocked(ActProgress.Kiln));
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void FromSave_ClampsAndNeverCompletesWithoutKiln()
    {
        var a = ActProgress.FromSave(99, true, null);
        Assert.AreEqual(ActProgress.Kiln, a.HighestUnlocked);
        var b = ActProgress.FromSave(0, true, null);
        Assert.AreEqual(ActProgress.Ossuary, b.HighestUnlocked);
        Assert.IsFalse(b.ActOneComplete);
    }
}
