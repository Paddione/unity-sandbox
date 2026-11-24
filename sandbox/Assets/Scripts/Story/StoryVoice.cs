using UnityEngine;

/// <summary>
/// Spoken story lines (cinderreach-story-voice). Every dialog and bark line key
/// has a German clip under <c>Resources/Voice/&lt;key&gt;</c>, rendered offline by
/// <c>scripts/voice/render_voices.py</c> (synthetic Qwen3-TTS voices, one per
/// speaker, see <c>docs/audio/voices.json</c>). Only German is voiced; with
/// <c>-lang en</c> the subtitles stay and the clips stay silent. One line plays
/// at a time: a new line or closing the window cuts the old one.
/// </summary>
public static class StoryVoice
{
    public const string ResourceFolder = "Voice/";

    /// <summary>Test counters: lines started, lines without a clip, last key asked for.</summary>
    public static int played;
    public static int missing;
    public static string lastKey;

    static AudioSource source;

    /// <summary>The clip for a catalog line key, or null when none is rendered.</summary>
    public static AudioClip Clip(string key) =>
        string.IsNullOrEmpty(key) ? null : Resources.Load<AudioClip>(ResourceFolder + key);

    /// <summary>Whether voice playback applies at all (German, not muted, not batchmode).</summary>
    public static bool Active
    {
        get
        {
            if (Loc.Language != Loc.German || DiabloAudio.MutePlayback) return false;
            try { return !Application.isBatchMode; } catch { return false; }
        }
    }

    /// <summary>Speaks <paramref name="key"/>; returns the clip length in seconds (0 when silent).</summary>
    public static float Play(string key)
    {
        lastKey = key;
        Stop();
        if (Loc.Language != Loc.German) return 0f;
        var clip = Clip(key);
        if (clip == null) { missing++; return 0f; }
        played++;
        if (!Active) return clip.length;
        try
        {
            EnsureSource();
            source.clip = clip;
            source.volume = DiabloSettings.EffectiveVoice;
            source.Play();
        }
        catch { }
        return clip.length;
    }

    public static void Stop()
    {
        if (source != null && source.isPlaying) source.Stop();
    }

    public static bool IsPlaying => source != null && source.isPlaying;

    static void EnsureSource()
    {
        if (source != null) return;
        var go = new GameObject("StoryVoice");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.priority = 16;
    }
}
