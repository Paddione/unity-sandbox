using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>cinderreach-party-roles S4: solo fills missing roles, fights and heals, death rejoins.</summary>
public class SoloPartyTests
{
    GameObject leader;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
        leader = new GameObject(DiabloNetworkPlayer.HeroName);
        leader.transform.position = new Vector3(0f, 0.05f, 0f);
        leader.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var c in Object.FindObjectsByType<PartyCompanion>(FindObjectsSortMode.None)) Object.DestroyImmediate(c.gameObject);
        var roster = GameObject.Find("PartyRoster");
        if (roster != null) Object.DestroyImmediate(roster);
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        foreach (var p in Object.FindObjectsByType<DiabloProjectile>(FindObjectsSortMode.None)) Object.DestroyImmediate(p.gameObject);
        GameObject fx;
        while ((fx = GameObject.Find("MendingLight")) != null) Object.DestroyImmediate(fx);
        while ((fx = GameObject.Find("SeismicSlam")) != null) Object.DestroyImmediate(fx);
        Object.DestroyImmediate(leader);
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
    }

    static EnemyHealth Husk(string name, Vector3 pos, int hp = 30)
    {
        var go = new GameObject(name);
        go.tag = "Husk";
        go.transform.position = pos;
        var h = go.AddComponent<EnemyHealth>();
        h.SetTier(hp, LootTier.Normal);
        return h;
    }

    static PartyCompanion ByRole(PartyRoster roster, ClassDefinition.PartyRole role)
    {
        foreach (var c in roster.Companions)
            if (c != null && c.Role.role == role) return c;
        return null;
    }

    [UnityTest]
    public IEnumerator FillsTheTwoMissingRoles()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        Assert.IsNotNull(roster);
        yield return null; // companions wire their class in Start
        Assert.AreEqual(2, roster.Companions.Count);
        var tank = ByRole(roster, ClassDefinition.PartyRole.Tank);
        var healer = ByRole(roster, ClassDefinition.PartyRole.Healer);
        Assert.IsNotNull(tank, "rogue gets a tank");
        Assert.IsNotNull(healer, "rogue gets a healer");
        Assert.IsNull(ByRole(roster, ClassDefinition.PartyRole.Dps), "no second dps");
        Assert.AreSame(ClassCatalog.Fenwalker, tank.Role);
        Assert.AreSame(ClassCatalog.Lanternbearer, healer.Role);
        Assert.IsNotNull(tank.GetComponent<PlayerHealth>());
        Assert.IsNotNull(tank.GetComponent<DiabloSkills>());
        Assert.AreSame(ClassCatalog.Fenwalker, tank.GetComponent<DiabloSkills>().ActiveClass);
        Assert.AreEqual(3, HeroLocator.AllHeroes().Count, "leader plus two companions");
        // Idempotent: a second call adds nothing.
        PartyRoster.EnsureSoloParty(leader.transform);
        Assert.AreEqual(2, roster.Companions.Count);
    }

    [UnityTest]
    public IEnumerator TankLeader_GetsHealerAndDps()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        Assert.AreEqual(2, roster.Companions.Count);
        Assert.IsNotNull(ByRole(roster, ClassDefinition.PartyRole.Healer));
        Assert.IsNotNull(ByRole(roster, ClassDefinition.PartyRole.Dps));
        Assert.IsNull(ByRole(roster, ClassDefinition.PartyRole.Tank));
    }

    [UnityTest]
    public IEnumerator Party_FightsAndHealsOnItsOwn()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var leaderHp = leader.GetComponent<PlayerHealth>();
        leaderHp.TakeDamage(10f);
        float hurt = leaderHp.CurrentHP;
        var husk = Husk("FightHusk", new Vector3(0f, 0.05f, 5f), 30);
        PartyRoster.EnsureSoloParty(leader.transform);
        yield return DiabloTestFixture.WaitSeconds(2.0f);
        Assert.Less(husk.HPFraction(), 1f, "companions damage the husk without orders");
        Assert.Greater(leaderHp.CurrentHP, hurt, "the healer mends the leader");
    }

    [UnityTest]
    public IEnumerator DeadCompanion_RejoinsAtLeader()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        var tank = ByRole(roster, ClassDefinition.PartyRole.Tank);
        Assert.IsNotNull(tank);
        tank.GetComponent<PlayerHealth>().TakeDamage(15f);
        tank.Die();
        Assert.IsFalse(tank.gameObject.activeInHierarchy, "death stands the companion down");
        Assert.IsFalse(HeroLocator.AllHeroes().Contains(tank.transform), "the dead leave the fighting party");
        tank.Rejoin();
        Assert.IsTrue(tank.gameObject.activeInHierarchy);
        Assert.AreEqual(tank.GetComponent<PlayerHealth>().MaxHP, tank.GetComponent<PlayerHealth>().CurrentHP, 0.0001f);
        Assert.AreEqual(leader.transform.position, tank.transform.position, "rejoin lands on the leader");
        Assert.IsTrue(HeroLocator.AllHeroes().Contains(tank.transform));
    }

    [UnityTest]
    public IEnumerator Companions_PortWithLeaderThroughZoneTravel()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        var arrival = new Vector3(200f, 0.05f, 2f);
        ZoneTravel.TeleportHero(leader, arrival);
        foreach (var c in roster.Companions)
        {
            var d = c.transform.position - arrival;
            d.y = 0f;
            Assert.Less(d.magnitude, 4f, $"{c.name} arrives with the leader (playtest 2026-11-23)");
        }
        Assert.AreNotEqual(roster.Companions[0].transform.position, roster.Companions[1].transform.position,
            "each companion takes its own formation slot");
    }

    [UnityTest]
    public IEnumerator Companions_WalkWithTheLeader()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        // Hero walks 9 m north at hero speed; the party must keep formation, never leash-teleport.
        float t = 0f;
        while (t < 1.8f)
        {
            leader.transform.position += Vector3.forward * PartyCompanion.MoveSpeed * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }
        yield return DiabloTestFixture.WaitSeconds(0.8f);
        foreach (var c in roster.Companions)
        {
            var d = c.transform.position - c.FollowPoint;
            d.y = 0f;
            Assert.Less(d.magnitude, 1.0f, $"{c.name} walks back into its formation slot");
        }
    }

    [UnityTest]
    public IEnumerator Enemies_TargetCompanionsThroughThreat()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        var tank = ByRole(roster, ClassDefinition.PartyRole.Tank);
        var husk = Husk("AggroHusk", tank.transform.position + new Vector3(0f, 0f, 2f), 30);
        var atk = husk.gameObject.AddComponent<EnemyAttack>();
        var tankSkills = tank.GetComponent<DiabloSkills>();
        tankSkills.DoSlam(cosmetic: false);
        Assert.AreSame(tank.transform, atk.AcquireHero(), "slam threat pulls aggro to the companion");
    }
}
