using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>HD regression at runtime: every Diablo canvas must scale with the
/// screen against the 1920x1080 reference. Unlike the EditMode HD tests,
/// this covers the two play-mode-only canvases too (Juice builds on
/// ShowText, Skills builds on Update), so all six share one proof.</summary>
public class DiabloHdPlayTests
{
    GameObject floor;
    GameObject camGo;
    GameObject player;

    [SetUp]
    public void Setup()
    {
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TestFloor";
        camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        if (player != null) Object.DestroyImmediate(player);
        if (camGo != null) Object.DestroyImmediate(camGo);
        if (floor != null) Object.DestroyImmediate(floor);
        DiabloJuice.ResetRun();
    }

    static void AssertHd(string canvasObjectName, string label)
    {
        var go = GameObject.Find(canvasObjectName);
        Assert.IsNotNull(go, label + " canvas '" + canvasObjectName + "' must exist.");
        var scaler = go.GetComponent<CanvasScaler>();
        Assert.IsNotNull(scaler, label + " needs a CanvasScaler.");
        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, label);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution, label);
        Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            scaler.screenMatchMode, label);
    }

    [UnityTest]
    public IEnumerator AllCanvases_UseHdScaler()
    {
        var ui = player.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        player.AddComponent<DiabloProgression>().AddXp(1);
        DiabloLifeGlobe.Ensure();
        player.AddComponent<DiabloTargetBar>();
        DiabloJuice.ShowText(Vector3.zero, "t", Color.white);
        player.AddComponent<DiabloClickController>();
        ui.SetOpen(false);
        for (int i = 0; i < 5; i++) yield return null;

        AssertHd("InventoryCanvas", "Inventory");
        AssertHd("XpCanvas", "Progression");
        AssertHd("LifeGlobeCanvas", "LifeGlobe");
        AssertHd("TargetBarCanvas", "TargetBar");
        AssertHd("DiabloJuice", "Juice");
        AssertHd("SkillBarCanvas", "Skills");
    }
}
