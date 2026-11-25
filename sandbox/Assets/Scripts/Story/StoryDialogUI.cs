using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bottom dialog window (cinderreach-deutsch-story S4): one catalog line at a
/// time, speaker name above it, click/space advances, closing on the last line
/// books the talk via <see cref="StoryDirector.BookTalk"/>. No booking when
/// the window is closed early, so scripts simply replay.
/// Co-op (cinderreach-group-quest-map): the speaker's window publishes every
/// line (<see cref="StoryDialogShare"/>); the other players see it as a
/// listener window with the same voice, which never blocks movement or books
/// anything and can be hidden. A player's own talk replaces a shared one.
/// </summary>
public sealed class StoryDialogUI : MonoBehaviour
{
    public static bool AnyOpen { get; private set; }
    /// <summary>True while some listener window shows another hero's talk.</summary>
    public static bool AnyShared { get; private set; }
    public bool IsOpen { get; private set; }
    /// <summary>Listener window: showing another hero's talk.</summary>
    public bool IsShared { get; private set; }
    /// <summary>Line on display (index into the visible lines).</summary>
    public int LineIndex => index;

    Canvas canvas;
    Text speakerText, bodyText, buttonText, sharedText;
    ulong sharedFrom;
    StoryDialog dialog;
    StoryTalkPlan plan;
    string npc;
    readonly List<int> lines = new List<int>();
    int index;

    public void Show(StoryTalkPlan talkPlan, string npcId)
    {
        EnsureBuilt();
        plan = talkPlan;
        dialog = talkPlan.dialog;
        npc = npcId;
        lines.Clear();
        if (dialog != null)
            for (int i = 0; i < dialog.speakers.Length; i++)
            {
                if (!StoryDialog.Visible(dialog.speakers[i], StoryDirector.State.Flags)) continue;
                lines.Add(i);
            }
        index = 0;
        EndShared();
        IsOpen = lines.Count > 0;
        AnyOpen = IsOpen;
        canvas.enabled = IsOpen;
        if (IsOpen)
        {
            DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
            ShowLine();
            StoryDialogShare.Publish(SharedDialogOp.Open, dialog.id, npc, lines, index);
        }
    }

    /// <summary>
    /// Listener window for another hero's talk. Ignored while this player's own
    /// talk is open; a newer shared talk replaces an older one.
    /// </summary>
    public bool ShowShared(ulong from, string speakerName, string npcId, StoryDialog sharedDialog, List<int> visible, int line)
    {
        if (IsOpen || sharedDialog == null || visible == null || visible.Count == 0) return false;
        EnsureBuilt();
        dialog = sharedDialog;
        npc = npcId;
        lines.Clear();
        lines.AddRange(visible);
        index = Mathf.Clamp(line, 0, lines.Count - 1);
        sharedFrom = from;
        IsShared = true;
        AnyShared = true;
        sharedText.text = Loc.F("story.dialog.shared", speakerName ?? "", Loc.T("npc." + npcId + ".name"));
        sharedText.enabled = true;
        canvas.enabled = true;
        ShowLine();
        return true;
    }

    /// <summary>The speaker moved on: show that line (and speak it).</summary>
    public void SharedLine(ulong from, int line)
    {
        if (!IsShared || from != sharedFrom || line < 0 || line >= lines.Count || line == index) return;
        index = line;
        ShowLine();
    }

    /// <summary>The speaker closed the talk.</summary>
    public void SharedClose(ulong from)
    {
        if (IsShared && from == sharedFrom) HideShared();
    }

    /// <summary>Listener hides the shared window locally (the talk goes on for the others).</summary>
    public void HideShared()
    {
        if (!IsShared) return;
        EndShared();
        StoryVoice.Stop();
        if (canvas != null && !IsOpen) canvas.enabled = false;
    }

    void EndShared()
    {
        IsShared = false;
        AnyShared = false;
        if (sharedText != null) sharedText.enabled = false;
    }

    void Update()
    {
        if (IsShared)
        {
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame)
            {
                HideShared();
                DiabloUIFactory.MarkEscConsumed();
            }
            return;
        }
        if (!IsOpen) return;
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            Advance();
        else if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Close(false);
            DiabloUIFactory.MarkEscConsumed();
        }
    }

    public void Advance()
    {
        if (IsShared) { HideShared(); return; }
        if (!IsOpen) return;
        DiabloAudio.PlayUIClick();
        if (index + 1 < lines.Count)
        {
            index++;
            ShowLine();
            StoryDialogShare.Publish(SharedDialogOp.Line, dialog.id, npc, null, index);
        }
        else Close(true);
    }

    void Close(bool book)
    {
        if (!IsOpen) return;
        IsOpen = false;
        AnyOpen = false;
        StoryVoice.Stop();
        if (canvas != null) canvas.enabled = false;
        StoryDialogShare.Publish(SharedDialogOp.Close, dialog != null ? dialog.id : "", npc, null, index);
        if (book) StoryDirector.BookTalk(npc, gameObject);
    }

    /// <summary>Closes without booking (modal coordination).</summary>
    public void SetOpen(bool open)
    {
        if (!open) Close(false);
    }

    void OnDisable()
    {
        if (IsOpen) Close(false);
        if (IsShared) HideShared();
    }

    void ShowLine()
    {
        int i = lines[index];
        string speaker = StoryDialog.Speaker(dialog.speakers[i]);
        speakerText.text = speaker == "n" ? "" : Loc.T("npc." + speaker + ".name");
        bodyText.text = Loc.T(dialog.Key(i));
        StoryVoice.Play(dialog.Key(i));
        bodyText.fontStyle = speaker == "n" ? FontStyle.Italic : FontStyle.Normal;
        buttonText.text = IsShared ? Loc.T("story.dialog.hide")
            : Loc.T(index + 1 < lines.Count ? "story.dialog.continue" : "story.dialog.end");
    }

    void EnsureBuilt()
    {
        if (canvas != null) return;
        var canvasGo = new GameObject("StoryDialogCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 26);
        canvas.enabled = false;

        var panel = DiabloUIFactory.MakePanel(canvasGo.transform, "DialogPanel", new Vector2(860f, 190f), new Vector2(0f, -330f));
        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(22, 22, 14, 14);
        v.spacing = 8f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        speakerText = DiabloUIFactory.MakeText(panel.transform, "Speaker", "", 18, DiabloPalette.TrimGold);
        DiabloUIFactory.SetLayout(speakerText.gameObject, prefH: 26f, flexW: 1f);
        bodyText = DiabloUIFactory.MakeText(panel.transform, "Body", "", 19, DiabloPalette.Parchment);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        DiabloUIFactory.SetLayout(bodyText.gameObject, prefH: 84f, flexW: 1f);
        var btn = DiabloUIFactory.MakeSmallButton(panel.transform, "", () => Advance(), 36f, 180f);
        buttonText = btn.GetComponentInChildren<Text>();
        // Listener banner above the panel: "<hero> spricht mit <npc>".
        sharedText = DiabloUIFactory.MakeText(canvasGo.transform, "DialogShared", "", 15, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        var sr = sharedText.rectTransform;
        sr.anchoredPosition = new Vector2(0f, -222f);
        sr.sizeDelta = new Vector2(860f, 24f);
        sharedText.enabled = false;
    }
}
