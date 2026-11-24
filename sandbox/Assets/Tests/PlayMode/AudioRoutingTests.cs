using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>S2 audio routing: every cue fires a clip through the DiabloSettings
/// gain layer; missing asset clips fall back to synth with a counter.
/// No LogAssert: DiabloAudio never logs.</summary>
public class AudioRoutingTests
{
    [SetUp]
    public void Setup()
    {
        DiabloAudio.MutePlayback = true;
        DiabloAudio.ResetRun();
        DiabloAudio.ClearOverrides();
        DiabloSettings.MasterVol = 0.8f;
        DiabloSettings.MusicVol = 0.8f;
        DiabloSettings.SFXVol = 0.8f;
    }

    [TearDown]
    public void TearDown()
    {
        DiabloAudio.ClearOverrides();
        DiabloAudio.ResetRun();
        var md = Object.FindFirstObjectByType<MusicDirector>();
        if (md != null) Object.DestroyImmediate(md.gameObject);
    }

    [Test]
    public void AllNewCues_ResolveSynthClips_AndCount()
    {
        DiabloAudio.PlaySwordHit1();
        DiabloAudio.PlaySwordHit2();
        DiabloAudio.PlaySwordHit3();
        DiabloAudio.PlayFireball();
        DiabloAudio.PlayArrow();
        DiabloAudio.PlayCleave();
        DiabloAudio.PlayHuskGrowl();
        DiabloAudio.PlayWardenTaunt();
        DiabloAudio.PlayBossRoar();
        DiabloAudio.PlayGold();
        DiabloAudio.PlayRareDrop();
        DiabloAudio.PlayVendorBuy();
        DiabloAudio.PlayVendorSell();
        DiabloAudio.PlayGate();

        Assert.AreEqual(3, DiabloAudio.soundedSwordHits);
        Assert.AreEqual(3, DiabloAudio.soundedSkillShots, "Fireball/Arrow/Cleave counted.");
        Assert.AreEqual(3, DiabloAudio.soundedEnemyVox);
        Assert.AreEqual(1, DiabloAudio.soundedGold);
        Assert.AreEqual(1, DiabloAudio.soundedRare);
        Assert.AreEqual(2, DiabloAudio.soundedVendor);
        Assert.AreEqual(1, DiabloAudio.soundedGate);

        foreach (var clip in new[]
        {
            DiabloAudio.SwordHit1Clip, DiabloAudio.SwordHit2Clip, DiabloAudio.SwordHit3Clip,
            DiabloAudio.FireballClip, DiabloAudio.ArrowClip, DiabloAudio.CleaveClip,
            DiabloAudio.HuskGrowlClip, DiabloAudio.WardenTauntClip, DiabloAudio.BossRoarClip,
            DiabloAudio.GoldClip, DiabloAudio.RareDropClip, DiabloAudio.VendorBuyClip,
            DiabloAudio.VendorSellClip, DiabloAudio.GateClip,
        })
            Assert.IsNotNull(clip, "Every S2 cue must resolve a synth clip.");
    }

    [Test]
    public void MissingAssetClip_UsesSynthFallbackCounter()
    {
        int before = DiabloAudio.soundedFallbacks;
        DiabloAudio.PlayGold(); // no override registered -> synth fallback
        Assert.Greater(DiabloAudio.soundedFallbacks, before, "Missing clip must bump the fallback counter.");
        Assert.AreEqual("Gold", DiabloAudio.lastFallbackCue);
    }

    [Test]
    public void RegisteredOverride_SuppressesFallback()
    {
        var clip = AudioClip.Create("TestGold", 128, 1, DiabloAudio.SampleRate, false);
        int before = DiabloAudio.soundedFallbacks;
        try
        {
            DiabloAudio.RegisterOverride("Gold", clip);
            DiabloAudio.PlayGold();
            Assert.AreEqual(before, DiabloAudio.soundedFallbacks, "Registered asset clip must not count as fallback.");
        }
        finally
        {
            Object.DestroyImmediate(clip);
        }
    }

