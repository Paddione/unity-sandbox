using System;
using UnityEngine;

/// <summary>
/// D2 combat sounds, fully synthesized at runtime (Blood Moor has no audio
/// pipeline and the repo bans binary assets). 22050 Hz mono clips built once
/// from decaying sine/noise shapes; playback is a silent no-op headless.
/// Static counters (strikeCount precedent) prove sounds fired in tests.
/// Play methods never log, so LogAssert suites stay clean.
/// </summary>
public static class DiabloAudio
{
    public const int SampleRate = 22050;

    public static int soundedSwings;
    public static int soundedHits;
    public static int soundedDeaths;
    public static int soundedLoot;
    public static int soundedCasts;
    public static int soundedBlasts;
    public static int soundedCrits;
    public static int soundedLevelUps;

    static AudioSource source;
    static AudioClip swingClip, hitClip, deathClip, lootClip, uiClickClip, hurtClip, playerDeathClip;
    static AudioClip whooshClip, blastClip, twangClip, critClip, levelUpClip, ambientClip;
    static readonly System.Random pitchRng = new System.Random(77);
    static bool built;

    public static AudioClip SwingClip { get { EnsureBuilt(); return swingClip; } }
    public static AudioClip HitClip { get { EnsureBuilt(); return hitClip; } }
    public static AudioClip DeathClip { get { EnsureBuilt(); return deathClip; } }
    public static AudioClip LootClip { get { EnsureBuilt(); return lootClip; } }
    public static AudioClip UIClickClip { get { EnsureBuilt(); return uiClickClip; } }
    public static AudioClip HurtClip { get { EnsureBuilt(); return hurtClip; } }
    public static AudioClip PlayerDeathClip { get { EnsureBuilt(); return playerDeathClip; } }
    public static AudioClip WhooshClip { get { EnsureBuilt(); return whooshClip; } }
    public static AudioClip BlastClip { get { EnsureBuilt(); return blastClip; } }
    public static AudioClip TwangClip { get { EnsureBuilt(); return twangClip; } }
    public static AudioClip CritClip { get { EnsureBuilt(); return critClip; } }
    public static AudioClip LevelUpClip { get { EnsureBuilt(); return levelUpClip; } }
    /// <summary>Seamless 6 s moor wind + low drone loop (ambient bed).</summary>
    public static AudioClip AmbientClip { get { EnsureBuilt(); return ambientClip; } }

    public static void PlaySwing() { EnsureBuilt(); source.PlayOneShot(swingClip); soundedSwings++; }
    public static void PlayHit() { EnsureBuilt(); source.PlayOneShot(hitClip); soundedHits++; }
    public static void PlayDeath() { EnsureBuilt(); source.PlayOneShot(deathClip); soundedDeaths++; }
    public static void PlayLoot() { EnsureBuilt(); source.PlayOneShot(lootClip); soundedLoot++; }
    public static void PlayUIClick() { EnsureBuilt(); source.PlayOneShot(uiClickClip); }
    public static void PlayHurt() { EnsureBuilt(); source.PlayOneShot(hurtClip); }
    public static void PlayPlayerDeath() { EnsureBuilt(); source.PlayOneShot(playerDeathClip); }
    /// <summary>Fireball leaving the hand.</summary>
    public static void PlayWhoosh() { EnsureBuilt(); Varied(whooshClip, 0.9f); soundedCasts++; }
    /// <summary>Bowstring release.</summary>
    public static void PlayTwang() { EnsureBuilt(); Varied(twangClip, 0.8f); soundedCasts++; }
    public static void PlayBlast() { EnsureBuilt(); Varied(blastClip, 1f); soundedBlasts++; }
    public static void PlayCrit() { EnsureBuilt(); Varied(critClip, 0.9f); soundedCrits++; }
    public static void PlayLevelUp() { EnsureBuilt(); source.PlayOneShot(levelUpClip, 0.9f); soundedLevelUps++; }

    /// <summary>
    /// Repeats must not grate: a pooled pitched source per call (±8 %).
    /// PlayOneShot cannot pitch, so rotate through a few extra sources.
    /// </summary>
    static void Varied(AudioClip clip, float volume)
    {
        var s = pitched[pitchedNext];
        pitchedNext = (pitchedNext + 1) % pitched.Length;
        s.pitch = 0.92f + (float)pitchRng.NextDouble() * 0.16f;
        s.PlayOneShot(clip, volume);
    }

    static AudioSource[] pitched;
    static int pitchedNext;

