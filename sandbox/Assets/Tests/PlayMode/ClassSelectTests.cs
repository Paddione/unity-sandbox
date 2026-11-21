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
        SaveService.SaveNow(hero, path: path);
        RunProfile.Reset();
        Assert.AreEqual(ClassCatalog.FenwalkerId, RunProfile.ClassId);

        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        Assert.IsTrue(ui.Continue(path));
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        Assert.AreEqual(ClassCatalog.LanternbearerId, hero.GetComponent<ClassAppearance>().AppliedClassId);
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
        StringAssert.Contains("77 gold", ClassSelectUI.ReplacedHeroLine());

        ui.CancelNew();
        Assert.IsFalse(ui.ConfirmVisible);
        Assert.IsTrue(ui.IsOpen, "Back returns to the cards");

        ui.RequestNew(ClassCatalog.RogueId);
        ui.ConfirmNew();
        Assert.AreEqual(ClassCatalog.RogueId, RunProfile.ClassId);
        Assert.IsFalse(ui.IsOpen);
        Assert.IsTrue(ClassSelectUI.HeroInWorld);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RequestNew_WithNothingAtStakeBeginsAtOnce()
    {
        ClassSelectUI.HeroInWorld = false;
        ClassSelectUI.PeekPathOverride = path; // no file there
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.RequestNew(ClassCatalog.LanternbearerId);
        Assert.IsFalse(ui.ConfirmVisible);
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        yield return null;
    }
}
