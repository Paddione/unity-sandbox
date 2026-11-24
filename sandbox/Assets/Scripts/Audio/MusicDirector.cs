using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S2 music director: crossfades zone loops / boss track / menu theme.
/// Headless-safe: no real AudioMixer asset required — gain routes through
/// DiabloSettings (Master x Music).
/// Synthesizes 16-second circularly-seamless thematic loops (warm chord pads,
/// sparse non-distracting melodies, subtle room/forge/battle pulse, low-pass
/// warmth filter) until studio .ogg files replace them 1:1.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }

    public const float CrossfadeSeconds = 2f;
    const float LoopSeconds = 16f;
    const float BarSeconds = 4f;

    public string CurrentTrack { get; private set; } = "";
    public float crossfadeProgress = 1f;
    public int crossfadesStarted;

    AudioSource primary;
    AudioSource secondary;
    bool usingPrimary = true;
    Coroutine fade;
    readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

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

    enum MelodyTimbre { Lute, Cello, BronzeBell, GlassMarimba, LowHorn }

    struct NoteEvent
    {
        public float Time, Duration, Freq, Vel;
        public NoteEvent(float time, float duration, float freq, float vel)
        {
            Time = time; Duration = duration; Freq = freq; Vel = vel;
        }
    }

    struct Chord
    {
        public float Bass, Mid, Top;
        public Chord(float bass, float mid, float top)
        {
            Bass = bass; Mid = mid; Top = top;
        }
    }

    struct ThemeSpec
    {
        public Chord[] Chords;
        public NoteEvent[] Melody;
        public MelodyTimbre Timbre;
        public float CutoffHz;
        public float NoiseAmt;
        public float PulseIntervalSec; // 0 = none
        public float PulseAmp;
        public float PadGain;
        public float MelodyGain;
    }

    static ThemeSpec SpecFor(string name)
    {
        switch (name)
        {
            case "MenuTheme":
                // D Aeolian/Dorian: Dm -> C -> Bb -> Am, calm lute motif with breathing space.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(73.42f, 146.83f, 220.00f),
                        new Chord(65.41f, 130.81f, 196.00f),
                        new Chord(58.27f, 116.54f, 174.61f),
                        new Chord(55.00f, 110.00f, 164.81f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.30f, 1.50f, 293.66f, 0.85f),
                        new NoteEvent(1.80f, 1.10f, 349.23f, 0.78f),
                        new NoteEvent(2.90f, 1.20f, 329.63f, 0.72f),
                        new NoteEvent(4.30f, 1.50f, 261.63f, 0.82f),
                        new NoteEvent(5.80f, 1.10f, 329.63f, 0.75f),
                        new NoteEvent(6.90f, 1.20f, 293.66f, 0.70f),
                        new NoteEvent(8.30f, 1.50f, 233.08f, 0.80f),
                        new NoteEvent(9.80f, 1.10f, 293.66f, 0.75f),
                        new NoteEvent(10.90f, 1.20f, 261.63f, 0.68f),
                        new NoteEvent(12.30f, 2.80f, 220.00f, 0.80f),
                    },
                    Timbre = MelodyTimbre.Lute,
                    CutoffHz = 1150f,
                    NoiseAmt = 0.025f,
                    PulseIntervalSec = 0f,
                    PulseAmp = 0f,
                    PadGain = 0.55f,
                    MelodyGain = 0.62f,
                };

            case "LanternHoldLoop":
                // F Major / D Dorian hearth: F -> Dm -> Bb -> C, gentle acoustic safe-haven melody.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(87.31f, 174.61f, 261.63f),
                        new Chord(73.42f, 146.83f, 220.00f),
                        new Chord(58.27f, 116.54f, 174.61f),
                        new Chord(65.41f, 130.81f, 196.00f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.25f, 1.30f, 220.00f, 0.75f),
                        new NoteEvent(1.50f, 1.30f, 261.63f, 0.80f),
                        new NoteEvent(2.75f, 1.50f, 293.66f, 0.72f),
                        new NoteEvent(4.40f, 1.60f, 220.00f, 0.78f),
                        new NoteEvent(6.00f, 1.80f, 174.61f, 0.70f),
                        new NoteEvent(8.25f, 1.20f, 293.66f, 0.76f),
                        new NoteEvent(9.50f, 1.20f, 261.63f, 0.72f),
                        new NoteEvent(10.75f, 1.40f, 233.08f, 0.68f),
                        new NoteEvent(12.25f, 1.20f, 196.00f, 0.72f),
                        new NoteEvent(13.50f, 1.10f, 220.00f, 0.70f),
                        new NoteEvent(14.70f, 1.60f, 261.63f, 0.75f),
                    },
                    Timbre = MelodyTimbre.Lute,
                    CutoffHz = 1200f,
                    NoiseAmt = 0.02f,
                    PulseIntervalSec = 0f,
                    PulseAmp = 0f,
                    PadGain = 0.58f,
                    MelodyGain = 0.58f,
                };

            case "AshenFenLoop":
                // D Aeolian pedal: Dm -> Gm/D -> Dm -> A5/D, mournful bowed cello line + moor breeze.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(73.42f, 110.00f, 174.61f),
                        new Chord(73.42f, 116.54f, 196.00f),
                        new Chord(73.42f, 110.00f, 174.61f),
                        new Chord(73.42f, 110.00f, 164.81f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.40f, 1.80f, 293.66f, 0.80f),
                        new NoteEvent(2.20f, 1.40f, 349.23f, 0.85f),
                        new NoteEvent(4.20f, 1.50f, 329.63f, 0.76f),
                        new NoteEvent(5.80f, 1.20f, 261.63f, 0.65f),
                        new NoteEvent(8.30f, 1.40f, 233.08f, 0.78f),
                        new NoteEvent(9.75f, 1.40f, 220.00f, 0.75f),
                        new NoteEvent(11.15f, 1.10f, 196.00f, 0.68f),
                        new NoteEvent(12.40f, 2.00f, 220.00f, 0.74f),
                    },
                    Timbre = MelodyTimbre.Cello,
                    CutoffHz = 1000f,
                    NoiseAmt = 0.055f,
                    PulseIntervalSec = 0f,
                    PulseAmp = 0f,
                    PadGain = 0.52f,
                    MelodyGain = 0.64f,
                };

            case "OssuaryLoop":
                // B Phrygian crypt: Bm -> C/B -> Em/B -> F#5/B, sparse distant bronze bells.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(61.74f, 123.47f, 185.00f),
                        new Chord(61.74f, 130.81f, 196.00f),
                        new Chord(61.74f, 123.47f, 164.81f),
                        new Chord(61.74f, 110.00f, 185.00f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.50f, 2.20f, 246.94f, 0.80f),
                        new NoteEvent(2.70f, 1.80f, 261.63f, 0.74f),
                        new NoteEvent(4.90f, 2.10f, 246.94f, 0.78f),
                        new NoteEvent(7.10f, 1.70f, 196.00f, 0.70f),
                        new NoteEvent(9.20f, 1.90f, 220.00f, 0.76f),
                        new NoteEvent(11.10f, 1.70f, 185.00f, 0.70f),
                        new NoteEvent(13.00f, 2.60f, 246.94f, 0.76f),
                    },
                    Timbre = MelodyTimbre.BronzeBell,
                    CutoffHz = 950f,
                    NoiseAmt = 0.03f,
                    PulseIntervalSec = 0f,
                    PulseAmp = 0f,
                    PadGain = 0.56f,
                    MelodyGain = 0.58f,
                };

            case "KilnLoop":
                // C Phrygian forge: Cm -> Db/C -> Ab -> G5, brooding low horn + slow muffled forge thump.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(65.41f, 130.81f, 196.00f),
                        new Chord(65.41f, 138.59f, 207.65f),
                        new Chord(51.91f, 103.83f, 155.56f),
                        new Chord(49.00f, 98.00f, 146.83f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.35f, 1.60f, 196.00f, 0.82f),
                        new NoteEvent(2.05f, 1.20f, 233.08f, 0.78f),
                        new NoteEvent(3.30f, 1.20f, 207.65f, 0.74f),
                        new NoteEvent(4.80f, 2.30f, 196.00f, 0.80f),
                        new NoteEvent(8.30f, 1.40f, 155.56f, 0.78f),
                        new NoteEvent(9.80f, 1.30f, 146.83f, 0.74f),
                        new NoteEvent(11.15f, 1.20f, 130.81f, 0.70f),
                        new NoteEvent(12.60f, 2.60f, 146.83f, 0.78f),
                    },
                    Timbre = MelodyTimbre.LowHorn,
                    CutoffHz = 950f,
                    NoiseAmt = 0.035f,
                    PulseIntervalSec = 2.0f,
                    PulseAmp = 0.14f,
                    PadGain = 0.54f,
                    MelodyGain = 0.62f,
                };

            case "GlassBarrensLoop":
                // A Dorian crystal desert: Am -> G/A -> F#m7/A -> Em/A, warm glass marimba motif.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(55.00f, 110.00f, 164.81f),
                        new Chord(55.00f, 123.47f, 196.00f),
                        new Chord(55.00f, 110.00f, 185.00f),
                        new Chord(55.00f, 123.47f, 164.81f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.30f, 1.30f, 220.00f, 0.78f),
                        new NoteEvent(1.65f, 1.30f, 329.63f, 0.82f),
                        new NoteEvent(3.00f, 1.40f, 293.66f, 0.74f),
                        new NoteEvent(4.60f, 2.20f, 246.94f, 0.78f),
                        new NoteEvent(8.30f, 1.20f, 261.63f, 0.76f),
                        new NoteEvent(9.60f, 1.20f, 246.94f, 0.72f),
                        new NoteEvent(10.85f, 1.50f, 220.00f, 0.76f),
                        new NoteEvent(12.60f, 1.40f, 164.81f, 0.70f),
                        new NoteEvent(14.10f, 1.70f, 196.00f, 0.74f),
                    },
                    Timbre = MelodyTimbre.GlassMarimba,
                    CutoffHz = 1250f,
                    NoiseAmt = 0.03f,
                    PulseIntervalSec = 0f,
                    PulseAmp = 0f,
                    PadGain = 0.54f,
                    MelodyGain = 0.58f,
                };

            case "GlasswrightTrack":
                // F# Phrygian boss: F#m -> G -> Em -> C#5, tense marimba/horn motif + steady muted drum.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(46.25f, 92.50f, 138.59f),
                        new Chord(49.00f, 98.00f, 146.83f),
                        new Chord(41.20f, 82.41f, 123.47f),
                        new Chord(69.30f, 103.83f, 138.59f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.20f, 0.95f, 185.00f, 0.82f),
                        new NoteEvent(1.20f, 0.95f, 220.00f, 0.85f),
                        new NoteEvent(2.20f, 1.40f, 196.00f, 0.80f),
                        new NoteEvent(4.20f, 1.10f, 185.00f, 0.78f),
                        new NoteEvent(5.40f, 1.50f, 164.81f, 0.75f),
                        new NoteEvent(8.20f, 0.95f, 196.00f, 0.82f),
                        new NoteEvent(9.20f, 0.95f, 246.94f, 0.85f),
                        new NoteEvent(10.20f, 1.40f, 220.00f, 0.80f),
                        new NoteEvent(12.20f, 0.95f, 196.00f, 0.78f),
                        new NoteEvent(13.20f, 0.95f, 185.00f, 0.76f),
                        new NoteEvent(14.20f, 1.50f, 164.81f, 0.80f),
                    },
                    Timbre = MelodyTimbre.GlassMarimba,
                    CutoffHz = 1200f,
                    NoiseAmt = 0.03f,
                    PulseIntervalSec = 0.5f,
                    PulseAmp = 0.16f,
                    PadGain = 0.50f,
                    MelodyGain = 0.62f,
                };

            default: // "BossTrack"
                // G Phrygian/Minor: Gm -> Ab -> Fm -> D5, focused urgent horn motif + war-drum pulse.
                return new ThemeSpec
                {
                    Chords = new[]
                    {
                        new Chord(49.00f, 98.00f, 146.83f),
                        new Chord(51.91f, 103.83f, 155.56f),
                        new Chord(43.65f, 87.31f, 130.81f),
                        new Chord(73.42f, 110.00f, 146.83f),
                    },
                    Melody = new[]
                    {
                        new NoteEvent(0.20f, 0.95f, 196.00f, 0.85f),
                        new NoteEvent(1.20f, 0.95f, 233.08f, 0.82f),
                        new NoteEvent(2.20f, 1.40f, 220.00f, 0.78f),
                        new NoteEvent(4.20f, 0.95f, 207.65f, 0.82f),
                        new NoteEvent(5.20f, 0.95f, 196.00f, 0.78f),
                        new NoteEvent(6.20f, 1.50f, 174.61f, 0.76f),
                        new NoteEvent(8.20f, 0.90f, 196.00f, 0.82f),
                        new NoteEvent(9.10f, 0.90f, 233.08f, 0.85f),
                        new NoteEvent(10.00f, 0.95f, 261.63f, 0.88f),
                        new NoteEvent(11.00f, 1.00f, 233.08f, 0.80f),
                        new NoteEvent(12.25f, 2.75f, 220.00f, 0.84f),
                    },
                    Timbre = MelodyTimbre.LowHorn,
                    CutoffHz = 1100f,
                    NoiseAmt = 0.03f,
                    PulseIntervalSec = 0.5f,
                    PulseAmp = 0.18f,
                    PadGain = 0.52f,
                    MelodyGain = 0.64f,
                };
        }
    }

    static void AddCircularPad(float[] data, int n, ThemeSpec spec)
    {
        const float overlap = 0.85f;
        int chordLen = Mathf.RoundToInt((BarSeconds + overlap) * DiabloAudio.SampleRate);
        int overlapSamples = Mathf.RoundToInt(overlap * DiabloAudio.SampleRate);

        for (int b = 0; b < spec.Chords.Length; b++)
        {
            var c = spec.Chords[b];
            int start = Mathf.RoundToInt(b * BarSeconds * DiabloAudio.SampleRate);
            for (int i = 0; i < chordLen; i++)
            {
                float time = i / (float)DiabloAudio.SampleRate;
                float fadeIn = i < overlapSamples ? 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / overlapSamples) : 1f;
                int rem = chordLen - 1 - i;
                float fadeOut = rem < overlapSamples ? 0.5f - 0.5f * Mathf.Cos(Mathf.PI * rem / overlapSamples) : 1f;
                float env = fadeIn * fadeOut * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.25f * time));

                float s = 0.45f * Mathf.Sin(2f * Mathf.PI * c.Bass * time)
                        + 0.35f * Mathf.Sin(2f * Mathf.PI * c.Mid * time)
                        + 0.28f * Mathf.Sin(2f * Mathf.PI * c.Top * time)
                        + 0.10f * Mathf.Sin(2f * Mathf.PI * (c.Mid * 2f) * time);

                data[(start + i) % n] += s * env * spec.PadGain;
            }
        }
    }

    static void AddCircularMelody(float[] data, int n, ThemeSpec spec)
    {
        for (int m = 0; m < spec.Melody.Length; m++)
        {
            var note = spec.Melody[m];
            int start = Mathf.RoundToInt(note.Time * DiabloAudio.SampleRate);
            int count = Mathf.Max(1, Mathf.RoundToInt(note.Duration * DiabloAudio.SampleRate));
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float time = i / (float)DiabloAudio.SampleRate;
                float env;
                float sig;

                switch (spec.Timbre)
                {
                    case MelodyTimbre.Cello:
                    {
                        float att = Mathf.Clamp01(time / 0.09f);
                        float rel = Mathf.Clamp01((note.Duration - time) / 0.22f);
                        float vib = 1f + 0.0035f * Mathf.Sin(2f * Mathf.PI * 4.5f * time) * Mathf.Clamp01(time / 0.25f);
                        float f = note.Freq * vib;
                        env = att * rel;
                        sig = 0.65f * Mathf.Sin(2f * Mathf.PI * f * time)
                            + 0.26f * Mathf.Sin(2f * Mathf.PI * f * 2f * time)
                            + 0.09f * Mathf.Sin(2f * Mathf.PI * f * 3f * time);
                        break;
                    }
                    case MelodyTimbre.BronzeBell:
                    {
                        float att = Mathf.Clamp01(time / 0.018f);
                        env = att * Mathf.Exp(-2.8f * t) * Mathf.Clamp01((1f - t) / 0.12f);
                        sig = 0.30f * Mathf.Sin(2f * Mathf.PI * (note.Freq * 0.5f) * time)
                            + 0.58f * Mathf.Sin(2f * Mathf.PI * note.Freq * time)
                            + 0.18f * Mathf.Sin(2f * Mathf.PI * (note.Freq * 1.5f) * time) * Mathf.Exp(-6f * t);
                        break;
                    }
                    case MelodyTimbre.GlassMarimba:
                    {
                        float att = Mathf.Clamp01(time / 0.010f);
                        env = att * Mathf.Exp(-3.0f * t) * Mathf.Clamp01((1f - t) / 0.10f);
                        sig = 0.78f * Mathf.Sin(2f * Mathf.PI * note.Freq * time)
                            + 0.18f * Mathf.Sin(2f * Mathf.PI * (note.Freq * 2f) * time) * Mathf.Exp(-5.5f * t);
                        break;
                    }
                    case MelodyTimbre.LowHorn:
                    {
                        float att = Mathf.Clamp01(time / 0.075f);
                        float rel = Mathf.Clamp01((note.Duration - time) / 0.18f);
                        env = att * rel;
                        sig = 0.62f * Mathf.Sin(2f * Mathf.PI * note.Freq * time)
                            + 0.30f * Mathf.Sin(2f * Mathf.PI * note.Freq * 2f * time)
                            + 0.12f * Mathf.Sin(2f * Mathf.PI * note.Freq * 3f * time);
                        break;
                    }
                    default: // Lute
                    {
                        float att = Mathf.Clamp01(time / 0.012f);
                        env = att * Mathf.Exp(-2.6f * t) * Mathf.Clamp01((1f - t) / 0.10f);
                        sig = 0.72f * Mathf.Sin(2f * Mathf.PI * note.Freq * time)
                            + 0.24f * Mathf.Sin(2f * Mathf.PI * note.Freq * 2f * time) * Mathf.Exp(-5.5f * t)
                            + 0.08f * Mathf.Sin(2f * Mathf.PI * note.Freq * 3f * time) * Mathf.Exp(-8.5f * t);
                        break;
                    }
                }

                data[(start + i) % n] += sig * env * note.Vel * spec.MelodyGain;
            }
        }
    }

    static void AddCircularAtmosphereAndPulse(float[] data, int n, ThemeSpec spec, int seed)
    {
        var rng = new System.Random(seed);
        int fade = Mathf.RoundToInt(1.2f * DiabloAudio.SampleRate);
        var noise = new float[n + fade];
        float lp = 0f;
        float a = Mathf.Clamp01(1f - Mathf.Exp(-2f * Mathf.PI * 340f / DiabloAudio.SampleRate));
        for (int i = 0; i < noise.Length; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += a * (white - lp);
            noise[i] = lp;
        }
        for (int i = 0; i < fade; i++)
        {
            float w = (float)i / fade;
            noise[i] = noise[i] * w + noise[n + i] * (1f - w);
        }
        for (int i = 0; i < n; i++) data[i] += noise[i] * spec.NoiseAmt;

        if (spec.PulseIntervalSec > 0f && spec.PulseAmp > 0f)
        {
            int hits = Mathf.RoundToInt(LoopSeconds / spec.PulseIntervalSec);
            int hitSamples = Mathf.RoundToInt(0.18f * DiabloAudio.SampleRate);
            for (int h = 0; h < hits; h++)
            {
                int start = Mathf.RoundToInt(h * spec.PulseIntervalSec * DiabloAudio.SampleRate);
                float accent = (h % 4 == 0) ? 1.0f : 0.62f;
                float phase = 0f;
                for (int i = 0; i < hitSamples; i++)
                {
                    float t = (float)i / hitSamples;
                    float f = 72f * Mathf.Pow(38f / 72f, t);
                    phase += 2f * Mathf.PI * f / DiabloAudio.SampleRate;
                    float env = Mathf.Clamp01(t / 0.03f) * Mathf.Exp(-9f * t);
                    data[(start + i) % n] += Mathf.Sin(phase) * env * spec.PulseAmp * accent;
                }
            }
        }
    }

    static void LowPass(float[] data, float cutoffHz, int passes)
    {
        float a = Mathf.Clamp01(1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / DiabloAudio.SampleRate));
        for (int p = 0; p < passes; p++)
        {
            // Seed filter state from tail so loop wrap remains smooth.
            float y = data[data.Length - 1];
            for (int i = 0; i < data.Length; i++)
            {
                y += a * (data[i] - y);
                data[i] = y;
            }
        }
    }

    AudioClip SynthLoop(string name, float root, int seed)
    {
        if (clipCache.TryGetValue(name, out var cached) && cached != null)
            return cached;

        int n = Mathf.Max(1, Mathf.RoundToInt(LoopSeconds * DiabloAudio.SampleRate));
        var data = new float[n];
        var spec = SpecFor(name);

        AddCircularPad(data, n, spec);
        AddCircularMelody(data, n, spec);
        AddCircularAtmosphereAndPulse(data, n, spec, seed);
        LowPass(data, spec.CutoffHz, 2);

        double sum = 0.0;
        for (int i = 0; i < n; i++) sum += data[i];
        float mean = (float)(sum / n);
        float peak = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = data[i] - mean;
            x = x / (1f + Mathf.Abs(x) * 0.25f);
            data[i] = x;
            float ax = Mathf.Abs(x);
            if (ax > peak) peak = ax;
        }
        if (peak > 1e-5f)
        {
            float scale = 0.36f / peak;
            for (int i = 0; i < n; i++) data[i] *= scale;
        }

        var clip = AudioClip.Create(name, n, 1, DiabloAudio.SampleRate, false);
        clip.SetData(data, 0);
        clipCache[name] = clip;
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
        try
        {
            @in.clip = next;
            @in.volume = 0f;
            if (!PlaybackMuted) @in.Play();
        }
        catch { }
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
        try
        {
            @out.Stop();
            @in.volume = Mathf.Clamp01(MusicGain);
        }
        catch { }
        crossfadeProgress = 1f;
        fade = null;
    }

    void Update()
    {
        // Keep active music volume glued to DiabloSettings even when idle.
        try
        {
            if (fade == null && crossfadeProgress >= 1f)
            {
                var active = usingPrimary ? primary : secondary;
                if (active != null && (active.isPlaying || (PlaybackMuted && active.clip != null)))
                    active.volume = Mathf.Clamp01(MusicGain);
            }
            if (primary != null && primary.isPlaying) primary.volume = Mathf.Clamp01(primary.volume);
            if (secondary != null && secondary.isPlaying) secondary.volume = Mathf.Clamp01(secondary.volume);
        }
        catch { }
    }
}
