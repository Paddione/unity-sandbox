using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared HUD building blocks (cinderreach-hud-redesign): dark crypt panel with a
/// bronze edge and optional ember-gold top line, filled bars, badges and a
/// procedural disc. Everything is raycast-free unless a caller opts in, so HUD
/// chrome never swallows world clicks.
/// </summary>
public static class HudChrome
{
    public const float GoldTopHeight = 2f;

    /// <summary>Panel with background, 1 px bronze outline and optional gold top line.</summary>
    public static RectTransform Panel(Transform parent, string name, Vector2 size, bool goldTop = false, Color? fill = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = fill ?? DiabloPalette.HudPanel;
        img.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = DiabloPalette.PanelEdge;
        outline.effectDistance = new Vector2(1f, -1f);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        if (goldTop)
        {
            var top = Img(go.transform, "GoldTop", null, DiabloPalette.TrimGold);
            var tr = top.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(0f, GoldTopHeight);
            tr.anchoredPosition = Vector2.zero;
        }
        return rt;
    }

    /// <summary>Horizontal bar: dark track plus a filled image (fillAmount drives it).</summary>
    public static RectTransform Bar(Transform parent, string name, Vector2 size, Color color, out Image fill)
    {
        var track = Img(parent, name, null, DiabloPalette.BarTrack);
        var outline = track.gameObject.AddComponent<Outline>();
        outline.effectColor = DiabloPalette.PanelEdge;
        outline.effectDistance = new Vector2(1f, -1f);
        track.rectTransform.sizeDelta = size;
        fill = Img(track.transform, name + "Fill", DiabloMenuArt.ProgressFillSprite(), color);
        fill.type = Image.Type.Filled; // needs a sprite, or fillAmount no-ops
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        Stretch(fill.rectTransform, 0f);
        return track.rectTransform;
    }

    /// <summary>Small outlined badge with uppercase display text (tier plaques, key hints).</summary>
    public static Text Badge(Transform parent, string name, string text, Color color, int size = 14)
    {
        var bg = Img(parent, name, null, new Color(0f, 0f, 0f, 0f));
        var outline = bg.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
        var t = DiabloUIFactory.MakeText(bg.transform, "Label", text, size, color, TextAnchor.MiddleCenter, shadow: false, font: DiabloUIFactory.BodyFont);
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Stretch(t.rectTransform, 0f);
        bg.rectTransform.sizeDelta = new Vector2(t.preferredWidth + 18f, size + 8f);
        return t;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static void Stretch(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    public static void Anchor(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    static Sprite white;

    /// <summary>Plain white sprite: Filled images need one or fillAmount no-ops.</summary>
    public static Sprite WhiteSprite()
    {
        if (white != null) return white;
        var tex = Texture2D.whiteTexture;
        white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return white;
    }

    static Sprite circle;

    /// <summary>Procedural anti-aliased disc (no editor-only builtin sprites at runtime).</summary>
    public static Sprite Circle()
    {
        if (circle != null) return circle;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                byte a = (byte)(Mathf.Clamp01(c - d) * 255f);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return circle;
    }
}

/// <summary>
/// Bottom HUD cluster geometry (1920×1080 reference, bottom-centre anchor), shared
/// by <see cref="DiabloLifeGlobe"/>, <see cref="SkillBarHud"/> and the XP bar in
/// <see cref="DiabloProgression"/> so the separate canvases line up as one block.
/// </summary>
public static class HudLayout
{
    public const float SlotSize = 72f;
    public const float SlotGap = 10f;
    public const int SkillSlots = 6;
    public const float PanelWidth = 604f;
    public const float PanelBottom = 20f;
    public const float PanelHeight = 178f;
    public const float XpLabelY = 32f;
    public const float XpBarY = 54f;
    public const float XpBarWidth = PanelWidth - 36f;
    public const float SlotNameY = 72f;
    public const float SlotY = 92f;
    public const float ClusterGap = 28f;
    public const float GlobeSize = 176f;
    public const float SideWidth = 176f;

    /// <summary>Width of the slot row: six skills, one gap, the flask slot.</summary>
    public const float RowWidth = (SkillSlots + 1) * SlotSize + SkillSlots * SlotGap;

    /// <summary>Centre x of the skill-only row (six slots) relative to screen centre.</summary>
    public static float SkillRowCenterX => -RowWidth * 0.5f + (SkillSlots * SlotSize + (SkillSlots - 1) * SlotGap) * 0.5f;

    /// <summary>Centre x of the flask slot (last cell of the row).</summary>
    public static float FlaskCenterX => RowWidth * 0.5f - SlotSize * 0.5f;

    /// <summary>Centre x of the life globe (left of the panel).</summary>
    public static float GlobeCenterX => -(PanelWidth * 0.5f + ClusterGap + GlobeSize * 0.5f);

    /// <summary>Centre x of the gold/menu column (right of the panel).</summary>
    public static float SideCenterX => PanelWidth * 0.5f + ClusterGap + SideWidth * 0.5f;
}
