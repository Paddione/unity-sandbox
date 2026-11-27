using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S3 presentation VFX: one pooled spawn API for skill trails, cleave arcs,
/// boss telegraphs/phase bursts, level-up pillars and rare beams. Pooling
/// follows <see cref="DiabloJuice"/> (one ParticleSystem per look, fed by
/// <c>Emit</c> - no Instantiate/Destroy per event) and the ember pillar in
/// <see cref="DiabloAmbience"/>. URP particle materials only
/// (SkillArt/FireAdditive + FireSmoke), no VFX Graph, so Linux-llvmpipe
/// stays portable. All colors come from <see cref="DiabloPalette"/>.
/// Event bursts live 0.3-0.8 s, milestones (pillar/beam) 1.2 s. Every
/// velocityOverLifetime uses the same curve mode on all three axes (mixed
/// modes flood the console - craft-game-floors vfx failure mode).
/// Static counters are test seams (see VfxSpawnTests).
/// </summary>
public static class PresentationVfx
{
    /// <summary>Max live particles across all pooled systems.</summary>
    public const int MaxBudget = 2000;

    public static int FireballTrails;
    public static int ArrowTrails;
    public static int CleaveArcs;
    public static int BossTelegraphs;
    public static int PhaseTwoBursts;
    public static int LevelUpPillars;
    public static int RareBeams;
    public static int VeilPuffs;

    static readonly Dictionary<string, ParticleSystem> systems = new Dictionary<string, ParticleSystem>();

    /// <summary>Fireball / Lantern Bolt launch: ember flick at the muzzle.</summary>
    public static void FireballTrail(Vector3 at)
    {
        FireballTrails++;
        var ps = SystemFor("FireTrail", "SkillArt/FireAdditive", 300, -0.2f);
        if (ps == null) return;
        for (int i = 0; i < 10; i++)
            Emit(ps, at + Random.onUnitSphere * 0.15f, Random.onUnitSphere * 1.2f,
                Random.Range(0.3f, 0.5f), Random.Range(0.1f, 0.22f),
                Color.Lerp(DiabloPalette.TrimGold, new Color(1f, 0.42f, 0.12f, 1f), Random.value));
    }

    /// <summary>Arrow launch: pale streak at the bow.</summary>
    public static void ArrowTrail(Vector3 at)
    {
        ArrowTrails++;
        var ps = SystemFor("ArrowTrail", "SkillArt/FireSmoke", 200, 0f);
        if (ps == null) return;
        for (int i = 0; i < 6; i++)
            Emit(ps, at + Random.onUnitSphere * 0.1f, Random.onUnitSphere * 0.8f,
                Random.Range(0.3f, 0.5f), Random.Range(0.06f, 0.12f),
                Color.Lerp(DiabloPalette.Parchment, DiabloPalette.ParchmentDim, Random.value));
    }

