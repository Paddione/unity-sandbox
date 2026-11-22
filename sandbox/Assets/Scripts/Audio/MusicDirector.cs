using System.Collections;
using UnityEngine;

/// <summary>
/// S2 music director: crossfades zone loops / boss track / menu theme.
/// Headless-safe: no real AudioMixer asset required — gain routes through
/// DiabloSettings (Master x Music). TODO (human session, per craft-game-floors
/// game-audio headless floor): author Assets/Audio/CinderreachMixer.mixer with
/// Master/Music/SFX/UI groups and assign musicSource.outputAudioMixerGroup.
/// Synth loops below are placeholders until studio .ogg files land
/// (see docs/audio/manifest.json); 22050 Hz mono, 5 ms fades, like DiabloAudio.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }

    public const float CrossfadeSeconds = 2f;

    public string CurrentTrack { get; private set; } = "";
    public float crossfadeProgress = 1f;
    public int crossfadesStarted;

    AudioSource primary;
    AudioSource secondary;
    bool usingPrimary = true;
    Coroutine fade;

    /// <summary>Mirrors DiabloAudio.MutePlayback so music stays silent in tests.</summary>
    public static bool MutePlayback
    {
        get => DiabloAudio.MutePlayback;
        set => DiabloAudio.MutePlayback = value;
    }

    static bool PlaybackMuted
    {
        get
        {
            try { return DiabloAudio.MutePlayback || Application.isBatchMode; }
            catch { return DiabloAudio.MutePlayback; }
        }
    }

    public static MusicDirector Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("MusicDirector");
        if (Application.isPlaying) DontDestroyOnLoad(go);
        Instance = go.AddComponent<MusicDirector>();
        Instance.BuildSources();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildSources();
    }

    void BuildSources()
    {
        if (primary == null)
        {
            primary = gameObject.AddComponent<AudioSource>();
            secondary = gameObject.AddComponent<AudioSource>();
            primary.loop = true;
            secondary.loop = true;
        }
    }

    float MusicGain => Application.isPlaying
        ? DiabloSettings.MasterVol * DiabloSettings.MusicVol
        : 1f;

    AudioClip SynthLoop(string name, float root, int seed)
    {
        const float seconds = 8f; // short deterministic stand-in; real loops are 60-90 s .ogg
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * DiabloAudio.SampleRate));
        var data = new float[n];
        var rng = new System.Random(seed);
        float brown = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)DiabloAudio.SampleRate;
            brown = Mathf.Clamp(brown + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.02f, -1f, 1f) * 0.998f;
            float pad = Mathf.Sin(2f * Mathf.PI * root * t)
                + 0.5f * Mathf.Sin(2f * Mathf.PI * root * 1.5f * t)
                + 0.25f * Mathf.Sin(2f * Mathf.PI * root * 2f * t);
            data[i] = (pad * 0.18f + brown * 0.05f);
        }
        int edge = Mathf.Min(data.Length / 2, Mathf.RoundToInt(0.005f * DiabloAudio.SampleRate));
        for (int i = 0; i < edge; i++)
        {
            float w = (float)i / edge;
            data[i] *= w;
            data[data.Length - 1 - i] *= w;
        }
        var clip = AudioClip.Create(name, data.Length, 1, DiabloAudio.SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public AudioClip TrackClip(string track)
    {
        switch (track)
        {
            case "Menu": return SynthLoop("MenuTheme", 220f, 101);
            case "Zone0": return SynthLoop("LanternHoldLoop", 174.6f, 102);
            case "Zone1": return SynthLoop("AshenFenLoop", 146.8f, 103);
            case "Zone2": return SynthLoop("OssuaryLoop", 123.5f, 104);
            case "Boss": return SynthLoop("BossTrack", 98f, 105);
            case "Zone4": return SynthLoop("GlassBarrensLoop", 110f, 106);
            case GlassBossTrack: return SynthLoop("GlasswrightTrack", 92.5f, 107);
            default: return SynthLoop(track, 174.6f, 42);
        }
    }

    /// <summary>act-two-opening S3: the Glasswright's own track.</summary>
    public const string GlassBossTrack = "BossGlass";

    public void PlayMenu() => PlayTrack("Menu");
    /// <summary>Zones 0–3 keep their historical clamp (the Kiln shares the Ossuary loop); the Barrens get their own.</summary>
    public void PlayZone(int zoneIndex) =>
        PlayTrack(zoneIndex == ActProgress.Barrens ? "Zone4" : "Zone" + Mathf.Clamp(zoneIndex, 0, 2));
    public void PlayBoss() => PlayTrack("Boss");

    public void PlayTrack(string track)
    {
        if (CurrentTrack == track) return;
        CurrentTrack = track;
        crossfadesStarted++;
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(CrossfadeTo(TrackClip(track)));
    }

    IEnumerator CrossfadeTo(AudioClip next)
    {
        BuildSources();
        var @in = usingPrimary ? secondary : primary;
        var @out = usingPrimary ? primary : secondary;
        usingPrimary = !usingPrimary;
        if (!PlaybackMuted)
        {
            try
            {
                @in.clip = next;
                @in.volume = 0f;
                @in.Play();
            }
            catch { }
        }
        crossfadeProgress = 0f;
        float t = 0f;
        while (t < CrossfadeSeconds)
        {
            t += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.05f;
            crossfadeProgress = Mathf.Clamp01(t / CrossfadeSeconds);
            try
            {
                @in.volume = crossfadeProgress * MusicGain;
                @out.volume = (1f - crossfadeProgress) * MusicGain;
            }
            catch { }
            yield return null;
        }
        try { @out.Stop(); } catch { }
        crossfadeProgress = 1f;
    }

    void Update()
    {
        // Keep volumes glued to the settings gain layer (headless-safe).
        try
        {
            if (primary != null && primary.isPlaying) primary.volume = Mathf.Clamp01(primary.volume);
            if (secondary != null && secondary.isPlaying) secondary.volume = Mathf.Clamp01(secondary.volume);
        }
        catch { }
    }
}
