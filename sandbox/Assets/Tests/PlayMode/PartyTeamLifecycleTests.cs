using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// party-team: computer teammates spawn and despawn together with teamplay.
/// The spawned party fights, heals and holds threat; a joiner despawns its
/// role while the rest keeps playing; a leaver refills the party; the dead
/// hold their slot until they rejoin; an overfull join changes nothing.
/// </summary>
public class PartyTeamLifecycleTests
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
        leader.AddComponent<Equipment>();
        leader.AddComponent<PlayerHealth>();
        leader.AddComponent<DiabloProgression>();
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
    public IEnumerator SpawnedParty_FightsHealsAndHoldsThreat()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        leader.GetComponent<DiabloProgression>().SetState(3, 0);
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        Assert.IsNotNull(roster);
        yield return null; // companions wire their class and mirror the hero level
        Assert.AreEqual(2, roster.Companions.Count);
        var tank = ByRole(roster, ClassDefinition.PartyRole.Tank);
        var healer = ByRole(roster, ClassDefinition.PartyRole.Healer);
        Assert.IsNotNull(tank, "rogue gets a tank");
        Assert.IsNotNull(healer, "rogue gets a healer");
        Assert.AreEqual(3, tank.GetComponent<DiabloProgression>().Level,
            "companions arrive on the hero's level");
        Assert.AreEqual(3, HeroLocator.AllHeroes().Count, "leader plus two companions");

        var leaderHp = leader.GetComponent<PlayerHealth>();
        leaderHp.TakeDamage(10f);
        float hurt = leaderHp.CurrentHP;
        var husk = Husk("FightHusk", new Vector3(0f, 0.05f, 5f), 30);
        yield return DiabloTestFixture.WaitSeconds(2.0f);
        Assert.Less(husk.HPFraction(), 1f, "spawned companions damage the husk without orders");
        Assert.Greater(leaderHp.CurrentHP, hurt, "the spawned healer mends the leader");

        var aggro = Husk("AggroHusk", tank.transform.position + new Vector3(0f, 0f, 2f), 30);
        var atk = aggro.gameObject.AddComponent<EnemyAttack>();
        tank.GetComponent<DiabloSkills>().DoSlam(cosmetic: false);
        Assert.AreSame(tank.transform, atk.AcquireHero(), "slam threat pulls aggro to the spawned tank");
    }

    [UnityTest]
    public IEnumerator JoinerDespawnsSameRole_TeamKeepsPlaying()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId; // tank leader -> healer + dps
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        Assert.AreEqual(2, roster.Companions.Count);

        int removed = roster.ApplyRemoteRoles(
            new List<ClassDefinition.PartyRole> { ClassDefinition.PartyRole.Dps });
        Assert.AreEqual(1, removed, "the joining dps steps its companion aside");
        yield return null; // deferred destroy runs
        Assert.AreEqual(1, roster.Companions.Count);
        Assert.IsNotNull(ByRole(roster, ClassDefinition.PartyRole.Healer), "the healer stays");
        Assert.IsNull(ByRole(roster, ClassDefinition.PartyRole.Dps), "the dps companion is gone");
        Assert.AreEqual(2, HeroLocator.AllHeroes().Count, "leader plus the remaining companion");

        var leaderHp = leader.GetComponent<PlayerHealth>();
        leaderHp.TakeDamage(10f);
        float hurt = leaderHp.CurrentHP;
        var husk = Husk("RemainHusk", new Vector3(0f, 0.05f, 4f), 30);
        yield return DiabloTestFixture.WaitSeconds(2.0f);
        Assert.Greater(leaderHp.CurrentHP, hurt, "the remaining healer still mends after the despawn");
        Assert.Less(husk.HPFraction(), 1f, "the reduced party still damages the husk");
    }

    [UnityTest]
    public IEnumerator LeaverRefillsSameRole_TeamWholeAgain()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        Assert.AreEqual(1, roster.ApplyRemoteRoles(
            new List<ClassDefinition.PartyRole> { ClassDefinition.PartyRole.Dps }));
        yield return null;
        Assert.AreEqual(1, roster.Companions.Count);

        int removed = roster.ApplyRemoteRoles(new List<ClassDefinition.PartyRole>());
        Assert.AreEqual(0, removed, "nobody steps aside when the party is already short");
        yield return null; // refill spawns and mirrors the hero level
        Assert.AreEqual(2, roster.Companions.Count, "the leaver's role refills");
        var dps = ByRole(roster, ClassDefinition.PartyRole.Dps);
        Assert.IsNotNull(dps, "the dps is back");
        Assert.IsTrue(dps.gameObject.activeInHierarchy);
        var dpsHp = dps.GetComponent<PlayerHealth>();
        Assert.AreEqual(dpsHp.MaxHP, dpsHp.CurrentHP, 0.0001f, "the refilled companion arrives whole");
        Assert.AreEqual(3, HeroLocator.AllHeroes().Count, "the fighting party is whole again");
        Assert.IsNotNull(dps.Gear, "the refilled companion is itemizable");
    }

    [UnityTest]
    public IEnumerator OverfullJoin_ChangesNothing()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        int removed = roster.ApplyRemoteRoles(new List<ClassDefinition.PartyRole>
        {
            ClassDefinition.PartyRole.Tank,
            ClassDefinition.PartyRole.Healer,
            ClassDefinition.PartyRole.Dps,
        });
        Assert.AreEqual(-1, removed, "join gate: a fourth hero is refused before anything despawns");
        Assert.AreEqual(2, roster.Companions.Count, "the party is untouched");
        yield return null;
        Assert.AreEqual(2, roster.Companions.Count);
        Assert.AreEqual(3, HeroLocator.AllHeroes().Count);
    }

    [UnityTest]
    public IEnumerator DeadCompanion_HoldsSlot_ThenRejoins()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        var tank = ByRole(roster, ClassDefinition.PartyRole.Tank);
        Assert.IsNotNull(tank);
        tank.Die();
        Assert.IsFalse(tank.RejoinDue, "the camp timer is armed, the roster picks it up later");
        PartyRoster.EnsureSoloParty(leader.transform);
        Assert.AreEqual(2, roster.Companions.Count, "the dead hold their slot: no duplicate spawns");
        Assert.IsFalse(HeroLocator.AllHeroes().Contains(tank.transform),
            "the dead leave the fighting party");
        tank.Rejoin();
        yield return null;
        PartyRoster.EnsureSoloParty(leader.transform);
        Assert.AreEqual(2, roster.Companions.Count, "no duplicate after the rejoin either");
        Assert.IsTrue(HeroLocator.AllHeroes().Contains(tank.transform), "the rejoined fights again");
    }
}
