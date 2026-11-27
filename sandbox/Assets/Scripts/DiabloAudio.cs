using System;
using UnityEngine;

/// <summary>
/// Cinderreach combat & world SFX: resolves authored Addressable clips under
/// <c>Assets/AddressableAssets/Sfx/</c> (<c>Res.TryGet&lt;AudioClip&gt;</c>) with
/// automatic runtime synth fallback (22050 Hz mono clips built deterministically).
/// Playback is a silent no-op headless; static counters prove sounds fired in tests.
/// Play methods never log, so LogAssert suites stay clean.
/// </summary>
public static class DiabloAudio
{
    public const int SampleRate = 22050;
    public const string SfxAddressPrefix = "Sfx/";

    /// <summary>When false, skips Addressable SFX lookup and uses overrides/synth fallback.</summary>
    public static bool EnableAddressableSfx { get; set; } = true;

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
        Heal, Quest, Shrine, Block, Footstep,
        MireHook, BogCharge, MireGround, IronVow,
        SmokePot, ShadowRoll, VenomVial, PowderKeg,
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
    public static int soundedHeals;
    public static int soundedQuests;
    public static int soundedShrines;
    public static int soundedBlocks;
    public static int soundedSteps;
    /// <summary>Synth-fallback hits: asset override missing, synth clip used.</summary>
    public static int soundedFallbacks;
    public static string lastFallbackCue;

    public enum HitSurface { FleshBone, ArmorMetal, StoneAsh, Glass, GlassShield }
    public enum GroundSurface { Moor, Stone, Basalt, Sand }
    public static HitSurface lastHitSurface;
    public static GroundSurface lastGroundSurface;
    public static int lastVariantIndex;

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
    static AudioClip mireHookClip, bogChargeClip, mireGroundClip, ironVowClip;
    static AudioClip smokePotClip, shadowRollClip, venomVialClip, powderKegClip;
    static AudioClip swordHit1Clip, swordHit2Clip, swordHit3Clip;
    static AudioClip fireballClip, arrowClip, cleaveClip;
    static AudioClip huskGrowlClip, wardenTauntClip, bossRoarClip;
    static AudioClip goldClip, rareDropClip, vendorBuyClip, vendorSellClip, gateClip;
    static AudioClip healClip, questClip, shrineClip, blockClip, footstepClip;
    static AudioClip[] swingVariants;
    static AudioClip[] hitFleshBoneVariants;
    static AudioClip[] hitArmorMetalVariants;
    static AudioClip[] hitStoneAshVariants;
    static AudioClip[] hitGlassVariants;
    static AudioClip[] hitGlassShieldVariants;
    static AudioClip[] stepMoorVariants;
    static AudioClip[] stepStoneVariants;
    static AudioClip[] stepBasaltVariants;
    static AudioClip[] stepSandVariants;
    static int lastSwingIdx = -1, lastHitIdx = -1, lastStepIdx = -1;
    static readonly System.Random pitchRng = new System.Random(77);
    static readonly System.Random variantRng = new System.Random(313);
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
    /// <summary>Seamless moor wind + subtle sub floor loop (ambient bed synth clip).</summary>
    public static AudioClip AmbientClip { get { EnsureBuilt(); return ambientClip; } }

    /// <summary>Authored Addressable ambient bed if present, else runtime synth fallback.</summary>
    public static AudioClip ResolveAmbient() { EnsureBuilt(); return Resolve("Ambient", ambientClip); }

    static readonly System.Collections.Generic.Dictionary<string, AudioClip> overrides =
        new System.Collections.Generic.Dictionary<string, AudioClip>();

    /// <summary>Test seam: prefer a generated .ogg clip when present.</summary>
    public static void RegisterOverride(string cueName, AudioClip clip)
    {
        overrides[cueName] = clip;
    }
    public static void ClearOverrides() { overrides.Clear(); }

    /// <summary>Try test override first, then authored Addressable Sfx/&lt;key&gt; clip.</summary>
    static bool TryResolveKey(string key, out AudioClip clip)
    {
        if (overrides.TryGetValue(key, out clip) && clip != null) return true;
        if (EnableAddressableSfx && TryLoadSfxAsset(key, out clip)) return true;
        clip = null;
        return false;
    }

    static bool TryLoadSfxAsset(string key, out AudioClip clip)
    {
        clip = null;
        if (string.IsNullOrEmpty(key)) return false;
        try { return Res.TryGet(SfxAddressPrefix + key, out clip); }
        catch { return false; }
    }

    /// <summary>Asset clip if registered or authored in Addressables, else synth + fallback counter.</summary>
    static AudioClip Resolve(string cueName, AudioClip synth)
    {
        if (TryResolveKey(cueName, out var clip)) return clip;
        soundedFallbacks++;
        lastFallbackCue = cueName;
        return synth;
    }

    /// <summary>
    /// Variant/surface-aware SFX resolution: checks surface+variant, surface,
    /// variant, and base cue across overrides and Addressables before falling back to synth.
    /// </summary>
    static AudioClip ResolveVariant(string baseCue, string surfaceTag, int variantIdx, AudioClip synth)
    {
        AudioClip clip;
        if (!string.IsNullOrEmpty(surfaceTag))
        {
            if (TryResolveKey(baseCue + "_" + surfaceTag + "_" + variantIdx, out clip)) return clip;
            if (TryResolveKey(baseCue + "_" + surfaceTag, out clip)) return clip;
        }
        if (TryResolveKey(baseCue + "_" + variantIdx, out clip)) return clip;
        if (TryResolveKey(baseCue, out clip)) return clip;
        soundedFallbacks++;
        lastFallbackCue = baseCue;
        return synth;
    }

    /// <summary>SFX gain layer via DiabloSettings.</summary>
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

    static void PlayUiSfx(AudioClip synth, string cueName, float volume)
    {
        EnsureBuilt();
        var clip = Resolve(cueName, synth);
        if (PlaybackMuted) return;
        try { source.PlayOneShot(clip, volume * DiabloSettings.EffectiveUI); }
        catch { soundedFallbacks++; }
    }

