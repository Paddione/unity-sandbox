using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-deutsch-story S1: the catalog is complete and consistent, every
/// <c>Loc.T/F("key")</c> in runtime code exists, and no player-visible English
/// literal lives outside <c>Scripts/Localization</c>. Literals that are not
/// player text (object names, ids, log lines the heuristic misses) sit in
/// <c>loc-allowlist.txt</c>; untranslated player text sits in
/// <c>loc-pending.txt</c> until S2 empties it. Both lists fail on stale lines.
/// </summary>
public class LocalizationTests
{
    static string ScriptsDir => Path.Combine(Application.dataPath, "Scripts");
    static string TestsDir => Path.Combine(Application.dataPath, "Tests", "EditMode");

    /// <summary>Upper bound for <c>loc-pending.txt</c>; only ever lowered, 0 closes S2.</summary>
    public const int PendingCeiling = 0;

    [TearDown]
    public void ResetLanguage() => Loc.Language = Loc.German;

    [Test]
    public void Catalog_KeysUniqueAndBothLanguagesFilled()
    {
        var seen = new HashSet<string>();
        var bad = new List<string>();
        foreach (var e in LocCatalog.All())
        {
            if (!seen.Add(e.key)) bad.Add("duplicate " + e.key);
            if (string.IsNullOrWhiteSpace(e.de)) bad.Add("no de " + e.key);
            if (string.IsNullOrWhiteSpace(e.en)) bad.Add("no en " + e.key);
            if (!Regex.IsMatch(e.key, @"^[a-z0-9]+(\.[a-z0-9_-]+)+$")) bad.Add("key shape " + e.key);
        }
        Assert.IsEmpty(bad, string.Join("\n", bad));
    }

    [Test]
    public void Catalog_PlaceholdersMatchBetweenLanguages()
    {
        var hole = new Regex(@"\{(\d+)(?:[:,][^}]*)?\}");
        var bad = new List<string>();
        foreach (var e in LocCatalog.All())
        {
            var de = new SortedSet<string>(hole.Matches(e.de).Cast<Match>().Select(m => m.Groups[1].Value));
            var en = new SortedSet<string>(hole.Matches(e.en).Cast<Match>().Select(m => m.Groups[1].Value));
            if (!de.SetEquals(en)) bad.Add($"{e.key}: de {{{string.Join(",", de)}}} en {{{string.Join(",", en)}}}");
        }
        Assert.IsEmpty(bad, string.Join("\n", bad));
    }

