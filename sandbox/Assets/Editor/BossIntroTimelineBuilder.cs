using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

public static class BossIntroTimelineBuilder
{
    public static void Build()
    {
        const string path = "Assets/AddressableAssets/Cutscene_BossIntro.playable";
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) return;
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        var cameras = timeline.CreateTrack<CinemachineTrack>(null, "Arena");
        for (int i = 0; i < 2; i++)
        {
            var clip = cameras.CreateClip<CinemachineShot>();
            clip.start = i * 4.5;
            clip.duration = 4.5;
            clip.displayName = i == 0 ? "Ossuary approach" : "The Abbot";
            ((CinemachineShot)clip.asset).VirtualCamera.exposedName = clip.displayName;
        }
        var animation = AssetDatabase.LoadAllAssetsAtPath(
            "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_General.fbx")
            .OfType<AnimationClip>().First(a => !a.name.StartsWith("__preview") && a.name.Contains("Idle"));
        var pose = timeline.CreateTrack<AnimationTrack>(null, "Abbot").CreateClip(animation);
        pose.duration = 9;
        var voice = StoryVoice.Clip(BossIntroTimeline.VoiceKey);
        var narration = timeline.CreateTrack<AudioTrack>(null, "Ottilie").CreateClip<AudioPlayableAsset>();
        ((AudioPlayableAsset)narration.asset).clip = voice;
        narration.duration = Mathf.Min(voice.length, 9);
        var signal = ScriptableObject.CreateInstance<SignalAsset>();
        signal.name = "Release arena";
        AssetDatabase.AddObjectToAsset(signal, timeline);
        var end = timeline.CreateTrack<SignalTrack>(null, "Release").CreateMarker<SignalEmitter>(8.98);
        end.asset = signal;
        end.emitOnce = true;
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
    }
}
