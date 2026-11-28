using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Display mode of the zone map (cinderreach-map-overlay D2/D3).</summary>
public enum WorldMapMode { Overlay, Fullscreen }

/// <summary>
/// Zone map on M (cinderreach-group-quest-map, cinderreach-map-overlay): the whole
/// zone picture with live icons, ◀/▶ through the zones the act has opened, tracked
/// tasks on the left, a filterable legend and zoom 75–150 % centred on the hero.
/// Overlay (default) keeps the world visible and dimmed under a translucent map and
/// does not block movement; Fullscreen is the opaque modal form. Tab switches,
/// the choice is remembered in DiabloSettings. Esc or M closes it.
/// </summary>
public sealed class WorldMapUI : MonoBehaviour
{
    public const float MapPx = 640f;

    public bool IsOpen { get; private set; }
    /// <summary>Zone index on display.</summary>
    public int ViewZone { get; private set; } = ActProgress.Town;
    public int IconsShown => layer != null ? layer.Shown : 0;

    /// <summary>Current display mode; restored from DiabloSettings on open.</summary>
    public WorldMapMode Mode { get; private set; } = WorldMapMode.Overlay;
    public bool IsFullscreen => Mode == WorldMapMode.Fullscreen;
    public bool IsOverlay => Mode == WorldMapMode.Overlay;
    /// <summary>True while open in the modal (fullscreen) form; overlay never blocks the world.</summary>
    public bool BlocksWorld => IsOpen && IsFullscreen;

    /// <summary>Map zoom 0.75–1.5 (WorldMapZoom).</summary>
    public float Zoom { get; private set; } = 1f;
    /// <summary>Legend categories currently shown.</summary>
    public MapCategory EnabledCategories { get; private set; } = MapCategory.All;

    /// <summary>Backdrop alpha of the map backing (test seam: translucent overlay, opaque fullscreen).</summary>
    public float BackingAlpha => backing != null ? backing.color.a : 0f;
    /// <summary>Full-screen dim alpha behind the panel (test seam; 0 in fullscreen).</summary>
    public float DimAlpha => dim != null && dim.gameObject.activeSelf ? dim.color.a : 0f;

