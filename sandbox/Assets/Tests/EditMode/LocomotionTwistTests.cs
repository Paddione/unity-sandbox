using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Split-body locomotion rule: legs follow the travel direction, the upper
/// body keeps the facing, travel against the facing backpedals (with
/// hysteresis), and NPC use turns only animated figures.
/// </summary>
public class LocomotionTwistTests
{
    static readonly Vector3 Fwd = Vector3.forward;

    [Test]
    public void LegYaw_FollowsTravelWithinFrontArc()
    {
        Assert.AreEqual(0f, LocomotionTwist.SolveLegYaw(Fwd, Fwd, false, out var back), 0.01f);
        Assert.IsFalse(back);
        Assert.AreEqual(90f, LocomotionTwist.SolveLegYaw(Fwd, Vector3.right, false, out back), 0.01f, "strafe right");
        Assert.IsFalse(back);
        Assert.AreEqual(-90f, LocomotionTwist.SolveLegYaw(Fwd, Vector3.left, false, out back), 0.01f, "strafe left");
        Assert.IsFalse(back);
    }

    [Test]
    public void LegYaw_BackpedalsWhenMovingAgainstFacing()
    {
        Assert.AreEqual(0f, LocomotionTwist.SolveLegYaw(Fwd, Vector3.back, false, out var back), 0.01f);
        Assert.IsTrue(back, "straight back reverses the run instead of twisting 180°");
        var backRight = (Vector3.back + Vector3.right).normalized;
        float yaw = LocomotionTwist.SolveLegYaw(Fwd, backRight, false, out back);
        Assert.IsTrue(back);
        Assert.AreEqual(-45f, yaw, 0.01f, "legs face front-left, run reversed toward back-right");
    }

    [Test]
    public void LegYaw_HysteresisAtTheBoundary()
    {
        var at90 = Quaternion.Euler(0f, 90f, 0f) * Fwd;
        LocomotionTwist.SolveLegYaw(Fwd, at90, false, out var back);
        Assert.IsFalse(back, "90° from facing still runs forward");
        LocomotionTwist.SolveLegYaw(Fwd, at90, true, out back);
        Assert.IsTrue(back, "an active backpedal holds until below the exit angle");
        var at70 = Quaternion.Euler(0f, 70f, 0f) * Fwd;
        LocomotionTwist.SolveLegYaw(Fwd, at70, true, out back);
        Assert.IsFalse(back);
    }

    [Test]
    public void LegYaw_ZeroWhenStanding()
    {
        Assert.AreEqual(0f, LocomotionTwist.SolveLegYaw(Fwd, Vector3.zero, false, out var back));
        Assert.IsFalse(back);
    }

    [Test]
    public void FaceHero_OnlyAnimatedFiguresTurn()
    {
        var hero = new GameObject("Hero");
        var figure = new GameObject("Figure");
        var prop = new GameObject("Anvil");
        try
        {
            hero.transform.position = new Vector3(3f, 0f, 0f);
            figure.AddComponent<Animator>();
            var npc = figure.AddComponent<StoryNpc>();
            var anvil = prop.AddComponent<StoryNpc>();
            npc.FaceHero(hero);
            anvil.FaceHero(hero);
            Assert.IsTrue(npc.IsTurning);
            Assert.IsFalse(anvil.IsTurning, "props without an Animator keep their pose");
        }
        finally
        {
            Object.DestroyImmediate(hero);
            Object.DestroyImmediate(figure);
            Object.DestroyImmediate(prop);
        }
    }
}
