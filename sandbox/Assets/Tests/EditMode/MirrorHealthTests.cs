using NUnit.Framework;
using UnityEngine;

/// <summary>
/// coop-hp-sync: a co-op mirror shows its owner's HP. Local hits, heals and
/// deaths change nothing on it; the owner re-sends only real changes.
/// </summary>
public class MirrorHealthTests
{
    GameObject go;

    [SetUp]
    public void Setup() => go = new GameObject("Mirror");

    [TearDown]
    public void Teardown() => Object.DestroyImmediate(go);

    [Test]
    public void Mirror_FollowsOwner_IgnoresLocalHitsHealsAndDeath()
    {
        var hp = go.AddComponent<PlayerHealth>();
        hp.SetMirrored(0f, 0f);
        Assert.IsFalse(hp.IsMirror, "no owner report yet: stays a local hero");

        hp.SetMirrored(14f, 30f);
        Assert.IsTrue(hp.IsMirror);
        Assert.AreEqual(30f, hp.MaxHP);
        Assert.AreEqual(14f, hp.CurrentHP);

        hp.TakeDamage(5f);
        hp.TakeDamage(5f, Vector3.zero);
        Assert.AreEqual(14f, hp.CurrentHP, "enemy hits on this process are not the owner's");
        Assert.AreEqual(10f, hp.Heal(10f), "predicted gain");
        Assert.AreEqual(14f, hp.CurrentHP, "the heal lands at the owner");
        Assert.AreEqual(16f, hp.Heal(99f), "prediction clamps at max");

        var before = go.transform.position;
        hp.SetMirrored(0f, 30f);
        hp.DieAndRespawn();
        Assert.AreEqual(before, go.transform.position, "only the owner respawns its hero");
        Assert.AreEqual(0f, hp.CurrentHP);
    }

    [Test]
    public void LocalHero_HealReturnsRestored()
    {
        var hp = go.AddComponent<PlayerHealth>();
        hp.TakeDamage(4f);
        Assert.AreEqual(4f, hp.Heal(10f));
        Assert.AreEqual(hp.MaxHP, hp.CurrentHP);
        Assert.AreEqual(0f, hp.Heal(10f));
    }

    [Test]
    public void HpSync_SendsFirstReportAndRealChangesOnly()
    {
        Assert.IsTrue(DiabloNetworkPlayer.NeedsHpSync(Vector2.zero, new Vector2(20f, 20f)), "first report");
        Assert.IsFalse(DiabloNetworkPlayer.NeedsHpSync(new Vector2(15f, 20f), new Vector2(15.01f, 20f)), "noise");
        Assert.IsTrue(DiabloNetworkPlayer.NeedsHpSync(new Vector2(20f, 20f), new Vector2(19.99f, 20f)),
            "leaving full HP always syncs: the mirror becomes a heal target");
        Assert.IsTrue(DiabloNetworkPlayer.NeedsHpSync(new Vector2(20f, 20f), new Vector2(17f, 20f)), "hit");
        Assert.IsTrue(DiabloNetworkPlayer.NeedsHpSync(new Vector2(20f, 20f), new Vector2(20f, 26f)), "gear raised max");
        Assert.IsTrue(DiabloNetworkPlayer.NeedsHpSync(new Vector2(0.03f, 20f), new Vector2(0f, 20f)), "death always syncs");
        Assert.IsTrue(DiabloNetworkPlayer.NeedsHpSync(new Vector2(19.98f, 20f), new Vector2(20f, 20f)), "whole again always syncs");
    }
}
