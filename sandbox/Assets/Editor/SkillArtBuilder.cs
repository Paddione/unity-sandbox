using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the ranged-skill art into Resources/SkillArt (runtime loads it via
/// Resources.Load, so materials and shaders ship with every build):
/// ArrowProjectile (KayKit arrow_bow mesh, tip on +Z), FireballProjectile
/// (particle core, flame trail, embers, light) and FireballBlast (burst).
/// Batchmode: -executeMethod SkillArtBuilder.Build
/// </summary>
public static class SkillArtBuilder
{
    const string Dir = "Assets/Resources/SkillArt";
    const string ArrowFbx = "Assets/ThirdParty/KayKit/Adventurers/Accessories/arrow_bow.fbx";

    [MenuItem("Sandbox/Diablo/Build Skill Art")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir);
        var soft = SoftTexture();
        var additive = ParticleMaterial("FireAdditive", soft, additive: true);
        var smoke = ParticleMaterial("FireSmoke", soft, additive: false);

        BuildArrow();
        BuildFireball(additive, smoke);
        BuildBlast(additive, smoke);
        BuildHeroBow();
        UpgradeHeroController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SkillArt] Built ArrowProjectile, FireballProjectile, FireballBlast, HeroBow.");
    }

    // ---------- bow animation states ----------

    const string HeroControllerPath = "Assets/Resources/DiabloLocomotion.controller";
    const string RangedFbx = "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_CombatRanged.fbx";
    public const string BowAimState = "BowAim";
    public const string BowShootState = "BowShoot";

    /// <summary>Add the bow states to the existing hero controller in place (keeps its GUID).</summary>
    [MenuItem("Sandbox/Diablo/Upgrade Hero Controller (Bow)")]
    public static void UpgradeHeroController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(HeroControllerPath);
        if (controller == null)
        {
            Debug.LogWarning($"[SkillArt] {HeroControllerPath} missing; build the Diablo scenes first.");
            return;
        }
        AddBowStates(controller);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Bow layer of the hero state machine (idempotent): "BowShoot" trigger
    /// plays Ranged_Bow_Release from any state; while "Bow" is set and the
    /// hero stands still it holds Ranged_Bow_Aiming_Idle instead of Idle.
    /// </summary>
    public static void AddBowStates(UnityEditor.Animations.AnimatorController controller)
    {
        var aimClip = FindTake(RangedFbx, "Ranged_Bow_Aiming_Idle");
        var releaseClip = FindTake(RangedFbx, "Ranged_Bow_Release");
        if (aimClip == null || releaseClip == null)
        {
            Debug.LogWarning("[SkillArt] Ranged_Bow takes missing; hero keeps the sword chop for arrows.");
            return;
        }
        EnsureParam(controller, "Bow", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "BowShoot", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;
        foreach (var t in sm.anyStateTransitions)
            if (t.destinationState != null && (t.destinationState.name == BowAimState || t.destinationState.name == BowShootState))
                sm.RemoveAnyStateTransition(t);
        UnityEditor.Animations.AnimatorState idle = null, run = null;
        foreach (var cs in sm.states)
        {
            if (cs.state.name == BowAimState || cs.state.name == BowShootState) sm.RemoveState(cs.state);
        }
        foreach (var cs in sm.states)
        {
            if (cs.state.name == "Idle") idle = cs.state;
            if (cs.state.name == "Run") run = cs.state;
        }
        if (idle == null || run == null)
        {
            Debug.LogWarning("[SkillArt] Hero controller lacks Idle/Run; bow states skipped.");
            return;
        }
        foreach (var t in idle.transitions)
            if (t.destinationState == null || t.destinationState.name == BowAimState) idle.RemoveTransition(t);

        var aim = sm.AddState(BowAimState);
        aim.motion = aimClip;
        var shoot = sm.AddState(BowShootState);
        shoot.motion = releaseClip;
        shoot.speed = 1.6f;

        // Any state -> release (rapid fire may re-enter it).
        var toShoot = sm.AddAnyStateTransition(shoot);
        toShoot.hasExitTime = false;
        toShoot.duration = 0.05f;
        toShoot.canTransitionToSelf = true;
        toShoot.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "BowShoot");

        // Release -> hold the aim (bow still drawn) or back to idle.
        var shootToAim = shoot.AddTransition(aim);
        shootToAim.hasExitTime = true;
        shootToAim.exitTime = 0.85f;
        shootToAim.duration = 0.1f;
        shootToAim.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Bow");
        var shootToIdle = shoot.AddTransition(idle);
        shootToIdle.hasExitTime = true;
        shootToIdle.exitTime = 0.85f;
        shootToIdle.duration = 0.15f;

        // Idle <-> aim on the Bow flag; moving always wins.
        var idleToAim = idle.AddTransition(aim);
        idleToAim.hasExitTime = false;
        idleToAim.duration = 0.15f;
        idleToAim.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Bow");
        idleToAim.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "Moving");
        var aimToIdle = aim.AddTransition(idle);
        aimToIdle.hasExitTime = false;
        aimToIdle.duration = 0.2f;
        aimToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "Bow");
        var aimToRun = aim.AddTransition(run);
        aimToRun.hasExitTime = false;
        aimToRun.duration = 0.15f;
        aimToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Moving");
        Debug.Log("[SkillArt] Hero controller: BowAim + BowShoot states wired.");
    }

    static void EnsureParam(UnityEditor.Animations.AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, type);
    }

    static AnimationClip FindTake(string fbxPath, string takeName)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (obj is AnimationClip clip && clip.name == takeName)
                return clip;
        return null;
    }

    // ---------- held bow ----------

    const string BowFbx = "Assets/ThirdParty/KayKit/Adventurers/Accessories/bow_withString.fbx";

    /// <summary>
    /// Bow the hero holds while shooting. KayKit accessories are authored for
    /// the rig's handslot bones, so the mesh keeps its native transform.
    /// </summary>
    static void BuildHeroBow()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(BowFbx);
        var root = new GameObject("HeroBow");
        var inst = (GameObject)Object.Instantiate(model, root.transform);
        inst.name = "bow_withString";
        foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        Save(root, "HeroBow");
    }

    // ---------- textures & materials ----------

    static Texture2D SoftTexture()
    {
        string path = Dir + "/SoftParticle.png";
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material ParticleMaterial(string name, Texture2D tex, bool additive)
    {
        string path = $"{Dir}/{name}.mat";
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Surface", 1f); // transparent
        mat.SetFloat("_Blend", additive ? 2f : 0f); // 2 = additive, 0 = alpha
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- arrow ----------

    static void BuildArrow()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowFbx);
        var root = new GameObject("ArrowProjectile");
        var inst = (GameObject)Object.Instantiate(model, root.transform);
        inst.name = "arrow_bow";
        foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);

        // Orient the long axis onto +Z with the tip forward. The tip end is the
        // one whose extreme slice is narrowest (fletching keeps the nock wide).
        var mf = inst.GetComponentInChildren<MeshFilter>();
        var verts = mf.sharedMesh.vertices;
        var b = mf.sharedMesh.bounds;
        int axis = b.size.x >= b.size.y && b.size.x >= b.size.z ? 0 : (b.size.y >= b.size.z ? 1 : 2);
        float min = b.min[axis], max = b.max[axis], len = max - min;
        float wMin = 0f, wMax = 0f;
        foreach (var v in verts)
        {
            float w = 0f;
            for (int k = 0; k < 3; k++) if (k != axis) w = Mathf.Max(w, Mathf.Abs(v[k] - b.center[k]));
            if (v[axis] < min + len * 0.04f) wMin = Mathf.Max(wMin, w);
            if (v[axis] > max - len * 0.04f) wMax = Mathf.Max(wMax, w);
        }
        bool tipPositive = wMax < wMin;
        Vector3 tipLocal = Vector3.zero;
        tipLocal[axis] = tipPositive ? 1f : -1f;
        Vector3 tipWorld = mf.transform.TransformDirection(tipLocal);
        inst.transform.rotation = Quaternion.FromToRotation(tipWorld, Vector3.forward) * inst.transform.rotation;

        // Scale to ~0.9 m and center on the pivot.
        float worldLen = len * mf.transform.lossyScale[axis];
        float s = worldLen > 0.0001f ? 0.9f / worldLen : 1f;
        inst.transform.localScale *= s;
        var rb = inst.GetComponentInChildren<Renderer>().bounds;
        inst.transform.position -= rb.center;
        Debug.Log($"[SkillArt] arrow axis={axis} tipPositive={tipPositive} wMin={wMin:0.###} wMax={wMax:0.###} scale={s:0.###}");

        // Faint motion streak behind the arrow.
        var trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.12f;
        trail.startWidth = 0.05f;
        trail.endWidth = 0f;
        trail.material = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/FireSmoke.mat");
        trail.startColor = new Color(1f, 0.95f, 0.85f, 0.5f);
        trail.endColor = new Color(1f, 1f, 1f, 0f);
        trail.shadowCastingMode = ShadowCastingMode.Off;

        Save(root, "ArrowProjectile");
    }

    // ---------- fireball ----------

    static void BuildFireball(Material additive, Material smoke)
    {
        var root = new GameObject("FireballProjectile");

        var core = Particles(root.transform, "Core", additive);
        var m = core.main;
        m.startLifetime = 0.18f;
        m.startSpeed = 0f;
        m.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.8f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.55f, 0.1f));
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        m.simulationSpace = ParticleSystemSimulationSpace.Local;
        var e = core.emission; e.rateOverTime = 60f;
        var sh = core.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.05f;
        SizeFade(core, 1f, 0.5f);
        ColorFade(core, new Color(1f, 0.9f, 0.6f), new Color(1f, 0.3f, 0.05f));

        var flame = Particles(root.transform, "FlameTrail", additive);
        m = flame.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.15f), new Color(1f, 0.35f, 0.05f));
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        e = flame.emission; e.rateOverTime = 0f; e.rateOverDistance = 14f;
        sh = flame.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.12f;
        SizeFade(flame, 1f, 0.1f);
        ColorFade(flame, new Color(1f, 0.7f, 0.25f), new Color(0.6f, 0.08f, 0.02f));

        var embers = Particles(root.transform, "Embers", additive);
        m = embers.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        m.startColor = new Color(1f, 0.75f, 0.3f);
        m.gravityModifier = -0.15f;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        e = embers.emission; e.rateOverTime = 0f; e.rateOverDistance = 8f;
        sh = embers.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
        SizeFade(embers, 1f, 0f);

        var smokeTrail = Particles(root.transform, "Smoke", smoke);
        m = smokeTrail.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        m.startColor = new Color(0.15f, 0.12f, 0.1f, 0.35f);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        e = smokeTrail.emission; e.rateOverTime = 0f; e.rateOverDistance = 5f;
        sh = smokeTrail.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
        SizeFade(smokeTrail, 0.6f, 1.4f);
        AlphaFade(smokeTrail, new Color(0.2f, 0.16f, 0.12f), 0.35f);

        var light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.15f);
        light.range = 5f;
        light.intensity = 3f;
        light.shadows = LightShadows.None;

        Save(root, "FireballProjectile");
    }

    static void BuildBlast(Material additive, Material smoke)
    {
        var root = new GameObject("FireballBlast");

        var burst = Particles(root.transform, "Burst", additive);
        var m = burst.main;
        m.loop = false;
        m.duration = 0.3f;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.35f), new Color(1f, 0.4f, 0.05f));
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var e = burst.emission; e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
        var sh = burst.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
        var lim = burst.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.5f; lim.dampen = 0.25f;
        SizeFade(burst, 1f, 0.3f);
        ColorFade(burst, new Color(1f, 0.85f, 0.5f), new Color(0.5f, 0.06f, 0.02f));

        var sparks = Particles(root.transform, "Sparks", additive);
        m = sparks.main;
        m.loop = false;
        m.duration = 0.3f;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        m.startColor = new Color(1f, 0.8f, 0.4f);
        m.gravityModifier = 1f;
        e = sparks.emission; e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
        sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.2f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        SizeFade(sparks, 1f, 0f);

        var puff = Particles(root.transform, "Smoke", smoke);
        m = puff.main;
        m.loop = false;
        m.duration = 0.3f;
        m.startDelay = 0.08f;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        m.startColor = new Color(0.15f, 0.12f, 0.1f, 0.45f);
        m.gravityModifier = -0.1f;
        e = puff.emission; e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        sh = puff.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
        SizeFade(puff, 0.6f, 1.6f);
        AlphaFade(puff, new Color(0.18f, 0.14f, 0.11f), 0.45f);

        var light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.15f);
        light.range = 8f;
        light.intensity = 6f;
        light.shadows = LightShadows.None;

        Save(root, "FireballBlast");
    }

    // ---------- helpers ----------

    static ParticleSystem Particles(Transform parent, string name, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.maxParticles = 200;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
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

    static void AlphaFade(ParticleSystem ps, Color tint, float alpha)
    {
        var c = ps.colorOverLifetime;
        c.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, 0.2f), new GradientAlphaKey(0f, 1f) });
        c.color = g;
    }

    static void Save(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, $"{Dir}/{name}.prefab");
        Object.DestroyImmediate(root);
    }
}
