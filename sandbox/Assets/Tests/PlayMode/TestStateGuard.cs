using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(TestStateGuard))]

/// <summary>
/// Run-wide per-test guard (TestRunCallback fires for [Test] and [UnityTest];
/// an assembly-level NUnit ITestAction does not reach UnityTests).
/// 1. Time.timeScale: a test that fails between PauseMenu.SetOpen(true) or
///    HitStop.Trigger and its restore leaves the clock frozen; every later
///    game-time wait then runs into the 180 s per-test timeout (observed
///    2026-11-20: one leak cost 6 tests x 180 s). Reset before each test.
/// 2. Audio listener: test scenes mostly have none, and Unity then logs
///    "There are no audio listeners in the scene" every frame (2.08 M lines,
///    222 MB PlayMode log). A persistent fallback listener enables itself only
///    while no other listener is active, so scenes and tests that bring their
///    own never see two.
/// </summary>
public class TestStateGuard : ITestRunCallback
{
    public void RunStarted(ITest testsToRun) { }
    public void RunFinished(ITestResult testResults) { }
    public void TestFinished(ITestResult result) { }

    public void TestStarted(ITest test)
    {
        if (test.IsSuite) return;
        if (Time.timeScale != 1f)
        {
            Debug.Log($"TestStateGuard: Time.timeScale was {Time.timeScale} before {test.FullName}; reset to 1.");
            Time.timeScale = 1f;
        }
        if (Application.isPlaying) FallbackAudioListener.Ensure();
    }
}

/// <summary>Persistent listener that steps aside whenever another one is active.</summary>
public class FallbackAudioListener : MonoBehaviour
{
    public const string ObjectName = "TestRunFallbackAudioListener";
    static FallbackAudioListener instance;
    AudioListener own;

    public static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject(ObjectName);
        Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<FallbackAudioListener>();
        instance.own = go.AddComponent<AudioListener>();
        instance.Refresh();
    }

    void LateUpdate() => Refresh();

    void Refresh()
    {
        bool other = false;
        foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (l != own && l.isActiveAndEnabled) { other = true; break; }
        own.enabled = !other;
    }
}
