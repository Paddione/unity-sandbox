using UnityEngine;

/// <summary>
/// Per-frame cached hero lookup (act-one S5 perf fix). Every enemy, boss,
/// field and shrine used to call <c>GameObject.Find("Player")</c> in its own
/// Update, roughly 50 full scene searches per frame once the Cinder Kiln was
/// populated; FPS fell from about 48 to 27 in fights. One search per frame now
/// serves everyone. The cache is only reused while the hero is alive, active
/// and still named "Player" (co-op takeover renames), so tests and scene
/// changes never see a stale hero.
/// </summary>
public static class HeroLocator
{
    static int frame = -1;
    static Transform cached;

    /// <summary>The local hero named "Player" with PlayerHealth, cached per frame; null when none.</summary>
    public static Transform Solo()
    {
        if (frame == Time.frameCount && cached != null && cached.gameObject.activeInHierarchy
            && cached.name == DiabloNetworkPlayer.HeroName)
            return cached;
        frame = Time.frameCount;
        cached = null;
        var solo = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (solo != null && solo.GetComponent<PlayerHealth>() != null) cached = solo.transform;
        return cached;
    }

    /// <summary>Solo hero, else the first PlayerHealth in the scene (co-op server without a "Player").</summary>
    public static Transform Hero()
    {
        var solo = Solo();
        if (solo != null) return solo;
        foreach (var h in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            return h.transform;
        return null;
    }

    /// <summary>
    /// Every living hero in the scene (party-roles S4): the local hero,
    /// co-op peers and active AI companions. Dead and stood-down entries
    /// are skipped so heals and HUDs only see the fighting party.
    /// </summary>
    public static System.Collections.Generic.List<Transform> AllHeroes()
    {
        var list = new System.Collections.Generic.List<Transform>();
        foreach (var h in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            if (h.CurrentHP <= 0f) continue;
            list.Add(h.transform);
        }
        return list;
    }

    /// <summary>Lookups that hit the scene this run (test seam for the cache).</summary>
    public static int Frame => frame;
}
