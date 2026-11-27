using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;

public class BossIntroPlaybackTests
{
    [UnityTest]
    public IEnumerator Timeline_CutsToSecondCameraAndEmitsRelease()
    {
        var cameraObject = new GameObject("Arena camera", typeof(Camera));
        var boss = new GameObject("Arena anchor", typeof(Animator));
        var intro = BossIntroTimeline.StartIntro(cameraObject.GetComponent<Camera>(), boss.transform, Vector3.back * 8);
        try
        {
            Assert.IsNotNull(intro);
            var director = intro.GetComponent<PlayableDirector>();
            yield return null;
            yield return null;
            var first = cameraObject.transform.position;
            Assert.That(first.sqrMagnitude, Is.GreaterThan(1));
            director.time = 5;
            director.Evaluate();
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(first, cameraObject.transform.position), Is.GreaterThan(1));
            director.time = 8.8;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(intro.Finished);
        }
        finally
        {
            if (intro) intro.Release();
            Object.Destroy(boss);
            Object.Destroy(cameraObject);
        }
    }
}