    static AudioClip PickVariant(AudioClip[] pool, ref int lastIdx)
    {
        if (pool == null || pool.Length == 0) return null;
        if (pool.Length == 1) { lastIdx = 0; lastVariantIndex = 0; return pool[0]; }
        int idx = lastIdx < 0 ? variantRng.Next(pool.Length) : variantRng.Next(pool.Length - 1);
        if (lastIdx >= 0 && idx >= lastIdx) idx++;
        lastIdx = idx;
        lastVariantIndex = idx;
        return pool[idx];
    }

    public static HitSurface SurfaceFor(GameObject target, Vector3 fromPos)
    {
        if (target == null) return HitSurface.FleshBone;
        var sg = target.GetComponent<ShardGuard>();
        if (sg != null)
        {
            var atk = target.GetComponent<EnemyAttack>();
            bool stunned = atk != null && atk.IsStunned;
            if (!stunned && ShardGuard.InArc(target.transform.position, target.transform.forward, fromPos, sg.arcDegrees))
                return HitSurface.GlassShield;
            return HitSurface.Glass;
        }
        if (target.GetComponent<GlassWard>() != null)
        {
            var gw = target.GetComponent<GlasswrightController>();
            if (gw != null && gw.LivePillars > 0)
                return HitSurface.GlassShield;
            return HitSurface.Glass;
        }
        string n = (target.name ?? "").ToLowerInvariant();
        if (n.Contains("glass") || n.Contains("shard") || n.Contains("pillar") || n.Contains("prism"))
            return HitSurface.Glass;
        if (target.GetComponent<AshLancer>() != null
            || n.Contains("kiln") || n.Contains("cinder") || n.Contains("ash")
            || n.Contains("thrall") || n.Contains("slag"))
            return HitSurface.StoneAsh;
        var eh = target.GetComponent<EnemyHealth>();
        if (target.GetComponent<BossController>() != null || (eh != null && eh.tier != LootTier.Normal))
            return HitSurface.ArmorMetal;
        return HitSurface.FleshBone;
    }

    public static GroundSurface GroundFor(Vector3 worldPos)
    {
        switch (WorldBounds.ZoneAt(worldPos).name)
        {
            case "Ossuary": return GroundSurface.Stone;
            case "Kiln": return GroundSurface.Basalt;
            case "Barrens": return GroundSurface.Sand;
            default: return GroundSurface.Moor;
        }
    }

    static AudioClip[] PoolFor(HitSurface surface)
    {
        switch (surface)
        {
            case HitSurface.ArmorMetal: return hitArmorMetalVariants;
            case HitSurface.StoneAsh: return hitStoneAshVariants;
            case HitSurface.Glass: return hitGlassVariants;
            case HitSurface.GlassShield: return hitGlassShieldVariants;
            default: return hitFleshBoneVariants;
        }
    }

    static AudioClip[] StepPoolFor(GroundSurface ground)
    {
        switch (ground)
        {
            case GroundSurface.Stone: return stepStoneVariants;
            case GroundSurface.Basalt: return stepBasaltVariants;
            case GroundSurface.Sand: return stepSandVariants;
            default: return stepMoorVariants;
        }
    }

    public static void PlaySwing()
    {
        EnsureBuilt();
        soundedSwings++;
        var chosen = PickVariant(swingVariants, ref lastSwingIdx);
        Varied(ResolveVariant("Swing", null, lastVariantIndex, chosen), 0.55f);
    }

    public static void PlayHit()
    {
        EnsureBuilt();
        lastHitSurface = HitSurface.FleshBone;
        soundedHits++;
        var chosen = PickVariant(hitFleshBoneVariants, ref lastHitIdx);
        Varied(ResolveVariant("Hit", HitSurface.FleshBone.ToString(), lastVariantIndex, chosen), 0.68f);
    }

