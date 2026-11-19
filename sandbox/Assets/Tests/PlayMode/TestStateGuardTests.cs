using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Proves TestStateGuard: a leaked pause does not reach the next test.</summary>
public class TestStateGuardTests
{
    [UnityTest, Order(1)]
    public IEnumerator A_LeaksAFrozenClock()
    {
        Time.timeScale = 0f; // simulates a test that failed before PauseMenu.Resume
        yield return null;
    }

    [UnityTest, Order(2)]
    public IEnumerator B_StartsWithRunningClock()
    {
        Assert.AreEqual(1f, Time.timeScale, "TestStateGuard must reset timeScale before each test");
        yield return DiabloTestFixture.WaitSeconds(0.05f);
    }
}
