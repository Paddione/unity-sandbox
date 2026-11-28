using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SkillVfxToolingTests
{
    static readonly string[] ExpectedShaders =
    {
        "Cinderreach/VFX/AbilityMesh",
        "Cinderreach/VFX/AbilityFlipbook",
        "Cinderreach/VFX/AbilityGroundZone",
    };

    static readonly string[] ExpectedMeshes =
    {
        "VfxSlashArc360.fbx",
        "VfxSlashCone90.fbx",
        "VfxShockwaveRing.fbx",
        "VfxGroundDisc.fbx",
        "VfxHookChain.fbx",
        "VfxChargeChevron.fbx",
        "VfxShieldDome.fbx",
        "VfxLanternOrb.fbx",
        "VfxBoltStreak.fbx",
        "VfxPowderKeg.fbx",
        "VfxFlaskProjectile.fbx",
        "VfxHealPillar.fbx",
    };

    static readonly string[] ExpectedTextures =
    {
        "VfxNoise_PerlinTile.png",
        "VfxNoise_VoronoiCracks.png",
        "VfxNoise_EnergyStreaks.png",
        "VfxMask_RadialRing.png",
        "VfxMask_SoftGlow.png",
        "VfxFlipbook_FireBlast_4x4.png",
        "VfxFlipbook_SmokePuff_4x4.png",
        "VfxFlipbook_MireSplash_4x4.png",
        "VfxFlipbook_ZeroMotion.png",
    };

    [Test]
    public void AbilityShaders_ExistAndCompileWithoutError()
    {
        foreach (var shaderName in ExpectedShaders)
        {
            var shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader, $"Missing shader: {shaderName}");
            Assert.AreNotEqual("Hidden/InternalErrorShader", shader.name, $"Error shader for {shaderName}");
            Assert.Greater(shader.subshaderCount, 0, $"Shader has no subshaders: {shaderName}");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), $"Shader compile error in {shaderName}");
        }
    }

    [Test]
    public void VfxMeshes_ExistWithUvsAndLowPolyBudget()
    {
        foreach (var file in ExpectedMeshes)
        {
            var assetPath = $"Assets/Art/SkillVfx/Meshes/{file}";
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Art/SkillVfx/Meshes", file)),
                $"Missing FBX on disk: {assetPath}");
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.IsNotNull(go, $"Failed to load FBX asset: {assetPath}");
            var mf = go.GetComponentInChildren<MeshFilter>();
            Assert.IsNotNull(mf, $"Missing MeshFilter in {assetPath}");
            Assert.IsNotNull(mf.sharedMesh, $"Missing sharedMesh in {assetPath}");
            Assert.Greater(mf.sharedMesh.vertexCount, 0, $"Empty mesh: {assetPath}");
            Assert.LessOrEqual(mf.sharedMesh.vertexCount, 1500, $"Vertex budget exceeded: {assetPath}");
            Assert.Greater(mf.sharedMesh.uv.Length, 0, $"Missing UV0 channel: {assetPath}");
        }
    }

    [Test]
    public void VfxTexturesAndFlipbooks_ExistWithinSizeAndNotBlank()
    {
        foreach (var file in ExpectedTextures)
        {
            var assetPath = $"Assets/Art/SkillVfx/Textures/{file}";
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Art/SkillVfx/Textures", file)),
                $"Missing texture on disk: {assetPath}");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Assert.IsNotNull(tex, $"Failed to load texture: {assetPath}");
            Assert.LessOrEqual(tex.width, 1024, $"Texture too wide: {assetPath}");
            Assert.LessOrEqual(tex.height, 1024, $"Texture too tall: {assetPath}");
            if (!file.Contains("ZeroMotion"))
            {
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.IsNotNull(importer, $"Missing TextureImporter: {assetPath}");
                Assert.AreNotEqual(TextureImporterType.Sprite, importer.textureType,
                    $"VFX texture must not be imported as UI Sprite: {assetPath}");
            }
        }
    }
}
