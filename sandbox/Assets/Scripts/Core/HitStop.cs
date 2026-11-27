using System.Collections;
using UnityEngine;

/// <summary>
/// Freeze-frame on landed hits (weight without screenshake). Drops
/// <c>Time.timeScale</c> for a realtime-bounded window and always restores
/// it — also when the host is disabled or destroyed mid-stop. Solo only (offline
/// or a host nobody joined): co-op time stays untouched. Inert outside play mode.
/// </summary>
public sealed class HitStop : MonoBehaviour
{
    public const float Duration = 0.06f;
    public const float FrozenScale = 0.05f;

    public static bool Enabled = true;
    public static int Triggered;

    static HitStop host;
    float restoreScale = 1f;
    bool active;

    public static void Trigger(float duration = Duration)
    {
        if (!Enabled || !Application.isPlaying) return;
        if (!Diablo.IsSolo()) return;
        if (host == null)
        {
            var go = new GameObject("HitStop");
            Object.DontDestroyOnLoad(go);
            host = go.AddComponent<HitStop>();
        }
        Triggered++;
        if (host.active) return; // never stack: one freeze per window
        host.StartCoroutine(host.Run(duration));
    }

    IEnumerator Run(float duration)
    {
        active = true;
        restoreScale = Time.timeScale;
        Time.timeScale = FrozenScale;
        yield return new WaitForSecondsRealtime(duration);
        Restore();
    }

    void Restore()
    {
        if (!active) return;
        active = false;
        Time.timeScale = restoreScale;
    }

    void OnDisable() => Restore();

    /// <summary>True while a freeze is running (test seam).</summary>
    public static bool IsActive => host != null && host.active;

    public static void ResetRun()
    {
        Triggered = 0;
        if (host != null) host.Restore();
    }
}