    [Test]
    public void SfxGain_MatchesSettingsLayer()
    {
        DiabloSettings.MasterVol = 0.5f;
        DiabloSettings.SFXVol = 0.5f;
        Assert.AreEqual(0.25f, DiabloAudio.SfxGain, 1e-5f, "Gain layer must be master x sfx (mixer TODO until authored).");
        Assert.AreEqual(DiabloSettings.EffectiveSFX, DiabloAudio.SfxGain, 1e-6f);
    }

    [UnityTest]
    public IEnumerator MusicDirector_CrossfadesZoneToBoss()
    {
        var md = MusicDirector.Ensure();
        yield return null;
        md.PlayZone(1);
        yield return null;
        Assert.AreEqual("Zone1", md.CurrentTrack);
        int fades = md.crossfadesStarted;
        md.PlayBoss();
        yield return null;
        Assert.AreEqual("Boss", md.CurrentTrack);
        Assert.Greater(md.crossfadesStarted, fades, "Zone->boss must start a crossfade.");
        md.PlayMenu();
        yield return null;
        Assert.AreEqual("Menu", md.CurrentTrack);
    }

    [Test]
    public void NewEventCues_ResolveAndCount_AndAmbientIsNoHit()
    {
        DiabloAudio.PlayHeal();
        DiabloAudio.PlayQuest();
        DiabloAudio.PlayShrine();
        DiabloAudio.PlayBlock();
        DiabloAudio.PlayFootstep();
        Assert.AreEqual(1, DiabloAudio.soundedHeals);
        Assert.AreEqual(1, DiabloAudio.soundedQuests);
        Assert.AreEqual(1, DiabloAudio.soundedShrines);
        Assert.AreEqual(1, DiabloAudio.soundedBlocks);
        Assert.AreEqual(1, DiabloAudio.soundedSteps);
        foreach (var clip in new[]
        {
            DiabloAudio.HealClip, DiabloAudio.QuestClip, DiabloAudio.ShrineClip,
            DiabloAudio.BlockClip, DiabloAudio.FootstepClip,
        })
            Assert.IsNotNull(clip, "Every event cue must resolve a synth clip.");
        int hits = DiabloAudio.soundedHits;
        DiabloAudio.PlayCue(DiabloAudio.Cue.Ambient);
        Assert.AreEqual(hits, DiabloAudio.soundedHits, "Ambient bed must never fall through to Hit.");
        DiabloAudio.PlayCue(DiabloAudio.Cue.Heal);
        Assert.AreEqual(2, DiabloAudio.soundedHeals, "PlayCue must route Heal.");
    }

    [Test]
    public void EveryZone_HasItsOwnTrack_AndKilnDiffersFromOssuary()
    {
        Assert.AreEqual("Zone1", MusicDirector.ZoneTrack(ActProgress.Town));
        Assert.AreEqual("Zone2", MusicDirector.ZoneTrack(ActProgress.Ossuary));
        Assert.AreEqual("Zone3", MusicDirector.ZoneTrack(ActProgress.Kiln));
        Assert.AreEqual("Zone4", MusicDirector.ZoneTrack(ActProgress.Barrens));
        var md = MusicDirector.Ensure();
        var kiln = md.TrackClip("Zone3");
        var ossuary = md.TrackClip("Zone2");
        Assert.IsNotNull(kiln);
        Assert.AreEqual("KilnLoop", kiln.name);
        Assert.AreNotEqual(ossuary.name, kiln.name, "Kiln must not share the Ossuary theme.");
    }

