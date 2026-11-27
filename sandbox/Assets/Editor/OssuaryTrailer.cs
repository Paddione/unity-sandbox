using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;

public static class OssuaryTrailer
{
    const string Folder = "Assets/Editor/Trailers/";
    const string Capture = Folder + "MenuCapture.playable";

    public static void BuildCapture()
    {
        if (AssetDatabase.LoadMainAssetAtPath(Capture)) return;
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, Capture);
        var track = timeline.CreateTrack<RecorderTrack>(null, "Game view");
        var clip = track.CreateClip<RecorderClip>();
        clip.duration = 3;
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Menu loop";
        movie.OutputFile = "Recordings/MenuLoop_<Take>";
        movie.FileNameGenerator.Root = OutputPath.Root.Project;
        movie.EncoderSettings = new CoreEncoderSettings { Codec = CoreEncoderSettings.OutputCodec.WEBM };
        AssetDatabase.AddObjectToAsset(movie, timeline);
        ((RecorderClip)clip.asset).settings = movie;
        EditorUtility.SetDirty(clip.asset);
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Cinderreach/Trailers/Record three seconds of Game view", true)]
    static bool CanCapture() => EditorApplication.isPlaying;

    [MenuItem("Cinderreach/Trailers/Record three seconds of Game view")]
    static void Record()
    {
        var host = new GameObject("Menu capture");
        var director = host.AddComponent<PlayableDirector>();
        director.extrapolationMode = DirectorWrapMode.None;
        director.timeUpdateMode = DirectorUpdateMode.GameTime;
        director.stopped += _ => Object.Destroy(host);
        director.Play(AssetDatabase.LoadAssetAtPath<TimelineAsset>(Capture));
    }

    [MenuItem("Cinderreach/Trailers/Preview Ossuary", true)]
    static bool CanPreview() => EditorApplication.isPlaying && Camera.main;

    [MenuItem("Cinderreach/Trailers/Preview Ossuary")]
    static void Preview()
    {
        var host = new GameObject("Ossuary trailer preview");
        var player = host.AddComponent<VideoPlayer>();
        player.renderMode = VideoRenderMode.CameraNearPlane;
        player.targetCamera = Camera.main;
        player.url = System.IO.Path.Combine(Application.dataPath, "Editor/Trailers/Ossuary.webm");
        player.loopPointReached += _ => Object.Destroy(host);
        player.errorReceived += (_, message) => { Debug.LogError(message); Object.Destroy(host); };
        player.Play();
    }
}
