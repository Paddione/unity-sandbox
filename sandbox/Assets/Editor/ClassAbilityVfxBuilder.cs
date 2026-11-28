using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Deterministically builds stylized dark-fantasy materials and prefabs for all
/// 18 class abilities (+ variants) from the Goal 1 O1+flipbook assets.
/// Outputs materials to Assets/Art/SkillVfx/Materials/ and prefabs to
/// Assets/AddressableAssets/SkillVfx/ (registered via AddressablesSetup.Ensure).
/// Batchmode: -executeMethod ClassAbilityVfxBuilder.Build
/// </summary>
public static class ClassAbilityVfxBuilder
{
    const string MeshDir = "Assets/Art/SkillVfx/Meshes";
    const string TexDir = "Assets/Art/SkillVfx/Textures";
    const string MatDir = "Assets/Art/SkillVfx/Materials";
    const string PrefabDir = "Assets/AddressableAssets/SkillVfx";

    const string MeshShaderName = "Cinderreach/VFX/AbilityMesh";
    const string FlipbookShaderName = "Cinderreach/VFX/AbilityFlipbook";
    const string ZoneShaderName = "Cinderreach/VFX/AbilityGroundZone";

    static readonly Color BogOchre = new Color(0.46f, 0.42f, 0.16f, 1f);
    static readonly Color BogRim = new Color(0.78f, 0.62f, 0.22f, 1f);
    static readonly Color HealEmerald = new Color(0.32f, 0.92f, 0.56f, 1f);
    static readonly Color HealGold = new Color(0.96f, 0.85f, 0.36f, 1f);
    static readonly Color ShadowIndigo = new Color(0.46f, 0.32f, 0.85f, 1f);
    static readonly Color AshSmoke = new Color(0.55f, 0.57f, 0.64f, 1f);
    static readonly Color EmberOrange = new Color(1.0f, 0.46f, 0.12f, 1f);

