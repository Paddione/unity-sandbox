using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum MapIconShape { Dot, Diamond, Arrow, Ring, Square }

/// <summary>One live map icon at a world position.</summary>
public struct MapIcon
{
    public Vector3 world;
    public MapIconShape shape;
    public Color color;
    public float size;
    /// <summary>UI z-rotation in degrees (arrows).</summary>
    public float rotation;
    /// <summary>Quest icons stay on the minimap edge as an arrow when their place is out of view.</summary>
    public bool pinToEdge;
}

/// <summary>Procedural icon sprites (no texture assets): dot, diamond, arrow, ring, square.</summary>
public static class MapIcons
{
    const int Size = 64;
    static readonly Dictionary<MapIconShape, Sprite> sprites = new Dictionary<MapIconShape, Sprite>();

    public static readonly Color QuestMain = new Color(0.98f, 0.80f, 0.28f, 1f);
    public static readonly Color QuestSide = new Color(0.86f, 0.86f, 0.80f, 1f);
    public static readonly Color Offer = new Color(0.96f, 0.56f, 0.18f, 1f);
    public static readonly Color Ally = new Color(0.55f, 0.72f, 0.88f, 1f);
    public static readonly Color Enemy = new Color(0.82f, 0.16f, 0.14f, 1f);
    public static readonly Color Npc = new Color(0.91f, 0.85f, 0.75f, 1f);
    public static readonly Color Gate = new Color(0.68f, 0.48f, 0.95f, 1f);
    public static readonly Color GateLocked = new Color(0.42f, 0.40f, 0.44f, 1f);
    public static readonly Color Waypoint = new Color(0.40f, 0.85f, 0.90f, 1f);
    public static readonly Color Self = Color.white;

    public static Sprite For(MapIconShape shape)
    {
        if (sprites.TryGetValue(shape, out var s) && s != null) return s;
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "MapIcon_" + shape,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        var px = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size * 2f - 1f, v = (y + 0.5f) / Size * 2f - 1f;
                float a = Coverage(shape, u, v);
                px[y * Size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        s.name = tex.name;
        sprites[shape] = s;
        return s;
    }

    /// <summary>Alpha of a shape at (u, v) in [-1, 1], with a soft 2-pixel edge.</summary>
    static float Coverage(MapIconShape shape, float u, float v)
    {
        const float edge = 2f / Size * 2f;
        float d; // signed distance, negative inside
        switch (shape)
        {
            case MapIconShape.Diamond: d = (Mathf.Abs(u) + Mathf.Abs(v)) * 0.7071f - 0.66f; break;
            case MapIconShape.Square: d = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) - 0.72f; break;
            case MapIconShape.Ring: d = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - 0.72f) - 0.16f; break;
            case MapIconShape.Arrow:
                // Pointing up: apex (0, 0.9), base corners (±0.75, -0.75), notch up to (0, -0.35).
                d = Mathf.Abs(u) * 0.92f + v * 0.42f - 0.38f;
                d = Mathf.Max(d, -(v + 0.35f + 0.57f * Mathf.Abs(u)) * 0.85f);
                break;
            default: d = Mathf.Sqrt(u * u + v * v) - 0.8f; break;
        }
        return 0.5f - d / edge;
    }
}

/// <summary>Pooled UI images for live map icons on one rect.</summary>
public sealed class MapIconLayer
{
    readonly RectTransform root;
    readonly List<Image> pool = new List<Image>();
    public int Shown { get; private set; }

    public MapIconLayer(RectTransform root) { this.root = root; }

    /// <summary>
    /// Places <paramref name="icons"/>; <paramref name="toLocal"/> maps a world
    /// point to local pixels on <c>root</c> (centre pivot). With
    /// <paramref name="halfSize"/> &gt; 0, icons outside that box are dropped,
    /// except edge-pinned ones, which sit on the border as an outward arrow.
    /// </summary>
    public void Draw(List<MapIcon> icons, System.Func<Vector3, Vector2> toLocal, Vector2 halfSize)
    {
        int n = 0;
        foreach (var icon in icons)
        {
            var p = toLocal(icon.world);
            var shape = icon.shape;
            float rot = icon.rotation, size = icon.size;
            if (halfSize.x > 0f && (Mathf.Abs(p.x) > halfSize.x || Mathf.Abs(p.y) > halfSize.y))
            {
                if (!icon.pinToEdge) continue;
                float inset = size * 0.6f;
                float sx = (halfSize.x - inset) / Mathf.Max(0.001f, Mathf.Abs(p.x));
                float sy = (halfSize.y - inset) / Mathf.Max(0.001f, Mathf.Abs(p.y));
                rot = Mathf.Atan2(p.x, p.y) * -Mathf.Rad2Deg;
                p *= Mathf.Min(sx, sy);
                shape = MapIconShape.Arrow;
            }
            var img = Get(n++);
            img.sprite = MapIcons.For(shape);
            img.color = icon.color;
            var rt = img.rectTransform;
            rt.anchoredPosition = p;
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0f, 0f, rot);
            if (!img.enabled) img.enabled = true;
        }
        for (int i = n; i < pool.Count; i++) if (pool[i].enabled) pool[i].enabled = false;
        Shown = n;
    }

    Image Get(int i)
    {
        while (pool.Count <= i)
        {
            var go = new GameObject("MapIcon", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            pool.Add(img);
        }
        return pool[i];
    }
}

