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
}
