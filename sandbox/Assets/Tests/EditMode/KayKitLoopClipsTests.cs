using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cyclic KayKit takes loop after import (run used to freeze mid-stride on
/// its last frame); one-shot takes stay single.
/// </summary>
public class KayKitLoopClipsTests
{
    const string Rig = "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_";

    static AnimationClip Clip(string fbx, string take)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Rig + fbx + ".fbx"))
            if (o is AnimationClip c && c.name == take) return c;
        Assert.Fail($"take {take} missing in {fbx}");
        return null;
    }

    [TestCase("MovementBasic", "Running_A")]
    [TestCase("MovementBasic", "Walking_A")]
    [TestCase("General", "Idle_A")]
    [TestCase("CombatRanged", "Ranged_Bow_Aiming_Idle")]
    [TestCase("General", "Idle_B")]
    [TestCase("MovementAdvanced", "Running_Strafe_Left")]
    [TestCase("MovementAdvanced", "Walking_Backwards")]
    [TestCase("CombatMelee", "Melee_Blocking")]
    public void CyclicTakes_Loop(string fbx, string take) =>
        Assert.IsTrue(Clip(fbx, take).isLooping, take);

    [TestCase("CombatMelee", "Melee_1H_Attack_Chop")]
    [TestCase("General", "Hit_A")]
    [TestCase("General", "Death_A")]
    [TestCase("CombatRanged", "Ranged_Bow_Release")]
    [TestCase("CombatMelee", "Melee_Block")]
    [TestCase("MovementBasic", "Jump_Start")]
    public void OneShotTakes_StaySingle(string fbx, string take) =>
        Assert.IsFalse(Clip(fbx, take).isLooping, take);
}
