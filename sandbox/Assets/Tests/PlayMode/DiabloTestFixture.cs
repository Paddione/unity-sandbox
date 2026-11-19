using System.Collections;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Shared PlayMode test helpers (track P2). Consolidates the TeleportHero /
/// WaitSeconds / teardown-cleanup duplicates that lived in every loop-test file.
/// Headless traps encoded here: m_AutoSyncTransforms=0 (CC-toggle + Sync),
/// Destroy applies at frame end, LogAssert matches exactly.
/// </summary>
public static class DiabloTestFixture
{
    /// <summary>
    /// Teleport the hero so physics (and triggers) actually sees it: an enabled
    /// CharacterController does not rebase its internal position on a raw transform
    /// set (plus this project disables autoSyncTransforms).
    /// </summary>
    public static void TeleportHero(GameObject hero, Vector3 pos)
    {
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = pos;
        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms();
    }

    /// <summary>Real-time ceiling for one WaitSeconds call beyond its game-time span.</summary>
    public const float WaitRealtimeSlack = 30f;

    /// <summary>
    /// Frame-rate independent test wait (replaces per-file WaitSeconds). Fails
    /// fast instead of spinning until the stall watchdog when game time stops
    /// (Time.timeScale left at 0 by a pause/hit-stop that never restored).
    /// </summary>
    public static IEnumerator WaitSeconds(float seconds)
    {
        float end = Time.time + seconds;
        float realDeadline = Time.realtimeSinceStartup + seconds + WaitRealtimeSlack;
        while (Time.time < end)
        {
            if (Time.realtimeSinceStartup > realDeadline)
                Assert.Fail($"WaitSeconds({seconds}) exceeded {WaitRealtimeSlack} s real time: " +
                            $"Time.timeScale={Time.timeScale}, game clock stalled.");
            yield return null;
        }
    }

    /// <summary>
    /// Destroy at frame end (for kill asserts that must yield). Immediate variant:
    /// use Object.DestroyImmediate directly in TearDown.
    /// </summary>
    public static IEnumerator DestroyAfterFrame(GameObject go)
    {
        Object.Destroy(go);
        yield return null;
    }

    /// <summary>
    /// Remove leaked test objects between suites: dropped orbs (with or without a
    /// LootDrop — no-drop rolls leave legacy orbs), loot, husks, heal zones, the
    /// test EventSystem/listener and the player. Call from TearDown, then reset
    /// the run-scoped production statics (ToyTrigger/EnemyHealth/DiabloMelee/
    /// DiabloAudio ResetRun) in the test file itself.
    /// </summary>
    public static void CleanupTestScene(GameObject player)
    {
        foreach (var orb in GameObject.FindObjectsOfType<ToyTrigger>())
            if (orb.gameObject.name.StartsWith("DiabloOrb_"))
                Object.DestroyImmediate(orb.gameObject);
        foreach (var loot in GameObject.FindObjectsOfType<LootDrop>())
            if (loot != null) Object.DestroyImmediate(loot.gameObject);
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(husk);
        foreach (var left in GameObject.FindObjectsOfType<CampfireHeal>())
            Object.DestroyImmediate(left.gameObject);
        var eventSys = GameObject.Find("EventSystem");
        if (eventSys != null) Object.DestroyImmediate(eventSys);
        var listener = GameObject.Find("TestAudioListener");
        if (listener != null) Object.DestroyImmediate(listener);
        if (player != null) Object.DestroyImmediate(player);
    }
}
