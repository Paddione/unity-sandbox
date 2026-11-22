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

    [Test]
    public void V9Migration_IdentifiesItemsWithoutRerolling()
    {
        const string json = "{\"v\":9,\"bag\":[{\"slot\":1,\"baseType\":\"Dagger\",\"affixes\":[{\"key\":\"Damage\",\"value\":7}]}]}";
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, SaveMigrations.Migrate(json, out var save, out _));
        Assert.AreEqual(7f, save.bag[0].GetAffix("Damage"));
        Assert.AreEqual(0, save.bag[0].itemLevel);
        Assert.That(save.bag[0].instanceId, Does.Match("^[0-9a-f]{32}$"));
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, SaveMigrations.Migrate(json, out var repeated, out _));
        Assert.AreEqual(save.bag[0].instanceId, repeated.bag[0].instanceId);

        save.bag[0].itemLevel = 7;
        var currentJson = JsonUtility.ToJson(save);
        Assert.AreEqual(SaveMigrations.Outcome.Current, SaveMigrations.Migrate(currentJson, out var reloaded, out _));
        Assert.AreEqual(save.bag[0].instanceId, reloaded.bag[0].instanceId);
        Assert.AreEqual(7, reloaded.bag[0].itemLevel);
        Assert.AreEqual(7f, reloaded.bag[0].GetAffix("Damage"));
    }

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
    public void CurrentVersion_IsEleven()
    {
        Assert.AreEqual(11, SaveGame.CurrentVersion);
        Assert.AreEqual(11, new SaveGame().v);
    }

    [Test]
    public void V10_OpensBarrensOnlyForKilnmotherSlayers()
    {
        const string slayerJson = @"{""v"":10,""level"":12,""actOneComplete"":true,""highestUnlockedZone"":3}";
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, SaveMigrations.Migrate(slayerJson, out SaveGame slayer, out int from));
        Assert.AreEqual(10, from);
        Assert.AreEqual(11, slayer.v);
        Assert.AreEqual(ActProgress.Barrens, slayer.highestUnlockedZone);
        Assert.IsFalse(slayer.actTwoBossDefeated);

        const string midAct = @"{""v"":10,""level"":7,""actOneComplete"":false,""highestUnlockedZone"":3}";
        SaveMigrations.Migrate(midAct, out SaveGame mid, out _);
        Assert.AreEqual(ActProgress.Kiln, mid.highestUnlockedZone, "Kiln stays the frontier until the Kilnmother falls");
        Assert.IsFalse(mid.actTwoBossDefeated);
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
    public void V4_MigratesWithEmptyContractState()
    {
        const string v4 = @"{""v"":4,""level"":6,""xp"":340,""gold"":211,""zoneReached"":2}";
        var outcome = SaveMigrations.Migrate(v4, out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(4, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v);
        Assert.IsNotNull(save.contract);
        Assert.IsFalse(save.contract.hasContract);
        Assert.AreEqual(6, save.level);
        Assert.AreEqual(211, save.gold);
    }

    [Test]
    public void ActiveContract_KeepsPositionThroughSaveReload()
    {
        hero = Hero();
        RunProfile.RunSeed = 4242;
        var offers = ContractCatalog.Offers(RunProfile.RunSeed, RunProfile.Act);
        Assert.IsTrue(ContractProgress.TryAccept(offers[0]));
        SaveService.SaveNow(hero, path: path);

        RunProfile.Reset();
        ContractProgress.Reset();
        var fresh = Hero();
        Object.DestroyImmediate(hero);
        hero = fresh;
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.IsTrue(ContractProgress.HasContract);
        Assert.AreEqual(offers[0].id, ContractProgress.ActiveOffer.id);
        Assert.AreEqual(offers[0].position, ContractProgress.ActiveOffer.position);
        Assert.AreEqual(ContractStatus.Active, ContractProgress.Status);
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
    public void V4_MigratesToV7WithVariantDefaults()
    {
        var outcome = SaveMigrations.Migrate(
            "{\"v\":4,\"level\":3,\"xp\":5,\"skillRanks\":[],\"bag\":[],\"gold\":0,\"zoneReached\":1," +
            "\"classId\":\"fenwalker\",\"highestUnlockedZone\":2,\"actOneComplete\":false,\"waypoints\":[]}",
            out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(4, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v, "v4 lifts through every step");
        Assert.IsNotNull(save.skillVariants);
        Assert.AreEqual(0, save.skillVariants.Count);
    }

    [Test]
    public void SiblingV5OrV6_MigratesToV7KeepingFields()
    {
        var outcome = SaveMigrations.Migrate(
            "{\"v\":5,\"level\":3,\"xp\":5,\"skillRanks\":[],\"bag\":[],\"gold\":9,\"zoneReached\":1}",
            out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(5, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v, "v5 lifts through every step");
        Assert.AreEqual(9, save.gold);
        Assert.IsNotNull(save.skillVariants);
    }

    [Test]
    public void VariantPick_SurvivesSaveRoundtrip()
    {
        hero = Hero();
        RunProfile.SkillVariants["fenwalker:Cleave"] = SkillVariantCatalog.CleaveFrontalId;
        SaveService.SaveNow(hero, path: path);
        Assert.AreEqual(SaveGame.CurrentVersion, SaveMigrations.ReadVersion(path));

        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.AreEqual(SkillVariantCatalog.CleaveFrontalId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));

        // Unknown stored ids never load into the profile.
        RunProfile.Reset();
        var save = SaveService.CaptureState(hero);
        save.skillVariants.Add(new SkillVariantEntry { classId = "fenwalker", skill = "Cleave", variant = "bogus" });
        SaveService.WriteSave(save, path);
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
    }

    [Test]
    public void V4_MigratesToV9WithClaimDefaults()
    {
        const string v4 = @"{""v"":4,""level"":9,""actOneComplete"":true,""highestUnlockedZone"":3}";
        var outcome = SaveMigrations.Migrate(v4, out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(4, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v, "v4 lifts through every step");
        Assert.IsFalse(save.kindledRewardPending, "no retroactive claim");
        Assert.IsFalse(save.kindledRewardGranted);
        Assert.AreEqual("", save.kindledRewardClass);
        Assert.IsTrue(save.actOneComplete);
        Assert.AreEqual(ActProgress.Barrens, save.highestUnlockedZone, "v11: Kilnmother slayers find the Barrens open");
    }

    [Test]
    public void V9Claim_SurvivesSaveRoundtrip()
    {
        hero = Hero();
        RunProfile.KindledRewardPending = true;
        RunProfile.KindledRewardClass = ClassCatalog.LanternbearerId;
        SaveService.SaveNow(hero, path: path);
        Assert.AreEqual(SaveGame.CurrentVersion, SaveMigrations.ReadVersion(path));
        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.IsTrue(RunProfile.KindledRewardPending);
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.KindledRewardClass);
        Assert.IsFalse(RunProfile.KindledRewardGranted);
    }

    [Test]
    public void DeleteSave_ResetsRunProfile()
    {
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        RunProfile.Stash.Add(ItemDatabase.CreateStarterSword());
        RunProfile.SkillVariants["fenwalker:Cleave"] = SkillVariantCatalog.CleaveFrontalId;
        SaveService.DeleteSave(path);
        Assert.IsFalse(RunProfile.DifficultyUnlocked);
        Assert.AreEqual(RunProfile.Normal, RunProfile.Difficulty);
        Assert.AreEqual(0, RunProfile.Stash.Count);
        Assert.AreEqual(0, RunProfile.SkillVariants.Count);
    }

    // smithing S1: v6 (Material + Tier-Defaults) --------------------------

    // v4 ohne smithing-Felder: Material fehlt (= 0), Einträge ohne Tier (= 0).
    const string V4Fixture = @"{
    ""v"": 4,
    ""level"": 9,
    ""xp"": 120,
    ""equipmentWeapon"": { ""slot"": 1, ""rarity"": 0, ""baseType"": ""ShortSword"", ""affixes"": [ { ""key"": ""Damage"", ""value"": 2.0 } ], ""uniqueId"": """", ""affixTier"": 0, ""reforgeIndexPlusOne"": 0 },
    ""equipmentArmor"": null,
    ""equipmentHelmet"": null,
    ""bag"": [ { ""slot"": 0, ""rarity"": 1, ""baseType"": ""LeatherVest"", ""affixes"": [ { ""key"": ""Armor"", ""value"": 3.0 } ], ""uniqueId"": """", ""affixTier"": 0, ""reforgeIndexPlusOne"": 0 } ],
    ""gold"": 77,
    ""zoneReached"": 2,
    ""difficulty"": ""Normal"",
    ""stash"": []
}";

    [Test]
    public void V4_MigratesToV6WithTierOneAndZeroMaterial()
    {
        var outcome = SaveMigrations.Migrate(V4Fixture, out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(4, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v, "v4 lifts through every step");
        Assert.AreEqual(0, save.smithingMaterial);
        Assert.AreEqual(1, save.equipmentWeapon.affixTier);
        Assert.AreEqual(1, save.bag[0].affixTier);
        Assert.AreEqual(0, save.bag[0].reforgeIndexPlusOne);
        Assert.AreEqual(2f, save.equipmentWeapon.GetAffix("Damage"), 0.001f);
        Assert.AreEqual(3f, save.bag[0].GetAffix("Armor"), 0.001f);
        Assert.AreEqual(77, save.gold);
    }

    [Test]
    public void V5_UnknownFieldsMigrateToV6WithDefaults()
    {
        // v5 gehört den Zone-Contracts: unbekanntes Feld + fehlende v6-Felder.
        string v5 = V4Fixture.Replace(@"""v"": 4", @"""v"": 5, ""zoneContract"": ""seal-the-fen-gate""");
        var outcome = SaveMigrations.Migrate(v5, out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(5, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v, "v5 lifts through every step");
        Assert.AreEqual(0, save.smithingMaterial);
        Assert.AreEqual(1, save.bag[0].affixTier);
        Assert.AreEqual(9, save.level);
    }

    [Test]
    public void Material_SurvivesSaveRoundtrip()
    {
        hero = Hero();
        RunProfile.SmithingMaterial = 7;
        var sword = ItemDatabase.CreateStarterSword();
        sword.affixTier = 2;
        sword.reforgeIndexPlusOne = 1;
        Assert.IsTrue(hero.GetComponent<Inventory>().Add(sword));
        SaveService.SaveNow(hero, path: path);
        Assert.AreEqual(SaveGame.CurrentVersion, SaveMigrations.ReadVersion(path));

        RunProfile.Reset();
        var fresh = Hero();
        Object.DestroyImmediate(hero);
        hero = fresh;
        Assert.IsTrue(SaveService.TryLoad(fresh, path));
        Assert.AreEqual(7, RunProfile.SmithingMaterial);
        var loaded = fresh.GetComponent<Inventory>().Get(0);
        Assert.AreEqual(2, loaded.affixTier);
        Assert.AreEqual(1, loaded.reforgeIndexPlusOne);
    }
}
