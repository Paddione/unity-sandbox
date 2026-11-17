using UnityEngine;

/// <summary>
/// A living fire for world props (campfire, braziers, torches): a warm point
/// light that flickers on two Perlin octaves and, at runtime only, a small
/// pooled ember stream. Particles are created in Awake so saved scenes keep
/// only URP/Lit renderers (DiabloZoneScene_UsesOnlyUrpLitMaterials).
/// </summary>
[RequireComponent(typeof(Light))]
public sealed class DiabloFire : MonoBehaviour
{
    public float baseIntensity = 2.2f;
    public float flicker = 0.35f;
    public float embersPerSecond = 6f;

    Light fireLight;
    float seed;
    Vector3 basePos;

    void Awake()
    {
        fireLight = GetComponent<Light>();
        seed = Random.value * 100f;
        basePos = transform.localPosition;
        if (Application.isPlaying && embersPerSecond > 0f) BuildEmbers();
    }

    void Update()
    {
        float t = Time.time * 7f + seed;
        float n = Mathf.PerlinNoise(t, seed) * 0.7f + Mathf.PerlinNoise(t * 2.7f, seed + 3f) * 0.3f;
        fireLight.intensity = baseIntensity * (1f - flicker + flicker * 2f * n);
        transform.localPosition = basePos + new Vector3(
            (Mathf.PerlinNoise(t * 0.5f, 1f) - 0.5f) * 0.06f, 0f,
            (Mathf.PerlinNoise(1f, t * 0.5f) - 0.5f) * 0.06f);
    }

    void BuildEmbers()
    {
        var go = new GameObject("Embers");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(DiabloPalette.TrimGold, new Color(1f, 0.38f, 0.08f, 1f));
        main.gravityModifier = -0.15f;
        main.maxParticles = 40;
        var emission = ps.emission;
        emission.rateOverTime = embersPerSecond;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.15f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var mat = Resources.Load<Material>("SkillArt/FireAdditive");
        if (mat != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        ps.Play();
    }
}
