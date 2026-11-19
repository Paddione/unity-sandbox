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
