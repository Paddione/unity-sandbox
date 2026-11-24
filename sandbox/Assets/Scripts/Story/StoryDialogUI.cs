using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bottom dialog window (cinderreach-deutsch-story S4): one catalog line at a
/// time, speaker name above it, click/space advances, closing on the last line
/// books the talk via <see cref="StoryDirector.BookTalk"/>. No booking when
/// the window is closed early, so scripts simply replay.
/// </summary>
public sealed class StoryDialogUI : MonoBehaviour
{
    public static bool AnyOpen { get; private set; }
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Text speakerText, bodyText, buttonText;
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
        IsOpen = lines.Count > 0;
        AnyOpen = IsOpen;
        canvas.enabled = IsOpen;
        if (IsOpen) { DiabloUIFactory.CloseSecondaryPanels(gameObject, this); ShowLine(); }
    }

    void Update()
    {
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
        if (!IsOpen) return;
        DiabloAudio.PlayUIClick();
        if (index + 1 < lines.Count) { index++; ShowLine(); }
        else Close(true);
    }

    void Close(bool book)
    {
        IsOpen = false;
        AnyOpen = false;
        StoryVoice.Stop();
        if (canvas != null) canvas.enabled = false;
        if (book) StoryDirector.BookTalk(npc, gameObject);
    }

    /// <summary>Closes without booking (modal coordination).</summary>
    public void SetOpen(bool open)
    {
        if (!open) Close(false);
    }

    void ShowLine()
    {
        int i = lines[index];
        string speaker = StoryDialog.Speaker(dialog.speakers[i]);
        speakerText.text = speaker == "n" ? "" : Loc.T("npc." + speaker + ".name");
        bodyText.text = Loc.T(dialog.Key(i));
        StoryVoice.Play(dialog.Key(i));
        bodyText.fontStyle = speaker == "n" ? FontStyle.Italic : FontStyle.Normal;
        buttonText.text = Loc.T(index + 1 < lines.Count ? "story.dialog.continue" : "story.dialog.end");
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
    }
}