    [Test]
    public void Loc_GermanByDefault_EnglishOnFlag_KeyOnMiss()
    {
        Assert.AreEqual(Loc.German, Loc.FromArgs(new[] { "game" }));
        Assert.AreEqual(Loc.English, Loc.FromArgs(new[] { "game", "-lang", "en" }));
        Assert.AreEqual(Loc.German, Loc.FromArgs(new[] { "game", "-lang", "fr" }));
        Loc.Language = Loc.German;
        Assert.AreEqual("Laternenwacht", Loc.T("name.zone.town"));
        Loc.Language = Loc.English;
        Assert.AreEqual("Lantern Hold", Loc.T("name.zone.town"));
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
        Assert.AreEqual("no.such.key", Loc.T("no.such.key"));
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void Loc_FormatsNumbersInLanguageCulture()
    {
        Loc.Language = Loc.German;
        Assert.AreEqual("1,5", Loc.Num(1.5f, "0.0"));
        Loc.Language = Loc.English;
        Assert.AreEqual("1.5", Loc.Num(1.5f, "0.0"));
    }

    [Test]
    public void GermanTexts_CarryNoOldEnglishProperNames()
    {
        // Glossary names that changed in German (docs/story-bible.md) must not leak into German text.
        var old = LocCatalog.All()
            .Where(e => e.key.StartsWith("name.") && e.de != e.en && e.en.Length >= 4 && e.en.Contains(" "))
            .Select(e => e.en.StartsWith("The ") ? e.en.Substring(4) : e.en)
            .Distinct().ToList();
        var bad = new List<string>();
        foreach (var e in LocCatalog.All())
            foreach (var name in old)
                if (e.de.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) bad.Add($"{e.key}: {name}");
        Assert.IsEmpty(bad, "German texts with old English names:\n" + string.Join("\n", bad));
    }

    [Test]
    public void AffixesAndStats_HaveCatalogText()
    {
        var missing = new List<string>();
        foreach (var d in AffixLibrary.All)
        {
            if (!Loc.Has("affix." + d.key.ToLowerInvariant())) missing.Add("affix." + d.key);
            if (!Loc.Has("stat." + d.key.ToLowerInvariant())) missing.Add("stat." + d.key);
        }
        foreach (var c in ClassCatalog.All)
            if (!Loc.Has("class.blurb." + c.id)) missing.Add("class.blurb." + c.id);
        Assert.IsEmpty(missing, string.Join("\n", missing));
    }

    [Test]
    public void Display_MapsSceneNamesToGerman()
    {
        Loc.Language = Loc.German;
        Assert.AreEqual("Aschenhüne", Loc.Display("Ash Elite"));
        Assert.AreEqual("Glassäule", Loc.Display("Glass Pillar_2"));
        Assert.AreEqual("Der Hohle Abt", Loc.Display("Hollow Abbot"));
        Assert.AreEqual("Laternenwacht", Loc.Display("Lantern Hold"));
        Assert.AreEqual("Entfacht", Loc.Display(RunProfile.Kindled));
        Assert.AreEqual("Laterne der Wacht", LootDrop.PrettyName("Lantern of the Hold"));
        Assert.AreEqual("Kurzschwert", LootDrop.PrettyName("ShortSword"));
        Assert.AreEqual("Gnadentuch", LootDrop.PrettyName(ItemDatabase.MercyShroudId));
    }

    [Test]
    public void EveryKeyUsedInCode_ExistsInCatalog()
    {
        var call = new Regex(@"Loc\.(?:T|F)\(\s*""([^""]+)""\s*[,)]");
        var missing = new List<string>();
        foreach (var file in RuntimeSources())
            foreach (Match m in call.Matches(File.ReadAllText(file)))
                if (!Loc.Has(m.Groups[1].Value)) missing.Add($"{Rel(file)}: {m.Groups[1].Value}");
        Assert.IsEmpty(missing, "Keys missing from LocCatalog:\n" + string.Join("\n", missing));
    }

    [Test]
    public void NoUnlistedPlayerVisibleLiterals()
    {
        var found = new HashSet<string>(LocLint.Scan(ScriptsDir));
        var allow = ReadList("loc-allowlist.txt");
        var pending = ReadList("loc-pending.txt");
        var unlisted = found.Where(f => !allow.Contains(f) && !pending.Contains(f)).OrderBy(f => f).ToList();
        var stale = allow.Concat(pending).Where(f => !found.Contains(f)).OrderBy(f => f).ToList();
        Assert.IsEmpty(unlisted, "Player-visible literals outside Scripts/Localization (move to LocCatalog or allowlist):\n" + string.Join("\n", unlisted));
        Assert.IsEmpty(stale, "Stale allowlist/pending lines:\n" + string.Join("\n", stale));
        Assert.LessOrEqual(pending.Count, PendingCeiling, "loc-pending.txt may only shrink.");
    }

    static HashSet<string> ReadList(string name)
    {
        string path = Path.Combine(TestsDir, name);
        var set = new HashSet<string>();
        if (!File.Exists(path)) return set;
        foreach (var line in File.ReadAllLines(path))
            if (line.Length > 0 && !line.StartsWith("#")) set.Add(line);
        return set;
    }

    static IEnumerable<string> RuntimeSources() =>
        Directory.GetFiles(ScriptsDir, "*.cs", SearchOption.AllDirectories);

    static string Rel(string file) => file.Substring(ScriptsDir.Length + 1).Replace('\\', '/');
}

/// <summary>
/// Heuristic scan for player-visible English in runtime code. A finding is
/// <c>relative/path.cs&lt;TAB&gt;literal</c>. Flags multi-word text, ALL-CAPS words
/// single capitalised words on display sinks (<c>return</c>, <c>.text =</c>,
/// ternaries, button labels) and sentence fragments (a capitalised word plus
/// <c>. : ! ? —</c>, so "Sold." and $"Crit {x}:" count). Skips comments, log/assert lines and lines that
/// only name objects or assets. Scripts/Localization is the one place text lives.
/// </summary>
public static class LocLint
{
    static readonly Regex Literal = new Regex(@"(?<pre>\$@|@\$|\$|@)?""(?<body>(?:[^""\\\n]|\\.)*)""");
    static readonly Regex Hole = new Regex(@"\{[^{}]*\}");
    static readonly Regex Word = new Regex(@"[A-Za-zÄÖÜäöüß]{2,}");
    static readonly Regex Fragment = new Regex(@"[.:!?—]");
    static readonly Regex Sink = new Regex(@"(return\s*$|\.text\s*=\s*$|[?:]\s*$|MakeSmallButton\(\s*$|label\s*[:=]\s*$|title\s*[:=]\s*$)");
    static readonly string[] SkipMarkers =
    {
        "Debug.Log", "Debug.Assert", "Debug.LogWarning", "Debug.LogError", "[Header(", "[Tooltip(",
        "[MenuItem(", "new GameObject(", "Resources.Load", ".Find(", "CompareTag", "Shader.Find",
        "Animator.StringToHash", "PlayerPrefs.", "Console.Write", "[assembly", "FindGameObjectsWithTag",
        "SetTrigger(", "SetBool(", "SetFloat(", "SetInteger(", "Play(\"", "LogWarning(", "LogError(",
    };

