using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Expedition log ("Fuhrbuch", key J; cinderreach-deutsch-story S4): main and
/// side tasks per act with objective counts, ready and done marks. Read-only;
/// talking and fighting move the state, never this window.
/// </summary>
public sealed class StoryQuestUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    Canvas canvas;
    Transform list;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || DiabloUIFactory.IsRemoteMirror(this)) return;
        if (kb.jKey.wasPressedThisFrame && (!DiabloUIFactory.IsAnyModalOpen(gameObject) || IsOpen))
            SetOpen(!IsOpen);
        else if (kb.escapeKey.wasPressedThisFrame && IsOpen)
        {
            SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
        }
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (open) DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
        canvas.enabled = open;
        if (open) Refresh();
    }

    /// <summary>One display line per started quest plus its objectives (test seam).</summary>
    public static string[] Lines(StoryState s)
    {
        var rows = new System.Collections.Generic.List<string>();
        foreach (var q in StoryCatalog.All)
        {
            int status = s.Status(q.id);
            if (status == StoryStatus.Locked || status == StoryStatus.Available) continue;
            string mark = status == StoryStatus.Done ? " ✓" : status == StoryStatus.Ready ? " !" : "";
            rows.Add(Loc.T(q.TitleKey) + mark);
            if (status == StoryStatus.Done) continue;
            for (int i = 0; i < q.objectives.Length; i++)
            {
                var o = q.objectives[i];
                string row = "  " + Loc.T(o.textKey);
                if (o.count > 1) row += "  " + s.Count(q.id, i) + "/" + o.count;
                else if (s.ObjectiveDone(q, i)) row += " ✓";
                rows.Add(row);
            }
        }
        return rows.ToArray();
    }

    public void Refresh()
    {
        EnsureBuilt();
        foreach (Transform child in list) Destroy(child.gameObject);
        var s = StoryDirector.State;
        AddSection(s, true);
        AddSection(s, false);
        bool any = false;
        foreach (var q in StoryCatalog.All)
            if (s.Status(q.id) > StoryStatus.Available) { any = true; break; }
        if (!any) AddRow(Loc.T("story.log.empty"), DiabloPalette.ParchmentDim, 15);
    }

    void AddSection(StoryState s, bool main)
    {
        bool header = false;
        foreach (var q in StoryCatalog.All)
        {
            if (q.main != main) continue;
            int status = s.Status(q.id);
            if (status == StoryStatus.Locked || status == StoryStatus.Available) continue;
            if (!header)
            {
                AddRow(Loc.T(main ? "story.log.main" : "story.log.side"), DiabloPalette.TrimGold, 16);
                header = true;
            }
            string mark = status == StoryStatus.Done ? " ✓" : status == StoryStatus.Ready ? " !" : "";
            AddRow(Loc.F("story.log.act", q.act) + ": " + Loc.T(q.TitleKey) + mark,
                status == StoryStatus.Done ? DiabloPalette.ParchmentDim : DiabloPalette.GoldBright, 17);
            if (status == StoryStatus.Done) continue;
            AddRow(Loc.T(q.SummaryKey), DiabloPalette.ParchmentDim, 13);
            for (int i = 0; i < q.objectives.Length; i++)
            {
                var o = q.objectives[i];
                string row = "  " + Loc.T(o.textKey);
                if (o.count > 1) row += "  " + s.Count(q.id, i) + "/" + o.count;
                else if (s.ObjectiveDone(q, i)) row += " ✓";
                AddRow(row, s.ObjectiveDone(q, i) ? DiabloPalette.ParchmentDim : DiabloPalette.Parchment, 14);
            }
        }
    }

    void AddRow(string text, Color color, int size)
    {
        var t = DiabloUIFactory.MakeText(list, "QuestRow", text, size, color);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        DiabloUIFactory.SetLayout(t.gameObject, prefH: size + 10f, flexW: 1f);
    }

    void EnsureBuilt()
    {
        if (canvas != null) return;
        var canvasGo = new GameObject("StoryLogCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 24);
        canvas.enabled = false;
        var panel = DiabloUIFactory.MakePanel(canvasGo.transform, "LogPanel", new Vector2(680f, 620f), Vector2.zero);
        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 16, 16);
        v.spacing = 8f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        DiabloUIFactory.MakeHeader(panel.transform, Loc.T("story.log.title"), null, () => SetOpen(false));
        var listGo = new GameObject("LogList", typeof(RectTransform));
        listGo.transform.SetParent(panel.transform, false);
        DiabloUIFactory.SetLayout(listGo, prefH: 500f, flexW: 1f);
        var lv = listGo.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 4f;
        lv.childControlWidth = true;
        lv.childControlHeight = false;
        list = listGo.transform;
    }
}
