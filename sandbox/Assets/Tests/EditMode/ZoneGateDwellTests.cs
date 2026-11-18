using NUnit.Framework;
using UnityEngine;

/// <summary>Fix 2026-11-18: the Fen Gate fires only after the hero stands on it for DwellSeconds.</summary>
public class ZoneGateDwellTests
{
    GameObject hero;
    GameObject gate;
    ZoneGate zg;

    [SetUp]
    public void Setup()
    {
        ZoneGate.ResetRun();
        hero = new GameObject("Player");
        hero.AddComponent<PlayerHealth>();
        gate = new GameObject(ZoneGate.FenGateName);
        zg = gate.AddComponent<ZoneGate>();
        zg.destination = new Vector3(200f, 1f, 10f);
        zg.targetZone = 2;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(hero);
        Object.DestroyImmediate(gate);
        ZoneGate.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    [Test]
    public void RunningAcross_DoesNotTeleport()
    {
        zg.BeginDwell(hero, 10f);
        Assert.IsFalse(zg.TickDwell(10f + ZoneGate.DwellSeconds * 0.5f, -1f));
        zg.EndDwell(hero);
        Assert.IsFalse(zg.TickDwell(20f, -1f));
        Assert.AreEqual(0, ZoneGate.Teleports);
        Assert.AreNotEqual(zg.destination, hero.transform.position);
    }

    [Test]
    public void StandingOnTheGate_Teleports()
    {
        zg.BeginDwell(hero, 10f);
        Assert.IsTrue(zg.TickDwell(10f + ZoneGate.DwellSeconds, -1f));
        Assert.AreEqual(1, ZoneGate.Teleports);
        Assert.AreEqual(zg.destination, hero.transform.position);
        Assert.IsFalse(zg.TickDwell(30f, -1f), "fires once per dwell");
    }

    [Test]
    public void FightingOnTheGate_RestartsTheDwell()
    {
        zg.BeginDwell(hero, 10f);
        // Hits at 10.5 and 11.0: the dwell restarts each time.
        Assert.IsFalse(zg.TickDwell(10.55f, 10.5f));
        Assert.IsFalse(zg.TickDwell(11.05f, 11.0f));
        Assert.IsFalse(zg.TickDwell(11.0f + ZoneGate.DwellSeconds * 0.9f, 11.0f));
        Assert.AreEqual(0, ZoneGate.Teleports);
        Assert.IsTrue(zg.TickDwell(11.0f + ZoneGate.DwellSeconds, 11.0f), "stops fighting, stands still, travels");
        Assert.AreEqual(1, ZoneGate.Teleports);
    }

    [Test]
    public void HeroMovedOffWithoutExitEvent_DoesNotTeleport()
    {
        var box = gate.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.4f, 2.4f, 0.6f);
        hero.transform.position = gate.transform.position;
        zg.BeginDwell(hero, 10f);
        // Nudged 3 m away with the CharacterController disabled: no OnTriggerExit.
        hero.transform.position = gate.transform.position + new Vector3(3f, 0f, 3f);
        Assert.IsFalse(zg.TickDwell(10f + ZoneGate.DwellSeconds * 2f, -1f));
        Assert.AreEqual(0, ZoneGate.Teleports);
    }

    [Test]
    public void WalkingAlongTheGate_DoesNotTeleport()
    {
        zg.BeginDwell(hero, 10f);
        // 2 m in 0.6 s along the gate width (seed-1 autoplay trace, 2026-11-18).
        for (int i = 1; i <= 6; i++)
        {
            hero.transform.position = new Vector3(i * 0.35f, 0f, 0f);
            Assert.IsFalse(zg.TickDwell(10f + i * 0.1f, -1f), $"step {i}");
        }
        Assert.AreEqual(0, ZoneGate.Teleports);
        // Then stands still: travels after DwellSeconds.
        Assert.IsTrue(zg.TickDwell(10.6f + ZoneGate.DwellSeconds, -1f));
    }

    [Test]
    public void OnlyTheHeroStartsADwell()
    {
        var husk = new GameObject("Husk_0");
        zg.BeginDwell(husk, 0f);
        Assert.IsFalse(zg.TickDwell(5f, -1f));
        Object.DestroyImmediate(husk);
        Assert.Greater(ZoneGate.DwellSeconds, 0.3f, "long enough that a chase step does not fire it");
    }
}
