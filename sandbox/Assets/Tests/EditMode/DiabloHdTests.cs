using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HD regression: every UI canvas that can be built in an EditMode
/// test must scale with the screen against the 1920x1080 reference.
/// Headless EditMode AddComponent can skip Awake-time wiring (see
/// DiabloInventoryUI.EnsureBuilt), so tests drive each canvas through its
/// own healing path: the public SetOpen for inventory, the private build
/// step by reflection where no public headless trigger exists. Juice and
/// Skills build only in play mode (ShowNumber early-returns outside play,
/// UpdateHud runs on Update), so they are covered by the identical scaler
/// stanza, the green build, and the 1920x1080 boot log instead.</summary>
public class DiabloHdTests
{
    static void AssertHd(CanvasScaler scaler, string name)
    {
        Assert.IsNotNull(scaler, name + " needs a CanvasScaler.");
        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, name);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution, name);
        Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            scaler.screenMatchMode, name);
    }

    static void InvokePrivate(object target, string method)
    {
        var mi = target.GetType().GetMethod(method,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(mi, target.GetType().Name + "." + method + " must exist.");
        mi.Invoke(target, null);
    }

    [Test]
    public void Inventory_UsesHdScaler()
    {
        var go = new GameObject("Player");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        AssertHd(go.GetComponentInChildren<CanvasScaler>(true), "Inventory");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Progression_UsesHdScaler()
    {
        var go = new GameObject("Player");
        var prog = go.AddComponent<DiabloProgression>();
        InvokePrivate(prog, "EnsureHud");
        AssertHd(go.GetComponentInChildren<CanvasScaler>(true), "Progression");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void LifeGlobe_UsesHdScaler()
    {
        DiabloLifeGlobe.Ensure();
        var globe = DiabloLifeGlobe.Instance;
        if (globe.GetComponentInChildren<CanvasScaler>(true) == null)
            InvokePrivate(globe, "BuildUI");
        AssertHd(globe.GetComponentInChildren<CanvasScaler>(true), "LifeGlobe");
        Object.DestroyImmediate(globe.gameObject);
    }

    [Test]
    public void TargetBar_UsesHdScaler()
    {
        var go = new GameObject("DTargetBar");
        var bar = go.AddComponent<DiabloTargetBar>();
        if (go.GetComponentInChildren<CanvasScaler>(true) == null)
            InvokePrivate(bar, "BuildUI");
        AssertHd(go.GetComponentInChildren<CanvasScaler>(true), "TargetBar");
        Object.DestroyImmediate(go);
    }
}
