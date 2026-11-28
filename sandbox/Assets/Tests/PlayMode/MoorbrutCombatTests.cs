using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MoorbrutCombatTests
{
    GameObject hero;
    GameObject foe;
    AnimationClip clip;

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(foe);
        Object.DestroyImmediate(clip);
        DiabloTestFixture.CleanupTestScene(hero);
    }

    [UnityTest]
    public IEnumerator TankLocksDirectionAndMissesASidestep()
    {
        Spawn(true);
        var hp = hero.GetComponent<PlayerHealth>();
        float before = hp.CurrentHP;
        yield return null;
        Assert.AreEqual(1, foe.GetComponent<HuskAnimator>().attackCount);
        hero.transform.position = foe.transform.position + Vector3.right;
        yield return DiabloTestFixture.WaitSeconds(foe.GetComponent<MoorbrutCombat>().ImpactTime + 0.1f);
        Assert.AreEqual(before, hp.CurrentHP);
    }

    [UnityTest]
    public IEnumerator RunnerDamagesHeroAtImpact()
    {
        Spawn(false);
        var hp = hero.GetComponent<PlayerHealth>();
        float before = hp.CurrentHP;
        yield return null;
        Assert.AreEqual(before, hp.CurrentHP);
        yield return DiabloTestFixture.WaitSeconds(foe.GetComponent<MoorbrutCombat>().ImpactTime + 0.1f);
        Assert.Less(hp.CurrentHP, before);
    }

    void Spawn(bool tank)
    {
        hero = new GameObject("Player");
        hero.transform.position = new Vector3(-17, 0, 12);
        hero.AddComponent<PlayerHealth>();
        foe = new GameObject("Brood");
        foe.transform.position = hero.transform.position - Vector3.forward;
        var combat = foe.AddComponent<MoorbrutCombat>();
        combat.tank = tank;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(foe.transform, false);
        combat.model = visual.AddComponent<Animation>();
        clip = new AnimationClip { legacy = true };
        clip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 3, 0));
        combat.model.AddClip(clip, "Walk");
        combat.model.AddClip(clip, "Strike");
        foe.AddComponent<EnemyAttack>();
    }
}
