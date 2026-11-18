using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Co-op zone travel + corpse fan-out (cinderreach-coop-session S3).
/// Transport rides on the host's player object (<see cref="DiabloNetworkPlayer"/>,
/// the only reliably replicated RPC path: player prefab, like ShareKillXP).
/// This file owns the pure gates (<see cref="GroupReady"/>,
/// <see cref="SpawnOffset"/>), the fullscreen gate fade, and the local apply
/// step every side runs inside the broadcast. The 2-process pair run proves
/// delivery (both sides reach zone 2).
/// </summary>
public static class ZoneTravelRelay
{
    /// <summary>Fade out/in around a broadcast travel (seconds each way).</summary>
    public const float FadeSeconds = 0.3f;

    static GameObject s_fadeObj;
    static Image s_fadeImg;

    /// <summary>
    /// Pure group-travel gate (S3): fire when every living hero stands on the
    /// gate, or when the host held it for <see cref="ZoneGate.HostHoldSeconds"/>
    /// (pulls stragglers along). Never on a locked gate.
    /// </summary>
    public static bool GroupReady(bool allLivingOnGate, float hostHoldTime, bool locked)
    {
        if (locked) return false;
        if (allLivingOnGate) return true;
        return hostHoldTime >= ZoneGate.HostHoldSeconds;
    }

    /// <summary>
    /// Deterministic spawn spread around a gate destination (index 0 stays
    /// put, followers fan out laterally, all within ~3 m).
    /// </summary>
    public static Vector3 SpawnOffset(int index)
    {
        if (index <= 0) return Vector3.zero;
        float lateral = ((index + 1) / 2) * 1.5f * (index % 2 == 1 ? 1f : -1f);
        float depth = (index / 2) * 1.0f;
        return new Vector3(lateral, 0f, depth);
    }

    /// <summary>Index of this process among the connected clients (sorted).</summary>
    public static int OwnIndex()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return 0;
        var ids = new List<ulong>(nm.ConnectedClientsIds);
        ids.Sort();
        int i = ids.IndexOf(nm.LocalClientId);
        return i < 0 ? 0 : i;
    }

    /// <summary>Fade, teleport the local hero, fade back (run on the RPC owner).</summary>
    public static IEnumerator TravelRoutine(string gateName, Vector3 baseDest, int targetZone)
    {
        yield return FadeTo(1f);
        ApplyLocalTravel(gateName, baseDest, targetZone);
        yield return FadeTo(0f);
    }

    /// <summary>Teleport this process's own hero (offset so the party does not stack).</summary>
    public static void ApplyLocalTravel(string gateName, Vector3 baseDest, int targetZone)
    {
        var hero = LocalHero();
        if (hero == null) return;
        var dest = baseDest + SpawnOffset(OwnIndex());
        var gateGo = string.IsNullOrEmpty(gateName) ? null : GameObject.Find(gateName);
        var gate = gateGo != null ? gateGo.GetComponent<ZoneGate>() : null;
        if (gate != null) gate.ApplyTravel(hero, dest);
        else ZoneTravel.TeleportHero(hero, dest);
        Debug.Log($"[Zone] co-op travel {gateName} → zone {targetZone} (offset {SpawnOffset(OwnIndex())}).");
    }

    /// <summary>Rebuild the death visual from this process's own husk copy.</summary>
    public static void ApplyRemoteCorpse(ulong huskNetId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null) return;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(huskNetId, out var nb)) return;
        var corpse = DiabloCorpse.SpawnFrom(nb.gameObject, remote: true);
        if (corpse != null) Debug.Log($"[Zone] remote corpse for husk {huskNetId}.");
    }

    static GameObject LocalHero()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            var local = nm.SpawnManager.GetLocalPlayerObject();
            if (local != null) return local.gameObject;
        }
        var solo = HeroLocator.Solo();
        return solo != null ? solo.gameObject : null;
    }

    /// <summary>Fullscreen black overlay for gate fades (created on demand).</summary>
    public static void EnsureFade()
    {
        if (s_fadeObj != null) return;
        var canvasGo = new GameObject("ZoneFadeCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        var imgGo = new GameObject("ZoneFade");
        imgGo.transform.SetParent(canvasGo.transform, false);
        s_fadeImg = imgGo.AddComponent<Image>();
        s_fadeImg.color = new Color(0f, 0f, 0f, 0f);
        s_fadeImg.raycastTarget = false; // never swallow input, even if a fade stalls
        var r = s_fadeImg.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.one;
        Object.DontDestroyOnLoad(canvasGo);
        s_fadeObj = canvasGo;
    }

    /// <summary>Current fade alpha (test seam).</summary>
    public static float FadeAlpha => s_fadeImg != null ? s_fadeImg.color.a : 0f;

    /// <summary>Animate the overlay toward <paramref name="target"/> alpha.</summary>
    public static IEnumerator FadeTo(float target)
    {
        EnsureFade();
        float start = FadeAlpha;
        float t = 0f;
        while (t < FadeSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / FadeSeconds);
            SetFade(Mathf.Lerp(start, target, k));
            yield return null;
        }
        SetFade(target);
    }

    static void SetFade(float a)
    {
        if (s_fadeImg == null) return;
        var c = s_fadeImg.color;
        c.a = a;
        s_fadeImg.color = c;
    }
}