    /// <summary>Reset counters (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        soundedSwings = 0;
        soundedHits = 0;
        soundedDeaths = 0;
        soundedLoot = 0;
        soundedCasts = 0;
        soundedBlasts = 0;
        soundedCrits = 0;
        soundedLevelUps = 0;
    }

    static void EnsureBuilt()
    {
        if (built) return;
        built = true;
        var go = new GameObject("DiabloAudio");
        if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        var rng = new System.Random(1234); // deterministic clips
        swingClip = NoiseBurst("Swing", 0.12f, rng);
        hitClip = Thump("Hit", 0.15f, 180f, 90f);
        deathClip = Rattle("Death", 0.4f, 300f, 60f, rng);
        lootClip = Sweep("Loot", 0.09f, 880f, 1320f);
        uiClickClip = Sweep("UIClick", 0.05f, 1200f, 1200f);
        hurtClip = Thump("Hurt", 0.2f, 150f, 100f);
        playerDeathClip = Sweep("PlayerDeath", 0.6f, 400f, 70f);
        whooshClip = Whoosh("Whoosh", 0.38f, rng);
        blastClip = Boom("Blast", 0.7f, rng);
        twangClip = Pluck("Twang", 0.28f, 196f, rng);
        critClip = Crit("Crit", 0.18f);
        levelUpClip = Sting("LevelUp");
        ambientClip = Ambient("MoorWind", 6f, rng);
        pitched = new AudioSource[4];
        for (int i = 0; i < pitched.Length; i++) pitched[i] = go.AddComponent<AudioSource>();
    }

    /// <summary>Noise through a one-pole lowpass whose cutoff rises then falls.</summary>
    static AudioClip Whoosh(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float a = Mathf.Lerp(0.02f, 0.35f, Mathf.Sin(Mathf.PI * t));
            lp += a * ((float)(rng.NextDouble() * 2.0 - 1.0) - lp);
            data[i] = lp * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f)) * 1.4f;
        }
        return Bake(name, data);
    }

    /// <summary>Falling sub thump + lowpassed rumble, exponential tail.</summary>
    static AudioClip Boom(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        float lp = 0f, phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            phase += 2f * Mathf.PI * Mathf.Lerp(80f, 32f, t) / SampleRate;
            lp += 0.06f * ((float)(rng.NextDouble() * 2.0 - 1.0) - lp);
            float env = Mathf.Exp(-5f * t);
            data[i] = (Mathf.Sin(phase) * 0.7f + lp * 2.2f) * env * 0.8f;
        }
        return Bake(name, data);
    }

    /// <summary>Plucked string: fundamental + octave, noise attack, fast decay.</summary>
    static AudioClip Pluck(string name, float seconds, float f, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float time = i / (float)SampleRate;
            float t = (float)i / n;
            float tone = Mathf.Sin(2f * Mathf.PI * f * time) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 2.01f * time)
                + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 3.02f * time);
            float attack = t < 0.04f ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t / 0.04f) : 0f;
            data[i] = (tone * 0.35f + attack * 0.5f) * Mathf.Exp(-9f * t);
        }
        return Bake(name, data);
    }

    /// <summary>Heavy thump with a bright metallic ping on top.</summary>
    static AudioClip Crit(string name, float seconds)
    {
        int n = Samples(seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float time = i / (float)SampleRate;
            float t = (float)i / n;
            float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 70f, t) * time) * (1f - t) * (1f - t);
            float ping = Mathf.Sin(2f * Mathf.PI * 1760f * time) * Mathf.Exp(-14f * t);
            data[i] = thump * 0.7f + ping * 0.3f;
        }
        return Bake(name, data);
    }

    /// <summary>Three ascending notes of a major triad, then the octave held.</summary>
    static AudioClip Sting(string name)
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        const float step = 0.12f;
        float seconds = step * 3f + 0.6f;
        int n = Samples(seconds);
        var data = new float[n];
        for (int k = 0; k < notes.Length; k++)
        {
            int start = Samples(step * k);
            float len = k == notes.Length - 1 ? 0.6f : step * 1.6f;
            int count = Mathf.Min(Samples(len), n - start);
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)SampleRate;
                float t = (float)i / count;
                float tone = Mathf.Sin(2f * Mathf.PI * notes[k] * time)
                    + 0.3f * Mathf.Sin(2f * Mathf.PI * notes[k] * 2f * time);
                data[start + i] += tone * Mathf.Exp(-3f * t) * 0.22f;
            }
        }
        return Bake(name, data);
    }

    /// <summary>
    /// Room tone for the moor: brown noise with slow gusts plus a 55 Hz drone.
    /// Every periodic term completes whole cycles in the clip and the noise
    /// tail is crossfaded into the head, so the loop has no seam.
    /// </summary>
    static AudioClip Ambient(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        float brown = 0f;
        for (int i = 0; i < n; i++)
        {
            brown = Mathf.Clamp(brown + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.04f, -1f, 1f) * 0.998f;
            data[i] = brown;
        }
        int fade = Samples(0.8f);
        for (int i = 0; i < fade; i++)
        {
            float w = (float)i / fade;
            data[i] = data[i] * w + data[n - fade + i] * (1f - w);
        }
        var loop = new float[n - fade];
        for (int i = 0; i < loop.Length; i++)
        {
            float time = i / (float)SampleRate;
            float cycle = (float)i / loop.Length; // 0..1 across the loop
            float gust = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * cycle * 2f) * Mathf.Sin(2f * Mathf.PI * cycle);
            float drone = Mathf.Sin(2f * Mathf.PI * 55f * time) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * cycle));
            loop[i] = data[i] * gust * 0.9f + drone * 0.12f;
        }
        var clip = AudioClip.Create(name, loop.Length, 1, SampleRate, false);
        clip.SetData(loop, 0);
        return clip;
    }

    static int Samples(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));

    static AudioClip NoiseBurst(string name, float seconds, System.Random rng)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            data[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t) * 0.6f;
        }
        return Bake(name, data);
    }

    static AudioClip Thump(string name, float seconds, float f0, float f1)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SampleRate))
                * (1f - t) * (1f - t) * 0.8f;
        }
        return Bake(name, data);
    }

    static AudioClip Rattle(string name, float seconds, float f0, float f1, System.Random rng)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            float tone = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SampleRate));
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.4f;
            data[i] = (tone + noise) * (1f - t) * 0.5f;
        }
        return Bake(name, data);
    }

    static AudioClip Sweep(string name, float seconds, float f0, float f1)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SampleRate))
                * Mathf.Sin(Mathf.PI * t) * 0.5f;
        }
        return Bake(name, data);
    }

    static AudioClip Bake(string name, float[] data)
    {
        // 5 ms fade at both edges: raw edges click.
        int edge = Mathf.Min(data.Length / 2, Samples(0.005f));
        for (int i = 0; i < edge; i++)
        {
            float w = (float)i / edge;
            data[i] *= w;
            data[data.Length - 1 - i] *= w;
        }
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
