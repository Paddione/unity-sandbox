using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// cinderreach-akt3-finale S1 (bibel+akt): Akt-III-Tabellen (H10–H13/N6–N7,
/// Boss-/Entscheidungs-Definition aus docs/story-bible.md §Akt III) plus
/// ActProgress-Entwurf (Wacht/Moortor auf Abbot→Kiln-Muster) als Modelltests.
/// StoryModel/StoryState bleiben in S1 unangetastet: H10–H13/N6–N7 sind
/// reserviert und werden erst in S2/S3 an H9 gehaengt. Die Tests pruefen
/// darum Reservierung, H9-Anker (inkl. H9-Varianten merle.7/ottilie.7-8 seit
/// Akt-II-S2), Katalog-Reihenfolge und Catch-up — plus Wacht-/Moortor-Gates,
/// Finalboss-Reihenfolge, H13-Entscheidung und Save-v16-Stillstand.
/// </summary>
public class Akt3ModellTests
{
    static readonly string[] Akt3Haupt = { "h10", "h11", "h12", "h13" }; // quest:h10 quest:h11 quest:h12 quest:h13
    static readonly string[] Akt3Neben = { "n6", "n7" }; // quest:n6 quest:n7

    static StoryState Neu() => new StoryState();

    static ActProgress AktZweiFertig()
    {
        var act = new ActProgress();
        act.Unlock(ActProgress.Kiln);
        act.CompleteAct();
        act.DefeatZoneBoss();
        return act;
    }

    static ActProgressAkt3 MoortorOffen()
    {
        var finale = new ActProgressAkt3();
        Assert.IsTrue(finale.OpenMoortor(AktZweiFertig()));
        return finale;
    }

    [Test]
    public void Kennungen_HaengenAnH9()
    {
        // S3 loest die Reservierung ein: H10–H13/N6–N7 haengen an H9.
        Assert.AreEqual("h9", StoryCatalog.Get("h10").prereq);
        Assert.AreEqual("h10", StoryCatalog.Get("n6").prereq);
        Assert.AreEqual("h10", StoryCatalog.Get("n7").prereq);
        Assert.AreEqual("h10", StoryCatalog.Get("h11").prereq);
        Assert.AreEqual("h11", StoryCatalog.Get("h12").prereq);
        Assert.AreEqual("h12", StoryCatalog.Get("h13").prereq);
    }

    [Test]
    public void H9_Anker_FuerH10()
    {
        // H10 haengt an H9 (prereq): Anker bleibt unangetastet.
        var h9 = StoryCatalog.Get("h9");
        Assert.IsNotNull(h9);
        Assert.AreEqual("h8", h9.prereq, "quest:h9 braucht quest:h8");
        Assert.AreEqual(StoryIds.Ottilie, h9.giver);
        Assert.AreEqual(2, h9.objectives.Length);
        Assert.AreEqual(StoryObjectiveType.Talk, h9.objectives[0].type);
        Assert.AreEqual(StoryIds.Merle, h9.objectives[0].target);
        Assert.AreEqual(StoryIds.Ottilie, h9.objectives[1].target);
        Assert.IsTrue(h9.autoAccept);
        Assert.IsFalse(h9.turnIn);
    }

    [Test]
    public void H9_Varianten_SeitAktIIS2()
    {
        // H9-Varianten (merle.7/ottilie.7-8): H10 schliesst an diese Zeilen an.
        var merle = StoryDialogs.Get("h9.talk.merle");
        Assert.IsNotNull(merle);
        Assert.AreEqual(7, merle.speakers.Length);
        Assert.AreEqual(StoryIds.Merle + "|n12", merle.speakers[6]);
        Assert.AreNotEqual(merle.Key(6), Loc.T(merle.Key(6)), merle.Key(6));
        var ottilie = StoryDialogs.Get("h9.talk.ottilie");
        Assert.IsNotNull(ottilie);
        Assert.AreEqual(8, ottilie.speakers.Length);
        Assert.AreEqual(StoryIds.Ottilie + "|n13", ottilie.speakers[6]);
        Assert.AreEqual(StoryIds.Ottilie + "|n15", ottilie.speakers[7]);
        Assert.AreNotEqual(ottilie.Key(6), Loc.T(ottilie.Key(6)), ottilie.Key(6));
        Assert.AreNotEqual(ottilie.Key(7), Loc.T(ottilie.Key(7)), ottilie.Key(7));
    }

