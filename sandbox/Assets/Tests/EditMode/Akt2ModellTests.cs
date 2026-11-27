using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// cinderreach-akt2-vertiefung S1 (bibel+modell): Die Akt-II-Vertiefung
/// (H18-H19, N12-N15, Raetsel R4-R6 aus docs/story-bible.md) haengt als Daten
/// am bestehenden <see cref="StoryCatalog"/> - Anker H6 (Ankunft), H8
/// (Glasmacher-Kill) und H9 (Rueckkehr) bleiben unangetastet, neue Kennungen
/// meiden h10-h13/n6-n7 (Akt III) und bleiben coverage-faehig ([a-z]+\d+).
/// State/Director brauchen keine neuen Pfade (Refresh/Apply/Booking sind
/// generisch); Loc-Texte und Dialoge folgen in S2.
/// </summary>
public class Akt2ModellTests
{
    static readonly string[] NeuHaupt = { "h18", "h19" }; // quest:h18 quest:h19
    static readonly string[] NeuNeben = { "n12", "n13", "n14", "n15" }; // quest:n12 quest:n13 quest:n14 quest:n15

    static StoryState Neu() => new StoryState();

    static ActProgress AktZweiOffen()
    {
        var act = new ActProgress();
        act.Unlock(ActProgress.Kiln);
        act.CompleteAct();
        return act;
    }

    static ActProgress BossBesiegt()
    {
        var act = AktZweiOffen();
        act.DefeatZoneBoss();
        return act;
    }

    [Test]
    public void AktII_HatZwoelfEintraege()
    {
        int haupt = 0, neben = 0;
        foreach (var q in StoryCatalog.All)
        {
            if (q.act != 2) continue;
            if (q.main) haupt++; else neben++;
        }
        Assert.AreEqual(6, haupt, "H6-H9 + H18-H19");
        Assert.AreEqual(6, neben, "N4-N5 + N12-N15");
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
        // Akt-III-Nummern bleiben frei.
        foreach (var id in new[] { "h10", "h11", "h12", "h13", "n6", "n7" })
            Assert.IsNull(StoryCatalog.Get(id), id);
    }

    [Test]
    public void Voraussetzungen_Existieren()
    {
        foreach (var id in NeuHaupt) AssertVoraussetzung(id);
        foreach (var id in NeuNeben) AssertVoraussetzung(id);
    }

    static void AssertVoraussetzung(string id)
    {
        var q = StoryCatalog.Get(id);
        Assert.IsNotNull(q, id);
        if (q.prereq == null) return;
        Assert.IsNotNull(StoryCatalog.Get(q.prereq), id + " braucht " + q.prereq);
        Assert.IsTrue(q.prereqStatus == StoryStatus.Active || q.prereqStatus == StoryStatus.Done, id);
    }

    [Test]
    public void Anker_BleibenUnangetastet()
    {
        var h6 = StoryCatalog.Get("h6");
        Assert.AreEqual(StoryIds.Merle, h6.giver);
        Assert.AreEqual(StoryGate.ActOneComplete, h6.gate);
        Assert.IsFalse(h6.turnIn);
        Assert.AreEqual(StoryGate.ZoneBossDefeated, h6.catchUp);
        var h8 = StoryCatalog.Get("h8");
        Assert.AreEqual("h7", h8.prereq);
        Assert.IsTrue(h8.autoAccept);
        Assert.AreEqual(1, h8.objectives.Length);
        Assert.AreEqual(StoryObjectiveType.Kill, h8.objectives[0].type);
        Assert.AreEqual(StoryIds.Glasswright, h8.objectives[0].target);
        var h9 = StoryCatalog.Get("h9");
        Assert.AreEqual("h8", h9.prereq);
        Assert.AreEqual(StoryIds.Ottilie, h9.giver);
        Assert.AreEqual(2, h9.objectives.Length);
        Assert.AreEqual(StoryObjectiveType.Talk, h9.objectives[0].type);
        Assert.AreEqual(StoryIds.Merle, h9.objectives[0].target);
        Assert.AreEqual(StoryIds.Ottilie, h9.objectives[1].target);
    }

