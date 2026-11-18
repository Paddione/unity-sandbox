using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>cinderreach-party-roles S2: tank slam stuns in radius, books top threat, pulls aggro.</summary>
public class TankAggroTests
{
    GameObject tank;
    GameObject rogue;
    DiabloSkills tankSkills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        tank = Hero("Tank", new Vector3(0f, 0.05f, 0f), withSkills: true);
        tankSkills = tank.GetComponent<DiabloSkills>();
        rogue = Hero("Rogue", new Vector3(1.5f, 0.05f, 0f), withSkills: false);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        var slamFx = GameObject.Find("SeismicSlam");
        if (slamFx != null) Object.DestroyImmediate(slamFx);
        Object.DestroyImmediate(tank);
        Object.DestroyImmediate(rogue);
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        RunProfile.Reset();
    }

    static GameObject Hero(string name, Vector3 pos, bool withSkills)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.AddComponent<DiabloProgression>();
        go.AddComponent<PlayerHealth>();
        if (withSkills) go.AddComponent<DiabloSkills>();
        return go;
    }

    static EnemyHealth Husk(string name, Vector3 pos, int hp = 30)
    {
        var go = new GameObject(name);
        go.tag = "Husk";
        go.transform.position = pos;
        var h = go.AddComponent<EnemyHealth>();
        h.SetTier(hp, LootTier.Normal);
        go.AddComponent<EnemyAttack>();
        return h;
    }

    [UnityTest]
    public IEnumerator Slam_StunsInsideRadiusOnly()
    {
        var inside = Husk("Inside", new Vector3(2.5f, 0.05f, 0f));
        var outside = Husk("Outside", new Vector3(10f, 0.05f, 0f));
        int hits = tankSkills.DoSlam(cosmetic: false);
        Assert.AreEqual(1, hits);
        Assert.IsTrue(inside.GetComponent<EnemyAttack>().IsStunned, "slam stuns inside the radius");
        Assert.IsFalse(outside.GetComponent<EnemyAttack>().IsStunned, "slam ignores husks outside the radius");
        Assert.Less(inside.HPFraction(), 1f, "slam damages inside the radius");
        Assert.AreEqual(1f, outside.HPFraction(), "slam never touches outside the radius");
        Assert.LessOrEqual(inside.HPFraction(), 0.9f, "30 HP husk takes the 3-damage blow");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Slam_PullsAggroToTankDespiteDistance()
    {
        var husk = Husk("Aggro", new Vector3(2.5f, 0.05f, 0f));
        var atk = husk.GetComponent<EnemyAttack>();
        // Rogue is nearer (1 m) and hits first: nearest + top threat agree on the rogue.
        DiabloSkills.ApplyHit(husk.gameObject, 2, rogue.transform.position, 0f, rogue.transform);
        Assert.AreSame(rogue.transform, atk.AcquireHero());
        // Tank slams from 2.5 m: 3 damage + 20 bonus beats the rogue's 2 threat.
        tankSkills.DoSlam(cosmetic: false);
        Assert.AreSame(tank.transform, atk.AcquireHero(), "slam threat pulls aggro to the tank");
        Assert.Greater(husk.GetComponent<ThreatTable>().ThreatOf(tank.transform),
            husk.GetComponent<ThreatTable>().ThreatOf(rogue.transform));
        yield return null;
    }

    [UnityTest]
    public IEnumerator EmptyTable_KeepsNearestLivingTargeting()
    {
        var husk = Husk("Fallback", new Vector3(2.5f, 0.05f, 0f));
        var atk = husk.GetComponent<EnemyAttack>();
        Assert.AreSame(rogue.transform, atk.AcquireHero(), "no threat: nearest living hero, like before S2");
        yield return null;
    }

    [UnityTest]
    public IEnumerator StunnedHusk_DoesNotStrikeWhileStunHolds()
    {
        var striker = Husk("Striker", new Vector3(0.5f, 0.05f, 1f));
        var held = Husk("Held", new Vector3(0.5f, 0.05f, -1f));
        var strikerAtk = striker.GetComponent<EnemyAttack>();
        var heldAtk = held.GetComponent<EnemyAttack>();
        heldAtk.Stun(60f);
        float tankHp = tank.GetComponent<PlayerHealth>().CurrentHP;
        float rogueHp = rogue.GetComponent<PlayerHealth>().CurrentHP;
        yield return new WaitForSeconds(1.5f);
        Assert.Less(tank.GetComponent<PlayerHealth>().CurrentHP, tankHp, "unstunned husk strikes in range");
        Assert.IsTrue(heldAtk.IsStunned);
        Assert.AreEqual(rogueHp, rogue.GetComponent<PlayerHealth>().CurrentHP, "no cross damage to the rogue");
        // The stunned husk never started a windup: its scale stays at rest.
        Assert.AreEqual(Vector3.one, held.transform.localScale);
    }
}
