using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// cinderreach-deutsch-story S4: quest data is fully written (every title,
/// summary, objective and dialog line exists), the H1–H9/N1–N5 flow books in
/// order, act catch-up closes skipped chains, and save/snapshot round-trips
/// never regress.
/// </summary>
public class StoryStateTests
{
    [Test]
    public void Catalog_EveryQuestTextAndDialogLineExists()
    {
        var missing = new List<string>();
        foreach (var q in StoryCatalog.All)
        {
            if (!Loc.Has(q.TitleKey)) missing.Add(q.TitleKey);
            if (!Loc.Has(q.SummaryKey)) missing.Add(q.SummaryKey);
            foreach (var o in q.objectives)
                if (!Loc.Has(o.textKey)) missing.Add(o.textKey);
            if (!Loc.Has("npc." + q.giver + ".name"))
                missing.Add("npc." + q.giver + ".name");
        }
        foreach (var d in StoryDialogs.All)
            for (int i = 0; i < d.speakers.Length; i++)
                if (!Loc.Has(d.Key(i))) missing.Add(d.Key(i));
        Assert.IsEmpty(missing, string.Join("\n", missing));
    }

    [Test]
    public void Flow_H1OfferCollectKillTurnInOpensH2()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        Assert.AreEqual(StoryStatus.Available, s.Status("h1"));
        Assert.IsTrue(s.Accept("h1"));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief));
        Assert.AreEqual(StoryStatus.Active, s.Status("h1"));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        Assert.AreEqual(StoryStatus.Ready, s.Status("h1"));
        Assert.IsTrue(s.TurnIn("h1"));
        s.Refresh(new ActProgress());
        Assert.AreEqual(StoryStatus.Available, s.Status("h2"));
        Assert.AreEqual(StoryStatus.Available, s.Status("n1")); // opens with h1 active-or-later
    }

    [Test]
    public void CatchUp_ActOneCompleteClosesActOneChain()
    {
        var s = new StoryState();
        var act = new ActProgress();
        act.Unlock(ActProgress.Kiln);
        act.CompleteAct();
        s.Refresh(act);
        Assert.AreEqual(StoryStatus.Done, s.Status("h1"));
        Assert.AreEqual(StoryStatus.Done, s.Status("h4"));
        Assert.AreEqual(StoryStatus.Available, s.Status("h5"));
    }

    [Test]
    public void TalkPlan_TurnInBeatsOfferAndHint()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        var offer = StoryDialogs.Plan(s, StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Offer, offer.kind);
        Assert.AreEqual("h1", offer.questId);
        s.Accept("h1");
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute);
        var turnIn = StoryDialogs.Plan(s, StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.TurnIn, turnIn.kind);
        Assert.AreEqual("h1.done", turnIn.dialog.id);
    }

    [Test]
    public void SaveAndSnapshot_NeverRegress()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        s.Accept("h1");
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        var back = StoryState.FromSave(s.ToSave(), new List<string>(s.Flags));
        Assert.AreEqual(StoryStatus.Active, back.Status("h1"));
        Assert.AreEqual(1, back.Count("h1", 0));

        var other = new StoryState();
        Assert.IsTrue(other.Merge(s.Snapshot()));
        Assert.AreEqual(StoryStatus.Active, other.Status("h1"));
        string empty = new StoryState().Snapshot();
        Assert.IsFalse(other.Merge(empty), "an empty snapshot must not rewind progress");
        Assert.AreEqual(StoryStatus.Active, other.Status("h1"));
    }

    [Test]
    public void KillTarget_StripsSceneSuffixes()
    {
        Assert.AreEqual("Kilnmother", StoryIds.KillTarget("The Kilnmother(Clone)"));
        Assert.AreEqual("Ash Lancer", StoryIds.KillTarget("Ash Lancer_3"));
        Assert.AreEqual("Cinder Thrall", StoryIds.KillTarget("Kiln Summon_1"));
        Assert.AreEqual("Glasswright", StoryIds.KillTarget("The Glasswright"));
    }

    [Test]
    public void QuestLog_LinesShowCountsForTrackedQuest()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        Assert.IsTrue(s.Accept("h1"));
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute);
        Assert.IsTrue(s.TurnIn("h1"));
        s.Refresh(new ActProgress());
        Assert.IsTrue(s.Accept("h2"));
        s.Apply(StoryObjectiveType.Kill, StoryIds.BoneWarden);
        var lines = StoryQuestUI.Lines(s);
        Assert.IsTrue(System.Array.Exists(lines, l => l.Contains("1/4")), string.Join("\n", lines));
    }
}
