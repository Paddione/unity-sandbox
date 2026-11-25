using System.Collections.Generic;
using UnityEngine;

/// <summary>Direction of one stat when a candidate replaces the equipped item.</summary>
public enum StatTrend { Same, Better, Worse }

/// <summary>One affix key compared between a candidate and the equipped item in its slot.</summary>
public readonly struct StatDelta
{
    public readonly string key;
    public readonly float mine;
    public readonly float theirs;

    public StatDelta(string key, float mine, float theirs)
    {
        this.key = key;
        this.mine = mine;
        this.theirs = theirs;
    }

    public float Delta => mine - theirs;

    /// <summary>Every affix in the library grows stronger with its value, so a positive delta is an upgrade.</summary>
    public StatTrend Trend =>
        Mathf.Abs(Delta) < ItemCompare.Epsilon ? StatTrend.Same : Delta > 0f ? StatTrend.Better : StatTrend.Worse;
}

/// <summary>
/// Shift-compare model shared by inventory, stash, vendor and smith tooltips:
/// the union of affix keys of a candidate and the equipped item, each with a
/// trend. An empty slot counts as zero, so every rolled stat is an upgrade.
/// </summary>
public static class ItemCompare
{
    public const float Epsilon = 0.001f;
    public const string UpArrow = "▲";
    public const string DownArrow = "▼";

    public static List<StatDelta> Diff(ItemInstance item, ItemInstance equipped)
    {
        var result = new List<StatDelta>();
        if (item == null) return result;
        var keys = new List<string>();
        void Collect(ItemInstance it)
        {
            if (it?.affixes == null) return;
            foreach (var a in it.affixes)
                if (!keys.Contains(a.key)) keys.Add(a.key);
        }
        Collect(item);
        Collect(equipped);
        foreach (var key in keys)
            result.Add(new StatDelta(key, item.GetAffix(key), equipped != null ? equipped.GetAffix(key) : 0f));
        return result;
    }

    /// <summary>Net count of better minus worse stats (positive reads as an overall upgrade).</summary>
    public static int Score(ItemInstance item, ItemInstance equipped)
    {
        int score = 0;
        foreach (var d in Diff(item, equipped))
            score += d.Trend == StatTrend.Better ? 1 : d.Trend == StatTrend.Worse ? -1 : 0;
        return score;
    }

    /// <summary>
    /// Rich-text compare lines: green ▲ with delta for gains, red ▼ with delta
    /// for losses, plain text without arrow for equal stats.
    /// </summary>
    public static string ArrowLines(ItemInstance item, ItemInstance equipped)
    {
        if (item == null) return "";
        var diff = Diff(item, equipped);
        if (diff.Count == 0) return Loc.T("inv.nobonus");
        var lines = new List<string>(diff.Count);
        foreach (var d in diff)
            lines.Add(ArrowLine(d));
        return string.Join("\n", lines.ToArray());
    }

    public static string ArrowLine(StatDelta d)
    {
        string text = AffixLibrary.Describe(new Affix(d.key, d.mine));
        switch (d.Trend)
        {
            case StatTrend.Better:
                return Loc.F("inv.cmp.better", DiabloInventoryUI.BetterColor, UpArrow, text, d.Delta);
            case StatTrend.Worse:
                return Loc.F("inv.cmp.worse", DiabloInventoryUI.WorseColor, DownArrow, text, d.Delta);
            default:
                return $"<color=#8a7f6c>=</color> {text}";
        }
    }
}