    [Test]
    public void NeueKette_HaengtAnBestandsstufen()
    {
        Assert.AreEqual("h7", StoryCatalog.Get("h18").prereq); // quest:h18
        Assert.IsTrue(StoryCatalog.Get("h18").autoAccept);
        Assert.AreEqual("h8", StoryCatalog.Get("h19").prereq); // quest:h19
        Assert.IsTrue(StoryCatalog.Get("h19").autoAccept);
        Assert.AreEqual("n4", StoryCatalog.Get("n12").prereq); // quest:n12
        Assert.AreEqual("n5", StoryCatalog.Get("n13").prereq); // quest:n13
        Assert.AreEqual("h18", StoryCatalog.Get("n14").prereq); // quest:n14
        Assert.AreEqual("n5", StoryCatalog.Get("n15").prereq); // quest:n15
        // Tamm-/Ilsabe-Folgen warten auf fertige Vorgaenger, keine Tore.
        Assert.AreEqual(StoryGate.None, StoryCatalog.Get("n12").gate);
        Assert.AreEqual(StoryGate.None, StoryCatalog.Get("n13").gate);
        Assert.AreEqual(StoryGate.None, StoryCatalog.Get("n15").gate);
    }

    [Test]
    public void ErsteAngebote_BleibenBestehen()
    {
        // Katalog-Reihenfolge: erstes Angebot je NPC bleibt h1/h2/h3/h5/n1/n5 -
        // neue Akt-II-Quests stehen dahinter (Dialog-Plaene folgen in S2).
        AssertErstesAngebot(StoryIds.Merle, "h6");
        AssertErstesAngebot(StoryIds.Ilsabe, "n1");
        AssertErstesAngebot(StoryIds.Ottilie, "h1");
        AssertErstesAngebot(StoryIds.Jorin, "n3");
    }

    static void AssertErstesAngebot(string npc, string erwartet)
    {
        string erstes = null;
        foreach (var q in StoryCatalog.All)
        {
            if (q.giver != npc || q.autoAccept) continue;
            erstes = q.id;
            break;
        }
        Assert.AreEqual(erwartet, erstes, npc);
    }

    [Test]
    public void NeueNebenaufgaben_HabenBelohnung()
    {
        foreach (var id in NeuNeben)
        {
            var q = StoryCatalog.Get(id);
            Assert.IsTrue(q.reward != StoryReward.None || q.rewardGold > 0, id);
        }
        Assert.AreEqual(150, StoryCatalog.Get("n12").rewardGold);
        Assert.AreEqual(200, StoryCatalog.Get("n13").rewardGold);
        Assert.AreEqual(100, StoryCatalog.Get("n14").rewardGold);
        Assert.AreEqual(150, StoryCatalog.Get("n15").rewardGold);
    }

