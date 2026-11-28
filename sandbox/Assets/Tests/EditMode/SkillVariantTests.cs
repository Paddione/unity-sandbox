using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// skill-variants S1: catalog defaults/fallbacks, Hold-gated selection,
/// points survive switches, save+reload keeps the pick, old saves default.
/// </summary>
public class SkillVariantTests
{
    static readonly Vector3 HoldPos = new Vector3(0f, 0f, 11f);
    static readonly Vector3 FenPos = new Vector3(-15f, 0f, -4f);

    string path;
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-variant-{System.Guid.NewGuid():N}.json");
        RunProfile.Reset();
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
    }

    [TearDown]
    public void TearDown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        Wallet.ResetRun();
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        foreach (var f in new[] { path + ".corrupt", SaveMigrations.BackupPath(path, 1) })
            if (File.Exists(f)) File.Delete(f);
        HaloZone.ResetRun();
        RunProfile.Reset();
    }

    static GameObject Hero()
    {
        var go = new GameObject("Player");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        go.AddComponent<Wallet>();
        go.AddComponent<DiabloProgression>();
        go.AddComponent<DiabloSkills>();
        go.AddComponent<DiabloCharacterUI>();
        return go;
    }

    [Test]
    public void Catalog_KnownSkillsHaveTwoVariantsWithLegacyDefault()
    {
        var cleave = SkillVariantCatalog.VariantIds(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave);
        Assert.AreEqual(2, cleave.Length);
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            SkillVariantCatalog.DefaultVariantId(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));

        var halo = SkillVariantCatalog.VariantIds(ClassCatalog.LanternbearerId, DiabloSkills.Skill.SearingHalo);
        Assert.AreEqual(2, halo.Length);
        Assert.AreEqual(SkillVariantCatalog.HaloFollowId,
            SkillVariantCatalog.DefaultVariantId(ClassCatalog.LanternbearerId, DiabloSkills.Skill.SearingHalo));

        Assert.IsEmpty(SkillVariantCatalog.VariantIds(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Fireball));
        Assert.IsNull(SkillVariantCatalog.DefaultVariantId(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Fireball));
    }

    [Test]
    public void Resolve_FallsBackForMissingUnknownAndForeignIds()
    {
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
        RunProfile.SkillVariants["fenwalker:Cleave"] = "nope";
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
        // Class-foreign: halo ids mean nothing to the Fenwalker.
        RunProfile.SkillVariants["fenwalker:SearingHalo"] = SkillVariantCatalog.HaloWardId;
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
        Assert.IsNull(RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.SearingHalo));
    }

    [Test]
    public void Selection_HoldAcceptsFenRejects()
    {
        hero = Hero();
        hero.transform.position = HoldPos;
        var ui = hero.GetComponent<DiabloCharacterUI>();
        Assert.IsTrue(ui.CanChangeVariant());
        Assert.IsTrue(ui.TrySelectVariant(DiabloSkills.Skill.Cleave, SkillVariantCatalog.CleaveFrontalId));
        Assert.AreEqual(SkillVariantCatalog.CleaveFrontalId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));

        hero.transform.position = FenPos;
        Assert.IsFalse(ui.CanChangeVariant(), "same act, but outside the camp ring");
        Assert.IsFalse(ui.TrySelectVariant(DiabloSkills.Skill.Cleave, SkillVariantCatalog.CleaveRoundId));
        Assert.AreEqual(SkillVariantCatalog.CleaveFrontalId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
    }

    [Test]
    public void CycleVariant_ButtonPathTogglesInHoldOnly()
    {
        hero = Hero();
        hero.transform.position = HoldPos;
        var ui = hero.GetComponent<DiabloCharacterUI>();
        ui.SetOpen(true);
        bool hasButton = false;
        foreach (var b in hero.GetComponentsInChildren<Button>(true))
            if (b.name == "VariantButton") hasButton = true;
        Assert.IsTrue(hasButton, "Fenwalker Cleave row offers a visible variant switch");

        Assert.IsTrue(ui.CycleVariant(DiabloSkills.Skill.Cleave));
        Assert.AreEqual(SkillVariantCatalog.CleaveFrontalId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
        Assert.IsTrue(ui.CycleVariant(DiabloSkills.Skill.Cleave));
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));

        hero.transform.position = FenPos;
        Assert.IsFalse(ui.CycleVariant(DiabloSkills.Skill.Cleave), "locked outside the Hold");
        Assert.IsFalse(ui.CycleVariant(DiabloSkills.Skill.Fireball), "no variants on Fireball");
        ui.SetOpen(false);
    }

    [Test]
    public void Selection_UnknownIdRejected()
    {
        hero = Hero();
        hero.transform.position = HoldPos;
        var ui = hero.GetComponent<DiabloCharacterUI>();
        Assert.IsFalse(ui.TrySelectVariant(DiabloSkills.Skill.Cleave, "cleave_spin"));
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
    }

    [Test]
    public void Switch_KeepsPointsAndRanks()
    {
        hero = Hero();
        hero.transform.position = HoldPos;
        var prog = hero.GetComponent<DiabloProgression>();
        prog.SetState(3, 0, 0, 1, 2, 1, 0,
            new System.Collections.Generic.List<SkillRankEntry>
                { new SkillRankEntry { skill = "Cleave", rank = 2 } });
        var ui = hero.GetComponent<DiabloCharacterUI>();
        Assert.IsTrue(ui.TrySelectVariant(DiabloSkills.Skill.Cleave, SkillVariantCatalog.CleaveFrontalId));
        var sheet = prog.Sheet;
        Assert.AreEqual(2, sheet.GetRank(DiabloSkills.Skill.Cleave));
        Assert.AreEqual(2, sheet.strength);
        Assert.IsTrue(ui.VariantLines().Contains("Frontbogen"));
        Assert.IsTrue(ui.VariantLines().Contains("90"));
    }

    [Test]
    public void SaveReload_PreservesPick()
    {
        hero = Hero();
        hero.transform.position = HoldPos;
        var ui = hero.GetComponent<DiabloCharacterUI>();
        Assert.IsTrue(ui.TrySelectVariant(DiabloSkills.Skill.Cleave, SkillVariantCatalog.CleaveFrontalId));
        SaveService.SaveNow(hero, path: path);

        RunProfile.Reset();
        var fresh = Hero();
        Object.DestroyImmediate(hero);
        hero = fresh;
        Assert.IsTrue(SaveService.TryLoad(fresh, path));
        Assert.AreEqual(SkillVariantCatalog.CleaveFrontalId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
    }

    [Test]
    public void OldSave_StartsWithLegacyVariant()
    {
        var outcome = SaveMigrations.Migrate(
            "{\"v\":1,\"level\":2,\"xp\":10,\"skillRanks\":[],\"bag\":[],\"gold\":0,\"zoneReached\":1}",
            out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(1, from);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v);
        Assert.IsNotNull(save.skillVariants);
        Assert.AreEqual(0, save.skillVariants.Count);
        RunProfile.SkillVariants.Clear();
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId,
            RunProfile.GetSkillVariant(ClassCatalog.FenwalkerId, DiabloSkills.Skill.Cleave));
    }

    [Test]
    public void TownBoundary_HoldInsideFenOutside()
    {
        Assert.IsTrue(SkillVariantCatalog.IsInLanternHold(HoldPos));
        Assert.IsTrue(SkillVariantCatalog.IsInLanternHold(new Vector3(3.5f, 0f, 11f)));
        Assert.IsFalse(SkillVariantCatalog.IsInLanternHold(FenPos));
        Assert.IsFalse(SkillVariantCatalog.IsInLanternHold(Vector3.zero));
    }

    [Test]
    public void Targeting_RoundHitsAllAround()
    {
        var origin = Vector3.zero;
        Assert.IsTrue(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward, new Vector3(0f, 0f, 2f), 2.6f, 360f));
        Assert.IsTrue(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward, new Vector3(0f, 0f, -2f), 2.6f, 360f));
        Assert.IsFalse(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward, new Vector3(0f, 0f, 3.5f), 2.6f, 360f));
    }

    [Test]
    public void Targeting_FrontalConeRimAndBack()
    {
        var origin = Vector3.zero;
        float reach = 2.6f * SkillVariantCatalog.FrontalRangeFactor;
        Assert.IsTrue(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward, new Vector3(0f, 0f, 3.5f), reach, 90f));
        float a40 = 40f * Mathf.Deg2Rad, a50 = 50f * Mathf.Deg2Rad;
        Assert.IsTrue(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward,
            new Vector3(Mathf.Sin(a40) * 3f, 0f, Mathf.Cos(a40) * 3f), reach, 90f));
        Assert.IsFalse(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward,
            new Vector3(Mathf.Sin(a50) * 3f, 0f, Mathf.Cos(a50) * 3f), reach, 90f));
        Assert.IsFalse(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward, new Vector3(0f, 0f, -2f), reach, 90f));
        Assert.IsFalse(SkillVariantTargeting.InCleaveArea(origin, Vector3.forward, new Vector3(3f, 0f, 0.2f), reach, 90f));
    }
}