/// <summary>
/// What the maps draw for one zone (cinderreach-group-quest-map): gates,
/// waypoints, story NPCs, enemies, allies, quest places and the hero last,
/// so it sits on top. Static scene objects are looked up every half second.
/// </summary>
public static class MapScene
{
    static float stamp = -1f;
    static readonly List<ZoneGate> gates = new List<ZoneGate>();
    static readonly List<WaypointShrine> waypoints = new List<WaypointShrine>();
    static readonly List<StoryNpc> npcs = new List<StoryNpc>();
    static readonly List<EnemyHealth> enemies = new List<EnemyHealth>();

    static void Refresh()
    {
        float now = Time.realtimeSinceStartup;
        if (stamp >= 0f && now - stamp < 0.5f) return;
        stamp = now;
        gates.Clear(); gates.AddRange(Object.FindObjectsByType<ZoneGate>(FindObjectsSortMode.None));
        waypoints.Clear(); waypoints.AddRange(Object.FindObjectsByType<WaypointShrine>(FindObjectsSortMode.None));
        npcs.Clear(); npcs.AddRange(Object.FindObjectsByType<StoryNpc>(FindObjectsSortMode.None));
        enemies.Clear(); enemies.AddRange(Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None));
    }

    public static void ResetCache() => stamp = -1f;

    /// <summary>
    /// Icons for zone <paramref name="zone"/>. Quest places in other zones are
    /// drawn on the gate toward them when <paramref name="routeOtherZones"/> is set
    /// (minimap: the next step); the world map shows each place on its own zone.
    /// </summary>
    public static void Gather(int zone, Transform self, List<QuestMark> marks, IQuestWorld world,
        bool routeOtherZones, bool withEnemies, List<MapIcon> into)
    {
        into.Clear();
        Refresh();
        foreach (var g in gates)
        {
            if (g == null || !g.gameObject.activeInHierarchy || ZoneMap.IndexAt(g.transform.position) != zone) continue;
            into.Add(new MapIcon { world = g.transform.position, shape = MapIconShape.Ring,
                color = g.Locked ? MapIcons.GateLocked : MapIcons.Gate, size = 16f });
        }
        foreach (var w in waypoints)
        {
            if (w == null || !w.gameObject.activeInHierarchy || ZoneMap.IndexAt(w.transform.position) != zone) continue;
            into.Add(new MapIcon { world = w.transform.position, shape = MapIconShape.Diamond, color = MapIcons.Waypoint, size = 11f });
        }
        foreach (var n in npcs)
        {
            if (n == null || !n.isActiveAndEnabled || ZoneMap.IndexAt(n.transform.position) != zone) continue;
            if (n.npcId == StoryIds.Merle && !RunProfile.Act.ActOneComplete) continue;
            into.Add(new MapIcon { world = n.transform.position, shape = MapIconShape.Dot, color = MapIcons.Npc, size = 9f });
        }
        if (withEnemies)
            foreach (var e in enemies)
            {
                if (e == null || !e.IsAlive || !e.gameObject.activeInHierarchy) continue;
                var p = e.transform.position;
                if (ZoneMap.IndexAt(p) != zone) continue;
                bool big = e.tier != LootTier.Normal;
                into.Add(new MapIcon { world = p, shape = MapIconShape.Dot, color = MapIcons.Enemy, size = big ? 9f : 6f });
            }
        foreach (var h in HeroLocator.AllHeroes())
        {
            if (h == null || h == self || ZoneMap.IndexAt(h.position) != zone) continue;
            into.Add(new MapIcon { world = h.position, shape = MapIconShape.Dot, color = MapIcons.Ally, size = 10f });
        }
        if (marks != null)
            foreach (var m in marks)
            {
                var at = m.position;
                if (m.zone != zone)
                {
                    if (!routeOtherZones || m.kind == QuestMarkKind.Offer) continue;
                    if (!QuestDestinations.Route(world, zone, m, out at)) continue;
                }
                into.Add(QuestIcon(m, at));
            }
        if (self != null && ZoneMap.IndexAt(self.position) == zone)
            into.Add(new MapIcon { world = self.position, shape = MapIconShape.Arrow, color = MapIcons.Self, size = 18f,
                rotation = ZoneMap.IconRotation(self.eulerAngles.y) });
    }

    static MapIcon QuestIcon(QuestMark m, Vector3 at)
    {
        switch (m.kind)
        {
            case QuestMarkKind.Offer:
                return new MapIcon { world = at, shape = MapIconShape.Diamond, color = MapIcons.Offer, size = 14f, pinToEdge = true };
            case QuestMarkKind.TurnIn:
                return new MapIcon { world = at, shape = MapIconShape.Square, color = m.main ? MapIcons.QuestMain : MapIcons.QuestSide, size = 14f, pinToEdge = true };
            default:
                return new MapIcon { world = at, shape = MapIconShape.Diamond, color = m.main ? MapIcons.QuestMain : MapIcons.QuestSide, size = 15f, pinToEdge = true };
        }
    }
}
