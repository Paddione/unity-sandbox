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
    public static float VoiceVol = 0.9f;
    public static int QualityLevel = 2; // 0 Low, 1 Med, 2 High
    public static bool ShowFPS = false;
    public static bool ScreenShake = true;
    public static bool HitStop = true;
    public static bool ShowDamageNumbers = true;
    public static bool VSync = true;
    public static bool Fullscreen = true;
    public static int ResolutionIndex = 0; // 0=1920x1080, 1=1600x900, 2=1280x720, 3=2560x1440
    /// <summary>Zone map (M) opens fullscreen when true; overlay is the default (cinderreach-map-overlay D2).</summary>
    public static bool MapFullscreen = false;
    /// <summary>Ground loot names always shown; off = only while Alt is held (cinderreach-loot-click-filter D2).</summary>
    public static bool LootLabelsAlways = true;
    /// <summary>Clicks pick up loot only while <see cref="LootKey"/> is held (D2, default off).</summary>
    public static bool LootRequiresKey = false;
    /// <summary>Loot key for <see cref="LootRequiresKey"/>; rebindable in the settings panel.</summary>
    public static UnityEngine.InputSystem.Key LootKey = UnityEngine.InputSystem.Key.LeftAlt;
    /// <summary>Loot filter: true hides Rare (and Magic) ground labels, Uniques stay (D3).</summary>
    public static bool LootFilterUniqueOnly = false;

    const string Prefix = "DiabloMenu_";
    static readonly (int w, int h)[] Resolutions =
    {
        (1920, 1080),
        (1600, 900),
        (1280, 720),
        (2560, 1440),
        // Intel iGPU fallbacks: HD 620 class targets 960x540 for 30fps,
        // Iris Xe class targets 1280x720 (index 2). Kept at the end so
        // existing saves (0..3) keep their resolution.
        (960, 540),
        (854, 480),
    };

    static DiabloSettings() { Load(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BootApply()
    {
        if (!Application.isPlaying) return;
        Load();
        AutoPickIntelGpuIfFresh();
        ApplyAudioAndFeel();
        bool savedResolution = false;
        try { savedResolution = PlayerPrefs.HasKey(Prefix + "Resolution"); } catch { }
        if (BootMayTouchDisplay(System.Environment.GetCommandLineArgs()))
            ApplyDisplay(setResolution: savedResolution);
    }

    /// <summary>
    /// Boot rule: automation (autoplay, shotcheck, batch) and explicit
    /// -screen-* arguments own the display; the saved menu choice must not
    /// override them (a forced 1080p fullscreen cost the FPS gate ~2.7x).
    /// </summary>
    public static bool BootMayTouchDisplay(string[] args)
    {
        if (AutomationArgs.IsDriven(args)
            || AutomationArgs.Any(args, AutomationArgs.BatchMode, AutomationArgs.NoGraphics)) return false;
        foreach (var a in args)
        {
            if (a == null) continue;
            if (a.StartsWith("-screen-")) return false;
        }
        return true;
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
            VoiceVol = PlayerPrefs.GetFloat(Prefix + "Voice", 0.9f);
            QualityLevel = PlayerPrefs.GetInt(Prefix + "Quality", 2);
            ShowFPS = PlayerPrefs.GetInt(Prefix + "FPS", 0) == 1;
            ScreenShake = PlayerPrefs.GetInt(Prefix + "Shake", 1) == 1;
            HitStop = PlayerPrefs.GetInt(Prefix + "HitStop", 1) == 1;
            ShowDamageNumbers = PlayerPrefs.GetInt(Prefix + "DmgNums", 1) == 1;
            VSync = PlayerPrefs.GetInt(Prefix + "VSync", 1) == 1;
            Fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) == 1;
            ResolutionIndex = PlayerPrefs.GetInt(Prefix + "Resolution", 0);
            MapFullscreen = PlayerPrefs.GetInt(Prefix + "MapFullscreen", 0) == 1;
            LootLabelsAlways = PlayerPrefs.GetInt(Prefix + "LootLabels", 1) == 1;
            LootRequiresKey = PlayerPrefs.GetInt(Prefix + "LootRequiresKey", 0) == 1;
            LootKey = (UnityEngine.InputSystem.Key)PlayerPrefs.GetInt(Prefix + "LootKey", (int)UnityEngine.InputSystem.Key.LeftAlt);
            LootFilterUniqueOnly = PlayerPrefs.GetInt(Prefix + "LootFilter", 0) == 1;
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
            PlayerPrefs.SetFloat(Prefix + "Voice", VoiceVol);
            PlayerPrefs.SetInt(Prefix + "Quality", QualityLevel);
            PlayerPrefs.SetInt(Prefix + "FPS", ShowFPS ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Shake", ScreenShake ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "HitStop", HitStop ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "DmgNums", ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "VSync", VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Resolution", ResolutionIndex);
            PlayerPrefs.SetInt(Prefix + "MapFullscreen", MapFullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "LootLabels", LootLabelsAlways ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "LootRequiresKey", LootRequiresKey ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "LootKey", (int)LootKey);
            PlayerPrefs.SetInt(Prefix + "LootFilter", LootFilterUniqueOnly ? 1 : 0);
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
        VoiceVol = 0.9f;
        QualityLevel = 2;
        ShowFPS = false;
        ScreenShake = true;
        HitStop = true;
        ShowDamageNumbers = true;
        VSync = true;
        Fullscreen = true;
        ResolutionIndex = 0;
        MapFullscreen = false;
        LootLabelsAlways = true;
        LootRequiresKey = false;
        LootKey = UnityEngine.InputSystem.Key.LeftAlt;
        LootFilterUniqueOnly = false;
        Save();
        Apply();
    }

    public static void Clamp()
    {
        MasterVol = Mathf.Clamp01(MasterVol);
        MusicVol = Mathf.Clamp01(MusicVol);
        SFXVol = Mathf.Clamp01(SFXVol);
        UIVol = Mathf.Clamp01(UIVol);
        VoiceVol = Mathf.Clamp01(VoiceVol);
        QualityLevel = Mathf.Clamp(QualityLevel, 0, 2);
        ResolutionIndex = ((ResolutionIndex % Resolutions.Length) + Resolutions.Length) % Resolutions.Length;
        if (!LootClick.IsBindable(LootKey)) LootKey = UnityEngine.InputSystem.Key.LeftAlt;
    }

    public static string QualityName => QualityLevel == 0 ? Loc.T("settings.quality.low") : QualityLevel == 1 ? Loc.T("settings.quality.med") : Loc.T("settings.quality.high");

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

    /// <summary>Effective dialog voice gain (master × voice).</summary>
    public static float EffectiveVoice => MasterVol * VoiceVol;

    /// <summary>Menu "Apply": everything, including the chosen resolution.</summary>
    public static void Apply()
    {
        ApplyAudioAndFeel();
        ApplyDisplay(setResolution: true);
    }

    static void ApplyAudioAndFeel()
    {
        Clamp();
        try { AudioListener.volume = Mathf.Clamp01(MasterVol); } catch { }
        try { global::HitStop.Enabled = HitStop; } catch { }
        try { DiabloAmbience.ApplySettings(); } catch { }
    }

    static void ApplyDisplay(bool setResolution)
    {
        Clamp();
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
                ApplyTierTweaks(QualityLevel);
            }
        }
        catch { }
        try
        {
            if (setResolution && Application.isPlaying && !Application.isBatchMode && !Application.isEditor)
            {
                var r = ResolutionSize;
                Screen.SetResolution(r.w, r.h, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
        }
        catch { }
    }

    /// <summary>
    /// Intel iGPU tiers for 30fps+: tier 0 = Potato (HD 620 class, i5-7200U),
    /// tier 1 = Low (Iris Xe class, i7-1165G7), tier 2 = High (discrete).
    /// Tiers 1/2 share the PC RPAsset, so the split is applied here at runtime
    /// (renderScale via reflection so this compiles without the URP package
    /// reference; everything else is plain QualitySettings). Headless-safe.
    /// </summary>
    static void ApplyTierTweaks(int level)
    {
        try
        {
            switch (level)
            {
                case 0: // Potato: HD 620 — no shadows, half render scale.
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowDistance = 0f;
                    QualitySettings.shadowCascades = 0;
                    QualitySettings.pixelLightCount = 0;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.softParticles = false;
                    QualitySettings.lodBias = 0.7f;
                    TrySetRenderScale(0.5f);
                    break;
                case 1: // Low: Iris Xe — hard shadows only, reduced scale/distance.
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowDistance = 20f;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.pixelLightCount = 1;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.softParticles = false;
                    QualitySettings.lodBias = 1f;
                    TrySetRenderScale(0.65f);
                    break;
                default: // High: asset defaults (PC RPAsset, scale 1.0).
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 40f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.pixelLightCount = 2;
                    TrySetRenderScale(1f);
                    break;
            }
        }
        catch { }
    }

    /// <summary>Sets URP renderScale on the active RPAsset via reflection (no URP compile dependency).</summary>
    static void TrySetRenderScale(float v)
    {
        try
        {
            var rp = QualitySettings.renderPipeline;
            if (rp == null) return;
            var p = rp.GetType().GetProperty("renderScale");
            if (p != null && p.CanWrite) p.SetValue(rp, Mathf.Clamp(v, 0.25f, 1f), null);
        }
        catch { }
    }

    /// <summary>
    /// First-boot guard: when no quality choice was saved yet, pick a tier +
    /// resolution from the GPU name. Existing saves are never touched.
    /// i5-7200U/HD 620 -&gt; Potato + 960x540 windowed, VSync off.
    /// i7-1165G7/Iris Xe -&gt; Low + 1280x720, VSync on.
    /// </summary>
    static void AutoPickIntelGpuIfFresh()
    {
        try
        {
            if (PlayerPrefs.HasKey(Prefix + "Quality")) return;
            string gpu = "";
            try { gpu = SystemInfo.graphicsDeviceName ?? ""; } catch { }
            int q = RecommendQualityLevel(gpu);
            int r = RecommendResolutionIndex(gpu);
            QualityLevel = q;
            if (r >= 0) ResolutionIndex = r;
            if (q == 0) { VSync = false; Fullscreen = false; }
            Save();
        }
        catch { }
    }

    /// <summary>Pure GPU-string -&gt; quality tier mapping (test seam, no Unity state).</summary>
    public static int RecommendQualityLevel(string gpuName)
    {
        string g = (gpuName ?? "").ToLowerInvariant();
        if (g.Contains("hd 620") || g.Contains("hd 520") || g.Contains("hd 530") ||
            g.Contains("hd 515") || g.Contains("hd 615") || g.Contains("uhd 610") ||
            g.Contains("uhd 600") || g.Contains("bay trail") || g.Contains("cherry trail"))
            return 0;
        if (g.Contains("iris xe") || g.Contains("iris plus") || g.Contains("iris pro") ||
            g.Contains("uhd 7") || g.Contains("uhd 6") || g.Contains("arc ") ||
            (g.Contains("intel") && (g.Contains("xe") || g.Contains("uhd"))))
            return 1;
        if (g.Contains("intel")) return 1; // unknown Intel iGPU: safe Low.
        return 2;
    }

    /// <summary>Pure GPU-string -&gt; resolution index mapping. -1 = keep default.</summary>
    public static int RecommendResolutionIndex(string gpuName)
    {
        string g = (gpuName ?? "").ToLowerInvariant();
        if (g.Contains("hd 620") || g.Contains("hd 520") || g.Contains("hd 530") ||
            g.Contains("hd 515") || g.Contains("hd 615") || g.Contains("uhd 610") ||
            g.Contains("uhd 600") || g.Contains("bay trail") || g.Contains("cherry trail"))
            return 4; // 960x540
        if (g.Contains("iris xe") || g.Contains("iris plus") || g.Contains("iris pro") ||
            g.Contains("uhd 7") || g.Contains("uhd 6") || g.Contains("arc ") ||
            g.Contains("intel"))
            return 2; // 1280x720
        return -1;
    }
}
