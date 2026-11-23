using System.Collections.Generic;

/// <summary>
/// All player-visible text (cinderreach-deutsch-story). Each domain lives in its
/// own partial file <c>LocCatalog.&lt;Domain&gt;.cs</c> as one array of
/// <see cref="Entry"/>, German and English side by side, so a line can never
/// drift between languages. A new domain file adds its array to <see cref="Domains"/>.
/// Names follow the glossary in <c>docs/story-bible.md</c>.
/// </summary>
public static partial class LocCatalog
{
    public readonly struct Entry
    {
        public readonly string key, de, en;
        public Entry(string key, string de, string en) { this.key = key; this.de = de; this.en = en; }
    }

    static Entry E(string key, string de, string en) => new Entry(key, de, en);

    static Dictionary<string, Entry> index;

    /// <summary>Every domain array; tests walk this for completeness.</summary>
    public static IEnumerable<Entry[]> Domains()
    {
        yield return Names;
        yield return Char;
        yield return Hud;
        yield return Key;
        yield return Stash;
        yield return Town;
        yield return Vendor;
        yield return Smith;
        yield return Inv;
        yield return Reward;
        yield return Unique;
        yield return Stat;
        yield return Affix;
        yield return Item;
        yield return Account;
        yield return Join;
        yield return Settings;
        yield return Menu;
        yield return Class;
    }

    /// <summary>All entries in declaration order (duplicates included, for tests).</summary>
    public static IEnumerable<Entry> All()
    {
        foreach (var d in Domains())
            foreach (var e in d) yield return e;
    }

    public static bool TryGet(string key, string lang, out string text)
    {
        if (index == null)
        {
            var map = new Dictionary<string, Entry>();
            foreach (var e in All()) map[e.key] = e;
            index = map;
        }
        if (key != null && index.TryGetValue(key, out var entry))
        {
            text = lang == Loc.English ? entry.en : entry.de;
            return !string.IsNullOrEmpty(text);
        }
        text = null;
        return false;
    }
}
