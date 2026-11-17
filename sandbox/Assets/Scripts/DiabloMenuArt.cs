using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural blood-moor menu art (no binary assets — the repo bans them).
/// Every texture is generated once at runtime and cached: stone panels with
/// vignette + grain, bronze borders with ember-gold inner trim, rarity slot
/// frames, and simple item glyphs per base type. Headless-safe (EditMode
/// tests call these without a GPU). Blender MCP was evaluated and skipped:
/// no Blender binary on this host and UI chrome is 2D — procedural sprites
/// are the correct source, not 3D renders.
/// </summary>
public static class DiabloMenuArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static System.Random rng = new System.Random(1337);

    public static Sprite PanelBackground(int w = 512, int h = 640)
    {
        return Get($"panel_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var baseC = DiabloPalette.Panel;
            var edgeC = DiabloPalette.PanelEdge;
            var gold = DiabloPalette.TrimGold;
            float cx = w * 0.5f, cy = h * 0.5f;
            float maxD = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / cx, dy = (y - cy) / cy;
                    float grain = (float)(rng.NextDouble() - 0.5) * 0.035f;
                    float vig = 1f - 0.35f * Mathf.Sqrt(dx * dx + dy * dy) * 0.5f;
                    Color c = new Color(
                        Mathf.Clamp01((baseC.r + grain) * vig),
                        Mathf.Clamp01((baseC.g + grain) * vig),
                        Mathf.Clamp01((baseC.b + grain * 1.2f) * vig), 0.96f);
                    // Bronze outer border (10px) + ember-gold inner line (2px).
                    bool outer = x < 10 || y < 10 || x >= w - 10 || y >= h - 10;
                    bool inner = !outer && (x < 14 || y < 14 || x >= w - 14 || y >= h - 14);
                    if (outer) c = edgeC;
                    else if (inner) c = gold * 0.85f;
                    // Corner rivets.
                    bool rivet = (x - 22) * (x - 22) + (y - 22) * (y - 22) < 36
                        || (x - (w - 23)) * (x - (w - 23)) + (y - 22) * (y - 22) < 36
                        || (x - 22) * (x - 22) + (y - (h - 23)) * (y - (h - 23)) < 36
                        || (x - (w - 23)) * (x - (w - 23)) + (y - (h - 23)) * (y - (h - 23)) < 36;
                    if (rivet) c = gold;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        });
    }

    public static Sprite HeaderBanner(int w = 512, int h = 56)
    {
        return Get($"header_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = (float)y / h;
                    Color c = Color.Lerp(
                        new Color(0.16f, 0.11f, 0.07f, 1f),
                        new Color(0.30f, 0.21f, 0.11f, 1f), Mathf.Sin(t * Mathf.PI));
                    if (y < 3 || y >= h - 3) c = DiabloPalette.TrimGold * 0.9f;
                    if (y == h / 2) c *= 1.08f;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        });
    }

    public static Sprite SlotBackground(ItemRarity? rarity, int size = 96)
    {
        string key = rarity.HasValue ? $"slot_{rarity}_{size}" : $"slot_empty_{size}";
        return Get(key, () =>
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color border = rarity.HasValue ? DiabloPalette.Rarity(rarity.Value) : DiabloPalette.PanelEdge * 1.4f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float grain = (float)(rng.NextDouble() - 0.5) * 0.03f;
                    Color c = new Color(
                        DiabloPalette.SlotEmpty.r + grain,
                        DiabloPalette.SlotEmpty.g + grain,
                        DiabloPalette.SlotEmpty.b + grain, 1f);
                    int b = 5;
                    bool frame = x < b || y < b || x >= size - b || y >= size - b;
                    bool glow = !frame && (x < b + 3 || y < b + 3 || x >= size - b - 3 || y >= size - b - 3);
                    if (frame) c = border;
                    else if (glow && rarity.HasValue) c = Color.Lerp(c, border, 0.45f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        });
    }

    public static Sprite TabBackground(bool active, int w = 128, int h = 44)
    {
        return Get(active ? $"tab_on_{w}x{h}" : $"tab_off_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color fill = active ? DiabloPalette.TabActive : DiabloPalette.TabInactive;
            Color top = active ? DiabloPalette.TrimGold : DiabloPalette.PanelEdge * 1.6f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = fill;
                    if (y >= h - 4) c = top;
                    else if (x < 2 || x >= w - 2 || y < 2) c = DiabloPalette.PanelEdge;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        });
    }

    /// <summary>Simple item glyph per base type, tinted by rarity edge.</summary>
    public static Sprite ItemIcon(string baseType, ItemRarity rarity, int size = 64)
    {
        string key = $"icon_{baseType}_{rarity}_{size}";
        return Get(key, () =>
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color ink = DiabloPalette.Parchment;
            Color glow = DiabloPalette.Rarity(rarity);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    Color px = new Color(0, 0, 0, 0);
                    bool glyph = GlyphHit(baseType, dx, dy);
                    if (glyph) px = ink;
                    // Rarity edge glow near the rim.
                    if (r > 0.82f && r < 0.98f) px = Color.Lerp(px, glow, 0.85f);
                    tex.SetPixel(x, y, px);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    static bool GlyphHit(string baseType, float dx, float dy)
    {
        string b = (baseType ?? "").ToLowerInvariant();
        bool isBlade = b.Contains("sword") || b.Contains("dagger") || b.Contains("axe") || b.Contains("blade");
        bool isChest = b.Contains("tunic") || b.Contains("vest") || b.Contains("breast") || b.Contains("plate") || b.Contains("armor");
        if (isBlade)
        {
            // Diagonal blade with crossguard: |dx+dy| thin, guard near bottom.
            if (Mathf.Abs(dx + dy) < 0.09f && dx > -0.6f && dx < 0.6f) return true;
            if (Mathf.Abs(dx - dy + 0.35f) < 0.07f && Mathf.Abs(dx + 0.35f) < 0.30f) return true; // guard
            if ((dx + 0.45f) * (dx + 0.45f) + (dy + 0.45f) * (dy + 0.45f) < 0.02f) return true; // pommel
            return false;
        }
        if (isChest)
        {
            // Chestplate: torso trapezoid + shoulder bars.
            bool torso = Mathf.Abs(dx) < 0.32f - 0.10f * dy && dy > -0.5f && dy < 0.4f;
            bool shoulders = Mathf.Abs(dx) < 0.55f && dy > 0.25f && dy < 0.42f;
            return torso || shoulders;
        }
        // Helmets / circlets: dome + slit or band.
        bool dome = dx * dx + (dy + 0.1f) * (dy + 0.1f) < 0.30f && dy < 0.45f;
        bool slit = Mathf.Abs(dy) < 0.07f && Mathf.Abs(dx) < 0.42f;
        bool band = b.Contains("circlet") && Mathf.Abs(dy + 0.05f) < 0.10f && Mathf.Abs(dx) < 0.5f;
        return dome && !slit || band;
    }

    static Sprite Get(string key, System.Func<Sprite> build)
    {
        if (cache.TryGetValue(key, out var s) && s != null) return s;
        s = build();
        cache[key] = s;
        return s;
    }

    /// <summary>Test seam: cached sprite count.</summary>
    public static int TestCacheCount => cache.Count;
}
