using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Vendor click-away: a click outside the trade window dismisses it (and
/// autosaves), a click inside keeps it open. Positions derive from the live
/// panel rect, so no resolution is assumed.
/// </summary>
public class VendorDismissTests
{
    GameObject player;
    DiabloVendorUI ui;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        DiabloClickController.ExternalDrive = false;

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloProgression>();
        player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        player.AddComponent<Wallet>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<DiabloClickController>();
        ui = player.AddComponent<DiabloVendorUI>();

        var eventSys = new GameObject("EventSystem");
        eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var listener = new GameObject("TestAudioListener");
        listener.AddComponent<AudioListener>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        var eventSys = GameObject.Find("EventSystem");
        if (eventSys != null) Object.DestroyImmediate(eventSys);
        var listener = GameObject.Find("TestAudioListener");
        if (listener != null) Object.DestroyImmediate(listener);
        DiabloClickController.ExternalDrive = false;
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static RectTransform PanelRect()
    {
        var panel = GameObject.Find("Player/VendorCanvas/Panel");
        Assert.IsNotNull(panel, "vendor panel built");
        return panel.GetComponent<RectTransform>();
    }

    /// <summary>Screen point of the panel center (overlay canvas: no camera).</summary>
    static Vector2 InsidePoint()
    {
        var corners = new Vector3[4];
        PanelRect().GetWorldCorners(corners);
        var world = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(null, world);
    }

    static Vector2 OutsidePoint() => InsidePoint() + new Vector2(10000f, 10000f);

    [UnityTest]
    public IEnumerator OutsideClick_ClosesAndSaves()
    {
        ui.SetOpen(true);
        yield return null; // layout + past the opening frame
        Canvas.ForceUpdateCanvases();

        Assert.IsTrue(ui.TryCloseOnOutsideClick(OutsidePoint()), "outside click closes");
        Assert.IsFalse(ui.IsOpen);
        Assert.IsFalse(DiabloClickController.ExternalDrive, "movement unlocked");
        Assert.IsTrue(SaveService.HasSave(), "close autosaves");
    }

    [UnityTest]
    public IEnumerator InsideClick_KeepsWindowOpen()
    {
        ui.SetOpen(true);
        yield return null;
        Canvas.ForceUpdateCanvases();

        Assert.IsFalse(ui.TryCloseOnOutsideClick(InsidePoint()), "inside click keeps it open");
        Assert.IsTrue(ui.IsOpen);

        ui.SetOpen(false);
        Assert.IsFalse(ui.IsOpen);
        yield return null;
    }
}
