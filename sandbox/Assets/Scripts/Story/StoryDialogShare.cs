using System.Collections.Generic;
using UnityEngine;

public enum SharedDialogOp { Open = 0, Line = 1, Close = 2 }

/// <summary>
/// Group dialogs (cinderreach-group-quest-map). When one hero talks to a story
/// NPC in co-op, every other player sees the same lines in their dialog window
/// and hears the same voice clip, line by line, while the speaker clicks on.
/// The speaker's process sends the dialog id, the visible line indices (its
/// story flags decide them) and the current line; the host checks and relays
/// (<see cref="DiabloNetworkPlayer"/> <c>ShareDialog</c>). Listeners never book
/// the talk and may hide the window; their own talk always wins over a shared one.
/// </summary>
public static class StoryDialogShare
{
    /// <summary>Most lines a shared dialog may carry (scripts have at most seven).</summary>
    public const int MaxLines = 16;

    /// <summary>Test counters: messages applied by this process and the last one.</summary>
    public static int Received;
    public static SharedDialogOp LastOp;
    public static string LastDialog;
    public static int LastIndex;
    public static string LastSpeaker;

    /// <summary>Optional speaker seam: set by the network layer, null solo.</summary>
    public static System.Action<SharedDialogOp, string, string, string, int> Send;

    public static string EncodeLines(IList<int> lines)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(lines[i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Line indices of <paramref name="csv"/> that exist in <paramref name="dialog"/>,
    /// in order and without repeats; null when the message is unusable.
    /// </summary>
    public static List<int> DecodeLines(string csv, StoryDialog dialog)
    {
        if (dialog == null || string.IsNullOrEmpty(csv)) return null;
        var parts = csv.Split(',');
        if (parts.Length > MaxLines) return null;
        var list = new List<int>();
        int last = -1;
        foreach (var p in parts)
        {
            if (!int.TryParse(p, out int i) || i <= last || i >= dialog.speakers.Length) return null;
            list.Add(i);
            last = i;
        }
        return list.Count > 0 ? list : null;
    }

    /// <summary>Host-side check of a relayed message; false drops it.</summary>
    public static bool Valid(SharedDialogOp op, string dialogId, string lines, int index)
    {
        if (op == SharedDialogOp.Close) return true;
        var dialog = StoryDialogs.Get(dialogId);
        if (dialog == null) return false;
        if (op == SharedDialogOp.Line) return index >= 0 && index < MaxLines;
        var list = DecodeLines(lines, dialog);
        return list != null && index >= 0 && index < list.Count;
    }

    /// <summary>The speaker's dialog window changed: tell the group (no-op solo).</summary>
    public static void Publish(SharedDialogOp op, string dialogId, string npc, IList<int> lines, int index)
    {
        Send?.Invoke(op, dialogId ?? "", npc ?? "", lines != null ? EncodeLines(lines) : "", index);
    }

    /// <summary>A group member's dialog changed: show it in the local hero's window.</summary>
    public static void Receive(ulong from, string speakerName, SharedDialogOp op, string dialogId, string npc, string lines, int index)
    {
        Received++;
        LastOp = op;
        LastDialog = dialogId;
        LastIndex = index;
        LastSpeaker = speakerName;
        Debug.Log($"[Story] shared dialog {op} {dialogId} line {index} from client {from}.");
        var hero = HeroLocator.Solo();
        if (hero == null) return;
        var ui = hero.GetComponent<StoryDialogUI>();
        if (ui == null) ui = hero.gameObject.AddComponent<StoryDialogUI>();
        switch (op)
        {
            case SharedDialogOp.Open:
                var dialog = StoryDialogs.Get(dialogId);
                var list = DecodeLines(lines, dialog);
                if (list != null) ui.ShowShared(from, speakerName, npc, dialog, list, index);
                break;
            case SharedDialogOp.Line:
                ui.SharedLine(from, index);
                break;
            case SharedDialogOp.Close:
                ui.SharedClose(from);
                break;
        }
    }

    public static void ResetForTests()
    {
        Received = 0;
        LastOp = SharedDialogOp.Open;
        LastDialog = null;
        LastIndex = 0;
        LastSpeaker = null;
    }
}
