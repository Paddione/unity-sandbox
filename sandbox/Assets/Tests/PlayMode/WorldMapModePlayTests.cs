using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-map-overlay in play: overlay is the default and leaves the world
/// unblocked, Tab's fullscreen form is modal, the mode survives reopening via
/// DiabloSettings, zoom clamps, and legend toggles drop exactly their icons.
/// </summary>
public sealed class WorldMapModePlayTests
{
    GameObject hero;
    bool savedFullscreen;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        MapScene.ResetCache();
        savedFullscreen = DiabloSettings.MapFullscreen;
        DiabloSettings.MapFullscreen = false;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        DiabloSettings.MapFullscreen = savedFullscreen;
        if (hero != null) Object.Destroy(hero);
        hero = null;
        RunProfile.Reset();
        yield return null;
    }

    GameObject MakeHero(Vector3 at)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = DiabloNetworkPlayer.HeroName;
        go.transform.position = at;
        go.AddComponent<DiabloProgression>();
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        go.AddComponent<Wallet>();
        go.AddComponent<PlayerHealth>();
        go.AddComponent<StoryDirector>();
        return go;
    }

    [UnityTest]
    public IEnumerator OverlayDefault_FullscreenModal_ModePersistsAndZoomClamps()
    {
        hero = MakeHero(new Vector3(0f, 1f, 8f));
        RunProfile.Story.Refresh(RunProfile.Act);
        yield return DiabloTestFixture.WaitSeconds(0.4f);

        var map = hero.GetComponent<WorldMapUI>();
        Assert.IsNotNull(map);
        map.SetOpen(true);
        yield return null;

        Assert.AreEqual(WorldMapMode.Overlay, map.Mode, "overlay is the default (D2)");
        Assert.IsFalse(map.BlocksWorld);
        Assert.IsFalse(DiabloUIFactory.IsAnyModalOpen(hero), "overlay leaves movement/world input open (D3)");
        // Movement is WASD/arrows in DiabloClickController.PollKeyboard, which no modal gates;
        // the modal seam above is what used to freeze it via IsAnyModalOpen.
        Assert.Greater(map.DimAlpha, 0.3f, "overlay dims the world behind the map");
        Assert.Less(map.BackingAlpha, 1f, "overlay backing is translucent");

        map.ToggleMode();
        yield return null;
        Assert.AreEqual(WorldMapMode.Fullscreen, map.Mode, "Tab/ToggleMode switches to fullscreen");
        Assert.IsTrue(DiabloSettings.MapFullscreen, "the choice is written to DiabloSettings");
        Assert.IsTrue(map.BlocksWorld);
        Assert.IsTrue(DiabloUIFactory.IsAnyModalOpen(hero), "fullscreen behaves like the old modal map");
        Assert.AreEqual(0f, map.DimAlpha, 1e-4f, "fullscreen has no dim layer");
        Assert.AreEqual(1f, map.BackingAlpha, 1e-4f, "fullscreen backing is opaque");

        // The remembered mode survives closing and reopening (PlayerPrefs seam).
        map.SetOpen(false);
        yield return null;
        map.SetOpen(true);
        yield return null;
        Assert.AreEqual(WorldMapMode.Fullscreen, map.Mode, "mode is restored from DiabloSettings on open");

        map.ToggleMode();
        Assert.AreEqual(WorldMapMode.Overlay, map.Mode);
        Assert.IsFalse(DiabloSettings.MapFullscreen);

        // Zoom clamps to 75–150 % and the label follows.
        map.SetZoom(4f);
        Assert.AreEqual(WorldMapZoom.MaxZoom, map.Zoom, 1e-4f);
        map.SetZoom(0.1f);
        Assert.AreEqual(WorldMapZoom.MinZoom, map.Zoom, 1e-4f);
        map.SetZoom(1.25f);
        Assert.AreEqual(1.25f, map.Zoom, 1e-4f);
        var picture = hero.transform.Find("WorldMapCanvas/MapPanel/MapFrame/MapPicture");
        Assert.IsNotNull(picture, "map picture exists");
        Assert.AreEqual(WorldMapUI.MapPx * 1.25f, ((RectTransform)picture).sizeDelta.x, 1f, "picture scales with zoom");

        // Legend toggle drops its category and brings it back.
        int before = map.IconsShown;
        map.SetCategory(MapCategory.Elites, false);
        yield return null;
        Assert.AreEqual(MapCategory.All & ~MapCategory.Elites, map.EnabledCategories);
        map.SetCategory(MapCategory.Elites, true);
        yield return null;
        Assert.AreEqual(before, map.IconsShown, "restoring the category restores its markers");

        map.SetOpen(false);
        yield return null;
        Assert.IsFalse(map.IsOpen);
    }

    [UnityTest]
    public IEnumerator Header_ControlsDoNotOverlap()
    {
        hero = MakeHero(new Vector3(0f, 1f, 8f));
        RunProfile.Story.Refresh(RunProfile.Act);
        yield return DiabloTestFixture.WaitSeconds(0.4f);
        var map = hero.GetComponent<WorldMapUI>();
        map.SetOpen(true);
        yield return null;

        // All header controls are anchored to the right edge; each one must end
        // left of the next control's start (review finding: '+' was covered by
        // the mode button before the fix).
        var zoomOut = (RectTransform)hero.transform.Find("WorldMapCanvas/MapPanel/Header/MapZoomOut");
        var label = (RectTransform)hero.transform.Find("WorldMapCanvas/MapPanel/Header/MapZoomLabel");
        var zoomIn = (RectTransform)hero.transform.Find("WorldMapCanvas/MapPanel/Header/MapZoomIn");
        var mode = (RectTransform)hero.transform.Find("WorldMapCanvas/MapPanel/Header/MapModeButton");
        Assert.IsNotNull(zoomOut, "zoom − button exists");
        Assert.IsNotNull(label, "zoom label exists");
        Assert.IsNotNull(zoomIn, "zoom + button exists");
        Assert.IsNotNull(mode, "mode button exists");
        Assert.LessOrEqual(zoomOut.anchoredPosition.x, label.anchoredPosition.x - label.sizeDelta.x,
            "zoom − clears the zoom label");
        Assert.LessOrEqual(label.anchoredPosition.x, zoomIn.anchoredPosition.x - zoomIn.sizeDelta.x,
            "zoom label clears the + button");
        Assert.LessOrEqual(zoomIn.anchoredPosition.x, mode.anchoredPosition.x - mode.sizeDelta.x,
            "+ clears the mode button");
        map.SetOpen(false);
    }

    [UnityTest]
    public IEnumerator QuestColumn_StandsOnTheLeft()
    {
        hero = MakeHero(new Vector3(0f, 1f, 8f));
        RunProfile.Story.Refresh(RunProfile.Act);
        yield return DiabloTestFixture.WaitSeconds(0.4f);
        var map = hero.GetComponent<WorldMapUI>();
        map.SetOpen(true);
        yield return null;

        var quest = hero.transform.Find("WorldMapCanvas/MapPanel/MapQuestPanel");
        var frame = hero.transform.Find("WorldMapCanvas/MapPanel/MapFrame");
        Assert.IsNotNull(quest, "tracked-quest column exists");
        Assert.IsNotNull(frame, "map frame exists");
        Assert.Less(((RectTransform)quest).anchoredPosition.x, ((RectTransform)frame).anchoredPosition.x,
            "tracked quests stand left of the map");
        Assert.IsNotNull(hero.transform.Find("WorldMapCanvas/MapPanel/MapSide/MapFilter_Quests"), "quest filter toggle exists");
        Assert.IsNotNull(hero.transform.Find("WorldMapCanvas/MapPanel/MapSide/MapFilter_Elites"), "elite filter toggle exists");
        map.SetOpen(false);
    }
}
