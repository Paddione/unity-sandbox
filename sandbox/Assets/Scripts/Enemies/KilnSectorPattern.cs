using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Solo-Kindled finale sector pattern (cinderreach-kindled-finale S1):
/// replaces fast waves + random ground fields in Kilnmother phase 2 on
/// Kindled. Four 90-degree sectors around the boss, up to two adjacent
/// active per wave. 1.5 s warning, then 2 s burn. Wave order derives from
/// the run seed. The same geometry drives marking and hits; at most one
/// hit per pattern. Effects are cleared on death/respawn/retreat by
/// <see cref="KilnmotherController.ClearSectorPatterns"/>.
/// </summary>
public sealed class KilnSectorPattern
{
    public const int SectorCount = 4;
    public const float SectorDegrees = 90f;
    public const float WarnSeconds = 1.5f;
    public const float BurnSeconds = 2f;
    public const float Radius = 10f;
    public const int Damage = 2;
    public const float PatternInterval = 5f;

    public static int PatternsFired;

    public readonly Vector3 center;
    public readonly float bornAt;
    public readonly List<int> sectors;
    public bool HitConsumed { get; private set; }

    public KilnSectorPattern(Vector3 center, float bornAt, List<int> sectors)
    {
        this.center = center;
        this.bornAt = bornAt;
        this.sectors = new List<int>(sectors);
        PatternsFired++;
    }

    /// <summary>Deterministic wave layout from the run seed: 1-2 adjacent sectors.</summary>
    public static List<int> ActiveSectors(int runSeed, int waveIndex)
    {
        var rng = new System.Random(runSeed + waveIndex * 7919);
        int start = rng.Next(0, SectorCount);
        int count = 1 + rng.Next(0, 2);
        var out_ = new List<int> { start };
        if (count > 1) out_.Add((start + 1) % SectorCount);
        return out_;
    }

    public static float AngleOf(Vector3 d) => Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;

    /// <summary>Sector index (0-3) of a direction, 0 = [-45,45) degrees.</summary>
    public static int SectorOf(Vector3 d)
    {
        float a = AngleOf(d);
        int s = Mathf.FloorToInt((a + 45f) / SectorDegrees);
        s %= SectorCount;
        if (s < 0) s += SectorCount;
        return s;
    }

    /// <summary>Shared warn/hit geometry: inside radius and in an active sector.</summary>
    public bool Contains(Vector3 point)
    {
        var d = point - center;
        d.y = 0f;
        if (d.magnitude > Radius) return false;
        if (d.magnitude < 0.01f) return true;
        return sectors.Contains(SectorOf(d));
    }

    public bool IsWarning(float now) => now - bornAt < WarnSeconds;
    public bool IsBurning(float now) => now - bornAt >= WarnSeconds && now - bornAt < WarnSeconds + BurnSeconds;
    public bool IsExpired(float now) => now - bornAt >= WarnSeconds + BurnSeconds;

    /// <summary>Burn damage, at most once per pattern. True when expired.</summary>
    public bool Tick(float now, Vector3 heroPos, System.Action onHit)
    {
        if (!HitConsumed && IsBurning(now) && Contains(heroPos) && Diablo.IsLocalAuthority())
        {
            HitConsumed = true;
            int dmg = DifficultyProfile.Current.ScaleDamage(Damage);
            onHit?.Invoke();
            var hero = HeroLocator.Solo();
            var hp = hero != null ? hero.GetComponent<PlayerHealth>() : null;
            if (hp != null) hp.TakeDamage(dmg);
            else if (Application.isPlaying) DiabloJuice.OnPlayerHurt(heroPos, dmg);
        }
        return IsExpired(now);
    }

    public static void ResetRun() => PatternsFired = 0;
}