    [UnityTest]
    public System.Collections.IEnumerator PlayingAudio_EnsuresAudioListenerOnMainCamera()
    {
        var camGo = new GameObject("ListenerTestCamera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        var fallback = Object.FindFirstObjectByType<AudioListener>();
        var fallbackGo = fallback != null ? fallback.gameObject : null;
        if (fallbackGo != null) fallbackGo.SetActive(false);
        Assert.IsNull(Object.FindFirstObjectByType<AudioListener>(), "Precondition: no listener.");
        DiabloAudio.PlaySwing();
        yield return null;
        var ensured = camGo.GetComponent<AudioListener>();
        if (fallbackGo != null) fallbackGo.SetActive(true);
        Assert.IsNotNull(ensured,
            "Without an AudioListener every source stays silent; DiabloAudio must ensure one.");
        Object.Destroy(camGo);
    }

    [Test]
    public void AllSfxClips_AreWarmBalancedAndNonClipping()
    {
        var clips = new[]
        {
            DiabloAudio.SwingClip, DiabloAudio.HitClip, DiabloAudio.DeathClip,
            DiabloAudio.LootClip, DiabloAudio.UIClickClip, DiabloAudio.HurtClip,
            DiabloAudio.PlayerDeathClip, DiabloAudio.WhooshClip, DiabloAudio.BlastClip,
            DiabloAudio.TwangClip, DiabloAudio.CritClip, DiabloAudio.LevelUpClip,
            DiabloAudio.AmbientClip, DiabloAudio.LanternClip, DiabloAudio.HaloClip,
            DiabloAudio.VeilClip, DiabloAudio.SwordHit1Clip, DiabloAudio.SwordHit2Clip,
            DiabloAudio.SwordHit3Clip, DiabloAudio.FireballClip, DiabloAudio.ArrowClip,
            DiabloAudio.CleaveClip, DiabloAudio.HuskGrowlClip, DiabloAudio.WardenTauntClip,
            DiabloAudio.BossRoarClip, DiabloAudio.GoldClip, DiabloAudio.RareDropClip,
            DiabloAudio.VendorBuyClip, DiabloAudio.VendorSellClip, DiabloAudio.GateClip,
            DiabloAudio.HealClip, DiabloAudio.QuestClip, DiabloAudio.ShrineClip,
            DiabloAudio.BlockClip, DiabloAudio.FootstepClip,
        };
        float lpAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 2000f / DiabloAudio.SampleRate);
        foreach (var clip in clips)
        {
            Assert.IsNotNull(clip);
            Assert.Greater(clip.samples, 0);
            Assert.AreEqual(DiabloAudio.SampleRate, clip.frequency);
            Assert.AreEqual(1, clip.channels);
            var data = new float[clip.samples];
            Assert.IsTrue(clip.GetData(data, 0));
            float peak = 0f;
            double totalE = 0.0, highE = 0.0;
            float lp = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float x = data[i];
                float ax = Mathf.Abs(x);
                if (ax > peak) peak = ax;
                lp += lpAlpha * (x - lp);
                float hp = x - lp;
                totalE += x * x;
                highE += hp * hp;
            }
            Assert.Greater(peak, 0.05f, $"{clip.name} peak too quiet: {peak}");
            Assert.LessOrEqual(peak, 0.95f, $"{clip.name} clips: {peak}");
            double hfFrac = totalE > 1e-9 ? highE / totalE : 0.0;
            Assert.Less(hfFrac, 0.35, $"{clip.name} too harsh/high-pitched (HF>2kHz={hfFrac:F3})");
        }
    }

    [Test]
    public void AllMusicTracks_HaveMelodicMovementWarmSpectrumAndSeamlessLoop()
    {
        var md = MusicDirector.Ensure();
        var tracks = new[]
        {
            "Menu", "Zone0", "Zone1", "Zone2", "Zone3", "Boss", "Zone4", MusicDirector.GlassBossTrack,
        };
        float lpAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 1800f / DiabloAudio.SampleRate);
        foreach (var track in tracks)
        {
            var clip = md.TrackClip(track);
            Assert.IsNotNull(clip, $"Missing clip for {track}");
            Assert.GreaterOrEqual(clip.length, 15.9f, $"{track} should be a full 16s musical loop");
            var data = new float[clip.samples];
            Assert.IsTrue(clip.GetData(data, 0));

            float peak = 0f;
            double totalE = 0.0, highE = 0.0;
            float lp = 0f;
            const int windows = 16;
            int winLen = data.Length / windows;
            var winRms = new double[windows];
            var winZc = new int[windows];
            for (int i = 0; i < data.Length; i++)
            {
                float x = data[i];
                float ax = Mathf.Abs(x);
                if (ax > peak) peak = ax;
                lp += lpAlpha * (x - lp);
                float hp = x - lp;
                totalE += x * x;
                highE += hp * hp;
                int w = Mathf.Min(windows - 1, i / winLen);
                winRms[w] += x * x;
                if (i > 0 && ((data[i - 1] < 0f && x >= 0f) || (data[i - 1] >= 0f && x < 0f)))
                    winZc[w]++;
            }
            Assert.Greater(peak, 0.05f, $"{track} silent");
            Assert.LessOrEqual(peak, 0.80f, $"{track} too loud for background: {peak}");
            double hfFrac = totalE > 1e-9 ? highE / totalE : 0.0;
            Assert.Less(hfFrac, 0.12, $"{track} has harsh >1.8kHz energy ({hfFrac:F3})");
            Assert.Less(Mathf.Abs(data[0] - data[data.Length - 1]), 0.05f,
                $"{track} loop seam discontinuity");

            double minRms = double.MaxValue, maxRms = 0.0;
            double minZcr = double.MaxValue, maxZcr = 0.0;
            for (int w = 0; w < windows; w++)
            {
                double rms = System.Math.Sqrt(winRms[w] / winLen);
                double zcr = (double)winZc[w] / winLen;
                if (rms < minRms) minRms = rms;
                if (rms > maxRms) maxRms = rms;
                if (zcr < minZcr) minZcr = zcr;
                if (zcr > maxZcr) maxZcr = zcr;
            }
            Assert.IsTrue((maxRms - minRms) > 0.01 || (maxZcr - minZcr) > 0.004,
                $"{track} lacks melodic movement (dRms={maxRms - minRms:F5}, dZcr={maxZcr - minZcr:F5})");
        }
    }

    [UnityTest]
    public IEnumerator MusicDirector_LiveVolumeTracksSettingsWhenIdle()
    {
        var md = MusicDirector.Ensure();
        md.PlayTown();
        while (md.crossfadeProgress < 1f) yield return null;
        DiabloSettings.MasterVol = 1f;
        DiabloSettings.MusicVol = 0.25f;
        yield return null;
        AudioSource active = null;
        foreach (var src in md.GetComponents<AudioSource>())
            if (src.clip != null && src.clip.name == "LanternHoldLoop") active = src;
        Assert.IsNotNull(active);
        Assert.AreEqual(0.25f, active.volume, 0.02f, "MusicDirector must track live MusicVol changes.");
    }

    [Test]
    public void LegacySignatures_StillCallable()
    {
        // Compile-time + headless smoke proof that no existing Play* broke.
        DiabloAudio.PlaySwing();
        DiabloAudio.PlayHit();
        DiabloAudio.PlayDeath();
        DiabloAudio.PlayLoot();
        DiabloAudio.PlayUIClick();
        DiabloAudio.PlayHurt();
        DiabloAudio.PlayPlayerDeath();
        DiabloAudio.PlayWhoosh();
        DiabloAudio.PlayTwang();
        DiabloAudio.PlayBlast();
        DiabloAudio.PlayCrit();
        DiabloAudio.PlayLantern();
        DiabloAudio.PlayHalo();
        DiabloAudio.PlayVeil();
        DiabloAudio.PlayLevelUp();
        Assert.GreaterOrEqual(DiabloAudio.soundedSwings, 1);
    }
}
