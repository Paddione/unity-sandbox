using UnityEngine;

/// <summary>
/// Playtest instrumentation: logs average FPS + frame time every 5 s of
/// unscaled time (render-bound, works without input). Pure math lives in
/// <see cref="ComputeFps"/> so PlayMode tests can pin it.
/// </summary>
public sealed class FpsReporter : MonoBehaviour
{
    float elapsed;
    int frames;

    static readonly System.Collections.Generic.List<float> windows = new System.Collections.Generic.List<float>();

    /// <summary>Every logged 5-s window this run, in order (read-only; autoplay result).</summary>
    public static System.Collections.Generic.IReadOnlyList<float> Windows => windows.AsReadOnly();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetWindows() => windows.Clear();

    public static float ComputeFps(int frames, float elapsed)
    {
        return elapsed > 0f ? frames / elapsed : 0f;
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        frames++;
        if (elapsed >= 5f)
        {
            float fps = ComputeFps(frames, elapsed);
            windows.Add(fps);
            Debug.Log($"[Perf] FPS avg {fps:F1} ({1000f / Mathf.Max(fps, 0.001f):F1} ms/frame) over {frames} frames.");
            elapsed = 0f;
            frames = 0;
        }
    }
}