    [Test]
    public void Katalog_Reihenfolge_ErstesAngebot()
    {
        // Save-Vertrag (anhaengen, nie umstellen): erstes Angebot je NPC bleibt.
        AssertErstesAngebot(StoryIds.Ottilie, "h1");
        AssertErstesAngebot(StoryIds.Ilsabe, "n1");
        AssertErstesAngebot(StoryIds.Jorin, "n3");
        AssertErstesAngebot(StoryIds.Merle, "h6");
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
    public void Kennungen_EindeutigUndCoverageFaehig()
    {
        // S2/S3-Kennungen (h10-h13/n6-n7) muessen in dieses Raster passen.
        var ids = new HashSet<string>();
        var form = new Regex("^[a-z]+\\d+$");
        foreach (var q in StoryCatalog.All)
        {
            Assert.IsTrue(ids.Add(q.id), "doppelte Kennung: " + q.id);
            Assert.IsTrue(form.IsMatch(q.id), "coverage-form: " + q.id);
            foreach (var o in q.objectives)
                Assert.Greater(o.count, 0, q.id + " zielmenge");
        }
        foreach (var id in Akt3Haupt) Assert.IsTrue(form.IsMatch(id), id);
        foreach (var id in Akt3Neben) Assert.IsTrue(form.IsMatch(id), id);
    }

    [Test]
    public void CatchUp_BarrensBoss_SchliesstAktIIHaupt()
    {
        // Bestand bleibt: ZoneBossDefeated schliesst die Akt-II-Hauptkette
        // (Vorbild fuer den H12-Catch-up in S3), Seiten warten weiter.
        var s = Neu();
        s.Refresh(AktZweiFertig());
        Assert.AreEqual(StoryStatus.Done, s.Status("h18"), "quest:h18");
        Assert.AreEqual(StoryStatus.Done, s.Status("h19"), "quest:h19");
        Assert.AreEqual(StoryStatus.Locked, s.Status("n12"), "quest:n12 wartet auf n4");
        Assert.AreEqual(StoryStatus.Locked, s.Status("n13"), "quest:n13 wartet auf n5");
    }

    [Test]
    public void Wacht_BrauchtKeinNeuesTor()
    {
        // H10 spielt in der Wacht (Town/Ossuary): von Anfang an offen.
        var act = new ActProgress();
        Assert.IsTrue(act.IsUnlocked(ActProgress.Town));
        Assert.IsTrue(act.IsUnlocked(ActProgress.Ossuary));
    }

    [Test]
    public void Moortor_BrauchtBarrensBoss()
    {
        // Abbot→Kiln-Muster: erst der Barrens-Boss oeffnet das Moortor.
        var frisch = new ActProgressAkt3();
        Assert.IsFalse(frisch.OpenMoortor(new ActProgress()), "ohne Zonen kein Moortor");
        Assert.IsFalse(frisch.OpenMoortor(null), "ohne Akt kein Moortor");
        var kiln = new ActProgress();
        kiln.Unlock(ActProgress.Kiln);
        kiln.CompleteAct();
        Assert.IsFalse(frisch.OpenMoortor(kiln), "ohne Barrens-Boss kein Moortor");
        Assert.IsFalse(frisch.MoortorOpen);
        Assert.IsTrue(frisch.OpenMoortor(AktZweiFertig()));
        Assert.IsTrue(frisch.MoortorOpen);
        Assert.IsFalse(frisch.OpenMoortor(AktZweiFertig()), "zweites Oeffnen ist no-op");
    }

    [Test]
    public void FinalBoss_BrauchtMoortor()
    {
        var finale = new ActProgressAkt3();
        Assert.IsFalse(finale.DefeatFinalBoss(), "ohne Moortor kein Finalboss");
        Assert.IsFalse(finale.FinalBossDefeated);
        finale.OpenMoortor(AktZweiFertig());
        Assert.IsTrue(finale.DefeatFinalBoss());
        Assert.IsTrue(finale.FinalBossDefeated);
        Assert.IsFalse(finale.DefeatFinalBoss(), "zweiter Kill ist no-op");
    }

    [Test]
    public void Entscheidung_BrauchtFinalboss()
    {
        var finale = MoortorOffen();
        Assert.AreEqual(ActProgressAkt3.NoDecision, finale.Decision);
        Assert.IsFalse(finale.Decide(true), "ohne Finalboss keine Entscheidung");
        Assert.AreEqual(ActProgressAkt3.NoDecision, finale.Decision);
        Assert.IsTrue(finale.DefeatFinalBoss());
        Assert.IsTrue(finale.Decide(true));
        Assert.AreEqual(ActProgressAkt3.Shatter, finale.Decision, "zerschlagen");
        Assert.IsFalse(finale.Decide(false), "genau eine Schlusszeile je Durchlauf");
        Assert.AreEqual(ActProgressAkt3.Shatter, finale.Decision);
    }

    [Test]
    public void Entscheidung_ZerschlagenUndHalten_JeEineSchlusszeile()
    {
        // Gleicher Durchlauf, zwei Ausgaenge: je eine Schlusszeile (Bibel H13).
        var zerschlagen = MoortorOffen();
        zerschlagen.DefeatFinalBoss();
        Assert.IsTrue(zerschlagen.Decide(true));
        var halten = MoortorOffen();
        halten.DefeatFinalBoss();
        Assert.IsTrue(halten.Decide(false));
        Assert.AreEqual(ActProgressAkt3.Shatter, zerschlagen.Decision);
        Assert.AreEqual(ActProgressAkt3.Hold, halten.Decision);
        Assert.AreNotEqual(zerschlagen.Decision, halten.Decision);
    }

    [Test]
    public void Save_V16_Bleibt()
    {
        // S1: kein Save-Eingriff, keine Migration — S3 verdrahtet Save/Koop.
        Assert.AreEqual(17, SaveGame.CurrentVersion, "Akt III braucht kein eigenes Feld; v17 kam mit bagSlots");
    }
}