    public static void PlayHitFor(GameObject target, Vector3 fromPos, bool melee = true)
    {
        EnsureBuilt();
        var surface = SurfaceFor(target, fromPos);
        lastHitSurface = surface;
        var chosen = PickVariant(PoolFor(surface), ref lastHitIdx);
        Varied(ResolveVariant("Hit", surface.ToString(), lastVariantIndex, chosen), surface == HitSurface.GlassShield ? 0.72f : 0.68f);
        soundedHits++;
        if (melee && surface == HitSurface.ArmorMetal) soundedSwordHits++;
    }
    public static void PlayDeath() { EnsureBuilt(); soundedDeaths++; Varied(Resolve("Death", deathClip), 0.60f); }
    public static void PlayLoot() { EnsureBuilt(); soundedLoot++; Varied(Resolve("Loot", lootClip), 0.50f); }
    /// <summary>Menu tick: muted wood/leather tap routed through EffectiveUI.</summary>
    public static void PlayUIClick() { EnsureBuilt(); PlayUiSfx(uiClickClip, "UIClick", UIClickVolume); }
    public static void PlayHurt() { EnsureBuilt(); Varied(Resolve("Hurt", hurtClip), 0.65f); }
    public static void PlayPlayerDeath() { EnsureBuilt(); PlaySfx(playerDeathClip, "PlayerDeath", 0.72f); }
    /// <summary>Fireball leaving the hand.</summary>
    public static void PlayWhoosh() { EnsureBuilt(); Varied(Resolve("Whoosh", whooshClip), 0.62f); soundedCasts++; }
    /// <summary>Bowstring release.</summary>
    public static void PlayTwang() { EnsureBuilt(); Varied(Resolve("Twang", twangClip), 0.60f); soundedCasts++; }
    public static void PlayBlast() { EnsureBuilt(); Varied(Resolve("Blast", blastClip), 0.78f); soundedBlasts++; }
    public static void PlayCrit() { EnsureBuilt(); Varied(Resolve("Crit", critClip), 0.75f); soundedCrits++; }
    // Lanternbearer: warm bronze chime, rising radiant swell, shadow mist whisper.
    public static void PlayLantern() { EnsureBuilt(); Varied(Resolve("Lantern", lanternClip), 0.58f); soundedCasts++; }
    public static void PlayHalo() { EnsureBuilt(); Varied(Resolve("Halo", haloClip), 0.62f); soundedCasts++; }
    public static void PlayVeil() { EnsureBuilt(); Varied(Resolve("Veil", veilClip), 0.55f); soundedCasts++; }
    // Fenwalker kit (class-kits-parity): iron chain rattle, heavy charge, wet bog, oath gong.
    public static void PlayMireHook() { EnsureBuilt(); Varied(Resolve("MireHook", mireHookClip), 0.62f); soundedCasts++; }
    public static void PlayBogCharge() { EnsureBuilt(); Varied(Resolve("BogCharge", bogChargeClip), 0.66f); soundedCasts++; }
    public static void PlayMireGround() { EnsureBuilt(); Varied(Resolve("MireGround", mireGroundClip), 0.60f); soundedCasts++; }
    public static void PlayIronVow() { EnsureBuilt(); Varied(Resolve("IronVow", ironVowClip), 0.62f); soundedCasts++; }
    // Rogue kit: clay pot puff, cloth roll, glass vial splash, powder blast.
    public static void PlaySmokePot() { EnsureBuilt(); Varied(Resolve("SmokePot", smokePotClip), 0.58f); soundedCasts++; }
    public static void PlayShadowRoll() { EnsureBuilt(); Varied(Resolve("ShadowRoll", shadowRollClip), 0.52f); soundedCasts++; }
    public static void PlayVenomVial() { EnsureBuilt(); Varied(Resolve("VenomVial", venomVialClip), 0.58f); soundedCasts++; }
    public static void PlayPowderKeg() { EnsureBuilt(); Varied(Resolve("PowderKeg", powderKegClip), 0.80f); soundedBlasts++; }
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
    public static AudioClip HealClip { get { EnsureBuilt(); return healClip; } }
    public static AudioClip QuestClip { get { EnsureBuilt(); return questClip; } }
    public static AudioClip ShrineClip { get { EnsureBuilt(); return shrineClip; } }
    public static AudioClip BlockClip { get { EnsureBuilt(); return blockClip; } }
    public static AudioClip FootstepClip { get { EnsureBuilt(); return footstepClip; } }

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
            case Cue.Heal: PlayHeal(); break;
            case Cue.Quest: PlayQuest(); break;
            case Cue.Shrine: PlayShrine(); break;
            case Cue.Block: PlayBlock(); break;
            case Cue.Footstep: PlayFootstep(); break;
            case Cue.MireHook: PlayMireHook(); break;
            case Cue.BogCharge: PlayBogCharge(); break;
            case Cue.MireGround: PlayMireGround(); break;
            case Cue.IronVow: PlayIronVow(); break;
            case Cue.SmokePot: PlaySmokePot(); break;
            case Cue.ShadowRoll: PlayShadowRoll(); break;
            case Cue.VenomVial: PlayVenomVial(); break;
            case Cue.PowderKeg: PlayPowderKeg(); break;
            case Cue.Ambient: break; // looping bed is owned by DiabloAmbience, never a hit
            default: PlayHit(); break;
        }
    }

    public static void PlaySwordHit1() { EnsureBuilt(); Varied(Resolve("SwordHit1", swordHit1Clip), 0.65f); soundedSwordHits++; soundedHits++; }
    public static void PlaySwordHit2() { EnsureBuilt(); Varied(Resolve("SwordHit2", swordHit2Clip), 0.65f); soundedSwordHits++; soundedHits++; }
    public static void PlaySwordHit3() { EnsureBuilt(); Varied(Resolve("SwordHit3", swordHit3Clip), 0.72f); soundedSwordHits++; soundedHits++; }
    public static void PlayFireball() { EnsureBuilt(); Varied(Resolve("Fireball", fireballClip), 0.68f); soundedSkillShots++; soundedCasts++; }
    public static void PlayArrow() { EnsureBuilt(); Varied(Resolve("Arrow", arrowClip), 0.55f); soundedSkillShots++; soundedCasts++; }
    public static void PlayCleave() { EnsureBuilt(); Varied(Resolve("Cleave", cleaveClip), 0.70f); soundedSkillShots++; soundedCasts++; }
    public static void PlayHuskGrowl() { EnsureBuilt(); Varied(Resolve("HuskGrowl", huskGrowlClip), 0.60f); soundedEnemyVox++; }
    public static void PlayWardenTaunt() { EnsureBuilt(); Varied(Resolve("WardenTaunt", wardenTauntClip), 0.62f); soundedEnemyVox++; }
    public static void PlayBossRoar() { EnsureBuilt(); Varied(Resolve("BossRoar", bossRoarClip), 0.80f); soundedEnemyVox++; }
    public static void PlayGold() { EnsureBuilt(); Varied(Resolve("Gold", goldClip), 0.48f); soundedGold++; soundedLoot++; }
    public static void PlayRareDrop() { EnsureBuilt(); PlaySfx(rareDropClip, "RareDrop", 0.62f); soundedRare++; soundedLoot++; }
    public static void PlayVendorBuy() { EnsureBuilt(); PlaySfx(vendorBuyClip, "VendorBuy", VendorVolume); soundedVendor++; }
    public static void PlayVendorSell() { EnsureBuilt(); PlaySfx(vendorSellClip, "VendorSell", VendorVolume); soundedVendor++; }
    public static void PlayGate() { EnsureBuilt(); PlaySfx(gateClip, "Gate", 0.68f); soundedGate++; }
    public static void PlayHeal() { EnsureBuilt(); PlaySfx(healClip, "Heal", 0.60f); soundedHeals++; }
    public static void PlayQuest() { EnsureBuilt(); PlaySfx(questClip, "Quest", 0.60f); soundedQuests++; }
    public static void PlayShrine() { EnsureBuilt(); PlaySfx(shrineClip, "Shrine", 0.62f); soundedShrines++; }
    public static void PlayBlock() { EnsureBuilt(); Varied(Resolve("Block", blockClip), 0.68f); soundedBlocks++; }
    public static void PlayFootstep()
    {
        EnsureBuilt();
        lastGroundSurface = GroundSurface.Moor;
        soundedSteps++;
        var chosen = PickVariant(stepMoorVariants, ref lastStepIdx);
        Varied(ResolveVariant("Footstep", GroundSurface.Moor.ToString(), lastVariantIndex, chosen), 0.24f);
    }

    public static void PlayFootstep(Vector3 worldPos)
    {
        EnsureBuilt();
        var ground = GroundFor(worldPos);
        lastGroundSurface = ground;
        soundedSteps++;
        var chosen = PickVariant(StepPoolFor(ground), ref lastStepIdx);
        Varied(ResolveVariant("Footstep", ground.ToString(), lastVariantIndex, chosen), ground == GroundSurface.Stone ? 0.26f : 0.24f);
    }

    public static void PlayLevelUp() { EnsureBuilt(); soundedLevelUps++; PlaySfx(levelUpClip, "LevelUp", 0.68f); }

    /// <summary>
    /// Repeats must not grate: a pooled pitched source per call (±7 % pitch and ±7 % micro-volume).
    /// PlayOneShot cannot pitch, so rotate through a few extra sources.
    /// Multiplies by SfxGain once.
    /// </summary>
    static void Varied(AudioClip clip, float volume)
    {
        if (PlaybackMuted) return;
        var s = pitched[pitchedNext];
        pitchedNext = (pitchedNext + 1) % pitched.Length;
        s.pitch = 0.93f + (float)pitchRng.NextDouble() * 0.14f;
        float microVol = 0.93f + (float)pitchRng.NextDouble() * 0.14f;
        try { s.PlayOneShot(clip, Mathf.Clamp01(volume * microVol * SfxGain)); } catch { }
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
        soundedHeals = 0;
        soundedQuests = 0;
        soundedShrines = 0;
        soundedBlocks = 0;
        soundedSteps = 0;
        soundedFallbacks = 0;
        lastFallbackCue = null;
        lastHitSurface = HitSurface.FleshBone;
        lastGroundSurface = GroundSurface.Moor;
        lastVariantIndex = 0;
        lastSwingIdx = -1;
        lastHitIdx = -1;
        lastStepIdx = -1;
    }

    static void EnsureBuilt()
    {
        // Scene opens destroy the synth objects (no DontDestroyOnLoad outside
        // play mode); rebuild instead of handing out dead clips.
        if (built && source != null && swingClip != null) { EnsureListener(); return; }
        built = true;
        var go = new GameObject("DiabloAudio");
        if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        EnsureListener();
        var rng = new System.Random(1234); // deterministic clips

        swingClip = FilteredNoise("Swing", 0.14f, 240f, 1050f, 140f, 80f, 0.15f, 0.55f, rng);
        hitClip = MetallicImpact("Hit", 0.14f, 155f, 60f, 260f, 390f, 0.12f, 1050f, 0.65f, rng);
        deathClip = SynthDeathRattle("Death", 0.38f, rng);
        lootClip = SynthLoot(rng);
        uiClickClip = FilteredNoise("UIClick", 0.055f, 200f, 920f, 185f, 115f, 0.45f, 0.45f, rng);
        hurtClip = FilteredNoise("Hurt", 0.18f, 90f, 520f, 135f, 58f, 0.65f, 0.62f, rng);
        playerDeathClip = SynthPlayerDeath("PlayerDeath", 0.70f);
        whooshClip = Whoosh("Whoosh", 0.34f, 220f, 900f, 200f, 125f, 0.16f, 0.58f, rng);
        blastClip = Boom("Blast", 0.66f, 85f, 34f, 460f, 0.78f, rng);
        twangClip = SynthTwang("Twang", 0.24f, 165f, rng);
        critClip = MetallicImpact("Crit", 0.22f, 170f, 52f, 330f, 495f, 0.35f, 1650f, 0.72f, rng);
        levelUpClip = ChordSting("LevelUp", new[] { 146.83f, 220f, 293.66f, 369.99f }, 0.11f, 0.72f, 1300f, 0.65f, 0.60f);
        ambientClip = Ambient("MoorWind", 6f, rng);
        swordHit1Clip = MetallicImpact("SwordHit1", 0.14f, 160f, 68f, 440f, 700f, 0.25f, 1550f, 0.62f, rng);
        swordHit2Clip = MetallicImpact("SwordHit2", 0.16f, 145f, 60f, 390f, 630f, 0.26f, 1450f, 0.62f, rng);
        swordHit3Clip = MetallicImpact("SwordHit3", 0.19f, 130f, 52f, 330f, 540f, 0.28f, 1350f, 0.68f, rng);
        fireballClip = Whoosh("Fireball", 0.38f, 170f, 780f, 170f, 98f, 0.24f, 0.65f, rng);
        arrowClip = SynthArrow("Arrow", 0.12f, rng);
        cleaveClip = FilteredNoise("Cleave", 0.24f, 150f, 850f, 130f, 65f, 0.35f, 0.65f, rng);
        huskGrowlClip = SynthHuskGrowl("HuskGrowl", 0.46f, rng);
        wardenTauntClip = SynthWardenTaunt("WardenTaunt", 0.58f, rng);
        bossRoarClip = SynthBossRoar("BossRoar", 1.05f, rng);
        goldClip = SynthGold(rng);
        rareDropClip = ChordSting("RareDrop", new[] { 220f, 293.66f, 329.63f, 440f }, 0.09f, 0.52f, 1300f, 0.58f, 0.65f);
        vendorBuyClip = ChordSting("VendorBuy", new[] { 220f, 329.63f }, 0.06f, 0.15f, 1050f, 0.48f, 0.45f);
        vendorSellClip = ChordSting("VendorSell", new[] { 329.63f, 246.94f }, 0.06f, 0.15f, 1050f, 0.48f, 0.45f);
        gateClip = SynthGate("Gate", 0.85f, rng);
        healClip = ChordSting("Heal", new[] { 293.66f, 369.99f, 440f }, 0.07f, 0.45f, 1200f, 0.56f, 0.55f);
        questClip = ChordSting("Quest", new[] { 220f, 293.66f, 329.63f, 293.66f }, 0.10f, 0.50f, 1200f, 0.56f, 0.55f);
        shrineClip = WarmBell("Shrine", 0.75f, 261.63f, 1100f, 0.58f);
        blockClip = MetallicImpact("Block", 0.16f, 175f, 72f, 310f, 470f, 0.38f, 1450f, 0.66f, rng);
        footstepClip = FilteredNoise("Footstep", 0.065f, 110f, 580f, 95f, 55f, 0.35f, 0.45f, rng);
        lanternClip = WarmBell("LanternBolt", 0.44f, 329.63f, 1250f, 0.55f);
        haloClip = SynthHalo("SearingHalo", 0.42f);
        veilClip = SynthVeil("VeilStep", 0.28f, rng);
        mireHookClip = MetallicImpact("MireHook", 0.26f, 210f, 90f, 520f, 780f, 0.45f, 1800f, 0.60f, rng);
        bogChargeClip = Whoosh("BogCharge", 0.42f, 120f, 520f, 110f, 62f, 0.40f, 0.68f, rng);
        mireGroundClip = FilteredNoise("MireGround", 0.55f, 60f, 340f, 70f, 38f, 0.55f, 0.58f, rng);
        ironVowClip = WarmBell("IronVow", 0.70f, 146.83f, 900f, 0.60f);
        smokePotClip = FilteredNoise("SmokePot", 0.38f, 220f, 1600f, 120f, 70f, 0.25f, 0.55f, rng);
        shadowRollClip = SynthVeil("ShadowRoll", 0.22f, rng);
        venomVialClip = SynthGlassImpact("VenomVial", 0.20f, 760f, 1140f, 0.56f, rng);
        powderKegClip = Boom("PowderKeg", 0.70f, 95f, 38f, 900f, 0.78f, rng);

        swingVariants = new[]
        {
            swingClip,
            FilteredNoise("SwingVar2", 0.12f, 300f, 1180f, 155f, 88f, 0.14f, 0.54f, rng),
            FilteredNoise("SwingVar3", 0.16f, 190f, 920f, 125f, 68f, 0.18f, 0.56f, rng),
        };
        hitFleshBoneVariants = new[]
        {
            hitClip,
            SynthBoneHit("HitBone2", 0.13f, 145f, 55f, 340f, 1150f, 0.62f, rng),
            SynthBoneHit("HitBone3", 0.15f, 135f, 50f, 300f, 1100f, 0.64f, rng),
        };
        hitArmorMetalVariants = new[] { swordHit1Clip, swordHit2Clip, swordHit3Clip };
        hitStoneAshVariants = new[]
        {
            SynthStoneAshImpact("HitStone1", 0.16f, 120f, 44f, 0.64f, rng),
            SynthStoneAshImpact("HitStone2", 0.17f, 110f, 40f, 0.62f, rng),
            SynthStoneAshImpact("HitStone3", 0.15f, 132f, 48f, 0.65f, rng),
        };
        hitGlassVariants = new[]
        {
            SynthGlassImpact("HitGlass1", 0.15f, 620f, 930f, 0.58f, rng),
            SynthGlassImpact("HitGlass2", 0.14f, 560f, 840f, 0.56f, rng),
            SynthGlassImpact("HitGlass3", 0.16f, 680f, 1020f, 0.58f, rng),
        };
        hitGlassShieldVariants = new[]
        {
            SynthGlassShieldDeflect("HitGlassShield1", 0.18f, 190f, 520f, 780f, 0.66f, rng),
            SynthGlassShieldDeflect("HitGlassShield2", 0.19f, 175f, 480f, 720f, 0.64f, rng),
            SynthGlassShieldDeflect("HitGlassShield3", 0.17f, 205f, 555f, 832f, 0.66f, rng),
        };
        stepMoorVariants = new[]
        {
            footstepClip,
            FilteredNoise("StepMoor2", 0.068f, 100f, 540f, 90f, 50f, 0.38f, 0.44f, rng),
            FilteredNoise("StepMoor3", 0.062f, 120f, 600f, 102f, 58f, 0.32f, 0.45f, rng),
        };
        stepStoneVariants = new[]
        {
            FilteredNoise("StepStone1", 0.052f, 240f, 960f, 160f, 85f, 0.42f, 0.46f, rng),
            FilteredNoise("StepStone2", 0.050f, 260f, 1000f, 175f, 90f, 0.40f, 0.45f, rng),
            FilteredNoise("StepStone3", 0.055f, 220f, 920f, 148f, 80f, 0.44f, 0.46f, rng),
        };
        stepBasaltVariants = new[]
        {
            FilteredNoise("StepBasalt1", 0.075f, 170f, 820f, 110f, 52f, 0.36f, 0.46f, rng),
            FilteredNoise("StepBasalt2", 0.072f, 185f, 850f, 100f, 48f, 0.38f, 0.45f, rng),
            FilteredNoise("StepBasalt3", 0.078f, 160f, 790f, 118f, 56f, 0.35f, 0.46f, rng),
        };
        stepSandVariants = new[]
        {
            FilteredNoise("StepSand1", 0.082f, 300f, 1120f, 95f, 65f, 0.15f, 0.42f, rng),
            FilteredNoise("StepSand2", 0.086f, 280f, 1060f, 90f, 60f, 0.14f, 0.41f, rng),
            FilteredNoise("StepSand3", 0.078f, 320f, 1160f, 100f, 68f, 0.16f, 0.42f, rng),
        };

        pitched = new AudioSource[4];
        for (int i = 0; i < pitched.Length; i++) pitched[i] = go.AddComponent<AudioSource>();
    }

    /// <summary>Without an AudioListener Unity renders silence: attach one to the
    /// main camera when the scene has none (builder scenes carry their own).</summary>
    static void EnsureListener()
    {
        if (!Application.isPlaying) return;
        try
        {
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() != null) return;
            var cam = Camera.main;
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
        }
        catch { }
    }

    // ---- DSP Primitives ----

    static int Samples(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));

    static void LowPass(float[] data, float cutoffHz, int passes = 2)
    {
        float a = Mathf.Clamp01(1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / SampleRate));
        for (int p = 0; p < passes; p++)
        {
            float y = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                y += a * (data[i] - y);
                data[i] = y;
            }
        }
    }

    static void HighPass(float[] data, float cutoffHz)
    {
        float rc = 1f / (2f * Mathf.PI * Mathf.Max(10f, cutoffHz));
        float dt = 1f / SampleRate;
        float a = rc / (rc + dt);
        float y = 0f;
        float prevX = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float x = data[i];
            y = a * (y + x - prevX);
            prevX = x;
            data[i] = y;
        }
    }

    static void BandPass(float[] data, float lowHz, float highHz)
    {
        HighPass(data, lowHz);
        LowPass(data, highHz, 2);
    }

    static AudioClip FinishClip(string name, float[] data, float targetPeak)
    {
        double sum = 0.0;
        for (int i = 0; i < data.Length; i++) sum += data[i];
        float mean = (float)(sum / data.Length);
        float peak = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float x = data[i] - mean;
            x = x / (1f + Mathf.Abs(x) * 0.25f);
            data[i] = x;
            float ax = Mathf.Abs(x);
            if (ax > peak) peak = ax;
        }
        if (peak > 1e-5f)
        {
            float scale = targetPeak / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= scale;
        }
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

    static void AddPluck(float[] data, int start, float seconds, float freq, float amp, float brightness)
    {
        int count = Mathf.Min(Samples(seconds), data.Length - start);
        if (count <= 0) return;
        float attackSec = 0.004f;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)SampleRate;
            float env = (time < attackSec ? time / attackSec : 1f) * Mathf.Exp(-time * (2.6f / Mathf.Max(0.12f, seconds)));
            float h2Env = Mathf.Exp(-time * (5.8f / Mathf.Max(0.12f, seconds)));
            float h3Env = Mathf.Exp(-time * (9.2f / Mathf.Max(0.12f, seconds)));
            float s1 = Mathf.Sin(2f * Mathf.PI * freq * time);
            float s2 = 0.32f * brightness * h2Env * Mathf.Sin(2f * Mathf.PI * freq * 2f * time);
            float s3 = 0.12f * brightness * h3Env * Mathf.Sin(2f * Mathf.PI * freq * 3f * time);
            data[start + i] += amp * env * (s1 + s2 + s3);
        }
    }

    static AudioClip WarmBell(string name, float seconds, float f0, float cutoffHz, float peak)
    {
        int n = Samples(seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float time = i / (float)SampleRate;
            float att = Mathf.Clamp01(time / 0.006f);
            float env1 = att * Mathf.Exp(-time * (4.5f / seconds));
            float envHi = att * Mathf.Exp(-time * (14f / seconds));
            float hum = 0.38f * Mathf.Sin(2f * Mathf.PI * (f0 * 0.5f) * time);
            float fund = 0.55f * Mathf.Sin(2f * Mathf.PI * f0 * time);
            float fifth = 0.24f * Mathf.Sin(2f * Mathf.PI * (f0 * 1.5f) * time) * envHi;
            float oct = 0.14f * Mathf.Sin(2f * Mathf.PI * (f0 * 2.0f) * time) * envHi;
            float shimmer = 0.06f * Mathf.Sin(2f * Mathf.PI * (f0 * 2.4f) * time) * Mathf.Exp(-time * 24f);
            data[i] = (hum + fund) * env1 + fifth + oct + shimmer;
        }
        LowPass(data, cutoffHz, 2);
        return FinishClip(name, data, peak);
    }

    static AudioClip MetallicImpact(
        string name, float seconds, float thumpF0, float thumpF1,
        float ringA, float ringB, float ringMix, float cutoffHz, float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        var noise = new float[n];
        for (int i = 0; i < n; i++)
            noise[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-26f * ((float)i / n));
        BandPass(noise, 180f, cutoffHz);

        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = thumpF0 * Mathf.Pow(Mathf.Max(20f, thumpF1) / Mathf.Max(20f, thumpF0), t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float thump = Mathf.Sin(phase) * Mathf.Exp(-10f * t);
            float ringEnv = Mathf.Exp(-28f * time);
            float ring = (Mathf.Sin(2f * Mathf.PI * ringA * time) + 0.65f * Mathf.Sin(2f * Mathf.PI * ringB * time)) * ringEnv;
            data[i] = thump * 0.72f + noise[i] * 0.45f + ring * ringMix;
        }
        LowPass(data, cutoffHz, 2);
        return FinishClip(name, data, peak);
    }

    static AudioClip FilteredNoise(
        string name, float seconds, float lowHz, float highHz,
        float bodyF0, float bodyF1, float bodyMix, float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var noise = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float env = Mathf.Clamp01(t / 0.08f) * Mathf.Exp(-4.5f * t);
            noise[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * env;
        }
        BandPass(noise, lowHz, highHz);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = bodyF0 * Mathf.Pow(Mathf.Max(20f, bodyF1) / Mathf.Max(20f, bodyF0), t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float body = Mathf.Sin(phase) * Mathf.Exp(-7f * t);
            data[i] = noise[i] + body * bodyMix;
        }
        LowPass(data, highHz, 1);
        return FinishClip(name, data, peak);
    }

    static AudioClip Whoosh(
        string name, float seconds, float cutLow, float cutHigh, float cutEnd,
        float bodyHz, float bodyMix, float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        float lp1 = 0f, lp2 = 0f;
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float swell = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f));
            float cut = t < 0.4f
                ? Mathf.Lerp(cutLow, cutHigh, t / 0.4f)
                : Mathf.Lerp(cutHigh, cutEnd, (t - 0.4f) / 0.6f);
            float a = Mathf.Clamp01(1f - Mathf.Exp(-2f * Mathf.PI * cut / SampleRate));
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp1 += a * (white - lp1);
            lp2 += a * (lp1 - lp2);
            phase += 2f * Mathf.PI * Mathf.Lerp(bodyHz, bodyHz * 0.68f, t) / SampleRate;
            float body = Mathf.Sin(phase) * swell;
            data[i] = (lp2 * 1.6f + body * bodyMix) * swell;
        }
        HighPass(data, 90f);
        return FinishClip(name, data, peak);
    }

    static AudioClip Boom(string name, float seconds, float f0, float f1, float cutoffHz, float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var rumble = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            rumble[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-4.2f * t);
        }
        BandPass(rumble, 45f, cutoffHz);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = f0 * Mathf.Pow(f1 / f0, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float sub = Mathf.Sin(phase) * Mathf.Exp(-5.2f * t);
            data[i] = sub * 0.75f + rumble[i] * 0.85f;
        }
        LowPass(data, cutoffHz, 1);
        return FinishClip(name, data, peak);
    }

    static AudioClip ChordSting(
        string name, float[] freqs, float stepSec, float holdSec,
        float cutoffHz, float peak, float brightness)
    {
        float total = stepSec * Mathf.Max(0, freqs.Length - 1) + holdSec;
        int n = Samples(total);
        var data = new float[n];
        for (int k = 0; k < freqs.Length; k++)
        {
            int start = Samples(stepSec * k);
            float dur = (k == freqs.Length - 1) ? holdSec : Mathf.Max(holdSec * 0.65f, stepSec * 2.2f);
            AddPluck(data, start, dur, freqs[k], 0.55f, brightness);
            if (k == 0)
                AddPluck(data, start, total, freqs[0] * 0.5f, 0.25f, 0.25f);
        }
        LowPass(data, cutoffHz, 2);
        return FinishClip(name, data, peak);
    }

    // ---- Cue-Specific Sculpted Recipes ----

    static AudioClip SynthDeathRattle(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var noise = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            noise[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t);
        }
        BandPass(noise, 180f, 750f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = 175f * Mathf.Pow(55f / 175f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float flutter = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 24f * time);
            float env = Mathf.Clamp01(t / 0.04f) * Mathf.Exp(-3.8f * t);
            data[i] = (Mathf.Sin(phase) * 0.55f + noise[i] * 0.65f) * flutter * env;
        }
        LowPass(data, 850f, 2);
        return FinishClip(name, data, 0.58f);
    }

    static AudioClip SynthPlayerDeath(string name, float seconds)
    {
        int n = Samples(seconds);
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = 165f * Mathf.Pow(62f / 165f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float env = Mathf.Clamp01(t / 0.03f) * Mathf.Exp(-3.2f * t);
            float toll = Mathf.Sin(phase) + 0.45f * Mathf.Sin(2f * Mathf.PI * 110f * time) * Mathf.Exp(-4f * t);
            data[i] = toll * env;
        }
        LowPass(data, 720f, 2);
        return FinishClip(name, data, 0.68f);
    }

    static AudioClip SynthTwang(string name, float seconds, float f0, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        AddPluck(data, 0, seconds, f0, 0.7f, 0.65f);
        var click = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            click[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-35f * t);
        }
        BandPass(click, 260f, 1100f);
        for (int i = 0; i < n; i++) data[i] += click[i] * 0.35f;
        LowPass(data, 1100f, 2);
        return FinishClip(name, data, 0.58f);
    }

    static AudioClip SynthArrow(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var air = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            air[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Sin(Mathf.PI * t);
        }
        BandPass(air, 420f, 1400f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = 480f * Mathf.Pow(240f / 480f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            data[i] = air[i] * 0.85f + Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * 0.2f;
        }
        LowPass(data, 1400f, 1);
        return FinishClip(name, data, 0.52f);
    }

    static AudioClip SynthHuskGrowl(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var rasp = new float[n];
        for (int i = 0; i < n; i++) rasp[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        BandPass(rasp, 110f, 460f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = 115f * Mathf.Pow(60f / 115f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float mod = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 18f * time);
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.65f));
            data[i] = (Mathf.Sin(phase) * 0.6f + 0.35f * Mathf.Sin(phase * 2f) + rasp[i] * 0.5f) * mod * env;
        }
        LowPass(data, 520f, 2);
        return FinishClip(name, data, 0.58f);
    }

    static AudioClip SynthWardenTaunt(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var breath = new float[n];
        for (int i = 0; i < n; i++) breath[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        BandPass(breath, 220f, 620f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = t < 0.45f ? Mathf.Lerp(110f, 160f, t / 0.45f) : Mathf.Lerp(160f, 130f, (t - 0.45f) / 0.55f);
            phase += 2f * Mathf.PI * f / SampleRate;
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f));
            data[i] = (Mathf.Sin(phase) * 0.55f + 0.3f * Mathf.Sin(phase * 1.5f) + breath[i] * 0.55f) * env;
        }
        LowPass(data, 680f, 2);
        return FinishClip(name, data, 0.60f);
    }

    static AudioClip SynthBossRoar(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var roarNoise = new float[n];
        for (int i = 0; i < n; i++) roarNoise[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        BandPass(roarNoise, 80f, 520f);

        var data = new float[n];
        float p1 = 0f, p2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f1 = 95f * Mathf.Pow(38f / 95f, t);
            float f2 = 142f * Mathf.Pow(70f / 142f, t);
            p1 += 2f * Mathf.PI * f1 / SampleRate;
            p2 += 2f * Mathf.PI * f2 / SampleRate;
            float env = Mathf.Clamp01(t / 0.08f) * Mathf.Exp(-2.2f * t);
            float grit = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 21f * time);
            data[i] = (Mathf.Sin(p1) * 0.6f + Mathf.Sin(p2) * 0.35f + roarNoise[i] * 0.8f) * grit * env;
        }
        LowPass(data, 560f, 2);
        return FinishClip(name, data, 0.78f);
    }

    static AudioClip SynthLoot(System.Random rng)
    {
        int n = Samples(0.18f);
        var data = new float[n];
        var rustle = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            rustle[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-18f * t);
        }
        BandPass(rustle, 220f, 900f);
        for (int i = 0; i < n; i++) data[i] += rustle[i] * 0.35f;
        AddPluck(data, 0, 0.11f, 293.66f, 0.45f, 0.45f);
        AddPluck(data, Samples(0.045f), 0.13f, 440f, 0.50f, 0.50f);
        LowPass(data, 1150f, 2);
        return FinishClip("Loot", data, 0.50f);
    }

    static AudioClip SynthGold(System.Random rng)
    {
        int n = Samples(0.14f);
        var data = new float[n];
        int t2 = Samples(0.042f);
        for (int i = 0; i < n; i++)
        {
            float time = i / (float)SampleRate;
            float env1 = Mathf.Clamp01(time / 0.002f) * Mathf.Exp(-time * 52f);
            float c1 = (Mathf.Sin(2f * Mathf.PI * 587.33f * time) + 0.35f * Mathf.Sin(2f * Mathf.PI * 880f * time)) * env1;
            float c2 = 0f;
            if (i >= t2)
            {
                float dt = (i - t2) / (float)SampleRate;
                float env2 = Mathf.Clamp01(dt / 0.002f) * Mathf.Exp(-dt * 44f);
                c2 = (Mathf.Sin(2f * Mathf.PI * 783.99f * dt) + 0.30f * Mathf.Sin(2f * Mathf.PI * 1174.66f * dt)) * env2;
            }
            float tick = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-time * 75f) * 0.12f;
            data[i] = c1 * 0.65f + c2 * 0.75f + tick;
        }
        LowPass(data, 1550f, 2);
        return FinishClip("Gold", data, 0.48f);
    }

    static AudioClip SynthGate(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var air = new float[n];
        for (int i = 0; i < n; i++) air[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        BandPass(air, 90f, 480f);

        var data = new float[n];
        float pRumble = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = Mathf.Lerp(65f, 98f, t);
            pRumble += 2f * Mathf.PI * f / SampleRate;
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f));
            float chord = 0.45f * Mathf.Sin(2f * Mathf.PI * 146.83f * time)
                        + 0.35f * Mathf.Sin(2f * Mathf.PI * 220f * time);
            data[i] = (Mathf.Sin(pRumble) * 0.65f + chord + air[i] * 0.45f) * env;
        }
        LowPass(data, 850f, 2);
        return FinishClip(name, data, 0.65f);
    }

    static AudioClip SynthHalo(string name, float seconds)
    {
        int n = Samples(seconds);
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = 196f * Mathf.Pow(2f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f));
            float fifth = 0.4f * Mathf.Sin(2f * Mathf.PI * 293.66f * time);
            data[i] = (Mathf.Sin(phase) * 0.7f + fifth) * env;
        }
        LowPass(data, 1100f, 2);
        return FinishClip(name, data, 0.58f);
    }

    static AudioClip SynthVeil(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var mist = new float[n];
        for (int i = 0; i < n; i++) mist[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        BandPass(mist, 280f, 1050f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = 370f * Mathf.Pow(185f / 370f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.65f));
            data[i] = (mist[i] * 0.75f + Mathf.Sin(phase) * 0.3f) * env;
        }
        LowPass(data, 1050f, 1);
        return FinishClip(name, data, 0.52f);
    }

    /// <summary>
    /// Room tone for the moor: warm lowpassed breeze with slow gusts plus a very subtle 55 Hz floor.
    /// Tail crossfaded into head so the loop has no seam.
    /// </summary>
    static AudioClip Ambient(string name, float seconds, System.Random rng)
    {
        int n = Samples(seconds);
        var data = new float[n];
        float brown = 0f;
        for (int i = 0; i < n; i++)
        {
            brown = Mathf.Clamp(brown + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.035f, -1f, 1f) * 0.997f;
            data[i] = brown;
        }
        LowPass(data, 360f, 2);
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
            float cycle = (float)i / loop.Length;
            float gust = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * cycle * 2f) * Mathf.Sin(2f * Mathf.PI * cycle);
            float drone = Mathf.Sin(2f * Mathf.PI * 55f * time) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * cycle));
            loop[i] = data[i] * gust * 0.85f + drone * 0.04f;
        }
        var clip = AudioClip.Create(name, loop.Length, 1, SampleRate, false);
        clip.SetData(loop, 0);
        return clip;
    }

    public static AudioClip[] AllVariantClips()
    {
        EnsureBuilt();
        var list = new System.Collections.Generic.List<AudioClip>(32);
        list.AddRange(swingVariants);
        list.AddRange(hitFleshBoneVariants);
        list.AddRange(hitArmorMetalVariants);
        list.AddRange(hitStoneAshVariants);
        list.AddRange(hitGlassVariants);
        list.AddRange(hitGlassShieldVariants);
        list.AddRange(stepMoorVariants);
        list.AddRange(stepStoneVariants);
        list.AddRange(stepBasaltVariants);
        list.AddRange(stepSandVariants);
        return list.ToArray();
    }

    static AudioClip SynthBoneHit(
        string name, float seconds, float thumpF0, float thumpF1,
        float snapLow, float cutoffHz, float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var snap = new float[n];
        for (int i = 0; i < n; i++)
            snap[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-38f * ((float)i / n));
        BandPass(snap, snapLow, cutoffHz);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = thumpF0 * Mathf.Pow(thumpF1 / thumpF0, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float thump = Mathf.Sin(phase) * Mathf.Exp(-9f * t);
            data[i] = thump * 0.72f + snap[i] * 0.65f;
        }
        LowPass(data, cutoffHz, 2);
        return FinishClip(name, data, peak);
    }

    static AudioClip SynthStoneAshImpact(
        string name, float seconds, float thumpF0, float thumpF1,
        float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var gravel = new float[n];
        for (int i = 0; i < n; i++)
            gravel[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-11f * ((float)i / n));
        BandPass(gravel, 160f, 820f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = thumpF0 * Mathf.Pow(thumpF1 / thumpF0, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float sub = Mathf.Sin(phase) * Mathf.Exp(-8f * t);
            data[i] = sub * 0.78f + gravel[i] * 0.65f;
        }
        LowPass(data, 950f, 2);
        return FinishClip(name, data, peak);
    }

    static AudioClip SynthGlassImpact(
        string name, float seconds, float p1, float p2,
        float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var shard = new float[n];
        for (int i = 0; i < n; i++)
            shard[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-22f * ((float)i / n));
        BandPass(shard, 360f, 1550f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = Mathf.Lerp(150f, 75f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float tap = Mathf.Sin(phase) * Mathf.Exp(-14f * t);
            float crystalEnv = Mathf.Clamp01(time / 0.002f) * Mathf.Exp(-time * 36f);
            float crystal = (Mathf.Sin(2f * Mathf.PI * p1 * time) + 0.55f * Mathf.Sin(2f * Mathf.PI * p2 * time)) * crystalEnv;
            data[i] = tap * 0.45f + crystal * 0.65f + shard[i] * 0.40f;
        }
        LowPass(data, 1650f, 2);
        return FinishClip(name, data, peak);
    }

    static AudioClip SynthGlassShieldDeflect(
        string name, float seconds, float knockF0, float p1, float p2,
        float peak, System.Random rng)
    {
        int n = Samples(seconds);
        var clink = new float[n];
        for (int i = 0; i < n; i++)
            clink[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-26f * ((float)i / n));
        BandPass(clink, 320f, 1450f);

        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float time = i / (float)SampleRate;
            float f = Mathf.Lerp(knockF0, 95f, t);
            phase += 2f * Mathf.PI * f / SampleRate;
            float knock = Mathf.Sin(phase) * Mathf.Exp(-12f * t);
            float bellEnv = Mathf.Clamp01(time / 0.002f) * Mathf.Exp(-time * 18f);
            float bell = (Mathf.Sin(2f * Mathf.PI * p1 * time) + 0.55f * Mathf.Sin(2f * Mathf.PI * p2 * time)) * bellEnv;
            data[i] = knock * 0.55f + bell * 0.72f + clink[i] * 0.30f;
        }
        LowPass(data, 1600f, 2);
        return FinishClip(name, data, peak);
    }
}
