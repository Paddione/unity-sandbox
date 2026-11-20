using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cinderreach dark-fantasy UI art system. Combines 9-sliced procedural gothic
/// chrome (beveled crypt slate panels, bronze trim with gold rivets, rarity
/// slots, buttons, dividers, tooltips, slider tracks/knobs, progress bars)
/// with 3D Blender-rendered sprites loaded from Resources/MenuArt.
/// Headless-safe (EditMode & automated PlayMode tests call these cleanly).
/// </summary>
public static class DiabloMenuArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static readonly System.Random rng = new System.Random(1337);

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
            int borderOuter = 8;
            int borderInner = 12;
            int rivetOffset = 20;
            int rivetRadiusSq = 30;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / Mathf.Max(1f, cx), dy = (y - cy) / Mathf.Max(1f, cy);
                    float edgeDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    float grain = (float)(rng.NextDouble() - 0.5) * 0.032f;
                    float vig = 1f - 0.28f * Mathf.Sqrt(dx * dx + dy * dy) * 0.55f;
                    // Subtle vertical stone sheen darker at bottom, slightly warmer up top
                    float grad = Mathf.Lerp(0.88f, 1.08f, (float)y / Mathf.Max(1, h));
                    Color c = new Color(
                        Mathf.Clamp01((baseC.r + grain) * vig * grad),
                        Mathf.Clamp01((baseC.g + grain) * vig * grad),
                        Mathf.Clamp01((baseC.b + grain * 1.1f) * vig * grad), 0.97f);
                    bool outer = edgeDist < borderOuter;
                    bool inner = !outer && edgeDist < borderInner;
                    bool innerShadow = !outer && !inner && edgeDist < borderInner + 5;
                    if (outer)
                    {
                        float bevel = (edgeDist < 2 || edgeDist >= borderOuter - 2) ? 0.72f : 1.05f;
                        c = new Color(edgeC.r * bevel, edgeC.g * bevel, edgeC.b * bevel, 1f);
                    }
                    else if (inner)
                    {
                        c = edgeDist == borderOuter + 1 ? gold * 0.95f : gold * 0.58f;
                    }
                    else if (innerShadow)
                    {
                        c *= 0.65f;
                        c.a = 0.98f;
                    }
                    bool rivet =
                        DistSq(x, y, rivetOffset, rivetOffset) < rivetRadiusSq ||
                        DistSq(x, y, w - 1 - rivetOffset, rivetOffset) < rivetRadiusSq ||
                        DistSq(x, y, rivetOffset, h - 1 - rivetOffset) < rivetRadiusSq ||
                        DistSq(x, y, w - 1 - rivetOffset, h - 1 - rivetOffset) < rivetRadiusSq;
                    if (rivet)
                    {
                        float rMin = Mathf.Min(
                            Mathf.Min(DistSq(x, y, rivetOffset, rivetOffset), DistSq(x, y, w - 1 - rivetOffset, rivetOffset)),
                            Mathf.Min(DistSq(x, y, rivetOffset, h - 1 - rivetOffset), DistSq(x, y, w - 1 - rivetOffset, h - 1 - rivetOffset)));
                        c = rMin < 9 ? gold : edgeC * 0.8f;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            float b = Mathf.Min(34f, Mathf.Min(w, h) * 0.3f);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(b, b, b, b));
        });
    }

    public static Sprite HeaderBanner(int w = 512, int h = 56)
    {
        return Get($"header_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / Mathf.Max(1, h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / Mathf.Max(1, w - 1);
                    float edgeFade = Mathf.Sin(t * Mathf.PI);
                    float centerGlow = 0.72f + 0.35f * Mathf.Sin(u * Mathf.PI);
                    Color c = Color.Lerp(
                        new Color(0.14f, 0.09f, 0.06f, 1f),
                        new Color(0.32f, 0.21f, 0.11f, 1f), edgeFade) * centerGlow;
                    c.a = 1f;
                    if (y < 2 || y >= h - 2) c = DiabloPalette.PanelEdge * 0.7f;
                    else if (y < 4 || y >= h - 4) c = DiabloPalette.TrimGold * 0.92f;
                    else if (x < 4 || x >= w - 4) c = DiabloPalette.TrimGold * 0.80f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            float bx = Mathf.Min(18f, w * 0.25f);
            float by = Mathf.Min(10f, h * 0.25f);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(bx, by, bx, by));
        });
    }

    public static Sprite SlotBackground(ItemRarity? rarity, int size = 96)
    {
        string key = rarity.HasValue ? $"slot_{rarity}_{size}" : $"slot_empty_{size}";
        return Get(key, () =>
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color border = rarity.HasValue ? DiabloPalette.Rarity(rarity.Value) : DiabloPalette.PanelEdge * 1.45f;
            int b = 4;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    float dx = (x - size * 0.5f) / (size * 0.5f);
                    float dy = (y - size * 0.5f) / (size * 0.5f);
                    float radial = Mathf.Clamp01(1f - 0.35f * Mathf.Sqrt(dx * dx + dy * dy));
                    float grain = (float)(rng.NextDouble() - 0.5) * 0.025f;
                    Color c = new Color(
                        Mathf.Clamp01((DiabloPalette.SlotEmpty.r + grain) * (0.85f + 0.25f * radial)),
                        Mathf.Clamp01((DiabloPalette.SlotEmpty.g + grain) * (0.85f + 0.25f * radial)),
                        Mathf.Clamp01((DiabloPalette.SlotEmpty.b + grain) * (0.85f + 0.25f * radial)), 1f);
                    if (rarity.HasValue)
                        c = Color.Lerp(c, border * 0.32f, (1f - radial) * 0.45f);
                    if (edge < 1) c = DiabloPalette.Abyss;
                    else if (edge < b) c = border;
                    else if (edge < b + 4 && rarity.HasValue) c = Color.Lerp(c, border, 0.50f);
                    else if (edge == b) c *= 0.65f;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            float sb = Mathf.Min(12f, size * 0.25f);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(sb, sb, sb, sb));
        });
    }

    public static Sprite TabBackground(bool active, int w = 128, int h = 44)
    {
        return Get(active ? $"tab_on_{w}x{h}" : $"tab_off_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color fill = active ? DiabloPalette.TabActive : DiabloPalette.TabInactive;
            Color top = active ? DiabloPalette.TrimGold : DiabloPalette.PanelEdge * 1.5f;
            for (int y = 0; y < h; y++)
            {
                float grad = Mathf.Lerp(0.84f, 1.12f, (float)y / Mathf.Max(1, h));
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(fill.r * grad, fill.g * grad, fill.b * grad, 1f);
                    if (y >= h - 4) c = top;
                    else if (x < 2 || x >= w - 2 || y < 2) c = DiabloPalette.PanelEdge;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            float tb = Mathf.Min(8f, Mathf.Min(w, h) * 0.25f);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(tb, tb, tb, tb));
        });
    }

    public static Sprite ButtonBackground(bool primary = false, int w = 220, int h = 48)
    {
        return Get($"btn_{(primary ? "pri" : "sec")}_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color baseCol = primary ? DiabloPalette.ButtonPrimary : DiabloPalette.ButtonSecondary;
            Color trim = primary ? DiabloPalette.GoldBright : DiabloPalette.PanelEdge * 1.85f;
            for (int y = 0; y < h; y++)
            {
                float grad = Mathf.Lerp(0.75f, 1.18f, (float)y / Mathf.Max(1, h - 1));
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c = new Color(baseCol.r * grad, baseCol.g * grad, baseCol.b * grad, 1f);
                    if (edge < 1) c = DiabloPalette.Abyss;
                    else if (edge < 3) c = trim;
                    else if (edge == 3) c *= 0.72f;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            float bb = Mathf.Min(10f, Mathf.Min(w, h) * 0.25f);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(bb, bb, bb, bb));
        });
    }

    public static Sprite TooltipBackground(int w = 256, int h = 160)
    {
        return Get($"tooltip_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color bg = DiabloPalette.TooltipBg;
            Color trim = DiabloPalette.TrimGold * 0.78f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c = bg;
                    if (edge == 0) c = DiabloPalette.Abyss;
                    else if (edge <= 2) c = trim;
                    else if (edge <= 4) c = DiabloPalette.PanelEdge;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(10, 10, 10, 10));
        });
    }

    public static Sprite DividerLine(int w = 256, int h = 6)
    {
        return Get($"divider_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                float vy = Mathf.Sin((float)y / Mathf.Max(1, h - 1) * Mathf.PI);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / Mathf.Max(1, w - 1);
                    float taper = Mathf.Sin(u * Mathf.PI);
                    Color c = Color.Lerp(DiabloPalette.PanelEdge, DiabloPalette.TrimGold, taper * 0.7f);
                    c.a = taper * vy;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    public static Sprite SliderTrackSprite(int w = 64, int h = 14)
    {
        return Get($"slider_track_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c = edge <= 1 ? DiabloPalette.PanelEdge : new Color(0.06f, 0.05f, 0.08f, 1f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(4, 4, 4, 4));
        });
    }

    public static Sprite SliderFillSprite(int w = 64, int h = 14)
    {
        return Get($"slider_fill_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                float v = 0.72f + 0.35f * Mathf.Sin((float)y / Mathf.Max(1, h - 1) * Mathf.PI);
                for (int x = 0; x < w; x++)
                {
                    Color c = DiabloPalette.TrimGold * v;
                    if (y == 0 || y == h - 1) c = DiabloPalette.PanelEdge;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(4, 4, 4, 4));
        });
    }

    public static Sprite SliderKnobSprite(int size = 24)
    {
        return Get($"slider_knob_{size}", () =>
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c0 = (size - 1) * 0.5f;
            float rMax = size * 0.45f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c0, dy = y - c0;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color px = new Color(0, 0, 0, 0);
                    if (d <= rMax)
                    {
                        float t = d / rMax;
                        px = t > 0.78f ? DiabloPalette.PanelEdge : Color.Lerp(DiabloPalette.GoldBright, DiabloPalette.TrimGold * 0.8f, t);
                        px.a = 1f;
                    }
                    tex.SetPixel(x, y, px);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    public static Sprite ProgressFillSprite(int w = 64, int h = 16)
    {
        return Get($"progress_fill_{w}x{h}", () =>
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                float highlight = 0.80f + 0.30f * Mathf.Sin((float)y / Mathf.Max(1, h - 1) * Mathf.PI);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color(highlight, highlight, highlight, 1f));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    /// <summary>Loads a Blender-baked sprite from Resources/MenuArt (null only in isolated headless without assets).</summary>
    public static Sprite MenuArtSprite(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return null;
        return Get($"menuart_{resourceName}", () => Resources.Load<Sprite>("MenuArt/" + resourceName));
    }

    public static Sprite ClassPortrait(string classId)
    {
        if (classId == ClassCatalog.LanternbearerId) return MenuArtSprite("PortraitLanternbearer");
        if (classId == ClassCatalog.RogueId) return MenuArtSprite("PortraitRogue");
        return MenuArtSprite("PortraitFenwalker");
    }

    public static Sprite SkillIcon(DiabloSkills.Skill skill)
    {
        string res;
        switch (skill)
        {
            case DiabloSkills.Skill.Fireball: res = "SkillFireball"; break;
            case DiabloSkills.Skill.Arrow: res = "SkillArrow"; break;
            case DiabloSkills.Skill.Cleave: res = "SkillCleave"; break;
            case DiabloSkills.Skill.LanternBolt: res = "SkillLanternBolt"; break;
            case DiabloSkills.Skill.Wisps: res = "SkillWisps"; break;
            case DiabloSkills.Skill.SearingHalo: res = "SkillSearingHalo"; break;
            case DiabloSkills.Skill.VeilStep: res = "SkillVeilStep"; break;
            case DiabloSkills.Skill.SeismicSlam: res = "SkillSeismicSlam"; break;
            case DiabloSkills.Skill.QuickShot: res = "SkillQuickShot"; break;
            case DiabloSkills.Skill.BladeFlurry: res = "SkillBladeFlurry"; break;
            case DiabloSkills.Skill.MendingLight: res = "SkillMendingLight"; break;
            case DiabloSkills.Skill.RenewingCircle: res = "SkillRenewingCircle"; break;
            default: return null;
        }
        return MenuArtSprite(res);
    }

    /// <summary>
    /// S5 art key: explicit map entry for every deliverable base and every
    /// matrix/legacy unique ID. Never "unknown" for shippable items — the
    /// fallback is reserved for null/empty inputs (test seam).
    /// </summary>
    public static string ArtKeyFor(ItemInstance item)
    {
        if (item == null) return "unknown";
        if (item.rarity == ItemRarity.Unique && !string.IsNullOrEmpty(item.uniqueId)
            && ItemDatabase.IsKnownUniqueId(item.uniqueId))
            return "unique:" + item.uniqueId;
        string b = (item.baseType ?? "").Trim();
        if (b.Length == 0) return "unknown";
        foreach (var known in AllArtBases)
            if (string.Equals(b, known, System.StringComparison.OrdinalIgnoreCase))
                return "base:" + known;
        // Display-name variants (e.g. "Worn Short Sword") resolve by keyword.
        string lower = b.ToLowerInvariant();
        foreach (var known in AllArtBases)
            if (lower.Contains(known.ToLowerInvariant())) return "base:" + known;
        if (lower.Contains("potion")) return "base:Healing Potion";
        return "base:" + b; // novel base: still explicit, drawn by family glyph
    }

    /// <summary>Every shippable base type with an explicit art entry.</summary>
    public static readonly string[] AllArtBases =
    {
        "ShortSword", "HeavyAxe", "Dagger",
        "Lantern", "HoodedLantern", "EmberCenser",
        "RogueBlade", "ShortBow", "HuntingBow",
        "Worn Short Sword", "Worn Lantern", "Worn Short Bow",
        "ClothTunic", "LeatherVest", "Breastplate",
        "Hood", "Helm", "Circlet", "Healing Potion",
    };

    /// <summary>S5 overload: icon for a concrete item (base + unique art).</summary>
    public static Sprite ItemIcon(ItemInstance item, int size = 64)
    {
        if (item == null) return ItemIcon("", ItemRarity.Magic, size);
        string key = $"icon_{ArtKeyFor(item)}_{item.rarity}_{size}";
        return Get(key, () =>
        {
            string iconName = ResolveIconName(item.rarity == ItemRarity.Unique && !string.IsNullOrEmpty(item.uniqueId)
                ? UniqueBaseFor(item.uniqueId) ?? item.baseType : item.baseType);
            if (!string.IsNullOrEmpty(iconName))
            {
                var baked = Resources.Load<Sprite>("MenuArt/" + iconName);
                if (baked != null) return baked;
            }
            return BuildGlyphSprite(item.baseType, item.uniqueId, item.rarity, size);
        });
    }

    static string UniqueBaseFor(string uniqueId)
    {
        try
        {
            var t = ItemDatabase.CreateUnique(uniqueId);
            return t != null ? t.baseType : null;
        }
        catch { return null; }
    }

    static Sprite BuildGlyphSprite(string baseType, string uniqueId, ItemRarity rarity, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color ink = DiabloPalette.Parchment;
        // Unique items draw in warm ember ink so the silhouette itself reads
        // "unique" even before the orange rim.
        if (rarity == ItemRarity.Unique) ink = new Color(1f, 0.82f, 0.55f, 1f);
        Color glow = DiabloPalette.Rarity(rarity);
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c, dy = (y - c) / c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                Color px = new Color(0, 0, 0, 0);
                if (GlyphHitEx(baseType, uniqueId, dx, dy)) px = ink;
                if (r > 0.82f && r < 0.98f) px = Color.Lerp(px, glow, 0.85f);
                tex.SetPixel(x, y, px);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Extended glyph dispatch: baked-family blades/chests/helms plus bow arcs
    /// and lantern bodies so every deliverable base has a distinct silhouette.
    /// </summary>
    static bool GlyphHitEx(string baseType, string uniqueId, float dx, float dy)
    {
        string b = ((baseType ?? "") + " " + (uniqueId ?? "")).ToLowerInvariant();
        bool isBow = b.Contains("bow");
        bool isLantern = b.Contains("lantern") || b.Contains("censer");
        if (isBow)
        {
            // Bow arc: ring segment on the right + horizontal string + arrow.
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            bool arc = r > 0.42f && r < 0.55f && dx > -0.1f;
            bool str = Mathf.Abs(dx + 0.05f) < 0.05f && dy > -0.5f && dy < 0.5f;
            bool arrow = Mathf.Abs(dy) < 0.05f && dx > -0.6f && dx < 0.6f;
            return arc || str || arrow;
        }
        if (isLantern)
        {
            // Lantern: body rectangle + cap + handle arch.
            bool body = Mathf.Abs(dx) < 0.28f && dy > -0.45f && dy < 0.25f;
            bool cap = Mathf.Abs(dx) < 0.34f && dy >= 0.25f && dy < 0.36f;
            bool handle = (dx * dx + (dy - 0.1f) * (dy - 0.1f) > 0.16f)
                && (dx * dx + (dy - 0.1f) * (dy - 0.1f) < 0.25f) && dy > 0.3f;
            bool glowDot = dx * dx + dy * dy < 0.03f;
            return body || cap || handle || glowDot;
        }
        return GlyphHit(baseType, dx, dy);
    }

    /// <summary>
    /// Blender-rendered icon when a baked sprite exists
    /// (Assets/Resources/MenuArt), else the procedural glyph. Rarity tints
    /// the slot frame, never the icon, so one bake serves all rarities.
    /// </summary>
    public static Sprite ItemIcon(string baseType, ItemRarity rarity, int size = 64)
    {
        string key = $"icon_{baseType}_{rarity}_{size}";
        return Get(key, () =>
        {
            string iconName = ResolveIconName(baseType);
            if (!string.IsNullOrEmpty(iconName))
            {
                var baked = Resources.Load<Sprite>("MenuArt/" + iconName);
                if (baked != null) return baked;
            }
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
                    if (r > 0.82f && r < 0.98f) px = Color.Lerp(px, glow, 0.85f);
                    tex.SetPixel(x, y, px);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    /// <summary>
    /// Map a base type (or display name like "Worn Short Sword", "Worn Lantern")
    /// to a baked icon file. Exact match first, then ordered keyword fallback
    /// (checking compound names like HoodedLantern before generic Hood/Lantern).
    /// </summary>
    public static string ResolveIconName(string baseType)
    {
        if (string.IsNullOrEmpty(baseType)) return "";
        string[] baked =
        {
            "ShortSword", "HeavyAxe", "Dagger",
            "ClothTunic", "LeatherVest", "Breastplate",
            "Hood", "Helm", "Circlet",
            "Lantern", "HoodedLantern", "EmberCenser",
            "HealingPotion", "GoldCoin"
        };
        foreach (var b in baked)
            if (string.Equals(baseType, b, System.StringComparison.OrdinalIgnoreCase)) return b;
        string lower = baseType.ToLowerInvariant();
        if (lower.Contains("hooded") && lower.Contains("lantern")) return "HoodedLantern";
        if (lower.Contains("censer")) return "EmberCenser";
        if (lower.Contains("lantern")) return "Lantern";
        if (lower.Contains("potion")) return "HealingPotion";
        if (lower.Contains("coin") || lower.Contains("gold")) return "GoldCoin";
        if (lower.Contains("sword") || lower.Contains("blade")) return "ShortSword";
        if (lower.Contains("axe")) return "HeavyAxe";
        if (lower.Contains("dagger") || lower.Contains("knife")) return "Dagger";
        if (lower.Contains("tunic")) return "ClothTunic";
        if (lower.Contains("vest")) return "LeatherVest";
        if (lower.Contains("breast") || lower.Contains("plate") || lower.Contains("armor") || lower.Contains("armour")) return "Breastplate";
        if (lower.Contains("hood")) return "Hood";
        if (lower.Contains("helm") || lower.Contains("helmet")) return "Helm";
        if (lower.Contains("circlet") || lower.Contains("crown")) return "Circlet";
        if (lower.Contains("lantern") || lower.Contains("censer")) return "HoodedLantern";
        if (lower.Contains("bow")) return "ShortBow";
        if (lower.Contains("rogueblade") || lower.Contains("rogue blade")) return "Dagger";
        if (lower.Contains("potion")) return "HealingPotion";
        return "";
    }

    static int DistSq(int x0, int y0, int x1, int y1) => (x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1);

    static bool GlyphHit(string baseType, float dx, float dy)
    {
        string b = (baseType ?? "").ToLowerInvariant();
        bool isBlade = b.Contains("sword") || b.Contains("dagger") || b.Contains("axe") || b.Contains("blade");
        bool isChest = b.Contains("tunic") || b.Contains("vest") || b.Contains("breast") || b.Contains("plate") || b.Contains("armor");
        if (isBlade)
        {
            if (Mathf.Abs(dx + dy) < 0.09f && dx > -0.6f && dx < 0.6f) return true;
            if (Mathf.Abs(dx - dy + 0.35f) < 0.07f && Mathf.Abs(dx + 0.35f) < 0.30f) return true;
            if ((dx + 0.45f) * (dx + 0.45f) + (dy + 0.45f) * (dy + 0.45f) < 0.02f) return true;
            return false;
        }
        if (isChest)
        {
            bool torso = Mathf.Abs(dx) < 0.32f - 0.10f * dy && dy > -0.5f && dy < 0.4f;
            bool shoulders = Mathf.Abs(dx) < 0.55f && dy > 0.25f && dy < 0.42f;
            return torso || shoulders;
        }
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
