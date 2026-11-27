using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-group-quest-map: quest tracking (flags, order, rows, tracker
/// text), quest destinations with zone routing, map projection math, the
/// shared-dialog protocol and listener window, and the host bark relay.
/// </summary>
public class GroupQuestMapTests
{
    sealed class FakeWorld : IQuestWorld
    {
        public readonly Dictionary<string, Vector3> npcs = new Dictionary<string, Vector3>();
        public readonly Dictionary<string, List<Vector3>> pickups = new Dictionary<string, List<Vector3>>();
        public readonly Dictionary<string, List<Vector3>> enemies = new Dictionary<string, List<Vector3>>();
        public readonly Dictionary<(int, int), Vector3> gates = new Dictionary<(int, int), Vector3>();

        public bool TryNpc(string npcId, out Vector3 position) => npcs.TryGetValue(npcId, out position);
        public void Pickups(string itemId, List<Vector3> into) { if (pickups.TryGetValue(itemId, out var l)) into.AddRange(l); }
        public void Enemies(string killTarget, List<Vector3> into) { if (enemies.TryGetValue(killTarget, out var l)) into.AddRange(l); }
        public bool TryGate(int from, int to, out Vector3 position) => gates.TryGetValue((from, to), out position);
    }

    static readonly Vector3 Ottilie = new Vector3(1.5f, 0f, 14.5f);
    static readonly Vector3 FenGate = new Vector3(0f, 0f, -16f);

    static StoryState H1Active()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        Assert.IsTrue(s.Accept("h1"));
        s.Refresh(new ActProgress());
        return s;
    }

    [SetUp]
    public void Setup()
    {
        StoryDialogShare.ResetForTests();
        StoryDialogShare.Send = null;
        StoryBarks.ResetForTests();
        StoryBarks.Relay = null;
    }

    [TearDown]
    public void TearDown()
    {
        StoryDialogShare.Send = null;
        StoryBarks.Relay = null;
        StoryBarks.ResetForTests();
    }

    // ---- tracking ----

    [Test]
    public void Tracking_ActiveQuestsTrackedMainFirst_HideSurvivesSave()
    {
        var s = H1Active();
        Assert.IsTrue(s.Accept("n1"));
        var tracked = StoryTracking.Tracked(s);
        Assert.AreEqual(new[] { "h1", "n1" }, tracked.ConvertAll(q => q.id).ToArray());

        Assert.IsFalse(StoryTracking.Toggle(s, "h1"), "toggle hides");
        Assert.AreEqual(new[] { "n1" }, StoryTracking.Tracked(s).ConvertAll(q => q.id).ToArray());

        var copy = StoryState.FromSave(s.ToSave(), new List<string>(s.Flags));
        Assert.IsFalse(StoryTracking.IsTracked(copy, "h1"), "hidden choice is saved with the story flags");
        Assert.IsTrue(StoryTracking.Toggle(copy, "h1"), "toggle again tracks");
        Assert.IsTrue(StoryTracking.IsTracked(copy, "h1"));
        Assert.IsFalse(copy.Snapshot().Contains("untrack"), "tracking never travels in the co-op snapshot");
    }

    [Test]
    public void Tracking_OffersAndOpenObjectives()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        Assert.AreEqual(new[] { "h1" }, StoryTracking.Offers(s).ConvertAll(q => q.id).ToArray());
        Assert.IsEmpty(StoryTracking.Tracked(s), "offers are not tracked yet");
        s.Accept("h1");
        var h1 = StoryCatalog.Get("h1");
        Assert.AreEqual(new[] { 0, 1 }, StoryTracking.OpenObjectives(s, h1).ToArray());
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        Assert.AreEqual(new[] { 1 }, StoryTracking.OpenObjectives(s, h1).ToArray());
    }

    [Test]
    public void Tracker_ListsObjectivesCountsDistanceAndHandIn()
    {
        var s = H1Active();
        s.Accept("h2"); // not available yet: stays out
        var world = new FakeWorld();
        world.pickups[StoryIds.Fuhrbrief] = new List<Vector3> { new Vector3(-10.6f, 0.5f, 1.6f) };
        world.enemies[StoryIds.FenBrute] = new List<Vector3> { new Vector3(-9f, 0f, 2f) };
        world.npcs[StoryIds.Ottilie] = Ottilie;
        var marks = new List<QuestMark>();
        var hero = new Vector3(-10.6f, 0f, 11.6f);
        QuestDestinations.Collect(s, world, hero, marks);
        string text = StoryDirector.TrackerText(s, world, marks, hero);
        StringAssert.Contains(Loc.T("quest.h1.title"), text);
        StringAssert.Contains(Loc.T("quest.h1.obj.letter"), text);
        StringAssert.Contains(Loc.T("quest.h1.obj.brute"), text);
        StringAssert.Contains(Loc.F("story.tracker.meters", 10), text, "metres to the nearest place in this zone");

        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute);
        Assert.AreEqual(StoryStatus.Ready, s.Status("h1"));
        QuestDestinations.Collect(s, world, hero, marks);
        text = StoryDirector.TrackerText(s, world, marks, hero);
        StringAssert.Contains(Loc.F("story.tracker.handin", Loc.T("npc.ottilie.name")), text);
    }

    [Test]
    public void ProgressToast_ShowsCountAndTick()
    {
        var h2 = StoryCatalog.Get("h2");
        string two = StoryDirector.ProgressToast(h2, new StoryChange { questId = "h2", objective = 1, count = 2 });
        Assert.AreEqual(Loc.T("quest.h2.obj.wardens") + "  2/4", two);
        string done = StoryDirector.ProgressToast(h2, new StoryChange { questId = "h2", objective = 1, count = 4 });
        StringAssert.EndsWith("4/4 ✓", done);
    }

    // ---- destinations ----

    [Test]
    public void Destinations_TalkCollectKillEnterAndOffers()
    {
        var s = H1Active();
        var world = new FakeWorld();
        world.npcs[StoryIds.Ottilie] = Ottilie;
        world.npcs[StoryIds.Ilsabe] = new Vector3(-3.5f, 0f, 11f);
        world.pickups[StoryIds.Fuhrbrief] = new List<Vector3> { new Vector3(-10f, 0f, 2f) };
        world.enemies[StoryIds.FenBrute] = new List<Vector3>
        {
            new Vector3(10f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(3f, 0f, 0f),
        };
        var marks = new List<QuestMark>();
        QuestDestinations.Collect(s, world, Vector3.zero, marks);

        var kills = marks.FindAll(m => m.questId == "h1" && m.objective == 1);
        Assert.AreEqual(QuestDestinations.MaxKillMarks, kills.Count, "nearest kill targets only");
        Assert.AreEqual(1f, kills[0].position.x, 1e-4f);
        Assert.AreEqual(3f, kills[1].position.x, 1e-4f);
        Assert.IsTrue(marks.Exists(m => m.questId == "h1" && m.objective == 0 && m.position.x == -10f));
        Assert.IsTrue(marks.Exists(m => m.questId == "n1" && m.kind == QuestMarkKind.Offer), "Ilsabe offers n1");
        Assert.IsTrue(marks.TrueForAll(m => m.zone == ActProgress.Town));

        // h2 entering the Ossuary points at that zone.
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute);
        s.TurnIn("h1");
        s.Refresh(new ActProgress());
        s.Accept("h2");
        QuestDestinations.Collect(s, world, Vector3.zero, marks);
        var enter = marks.Find(m => m.questId == "h2" && m.objective == 0);
        Assert.AreEqual(ActProgress.Ossuary, enter.zone);
    }

    [Test]
    public void Route_OtherZoneGoesThroughGateTowardIt()
    {
        var world = new FakeWorld();
        world.gates[(ActProgress.Town, ActProgress.Ossuary)] = FenGate;
        var mark = new QuestMark { questId = "h4", zone = ActProgress.Kiln, position = KilnLayout.KilnOffset };
        Assert.IsTrue(QuestDestinations.Route(world, ActProgress.Town, mark, out var step));
        Assert.AreEqual(FenGate, step, "Town → Kiln goes through the Fen gate first");
        Assert.IsFalse(QuestDestinations.Route(world, ActProgress.Barrens, mark, out _), "no gate known");
        mark.zone = ActProgress.Town;
        Assert.IsTrue(QuestDestinations.Route(world, ActProgress.Town, mark, out step));
        Assert.AreEqual(mark.position, step);

        var marks = new List<QuestMark>
        {
            new QuestMark { questId = "x", zone = ActProgress.Kiln, position = KilnLayout.KilnOffset },
            new QuestMark { questId = "x", zone = ActProgress.Town, position = new Vector3(12f, 0f, 0f) },
        };
        Assert.IsTrue(QuestDestinations.NearestStep(world, marks, "x", Vector3.zero, out step, out int zone));
        Assert.AreEqual(ActProgress.Town, zone, "a place in this zone beats a nearer gate");
    }

    // ---- map math ----

    [Test]
    public void ZoneMap_IndicesHopsAndProjection()
    {
        Assert.AreEqual(ActProgress.Town, ZoneMap.IndexOf("Fen"));
        Assert.AreEqual(ActProgress.Barrens, ZoneMap.IndexOf(StoryIds.ZoneBarrens));
        Assert.AreEqual(0, ZoneMap.IndexOf("Nowhere"));
        Assert.AreEqual(ActProgress.Kiln, ZoneMap.IndexAt(KilnLayout.KilnOffset));
        Assert.AreEqual(ActProgress.Ossuary, ZoneMap.NextHop(ActProgress.Town, ActProgress.Barrens));
        Assert.AreEqual(ActProgress.Kiln, ZoneMap.NextHop(ActProgress.Barrens, ActProgress.Town));
        Assert.AreEqual(ActProgress.Kiln, ZoneMap.NextHop(ActProgress.Kiln, ActProgress.Kiln));

        var z = ZoneMap.Zone(ActProgress.Town);
        var c = ZoneMap.ToMap(z.center, z);
        Assert.AreEqual(0.5f, c.x, 1e-4f);
        Assert.AreEqual(0.5f, c.y, 1e-4f);
        var fwd = Quaternion.Euler(0f, ZoneMap.Yaw, 0f) * Vector3.forward;
        var right = Quaternion.Euler(0f, ZoneMap.Yaw, 0f) * Vector3.right;
        var up = ZoneMap.ToMap(z.center + fwd * 5f, z);
        Assert.AreEqual(0.5f, up.x, 1e-4f, "camera forward is map up");
        Assert.Greater(up.y, 0.5f);
        Assert.Greater(ZoneMap.ToMap(z.center + right * 5f, z).x, 0.5f, "camera right is map right");

        foreach (var zone in WorldBounds.Zones)
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    var p = ZoneMap.ToMap(zone.center + new Vector3(sx, 0f, sz) * zone.halfExtent, zone);
                    Assert.That(p.x, Is.InRange(0f, 1f), zone.name);
                    Assert.That(p.y, Is.InRange(0f, 1f), zone.name);
                }
        Assert.AreEqual(0f, ZoneMap.IconRotation(ZoneMap.Yaw), 1e-4f, "facing camera forward points the arrow up");
        Assert.AreEqual(-90f, ZoneMap.IconRotation(ZoneMap.Yaw + 90f), 1e-4f, "turning right turns the arrow clockwise");
    }

    // ---- shared dialogs ----

    [Test]
    public void DialogShare_LinesRoundTripAndHostChecks()
    {
        var dlg = StoryDialogs.Get("h3.offer"); // 8 lines (S2: +n9-Folgezeile)
        Assert.AreEqual("0,1,2,3,4,6", StoryDialogShare.EncodeLines(new[] { 0, 1, 2, 3, 4, 6 }));
        Assert.AreEqual(new[] { 0, 1, 6 }, StoryDialogShare.DecodeLines("0,1,6", dlg).ToArray());
        Assert.IsNull(StoryDialogShare.DecodeLines("0,8", dlg), "line past the script");
        Assert.IsNull(StoryDialogShare.DecodeLines("1,0", dlg), "out of order");
        Assert.IsNull(StoryDialogShare.DecodeLines("0,x", dlg));
        Assert.IsNull(StoryDialogShare.DecodeLines("", dlg));
        Assert.IsTrue(StoryDialogShare.Valid(SharedDialogOp.Open, "h3.offer", "0,1,6", 2));
        Assert.IsFalse(StoryDialogShare.Valid(SharedDialogOp.Open, "h3.offer", "0,1,6", 3), "index past the lines");
        Assert.IsFalse(StoryDialogShare.Valid(SharedDialogOp.Open, "no.such", "0", 0));
        Assert.IsTrue(StoryDialogShare.Valid(SharedDialogOp.Line, "h3.offer", "", 4));
        Assert.IsTrue(StoryDialogShare.Valid(SharedDialogOp.Close, "", "", 0));
    }

    [Test]
    public void DialogShare_SpeakerPublishesOpenLinesClose()
    {
        var sent = new List<string>();
        StoryDialogShare.Send = (op, d, npc, lines, i) => sent.Add(op + ":" + d + ":" + npc + ":" + lines + ":" + i);
        var go = new GameObject("ShareSpeaker");
        try
        {
            var ui = go.AddComponent<StoryDialogUI>();
            var s = new StoryState();
            s.Refresh(new ActProgress());
            var plan = StoryDialogs.Plan(s, StoryIds.Ottilie);
            ui.Show(plan, StoryIds.Ottilie);
            Assert.IsTrue(ui.IsOpen);
            ui.Advance();
            Assert.AreEqual("Open:h1.offer:ottilie:0,1,2,3:0", sent[0]);
            Assert.AreEqual("Line:h1.offer:ottilie::1", sent[1]);
            ui.SetOpen(false);
            Assert.AreEqual("Close:h1.offer:ottilie::1", sent[2]);
            Assert.AreEqual(3, sent.Count);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void DialogShare_ListenerFollowsSpeakerAndOwnTalkWins()
    {
        var go = new GameObject("ShareListener");
        try
        {
            var ui = go.AddComponent<StoryDialogUI>();
            var dlg = StoryDialogs.Get("h2.offer");
            Assert.IsTrue(ui.ShowShared(7, "Alrik", StoryIds.Ottilie, dlg, new List<int> { 0, 1, 2, 3 }, 0));
            Assert.IsTrue(ui.IsShared);
            Assert.IsFalse(ui.IsOpen, "listening never opens a modal");
            Assert.IsTrue(StoryDialogUI.AnyShared);
            Assert.IsFalse(StoryDialogUI.AnyOpen);
            ui.SharedLine(9, 2);
            Assert.AreEqual(0, ui.LineIndex, "only the speaker moves the lines");
            ui.SharedLine(7, 2);
            Assert.AreEqual(2, ui.LineIndex);
            ui.SharedClose(9);
            Assert.IsTrue(ui.IsShared);
            ui.SharedClose(7);
            Assert.IsFalse(ui.IsShared);
            Assert.IsFalse(StoryDialogUI.AnyShared);

            // Own talk replaces a shared one and blocks new ones.
            ui.ShowShared(7, "Alrik", StoryIds.Ottilie, dlg, new List<int> { 0, 1 }, 0);
            var s = new StoryState();
            s.Refresh(new ActProgress());
            ui.Show(StoryDialogs.Plan(s, StoryIds.Ottilie), StoryIds.Ottilie);
            Assert.IsTrue(ui.IsOpen);
            Assert.IsFalse(ui.IsShared);
            Assert.IsFalse(ui.ShowShared(7, "Alrik", StoryIds.Ottilie, dlg, new List<int> { 0 }, 0));
            ui.SetOpen(false);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Barks_LocalSayRelaysRemoteSayDoesNot()
    {
        var relayed = new List<string>();
        StoryBarks.Relay = id => relayed.Add(id);
        Assert.IsTrue(StoryBarks.Say("abbot.phase2"));
        Assert.AreEqual(new[] { "abbot.phase2" }, relayed.ToArray());
        Assert.IsTrue(StoryBarks.SayRemote("kilnmother.phase2"));
        Assert.AreEqual(1, relayed.Count, "a relayed bark is not sent on");
        Assert.IsFalse(StoryBarks.SayRemote("abbot.phase2"), "the same bark within the repeat window stays one line");
        Assert.AreEqual(2, StoryBarks.relayed);
    }

    [Test]
    public void Catalog_MapAndShareTextsExist()
    {
        foreach (var key in new[]
        {
            "map.title", "map.quests", "map.noquest", "map.legend", "map.hint", "map.legend.you", "map.legend.main",
            "map.legend.side", "map.legend.turnin", "map.legend.offer", "map.legend.ally", "map.legend.npc",
            "map.legend.enemy", "map.legend.gate", "map.legend.waypoint", "story.dialog.hide", "story.dialog.shared",
            "story.tracker.meters", "story.tracker.zone", "story.tracker.handin", "story.log.offers", "story.log.offer",
            "story.log.track", "story.log.tracked", "story.log.where",
        })
            Assert.IsTrue(Loc.Has(key), key);
    }
}
