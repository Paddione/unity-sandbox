using NUnit.Framework;
using UnityEngine;

/// <summary>cinderreach-encounter-roles S1: layout variants, cover axes, retreat routes.</summary>
public class EncounterLayoutTests
{
    [Test]
    public void ForVariant_IsDeterministicAndRotates()
    {
        for (int v = 0; v < EncounterLayout.Variants; v++)
        {
            Assert.AreEqual(EncounterLayout.OssuaryWardenAnchor(v, 0), EncounterLayout.OssuaryWardenAnchor(v, 0));
            Assert.AreEqual(EncounterLayout.KilnThrallAnchor(v, 3), EncounterLayout.KilnThrallAnchor(v, 3));
        }
        Assert.AreNotEqual(EncounterLayout.OssuaryWardenAnchor(0, 0), EncounterLayout.OssuaryWardenAnchor(1, 0));
        Assert.AreNotEqual(EncounterLayout.KilnThrallAnchor(0, 0), EncounterLayout.KilnThrallAnchor(1, 0));
    }

    [Test]
    public void Variant0_MatchesBuilderLayout()
    {
        var off = KilnLayout.OssuaryOffset;
        Assert.AreEqual(off + new Vector3(7f, 0.05f, 0f), EncounterLayout.OssuaryWardenAnchor(0, 0));
        Assert.AreEqual(off + new Vector3(-7f, 0.05f, 0f), EncounterLayout.OssuaryWardenAnchor(0, 1));
        for (int i = 0; i < KilnLayout.ThrallCount; i++)
            Assert.AreEqual(KilnLayout.ThrallAnchor(0, i), EncounterLayout.KilnThrallAnchor(0, i));
        for (int i = 0; i < KilnLayout.LancerCount; i++)
            Assert.AreEqual(KilnLayout.LancerAnchor(0, i), EncounterLayout.KilnLancerAnchor(0, i));
    }

    [Test]
    public void EveryVariant_HasSafeArrivalAndReachableRetreat()
    {
        for (int v = 0; v < EncounterLayout.Variants; v++)
        {
            // Retreat is the return gate: arrival never bounces back through it.
            Assert.AreEqual(KilnLayout.OssuaryOffset + new Vector3(0f, 1f, 2f), EncounterLayout.OssuaryArrival);
            Assert.AreEqual(KilnLayout.OssuaryOffset + new Vector3(0f, 1f, 10f), EncounterLayout.OssuaryRetreat);
            Assert.AreEqual(KilnLayout.KilnArrival, EncounterLayout.KilnArrival);
            Assert.AreEqual(KilnLayout.KilnReturnPos, EncounterLayout.KilnRetreat);
            float ossuaryWalk = Vector3.Distance(EncounterLayout.OssuaryArrival, EncounterLayout.OssuaryRetreat);
            float kilnWalk = Vector3.Distance(EncounterLayout.KilnArrival, EncounterLayout.KilnRetreat);
            Assert.Less(ossuaryWalk, 25f, $"v{v} ossuary retreat reachable");
            Assert.Less(kilnWalk, 25f, $"v{v} kiln retreat reachable");
            // Retreat is behind the arrival, away from the boss: no crossfire on the way out.
            Assert.Greater(EncounterLayout.OssuaryRetreat.z, EncounterLayout.OssuaryArrival.z);
        }
    }

    [Test]
    public void WardenPair_StandsOnInterruptedSightAxes()
    {
        var boss = KilnLayout.OssuaryOffset + new Vector3(0f, 0.05f, -10f);
        for (int v = 0; v < EncounterLayout.Variants; v++)
            for (int k = 0; k < 2; k++)
            {
                var warden = EncounterLayout.OssuaryWardenAnchor(v, k);
                var cover = EncounterLayout.OssuaryCoverAnchor(v, k);
                Assert.IsTrue(EncounterLayout.BlockedByCover(
                    EncounterLayout.OssuaryArrival, warden, cover, EncounterLayout.CoverRadius),
                    $"v{v} pair{k}: cover interrupts the sight axis");
                Assert.IsFalse(EncounterLayout.BlockedByCover(
                    EncounterLayout.OssuaryArrival, boss, cover, EncounterLayout.CoverRadius),
                    $"v{v} pair{k}: boss axis stays open");
            }
    }

    [Test]
    public void KilnFrontAndBack_AreSeparableAtTheChoke()
    {
        for (int v = 0; v < EncounterLayout.Variants; v++)
        {
            int front = 0, back = 0;
            float minGap = float.MaxValue;
            for (int i = 0; i < KilnLayout.ThrallCount; i++)
            {
                if (EncounterLayout.IsKilnFront(i)) front++;
                else back++;
                for (int j = 0; j < KilnLayout.ThrallCount; j++)
                {
                    if (EncounterLayout.IsKilnFront(j) == EncounterLayout.IsKilnFront(i)) continue;
                    float d = Vector3.Distance(
                        EncounterLayout.KilnThrallAnchor(v, i), EncounterLayout.KilnThrallAnchor(v, j));
                    if (d < minGap) minGap = d;
                }
            }
            Assert.AreEqual(9, front, $"v{v} front packs 0-2");
            Assert.AreEqual(9, back, $"v{v} back packs 3-5");
            Assert.Greater(minGap, 3f, $"v{v} front/back gap for independent trigger (got {minGap:0.00})");
            // Hunter group: first two lancers fight with the Kiln Wardens.
            Assert.IsTrue(EncounterLayout.IsKilnHunterLancer(0));
            Assert.IsTrue(EncounterLayout.IsKilnHunterLancer(1));
            Assert.IsFalse(EncounterLayout.IsKilnHunterLancer(2));
        }
    }

    [Test]
    public void Choke_SplitsArrivalFromBoss()
    {
        Assert.IsFalse(EncounterLayout.PassedChoke(EncounterLayout.KilnArrival), "arrival before the choke");
        Assert.IsFalse(EncounterLayout.PassedChoke(new Vector3(0f, 1f, 2f)), "fen never past the kiln choke");
        Assert.IsTrue(EncounterLayout.PassedChoke(KilnLayout.KilnmotherPos), "boss behind the choke");
    }

    [Test]
    public void Zones_FollowAnnexOffsets()
    {
        Assert.AreEqual(ActProgress.Town, EncounterLayout.CurrentZone(new Vector3(0f, 0f, 0f)));
        Assert.AreEqual(ActProgress.Ossuary, EncounterLayout.CurrentZone(new Vector3(200f, 0f, 0f)));
        Assert.AreEqual(ActProgress.Kiln, EncounterLayout.CurrentZone(new Vector3(400f, 0f, 0f)));
    }

    [Test]
    public void BackGroup_ReleasesAtTheChoke()
    {
        EncounterLayout.ResetEncounter();
        Assert.IsTrue(EncounterLayout.GroupActive(EncounterLayout.FrontGroup, EncounterLayout.KilnArrival));
        Assert.IsFalse(EncounterLayout.GroupActive(EncounterLayout.BackGroup, EncounterLayout.KilnArrival));
        EncounterLayout.NoteHeroPosition(KilnLayout.KilnmotherPos);
        Assert.IsTrue(EncounterLayout.GroupActive(EncounterLayout.BackGroup, EncounterLayout.KilnArrival), "release latches");
        EncounterLayout.ResetEncounter();
        Assert.IsFalse(EncounterLayout.GroupActive(EncounterLayout.BackGroup, EncounterLayout.KilnArrival));
    }
}
