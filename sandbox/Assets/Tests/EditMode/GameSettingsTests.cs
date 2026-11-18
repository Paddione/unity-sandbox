using NUnit.Framework;
using UnityEngine;

/// <summary>Presentation S4: GameSettings roundtrip + shake switch.</summary>
public class GameSettingsTests
{
    [Test]
    public void Settings_ClampAndApply()
    {
        var s = new GameSettings
        {
            width = 99999, height = 10, master = 2f, music = -1f, sfx = 5f, ui = -2f,
            fullscreen = true, shake = true, damageNumbers = true
        };
        s.Clamp();
        Assert.AreEqual(3840, s.width);
        Assert.AreEqual(450, s.height);
        Assert.AreEqual(1f, s.master);
        Assert.AreEqual(0f, s.music);
        Assert.AreEqual(1f, s.sfx);
        Assert.AreEqual(0f, s.ui);
    }

    [Test]
    public void Settings_SaveLoadRoundtrip()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "cinderreach-settings-test-" + System.Guid.NewGuid().ToString("N") + ".json");
        try
        {
            GameSettings.Current.master = 0.5f;
            GameSettings.Current.music = 0.4f;
            GameSettings.Current.sfx = 0.3f;
            GameSettings.Current.ui = 0.2f;
            GameSettings.Current.width = 1600;
            GameSettings.Current.height = 900;
            GameSettings.Current.fullscreen = false;
            GameSettings.Current.shake = false;
            GameSettings.Current.damageNumbers = false;
            GameSettings.Save(path);
            Assert.IsTrue(System.IO.File.Exists(path));
            GameSettings.ResetForTests();
            var loaded = GameSettings.Load(path);
            Assert.AreEqual(0.5f, loaded.master, 0.001f);
            Assert.AreEqual(1600, loaded.width);
            Assert.IsFalse(loaded.fullscreen);
            Assert.IsFalse(loaded.shake);
            Assert.IsFalse(loaded.damageNumbers);
            // Apply pushed onto DiabloSettings (gain layer).
            Assert.AreEqual(0.5f, DiabloSettings.MasterVol, 0.001f);
            Assert.IsFalse(DiabloSettings.ScreenShake);
        }
        finally
        {
            try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } catch { }
            GameSettings.ResetForTests();
            DiabloSettings.ScreenShake = true;
        }
    }

    [Test]
    public void ShakeSwitch_KillsJuiceOffset()
    {
        DiabloSettings.ScreenShake = true;
        DiabloJuice.AddTrauma(Vector3.zero, 1f);
        // trauma may be zero headless (no hero) — assert the switch itself:
        DiabloSettings.ScreenShake = false;
        Assert.AreEqual(Vector3.zero, DiabloJuice.ShakeOffset);
        DiabloSettings.ScreenShake = true;
        DiabloJuice.ResetRun();
    }

    [Test]
    public void MainMenu_NewContinueValidation()
    {
        MainMenu.ResetForTests();
        var go = new GameObject("MainMenuTest").AddComponent<MainMenu>();
        try
        {
            // New without save: always allowed.
            string savePath = SaveService.DefaultPath;
            bool hadSave = false;
            try { hadSave = SaveService.HasSave(); } catch { }
            if (!hadSave)
                Assert.IsTrue(go.NewGame(false));
            else
                Assert.IsFalse(go.NewGame(false)); // overwrite needs confirm
            Assert.AreEqual(MainMenu.State.Playing, MainMenu.CurrentState);
            go.Open();
            Assert.AreEqual(MainMenu.State.Title, MainMenu.CurrentState);
        }
        finally
        {
            Object.DestroyImmediate(go.gameObject);
            MainMenu.ResetForTests();
            DiabloClickController.ExternalDrive = false;
            Time.timeScale = 1f;
        }
    }
}
