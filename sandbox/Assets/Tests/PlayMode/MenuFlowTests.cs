using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Presentation S4: menu flow New -> play -> pause -> menu (headless).</summary>
public class MenuFlowTests
{
    [UnityTest]
    public IEnumerator MenuFlow_NewPlayPauseMenu()
    {
        MainMenu.ResetForTests();
        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        var pause = new GameObject("Pause").AddComponent<PauseMenu>();
        yield return null;
        menu.Open();
        Assert.AreEqual(MainMenu.State.Title, MainMenu.CurrentState);
        Assert.IsTrue(DiabloClickController.ExternalDrive);
        menu.OpenSettings();
        Assert.AreEqual(MainMenu.State.Settings, MainMenu.CurrentState);
        menu.CloseSettings();
        Assert.AreEqual(MainMenu.State.Title, MainMenu.CurrentState);
        // New game (delete save first when present is covered by EditMode validation).
        DiabloClickController.ExternalDrive = false;
        MainMenu.CurrentState = MainMenu.State.Playing;
        pause.SetOpen(true);
        Assert.IsTrue(pause.IsOpen);
        Assert.AreEqual(0f, Time.timeScale);
        pause.Resume();
        Assert.IsFalse(pause.IsOpen);
        Assert.AreEqual(1f, Time.timeScale);
        pause.SetOpen(true);
        pause.BackToMenu();
        Assert.AreEqual(MainMenu.State.Title, MainMenu.CurrentState);
        Assert.AreEqual(1f, Time.timeScale);
        yield return null;
    }
}
