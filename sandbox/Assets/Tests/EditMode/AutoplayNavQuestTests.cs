using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>autoplay-quests: grid A* around obstacles, quest planning and the quest result contract.</summary>
public class AutoplayNavQuestTests
{
    static System.Func<Vector2Int, bool> Grid(params string[] rows) => c =>
        c.y >= 0 && c.y < rows.Length && c.x >= 0 && c.x < rows[c.y].Length && rows[c.y][c.x] != '#';

    [Test]
    public void AStar_GoesAroundAWall()
    {
        var walk = Grid(
            "..........",
            "....#.....",
            "....#.....",
            "....#.....",
            "..........");
        var start = new Vector2Int(1, 2);
        var goal = new Vector2Int(8, 2);
        var path = GridAStar.Find(start, goal, c => c == goal, walk);
        Assert.IsNotNull(path);
        Assert.AreEqual(start, path[0]);
        Assert.AreEqual(goal, path[path.Count - 1]);
        foreach (var c in path) Assert.IsTrue(walk(c), $"path crosses wall at {c}");
    }

    [Test]
    public void AStar_DoesNotCutWallCorners()
    {
        var walk = Grid(
            ".#",
            "#.");
        Assert.IsNull(GridAStar.Find(new Vector2Int(0, 0), new Vector2Int(1, 1), c => c == new Vector2Int(1, 1), walk));
    }

    [Test]
    public void AStar_ReturnsNullWhenSealed()
    {
        var walk = Grid(
            "..#..",
            "..#..",
            "..#..");
        Assert.IsNull(GridAStar.Find(new Vector2Int(0, 1), new Vector2Int(4, 1), c => c == new Vector2Int(4, 1), walk));
    }

    [Test]
    public void AStar_AcceptsGoalRadiusAroundBlockedTarget()
    {
        var walk = Grid(
            ".....",
            "..#..",
            ".....");
        var target = new Vector2Int(2, 1);
        var path = GridAStar.Find(new Vector2Int(0, 1), target, c => (c - target).sqrMagnitude <= 1, walk);
        Assert.IsNotNull(path);
        Assert.AreEqual(new Vector2Int(1, 1), path[path.Count - 1]);
    }

    [Test]
    public void Smooth_KeepsOnlyCornerCells()
    {
        var walk = Grid(
            ".....",
            ".###.",
            ".....");
        var start = new Vector2Int(0, 0);
        var goal = new Vector2Int(4, 2);
        var path = GridAStar.Find(start, goal, c => c == goal, walk);
        var smooth = GridAStar.Smooth(path, (a, b) => GridAStar.LineClear(a, b, walk));
        Assert.Less(smooth.Count, path.Count);
        for (int i = 1; i < smooth.Count; i++)
            Assert.IsTrue(GridAStar.LineClear(smooth[i - 1], smooth[i], walk), $"leg {i} blocked");
    }

    [Test]
    public void Quests_FreshRunWantsOttilieAndNoFinds()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        CollectionAssert.Contains(AutoplayQuests.PendingTalks(s), StoryIds.Ottilie);
        Assert.IsEmpty(AutoplayQuests.WantedFinds(s), "nothing is wanted before an offer is taken");
    }

    [Test]
    public void Quests_AcceptedH1WantsLetterAndBrute_ButNeverBosses()
    {
        var act = new ActProgress();
        var s = new StoryState();
        s.Refresh(act);
        Assert.IsTrue(s.Accept("h1"));
        CollectionAssert.Contains(AutoplayQuests.WantedFinds(s), StoryIds.Fuhrbrief);
        CollectionAssert.Contains(AutoplayQuests.WantedKills(s), StoryIds.FenBrute);
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute);
        s.Refresh(act);
        Assert.IsTrue(s.TurnIn("h1"));
        s.Refresh(act);
        Assert.IsTrue(s.Accept("h2"));
        Assert.IsFalse(AutoplayQuests.WantedKills(s).Contains(StoryIds.Abbot), "boss kills belong to the route");
        Assert.IsTrue(AutoplayQuests.WantedKills(s, includeBosses: true).Contains(StoryIds.Abbot));
        CollectionAssert.Contains(AutoplayQuests.WantedKills(s), StoryIds.BoneWarden);
    }

    [Test]
    public void Ledger_TellsPlayedFromCaughtUp()
    {
        var act = new ActProgress();
        var s = new StoryState();
        s.Refresh(act);
        var ledger = new AutoplayQuestLedger(s);
        Assert.IsTrue(s.Accept("h1"));
        ledger.Observe(s);
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute);
        s.TurnIn("h1");
        // Opening the Kiln closes the never-started h2 and h14 by catch-up.
        Assert.IsTrue(act.Unlock(ActProgress.Kiln));
        s.Refresh(act);
        ledger.Observe(s);
        var caught = ledger.CaughtUp(s);
        CollectionAssert.DoesNotContain(caught, "h1");
        CollectionAssert.Contains(caught, "h2");
        CollectionAssert.Contains(ledger.Done(s), "h1");
        Assert.AreEqual(ledger.Total, ledger.Done(s).Count + ledger.Open(s).Count);
    }

    [Test]
    public void Result_QuestModeNeedsQuestsPhase()
    {
        var r = new AutoplayResult();
        foreach (var p in AutoplayResult.RequiredPhases) r.Reach(p);
        r.actBossKills.AddRange(new[] { "1:Kilnmother:-", "2:Glasswright:-", "3:Thewes:-" });
        Assert.AreEqual(0, r.ComputeExit(), "without quest mode the route phases and the boss order suffice");
        r.questMode = true;
        Assert.AreEqual(1, r.ComputeExit());
        r.Reach(AutoplayResult.PhaseQuests);
        Assert.AreEqual(0, r.ComputeExit());
        var back = AutoplayResult.FromJson(r.ToJson());
        Assert.IsTrue(back.questMode);
    }
}