    [MenuItem("Sandbox/Diablo/Build Class Ability VFX")]
    public static void Build()
    {
        EnsureFolders();

        var meshShader = Shader.Find(MeshShaderName);
        var flipShader = Shader.Find(FlipbookShaderName);
        var zoneShader = Shader.Find(ZoneShaderName);
        if (meshShader == null || flipShader == null || zoneShader == null)
            throw new System.Exception("Missing Cinderreach/VFX/* shader; import Goal 1 assets first.");

        var noisePerlin = Tex("VfxNoise_PerlinTile.png");
        var noiseCracks = Tex("VfxNoise_VoronoiCracks.png");
        var noiseStreaks = Tex("VfxNoise_EnergyStreaks.png");
        var maskRing = Tex("VfxMask_RadialRing.png");
        var maskGlow = Tex("VfxMask_SoftGlow.png");
        var fbFire = Tex("VfxFlipbook_FireBlast_4x4.png");
        var fbSmoke = Tex("VfxFlipbook_SmokePuff_4x4.png");
        var fbMire = Tex("VfxFlipbook_MireSplash_4x4.png");
        var zeroMotion = Tex("VfxFlipbook_ZeroMotion.png");

        // Shared flipbook particle materials
        var matFireFb = FlipbookMat("Mat_Flipbook_FireBlast", flipShader, fbFire, zeroMotion, EmberOrange, 2.2f, 18f, additive: true);
        var matGoldFb = FlipbookMat("Mat_Flipbook_GoldSpark", flipShader, fbFire, zeroMotion, DiabloPalette.GoldBright, 2.0f, 18f, additive: true);
        var matSmokeFb = FlipbookMat("Mat_Flipbook_SmokePuff", flipShader, fbSmoke, zeroMotion, new Color(0.75f, 0.78f, 0.85f, 1f), 1.65f, 12f, additive: false);
        var matShadowFb = FlipbookMat("Mat_Flipbook_ShadowSmoke", flipShader, fbSmoke, zeroMotion, ShadowIndigo, 1.5f, 14f, additive: false);
        var matMireFb = FlipbookMat("Mat_Flipbook_MireSplash", flipShader, fbMire, zeroMotion, BogRim, 1.75f, 16f, additive: false);
        var matVenomFb = FlipbookMat("Mat_Flipbook_VenomSplash", flipShader, fbMire, zeroMotion, DiabloPalette.RogueVerdant, 2.1f, 18f, additive: true);

        // Shared soft-glow particle materials (fresnelInt = 0 keeps radial billboards crisp without square edges)
        var matSteelSparks = MeshMat("Mat_Particle_SteelGold", meshShader, maskGlow, noiseStreaks,
            DiabloPalette.FenSteel, DiabloPalette.TrimGold, 2.2f, Vector4.zero, additive: true, fresnelPow: 2f, fresnelInt: 0f);
        var matEmberSparks = MeshMat("Mat_Particle_Ember", meshShader, maskGlow, noisePerlin,
            DiabloPalette.LanternEmber, DiabloPalette.GoldBright, 2.4f, Vector4.zero, additive: true, fresnelPow: 2f, fresnelInt: 0f);
        var matHealSparks = MeshMat("Mat_Particle_Heal", meshShader, maskGlow, noisePerlin,
            HealEmerald, HealGold, 2.2f, Vector4.zero, additive: true, fresnelPow: 2f, fresnelInt: 0f);
        var matVenomSparks = MeshMat("Mat_Particle_Venom", meshShader, maskGlow, noisePerlin,
            DiabloPalette.RogueVerdant, HealGold, 2.2f, Vector4.zero, additive: true, fresnelPow: 2f, fresnelInt: 0f);

        // ---------------- Fenwalker (6 skills + CleaveCone90) ----------------
        BuildMeshBurst("FenCleave360", "VfxSlashArc360",
            MeshMat("Mat_Fen_Cleave360", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.FenSteel, DiabloPalette.TrimGold, 2.1f, new Vector4(1.4f, 0.2f, -0.6f, 0.4f), additive: true),
            matSteelSparks, DiabloPalette.FenSteel, 24, 3.6f, 0.14f, 5f, 3.5f);

        BuildMeshBurst("FenCleaveCone90", "VfxSlashCone90",
            MeshMat("Mat_Fen_CleaveCone90", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.TrimGold, DiabloPalette.FenSteel, 2.4f, new Vector4(1.8f, 0.1f, -0.8f, 0.3f), additive: true),
            matSteelSparks, DiabloPalette.TrimGold, 24, 4.0f, 0.14f, 5.5f, 3.8f);

        BuildHookChain("FenMireHook",
            MeshMat("Mat_Fen_HookChain", meshShader, noiseStreaks, noiseCracks,
                DiabloPalette.FenSteel, BogRim, 1.9f, new Vector4(-2.0f, 0f, 0.5f, 0f), additive: false),
            matMireFb);

        BuildSlam("FenSeismicSlam",
            MeshMat("Mat_Fen_SlamWave", meshShader, noiseCracks, noisePerlin,
                BogRim, DiabloPalette.TrimGold, 2.1f, new Vector4(0.3f, 0.4f, -0.5f, 0.3f), additive: true),
            ZoneMat("Mat_Fen_SlamGround", zoneShader, maskRing, noiseCracks,
                BogOchre, DiabloPalette.TrimGold, 1.7f, 0.12f, 0.5f, additive: false),
            matMireFb);

        BuildMeshBurst("FenBogCharge", "VfxChargeChevron",
            MeshMat("Mat_Fen_BogCharge", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.FenSteel, BogRim, 2.0f, new Vector4(0f, -1.8f, 0.2f, -0.8f), additive: true),
            matMireFb, BogRim, 22, 3.2f, 0.28f, 4.5f, 3.0f);

        BuildGroundZone("FenMireGround",
            ZoneMat("Mat_Fen_MireGround", zoneShader, noisePerlin, noiseCracks,
                new Color(0.65f, 0.56f, 0.18f, 1f), DiabloPalette.TrimGold, 2.1f, 0.12f, 0.92f, additive: false),
            matMireFb, BogRim, 22, 2.8f);

        BuildIronVow("FenIronVow",
            MeshMat("Mat_Fen_IronVowDome", meshShader, noiseCracks, noisePerlin,
                DiabloPalette.FenSteel, DiabloPalette.TrimGold, 1.8f, new Vector4(0.25f, 0.45f, -0.2f, 0.3f), additive: true, fresnelPow: 1.8f, fresnelInt: 1.8f),
            ZoneMat("Mat_Fen_IronVowRing", zoneShader, maskRing, noiseStreaks,
                DiabloPalette.FenSteel, DiabloPalette.TrimGold, 1.6f, 0.08f, 0.35f, additive: true),
            matSteelSparks);

        // ---------------- Lanternbearer (6 skills + WispStrike + HaloWard) ----------------
        BuildLanternBolt("LanternBoltVfx",
            MeshMat("Mat_Lantern_BoltCore", meshShader, maskGlow, noisePerlin,
                DiabloPalette.GoldBright, DiabloPalette.LanternEmber, 2.8f, new Vector4(0.5f, 1.2f, -0.4f, 0.6f), additive: true, fresnelPow: 1.4f, fresnelInt: 1.5f),
            MeshMat("Mat_Lantern_BoltTail", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.LanternEmber, DiabloPalette.Parchment, 2.3f, new Vector4(-2.2f, 0f, -0.6f, 0f), additive: true),
            matEmberSparks);

        BuildLanternWisp("LanternWispVfx",
            MeshMat("Mat_Lantern_WispOrb", meshShader, maskGlow, noisePerlin,
                DiabloPalette.GoldBright, DiabloPalette.Parchment, 2.6f, new Vector4(0.3f, 0.9f, -0.3f, 0.5f), additive: true),
            MeshMat("Mat_Lantern_WispHalo", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.LanternEmber, DiabloPalette.GoldBright, 2.0f, new Vector4(1.5f, 0f, -0.5f, 0f), additive: true),
            matEmberSparks);

        BuildHookChain("LanternWispStrike",
            MeshMat("Mat_Lantern_WispStrike", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.GoldBright, DiabloPalette.Parchment, 2.8f, new Vector4(3.0f, 0f, -1.0f, 0f), additive: true),
            matGoldFb);

        BuildSlam("LanternSearingHalo",
            MeshMat("Mat_Lantern_HaloWave", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.LanternEmber, DiabloPalette.GoldBright, 2.3f, new Vector4(1.2f, 0.2f, -0.8f, 0.3f), additive: true),
            ZoneMat("Mat_Lantern_HaloZone", zoneShader, maskRing, noisePerlin,
                DiabloPalette.LanternEmber, DiabloPalette.GoldBright, 1.9f, 0.1f, 0.45f, additive: true),
            matFireFb);

        BuildGroundZone("LanternHaloWard",
            ZoneMat("Mat_Lantern_HaloWard", zoneShader, maskRing, noisePerlin,
                DiabloPalette.LanternEmber, DiabloPalette.GoldBright, 1.85f, 0.08f, 0.42f, additive: true),
            matGoldFb, DiabloPalette.LanternEmber, 18, 2.8f);

        BuildPillarBurst("LanternVeilStep", "VfxHealPillar",
            MeshMat("Mat_Lantern_VeilRift", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.Parchment, DiabloPalette.FenSteel, 2.1f, new Vector4(0.8f, 1.6f, -0.4f, 0.8f), additive: true),
            matSmokeFb, DiabloPalette.Parchment, new Vector3(0.55f, 0.65f, 0.55f), 20, 0.45f);

        BuildPillarBurst("LanternMendingLight", "VfxHealPillar",
            MeshMat("Mat_Lantern_MendPillar", meshShader, noiseStreaks, noisePerlin,
                HealEmerald, HealGold, 2.2f, new Vector4(0.4f, 1.2f, -0.3f, 0.5f), additive: true),
            matHealSparks, HealEmerald, new Vector3(0.85f, 1.0f, 0.85f), 26, 0.2f);

        BuildGroundZone("LanternRenewingCircle",
            ZoneMat("Mat_Lantern_RenewCircle", zoneShader, maskRing, noisePerlin,
                HealEmerald, HealGold, 1.9f, 0.09f, 0.52f, additive: true),
            matHealSparks, HealEmerald, 28, 3.2f);

        // ---------------- Rogue (6 skills + PowderKegBlast) ----------------
        BuildStreak("RogueQuickShotTrail",
            MeshMat("Mat_Rogue_QuickShot", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.FenSteel, DiabloPalette.Parchment, 2.3f, new Vector4(-2.6f, 0f, -0.8f, 0f), additive: true),
            matSteelSparks);

        BuildFlurry("RogueBladeFlurry",
            MeshMat("Mat_Rogue_BladeFlurry", meshShader, noiseStreaks, noisePerlin,
                DiabloPalette.FenSteel, DiabloPalette.Blood, 2.5f, new Vector4(2.4f, 0.2f, -1.0f, 0.4f), additive: true),
            matSteelSparks);

        BuildGroundZone("RogueSmokePot",
            ZoneMat("Mat_Rogue_SmokeZone", zoneShader, noisePerlin, noiseCracks,
                new Color(0.32f, 0.34f, 0.42f, 1f), AshSmoke, 1.5f, 0.09f, 0.65f, additive: false),
            matSmokeFb, AshSmoke, 30, 2.8f, particleSize: 1.05f);

        BuildMeshBurst("RogueShadowRoll", "VfxChargeChevron",
            MeshMat("Mat_Rogue_ShadowRoll", meshShader, noiseStreaks, noisePerlin,
                ShadowIndigo, DiabloPalette.Parchment, 2.1f, new Vector4(0f, -2.0f, 0.3f, -1.0f), additive: true),
            matShadowFb, ShadowIndigo, 20, 2.8f, 0.38f, 4.0f, 2.6f);

        BuildGroundZone("RogueVenomVial",
            ZoneMat("Mat_Rogue_VenomZone", zoneShader, maskRing, noiseCracks,
                new Color(0.18f, 0.55f, 0.16f, 1f), DiabloPalette.RogueVerdant, 1.95f, 0.1f, 0.72f, additive: false),
            matVenomFb, DiabloPalette.RogueVerdant, 24, 3.0f);

        BuildPowderKegBody("RoguePowderKegBody", meshShader, noisePerlin, matEmberSparks);

        BuildPowderKegBlast("RoguePowderKegBlast",
            MeshMat("Mat_Rogue_KegWave", meshShader, noiseCracks, noisePerlin,
                EmberOrange, DiabloPalette.GoldBright, 2.5f, new Vector4(0.6f, 0.5f, -0.8f, 0.4f), additive: true),
            matFireFb, matSmokeFb);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ClassAbilityVfx] Built all 22 class ability VFX prefabs and materials.");
    }

    // ---------- prefab builders ----------

    static void BuildMeshBurst(string name, string meshName, Material meshMat, Material particleMat,
        Color lightColor, int burstCount, float particleSpeed, float particleSize, float lightRange, float lightIntensity)
    {
        var root = new GameObject(name);
        AddMeshChild(root.transform, meshName, meshMat);
        var ps = AddParticles(root.transform, "Sparks", particleMat, 48, false, 0.38f, particleSpeed, particleSize, lightColor, Color.white);
        SetBurst(ps, burstCount, 0.5f);
        AddPointLight(root, lightColor, lightRange, lightIntensity, Vector3.up * 0.4f);
        SavePrefab(root, name);
    }

    static void BuildHookChain(string name, Material chainMat, Material tipParticleMat)
    {
        var root = new GameObject(name);
        AddMeshChild(root.transform, "VfxHookChain", chainMat);
        var ps = AddParticles(root.transform, "ImpactSplash", tipParticleMat, 32, false, 0.35f, 2.4f, 0.32f, Color.white, BogRim);
        ps.transform.localPosition = new Vector3(0f, 0f, 1.0f);
        SetBurst(ps, 14, 0.15f);
        AddPointLight(root, BogRim, 4f, 2.8f, new Vector3(0f, 0.2f, 0.8f));
        SavePrefab(root, name);
    }

    static void BuildSlam(string name, Material waveMat, Material groundMat, Material particleMat)
    {
        var root = new GameObject(name);
        AddMeshChild(root.transform, "VfxShockwaveRing", waveMat);
        AddMeshChild(root.transform, "VfxGroundDisc", groundMat);
        var ps = AddParticles(root.transform, "Burst", particleMat, 48, false, 0.55f, 3.6f, 0.55f, Color.white, DiabloPalette.TrimGold);
        SetBurst(ps, 24, 0.4f);
        AddPointLight(root, DiabloPalette.TrimGold, 6f, 3.8f, Vector3.up * 0.4f);
        SavePrefab(root, name);
    }

    static void BuildGroundZone(string name, Material zoneMat, Material particleMat, Color tint, int rateOrBurst, float lightIntensity, float particleSize = 0.65f)
    {
        var root = new GameObject(name);
        AddMeshChild(root.transform, "VfxGroundDisc", zoneMat);
        var ps = AddParticles(root.transform, "ZoneMist", particleMat, 40, true, 0.85f, 0.8f, particleSize, tint, Color.white);
        var em = ps.emission;
        em.rateOverTime = rateOrBurst;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 0.85f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        AddPointLight(root, tint, 5.5f, lightIntensity, Vector3.up * 0.6f);
        SavePrefab(root, name);
    }

    static void BuildIronVow(string name, Material domeMat, Material ringMat, Material particleMat)
    {
        var root = new GameObject(name);
        var dome = AddMeshChild(root.transform, "VfxShieldDome", domeMat);
        dome.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
        var ring = AddMeshChild(root.transform, "VfxGroundDisc", ringMat);
        ring.transform.localScale = Vector3.one * 1.45f;
        var ps = AddParticles(root.transform, "VowSparks", particleMat, 36, true, 0.7f, 1.8f, 0.16f, DiabloPalette.FenSteel, DiabloPalette.TrimGold);
        var em = ps.emission;
        em.rateOverTime = 24f;
        AddPointLight(root, DiabloPalette.FenSteel, 6f, 3.2f, Vector3.up * 0.9f);
        SavePrefab(root, name);
    }

    static void BuildLanternBolt(string name, Material coreMat, Material tailMat, Material particleMat)
    {
        var root = new GameObject(name);
        var orb = AddMeshChild(root.transform, "VfxLanternOrb", coreMat);
        orb.transform.localScale = Vector3.one * 0.75f;
        var streak = AddMeshChild(root.transform, "VfxBoltStreak", tailMat);
        streak.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        streak.transform.localScale = new Vector3(0.9f, 0.9f, 1.2f);
        var ps = AddParticles(root.transform, "Embers", particleMat, 32, true, 0.35f, 0.6f, 0.14f, DiabloPalette.GoldBright, DiabloPalette.LanternEmber);
        var em = ps.emission;
        em.rateOverTime = 28f;
        AddPointLight(root, DiabloPalette.LanternEmber, 5f, 2.8f, Vector3.zero);
        SavePrefab(root, name);
    }

    static void BuildLanternWisp(string name, Material orbMat, Material haloMat, Material particleMat)
    {
        var root = new GameObject(name);
        var orb = AddMeshChild(root.transform, "VfxLanternOrb", orbMat);
        orb.transform.localScale = Vector3.one * 0.5f;
        var ring = AddMeshChild(root.transform, "VfxSlashArc360", haloMat);
        ring.transform.localScale = Vector3.one * 0.32f;
        var ps = AddParticles(root.transform, "WispSparks", particleMat, 20, true, 0.4f, 0.45f, 0.1f, DiabloPalette.GoldBright, DiabloPalette.Parchment);
        var em = ps.emission;
        em.rateOverTime = 16f;
        AddPointLight(root, DiabloPalette.LanternEmber, 3.5f, 1.6f, Vector3.zero);
        SavePrefab(root, name);
    }

    static void BuildPillarBurst(string name, string meshName, Material mat, Material particleMat,
        Color tint, Vector3 meshScale, int burstCount, float upSpeed)
    {
        var root = new GameObject(name);
        var col = AddMeshChild(root.transform, meshName, mat);
        col.transform.localScale = meshScale;
        var ps = AddParticles(root.transform, "Motes", particleMat, 40, false, 0.75f, 1.5f + upSpeed, 0.36f, tint, Color.white);
        SetBurst(ps, burstCount, 0.45f);
        AddPointLight(root, tint, 5f, 3.0f, Vector3.up * 1.1f);
        SavePrefab(root, name);
    }

    static void BuildStreak(string name, Material mat, Material particleMat)
    {
        var root = new GameObject(name);
        var streak = AddMeshChild(root.transform, "VfxBoltStreak", mat);
        streak.transform.localScale = new Vector3(0.65f, 0.65f, 1.15f);
        var ps = AddParticles(root.transform, "Trail", particleMat, 24, true, 0.25f, 0.5f, 0.1f, DiabloPalette.FenSteel, DiabloPalette.Parchment);
        var em = ps.emission;
        em.rateOverTime = 24f;
        SavePrefab(root, name);
    }

    static void BuildFlurry(string name, Material mat, Material particleMat)
    {
        var root = new GameObject(name);
        var slashA = AddMeshChild(root.transform, "VfxSlashCone90", mat);
        slashA.transform.localRotation *= Quaternion.Euler(0f, 0f, 16f);
        var slashB = AddMeshChild(root.transform, "VfxSlashCone90", mat);
        slashB.transform.localRotation *= Quaternion.Euler(0f, 0f, -18f);
        slashB.transform.localScale = new Vector3(-0.92f, 0.92f, 0.95f);
        var ps = AddParticles(root.transform, "SlashSparks", particleMat, 36, false, 0.32f, 4.2f, 0.13f, DiabloPalette.FenSteel, DiabloPalette.Blood);
        SetBurst(ps, 24, 0.35f);
        AddPointLight(root, DiabloPalette.FenSteel, 4.5f, 2.8f, Vector3.up * 0.2f);
        SavePrefab(root, name);
    }

    static void BuildPowderKegBody(string name, Shader meshShader, Texture2D noisePerlin, Material sparkMat)
    {
        var root = new GameObject(name);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{MeshDir}/VfxPowderKeg.fbx");
        var inst = (GameObject)Object.Instantiate(model, root.transform);
        inst.name = "PowderKegMesh";
        foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);

        var urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? meshShader;
        var woodMat = LitMat("Mat_Rogue_KegWood", urpLit, new Color(0.38f, 0.22f, 0.12f, 1f), Color.black);
        var ironMat = LitMat("Mat_Rogue_KegIron", urpLit, new Color(0.24f, 0.25f, 0.28f, 1f), Color.black);
        var fuseMat = MeshMat("Mat_Rogue_KegFuse", meshShader, Tex("VfxMask_SoftGlow.png"), noisePerlin,
            EmberOrange, DiabloPalette.GoldBright, 2.6f, new Vector4(0f, 1.5f, 0f, 0f), additive: true);

        var mr = inst.GetComponentInChildren<MeshRenderer>();
        if (mr != null) mr.sharedMaterials = new[] { woodMat, ironMat, fuseMat };

        var ps = AddParticles(root.transform, "FuseSparks", sparkMat, 24, true, 0.35f, 1.2f, 0.12f, DiabloPalette.GoldBright, EmberOrange);
        ps.transform.localPosition = new Vector3(0.05f, 0.88f, 0f);
        var em = ps.emission;
        em.rateOverTime = 24f;
        AddPointLight(root, EmberOrange, 3.2f, 2.4f, new Vector3(0.05f, 0.9f, 0f));
        SavePrefab(root, name);
    }

    static void BuildPowderKegBlast(string name, Material waveMat, Material fireFb, Material smokeFb)
    {
        var root = new GameObject(name);
        AddMeshChild(root.transform, "VfxShockwaveRing", waveMat);
        var fire = AddParticles(root.transform, "FireBurst", fireFb, 36, false, 0.62f, 3.5f, 0.95f, Color.white, EmberOrange);
        SetBurst(fire, 20, 0.35f);
        var smoke = AddParticles(root.transform, "SmokePlume", smokeFb, 24, false, 0.95f, 1.8f, 1.05f, AshSmoke, Color.white);
        SetBurst(smoke, 14, 0.45f);
        AddPointLight(root, EmberOrange, 7f, 5.5f, Vector3.up * 0.5f);
        SavePrefab(root, name);
    }

    // ---------- helpers ----------

    static GameObject AddMeshChild(Transform parent, string meshFileNoExt, Material mat)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{MeshDir}/{meshFileNoExt}.fbx");
        if (fbx == null) throw new System.Exception($"Missing VFX FBX: {MeshDir}/{meshFileNoExt}.fbx");
        var srcMf = fbx.GetComponentInChildren<MeshFilter>();
        if (srcMf == null || srcMf.sharedMesh == null) throw new System.Exception($"Missing mesh inside {meshFileNoExt}");

        var go = new GameObject(meshFileNoExt);
        go.transform.SetParent(parent, false);
        // Directional Blender +Y meshes export along Unity -Z; flip 180 deg onto +Z forward.
        if (meshFileNoExt == "VfxSlashCone90" || meshFileNoExt == "VfxHookChain" ||
            meshFileNoExt == "VfxChargeChevron" || meshFileNoExt == "VfxBoltStreak")
        {
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        go.AddComponent<MeshFilter>().sharedMesh = srcMf.sharedMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return go;
    }

    static ParticleSystem AddParticles(Transform parent, string name, Material mat, int maxParticles,
        bool looping, float lifetime, float speed, float size, Color colorA, Color colorB)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = looping;
        main.duration = Mathf.Max(0.25f, lifetime);
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Same two-constant curve mode on all 3 axes (VfxBuilder / CraftGameFloors invariant).
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.85f, 1f, 0.15f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.8f, 0.55f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    static void SetBurst(ParticleSystem ps, int count, float radius)
    {
        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = radius;
    }

    static void AddPointLight(GameObject root, Color color, float range, float intensity, Vector3 localPos)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
    }

    static Material MeshMat(string name, Shader shader, Texture2D mainTex, Texture2D noiseTex,
        Color color, Color edgeColor, float intensity, Vector4 scroll, bool additive,
        float fresnelPow = 2.2f, float fresnelInt = 0.7f)
    {
        var mat = LoadOrCreateMat(name, shader);
        mat.SetColor("_Color", color);
        mat.SetColor("_EdgeColor", edgeColor);
        mat.SetFloat("_Intensity", intensity);
        mat.SetTexture("_MainTex", mainTex);
        mat.SetTexture("_NoiseTex", noiseTex);
        mat.SetVector("_ScrollSpeed", scroll);
        mat.SetFloat("_Dissolve", 0f);
        mat.SetFloat("_EdgeWidth", 0.14f);
        mat.SetFloat("_FresnelPower", fresnelPow);
        mat.SetFloat("_FresnelIntensity", fresnelInt);
        SetBlend(mat, additive);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material FlipbookMat(string name, Shader shader, Texture2D atlas, Texture2D motion,
        Color tint, float intensity, float fps, bool additive)
    {
        var mat = LoadOrCreateMat(name, shader);
        mat.SetColor("_Color", tint);
        mat.SetFloat("_Intensity", intensity);
        mat.SetTexture("_MainTex", atlas);
        mat.SetTexture("_MotionTex", motion);
        mat.SetFloat("_MotionStrength", 0f);
        mat.SetFloat("_Rows", 4f);
        mat.SetFloat("_Cols", 4f);
        mat.SetFloat("_FPS", fps);
        mat.SetFloat("_UseManualFrame", 0f);
        mat.SetFloat("_Softness", 1f);
        SetBlend(mat, additive);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material ZoneMat(string name, Shader shader, Texture2D mask, Texture2D noise,
        Color fill, Color rim, float intensity, float rimWidth, float fillAlpha, bool additive)
    {
        var mat = LoadOrCreateMat(name, shader);
        mat.SetColor("_Color", fill);
        mat.SetColor("_RimColor", rim);
        mat.SetFloat("_Intensity", intensity);
        mat.SetTexture("_MainTex", mask);
        mat.SetTexture("_NoiseTex", noise);
        mat.SetVector("_ScrollSpeed", new Vector4(0.16f, -0.14f, 0f, 0f));
        mat.SetFloat("_PulseSpeed", 3.5f);
        mat.SetFloat("_RimWidth", rimWidth);
        mat.SetFloat("_FillAlpha", fillAlpha);
        mat.SetFloat("_Progress", 1f);
        SetBlend(mat, additive);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material LitMat(string name, Shader shader, Color baseColor, Color emission)
    {
        var mat = LoadOrCreateMat(name, shader);
        mat.SetColor("_BaseColor", baseColor);
        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material LoadOrCreateMat(string name, Shader shader)
    {
        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        return mat;
    }

    static void SetBlend(Material mat, bool additive)
    {
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    static Texture2D Tex(string file)
    {
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{file}");
        if (t == null) throw new System.Exception($"Missing VFX texture: {TexDir}/{file}");
        return t;
    }

    static void EnsureFolders()
    {
        if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);
        if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
        AssetDatabase.Refresh();
    }

    static void SavePrefab(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(root);
    }
}
