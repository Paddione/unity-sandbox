using UnityEngine;

/// <summary>
/// Presentation settings store (S4): resolution, fullscreen, volumes,
/// camera shake + damage numbers. Persisted as JSON in
/// persistentDataPath/cinderreach-settings.json (atomic tmp+rename),
/// applied onto DiabloSettings + Screen + QualitySettings.
/// Headless-safe: PlayerPrefs nie direkt, alles über diese Datei.
/// </summary>
[System.Serializable]
public sealed class GameSettings
{
    public const string FileName = "cinderreach-settings.json";

    public int width = 1920;
    public int height = 1080;
    public bool fullscreen = true;
    public float master = 0.8f;
    public float music = 0.8f;
    public float sfx = 0.8f;
    public float ui = 0.8f;
    public float voice = 0.9f;
    public bool shake = true;
    public bool damageNumbers = true;

    public static string DefaultPath =>
        System.IO.Path.Combine(Application.persistentDataPath, FileName);

    public static GameSettings Current { get; private set; } = new GameSettings();

    /// <summary>Load from path (or defaults when missing/corrupt). Applies onto DiabloSettings.</summary>
    public static GameSettings Load(string path = null)
    {
        string p = path ?? DefaultPath;
        GameSettings s = null;
        try
        {
            if (System.IO.File.Exists(p))
                s = JsonUtility.FromJson<GameSettings>(System.IO.File.ReadAllText(p));
        }
        catch { s = null; }
        if (s == null) s = new GameSettings();
        s.Clamp();
        Current = s;
        s.Apply();
        return s;
    }

    public static void Save(string path = null)
    {
        if (Current == null) Current = new GameSettings();
        Current.Clamp();
        string p = path ?? DefaultPath;
        try
        {
            string json = JsonUtility.ToJson(Current, prettyPrint: true);
            string tmp = p + ".tmp";
            System.IO.File.WriteAllText(tmp, json);
            if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            System.IO.File.Move(tmp, p);
        }
        catch { /* headless — ignore */ }
    }

    public void Clamp()
    {
        width = Mathf.Clamp(width, 800, 3840);
        height = Mathf.Clamp(height, 450, 2160);
        master = Mathf.Clamp01(master);
        music = Mathf.Clamp01(music);
        sfx = Mathf.Clamp01(sfx);
        ui = Mathf.Clamp01(ui);
        voice = Mathf.Clamp01(voice);
    }

    /// <summary>Push onto DiabloSettings + Screen + Quality (headless-safe).</summary>
    public void Apply()
    {
        Clamp();
        DiabloSettings.MasterVol = master;
        DiabloSettings.MusicVol = music;
        DiabloSettings.SFXVol = sfx;
        DiabloSettings.VoiceVol = voice;
        DiabloSettings.ScreenShake = shake;
        DiabloSettings.Apply();
        try { Screen.SetResolution(width, height, fullscreen); } catch { }
        // S2 MusicDirector (Scripts/Audio/MusicDirector.cs) picks up
        // DiabloSettings.MusicVol/SFXVol via its own ApplyVolumes in its Update;
        // no direct reference here so S4 compiles before/after S2 lands.
    }

    /// <summary>Test seam: reset to defaults without touching disk.</summary>
    public static void ResetForTests()
    {
        Current = new GameSettings();
        Current.Apply();
    }
}
