using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Kilnmother (act-one S4), Act I boss in the Cinder Kiln. Phase 1: fire
/// waves, an expanding ring with a 60° gap to step into. Phase 2 below 50 %
/// HP: faster waves, burning ground fields under the hero and four summoned
/// Cinder Thralls (once). Death completes Act I, saves and shows the act
/// screen. Loot comes from <see cref="LootTier.ActBoss"/> via EnemyHealth.
/// </summary>
public sealed class KilnmotherController : MonoBehaviour, IEnemyDeathHook
{
    public const int MaxHP = 320;
    public const float PhaseTwoFraction = 0.5f;
    public const float WaveInterval = 4f;
    public const float WaveIntervalPhaseTwo = 2.5f;
    public const float WaveSpeed = 6f;
    public const float WaveMaxRadius = 13f;
    public const float WaveThickness = 0.8f;
    public const float WaveGapDegrees = 60f;
    public const int WaveDamage = 3;
    public const float FieldInterval = 5f;
    public const int SummonCount = 4;
    public const float AggroRange = 18f;

    public static int Kills;
    public static int PhaseTwoEntries;
    public static int WavesFired;
    /// <summary>S1 kindled finale: sector waves fired (Kindled phase 2 only).</summary>
    public static int SectorPatternsFired;

    public bool PhaseTwo { get; private set; }
    bool dead;
    float lastWave = float.NegativeInfinity;
    float lastField = float.NegativeInfinity;
    float lastSector = float.NegativeInfinity;
    int sectorWave;
    readonly System.Random rng = new System.Random(31);
    readonly List<FireWave> waves = new List<FireWave>();
    readonly List<KilnSectorPattern> sectorPatterns = new List<KilnSectorPattern>();

    /// <summary>S1: Kindled phase 2 uses the sector finale (solo-Kindled), Normal keeps waves+fields.</summary>
    public static bool KindledFinaleActive => DifficultyProfile.Current == DifficultyProfile.Kindled;
    /// <summary>Live sector patterns (headless seam).</summary>
    public int ActiveSectorPatternCount => sectorPatterns.Count;

    void Update()
    {
        if (dead) return;
        var hp = GetComponent<EnemyHealth>();
        if (hp == null) return;
        if (!PhaseTwo && hp.HPFraction() <= PhaseTwoFraction) EnterPhaseTwo();
        var hero = Hero();
        if (hero == null) return;
        if (KilnLayout.PlazaSafe(hero.position)) return; // fair-fights arrival plaza
        var d = hero.position - transform.position;
        d.y = 0f;
        if (d.magnitude > AggroRange) return;
        float now = Time.time;
        if (PhaseTwo && KindledFinaleActive)
        {
            // S1 finale: sector pattern replaces faster waves + random fields; summons stay.
            if (now - lastSector >= KilnSectorPattern.PatternInterval) FireSectorPatternNow(now);
            TickSectorPatterns(now);
        }
        else
        {
            if (now - lastWave >= (PhaseTwo ? WaveIntervalPhaseTwo : WaveInterval)) FireWaveNow(now);
            if (PhaseTwo && now - lastField >= FieldInterval)
            {
                lastField = now;
                BurningGround.Spawn(hero.position);
            }
        }
        foreach (var w in waves.ToArray())
            if (w.Tick(now, hero)) waves.Remove(w);
    }

    static Transform Hero()
    {
        return HeroLocator.Solo();
    }

    public FireWave FireWaveNow(float now)
    {
        lastWave = now;
        WavesFired++;
        var w = new FireWave(transform.position, now, (float)(rng.NextDouble() * 360.0));
        waves.Add(w);
        DiabloJuice.OnBlast(transform.position);
        return w;
    }

    /// <summary>Phase 2 (headless-callable): summons four thralls once.</summary>
    public void EnterPhaseTwo()
    {
        if (PhaseTwo) return;
        PhaseTwo = true;
        PhaseTwoEntries++;
        for (int i = 0; i < SummonCount; i++)
        {
            float a = i * Mathf.PI * 2f / SummonCount;
            var pos = transform.position + new Vector3(Mathf.Cos(a) * 3f, 0f, Mathf.Sin(a) * 3f);
            var thrall = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            thrall.name = $"Kiln Summon_{i}_{PhaseTwoEntries}";
            thrall.tag = "Husk";
            thrall.transform.position = pos;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var r = thrall.GetComponent<Renderer>();
            if (shader != null && r != null) r.sharedMaterial = new Material(shader) { color = new Color(0.45f, 0.2f, 0.12f) };
            thrall.AddComponent<EnemyHealth>().SetTier(6, LootTier.Normal);
            thrall.AddComponent<EnemyAttack>().damage = 1;
            thrall.AddComponent<CinderThrall>();
        }
        Debug.Log($"[Kilnmother] Phase 2: burning ground and {SummonCount} thralls.");
    }

