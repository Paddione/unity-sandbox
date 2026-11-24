using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// cinderreach-story-voice: every dialog and bark line has a German voice clip,
/// side-task consequences show the right alternative line, barks map boss
/// kills and do not repeat, and English stays unvoiced.
/// </summary>
public class StoryVoiceTests
{
    [SetUp]
    public void SetUp()
    {
        DiabloAudio.MutePlayback = true;
        StoryBarks.ResetForTests();
        Loc.Language = Loc.German;
    }

    [TearDown]
    public void TearDown() => Loc.Language = Loc.German;

    static IEnumerable<string> AllSpokenKeys()
    {
        foreach (var d in StoryDialogs.All)
            for (int i = 0; i < d.speakers.Length; i++) yield return d.Key(i);
        foreach (var b in StoryBarks.All) yield return b.Key;
    }

    [Test]
    public void EveryDialogAndBarkLine_HasTextAndVoiceClip()
    {
        var missing = new List<string>();
        foreach (var key in AllSpokenKeys())
        {
            if (!Loc.Has(key)) missing.Add(key + " (text)");
            if (StoryVoice.Clip(key) == null) missing.Add(key + " (clip)");
        }
        foreach (var b in StoryBarks.All)
            if (b.speaker != "n" && !Loc.Has("npc." + b.speaker + ".name")) missing.Add("npc." + b.speaker + ".name");
        Assert.IsEmpty(missing, string.Join("\n", missing));
    }

    [Test]
    public void Play_GermanCountsClip_EnglishStaysSilent()
    {
        int before = StoryVoice.played;
        Assert.Greater(StoryVoice.Play("dlg.h1.offer.1"), 0.5f);
        Assert.AreEqual(before + 1, StoryVoice.played);
        Loc.Language = Loc.English;
        Assert.AreEqual(0f, StoryVoice.Play("dlg.h1.offer.1"));
        Assert.AreEqual(before + 1, StoryVoice.played);
    }

    [Test]
    public void Visible_FlagAndNegatedFlag()
    {
        var flags = new HashSet<string> { "n4" };
        Assert.IsTrue(StoryDialog.Visible("merle", flags));
        Assert.IsTrue(StoryDialog.Visible("merle|n4", flags));
        Assert.IsFalse(StoryDialog.Visible("merle|!n4", flags));
        Assert.IsFalse(StoryDialog.Visible("merle|n5", flags));
        Assert.IsTrue(StoryDialog.Visible("merle|!n5", flags));
    }

    [Test]
    public void MerleH9_ShowsExactlyOneConsequenceLine()
    {
        var d = StoryDialogs.Get("h9.talk.merle");
        foreach (var withTamm in new[] { false, true })
        {
            var flags = new HashSet<string>();
            if (withTamm) flags.Add("n4");
            int shown = 0;
            string chosen = null;
            for (int i = 0; i < d.speakers.Length; i++)
                if (StoryDialog.Flag(d.speakers[i]) != null && StoryDialog.Visible(d.speakers[i], flags)) { shown++; chosen = d.Key(i); }
            Assert.AreEqual(1, shown);
            Assert.AreEqual(withTamm ? "dlg.h9.talk.merle.4" : "dlg.h9.talk.merle.5", chosen);
        }
    }

    [Test]
    public void Barks_BossKillsMapAndRepeatIsSuppressed()
    {
        StoryBarks.OnKill(StoryIds.KillTarget("The Kilnmother(Clone)"));
        Assert.IsTrue(StoryBarks.TryTake(out var bark));
        Assert.AreEqual("kilnmother.death", bark.id);
        Assert.IsFalse(StoryBarks.Say("kilnmother.death"));
        StoryBarks.OnKill("Husk");
        Assert.IsFalse(StoryBarks.TryTake(out _));
        Assert.IsFalse(StoryBarks.Say("no.such.bark"));
        Assert.AreEqual(1, StoryBarks.said);
    }
}