    public static IEnumerable<string> Scan(string scriptsDir)
    {
        foreach (var file in Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories))
        {
            string rel = file.Substring(scriptsDir.Length + 1).Replace('\\', '/');
            if (rel.StartsWith("Localization/")) continue;
            foreach (var hit in ScanText(File.ReadAllText(file)))
                yield return rel + "\t" + hit;
        }
    }

    public static IEnumerable<string> ScanText(string source)
    {
        bool inBlock = false;
        foreach (var raw in source.Split('\n'))
        {
            string line = raw;
            if (inBlock)
            {
                int end = line.IndexOf("*/", System.StringComparison.Ordinal);
                if (end < 0) continue;
                line = line.Substring(end + 2);
                inBlock = false;
            }
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//") || trimmed.StartsWith("#")) continue;
            if (trimmed.StartsWith("/*")) { if (!trimmed.Contains("*/")) inBlock = true; continue; }
            if (SkipMarkers.Any(m => line.Contains(m))) continue;
            foreach (Match m in Literal.Matches(line))
            {
                string body = m.Groups["body"].Value;
                if (IsPlayerText(body, line.Substring(0, m.Index))) yield return body;
            }
        }
    }

    public static bool IsPlayerText(string body, string before)
    {
        string text = Hole.Replace(body, " ").Replace("\\n", " ");
        var words = Word.Matches(text).Cast<Match>().Select(w => w.Value).ToList();
        if (words.Count == 0) return false;
        if (text.Contains("/") && !text.Contains(" ")) return false;           // asset paths
        if (text.Contains("_") && !text.Contains(" ")) return false;           // object ids
        if (words.Count >= 2 && text.Trim().Contains(" ")) return true;        // phrases
        string w0 = words[0];
        if (w0.Length >= 3 && w0.ToUpperInvariant() == w0 && text.Trim() == w0) return true; // "QUIT"
        if (char.IsUpper(w0[0]) && text.Trim() == w0 && Sink.IsMatch(before.TrimEnd()))
            return true;                                                         // return "Bow";
        if (Regex.IsMatch(text.Trim(), @"^[\w.-]+\.\w{2,4}$")) return false;  // file names ("LegacyRuntime.ttf")
        if (!text.Contains("_") && !text.Contains("/") && Fragment.IsMatch(text)
            && words.Any(w => w.Length >= 3 && char.IsUpper(w[0])))
            return true;                                                         // "Sold." / $"Crit {x}:"
        return false;
    }
}
