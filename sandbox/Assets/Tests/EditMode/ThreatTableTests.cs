using NUnit.Framework;
using UnityEngine;

/// <summary>cinderreach-party-roles S2: threat table booking, top-source targeting, slam bonus.</summary>
public class ThreatTableTests
{
    GameObject enemy;
    GameObject heroA;
    GameObject heroB;

    [SetUp]
    public void Setup()
    {
        enemy = new GameObject("ThreatEnemy");
        heroA = Hero("HeroA");
        heroB = Hero("HeroB");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(heroA);
        Object.DestroyImmediate(heroB);
    }

    static GameObject Hero(string name)
    {
        var go = new GameObject(name);
        go.AddComponent<PlayerHealth>();
        return go;
    }

    [Test]
    public void SkillEnum_AppendsSlamAtSeven()
    {
        Assert.AreEqual(0, (int)DiabloSkills.Skill.Fireball);
        Assert.AreEqual(2, (int)DiabloSkills.Skill.Cleave);
        Assert.AreEqual(6, (int)DiabloSkills.Skill.VeilStep);
        Assert.AreEqual(7, (int)DiabloSkills.Skill.SeismicSlam);
    }

    [Test]
    public void AddThreat_AccumulatesAndIgnoresGarbage()
    {
        var table = enemy.AddComponent<ThreatTable>();
        table.AddThreat(heroA.transform, 3f);
        table.AddThreat(heroA.transform, 2f);
        Assert.AreEqual(5f, table.ThreatOf(heroA.transform), 0.0001f);
        table.AddThreat(null, 10f);
        table.AddThreat(heroB.transform, 0f);
        table.AddThreat(heroB.transform, -4f);
        Assert.AreEqual(0f, table.ThreatOf(heroB.transform), 0.0001f);
        Assert.AreEqual(0f, table.ThreatOf(null), 0.0001f);
    }

    [Test]
    public void TopSource_PicksHighestAndSkipsDead()
    {
        var table = enemy.AddComponent<ThreatTable>();
        Assert.IsNull(table.TopSource(), "empty table targets nobody");
        table.AddThreat(heroA.transform, 2f);
        table.AddThreat(heroB.transform, 5f);
        Assert.AreSame(heroB.transform, table.TopSource());
        heroB.GetComponent<PlayerHealth>().TakeDamage(9999f);
        Assert.AreSame(heroA.transform, table.TopSource(), "dead heroes lose aggro, threat stays booked");
        Assert.AreEqual(5f, table.ThreatOf(heroB.transform), 0.0001f);
    }

    [Test]
    public void TopSource_SkipsInactiveHeroes()
    {
        var table = enemy.AddComponent<ThreatTable>();
        table.AddThreat(heroA.transform, 9f);
        heroA.SetActive(false);
        Assert.IsNull(table.TopSource());
        heroA.SetActive(true);
        Assert.AreSame(heroA.transform, table.TopSource());
    }

    [Test]
    public void BookDamage_CreatesTableAndBooksOneToOne()
    {
        Assert.IsNull(enemy.GetComponent<ThreatTable>());
        ThreatTable.BookDamage(enemy, heroA.transform, 4f);
        var table = enemy.GetComponent<ThreatTable>();
        Assert.IsNotNull(table);
        Assert.AreEqual(4f * ThreatTable.DamageThreatPerPoint, table.ThreatOf(heroA.transform), 0.0001f);
        ThreatTable.BookDamage(null, heroA.transform, 4f);
        ThreatTable.BookDamage(enemy, null, 4f);
        ThreatTable.BookDamage(enemy, heroA.transform, 0f);
        Assert.AreEqual(4f, table.ThreatOf(heroA.transform), 0.0001f);
    }

    [Test]
    public void BookStun_AddsBonusOnTop()
    {
        ThreatTable.BookDamage(enemy, heroA.transform, 3f);
        ThreatTable.BookStun(enemy, heroA.transform);
        var table = enemy.GetComponent<ThreatTable>();
        Assert.AreEqual(3f + ThreatTable.SlamThreatBonus, table.ThreatOf(heroA.transform), 0.0001f);
        Assert.AreEqual(20f, ThreatTable.SlamThreatBonus, 0.0001f);
    }

    [Test]
    public void Clear_ForgetsAllThreat()
    {
        var table = enemy.AddComponent<ThreatTable>();
        table.AddThreat(heroA.transform, 7f);
        table.Clear();
        Assert.IsNull(table.TopSource());
    }
}
