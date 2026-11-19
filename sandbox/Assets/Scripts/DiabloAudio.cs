using System;
using UnityEngine;

/// <summary>
/// D2 combat sounds, fully synthesized at runtime (Ashen Fen has no audio
/// pipeline and the repo bans binary assets). 22050 Hz mono clips built once
/// from decaying sine/noise shapes; playback is a silent no-op headless.
/// Static counters (strikeCount precedent) prove sounds fired in tests.
/// Play methods never log, so LogAssert suites stay clean.
/// </summary>
public static class DiabloAudio
{
    public const int SampleRate = 22050;

    /// <summary>S2 cue catalogue (>=20 SFX). Each maps to a Play* method.</summary>
    public enum Cue
    {
        Swing, Hit, Death, Loot, UIClick, Hurt, PlayerDeath,
        Whoosh, Blast, Twang, Crit, LevelUp, Ambient,
        Lantern, Halo, Veil,
        SwordHit1, SwordHit2, SwordHit3,
        Fireball, Arrow, Cleave,
        HuskGrowl, WardenTaunt, BossRoar,
        Gold, RareDrop, VendorBuy, VendorSell, Gate,
    }

    public static int soundedSwings;
    public static int soundedHits;
    public static int soundedDeaths;
    public static int soundedLoot;
    public static int soundedCasts;
    public static int soundedBlasts;
    public static int soundedCrits;
    public static int soundedLevelUps;
    public static int soundedSwordHits;
    public static int soundedSkillShots;
    public static int soundedEnemyVox;
    public static int soundedGold;
    public static int soundedRare;
    public static int soundedVendor;
    public static int soundedGate;
    /// <summary>Synth-fallback hits: asset override missing, synth clip used.</summary>
    public static int soundedFallbacks;
    public static string lastFallbackCue;

    /// <summary>Menu base volumes (presentation tuning): UI sits under combat, vendor under loot.</summary>
    public const float UIClickVolume = 0.35f;
    public const float VendorVolume = 0.5f;

    /// <summary>When true, Play* methods bump counters and resolve clips but
    /// never touch the audio device. Tests set this so suites stay silent;
    /// batchmode runs are silent automatically (see PlaybackMuted).</summary>
    public static bool MutePlayback { get; set; }

    static bool PlaybackMuted
    {
        get
        {
            if (MutePlayback) return true;
            try { return Application.isBatchMode; }
            catch { return false; }
        }
    }

    static AudioSource source;
    static AudioClip swingClip, hitClip, deathClip, lootClip, uiClickClip, hurtClip, playerDeathClip;
    static AudioClip whooshClip, blastClip, twangClip, critClip, levelUpClip, ambientClip;
    static AudioClip lanternClip, haloClip, veilClip;
    static AudioClip swordHit1Clip, swordHit2Clip, swordHit3Clip;
    static AudioClip fireballClip, arrowClip, cleaveClip;
    static AudioClip huskGrowlClip, wardenTauntClip, bossRoarClip;
    static AudioClip goldClip, rareDropClip, vendorBuyClip, vendorSellClip, gateClip;
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

    static readonly System.Collections.Generic.Dictionary<string, AudioClip> overrides =
        new System.Collections.Generic.Dictionary<string, AudioClip>();

    /// <summary>Test seam: prefer a generated .ogg clip when present.</summary>
    public static void RegisterOverride(string cueName, AudioClip clip)
    {
        overrides[cueName] = clip;
    }
    public static void ClearOverrides() { overrides.Clear(); }

    /// <summary>Asset clip if registered, else synth + fallback counter.</summary>
    static AudioClip Resolve(string cueName, AudioClip synth)
    {
        AudioClip clip;
        if (overrides.TryGetValue(cueName, out clip) && clip != null) return clip;
        soundedFallbacks++;
        lastFallbackCue = cueName;
        return synth;
    }

    /// <summary>SFX gain layer via DiabloSettings (mixer authoring is a TODO
    /// for the human session: create CinderreachMixer with Master/Music/SFX/UI
    /// groups per craft-game-floors game-audio headless floor, then route the
    /// sources below via AudioSource.outputAudioMixerGroup).</summary>
    public static float SfxGain => DiabloSettings.EffectiveSFX;
    public static float MusicGain => DiabloSettings.MasterVol * DiabloSettings.MusicVol;

