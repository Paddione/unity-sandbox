using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ClassAbilityVfxTests
{
    static readonly DiabloSkills.Skill[] All18ClassSkills =
    {
        DiabloSkills.Skill.Cleave,
        DiabloSkills.Skill.MireHook,
        DiabloSkills.Skill.SeismicSlam,
        DiabloSkills.Skill.BogCharge,
        DiabloSkills.Skill.MireGround,
        DiabloSkills.Skill.IronVow,

        DiabloSkills.Skill.LanternBolt,
        DiabloSkills.Skill.Wisps,
        DiabloSkills.Skill.SearingHalo,
        DiabloSkills.Skill.VeilStep,
        DiabloSkills.Skill.MendingLight,
        DiabloSkills.Skill.RenewingCircle,

        DiabloSkills.Skill.QuickShot,
        DiabloSkills.Skill.BladeFlurry,
        DiabloSkills.Skill.SmokePot,
        DiabloSkills.Skill.ShadowRoll,
        DiabloSkills.Skill.VenomVial,
        DiabloSkills.Skill.PowderKeg,
    };

    static readonly string[] All22Prefabs =
    {
        ClassAbilityVfx.Cleave360Address,
        ClassAbilityVfx.CleaveCone90Address,
        ClassAbilityVfx.MireHookAddress,
        ClassAbilityVfx.SeismicSlamAddress,
        ClassAbilityVfx.BogChargeAddress,
        ClassAbilityVfx.MireGroundAddress,
        ClassAbilityVfx.IronVowAddress,
        ClassAbilityVfx.LanternBoltAddress,
        ClassAbilityVfx.LanternWispAddress,
        ClassAbilityVfx.LanternWispStrikeAddress,
        ClassAbilityVfx.SearingHaloAddress,
        ClassAbilityVfx.SearingHaloWardAddress,
        ClassAbilityVfx.VeilStepAddress,
        ClassAbilityVfx.MendingLightAddress,
        ClassAbilityVfx.RenewingCircleAddress,
        ClassAbilityVfx.QuickShotTrailAddress,
        ClassAbilityVfx.BladeFlurryAddress,
        ClassAbilityVfx.SmokePotAddress,
        ClassAbilityVfx.ShadowRollAddress,
        ClassAbilityVfx.VenomVialAddress,
        ClassAbilityVfx.PowderKegBodyAddress,
        ClassAbilityVfx.PowderKegBlastAddress,
    };

    [Test]
    public void All18ClassSkills_MapToDedicatedPrefabs()
    {
        var uniqueAddresses = new HashSet<string>();
        foreach (var skill in All18ClassSkills)
        {
            string addr = ClassAbilityVfx.PrefabAddressFor(skill);
            Assert.IsFalse(string.IsNullOrEmpty(addr), $"Missing prefab address for {skill}");
            StringAssert.StartsWith("SkillVfx/", addr, $"Unexpected folder for {skill}");
            Assert.IsTrue(uniqueAddresses.Add(addr), $"Duplicate primary prefab for {skill}: {addr}");
        }
    }

    [Test]
    public void All22SkillVfxPrefabs_ExistWithValidShadersAndUniformParticleVelocities()
    {
        foreach (var addr in All22Prefabs)
        {
            string assetPath = $"Assets/AddressableAssets/{addr}.prefab";
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, $"AddressableAssets/{addr}.prefab")),
                $"Missing prefab file: {assetPath}");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.IsNotNull(prefab, $"Failed to load prefab: {assetPath}");

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(renderers.Length, 0, $"Prefab has no renderers: {assetPath}");
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                Assert.Greater(mats.Length, 0, $"Renderer has no materials: {assetPath}/{r.name}");
                foreach (var mat in mats)
                {
                    Assert.IsNotNull(mat, $"Null material on {assetPath}/{r.name}");
                    Assert.IsNotNull(mat.shader, $"Null shader on {assetPath}/{r.name}");
                    Assert.AreNotEqual("Hidden/InternalErrorShader", mat.shader.name,
                        $"Error shader on {assetPath}/{r.name}");
                    Assert.IsFalse(ShaderUtil.ShaderHasError(mat.shader),
                        $"Shader compile error in {mat.shader.name} on {assetPath}/{r.name}");
                }
            }

            foreach (var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                var vel = ps.velocityOverLifetime;
                if (!vel.enabled) continue;
                Assert.AreEqual(vel.x.mode, vel.y.mode, $"Mixed velocity X/Y curve mode in {assetPath}/{ps.name}");
                Assert.AreEqual(vel.x.mode, vel.z.mode, $"Mixed velocity X/Z curve mode in {assetPath}/{ps.name}");
            }
        }
    }
}
