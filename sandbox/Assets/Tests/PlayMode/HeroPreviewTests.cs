using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-companion-rendering: the creation step shows an embedded 3D
/// draft preview (Vorschau) that follows every choice and closes with the modal.
/// </summary>
public class HeroPreviewTests
{
    GameObject hero;
    GameObject selectGo;
    string path;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-preview-{System.Guid.NewGuid():N}.json");
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
        foreach (var rig in GameObject.FindObjectsOfType<HeroPreviewView>())
            Object.DestroyImmediate(rig.gameObject);
        var stray = GameObject.Find(HeroPreviewView.PreviewObjectName);
        if (stray != null) Object.DestroyImmediate(stray);
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
    public IEnumerator OpenCustomize_ShowsPreviewRigThatFollowsTheDraft()
    {
        var head = new GameObject("head");
        head.transform.SetParent(hero.transform, false);
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.OpenCustomize(ClassCatalog.RogueId);
        Assert.IsTrue(ui.CustomizeVisible);
        Assert.IsTrue(ui.PreviewVisible, "Vorschau rig stands while customizing");
        var rig = GameObject.Find(HeroPreviewView.PreviewObjectName);
        Assert.IsNotNull(rig, "the draft renders on its own stage");
        ui.CycleGender();
        Assert.IsTrue(ui.PreviewVisible, "the rig survives draft changes");
        Assert.AreEqual(ui.DraftProfile.gender, rig.GetComponent<ClassAppearance>().AppliedProfile.gender,
            "the preview follows the draft like the hero does");
        ui.CancelCustomize();
        Assert.IsFalse(ui.PreviewVisible, "closing the step removes the rig");
        yield return null;
        Assert.IsNull(GameObject.Find(HeroPreviewView.PreviewObjectName));
    }

    [UnityTest]
    public IEnumerator StartCustomizedHero_HidesThePreview()
    {
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.OpenCustomize(ClassCatalog.FenwalkerId);
        Assert.IsTrue(ui.PreviewVisible);
        ui.SetDraftName("Vorschauheld");
        ui.StartCustomizedHero();
        Assert.IsFalse(ui.PreviewVisible, "the run starts without a leftover stage");
        yield return null;
        Assert.IsNull(GameObject.Find(HeroPreviewView.PreviewObjectName));
    }
}
