using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>replayability S1: save v2, v1 → v2 migration, stash, run profile.</summary>
public class SaveMigrationTests
{
    // Shape of a v1 save as written by arpg-session-loop S2–S5 (JsonUtility, prettyPrint).
    const string V1Fixture = @"{
    ""v"": 1,
    ""level"": 6,
    ""xp"": 340,
    ""freeAttributePoints"": 2,
    ""freeSkillPoints"": 1,
    ""strength"": 3,
    ""vitality"": 1,
    ""focus"": 0,
    ""skillRanks"": [
        { ""skill"": ""Fireball"", ""rank"": 2 },
        { ""skill"": ""Cleave"", ""rank"": 1 }
    ],
    ""equipmentArmor"": { ""slot"": 0, ""rarity"": 1, ""baseType"": ""Leather Vest"", ""affixes"": [ { ""key"": ""Armor"", ""value"": 4.0 } ], ""uniqueId"": """" },
    ""equipmentWeapon"": { ""slot"": 1, ""rarity"": 2, ""baseType"": ""Short Sword"", ""affixes"": [], ""uniqueId"": ""Emberbrand Blade"" },
    ""equipmentHelmet"": { ""slot"": 2, ""rarity"": 0, ""baseType"": ""Hood"", ""affixes"": [ { ""key"": ""MaxHP"", ""value"": 5.0 } ], ""uniqueId"": """" },
    ""bag"": [ { ""slot"": 3, ""rarity"": 0, ""baseType"": ""Healing Potion"", ""affixes"": [], ""uniqueId"": """" } ],
    ""gold"": 211,
    ""zoneReached"": 2
}";

    string path;
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-mig-{System.Guid.NewGuid():N}.json");
        RunProfile.Reset();
    }

    [TearDown]
    public void Teardown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        Wallet.ResetRun();
        foreach (var f in new[] { path, path + ".tmp", path + ".corrupt", SaveMigrations.BackupPath(path, 1) })
            if (File.Exists(f)) File.Delete(f);
        RunProfile.Reset();
    }

    static GameObject Hero()
    {
        var go = new GameObject("Player");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        go.AddComponent<Wallet>();
        go.AddComponent<DiabloProgression>();
        return go;
    }

    [Test]
    public void CurrentVersion_IsFour()
    {
        Assert.AreEqual(4, SaveGame.CurrentVersion);
        Assert.AreEqual(4, new SaveGame().v);
    }

    [Test]
    public void V1_MigratesLosslesslyWithV2Defaults()
    {
        var outcome = SaveMigrations.Migrate(V1Fixture, out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(1, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v, "v1 lifts through every step");
        Assert.AreEqual(ClassCatalog.FenwalkerId, save.classId);
        Assert.AreEqual(6, save.level);
        Assert.AreEqual(340, save.xp);
        Assert.AreEqual(2, save.freeAttributePoints);
        Assert.AreEqual(1, save.freeSkillPoints);
        Assert.AreEqual(3, save.strength);
        Assert.AreEqual(2, save.skillRanks.Count);
        Assert.AreEqual("Cleave", save.skillRanks[1].skill);
        Assert.AreEqual("Emberbrand Blade", save.equipmentWeapon.uniqueId);
        Assert.AreEqual(4f, save.equipmentArmor.GetAffix("Armor"), 0.001f);
        Assert.AreEqual(1, save.bag.Count);
        Assert.AreEqual(211, save.gold);
        Assert.AreEqual(2, save.zoneReached);
        Assert.IsFalse(save.difficultyUnlocked);
        Assert.AreEqual(RunProfile.Normal, save.difficulty);
        Assert.GreaterOrEqual(save.runSeed, 0);
        Assert.IsNotNull(save.stash);
        Assert.AreEqual(0, save.stash.Count);
    }

    [Test]
    public void FutureVersion_Refused()
    {
        Assert.AreEqual(SaveMigrations.Outcome.FutureVersion,
            SaveMigrations.Migrate("{\"v\":99,\"level\":9}", out SaveGame save, out int from));
        Assert.IsNull(save);
        Assert.AreEqual(99, from);
    }

    [Test]
    public void Garbage_IsCorrupt()
    {
        Assert.AreEqual(SaveMigrations.Outcome.Corrupt, SaveMigrations.Migrate("", out _, out _));
        Assert.AreEqual(SaveMigrations.Outcome.Corrupt, SaveMigrations.Migrate("{ nope", out _, out _));
        Assert.AreEqual(SaveMigrations.Outcome.Corrupt, SaveMigrations.Migrate("{\"level\":3}", out _, out _));
    }

    [Test]
    public void LoadingV1_KeepsBackupAndAppliesState()
    {
        File.WriteAllText(path, V1Fixture);
        hero = Hero();
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        string bak = SaveMigrations.BackupPath(path, 1);
        Assert.IsTrue(File.Exists(bak), "v1 original must be kept as *.v1.bak.");
        Assert.AreEqual(V1Fixture, File.ReadAllText(bak));
        Assert.AreEqual(211, hero.GetComponent<Wallet>().Gold);
        Assert.AreEqual(2, SaveService.LoadedZoneReached);
        Assert.AreEqual(RunProfile.Normal, RunProfile.Difficulty);

        SaveService.SaveNow(hero, path: path);
        Assert.AreEqual(SaveGame.CurrentVersion, SaveMigrations.ReadVersion(path), "Next write is current.");
        Assert.AreEqual(V1Fixture, File.ReadAllText(bak), "Backup is never overwritten.");
    }

    [Test]
    public void OverwritingV1WithoutLoad_StillBacksUp()
    {
        File.WriteAllText(path, V1Fixture);
        hero = Hero();
        SaveService.WriteSave(SaveService.CaptureState(hero), path);
        Assert.IsTrue(File.Exists(SaveMigrations.BackupPath(path, 1)));
        Assert.AreEqual(SaveGame.CurrentVersion, SaveMigrations.ReadVersion(path));
    }

    [Test]
    public void RunProfile_SurvivesSaveRoundtrip()
    {
        hero = Hero();
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        RunProfile.RunSeed = 12345;
        Assert.IsTrue(RunProfile.Stash.Add(ItemDatabase.CreateUnique("Seer's Circlet")));
        Assert.IsTrue(RunProfile.Stash.Add(ItemDatabase.CreateStarterSword()));
        SaveService.SaveNow(hero, path: path);

        RunProfile.Reset();
        var fresh = Hero();
        Object.DestroyImmediate(hero);
        hero = fresh;
        Assert.IsTrue(SaveService.TryLoad(fresh, path));
        Assert.IsTrue(RunProfile.DifficultyUnlocked);
        Assert.AreEqual(RunProfile.Kindled, RunProfile.Difficulty);
        Assert.AreEqual(12345, RunProfile.RunSeed);
        Assert.AreEqual(2, RunProfile.Stash.Count);
        Assert.AreEqual("Seer's Circlet", RunProfile.Stash.Get(0).uniqueId);
    }

    [Test]
    public void Stash_HoldsFortyAndRejectsMore()
    {
        var stash = new Stash();
        for (int i = 0; i < Stash.Capacity; i++)
            Assert.IsTrue(stash.Add(ItemDatabase.CreateStarterSword()), $"slot {i}");
        Assert.AreEqual(40, stash.Count);
        Assert.AreEqual(0, stash.FreeSlots);
        Assert.IsFalse(stash.Add(ItemDatabase.CreateStarterSword()));
        Assert.IsFalse(stash.Add(null));
        Assert.IsTrue(stash.RemoveAt(0));
        Assert.IsFalse(stash.RemoveAt(99));
        Assert.IsNull(stash.Get(-1));
        Assert.AreEqual(39, Stash.FromList(stash.ToList()).Count);
    }

    [Test]
    public void DeleteSave_ResetsRunProfile()
    {
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        RunProfile.Stash.Add(ItemDatabase.CreateStarterSword());
        SaveService.DeleteSave(path);
        Assert.IsFalse(RunProfile.DifficultyUnlocked);
        Assert.AreEqual(RunProfile.Normal, RunProfile.Difficulty);
        Assert.AreEqual(0, RunProfile.Stash.Count);
    }
}