    static void PlaySfx(AudioClip synth, string cueName, float volume)
    {
        EnsureBuilt();
        var clip = Resolve(cueName, synth);
        if (PlaybackMuted) return;
        try { source.PlayOneShot(clip, volume * SfxGain); }
        catch { soundedFallbacks++; }
    }

    static void PlayRaw(AudioClip clip)
    {
        if (PlaybackMuted) return;
        try { source.PlayOneShot(clip); } catch { }
    }

    public static void PlaySwing() { EnsureBuilt(); soundedSwings++; PlayRaw(swingClip); }
    public static void PlayHit() { EnsureBuilt(); soundedHits++; PlayRaw(hitClip); }
    public static void PlayDeath() { EnsureBuilt(); soundedDeaths++; PlayRaw(deathClip); }
    public static void PlayLoot() { EnsureBuilt(); soundedLoot++; PlayRaw(lootClip); }
    /// <summary>Menu tick: deep + quiet (presentation tuning) — routes gain + fallback like all SFX.</summary>
    public static void PlayUIClick() { EnsureBuilt(); PlaySfx(uiClickClip, "UIClick", UIClickVolume); }
    public static void PlayHurt() { EnsureBuilt(); PlayRaw(hurtClip); }
    public static void PlayPlayerDeath() { EnsureBuilt(); PlayRaw(playerDeathClip); }
    /// <summary>Fireball leaving the hand.</summary>
    public static void PlayWhoosh() { EnsureBuilt(); Varied(whooshClip, 0.9f); soundedCasts++; }
    /// <summary>Bowstring release.</summary>
    public static void PlayTwang() { EnsureBuilt(); Varied(twangClip, 0.8f); soundedCasts++; }
    public static void PlayBlast() { EnsureBuilt(); Varied(blastClip, 1f); soundedBlasts++; }
    public static void PlayCrit() { EnsureBuilt(); Varied(critClip, 0.9f); soundedCrits++; }
    // Lanternbearer (second-class S2): bell chime, rising hum, falling shimmer.
    public static void PlayLantern() { EnsureBuilt(); Varied(lanternClip, 0.7f); soundedCasts++; }
    public static void PlayHalo() { EnsureBuilt(); Varied(haloClip, 0.8f); soundedCasts++; }
    public static void PlayVeil() { EnsureBuilt(); Varied(veilClip, 0.6f); soundedCasts++; }
    public static AudioClip LanternClip { get { EnsureBuilt(); return lanternClip; } }
    public static AudioClip HaloClip { get { EnsureBuilt(); return haloClip; } }
    public static AudioClip VeilClip { get { EnsureBuilt(); return veilClip; } }
    public static AudioClip SwordHit1Clip { get { EnsureBuilt(); return swordHit1Clip; } }
    public static AudioClip SwordHit2Clip { get { EnsureBuilt(); return swordHit2Clip; } }
    public static AudioClip SwordHit3Clip { get { EnsureBuilt(); return swordHit3Clip; } }
    public static AudioClip FireballClip { get { EnsureBuilt(); return fireballClip; } }
    public static AudioClip ArrowClip { get { EnsureBuilt(); return arrowClip; } }
    public static AudioClip CleaveClip { get { EnsureBuilt(); return cleaveClip; } }
    public static AudioClip HuskGrowlClip { get { EnsureBuilt(); return huskGrowlClip; } }
    public static AudioClip WardenTauntClip { get { EnsureBuilt(); return wardenTauntClip; } }
    public static AudioClip BossRoarClip { get { EnsureBuilt(); return bossRoarClip; } }
    public static AudioClip GoldClip { get { EnsureBuilt(); return goldClip; } }
    public static AudioClip RareDropClip { get { EnsureBuilt(); return rareDropClip; } }
    public static AudioClip VendorBuyClip { get { EnsureBuilt(); return vendorBuyClip; } }
    public static AudioClip VendorSellClip { get { EnsureBuilt(); return vendorSellClip; } }
    public static AudioClip GateClip { get { EnsureBuilt(); return gateClip; } }

