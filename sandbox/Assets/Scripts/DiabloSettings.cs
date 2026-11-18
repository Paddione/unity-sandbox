using UnityEngine;

/// <summary>
/// Persisted audio, graphics and gameplay feel preferences via PlayerPrefs.
/// Headless EditMode tests fall back to defaults without touching PlayerPrefs.
/// </summary>
public static class DiabloSettings
{
    public static float MasterVol = 0.8f;
    public static float MusicVol = 0.8f;
    public static float SFXVol = 0.8f;
    public static float UIVol = 0.8f;
    public static int QualityLevel = 2; // 0 Low, 1 Med, 2 High
    public static bool ShowFPS = false;
    public static bool ScreenShake = true;
    public static bool HitStop = true;
    public static bool ShowDamageNumbers = true;
    public static bool VSync = true;
    public static bool Fullscreen = true;
    public static int ResolutionIndex = 0; // 0=1920x1080, 1=1600x900, 2=1280x720, 3=2560x1440

    const string Prefix = "DiabloMenu_";
    static readonly (int w, int h)[] Resolutions =
    {
        (1920, 1080),
        (1600, 900),
        (1280, 720),
        (2560, 1440),
    };

    static DiabloSettings() { Load(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BootApply()
    {
        if (!Application.isPlaying) return;
        Load();
        Apply();
    }

    public static void Load()
    {
        try
        {
            if (!Application.isPlaying && Application.isBatchMode) return;
            MasterVol = PlayerPrefs.GetFloat(Prefix + "Master", 0.8f);
            MusicVol = PlayerPrefs.GetFloat(Prefix + "Music", 0.8f);
            SFXVol = PlayerPrefs.GetFloat(Prefix + "SFX", 0.8f);
            UIVol = PlayerPrefs.GetFloat(Prefix + "UI", 0.8f);
            QualityLevel = PlayerPrefs.GetInt(Prefix + "Quality", 2);
            ShowFPS = PlayerPrefs.GetInt(Prefix + "FPS", 0) == 1;
            ScreenShake = PlayerPrefs.GetInt(Prefix + "Shake", 1) == 1;
            HitStop = PlayerPrefs.GetInt(Prefix + "HitStop", 1) == 1;
            ShowDamageNumbers = PlayerPrefs.GetInt(Prefix + "DmgNums", 1) == 1;
            VSync = PlayerPrefs.GetInt(Prefix + "VSync", 1) == 1;
            Fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) == 1;
            ResolutionIndex = PlayerPrefs.GetInt(Prefix + "Resolution", 0);
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
            PlayerPrefs.SetFloat(Prefix + "UI", UIVol);
            PlayerPrefs.SetInt(Prefix + "Quality", QualityLevel);
            PlayerPrefs.SetInt(Prefix + "FPS", ShowFPS ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Shake", ScreenShake ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "HitStop", HitStop ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "DmgNums", ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "VSync", VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Resolution", ResolutionIndex);
            PlayerPrefs.Save();
        }
        catch { /* headless — ignore */ }
    }

    public static void ResetDefaults()
    {
        MasterVol = 0.8f;
        MusicVol = 0.8f;
        SFXVol = 0.8f;
        UIVol = 0.8f;
        QualityLevel = 2;
        ShowFPS = false;
        ScreenShake = true;
        HitStop = true;
        ShowDamageNumbers = true;
        VSync = true;
        Fullscreen = true;
        ResolutionIndex = 0;
        Save();
        Apply();
    }

    public static void Clamp()
    {
        MasterVol = Mathf.Clamp01(MasterVol);
        MusicVol = Mathf.Clamp01(MusicVol);
        SFXVol = Mathf.Clamp01(SFXVol);
        UIVol = Mathf.Clamp01(UIVol);
        QualityLevel = Mathf.Clamp(QualityLevel, 0, 2);
        ResolutionIndex = ((ResolutionIndex % Resolutions.Length) + Resolutions.Length) % Resolutions.Length;
    }

    public static string QualityName => QualityLevel == 0 ? "Low" : QualityLevel == 1 ? "Med" : "High";

    public static (int w, int h) ResolutionSize
    {
        get
        {
            Clamp();
            return Resolutions[ResolutionIndex];
        }
    }

    public static string ResolutionLabel
    {
        get
        {
            var r = ResolutionSize;
            return $"{r.w}x{r.h}";
        }
    }

    public static void CycleResolution()
    {
        ResolutionIndex = (ResolutionIndex + 1) % Resolutions.Length;
    }

    /// <summary>Effective SFX gain (master × sfx). Sources multiply through this.</summary>
    public static float EffectiveSFX => MasterVol * SFXVol;

    /// <summary>Effective music gain (master × music).</summary>
    public static float EffectiveMusic => MasterVol * MusicVol;

    /// <summary>Effective UI click/chime gain (master × ui).</summary>
    public static float EffectiveUI => MasterVol * UIVol;

    public static void Apply()
    {
        Clamp();
        try { AudioListener.volume = Mathf.Clamp01(MasterVol); } catch { }
        try { global::HitStop.Enabled = HitStop; } catch { }
        try { DiabloAmbience.ApplySettings(); } catch { }
        try
        {
            if (Application.isPlaying && !Application.isBatchMode)
            {
                if (QualitySettings.names != null && QualitySettings.names.Length > 0)
                {
                    int target = Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1);
                    QualitySettings.SetQualityLevel(target, true);
                }
                QualitySettings.vSyncCount = VSync ? 1 : 0;
            }
        }
        catch { }
        try
        {
            if (Application.isPlaying && !Application.isBatchMode && !Application.isEditor)
            {
                var r = ResolutionSize;
                Screen.SetResolution(r.w, r.h, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
        }
        catch { }
    }
}
