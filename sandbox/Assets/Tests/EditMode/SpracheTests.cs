using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// cinderreach-sprache-voice S2: sichert Glättungsregeln (docs/story-bible.md
/// §Ton-Fortschreibung) als Tests — Glossar-Begriffe und Ton bleiben, nur
/// verstaubte Wendungen sind ersetzt; Anrede (ihr) und Zahlen (ausgeschrieben,
/// deutsches Komma/Prozent) gelten überall; Struktur (Keys/Speaker) ist
/// unberührt. Läuft ohne Assets (nur Loc/Story-Kataloge).
/// </summary>
public class SpracheTests
{
    [SetUp]
    public void SetUp() => Loc.Language = Loc.German;

    [TearDown]
    public void TearDown() => Loc.Language = Loc.German;

    static string T(string key) => Loc.T(key);

    static IEnumerable<string> AllSpokenKeys()
    {
        foreach (var d in StoryDialogs.All)
            for (int i = 0; i < d.speakers.Length; i++) yield return d.Key(i);
        foreach (var b in StoryBarks.All) yield return b.Key;
    }

    [Test]
    public void Ottilie_OhneIronie_EsWerdenMehr()
    {
        // Regel 4: Ironie nur bei Ilsabe/Schmuggler — h14.offer.3 (Ottilie) geglättet.
        Assert.AreEqual("Derselbe Moorschläger? Nein. Ein anderer. Es werden mehr.", T("dlg.h14.offer.3"));
    }

    [Test]
    public void Jorin_KurzOhneKontraktion()
    {
        // Regel 1: "Hab's mir gedacht" → klare Form, Jorin bleibt kurz.
        Assert.AreEqual("Ottilie schickt euch. Das dachte ich mir.", T("dlg.h3.talk.jorin.1"));
    }

    [Test]
    public void Rechtschreibung_SeheMitE_DaraufNiemand()
    {
        Assert.AreEqual("Sie hat euch etwas gesagt, bevor sie fiel. Ich sehe es euch an. Nein, sagt es mir nicht. Ich lese es selbst.", T("dlg.h4.done.2"));
        StringAssert.Contains("darauf", T("dlg.n4.offer.1"));
        StringAssert.Contains("niemand", T("dlg.n13.offer.1"));
        StringAssert.Contains("nicht lesbar", T("dlg.h15.talk.ottilie.2"));
    }

    [Test]
    public void Merle_OhneObendraufUndMitDrin()
    {
        StringAssert.DoesNotContain("obendrauf", T("dlg.h9.talk.merle.7"));
        StringAssert.DoesNotContain("obendrauf", T("dlg.n12.done.2"));
        StringAssert.DoesNotContain("mit drin", T("dlg.h10.talk.merle.2"));
        StringAssert.DoesNotContain("mit drin", T("dlg.h9.talk.merle.4"));
    }

    [Test]
    public void N8_OttilieFuerchtetKonkret()
    {
        Assert.AreEqual("Hundert Gold. Ich fürchte, ich weiß schon, wessen Siegel darauf ist.", T("dlg.n8.offer.2"));
    }

    [Test]
    public void H10_SatzstellungEntzerrt()
    {
        StringAssert.Contains("Morgen geht die Fuhre zum Moortor. Vorher will ich jede Stimme gehört haben", T("dlg.h10.offer.1"));
    }

    [Test]
    public void H11_ZusammenfassungGlatt()
    {
        Assert.AreEqual("Drei Glutspuren am Moortor. Mit Ilsabes letzter Spendenseite gelesen führen sie zu Thewes' Laterne.", T("quest.h11.summary"));
    }

    [Test]
    public void Glossar_BegriffeBleiben()
    {
        Assert.AreEqual("Der Hohle Abt", T("npc.abbot.name"));
        Assert.AreEqual("Glutspur", T("story.item.glutspur"));
        StringAssert.Contains("Glutofen", T("dlg.h3.hint.1"));
        StringAssert.Contains("Glutglas", T("dlg.h5.done.2"));
        foreach (var key in AllSpokenKeys())
        {
            string line = T(key);
            StringAssert.DoesNotContain("Zombie", line, key);
            StringAssert.DoesNotContain("Brennofen", line, key);
        }
    }

    [Test]
    public void Anrede_GruppeWirdGeduzt()
    {
        // Regel 5: Figuren duzen die Gruppe ("ihr/euch"); kein "Ihnen" in Zeilen.
        StringAssert.Contains("euch", T("dlg.h3.talk.jorin.1"));
        StringAssert.Contains("euch", T("dlg.h4.done.2"));
        StringAssert.Contains("Ihr", T("dlg.h5.offer.5"));
        foreach (var key in AllSpokenKeys())
            StringAssert.DoesNotContain("Ihnen", T(key), key);
    }

    [Test]
    public void Zahlen_AusgeschriebenDeutsch()
    {
        // Regel 6: Zahlen ausgeschrieben, keine Ziffern in Zeilen; deutsches Komma/Prozent via Loc-Culture.
        var digits = new Regex("[0-9]");
        foreach (var key in AllSpokenKeys())
            Assert.IsFalse(digits.IsMatch(T(key)), key + " enthält Ziffern: " + T(key));
        Assert.AreEqual("de-DE", Loc.Culture.Name);
        StringAssert.Contains("25 %", (0.25).ToString("0 %", Loc.Culture));
        StringAssert.Contains("1,5", (1.5).ToString("0.0", Loc.Culture));
    }

    [Test]
    public void Struktur_KeysUndSpeakerUnveraendert()
    {
        // Glättung ändert nur Zeilentexte: alle vertonten Keys lösen auf,
        // Skriptlängen und Sprecherreihenfolgen stehen wie im Bestand.
        foreach (var key in AllSpokenKeys())
            Assert.IsTrue(Loc.Has(key), key);
        Assert.AreEqual(4, StoryDialogs.Get("h14.offer").speakers.Length);
        Assert.AreEqual(7, StoryDialogs.Get("h9.talk.merle").speakers.Length);
        Assert.AreEqual(8, StoryDialogs.Get("h9.talk.ottilie").speakers.Length);
        Assert.AreEqual(6, StoryDialogs.Get("h10.done").speakers.Length); // N + O×3 + Folgen n6/n7
    }
}