    /// <summary>Generic cue entry point (counts toward the >=20 catalogue).</summary>
    public static void PlayCue(Cue cue)
    {
        switch (cue)
        {
            case Cue.Swing: PlaySwing(); break;
            case Cue.Hit: PlayHit(); break;
            case Cue.Death: PlayDeath(); break;
            case Cue.Loot: PlayLoot(); break;
            case Cue.UIClick: PlayUIClick(); break;
            case Cue.Hurt: PlayHurt(); break;
            case Cue.PlayerDeath: PlayPlayerDeath(); break;
            case Cue.Whoosh: PlayWhoosh(); break;
            case Cue.Blast: PlayBlast(); break;
            case Cue.Twang: PlayTwang(); break;
            case Cue.Crit: PlayCrit(); break;
            case Cue.LevelUp: PlayLevelUp(); break;
            case Cue.Lantern: PlayLantern(); break;
            case Cue.Halo: PlayHalo(); break;
            case Cue.Veil: PlayVeil(); break;
            case Cue.SwordHit1: PlaySwordHit1(); break;
            case Cue.SwordHit2: PlaySwordHit2(); break;
            case Cue.SwordHit3: PlaySwordHit3(); break;
            case Cue.Fireball: PlayFireball(); break;
            case Cue.Arrow: PlayArrow(); break;
            case Cue.Cleave: PlayCleave(); break;
            case Cue.HuskGrowl: PlayHuskGrowl(); break;
            case Cue.WardenTaunt: PlayWardenTaunt(); break;
            case Cue.BossRoar: PlayBossRoar(); break;
            case Cue.Gold: PlayGold(); break;
            case Cue.RareDrop: PlayRareDrop(); break;
            case Cue.VendorBuy: PlayVendorBuy(); break;
            case Cue.VendorSell: PlayVendorSell(); break;
            case Cue.Gate: PlayGate(); break;
            default: PlayHit(); break;
        }
    }

    public static void PlaySwordHit1() { EnsureBuilt(); Varied(Resolve("SwordHit1", swordHit1Clip), 0.9f * SfxGain); soundedSwordHits++; soundedHits++; }
    public static void PlaySwordHit2() { EnsureBuilt(); Varied(Resolve("SwordHit2", swordHit2Clip), 0.9f * SfxGain); soundedSwordHits++; soundedHits++; }
    public static void PlaySwordHit3() { EnsureBuilt(); Varied(Resolve("SwordHit3", swordHit3Clip), 0.9f * SfxGain); soundedSwordHits++; soundedHits++; }
    public static void PlayFireball() { EnsureBuilt(); Varied(Resolve("Fireball", fireballClip), 0.9f * SfxGain); soundedSkillShots++; soundedCasts++; }
    public static void PlayArrow() { EnsureBuilt(); Varied(Resolve("Arrow", arrowClip), 0.8f * SfxGain); soundedSkillShots++; soundedCasts++; }
    public static void PlayCleave() { EnsureBuilt(); Varied(Resolve("Cleave", cleaveClip), 1f * SfxGain); soundedSkillShots++; soundedCasts++; }
    public static void PlayHuskGrowl() { EnsureBuilt(); Varied(Resolve("HuskGrowl", huskGrowlClip), 0.7f * SfxGain); soundedEnemyVox++; }
    public static void PlayWardenTaunt() { EnsureBuilt(); Varied(Resolve("WardenTaunt", wardenTauntClip), 0.8f * SfxGain); soundedEnemyVox++; }
    public static void PlayBossRoar() { EnsureBuilt(); Varied(Resolve("BossRoar", bossRoarClip), 1f * SfxGain); soundedEnemyVox++; }
    public static void PlayGold() { EnsureBuilt(); PlaySfx(goldClip, "Gold", 0.8f); soundedGold++; soundedLoot++; }
    public static void PlayRareDrop() { EnsureBuilt(); PlaySfx(rareDropClip, "RareDrop", 0.9f); soundedRare++; soundedLoot++; }
    public static void PlayVendorBuy() { EnsureBuilt(); PlaySfx(vendorBuyClip, "VendorBuy", VendorVolume); soundedVendor++; }
    public static void PlayVendorSell() { EnsureBuilt(); PlaySfx(vendorSellClip, "VendorSell", VendorVolume); soundedVendor++; }
    public static void PlayGate() { EnsureBuilt(); PlaySfx(gateClip, "Gate", 1f); soundedGate++; }

    public static void PlayLevelUp() { EnsureBuilt(); soundedLevelUps++; PlayRawWithVolume(levelUpClip, 0.9f); }

