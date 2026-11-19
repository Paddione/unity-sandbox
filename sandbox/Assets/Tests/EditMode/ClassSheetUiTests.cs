using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>second-class S3: class-aware ranks, focus factor and character panel rows.</summary>
public class ClassSheetUiTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        hero = new GameObject("SheetHero");
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(hero);
        if (SaveService.HasSave()) SaveService.DeleteSave();
        RunProfile.Reset();
    }

    [Test]
    public void Ranks_SaveAllSevenAndKeepTheOtherClass()
    {
        var sheet = new CharacterSheet { fireballRank = 2, cleaveRank = 1, lanternBoltRank = 3, wispsRank = 4, haloRank = 1, veilRank = 2, slamRank = 3, quickRank = 1, flurryRank = 2, mendRank = 4, renewRank = 5 };
        var entries = sheet.ToEntries();
        Assert.AreEqual(12, entries.Count);
        var back = new CharacterSheet();
        back.FromEntries(entries);
        Assert.AreEqual(2, back.fireballRank);
        Assert.AreEqual(1, back.cleaveRank);
        Assert.AreEqual(3, back.GetRank(DiabloSkills.Skill.LanternBolt));
        Assert.AreEqual(4, back.GetRank(DiabloSkills.Skill.Wisps));
        Assert.AreEqual(1, back.GetRank(DiabloSkills.Skill.SearingHalo));
        Assert.AreEqual(2, back.GetRank(DiabloSkills.Skill.VeilStep));
        Assert.AreEqual(3, back.GetRank(DiabloSkills.Skill.SeismicSlam));
        Assert.AreEqual(1, back.GetRank(DiabloSkills.Skill.QuickShot));
        Assert.AreEqual(2, back.GetRank(DiabloSkills.Skill.BladeFlurry));
        Assert.AreEqual(4, back.GetRank(DiabloSkills.Skill.MendingLight));
        Assert.AreEqual(5, back.GetRank(DiabloSkills.Skill.RenewingCircle));
    }

    [Test]
    public void SpendSkill_RaisesEachLanternRankUpToFive()
    {
        var sheet = new CharacterSheet { freeSkillPoints = 10 };
        foreach (var s in ClassCatalog.Lanternbearer.skills) Assert.IsTrue(sheet.SpendSkill(s), s.ToString());
        Assert.AreEqual(4, sheet.freeSkillPoints);
        for (int i = 1; i < CharacterSheet.MaxSkillRank; i++) sheet.SpendSkill(DiabloSkills.Skill.LanternBolt);
        Assert.AreEqual(CharacterSheet.MaxSkillRank, sheet.lanternBoltRank);
        sheet.freeSkillPoints = 1;
        Assert.IsFalse(sheet.SpendSkill(DiabloSkills.Skill.LanternBolt), "capped at 5");
    }

    [Test]
    public void FocusFactor_OnlyScalesTheFocusBonus()
    {
        var sheet = new CharacterSheet { focus = 5, lanternBoltRank = 1 };
        Assert.AreEqual(1.4f, sheet.FocusSkillMultiplier(), 0.0001f);
        Assert.AreEqual(1.4f, sheet.FocusSkillMultiplier(1f), 0.0001f, "Fenwalker unchanged");
        Assert.AreEqual(1.6f, sheet.FocusSkillMultiplier(1.5f), 0.0001f);
        Assert.AreEqual(1.2f * 1.6f, sheet.SkillMultiplier(DiabloSkills.Skill.LanternBolt, 0, 1.5f), 0.0001f);
    }

    [Test]
    public void LanternRanks_SurviveTheSaveRoundtrip()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"cinderreach-sheet-{System.Guid.NewGuid():N}.json");
        try
        {
            RunProfile.ClassId = ClassCatalog.LanternbearerId;
            var prog = hero.GetComponent<DiabloProgression>();
            prog.Sheet.wispsRank = 3;
            prog.Sheet.haloRank = 2;
            SaveService.SaveNow(hero, path: path);
            var fresh = new GameObject("Fresh");
            fresh.AddComponent<DiabloProgression>();
            Assert.IsTrue(SaveService.TryLoad(fresh, path));
            var sheet = fresh.GetComponent<DiabloProgression>().Sheet;
            Assert.AreEqual(3, sheet.wispsRank);
            Assert.AreEqual(2, sheet.haloRank);
            Object.DestroyImmediate(fresh);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Test]
    public void CharacterPanel_ShowsTheClassSkillsWithHdScaler()
    {
        var ui = hero.AddComponent<DiabloCharacterUI>();
        ui.Refresh();
        CollectionAssert.AreEqual(ClassCatalog.Fenwalker.skills, ui.RowSkills);

        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        ui.Refresh();
        CollectionAssert.AreEqual(ClassCatalog.Lanternbearer.skills, ui.RowSkills);
        Assert.AreEqual(6, ui.RowSkills.Length);

        var scaler = hero.GetComponentInChildren<CanvasScaler>();
        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        var texts = hero.GetComponentsInChildren<Text>();
        bool sawWisps = false;
        foreach (var t in texts) if (t.text.StartsWith("Wisps")) sawWisps = true;
        Assert.IsTrue(sawWisps, "Wisps row rendered");
    }

    [Test]
    public void CharacterPanel_SpendSkillButtonsRankTheClassSkill()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        var prog = hero.GetComponent<DiabloProgression>();
        prog.Sheet.freeSkillPoints = 1;
        var ui = hero.AddComponent<DiabloCharacterUI>();
        ui.SpendSkill(DiabloSkills.Skill.SearingHalo);
        Assert.AreEqual(1, prog.Sheet.haloRank);
        Assert.AreEqual(0, prog.Sheet.freeSkillPoints);
    }
}
