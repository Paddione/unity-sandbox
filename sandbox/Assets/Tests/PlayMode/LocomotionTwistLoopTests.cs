using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Split-body locomotion in play: a strafing rig twists its hips toward the
/// travel direction while the chest keeps the root facing, the twist never
/// stacks on an un-posed rig, and used NPCs turn to face the hero.
/// </summary>
public class LocomotionTwistLoopTests
{
    GameObject rig;
    GameObject hero;

    [TearDown]
    public void TearDown()
    {
        if (rig != null) Object.Destroy(rig);
        if (hero != null) Object.Destroy(hero);
    }

    static Transform Bone(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static float Yaw(Transform t)
    {
        var f = t.forward;
        f.y = 0f;
        return Vector3.SignedAngle(Vector3.forward, f, Vector3.up);
    }

    /// <summary>Walk the rig at hero speed for 0.8 s (time-based: batchmode frames are tiny).</summary>
    IEnumerator Move(Vector3 dir)
    {
        float end = Time.time + 0.8f;
        while (Time.time < end)
        {
            rig.transform.position += dir * 5f * Time.deltaTime;
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator Strafing_LegsFollowTravel_ChestKeepsFacing()
    {
        rig = new GameObject("TwistRig");
        rig.AddComponent<Animator>();
        var hips = Bone("hips", Bone("root", rig.transform));
        var chest = Bone("chest", Bone("spine", hips));
        var twist = rig.AddComponent<LocomotionTwist>();
        yield return Move(Vector3.right);
        Assert.IsTrue(twist.MovingMeasured);
        Assert.IsFalse(twist.Backpedaling);
        Assert.AreEqual(90f, Yaw(hips), 8f, "hips turn to the strafe direction");
        Assert.AreEqual(0f, Yaw(chest), 1f, "chest keeps the aim facing");

        yield return Move(Vector3.back);
        Assert.IsTrue(twist.Backpedaling, "running away from the facing backpedals");
        Assert.AreEqual(0f, Yaw(hips), 8f);
        Assert.AreEqual(0f, Yaw(chest), 1f);

        yield return Move(Vector3.zero);
        Assert.IsFalse(twist.MovingMeasured);
        Assert.AreEqual(0f, Yaw(hips), 3f, "standing settles the hips without stacked twist");
        Assert.AreEqual(0f, Yaw(chest), 3f);
    }

    [UnityTest]
    public IEnumerator UsedNpc_TurnsToFaceTheHero()
    {
        rig = new GameObject("Npc");
        rig.AddComponent<Animator>();
        var npc = rig.AddComponent<StoryNpc>();
        npc.npcId = StoryIds.Ottilie;
        hero = new GameObject("Hero");
        hero.transform.position = new Vector3(-2f, 0f, 0f);
        npc.FaceHero(hero);
        float end = Time.time + 1f;
        while (npc.IsTurning && Time.time < end) yield return null;
        Assert.IsFalse(npc.IsTurning);
        Assert.AreEqual(-90f, Yaw(rig.transform), 1f);
    }
}
