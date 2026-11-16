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

    static AudioSource source;
    static AudioClip swingClip, hitClip, deathClip, lootClip, uiClickClip, hurtClip, playerDeathClip;
    static bool built;

    public static AudioClip SwingClip { get { EnsureBuilt(); return swingClip; } }
    public static AudioClip HitClip { get { EnsureBuilt(); return hitClip; } }
    public static AudioClip DeathClip { get { EnsureBuilt(); return deathClip; } }
    public static AudioClip LootClip { get { EnsureBuilt(); return lootClip; } }
    public static AudioClip UIClickClip { get { EnsureBuilt(); return uiClickClip; } }
    public static AudioClip HurtClip { get { EnsureBuilt(); return hurtClip; } }
    public static AudioClip PlayerDeathClip { get { EnsureBuilt(); return playerDeathClip; } }

    public static void PlaySwing() { EnsureBuilt(); source.PlayOneShot(swingClip); soundedSwings++; }
    public static void PlayHit() { EnsureBuilt(); source.PlayOneShot(hitClip); soundedHits++; }
    public static void PlayDeath() { EnsureBuilt(); source.PlayOneShot(deathClip); soundedDeaths++; }
    public static void PlayLoot() { EnsureBuilt(); source.PlayOneShot(lootClip); soundedLoot++; }
    public static void PlayUIClick() { EnsureBuilt(); source.PlayOneShot(uiClickClip); }
    public static void PlayHurt() { EnsureBuilt(); source.PlayOneShot(hurtClip); }
    public static void PlayPlayerDeath() { EnsureBuilt(); source.PlayOneShot(playerDeathClip); }

    /// <summary>Reset counters (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        soundedSwings = 0;
        soundedHits = 0;
        soundedDeaths = 0;
        soundedLoot = 0;
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
    }

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
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
