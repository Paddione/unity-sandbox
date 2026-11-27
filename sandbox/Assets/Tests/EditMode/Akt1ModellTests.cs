using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// cinderreach-akt1-vertiefung S1 (bibel+modell): Die Akt-I-Vertiefung
/// (H14–H17, N8–N11, Rätsel R1–R3 aus docs/story-bible.md) hängt als Daten
/// am bestehenden <see cref="StoryCatalog"/> — Anker H2-Tor, H4-Abschluss
/// und H5 bleiben unangetastet, neue Kennungen meiden h10-h13/n6-n7
/// (Akt III) und bleiben coverage-fähig ([a-z]+\d+).
/// </summary>
public class Akt1ModellTests
{
    static readonly string[] NeuHaupt = { "h14", "h15", "h16", "h17" }; // quest:h14 quest:h15 quest:h16 quest:h17
    static readonly string[] NeuNeben = { "n8", "n9", "n10", "n11" }; // quest:n8 quest:n9 quest:n10 quest:n11

    static StoryState Neu() => new StoryState();

    static ActProgress KilnOffen()
    {
        var act = new ActProgress();
        act.Unlock(ActProgress.Kiln);
        return act;
    }

    static ActProgress AktEinFertig()
    {
        var act = KilnOffen();
        act.CompleteAct();
        return act;
    }

    [Test]
    public void AktI_HatSechzehnEintraege()
    {
        int haupt = 0, neben = 0;
        foreach (var q in StoryCatalog.All)
        {
            if (q.act != 1) continue;
            if (q.main) haupt++; else neben++;
        }
        Assert.AreEqual(9, haupt, "H1-H5 + H14-H17");
        Assert.AreEqual(7, neben, "N1-N3 + N8-N11");
    }

    [Test]
    public void Kennungen_EindeutigUndCoverageFaehig()
    {
        var ids = new HashSet<string>();
        var form = new Regex("^[a-z]+\\d+$");
        foreach (var q in StoryCatalog.All)
        {
            Assert.IsTrue(ids.Add(q.id), "doppelte Kennung: " + q.id);
            Assert.IsTrue(form.IsMatch(q.id), "coverage-form: " + q.id);
            foreach (var o in q.objectives)
                Assert.Greater(o.count, 0, q.id + " zielmenge");
        }
        foreach (var id in NeuHaupt) Assert.IsNotNull(StoryCatalog.Get(id), id);
        foreach (var id in NeuNeben) Assert.IsNotNull(StoryCatalog.Get(id), id);
        // Akt-III-Kette haengt (S3): H9 -> H10 -> H11 -> H12 -> H13, N6/N7 ab H10.
        Assert.AreEqual("h9", StoryCatalog.Get("h10").prereq);
        Assert.AreEqual("h10", StoryCatalog.Get("n6").prereq);
        Assert.AreEqual("h10", StoryCatalog.Get("n7").prereq);
        Assert.AreEqual("h10", StoryCatalog.Get("h11").prereq);
        Assert.AreEqual("h11", StoryCatalog.Get("h12").prereq);
        Assert.AreEqual("h12", StoryCatalog.Get("h13").prereq);
    }

    [Test]
    public void Voraussetzungen_Existieren()
    {
        foreach (var q in StoryCatalog.All)
        {
            if (q.prereq == null) continue;
            Assert.IsNotNull(StoryCatalog.Get(q.prereq), q.id + " braucht " + q.prereq);
            Assert.IsTrue(q.prereqStatus == StoryStatus.Active || q.prereqStatus == StoryStatus.Done, q.id);
        }
    }

    [Test]
    public void Anker_BleibenUnangetastet()
    {
        var h2 = StoryCatalog.Get("h2");
        Assert.AreEqual("h1", h2.prereq);
        Assert.AreEqual(StoryGate.None, h2.gate);
        Assert.AreEqual(StoryGate.KilnOpen, h2.catchUp);
        var h4 = StoryCatalog.Get("h4");
        Assert.AreEqual("h3", h4.prereq);
        Assert.IsTrue(h4.autoAccept);
        Assert.AreEqual(StoryGate.ActOneComplete, h4.catchUp);
        var h5 = StoryCatalog.Get("h5");
        Assert.AreEqual("h4", h5.prereq);
        Assert.AreEqual(StoryGate.None, h5.gate);
        Assert.AreEqual(1, h5.objectives.Length);
        Assert.AreEqual(StoryObjectiveType.Talk, h5.objectives[0].type);
    }

    [Test]
    public void NeueKette_HaengtAnBestandsstufen()
    {
        Assert.AreEqual("h1", StoryCatalog.Get("h14").prereq); // quest:h14
        Assert.AreEqual("h2", StoryCatalog.Get("h15").prereq); // quest:h15
        Assert.AreEqual("h3", StoryCatalog.Get("h16").prereq); // quest:h16
        Assert.AreEqual(StoryGate.KilnOpen, StoryCatalog.Get("h16").gate);
        Assert.AreEqual("h4", StoryCatalog.Get("h17").prereq); // quest:h17
        Assert.AreEqual("h1", StoryCatalog.Get("n8").prereq); // quest:n8
        Assert.AreEqual("h2", StoryCatalog.Get("n9").prereq); // quest:n9
        Assert.AreEqual(StoryGate.KilnOpen, StoryCatalog.Get("n10").gate); // quest:n10
        Assert.IsNull(StoryCatalog.Get("n10").prereq);
        Assert.AreEqual("h3", StoryCatalog.Get("n11").prereq); // quest:n11
    }

