using UnityEngine;

/// <summary>
/// The moor's air: a looping wind-and-drone bed and drifting embers around
/// the local hero, so the world never sits silent or still. One instance,
/// follows the hero (re-targets when the network hero takes over). Also
/// hosts the level-up ember pillar so it reuses the same pooled system.
/// </summary>
public sealed class DiabloAmbience : MonoBehaviour
{
    public const float BedVolume = 0.22f;
    public const int MotesPerSecond = 12;

    static DiabloAmbience instance;

    Transform target;
    AudioSource bed;
    ParticleSystem motes;
    ParticleSystem pillar;

    public static DiabloAmbience Instance => instance;
    public AudioSource Bed => bed;
    public ParticleSystem Motes => motes;

    /// <summary>Silent in tests (SetUpFixture mute) and batchmode runs: the
    /// looping wind-and-drone bed is the PlayMode run's background drone.</summary>
    static bool AmbienceMuted
    {
        get
        {
            try { return DiabloAudio.MutePlayback || Application.isBatchMode; }
            catch { return DiabloAudio.MutePlayback; }
        }
    }

    public static DiabloAmbience Ensure(Transform hero)
    {
        if (instance == null)
        {
            var go = new GameObject("DiabloAmbience");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DiabloAmbience>();
            instance.Build();
        }
        instance.target = hero;
        return instance;
    }

    void Build()
    {
        bed = gameObject.AddComponent<AudioSource>();
        bed.clip = DiabloAudio.AmbientClip;
        bed.loop = true;
        bed.volume = BedVolume;
        bed.spatialBlend = 0f;
        if (!AmbienceMuted) bed.Play();

        motes = NewSystem("Embers", "SkillArt/FireAdditive", 80);
        var main = motes.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            DiabloPalette.TrimGold, new Color(1f, 0.42f, 0.12f, 1f));
        var emission = motes.emission;
        emission.enabled = true;
        emission.rateOverTime = MotesPerSecond;
        var shape = motes.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(22f, 0.5f, 22f);
        // All three axes in the same curve mode (mixed modes flood the console).
        var vel = motes.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.35f);
        vel.y = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        var noise = motes.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.4f;
        FadeInOut(motes);
        motes.Play();

        pillar = NewSystem("LevelPillar", "SkillArt/FireAdditive", 200);
        var pm = pillar.main;
        pm.gravityModifier = -0.4f;
        var pe = pillar.emission;
        pe.enabled = false;
        FadeInOut(pillar);
        pillar.Play();
    }

    static ParticleSystem NewSystem(string name, string material, int max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(instance.transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max;
        var shape = ps.shape;
        shape.enabled = false;
        var mat = Resources.Load<Material>(material);
        if (mat != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    static void FadeInOut(ParticleSystem ps)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
    }

    /// <summary>S1 light: ambience bed volume per zone (ActProgress id). No API break: <c>Ensure(Transform)</c> untouched.</summary>
    public static float BedVolumeForZone(int zone)
    {
        switch (zone)
        {
            case ActProgress.Ossuary: return 0.18f;
            case ActProgress.Kiln: return 0.15f;
            default: return BedVolume;
        }
    }

    /// <summary>S1 light: retune the bed for a zone and start the atmosphere cross-fade (headless-callable).</summary>
    public static void ApplyZone(int zone)
    {
        if (instance != null && instance.bed != null)
            instance.bed.volume = BedVolumeForZone(zone);
        var atmo = ZoneAtmosphere.Instance;
        if (atmo != null) atmo.ShowZone(zone);
        else if (Application.isPlaying) ZoneAtmosphere.Ensure().ShowZone(zone);
    }

    /// <summary>Level-up: a column of embers rising around the hero.</summary>
    public static void LevelPillar(Vector3 at)
    {
        if (instance == null || instance.pillar == null) return;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < 60; i++)
        {
            var ring = Random.insideUnitCircle * 0.9f;
            ep.position = at + new Vector3(ring.x, Random.Range(0f, 0.4f), ring.y);
            ep.velocity = new Vector3(ring.x * 0.4f, Random.Range(2.5f, 5f), ring.y * 0.4f);
            ep.startLifetime = Random.Range(0.9f, 1.3f);
            ep.startSize = Random.Range(0.14f, 0.3f);
            ep.startColor = Color.Lerp(DiabloPalette.TrimGold, new Color(1f, 0.95f, 0.75f, 1f), Random.value);
            instance.pillar.Emit(ep, 1);
        }
    }

    void LateUpdate()
    {
        // Follow this process's hero only: remote mirrors keep clone names.
        if (target == null || !target.gameObject.activeInHierarchy || target.name != DiabloNetworkPlayer.HeroName)
        {
            var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
            if (hero != null) target = hero.transform;
        }
        if (target != null && motes != null)
            motes.transform.position = target.position + Vector3.up * 0.2f;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
