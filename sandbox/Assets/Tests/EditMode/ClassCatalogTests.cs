using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>second-class S1: class catalog, Fenwalker regression anchor, save v3 class id.</summary>
public class ClassCatalogTests
{
    string path;

    [SetUp]
    public void Setup()
    {
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-class-{System.Guid.NewGuid():N}.json");
        RunProfile.Reset();
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var f in new[] { path, path + ".tmp", SaveMigrations.BackupPath(path, 2) })
            if (File.Exists(f)) File.Delete(f);
        RunProfile.Reset();
    }

    [Test]
    public void SkillEnum_KeepsNetworkValues()
    {
        Assert.AreEqual(0, (int)DiabloSkills.Skill.Fireball);
        Assert.AreEqual(1, (int)DiabloSkills.Skill.Arrow);
        Assert.AreEqual(2, (int)DiabloSkills.Skill.Cleave);
        Assert.AreEqual(3, (int)DiabloSkills.Skill.LanternBolt);
        Assert.AreEqual(6, (int)DiabloSkills.Skill.VeilStep);
    }

    [Test]
    public void Fenwalker_IsTheExistingHero()
    {
        var f = ClassCatalog.Fenwalker;
        CollectionAssert.AreEqual(
            new[] { DiabloSkills.Skill.Fireball, DiabloSkills.Skill.Arrow, DiabloSkills.Skill.Cleave }, f.skills);
        CollectionAssert.AreEqual(new[] { "Q", "E", "R" }, f.keys);
        Assert.AreEqual(1f, f.focusSkillFactor);
        Assert.AreEqual(1f, f.strengthMeleeFactor);
        Assert.AreEqual("Adventurers/Characters/Knight.fbx", f.modelPath);

        // Regression anchor: today's skill and sheet constants stay as they are.
        var go = new GameObject("AnchorHero");
        var skills = go.AddComponent<DiabloSkills>();
        Assert.AreEqual(3, skills.fireballDamage);
        Assert.AreEqual(2, skills.arrowDamage);
        Assert.AreEqual(4, skills.cleaveDamage);
        Assert.AreEqual(1.0f, skills.fireballCooldown, 0.0001f);
        Assert.AreEqual(0.35f, skills.arrowCooldown, 0.0001f);
        Assert.AreEqual(6f, skills.cleaveCooldown, 0.0001f);
        Object.DestroyImmediate(go);
        Assert.AreEqual(0.20f, CharacterSheet.RankDamageBonus, 0.0001f);
        Assert.AreEqual(0.08f, CharacterSheet.FocusSkillDamagePerPoint, 0.0001f);
        Assert.AreEqual(1, CharacterSheet.StrengthMeleePerPoint);
    }

    [Test]
    public void Lanternbearer_HasFourOwnSkillsOnQERSpace()
    {
        var l = ClassCatalog.Lanternbearer;
        Assert.AreEqual(4, l.skills.Length);
        Assert.AreEqual(4, l.keys.Length);
        foreach (var s in ClassCatalog.Fenwalker.skills)
            Assert.IsFalse(l.Has(s), $"{s} belongs to the Fenwalker");
        Assert.Greater(l.focusSkillFactor, 1f);
        Assert.AreEqual("Adventurers/Characters/Mage.fbx", l.modelPath);
        Assert.IsTrue(File.Exists("Assets/ThirdParty/KayKit/" + l.modelPath));
    }

    [Test]
    public void Get_FallsBackToFenwalker()
    {
        Assert.AreSame(ClassCatalog.Lanternbearer, ClassCatalog.Get("lanternbearer"));
        Assert.AreSame(ClassCatalog.Fenwalker, ClassCatalog.Get(null));
        Assert.AreSame(ClassCatalog.Fenwalker, ClassCatalog.Get(""));
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Unknown class id 'paladin'"));
        Assert.AreSame(ClassCatalog.Fenwalker, ClassCatalog.Get("paladin"));
    }

    [Test]
    public void V2Save_MigratesToFenwalkerWithBackup()
    {
        const string v2 = "{\"v\":2,\"level\":4,\"gold\":50,\"difficultyUnlocked\":true,\"difficulty\":\"Kindled\",\"runSeed\":7,\"stash\":[]}";
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, SaveMigrations.Migrate(v2, out SaveGame save, out int from));
        Assert.AreEqual(2, from);
        Assert.AreEqual(3, save.v);
        Assert.AreEqual(ClassCatalog.FenwalkerId, save.classId);
        Assert.IsTrue(save.difficultyUnlocked, "v2 fields survive");
        Assert.AreEqual(7, save.runSeed);

        File.WriteAllText(path, v2);
        var hero = new GameObject("Player");
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Object.DestroyImmediate(hero);
        Assert.IsTrue(File.Exists(SaveMigrations.BackupPath(path, 2)));
        Assert.AreEqual(ClassCatalog.FenwalkerId, RunProfile.ClassId);
    }

    [Test]
    public void ClassId_SurvivesSaveRoundtrip_UnknownFallsBack()
    {
        var hero = new GameObject("Player");
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        SaveService.SaveNow(hero, path: path);
        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        Assert.AreSame(ClassCatalog.Lanternbearer, RunProfile.Class);

        File.WriteAllText(path, File.ReadAllText(path).Replace("lanternbearer", "paladin"));
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Unknown class id 'paladin'"));
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.AreEqual(ClassCatalog.FenwalkerId, RunProfile.ClassId);
        Object.DestroyImmediate(hero);
    }
}
