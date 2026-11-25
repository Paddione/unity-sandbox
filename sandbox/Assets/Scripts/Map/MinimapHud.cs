using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimap in the top-right corner (cinderreach-group-quest-map): a window of
/// <see cref="ViewMeters"/> around the hero on the zone picture
/// (<see cref="ZoneMap"/>), turned like the camera, with live icons for
/// gates, waypoints, NPCs, enemies, allies and tracked quest places. Places
/// outside the window, or in another zone (routed to the gate toward them),
/// sit on the rim as arrows. Only the local hero builds it; remote mirrors stay bare.
/// </summary>
public sealed class MinimapHud : MonoBehaviour
{
    public const float ViewMeters = 28f;
    public const float SizePx = 220f;

    Canvas canvas;
    RawImage picture;
    RectTransform viewport;
    Text zoneLabel;
    MapIconLayer layer;
    StoryDirector director;
    int pictureZone = -1;
    float iconTick;
    readonly List<MapIcon> icons = new List<MapIcon>();

    /// <summary>True once built (test seam).</summary>
    public bool Built => canvas != null;
    public bool Visible => canvas != null && canvas.enabled;
    public int IconsShown => layer != null ? layer.Shown : 0;

    void Update()
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) { if (canvas != null) canvas.enabled = false; return; }
        if (canvas == null) Build();
        var map = GetComponent<WorldMapUI>();
        bool show = (map == null || !map.IsOpen) && !ClassSelectUI.AnyOpen;
        if (canvas.enabled != show) canvas.enabled = show;
        if (!show) return;

        int zone = ZoneMap.IndexAt(transform.position);
        var z = ZoneMap.Zone(zone);
        if (zone != pictureZone)
        {
            pictureZone = zone;
            var tex = ZoneMap.Picture(zone);
            picture.texture = tex;
            picture.color = tex != null ? Color.white : ZoneMap.Background;
            zoneLabel.text = ActProgress.ZoneName(zone);
            iconTick = 0f;
        }
        float frac = ViewMeters / ZoneMap.Span(z);
        var heroMap = ZoneMap.ToMap(transform.position, z);
        picture.uvRect = new Rect(heroMap.x - frac * 0.5f, heroMap.y - frac * 0.5f, frac, frac);

        iconTick -= Time.unscaledDeltaTime;
        if (iconTick > 0f) return;
        iconTick = 0.1f;
        if (director == null) director = GetComponent<StoryDirector>();
        MapScene.Gather(zone, transform, director != null ? director.Marks : null, director != null ? director.World : null,
            routeOtherZones: true, withEnemies: true, icons);
        float pxPerUnit = SizePx / frac;
        var half = new Vector2(SizePx * 0.5f, SizePx * 0.5f);
        layer.Draw(icons, w => (ZoneMap.ToMap(w, z) - heroMap) * pxPerUnit, half);
    }

    void Build()
    {
        var canvasGo = new GameObject("MinimapCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 22, raycaster: false);

        var frame = DiabloUIFactory.MakePanel(canvasGo.transform, "MinimapFrame", new Vector2(SizePx + 12f, SizePx + 36f), Vector2.zero);
        var fr = (RectTransform)frame.transform;
        fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(1f, 1f);
        fr.anchoredPosition = new Vector2(-14f, -40f);
        foreach (var g in frame.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;

        var vpGo = new GameObject("MinimapViewport", typeof(RectTransform));
        vpGo.transform.SetParent(frame.transform, false);
        viewport = (RectTransform)vpGo.transform;
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.anchoredPosition = new Vector2(0f, -6f);
        viewport.sizeDelta = new Vector2(SizePx, SizePx);
        vpGo.AddComponent<RectMask2D>();

        var picGo = new GameObject("MinimapPicture", typeof(RectTransform));
        picGo.transform.SetParent(viewport, false);
        picture = picGo.AddComponent<RawImage>();
        picture.raycastTarget = false;
        var pr = picture.rectTransform;
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        var iconsGo = new GameObject("MinimapIcons", typeof(RectTransform));
        iconsGo.transform.SetParent(viewport, false);
        var ir = (RectTransform)iconsGo.transform;
        ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0.5f, 0.5f);
        ir.sizeDelta = Vector2.zero;
        layer = new MapIconLayer(ir);

        zoneLabel = DiabloUIFactory.MakeText(frame.transform, "MinimapZone", "", 14, DiabloPalette.TrimGold, TextAnchor.MiddleCenter);
        zoneLabel.raycastTarget = false;
        var zr = zoneLabel.rectTransform;
        zr.anchorMin = zr.anchorMax = new Vector2(0.5f, 0f);
        zr.pivot = new Vector2(0.5f, 0f);
        zr.anchoredPosition = new Vector2(0f, 4f);
        zr.sizeDelta = new Vector2(SizePx, 22f);
    }
}
