using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Full zone map on M (cinderreach-group-quest-map): the whole zone picture
/// with live icons, ◀/▶ through the zones the act has opened, a legend and the
/// tracked tasks with where they lead. Opens on the hero's zone. Quest places
/// show on their own zone; the hero's zone also marks the gate toward places
/// elsewhere. Esc or M closes it.
/// </summary>
public sealed class WorldMapUI : MonoBehaviour
{
    public const float MapPx = 640f;

    public bool IsOpen { get; private set; }
    /// <summary>Zone index on display.</summary>
    public int ViewZone { get; private set; } = ActProgress.Town;
    public int IconsShown => layer != null ? layer.Shown : 0;

    Canvas canvas;
    RawImage picture;
    Text title, questList;
    MapIconLayer layer;
    StoryDirector director;
    float tick;
    readonly List<MapIcon> icons = new List<MapIcon>();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || DiabloUIFactory.IsRemoteMirror(this)) return;
        if (kb.mKey.wasPressedThisFrame && (IsOpen || !DiabloUIFactory.IsAnyModalOpen(gameObject)))
            SetOpen(!IsOpen);
        else if (kb.escapeKey.wasPressedThisFrame && IsOpen)
        {
            SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
        }
        else if (IsOpen && kb.leftArrowKey.wasPressedThisFrame) Cycle(-1);
        else if (IsOpen && kb.rightArrowKey.wasPressedThisFrame) Cycle(1);
        if (!IsOpen) return;
        tick -= Time.unscaledDeltaTime;
        if (tick <= 0f) { tick = 0.15f; Redraw(); }
    }

    public void SetOpen(bool open)
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        EnsureBuilt();
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (open)
        {
            DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
            ShowZone(ZoneMap.IndexAt(transform.position));
        }
        canvas.enabled = open;
    }

    /// <summary>Steps to the previous/next zone the act has opened (wraps).</summary>
    public void Cycle(int dir)
    {
        int z = ViewZone;
        for (int i = 0; i < ActProgress.LastZone; i++)
        {
            z += dir;
            if (z > ActProgress.LastZone) z = ActProgress.Town;
            if (z < ActProgress.Town) z = ActProgress.LastZone;
            if (RunProfile.Act.IsUnlocked(z)) break;
        }
        ShowZone(z);
    }

    void ShowZone(int zone)
    {
        ViewZone = zone;
        var tex = ZoneMap.Picture(zone);
        picture.texture = tex;
        picture.color = tex != null ? Color.white : ZoneMap.Background;
        title.text = Loc.F("map.title", ActProgress.ZoneName(zone));
        tick = 0f;
        Redraw();
    }

    void Redraw()
    {
        if (director == null) director = GetComponent<StoryDirector>();
        var marks = director != null ? director.Marks : null;
        var world = director != null ? director.World : null;
        int heroZone = ZoneMap.IndexAt(transform.position);
        MapScene.Gather(ViewZone, transform, marks, world, routeOtherZones: ViewZone == heroZone, withEnemies: true, icons);
        var z = ZoneMap.Zone(ViewZone);
        layer.Draw(icons, w => (ZoneMap.ToMap(w, z) - new Vector2(0.5f, 0.5f)) * MapPx, new Vector2(MapPx * 0.5f, MapPx * 0.5f));
        questList.text = QuestLines(StoryDirector.State, marks);
    }

    /// <summary>Tracked tasks with the zone each leads to (test seam).</summary>
    public static string QuestLines(StoryState s, List<QuestMark> marks)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var q in StoryTracking.Tracked(s))
        {
            sb.Append(q.main ? "<color=#f9cc47>◆</color> " : "<color=#dbdbcc>◆</color> ");
            sb.Append(Loc.T(q.TitleKey));
            int zone = 0;
            if (marks != null)
                foreach (var m in marks)
                    if (m.questId == q.id && m.kind != QuestMarkKind.Offer) { zone = m.zone; break; }
            if (zone > 0) sb.Append("  <color=#a69985>— ").Append(ActProgress.ZoneName(zone)).Append("</color>");
            sb.Append('\n');
        }
        if (sb.Length == 0) sb.Append(Loc.T("map.noquest"));
        return sb.ToString().TrimEnd('\n');
    }

    void EnsureBuilt()
    {
        if (canvas != null) return;
        var canvasGo = new GameObject("WorldMapCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 24);
        canvas.enabled = false;
        var panel = DiabloUIFactory.MakePanel(canvasGo.transform, "MapPanel", new Vector2(MapPx + 360f, MapPx + 110f), Vector2.zero);

        var header = DiabloUIFactory.MakeHeader(panel.transform, "", null, () => SetOpen(false));
        var hr = (RectTransform)header.transform;
        hr.anchorMin = new Vector2(0f, 1f); hr.anchorMax = new Vector2(1f, 1f); hr.pivot = new Vector2(0.5f, 1f);
        hr.anchoredPosition = new Vector2(0f, -10f);
        hr.sizeDelta = new Vector2(-24f, 52f);
        title = header.transform.Find("BannerLabel").GetComponent<Text>();
        var prev = DiabloUIFactory.MakeButton(header.transform, "MapPrev", "◀", 16, false, DiabloPalette.Parchment, () => Cycle(-1));
        Place((RectTransform)prev.transform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 32f));
        var next = DiabloUIFactory.MakeButton(header.transform, "MapNext", "▶", 16, false, DiabloPalette.Parchment, () => Cycle(1));
        Place((RectTransform)next.transform, new Vector2(1f, 0.5f), new Vector2(-54f, 0f), new Vector2(40f, 32f));

        var frameGo = new GameObject("MapFrame", typeof(RectTransform));
        frameGo.transform.SetParent(panel.transform, false);
        var frt = (RectTransform)frameGo.transform;
        frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(0f, 0f);
        frt.anchoredPosition = new Vector2(22f, 22f);
        frt.sizeDelta = new Vector2(MapPx, MapPx);
        frameGo.AddComponent<RectMask2D>();
        var picGo = new GameObject("MapPicture", typeof(RectTransform));
        picGo.transform.SetParent(frt, false);
        picture = picGo.AddComponent<RawImage>();
        picture.raycastTarget = false;
        var pr = picture.rectTransform;
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        var iconsGo = new GameObject("MapIcons", typeof(RectTransform));
        iconsGo.transform.SetParent(frt, false);
        var ir = (RectTransform)iconsGo.transform;
        ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0.5f, 0.5f);
        ir.sizeDelta = Vector2.zero;
        layer = new MapIconLayer(ir);

        var side = new GameObject("MapSide", typeof(RectTransform));
        side.transform.SetParent(panel.transform, false);
        var sr = (RectTransform)side.transform;
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(1f, 0f);
        sr.anchoredPosition = new Vector2(-22f, 22f);
        sr.sizeDelta = new Vector2(310f, MapPx);
        var v = side.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        var qh = DiabloUIFactory.MakeText(side.transform, "MapQuestHeader", Loc.T("map.quests"), 17, DiabloPalette.TrimGold);
        DiabloUIFactory.SetLayout(qh.gameObject, prefH: 26f, flexW: 1f);
        questList = DiabloUIFactory.MakeText(side.transform, "MapQuestList", "", 15, DiabloPalette.Parchment, TextAnchor.UpperLeft);
        DiabloUIFactory.SetLayout(questList.gameObject, prefH: 250f, flexW: 1f);
        var lh = DiabloUIFactory.MakeText(side.transform, "MapLegendHeader", Loc.T("map.legend"), 17, DiabloPalette.TrimGold);
        DiabloUIFactory.SetLayout(lh.gameObject, prefH: 26f, flexW: 1f);
        Legend(side.transform, MapIconShape.Arrow, MapIcons.Self, "map.legend.you");
        Legend(side.transform, MapIconShape.Diamond, MapIcons.QuestMain, "map.legend.main");
        Legend(side.transform, MapIconShape.Diamond, MapIcons.QuestSide, "map.legend.side");
        Legend(side.transform, MapIconShape.Square, MapIcons.QuestMain, "map.legend.turnin");
        Legend(side.transform, MapIconShape.Diamond, MapIcons.Offer, "map.legend.offer");
        Legend(side.transform, MapIconShape.Dot, MapIcons.Ally, "map.legend.ally");
        Legend(side.transform, MapIconShape.Dot, MapIcons.Npc, "map.legend.npc");
        Legend(side.transform, MapIconShape.Dot, MapIcons.Enemy, "map.legend.enemy");
        Legend(side.transform, MapIconShape.Ring, MapIcons.Gate, "map.legend.gate");
        Legend(side.transform, MapIconShape.Diamond, MapIcons.Waypoint, "map.legend.waypoint");
        var hint = DiabloUIFactory.MakeText(side.transform, "MapHint", Loc.T("map.hint"), 13, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(hint.gameObject, prefH: 36f, flexW: 1f);
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Legend(Transform parent, MapIconShape shape, Color color, string key)
    {
        var row = new GameObject("Legend_" + key, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(row, prefH: 22f, flexW: 1f);
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(row.transform, false);
        var img = iconGo.AddComponent<Image>();
        img.sprite = MapIcons.For(shape);
        img.color = color;
        img.raycastTarget = false;
        Place(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(14f, 14f));
        var t = DiabloUIFactory.MakeText(row.transform, "Label", Loc.T(key), 14, DiabloPalette.Parchment);
        var tr = t.rectTransform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(28f, 0f); tr.offsetMax = Vector2.zero;
    }
}
