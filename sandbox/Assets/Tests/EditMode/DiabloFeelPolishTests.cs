using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Feel polish (husk rig, corpses, hit-stop): scene + controller pins.</summary>
public class DiabloFeelPolishTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";
    const string ControllerPath = "Assets/Resources/HuskLocomotion.controller";

    [Test]
    public void HuskController_HasAllStatesAndParams()
    {
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(c, "Husk controller must be built into Resources.");
        var states = c.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
        foreach (var name in new[] { "Idle", "Walk", "Attack", "Hit", HuskAnimator.DeathState })
            Assert.Contains(name, states, $"Husk controller lacks state '{name}'.");
        foreach (var s in c.layers[0].stateMachine.states)
            Assert.IsNotNull(s.state.motion, $"State '{s.state.name}' has no clip.");
        var ps = c.parameters.Select(p => p.name).ToArray();
        foreach (var p in new[] { HuskAnimator.MoveParam, HuskAnimator.AttackParam, HuskAnimator.HitParam })
            Assert.Contains(p, ps, $"Husk controller lacks parameter '{p}'.");
        Assert.AreEqual("Idle", c.layers[0].stateMachine.defaultState.name);
    }

    [Test]
    public void ZoneHusks_AreAnimated()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid());
        var husks = GameObject.FindGameObjectsWithTag("Husk");
        Assert.GreaterOrEqual(husks.Length, 10);
        foreach (var h in husks)
        {
            if (h.GetComponent<EnemyHealth>() == null) continue;
            Assert.IsNotNull(h.GetComponent<HuskAnimator>(), $"{h.name} needs HuskAnimator.");
            var a = h.GetComponent<Animator>();
            Assert.IsNotNull(a, $"{h.name} needs an Animator.");
            Assert.IsNotNull(a.runtimeAnimatorController, $"{h.name} needs the husk controller.");
            Assert.IsFalse(a.applyRootMotion, $"{h.name}: root motion would fight the approach.");
            Assert.IsNotNull(h.GetComponent<EnemyHealth>().corpseModel,
                $"{h.name} needs a corpse model (host-safe corpse path).");
        }
    }

    [Test]
    public void HitStop_StaysInertOutsidePlayMode()
    {
        HitStop.ResetRun();
        HitStop.Trigger();
        Assert.AreEqual(0, HitStop.Triggered, "EditMode must never freeze time.");
        Assert.AreEqual(1f, Time.timeScale);
    }
}
