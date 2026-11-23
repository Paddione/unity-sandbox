using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>second-class S4: class select sets the class and look; Continue loads the saved class.</summary>
public class ClassSelectTests
{
    GameObject hero;
    GameObject selectGo;
    string path;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-select-{System.Guid.NewGuid():N}.json");
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>().Equip(ItemDatabase.CreateStarterSword());
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<ClassAppearance>();
        selectGo = new GameObject("ClassSelect");
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(selectGo);
        Object.Destroy(hero);
        DiabloClickController.ExternalDrive = false;
        if (File.Exists(path)) File.Delete(path);
        SaveService.ContinueAfterReload = false;
        SaveService.ContinueAfterReloadPath = null;
        SaveService.NewHeroAfterReload = null;
        SaveService.NewHeroProfile = default;
        ClassSelectUI.PeekPathOverride = null;
        ClassSelectUI.HeroInWorld = false;
        RunProfile.Reset();
    }

    [UnityTest]
    public IEnumerator ChooseNew_SetsClassLookAndUnlocksMovement()
    {
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        Assert.IsTrue(DiabloClickController.ExternalDrive, "movement locked while choosing");
        ui.ChooseNew(ClassCatalog.LanternbearerId);
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        Assert.AreEqual(ClassCatalog.LanternbearerId, hero.GetComponent<ClassAppearance>().AppliedClassId);
        Assert.AreEqual(ItemDatabase.StarterLanternBase, hero.GetComponent<Equipment>().weapon.baseType);
        Assert.IsFalse(ui.IsOpen);
        Assert.IsFalse(DiabloClickController.ExternalDrive);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Continue_LoadsTheSavedClassWithoutAChoice()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        RunProfile.Hero = new HeroProfile
        {
            heroName = "Selma", gender = (int)HeroGender.Female, hairStyle = 1, hairColor = 3, build = (int)HeroBuild.Slim,
        };
        SaveService.SaveNow(hero, path: path);
        RunProfile.Reset();
        Assert.AreEqual(ClassCatalog.FenwalkerId, RunProfile.ClassId);

        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        Assert.IsTrue(ui.Continue(path));
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        Assert.AreEqual(ClassCatalog.LanternbearerId, hero.GetComponent<ClassAppearance>().AppliedClassId);
        Assert.AreEqual("Selma", RunProfile.Hero.heroName, "hero name comes back from the save, not the account");
        Assert.AreEqual("Selma", hero.GetComponent<ClassAppearance>().AppliedProfile.heroName);
        Assert.AreEqual(1, hero.GetComponent<ClassAppearance>().AppliedProfile.hairStyle);
        Assert.IsFalse(ui.IsOpen);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReloadContinue_ResumesTheSavedHeroAfterNewRun()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        hero.GetComponent<Wallet>().SetGold(321);
        SaveService.SaveNow(hero, path: path);
        RunProfile.Reset();
        hero.GetComponent<Wallet>().SetGold(0);

        SaveService.ContinueAfterReload = true;
        SaveService.ContinueAfterReloadPath = path;
        var ui = ClassSelectUI.BeginReloadContinue();
        for (int i = 0; i < 6; i++) yield return null;

        Assert.IsFalse(SaveService.ContinueAfterReload, "the flag is consumed once");
        Assert.AreEqual(321, hero.GetComponent<Wallet>().Gold, "gold comes back from the save");
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId, "class comes back from the save");
        Assert.IsTrue(ui == null, "the helper removes itself");
    }

    [UnityTest]
    public IEnumerator RequestNew_ConfirmsBeforeReplacingASavedHero()
    {
        ClassSelectUI.HeroInWorld = false;
        hero.GetComponent<Wallet>().SetGold(77);
        SaveService.SaveNow(hero, path: path);
        ClassSelectUI.PeekPathOverride = path;

        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.RequestNew(ClassCatalog.RogueId);
        Assert.IsTrue(ui.ConfirmVisible, "a saved hero is at stake: ask first");
        Assert.AreEqual(ClassCatalog.RogueId, ui.PendingNewClass);
        Assert.AreEqual(ClassCatalog.FenwalkerId, RunProfile.ClassId, "nothing changes before the confirm");
        StringAssert.Contains("77 Gold", ClassSelectUI.ReplacedHeroLine());

        ui.CancelNew();
        Assert.IsFalse(ui.ConfirmVisible);
        Assert.IsTrue(ui.IsOpen, "Back returns to the cards");

        ui.RequestNew(ClassCatalog.RogueId);
        ui.ConfirmNew();
        Assert.IsTrue(ui.CustomizeVisible, "confirming opens the creation step before the run starts");
        Assert.AreEqual(ClassCatalog.RogueId, ui.PendingCustomizeClass);
        ui.StartCustomizedHero();
        Assert.AreEqual(ClassCatalog.RogueId, RunProfile.ClassId);
        Assert.IsFalse(ui.IsOpen);
        Assert.IsTrue(ClassSelectUI.HeroInWorld);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RequestNew_WithNothingAtStakeOpensCustomizeAtOnce()
    {
        ClassSelectUI.HeroInWorld = false;
        ClassSelectUI.PeekPathOverride = path; // no file there
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.RequestNew(ClassCatalog.LanternbearerId);
        Assert.IsFalse(ui.ConfirmVisible);
        Assert.IsTrue(ui.CustomizeVisible);
        Assert.AreEqual(ClassCatalog.LanternbearerId, ui.PendingCustomizeClass);
        Assert.IsNotNull(HeroNames.Clean(ui.DraftProfile.heroName), "a random name is prefilled");
        ui.StartCustomizedHero();
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Customize_CyclesPreviewAndStart_ApplyLookAndName()
    {
        var head = new GameObject("head");
        head.transform.SetParent(hero.transform, false);
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.OpenCustomize(ClassCatalog.RogueId);
        Assert.IsTrue(ui.CustomizeVisible);
        string firstName = ui.DraftProfile.heroName;
        Assert.IsNotNull(HeroNames.Clean(firstName));

        int gender0 = ui.DraftProfile.gender;
        ui.CycleGender();
        Assert.AreNotEqual(gender0, ui.DraftProfile.gender);
        Assert.AreEqual(ui.DraftProfile.gender, hero.GetComponent<ClassAppearance>().AppliedProfile.gender, "preview follows the draft");
        ui.CycleHair();
        ui.CycleHairColor();
        ui.CycleBuild();
        var draft = ui.DraftProfile;
        Assert.AreEqual(draft.hairStyle, hero.GetComponent<ClassAppearance>().AppliedProfile.hairStyle);
        if (draft.hairStyle != 4)
            Assert.IsNotNull(hero.GetComponent<ClassAppearance>().HairObject, "hair is built on the head bone");
        ui.SetDraftName("  Wunschname  ");
        ui.RerollDraftName();
        Assert.IsNotNull(HeroNames.Clean(ui.DraftProfile.heroName));
        ui.SetDraftName("Wunschname");
        ui.StartCustomizedHero();

        Assert.IsFalse(ui.CustomizeVisible);
        Assert.IsFalse(ui.IsOpen);
        Assert.AreEqual(ClassCatalog.RogueId, RunProfile.ClassId);
        Assert.AreEqual("Wunschname", RunProfile.Hero.heroName);
        Assert.AreEqual(draft.hairStyle, RunProfile.Hero.hairStyle);
        Assert.AreEqual(draft.build, RunProfile.Hero.build);
        Assert.AreEqual("Wunschname", hero.GetComponent<ClassAppearance>().AppliedProfile.heroName);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Customize_InvalidNameBlocksTheStart()
    {
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.OpenCustomize(ClassCatalog.FenwalkerId);
        ui.SetDraftName("x");
        ui.StartCustomizedHero();
        Assert.IsTrue(ui.CustomizeVisible, "a one-letter name does not start the run");
        Assert.IsTrue(ui.IsOpen);
        ui.SetDraftName("Gueltig");
        ui.StartCustomizedHero();
        Assert.IsFalse(ui.IsOpen);
        Assert.AreEqual("Gueltig", RunProfile.Hero.heroName);
        yield return null;
    }
}
