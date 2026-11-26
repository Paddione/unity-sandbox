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
        foreach (var name in new[] { HeroPreviewView.PreviewObjectName, HeroPreviewView.CardObjectName })
            for (var stray = GameObject.Find(name); stray != null; stray = GameObject.Find(name))
                Object.DestroyImmediate(stray);
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

    [UnityTest]
    public IEnumerator Preview_KeepsThePortraitAspectAndFramesTheWholeFigure()
    {
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.OpenCustomize(ClassCatalog.LanternbearerId);
        yield return null;
        yield return null;
        var rig = GameObject.Find(HeroPreviewView.PreviewObjectName);
        Assert.IsNotNull(rig);
        var view = Object.FindFirstObjectByType<HeroPreviewView>();
        foreach (var v in Object.FindObjectsByType<HeroPreviewView>(FindObjectsSortMode.None))
            if (v.Rig == rig) view = v;
        var fit = view.Image.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        Assert.IsNotNull(fit, "the render keeps its aspect instead of stretching");
        Assert.AreEqual(HeroPreviewView.TextureAspect, fit.aspectRatio, 0.001f);
        if (!HeroPreviewView.Headless)
        {
            Assert.AreEqual(HeroPreviewView.TextureWidth, view.Image.texture.width);
            Assert.AreEqual(HeroPreviewView.TextureHeight, view.Image.texture.height);
        }
        // Head and feet of the rig project inside the preview camera.
        var cam = view.PreviewCamera;
        Assert.IsNotNull(cam);
        if (HeroPreviewView.TryFigureBounds(rig, out var b))
        {
            var top = cam.WorldToViewportPoint(new Vector3(rig.transform.position.x, b.max.y, rig.transform.position.z));
            var feet = cam.WorldToViewportPoint(new Vector3(rig.transform.position.x, b.min.y, rig.transform.position.z));
            Assert.That(top.y, Is.InRange(0f, 1f), "head in view");
            Assert.That(feet.y, Is.InRange(0f, 1f), "feet in view");
        }
        ui.CancelCustomize();
    }

    [UnityTest]
    public IEnumerator Preview_DropsTheCurrentHerosWornGear()
    {
        var mount = new GameObject(HeroEquipmentAppearance.MountNames[1]);
        mount.transform.SetParent(hero.transform, false);
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        ui.OpenCustomize(ClassCatalog.RogueId);
        yield return null;
        var rig = GameObject.Find(HeroPreviewView.PreviewObjectName);
        Assert.IsNotNull(rig);
        Assert.IsNull(rig.transform.Find(HeroEquipmentAppearance.MountNames[1]), "a new hero starts without the old gear");
        ui.CancelCustomize();
    }

    [UnityTest]
    public IEnumerator ClassCards_ShowFullBodyFiguresWhileOpen()
    {
        var ui = selectGo.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        yield return null;
        foreach (var cls in ClassCatalog.All)
        {
            var fig = ui.CardFigure(cls.id);
            Assert.IsNotNull(fig, $"{cls.id} card has a figure");
            Assert.IsTrue(fig.IsLive, $"{cls.id} figure stands");
            Assert.AreEqual(cls.id, fig.Rig.GetComponent<ClassAppearance>().AppliedClassId, "each card shows its class");
            Assert.AreEqual(HeroPreviewView.CardObjectName, fig.Rig.name);
            Assert.IsFalse(fig.turntable, "card figures stand still");
        }
        ui.OpenCustomize(ClassCatalog.FenwalkerId);
        yield return null;
        Assert.IsFalse(ui.CardFigure(ClassCatalog.FenwalkerId).PreviewCamera.enabled, "hidden cards stop rendering");
        ui.CancelCustomize();
        yield return null;
        Assert.IsTrue(ui.CardFigure(ClassCatalog.FenwalkerId).PreviewCamera.enabled, "back on the cards they render again");
        ui.ChooseNew(ClassCatalog.FenwalkerId);
        Assert.IsFalse(ui.CardFigure(ClassCatalog.FenwalkerId).IsLive, "closing the screen removes the card rigs");
        yield return null;
        Assert.IsNull(GameObject.Find(HeroPreviewView.CardObjectName));
    }
}
