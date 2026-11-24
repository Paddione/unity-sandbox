using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// KayKit Rig_Medium takes import without loop flags (empty clipAnimations),
/// so cyclic takes played once and froze on their last frame — runs stopped
/// mid-stride with a raised leg. This postprocessor marks the cyclic takes
/// (idle, run, walk, strafe, crouch, held aim/block) as looping at import.
/// Bump <see cref="GetVersion"/> when <see cref="LoopPattern"/> changes.
/// </summary>
public sealed class KayKitLoopClips : AssetPostprocessor
{
    /// <summary>Cyclic take names; one-shot takes (attacks, hits, deaths, jumps) stay single.</summary>
    public static readonly Regex LoopPattern = new Regex(
        @"^(Idle_|Running_|Walking_|Crawling$|Sneaking$|Crouching$|Jump_Idle$)|_Idle$|Aiming|Blocking$|Spellcasting",
        RegexOptions.CultureInvariant);

    public static bool ShouldLoop(string takeName) => LoopPattern.IsMatch(takeName);

    public override uint GetVersion() => 1;

    void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!assetPath.Replace('\\', '/').Contains("/KayKit/CharacterAnimations/")) return;
        if (!ShouldLoop(clip.name)) return;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        if (settings.loopTime) return;
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }
}