    Canvas canvas;
    RawImage picture;
    Image dim, backing, panelImage;
    Text title, questList, zoomLabel, modeLabel;
    MapIconLayer layer;
    StoryDirector director;
    float tick;
    Vector2 lastPicSize = Vector2.zero;
    Vector2 lastPicPos = new Vector2(float.NaN, float.NaN);
    int lastQuestRevision = -1;
    int lastQuestZone = -1;
    readonly List<MapIcon> icons = new List<MapIcon>();
    readonly List<MapIcon> filtered = new List<MapIcon>();
    readonly Dictionary<MapCategory, Text> filterLabels = new Dictionary<MapCategory, Text>();
    readonly Dictionary<MapCategory, Image> filterIcons = new Dictionary<MapCategory, Image>();

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
        else if (IsOpen && kb.tabKey.wasPressedThisFrame) ToggleMode();
        else if (IsOpen && kb.leftArrowKey.wasPressedThisFrame) Cycle(-1);
        else if (IsOpen && kb.rightArrowKey.wasPressedThisFrame) Cycle(1);
        if (!IsOpen) return;
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.y.ReadValue();
            if (Mathf.Abs(scroll) > 0.01f) SetZoom(Zoom + scroll * 0.001f);
        }
        tick -= Time.unscaledDeltaTime;
        if (tick <= 0f) { tick = 0.3f; Redraw(); }
    }

    public void SetOpen(bool open)
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        EnsureBuilt();
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (open)
        {
            Mode = DiabloSettings.MapFullscreen ? WorldMapMode.Fullscreen : WorldMapMode.Overlay;
            ApplyMode();
            DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
            ShowZone(ZoneMap.IndexAt(transform.position));
        }
        canvas.enabled = open;
    }

    /// <summary>Switches overlay ↔ fullscreen and remembers the choice (Tab in play).</summary>
    public void ToggleMode() => SetMode(IsFullscreen ? WorldMapMode.Overlay : WorldMapMode.Fullscreen);

    /// <summary>Sets the display mode, persists it in DiabloSettings and repaints.</summary>
    public void SetMode(WorldMapMode mode)
    {
        Mode = mode;
        DiabloSettings.MapFullscreen = mode == WorldMapMode.Fullscreen;
        DiabloSettings.Save();
        EnsureBuilt();
        ApplyMode();
        if (IsOpen) { tick = 0f; Redraw(); }
    }

    /// <summary>Clamps and applies a new zoom (wheel and −/+ buttons in play).</summary>
    public void SetZoom(float zoom)
    {
        Zoom = WorldMapZoom.ClampZoom(zoom);
        if (zoomLabel != null) zoomLabel.text = Mathf.RoundToInt(Zoom * 100f) + "%";
        if (IsOpen) Redraw();
    }

    public void ZoomBy(float delta) => SetZoom(Zoom + delta);

    /// <summary>Toggles one legend category; returns the new visibility.</summary>
    public bool ToggleCategory(MapCategory category)
    {
        SetCategory(category, (EnabledCategories & category) == 0);
        return (EnabledCategories & category) != 0;
    }

    public void SetCategory(MapCategory category, bool visible)
    {
        EnabledCategories = visible ? EnabledCategories | category : EnabledCategories & ~category;
        PaintFilters();
        if (IsOpen) Redraw();
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
        ApplyPictureColor(tex);
        title.text = Loc.F("map.title", ActProgress.ZoneName(zone));
        tick = 0f;
        Redraw();
    }

    void ApplyPictureColor(Texture tex)
    {
        if (picture == null) return;
        var c = tex != null ? Color.white : ZoneMap.Background;
        c.a = IsFullscreen ? 1f : 0.92f;
        picture.color = c;
    }

    void ApplyMode()
    {
        if (dim != null)
        {
            dim.gameObject.SetActive(IsOverlay);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
        }
        if (backing != null)
        {
            var c = DiabloPalette.Abyss;
            c.a = IsFullscreen ? 1f : 0.78f;
            backing.color = c;
        }
        if (panelImage != null)
        {
            var c = panelImage.color;
            c.a = IsFullscreen ? 1f : 0.9f;
            panelImage.color = c;
        }
        if (picture != null) ApplyPictureColor(picture.texture);
        if (modeLabel != null) modeLabel.text = Loc.T(IsFullscreen ? "map.mode.fullscreen" : "map.mode.overlay");
    }

    void Redraw()
    {
        if (picture == null) return;
        if (director == null) director = GetComponent<StoryDirector>();
        var marks = director != null ? director.Marks : null;
        var world = director != null ? director.World : null;
        int heroZone = ZoneMap.IndexAt(transform.position);
        MapScene.Gather(ViewZone, transform, marks, world, routeOtherZones: ViewZone == heroZone, withEnemies: true, icons);
        WorldMapFilter.Apply(icons, EnabledCategories, filtered);
        var z = ZoneMap.Zone(ViewZone);
        var centre = ViewZone == heroZone ? ZoneMap.ToMap(transform.position, z) : new Vector2(0.5f, 0.5f);
        // cinderreach-map-overlay perf: RectTransform writes dirty the whole
        // canvas; touch the picture only when zoom/zone/centre actually moved.
        var picSize = new Vector2(MapPx * Zoom, MapPx * Zoom);
        if (picSize != lastPicSize) { picture.rectTransform.sizeDelta = picSize; lastPicSize = picSize; }
        var picPos = WorldMapZoom.Offset(centre, Zoom, MapPx);
        if (picPos != lastPicPos) { picture.rectTransform.anchoredPosition = picPos; lastPicPos = picPos; }
        layer.Draw(filtered, w => WorldMapZoom.ToLocal(ZoneMap.ToMap(w, z), centre, Zoom, MapPx), new Vector2(MapPx * 0.5f, MapPx * 0.5f));
        // The quest list only changes with story state or the viewed zone.
        int rev = StoryDirector.State != null ? StoryDirector.State.Revision : -1;
        if (rev != lastQuestRevision || ViewZone != lastQuestZone)
        {
            questList.text = QuestLines(StoryDirector.State, marks);
            lastQuestRevision = rev;
            lastQuestZone = ViewZone;
        }
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

        // Full-screen dim behind the panel (overlay only); never raycasts, so world clicks fall through.
        var dimGo = new GameObject("MapDim", typeof(RectTransform));
        dimGo.transform.SetParent(canvasGo.transform, false);
        dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = false;
        HudChrome.Stretch(dim.rectTransform, 0f);

        var panel = DiabloUIFactory.MakePanel(canvasGo.transform, "MapPanel", new Vector2(MapPx + 606f, MapPx + 110f), Vector2.zero);
        panelImage = panel.GetComponent<Image>();
        if (panelImage != null) panelImage.raycastTarget = false;
        // The framed panel sprite is see-through in the middle; world nameplates showed behind the legend.
        var backingGo = new GameObject("MapBacking", typeof(RectTransform));
        backingGo.transform.SetParent(panel.transform, false);
        backing = backingGo.AddComponent<Image>();
        backing.color = DiabloPalette.Abyss;
        backing.raycastTarget = false;
        var brt = backing.rectTransform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(8f, 8f); brt.offsetMax = new Vector2(-8f, -8f);

        var header = DiabloUIFactory.MakeHeader(panel.transform, "", null, () => SetOpen(false));
        var hr = (RectTransform)header.transform;
        hr.anchorMin = new Vector2(0f, 1f); hr.anchorMax = new Vector2(1f, 1f); hr.pivot = new Vector2(0.5f, 1f);
        hr.anchoredPosition = new Vector2(0f, -10f);
        hr.sizeDelta = new Vector2(-24f, 52f);
        title = header.transform.Find("BannerLabel").GetComponent<Text>();
        title.font = DiabloUIFactory.DisplayFont;
        var prev = DiabloUIFactory.MakeButton(header.transform, "MapPrev", "◀", 16, false, DiabloPalette.Parchment, () => Cycle(-1));
        Place((RectTransform)prev.transform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 32f));
        var next = DiabloUIFactory.MakeButton(header.transform, "MapNext", "▶", 16, false, DiabloPalette.Parchment, () => Cycle(1));
        Place((RectTransform)next.transform, new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(40f, 32f));
        var zoomOut = DiabloUIFactory.MakeButton(header.transform, "MapZoomOut", "−", 16, false, DiabloPalette.Parchment, () => ZoomBy(-0.25f));
        Place((RectTransform)zoomOut.transform, new Vector2(1f, 0.5f), new Vector2(-308f, 0f), new Vector2(40f, 32f));
        zoomLabel = DiabloUIFactory.MakeText(header.transform, "MapZoomLabel", "100%", 14, DiabloPalette.TrimGold, TextAnchor.MiddleCenter, font: DiabloUIFactory.BodyFont);
        Place(zoomLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-240f, 0f), new Vector2(56f, 32f));
        var zoomIn = DiabloUIFactory.MakeButton(header.transform, "MapZoomIn", "+", 16, false, DiabloPalette.Parchment, () => ZoomBy(0.25f));
        Place((RectTransform)zoomIn.transform, new Vector2(1f, 0.5f), new Vector2(-188f, 0f), new Vector2(40f, 32f));
        var modeBtn = DiabloUIFactory.MakeButton(header.transform, "MapModeButton", "", 14, false, DiabloPalette.Parchment, ToggleMode);
        Place((RectTransform)modeBtn.transform, new Vector2(1f, 0.5f), new Vector2(-56f, 0f), new Vector2(120f, 32f));
        modeLabel = modeBtn.GetComponentInChildren<Text>();

        // Tracked tasks on the left (GOAL: links steht die verfolgte Quest).
        var questPanel = HudChrome.Panel(panel.transform, "MapQuestPanel", new Vector2(250f, MapPx), goldTop: true);
        Place(questPanel, new Vector2(0f, 0f), new Vector2(22f, 22f), new Vector2(250f, MapPx));
        var questImg = questPanel.GetComponent<Image>();
        if (questImg != null) questImg.raycastTarget = false;
        var qh = DiabloUIFactory.MakeText(questPanel, "MapQuestHeader", Loc.T("map.quests"), 16, DiabloPalette.TrimGold, font: DiabloUIFactory.DisplayFont);
        var qhr = qh.rectTransform;
        qhr.anchorMin = new Vector2(0f, 1f); qhr.anchorMax = new Vector2(1f, 1f); qhr.pivot = new Vector2(0.5f, 1f);
        qhr.offsetMin = new Vector2(12f, -34f); qhr.offsetMax = new Vector2(-12f, -6f);
        questList = DiabloUIFactory.MakeText(questPanel, "MapQuestList", "", 14, DiabloPalette.Parchment, TextAnchor.UpperLeft, font: DiabloUIFactory.BodyFont);
        var qlr = questList.rectTransform;
        qlr.anchorMin = Vector2.zero; qlr.anchorMax = Vector2.one;
        qlr.offsetMin = new Vector2(12f, 12f); qlr.offsetMax = new Vector2(-12f, -42f);

        var frameGo = new GameObject("MapFrame", typeof(RectTransform));
        frameGo.transform.SetParent(panel.transform, false);
        var frt = (RectTransform)frameGo.transform;
        frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(0f, 0f);
        frt.anchoredPosition = new Vector2(288f, 22f);
        frt.sizeDelta = new Vector2(MapPx, MapPx);
        frameGo.AddComponent<RectMask2D>();
        var picGo = new GameObject("MapPicture", typeof(RectTransform));
        picGo.transform.SetParent(frt, false);
        picture = picGo.AddComponent<RawImage>();
        picture.raycastTarget = false;
        var pr = picture.rectTransform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(MapPx, MapPx);
        pr.anchoredPosition = Vector2.zero;
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
        sr.sizeDelta = new Vector2(280f, MapPx);
        var v = side.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        var lh = DiabloUIFactory.MakeText(side.transform, "MapLegendHeader", Loc.T("map.legend"), 16, DiabloPalette.TrimGold, font: DiabloUIFactory.DisplayFont);
        DiabloUIFactory.SetLayout(lh.gameObject, prefH: 26f, flexW: 1f);
        FilterRow(side.transform, MapCategory.Quests, MapIconShape.Diamond, MapIcons.QuestMain, "map.filter.quests");
        FilterRow(side.transform, MapCategory.Party, MapIconShape.Dot, MapIcons.Ally, "map.filter.party");
        FilterRow(side.transform, MapCategory.Waypoints, MapIconShape.Diamond, MapIcons.Waypoint, "map.filter.waypoints");
        FilterRow(side.transform, MapCategory.Persons, MapIconShape.Dot, MapIcons.Npc, "map.filter.persons");
        FilterRow(side.transform, MapCategory.Elites, MapIconShape.Dot, MapIcons.Enemy, "map.filter.elites");
        Legend(side.transform, MapIconShape.Arrow, MapIcons.Self, "map.legend.you");
        var hint = DiabloUIFactory.MakeText(side.transform, "MapHint", Loc.T("map.hint"), 13, DiabloPalette.ParchmentDim, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(hint.gameObject, prefH: 52f, flexW: 1f);
        SetZoom(1f);
        ApplyMode();
        PaintFilters();
    }

    void FilterRow(Transform parent, MapCategory category, MapIconShape shape, Color color, string key)
    {
        var btn = DiabloUIFactory.MakeButton(parent, "MapFilter_" + category, "", 14, false, DiabloPalette.Parchment, () => ToggleCategory(category));
        DiabloUIFactory.SetLayout(btn.gameObject, prefH: 38f, flexW: 1f);
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(btn.transform, false);
        var img = iconGo.AddComponent<Image>();
        img.sprite = MapIcons.For(shape);
        img.color = color;
        img.raycastTarget = false;
        Place(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(16f, 16f));
        var label = btn.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.font = DiabloUIFactory.BodyFont;
            var lr = label.rectTransform;
            lr.offsetMin = new Vector2(34f, 2f);
            lr.offsetMax = new Vector2(-8f, -2f);
            label.alignment = TextAnchor.MiddleLeft;
            filterLabels[category] = label;
            filterIcons[category] = img;
            label.text = Loc.T(key);
        }
        PaintFilter(category);
    }

    void PaintFilters()
    {
        foreach (var category in new[] { MapCategory.Quests, MapCategory.Party, MapCategory.Waypoints, MapCategory.Persons, MapCategory.Elites })
            PaintFilter(category);
    }

    void PaintFilter(MapCategory category)
    {
        if (!filterLabels.TryGetValue(category, out var label) || label == null) return;
        bool on = (EnabledCategories & category) != 0;
        string key = category switch
        {
            MapCategory.Quests => "map.filter.quests",
            MapCategory.Party => "map.filter.party",
            MapCategory.Waypoints => "map.filter.waypoints",
            MapCategory.Persons => "map.filter.persons",
            _ => "map.filter.elites",
        };
        label.text = (on ? "☑ " : "☐ ") + Loc.T(key);
        label.color = on ? DiabloPalette.Parchment : DiabloPalette.DisabledText;
        if (filterIcons.TryGetValue(category, out var img) && img != null)
        {
            var c = img.color;
            c.a = on ? 1f : 0.35f;
            img.color = c;
        }
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
        var t = DiabloUIFactory.MakeText(row.transform, "Label", Loc.T(key), 14, DiabloPalette.Parchment, font: DiabloUIFactory.BodyFont);
        var tr = t.rectTransform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(28f, 0f); tr.offsetMax = Vector2.zero;
    }
}
