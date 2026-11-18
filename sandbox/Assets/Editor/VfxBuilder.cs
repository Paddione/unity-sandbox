using UnityEditor;
using UnityEngine;

/// <summary>
/// S3 presentation VFX prefabs under Assets/Art/Vfx (runtime decoration for
/// the pooled <see cref="PresentationVfx"/> looks): FireballTrail,
/// ArrowTrail, CleaveBurst, BossTelegraph, PhaseTwoBurst, LevelUpPillar and
/// RareBeam. Plain URP particle prefabs only - no VFX Graph, so
/// Linux-llvmpipe runs stay portable. Particle materials are references to
/// the SkillArt builder output (SkillArt/FireAdditive + FireSmoke); colors
/// come from <see cref="DiabloPalette"/>, never a white default. Every
/// velocityOverLifetime uses the same curve mode on all three axes (mixed
/// modes flood the console - craft-game-floors vfx failure mode).
/// Batchmode: -executeMethod VfxBuilder.Build
/// </summary>
public static class VfxBuilder
{
    const string Dir = "Assets/Art/Vfx";
    const string AdditivePath = "Assets/Resources/SkillArt/FireAdditive.mat";
    const string SmokePath = "Assets/Resources/SkillArt/FireSmoke.mat";

    [MenuItem("Sandbox/Diablo/Build Presentation VFX")]
    public static void Build()
    {
        if (!System.IO.Directory.Exists("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!System.IO.Directory.Exists(Dir))
            AssetDatabase.CreateFolder("Assets/Art", "Vfx");

        var additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
        var smoke = AssetDatabase.LoadAssetAtPath<Material>(SmokePath);
        if (additive == null || smoke == null)
        {
            Debug.LogWarning("[Vfx] SkillArt materials missing; run Sandbox/Diablo/Build Skill Art first.");
            return;
        }

        BuildTrail("FireballTrail", additive, new Color(1f, 0.6f, 0.15f), new Color(1f, 0.35f, 0.05f),
            0.35f, 0.5f, 0.14f, 14f);
        BuildTrail("ArrowTrail", smoke, DiabloPalette.Parchment, DiabloPalette.ParchmentDim,
            0.25f, 0.4f, 0.08f, 8f);
        BuildRing("CleaveBurst", additive, DiabloPalette.TrimGold, new Color(1f, 0.45f, 0.1f),
            0.5f, 60, 1.0f);
        BuildRing("BossTelegraph", additive, DiabloPalette.Blood, DiabloPalette.TrimGold,
            0.8f, 48, 1.2f);
        BuildBurst("PhaseTwoBurst", additive, DiabloPalette.TrimGold, DiabloPalette.Blood,
            0.8f, 80, 6f, 0.2f);
        BuildPillar("LevelUpPillar", additive, DiabloPalette.TrimGold, new Color(1f, 0.95f, 0.75f, 1f),
            1.2f, 60, 4f, 0.9f);
        BuildPillar("RareBeam", additive, DiabloPalette.Rarity(ItemRarity.Rare), DiabloPalette.Parchment,
            1.2f, 60, 4.5f, 0.4f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Vfx] Built FireballTrail, ArrowTrail, CleaveBurst, BossTelegraph, PhaseTwoBurst, LevelUpPillar, RareBeam.");
    }

    static void BuildTrail(string name, Material mat, Color from, Color to,
        float lifetime, float speed, float size, float rate)
    {
        var root = new GameObject(name);
        var ps = Particles(root.transform, "Trail", mat, 200);
        var m = ps.main;
        m.loop = true;
        m.startLifetime = lifetime;
        m.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        m.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.3f);
        m.startColor = new ParticleSystem.MinMaxGradient(from, to);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        var e = ps.emission;
        e.enabled = true;
        e.rateOverTime = 0f;
        e.rateOverDistance = rate;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.12f;
        SameModeVelocity(ps, 0.2f, 0.4f);
        SizeFade(ps, 1f, 0.1f);
        ColorFade(ps, from, to);
        Save(root, name);
    }

    static void BuildRing(string name, Material mat, Color from, Color to,
        float lifetime, int burst, float radius)
    {
        var root = new GameObject(name);
        var ps = Particles(root.transform, "Ring", mat, 200);
        var m = ps.main;
        m.loop = false;
        m.duration = lifetime;
        m.startLifetime = lifetime;
        m.startSpeed = radius * 2f;
        m.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        m.startColor = new ParticleSystem.MinMaxGradient(from, to);
        var e = ps.emission;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = radius;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.radial = new ParticleSystem.MinMaxCurve(3f);
        SameModeVelocity(ps, 0f, 0f, keepRadial: true);
        SizeFade(ps, 1f, 0.2f);
        ColorFade(ps, from, to);
        Save(root, name);
    }

    static void BuildBurst(string name, Material mat, Color from, Color to,
        float lifetime, int burst, float speed, float size)
    {
        var root = new GameObject(name);
        var ps = Particles(root.transform, "Burst", mat, 300);
        var m = ps.main;
        m.loop = false;
        m.duration = lifetime;
        m.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
        m.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        m.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.3f);
        m.startColor = new ParticleSystem.MinMaxGradient(from, to);
        var e = ps.emission;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.3f;
        SameModeVelocity(ps, 0.3f, 0.6f);
        SizeFade(ps, 1f, 0.3f);
        ColorFade(ps, from, to);
        Save(root, name);
    }

    static void BuildPillar(string name, Material mat, Color from, Color to,
        float lifetime, int burst, float rise, float radius)
    {
        var root = new GameObject(name);
        var ps = Particles(root.transform, "Pillar", mat, 200);
        var m = ps.main;
        m.loop = false;
        m.duration = lifetime;
        m.gravityModifier = -0.4f;
        m.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.75f, lifetime);
        m.startSpeed = new ParticleSystem.MinMaxCurve(rise * 0.5f, rise);
        m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        m.startColor = new ParticleSystem.MinMaxGradient(from, to);
        var e = ps.emission;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = radius;
        SameModeVelocity(ps, 0.2f, 0.5f);
        SizeFade(ps, 1f, 0.4f);
        ColorFade(ps, from, to);
        Save(root, name);
    }

    /// <summary>XYZ velocity in the same two-constant mode (never mixed).</summary>
    static void SameModeVelocity(ParticleSystem ps, float spread, float up, bool keepRadial = false)
    {
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        if (!keepRadial) vel.radial = new ParticleSystem.MinMaxCurve(0f);
        vel.x = new ParticleSystem.MinMaxCurve(-spread, spread);
        vel.y = new ParticleSystem.MinMaxCurve(up * 0.5f, up);
        vel.z = new ParticleSystem.MinMaxCurve(-spread, spread);
    }

    static ParticleSystem Particles(Transform parent, string name, Material mat, int max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.maxParticles = max;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    static void SizeFade(ParticleSystem ps, float from, float to)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
    }

    static void ColorFade(ParticleSystem ps, Color from, Color to)
    {
        var c = ps.colorOverLifetime;
        c.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        c.color = g;
    }

    static void Save(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, Dir + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }
}
