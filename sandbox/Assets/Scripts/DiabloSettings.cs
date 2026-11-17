using UnityEngine;

/// <summary>
/// Menu settings store (headless-safe static gain layer — mixer authoring
/// needs editor UI, so volumes route through here per the audio floor).
/// Persisted via PlayerPrefs when available; EditMode tests run without it.
/// </summary>
public static class DiabloSettings
{
    public static float MasterVol = 0.8f;
    public static float MusicVol = 0.8f;
    public static float SFXVol = 0.8f;
    public static int QualityLevel = 2; // 0 Low, 1 Med, 2 High
    public static bool ShowFPS = false;
    public static bool ScreenShake = true;
    public static bool HitStop = true;

    const string Prefix = "DiabloMenu_";

    static DiabloSettings() { Load(); }

    public static void Load()
    {
        try
        {
            if (!Application.isPlaying && Application.isBatchMode) return;
            MasterVol = PlayerPrefs.GetFloat(Prefix + "Master", 0.8f);
            MusicVol = PlayerPrefs.GetFloat(Prefix + "Music", 0.8f);
            SFXVol = PlayerPrefs.GetFloat(Prefix + "SFX", 0.8f);
            QualityLevel = PlayerPrefs.GetInt(Prefix + "Quality", 2);
            ShowFPS = PlayerPrefs.GetInt(Prefix + "FPS", 0) == 1;
            ScreenShake = PlayerPrefs.GetInt(Prefix + "Shake", 1) == 1;
            HitStop = PlayerPrefs.GetInt(Prefix + "HitStop", 1) == 1;
        }
        catch { /* headless PlayerPrefs unavailable — keep defaults */ }
        Clamp();
    }

    public static void Save()
    {
        Clamp();
        try
        {
            PlayerPrefs.SetFloat(Prefix + "Master", MasterVol);
            PlayerPrefs.SetFloat(Prefix + "Music", MusicVol);
            PlayerPrefs.SetFloat(Prefix + "SFX", SFXVol);
            PlayerPrefs.SetInt(Prefix + "Quality", QualityLevel);
            PlayerPrefs.SetInt(Prefix + "FPS", ShowFPS ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Shake", ScreenShake ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "HitStop", HitStop ? 1 : 0);
            PlayerPrefs.Save();
        }
        catch { /* headless — ignore */ }
    }

    public static void Clamp()
    {
        MasterVol = Mathf.Clamp01(MasterVol);
        MusicVol = Mathf.Clamp01(MusicVol);
        SFXVol = Mathf.Clamp01(SFXVol);
        QualityLevel = Mathf.Clamp(QualityLevel, 0, 2);
    }

    public static string QualityName => QualityLevel == 0 ? "Low" : QualityLevel == 1 ? "Med" : "High";

    /// <summary>Effective SFX gain (master × sfx). Sources multiply through this.</summary>
    public static float EffectiveSFX => MasterVol * SFXVol;

    public static void Apply()
    {
        Clamp();
        try { AudioListener.volume = Mathf.Clamp01(MasterVol); } catch { }
        try
        {
            var fps = Object.FindFirstObjectByType<FpsReporter>();
            if (fps != null) fps.enabled = ShowFPS;
        }
        catch { }
    }
}
