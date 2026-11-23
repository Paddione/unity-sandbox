using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Player-visible text lookup (cinderreach-deutsch-story S1). Every string a
/// player reads comes from <see cref="LocCatalog"/> by a stable dotted key;
/// German is the shipped language, English the fallback. Runs without any
/// asset or UI dependency, so the dedicated server and EditMode tests use it
/// as is. <c>-lang en</c> switches a player to the English fallback.
/// Keys never change meaning once shipped: rename by adding a new key.
/// </summary>
public static class Loc
{
    public const string German = "de";
    public const string English = "en";

    static string language;
    static CultureInfo culture;
    static readonly HashSet<string> warned = new HashSet<string>();

    /// <summary>Active language: <see cref="German"/> unless <c>-lang en</c> is passed.</summary>
    public static string Language
    {
        get
        {
            if (language == null) language = FromArgs(Environment.GetCommandLineArgs());
            return language;
        }
        set
        {
            language = value == English ? English : German;
            culture = null;
        }
    }

    /// <summary>Number culture of <see cref="Language"/> (German decimal comma).</summary>
    public static CultureInfo Culture
    {
        get
        {
            if (culture != null) return culture;
            try { culture = CultureInfo.GetCultureInfo(Language == English ? "en-US" : "de-DE"); }
            catch (CultureNotFoundException) { culture = CultureInfo.InvariantCulture; }
            return culture;
        }
    }

    /// <summary>Language from command-line args: <c>-lang en</c> → English, anything else German.</summary>
    public static string FromArgs(string[] args)
    {
        if (args != null)
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-lang") return args[i + 1] == English ? English : German;
        return German;
    }

    /// <summary>Text for <paramref name="key"/> in the active language, English if the
    /// language lacks it, the key itself (with one warning) if the catalog lacks it.</summary>
    public static string T(string key)
    {
        if (LocCatalog.TryGet(key, Language, out var text)) return text;
        if (Language != English && LocCatalog.TryGet(key, English, out text)) return text;
        if (warned.Add(key)) Debug.LogWarning($"[Loc] missing key '{key}'");
        return key;
    }

    /// <summary><see cref="T"/> with <see cref="string.Format(IFormatProvider,string,object[])"/>
    /// in the language's culture ("{0:0.0} s" → "1,5 s").</summary>
    public static string F(string key, params object[] args)
    {
        string pattern = T(key);
        try { return string.Format(Culture, pattern, args); }
        catch (FormatException)
        {
            if (warned.Add(key + "#fmt")) Debug.LogWarning($"[Loc] bad format for '{key}'");
            return pattern;
        }
    }

    static Dictionary<string, string> nameKeys;

    /// <summary>Display form of an internal English proper name (scene object names,
    /// serialized shrine names, difficulty ids): "Ash Elite" → "Aschenhüne". Looks the
    /// name up among the English texts of the <c>name.*</c> keys; unknown names pass through.</summary>
    public static string Display(string internalName)
    {
        if (string.IsNullOrEmpty(internalName)) return internalName;
        if (nameKeys == null)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in LocCatalog.All())
                if (e.key.StartsWith("name.", StringComparison.Ordinal) && !map.ContainsKey(e.en)) map[e.en] = e.key;
            nameKeys = map;
        }
        string n = internalName.Replace("(Clone)", "").Trim();
        int us = n.IndexOf('_');
        if (us > 0) n = n.Substring(0, us).Trim();             // "Glass Pillar_2" → "Glass Pillar"
        if (nameKeys.TryGetValue(n, out var key)) return T(key);
        if (nameKeys.TryGetValue("The " + n, out key)) return T(key); // "Hollow Abbot" → "The Hollow Abbot"
        return internalName;
    }

    /// <summary>True when the catalog holds <paramref name="key"/>.</summary>
    public static bool Has(string key) => LocCatalog.TryGet(key, English, out _);

    /// <summary>Number in the language's culture, e.g. <c>Num(1.5f, "0.0")</c> → "1,5".</summary>
    public static string Num(float value, string format) => value.ToString(format, Culture);
}