    [Test]
    public void Raetsel_H18BrauchtZweiGlaeserUndGespraech()
    {
        var s = Neu();
        var act = AktZweiOffen();
        s.Refresh(act); // h6 per Tor offen, Rest per Kette
        Assert.IsTrue(s.Accept("h6"));
        s.Apply(StoryObjectiveType.Enter, StoryIds.ZoneBarrens);
        s.Refresh(act);
        Assert.AreEqual(StoryStatus.Active, s.Status("h7"), "quest:h7 startet von selbst");
        Assert.AreEqual(StoryStatus.Active, s.Status("h18"), "quest:h18 startet mit h7 von selbst");
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Sehglas));
        Assert.AreEqual(StoryStatus.Active, s.Status("h18"), "1/2 Glaeser reichen nicht (R4)");
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Sehglas));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Talk, StoryIds.Merle));
        Assert.AreEqual(StoryStatus.Done, s.Status("h18"), "h18 ohne Abgabe fertig");
    }

    [Test]
    public void Raetsel_H19BrauchtDreiScherben()
    {
        var s = Neu();
        var act = AktZweiOffen(); // Glasmacher-Kill ohne Boss-Sieg: kein catch-up
        s.Refresh(act);
        s.Accept("h6");
        s.Apply(StoryObjectiveType.Enter, StoryIds.ZoneBarrens);
        s.Refresh(act);
        s.Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur, 3); // h7 fertig (ohne Abgabe)
        s.Refresh(act);
        Assert.AreEqual(StoryStatus.Active, s.Status("h8"), "quest:h8 startet von selbst");
        s.Apply(StoryObjectiveType.Kill, StoryIds.Glasswright); // h8 fertig (ohne Abgabe)
        s.Refresh(act);
        Assert.AreEqual(StoryStatus.Done, s.Status("h8"));
        Assert.AreEqual(StoryStatus.Active, s.Status("h19"), "quest:h19 startet von selbst");
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Glasscherbe, 2));
        Assert.AreEqual(StoryStatus.Active, s.Status("h19"), "2/3 Scherben reichen nicht (R5)");
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Glasscherbe));
        Assert.AreEqual(StoryStatus.Done, s.Status("h19"));
    }

    [Test]
    public void Raetsel_N13BehaltenBrauchtAbgabe()
    {
        var s = Neu();
        s.Refresh(BossBesiegt()); // h4 per catch-up fertig, h5 wartet aufs Gespraech
        Assert.AreEqual(StoryStatus.Locked, s.Status("n13"), "quest:n13 braucht n5 fertig");
        Assert.IsTrue(s.Accept("h5"));
        s.Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe);
        Assert.IsTrue(s.TurnIn("h5"));
        s.Refresh(BossBesiegt());
        Assert.AreEqual(StoryStatus.Available, s.Status("n5"), "quest:n5 bleibt spielbar");
        Assert.AreEqual(StoryStatus.Locked, s.Status("n13"), "quest:n13 wartet weiter auf n5");
        Assert.IsTrue(s.Accept("n5"));
        s.Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur, 4);
        Assert.AreEqual(StoryStatus.Ready, s.Status("n5"));
        Assert.IsTrue(s.TurnIn("n5"));
        s.Refresh(BossBesiegt());
        Assert.AreEqual(StoryStatus.Available, s.Status("n13"), "quest:n13 oeffnet sich mit n5");
        Assert.IsTrue(s.Accept("n13"));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Sehglas));
        Assert.AreEqual(StoryStatus.Ready, s.Status("n13"), "behalten braucht die Abgabe (R6)");
        Assert.IsTrue(s.TurnIn("n13"));
        Assert.AreEqual(StoryStatus.Done, s.Status("n13"));
    }

    [Test]
    public void CatchUp_SchliesstNeueHauptkette_SeitenBleibenOffen()
    {
        var s = Neu();
        s.Refresh(BossBesiegt());
        Assert.AreEqual(StoryStatus.Done, s.Status("h18"), "quest:h18");
        Assert.AreEqual(StoryStatus.Done, s.Status("h19"), "quest:h19");
        Assert.AreEqual(StoryStatus.Locked, s.Status("n12"), "quest:n12 wartet auf n4");
        Assert.AreEqual(StoryStatus.Locked, s.Status("n13"), "quest:n13 wartet auf n5");
        Assert.AreEqual(StoryStatus.Available, s.Status("n14"), "quest:n14 spielt mit h18 aktiv");
        Assert.AreEqual(StoryStatus.Locked, s.Status("n15"), "quest:n15 wartet auf n5");
    }

    [Test]
    public void SaveUndSnapshot_NehmenNeueQuestsMit()
    {
        var s = Neu();
        var act = AktZweiOffen();
        s.Refresh(act);
        s.Accept("h6");
        s.Apply(StoryObjectiveType.Enter, StoryIds.ZoneBarrens);
        s.Refresh(act); // h7 und h18 starten von selbst
        Assert.AreEqual(StoryStatus.Active, s.Status("h18"), "quest:h18");
        s.Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur);
        s.Refresh(act);
        s.Apply(StoryObjectiveType.Collect, StoryIds.Sehglas);
        var back = StoryState.FromSave(s.ToSave(), new List<string>(s.Flags));
        Assert.AreEqual(s.Status("h18"), back.Status("h18"));
        Assert.AreEqual(1, back.Count("h18", 0));
        var other = new StoryState();
        Assert.IsTrue(other.Merge(s.Snapshot()));
        Assert.AreEqual(1, other.Count("h18", 0));
        Assert.AreEqual(s.Status("h18"), other.Status("h18"));
        string leer = new StoryState().Snapshot();
        Assert.IsFalse(other.Merge(leer));
    }
}