    public void OnEnemyDeath()
    {
        if (dead) return;
        dead = true;
        Kills++;
        ClearSectorPatterns();
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        bool completed = RunProfile.Act.CompleteAct();
        Debug.Log($"[Kilnmother] slain (kills={Kills}, act complete={RunProfile.Act.ActOneComplete}).");
        if (completed && hero != null && Diablo.IsLocalAuthority())
        {
            try { KindledFinaleReward.OnKilnmotherKill(hero); }
            catch (System.Exception e) { Debug.LogWarning($"[Kilnmother] Kindled reward failed: {e.Message}"); }
            SaveService.SaveNow(hero);
        }
        // coop-trio: clients book Act I and autosave their own hero, like solo.
        var relay = DiabloNetworkPlayer.HostRelay();
        if (relay != null) relay.BroadcastActBossKill();
        SessionComplete.ShowActComplete(hero);
    }

    /// <summary>S1 finale (headless-callable): fire one sector wave from the run seed.</summary>
    public KilnSectorPattern FireSectorPatternNow(float now)
    {
        lastSector = now;
        SectorPatternsFired++;
        var pattern = new KilnSectorPattern(transform.position, now,
            KilnSectorPattern.ActiveSectors(RunProfile.RunSeed, sectorWave++));
        sectorPatterns.Add(pattern);
        DiabloJuice.OnBlast(transform.position);
        return pattern;
    }

    void TickSectorPatterns(float now)
    {
        var hero = Hero();
        Vector3 heroPos = hero != null ? hero.position : transform.position + Vector3.right * (KilnSectorPattern.Radius + 1f);
        foreach (var p in sectorPatterns.ToArray())
            if (p.Tick(now, heroPos, null)) sectorPatterns.Remove(p);
    }

    /// <summary>S1: death, respawn or retreat removes the effect.</summary>
    public void ClearSectorPatterns() => sectorPatterns.Clear();

    public static void ResetRun()
    {
        Kills = 0;
        PhaseTwoEntries = 0;
        WavesFired = 0;
        SectorPatternsFired = 0;
    }
}

/// <summary>Expanding fire ring with a gap; hits the hero once when the ring crosses him outside the gap.</summary>
public sealed class FireWave
{
    public readonly Vector3 center;
    public readonly float bornAt;
    public readonly float gapAngle;
    public bool HitHero { get; private set; }

    public FireWave(Vector3 center, float bornAt, float gapAngle)
    {
        this.center = center;
        this.bornAt = bornAt;
        this.gapAngle = gapAngle;
    }

    public float RadiusAt(float now) => (now - bornAt) * KilnmotherController.WaveSpeed;

    /// <summary>Pure rule: is the hero inside the ring band and outside the gap at this time?</summary>
    public bool Overlaps(float now, Vector3 hero)
    {
        var d = hero - center;
        d.y = 0f;
        float r = RadiusAt(now);
        if (Mathf.Abs(d.magnitude - r) > KilnmotherController.WaveThickness * 0.5f) return false;
        float ang = Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
        return Mathf.Abs(Mathf.DeltaAngle(ang, gapAngle)) > KilnmotherController.WaveGapDegrees * 0.5f;
    }

    /// <summary>Advance; damages once. True when the wave is spent.</summary>
    public bool Tick(float now, Transform hero)
    {
        if (!HitHero && hero != null && Overlaps(now, hero.position) && Diablo.IsLocalAuthority())
        {
            HitHero = true;
            int dmg = DifficultyProfile.Current.ScaleDamage(KilnmotherController.WaveDamage);
            var hp = hero.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(dmg, center);
            DiabloJuice.OnPlayerHurt(hero.position, dmg);
        }
        return RadiusAt(now) > KilnmotherController.WaveMaxRadius;
    }
}