    /// <summary>Cleave / Searing Halo: expanding gold ring at the hero's feet.</summary>
    public static void CleaveArc(Vector3 at, float radius)
    {
        CleaveArcs++;
        var ps = SystemFor("CleaveArc", "SkillArt/FireAdditive", 300, 0f);
        if (ps == null) return;
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24;
            var rim = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Emit(ps, at + Vector3.up * 0.2f + rim * radius * 0.4f, rim * radius * 2.2f + Vector3.up * 0.4f,
                Random.Range(0.4f, 0.6f), Random.Range(0.2f, 0.35f),
                Color.Lerp(DiabloPalette.TrimGold, new Color(1f, 0.95f, 0.75f, 1f), Random.value));
        }
    }

    /// <summary>Boss slam: blood-red ground ring marking the radius.</summary>
    public static void BossTelegraph(Vector3 at, float radius)
    {
        BossTelegraphs++;
        var ps = SystemFor("BossTelegraph", "SkillArt/FireAdditive", 200, 0f);
        if (ps == null) return;
        for (int i = 0; i < 30; i++)
        {
            float a = i * Mathf.PI * 2f / 30;
            var rim = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Emit(ps, at + Vector3.up * 0.15f + rim * radius, -rim * 1.2f + Vector3.up * 0.3f,
                Random.Range(0.6f, 0.8f), Random.Range(0.18f, 0.3f),
                Color.Lerp(DiabloPalette.Blood, DiabloPalette.TrimGold, Random.value * 0.35f));
        }
    }

    /// <summary>Boss phase 2: ember eruption.</summary>
    public static void PhaseTwoBurst(Vector3 at)
    {
        PhaseTwoBursts++;
        var ps = SystemFor("PhaseBurst", "SkillArt/FireAdditive", 300, -0.3f);
        if (ps == null) return;
        for (int i = 0; i < 60; i++)
        {
            var dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.9f + 0.2f;
            Emit(ps, at + Vector3.up * 0.5f, dir * Random.Range(2f, 6f),
                Random.Range(0.5f, 0.8f), Random.Range(0.14f, 0.3f),
                Color.Lerp(DiabloPalette.TrimGold, DiabloPalette.Blood, Random.value * 0.5f));
        }
    }

    /// <summary>Level-up: column of embers around the hero (milestone, 1.2 s).</summary>
    public static void LevelUpPillar(Vector3 at)
    {
        LevelUpPillars++;
        DiabloAmbience.LevelPillar(at);
        var ps = SystemFor("LevelPillar", "SkillArt/FireAdditive", 200, -0.4f);
        if (ps == null) return;
        for (int i = 0; i < 40; i++)
        {
            var ring = Random.insideUnitCircle * 0.9f;
            Emit(ps, at + new Vector3(ring.x, Random.Range(0f, 0.4f), ring.y),
                new Vector3(ring.x * 0.4f, Random.Range(2.5f, 5f), ring.y * 0.4f),
                Random.Range(0.9f, 1.2f), Random.Range(0.14f, 0.3f),
                Color.Lerp(DiabloPalette.TrimGold, new Color(1f, 0.95f, 0.75f, 1f), Random.value));
        }
    }

    /// <summary>Rare/Unique drop: light pillar over the loot (milestone, 1.2 s).</summary>
    public static void RareBeam(Vector3 at, ItemRarity rarity)
    {
        RareBeams++;
        var ps = SystemFor("RareBeam", "SkillArt/FireAdditive", 300, -0.5f);
        if (ps == null) return;
        Color tint = DiabloPalette.Rarity(rarity);
        for (int i = 0; i < 40; i++)
        {
            var ring = Random.insideUnitCircle * 0.4f;
            Emit(ps, at + new Vector3(ring.x, Random.Range(0f, 0.3f), ring.y),
                new Vector3(ring.x * 0.2f, Random.Range(3f, 5.5f), ring.y * 0.2f),
                Random.Range(0.9f, 1.2f), Random.Range(0.12f, 0.26f),
                Color.Lerp(tint, DiabloPalette.Parchment, Random.value * 0.3f));
        }
    }

    /// <summary>Veil Step: ash-pale puff at each end of the blink.</summary>
    public static void VeilPuff(Vector3 at)
    {
        VeilPuffs++;
        var ps = SystemFor("VeilPuff", "SkillArt/FireSmoke", 200, -0.1f);
        if (ps == null) return;
        for (int i = 0; i < 16; i++)
            Emit(ps, at + Random.onUnitSphere * 0.3f, Random.onUnitSphere * 1.5f + Vector3.up * 0.8f,
                Random.Range(0.3f, 0.5f), Random.Range(0.15f, 0.3f),
                Color.Lerp(DiabloPalette.ParchmentDim, DiabloPalette.Parchment, Random.value));
    }

    /// <summary>Only Rare+ drops earn a beam (Magic stays quiet).</summary>
    public static bool ShouldBeamFor(ItemRarity rarity) =>
        rarity == ItemRarity.Rare || rarity == ItemRarity.Unique;

    /// <summary>Live particles across all pooled systems (budget seam).</summary>
    public static int ActiveParticles()
    {
        int n = 0;
        foreach (var kv in systems)
            if (kv.Value != null) n += kv.Value.particleCount;
        return n;
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        FireballTrails = ArrowTrails = CleaveArcs = BossTelegraphs = 0;
        PhaseTwoBursts = LevelUpPillars = RareBeams = VeilPuffs = 0;
    }

    static ParticleSystem SystemFor(string name, string material, int max, float gravity)
    {
        if (systems.TryGetValue(name, out var ps) && ps != null) return ps;
        if (!Application.isPlaying) return null;
        var go = new GameObject("PresentationVfx_" + name);
        Object.DontDestroyOnLoad(go);
        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = gravity;
        main.maxParticles = max;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;
        // All three axes in the same curve mode (mixed modes flood the console).
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0.4f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var mat = Res.Get<Material>(material);
        if (mat != null) r.sharedMaterial = mat;
        ps.Play();
        systems[name] = ps;
        return ps;
    }

    static void Emit(ParticleSystem ps, Vector3 pos, Vector3 velocity, float lifetime, float size, Color color)
    {
        var ep = new ParticleSystem.EmitParams();
        ep.position = pos;
        ep.velocity = velocity;
        ep.startLifetime = lifetime;
        ep.startSize = size;
        ep.startColor = color;
        ps.Emit(ep, 1);
    }
}
