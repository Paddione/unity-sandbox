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

    /// <summary>Per-track synth character: fifth/major-third spacing, sub-octave weight,
    /// bell/glass partials, wind noise and a slow pulse — so areas differ in timbre, not only pitch.</summary>
    struct TrackSpec
    {
        public float Interval, Sub, Bell, Glass, Noise, PulseHz;
        public TrackSpec(float interval, float sub, float bell, float glass, float noise, float pulseHz)
        { Interval = interval; Sub = sub; Bell = bell; Glass = glass; Noise = noise; PulseHz = pulseHz; }
    }

    static TrackSpec SpecFor(string name)
    {
        switch (name)
        {
            case "MenuTheme": return new TrackSpec(1.5f, 0.2f, 0.1f, 0f, 0.03f, 0.125f);
            case "LanternHoldLoop": return new TrackSpec(1.26f, 0.1f, 0.15f, 0f, 0.02f, 0.1f); // warm major third, hearth
            case "AshenFenLoop": return new TrackSpec(1.19f, 0.3f, 0f, 0f, 0.12f, 0.07f); // minor, wind-heavy moor
            case "OssuaryLoop": return new TrackSpec(1.19f, 0.4f, 0.5f, 0f, 0.04f, 0.05f); // crypt bells
            case "KilnLoop": return new TrackSpec(1.19f, 0.6f, 0.2f, 0f, 0.06f, 0.5f); // forge sub + slow pulse
            case "GlassBarrensLoop": return new TrackSpec(1.33f, 0.1f, 0.1f, 0.55f, 0.03f, 0.08f); // glass partials
            case "GlasswrightTrack": return new TrackSpec(1.19f, 0.5f, 0.1f, 0.4f, 0.04f, 0.8f);
            default: return new TrackSpec(1.19f, 0.55f, 0.25f, 0f, 0.05f, 1f); // boss: driving sub pulse
        }
    }

    AudioClip SynthLoop(string name, float root, int seed)
    {
        const float seconds = 8f; // short deterministic stand-in; real loops are 60-90 s .ogg
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * DiabloAudio.SampleRate));
        var data = new float[n];
        var rng = new System.Random(seed);
        var spec = SpecFor(name);
        float brown = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)DiabloAudio.SampleRate;
            brown = Mathf.Clamp(brown + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.02f, -1f, 1f) * 0.998f;
            float cycle = i / (float)n;
            float pulse = spec.PulseHz > 0f ? 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * spec.PulseHz * seconds * cycle) : 1f;
            float pad = Mathf.Sin(2f * Mathf.PI * root * t)
                + 0.5f * Mathf.Sin(2f * Mathf.PI * root * spec.Interval * t)
                + spec.Sub * Mathf.Sin(2f * Mathf.PI * root * 0.5f * t)
                + spec.Bell * Mathf.Sin(2f * Mathf.PI * root * 2.76f * t) * Mathf.Exp(-0.5f * (cycle % 0.25f) * 4f)
                + spec.Glass * Mathf.Sin(2f * Mathf.PI * root * 3.98f * t);
            data[i] = (pad * 0.16f * pulse + brown * spec.Noise);
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
            case "Zone3": return SynthLoop("KilnLoop", 130.8f, 108);
            case "Boss": return SynthLoop("BossTrack", 98f, 105);
            case "Zone4": return SynthLoop("GlassBarrensLoop", 110f, 106);
            case GlassBossTrack: return SynthLoop("GlasswrightTrack", 92.5f, 107);
            default: return SynthLoop(track, 174.6f, 42);
        }
    }

    /// <summary>act-two-opening S3: the Glasswright's own track.</summary>
    public const string GlassBossTrack = "BossGlass";

    public void PlayMenu() => PlayTrack("Menu");

    /// <summary>Track per Act zone: Town plays the Fen field theme (Lantern Hold itself
    /// is reached by waypoint, see <see cref="PlayTown"/>), every later zone its own.</summary>
    public static string ZoneTrack(int zoneIndex)
    {
        switch (zoneIndex)
        {
            case ActProgress.Ossuary: return "Zone2";
            case ActProgress.Kiln: return "Zone3";
            case ActProgress.Barrens: return "Zone4";
            default: return "Zone1";
        }
    }

    public void PlayZone(int zoneIndex) => PlayTrack(ZoneTrack(zoneIndex));
    /// <summary>Lantern Hold hearth theme (waypoint arrival in town).</summary>
    public void PlayTown() => PlayTrack("Zone0");
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