    static void PlayRawWithVolume(AudioClip clip, float volume)
    {
        if (PlaybackMuted) return;
        try { source.PlayOneShot(clip, volume); } catch { }
    }

    /// <summary>
    /// Repeats must not grate: a pooled pitched source per call (±8 %).
    /// PlayOneShot cannot pitch, so rotate through a few extra sources.
    /// </summary>
    static void Varied(AudioClip clip, float volume)
    {
        if (PlaybackMuted) return;
        var s = pitched[pitchedNext];
        pitchedNext = (pitchedNext + 1) % pitched.Length;
        s.pitch = 0.92f + (float)pitchRng.NextDouble() * 0.16f;
        try { s.PlayOneShot(clip, volume * SfxGain); } catch { }
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
        soundedSwordHits = 0;
        soundedSkillShots = 0;
        soundedEnemyVox = 0;
        soundedGold = 0;
        soundedRare = 0;
        soundedVendor = 0;
        soundedGate = 0;
        soundedFallbacks = 0;
        lastFallbackCue = null;
    }

    static void EnsureBuilt()
    {
        // Scene opens destroy the synth objects (no DontDestroyOnLoad outside
        // play mode); rebuild instead of handing out dead clips.
        if (built && source != null && swingClip != null) return;
        built = true;
        var go = new GameObject("DiabloAudio");
        if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        var rng = new System.Random(1234); // deterministic clips
        swingClip = NoiseBurst("Swing", 0.12f, rng);
        hitClip = Thump("Hit", 0.15f, 180f, 90f);
        deathClip = Rattle("Death", 0.4f, 300f, 60f, rng);
        lootClip = Sweep("Loot", 0.09f, 880f, 1320f);
        uiClickClip = Sweep("UIClick", 0.07f, 520f, 380f);
        hurtClip = Thump("Hurt", 0.2f, 150f, 100f);
        playerDeathClip = Sweep("PlayerDeath", 0.6f, 400f, 70f);
        whooshClip = Whoosh("Whoosh", 0.38f, rng);
        blastClip = Boom("Blast", 0.7f, rng);
        twangClip = Pluck("Twang", 0.28f, 196f, rng);
        critClip = Crit("Crit", 0.18f);
        levelUpClip = Sting("LevelUp");
        ambientClip = Ambient("MoorWind", 6f, rng);
        swordHit1Clip = Crit("SwordHit1", 0.14f);
        swordHit2Clip = Crit("SwordHit2", 0.16f);
        swordHit3Clip = Crit("SwordHit3", 0.2f);
        fireballClip = Whoosh("Fireball", 0.42f, rng);
        arrowClip = Sweep("Arrow", 0.12f, 2400f, 900f);
        cleaveClip = NoiseBurst("Cleave", 0.22f, rng);
        huskGrowlClip = Rattle("HuskGrowl", 0.5f, 140f, 70f, rng);
        wardenTauntClip = Sweep("WardenTaunt", 0.6f, 110f, 330f);
        bossRoarClip = Boom("BossRoar", 1.1f, rng);
        goldClip = Sweep("Gold", 0.12f, 1568f, 2093f);
        rareDropClip = Sting("RareDrop");
        vendorBuyClip = Sweep("VendorBuy", 0.18f, 330f, 495f);
        vendorSellClip = Sweep("VendorSell", 0.18f, 495f, 330f);
        gateClip = Sweep("Gate", 0.8f, 180f, 720f);
        lanternClip = Chime("LanternBolt", 0.45f, 660f);
        haloClip = Sweep("SearingHalo", 0.4f, 220f, 880f);
        veilClip = Sweep("VeilStep", 0.25f, 1400f, 480f);
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

    /// <summary>Bell: inharmonic partials (1, 2.76, 5.4) with exponential decay.</summary>
    static AudioClip Chime(string name, float seconds, float f0)
    {
        int n = Samples(seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float env = Mathf.Exp(-t * 9f);
            data[i] = env * (0.6f * Mathf.Sin(2f * Mathf.PI * f0 * t)
                             + 0.25f * Mathf.Sin(2f * Mathf.PI * f0 * 2.76f * t)
                             + 0.12f * Mathf.Sin(2f * Mathf.PI * f0 * 5.4f * t));
        }
        return Bake(name, data);
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
