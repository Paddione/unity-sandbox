using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>cinderreach-encounter-roles S2: cover holds fire, separate aggro, zone abort.</summary>
public class EncounterRoleTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        EncounterLayout.ResetEncounter();
        RangedCaster.ResetRun();
        AshLancer.ResetRun();
        EnemyHealth.ResetRun();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in GameObject.FindObjectsOfType<Transform>())
            if (go != null && go.name == "LancerTelegraph")
                Object.DestroyImmediate(go.gameObject);
        foreach (var wall in GameObject.FindObjectsOfType<BoxCollider>())
            if (wall != null && wall.gameObject.name == "TestWall")
                Object.DestroyImmediate(wall.gameObject);
        DiabloTestFixture.CleanupTestScene(hero);
        hero = null;
        EncounterLayout.ResetEncounter();
        RangedCaster.ResetRun();
        AshLancer.ResetRun();
    }

    GameObject MakeHero(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player";
        DiabloTestFixture.TeleportHero(go, pos);
        go.AddComponent<PlayerHealth>();
        return go;
    }

    GameObject MakeCaster(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Bone Warden";
        go.tag = "Husk";
        go.transform.position = pos;
        go.AddComponent<EnemyHealth>().SetTier(12, LootTier.Normal);
        go.AddComponent<RangedCaster>();
        return go;
    }

    GameObject MakeThrall(Vector3 pos, int group)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Cinder Thrall_0";
        go.tag = "Husk";
        go.transform.position = pos;
        go.AddComponent<EnemyHealth>().SetTier(6, LootTier.Normal);
        var atk = go.AddComponent<EnemyAttack>();
        atk.encounterGroup = group;
        atk.SetHome(pos);
        return go;
    }

    [UnityTest]
    public IEnumerator BlockedShot_HoldsFireUntilClear()
    {
        hero = MakeHero(new Vector3(0f, 0.05f, 0f));
        var caster = MakeCaster(new Vector3(0f, 0.05f, -7f)); // hold band, clear range
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "TestWall";
        wall.transform.position = new Vector3(0f, 1f, -3.5f);
        // Wide enough that sidestepping inside the leash cannot clear it:
        // the setup tests "blocked until clear", not the strafe itself.
        wall.transform.localScale = new Vector3(30f, 3f, 0.5f);
        Physics.SyncTransforms();

        yield return DiabloTestFixture.WaitSeconds(RangedCaster.Cooldown + RangedCaster.Telegraph + 0.6f);
        Assert.AreEqual(0, RangedCaster.ShotsFired, "cover holds the shot");
        Assert.Greater(RangedCaster.CoverHolds, 0, "hold is logged");

        Object.DestroyImmediate(wall);
        Physics.SyncTransforms();
        yield return DiabloTestFixture.WaitSeconds(RangedCaster.Cooldown + RangedCaster.Telegraph + 0.6f);
        Assert.Greater(RangedCaster.ShotsFired, 0, "clear axis fires");
    }

    [UnityTest]
    public IEnumerator SeparateAggro_FrontTriggersAlone()
    {
        // Kiln coordinates so all three share a zone; the hero stays before the choke.
        hero = MakeHero(new Vector3(400f, 0.05f, 2f));
        var front = MakeThrall(new Vector3(400f, 0.05f, -2f), EncounterLayout.FrontGroup);
        var back = MakeThrall(new Vector3(404f, 0.05f, -3f), EncounterLayout.BackGroup);
        Vector3 frontStart = front.transform.position;
        Vector3 backStart = back.transform.position;

        yield return DiabloTestFixture.WaitSeconds(1.2f);

        Assert.Greater(Vector3.Distance(front.transform.position, frontStart), 0.3f, "front advances");
        Assert.Less(Vector3.Distance(back.transform.position, backStart), 0.05f, "back stays dormant");

        // Crossing the choke releases the back group.
        DiabloTestFixture.TeleportHero(hero, new Vector3(400f, 0.05f, -4f));
        yield return DiabloTestFixture.WaitSeconds(1.2f);
        Assert.Greater(Vector3.Distance(back.transform.position, backStart), 0.2f, "back released at the choke");
    }

    [UnityTest]
    public IEnumerator ZoneChange_AbortsWindup()
    {
        hero = MakeHero(new Vector3(400f, 0.05f, 0f));
        var hp = hero.GetComponent<PlayerHealth>();
        float full = hp.CurrentHP;
        var thrall = MakeThrall(new Vector3(400f, 0.05f, -1.5f), EncounterLayout.FrontGroup);

        yield return DiabloTestFixture.WaitSeconds(0.2f); // windup started, not resolved
        DiabloTestFixture.TeleportHero(hero, new Vector3(0f, 0.05f, 0f)); // Fen: other zone
        Vector3 stuck = thrall.transform.position;
        yield return DiabloTestFixture.WaitSeconds(EnemyAttack.Windup + EnemyAttack.Cooldown);

        Assert.AreEqual(full, hp.CurrentHP, "no strike after the zone change");
        Assert.Less(Vector3.Distance(thrall.transform.position, stuck), 0.05f, "no cross-zone chase");
    }

    [UnityTest]
    public IEnumerator Lancer_ShowsChargeDirection()
    {
        hero = MakeHero(new Vector3(0f, 0.05f, 0f));
        var go = new GameObject("Ash Lancer_0");
        go.tag = "Husk";
        go.transform.position = new Vector3(0f, 0.05f, -8f);
        go.AddComponent<EnemyHealth>().SetTier(10, LootTier.Normal);
        var lancer = go.AddComponent<AshLancer>();

        int frames = 0;
        while (lancer.Current != AshLancer.State.Telegraph && frames++ < 120) yield return null;
        Assert.AreEqual(AshLancer.State.Telegraph, lancer.Current, "telegraph starts at hold distance");
        Assert.IsTrue(lancer.TelegraphVisible, "charge direction is visible");
        var toHero = hero.transform.position - go.transform.position;
        toHero.y = 0f;
        Assert.Less(Vector3.Angle(lancer.TelegraphDir, toHero.normalized), 5f, "line points at the hero");
    }

    [UnityTest]
    public IEnumerator Warden_WalksHomePastTheLeash()
    {
        var caster = MakeCaster(new Vector3(0f, 0.05f, 0f));
        var rc = caster.GetComponent<RangedCaster>();
        yield return null; // Start() sets home
        caster.transform.position = new Vector3(0f, 0.05f, -(RangedCaster.LeashRadius + RangedCaster.LeashSlack + 1f));
        Physics.SyncTransforms();

        yield return DiabloTestFixture.WaitSeconds(1.0f);
        Assert.IsTrue(rc.Returning, "past the leash the warden disengages");
        yield return DiabloTestFixture.WaitSeconds(6.0f);
        Assert.IsFalse(rc.Returning, "warden is home again");
        Assert.Less(Vector3.Distance(caster.transform.position, rc.Home), 0.5f);
    }
}