    [Test]
    public void NeueNebenaufgaben_HabenBelohnung()
    {
        foreach (var id in NeuNeben)
        {
            var q = StoryCatalog.Get(id);
            Assert.IsTrue(q.reward != StoryReward.None || q.rewardGold > 0, id);
        }
        Assert.AreEqual(100, StoryCatalog.Get("n8").rewardGold);
        Assert.AreEqual(60, StoryCatalog.Get("n9").rewardGold);
        Assert.AreEqual(80, StoryCatalog.Get("n10").rewardGold);
        Assert.AreEqual(120, StoryCatalog.Get("n11").rewardGold);
    }

    [Test]
    public void Kette_H14OeffnetMitH1_H17BrauchtH4Fertig()
    {
        var s = Neu();
        s.Refresh(new ActProgress());
        Assert.IsTrue(s.Accept("h1"));
        s.Refresh(new ActProgress());
        Assert.AreEqual(StoryStatus.Available, s.Status("h14"), "quest:h14");
        Assert.AreEqual(StoryStatus.Available, s.Status("n8"), "quest:n8");
        Assert.AreEqual(StoryStatus.Locked, s.Status("h17"), "quest:h17 braucht h4 fertig");
    }

    [Test]
    public void Raetsel_H14BrauchtBeideFetzenUndBrute()
    {
        var s = Neu();
        s.Refresh(new ActProgress());
        s.Accept("h1");
        s.Refresh(new ActProgress());
        Assert.IsTrue(s.Accept("h14")); // quest:h14
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrfetzen));
        Assert.AreEqual(StoryStatus.Active, s.Status("h14"), "1/2 Fetzen reicht nicht (R1)");
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrfetzen));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        Assert.AreEqual(StoryStatus.Ready, s.Status("h14"));
        Assert.IsTrue(s.TurnIn("h14"));
        Assert.AreEqual(StoryStatus.Done, s.Status("h14"));
    }

    [Test]
    public void Raetsel_H15BrauchtDreiBruecheUndGespraech()
    {
        var s = Neu();
        s.Refresh(new ActProgress());
        s.Accept("h1");
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief);
        s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute);
        s.TurnIn("h1");
        s.Refresh(new ActProgress());
        Assert.IsTrue(s.Accept("h2"));
        s.Refresh(new ActProgress());
        Assert.IsTrue(s.Accept("h15")); // quest:h15
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Tafelbruch, 2));
        Assert.AreEqual(StoryStatus.Active, s.Status("h15"), "2/3 Bruchstücke reichen nicht (R2)");
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Tafelbruch));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Talk, StoryIds.Ottilie));
        Assert.AreEqual(StoryStatus.Done, s.Status("h15"), "h15 ohne Abgabe fertig");
    }

    [Test]
    public void Raetsel_H17ZweiGespraecheOhneAbgabe()
    {
        var s = Neu();
        var act = AktEinFertig();
        s.Refresh(act); // h4 per catch-up fertig
        Assert.AreEqual(StoryStatus.Done, s.Status("h4"));
        Assert.AreEqual(StoryStatus.Available, s.Status("h17")); // quest:h17
        Assert.IsTrue(s.Accept("h17"));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe));
        Assert.AreEqual(StoryStatus.Active, s.Status("h17"), "ein Gespräch reicht nicht (R3)");
        Assert.IsTrue(s.Apply(StoryObjectiveType.Talk, StoryIds.Ottilie));
        Assert.AreEqual(StoryStatus.Done, s.Status("h17"));
    }

    [Test]
    public void CatchUp_SchliesstNeueHauptkette_SeitenBleibenOffen()
    {
        var s = Neu();
        s.Refresh(AktEinFertig());
        Assert.AreEqual(StoryStatus.Done, s.Status("h14"), "quest:h14");
        Assert.AreEqual(StoryStatus.Done, s.Status("h15"), "quest:h15");
        Assert.AreEqual(StoryStatus.Done, s.Status("h16"), "quest:h16");
        Assert.AreEqual(StoryStatus.Available, s.Status("h17"), "quest:h17 wartet auf das Gespräch");
        Assert.AreEqual(StoryStatus.Available, s.Status("n8"), "quest:n8 bleibt spielbar wie n1-n5");
        Assert.AreEqual(StoryStatus.Available, s.Status("n9"), "quest:n9 bleibt spielbar wie n1-n5");
        Assert.AreEqual(StoryStatus.Available, s.Status("n10"), "quest:n10 Tor offen, kein catch-up");
        Assert.AreEqual(StoryStatus.Available, s.Status("n11"), "quest:n11 bleibt spielbar wie n1-n5");
    }

    [Test]
    public void SaveUndSnapshot_NehmenNeueQuestsMit()
    {
        var s = Neu();
        s.Refresh(new ActProgress());
        s.Accept("h1");
        s.Refresh(new ActProgress());
        s.Accept("h14"); // quest:h14
        s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrfetzen);
        var back = StoryState.FromSave(s.ToSave(), new List<string>(s.Flags));
        Assert.AreEqual(StoryStatus.Active, back.Status("h14"));
        Assert.AreEqual(1, back.Count("h14", 0));
        var other = new StoryState();
        Assert.IsTrue(other.Merge(s.Snapshot()));
        Assert.AreEqual(1, other.Count("h14", 0));
        string leer = new StoryState().Snapshot();
        Assert.IsFalse(other.Merge(leer));
    }
}
