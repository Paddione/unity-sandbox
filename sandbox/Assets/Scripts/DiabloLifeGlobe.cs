using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D2-style life globe (bottom-left): red orb drains with the local hero's
/// <see cref="PlayerHealth"/>, "cur / max" label, and a centered death
/// notice after a respawn. Display-only canvas (no GraphicRaycaster).
/// Self-spawned once per scene by the first PlayerHealth (no scene rebuild).
/// Anchors to the hero named "Player" like <see cref="DiabloTargetBar"/>.
/// </summary>
public sealed class DiabloLifeGlobe : MonoBehaviour
{
    public const float DeathNoticeSeconds = 3f;
    const float Size = 120f;

    static DiabloLifeGlobe instance;

    Canvas canvas;
    Image fill;
    Text label;
    Text deathNotice;

    /// <summary>Test seams: shown fill (-1 unbuilt) and notice visibility.</summary>
    public float TestFillAmount => fill != null ? fill.fillAmount : -1f;
    public bool TestDeathNoticeShown => deathNotice != null && deathNotice.enabled;
    public static DiabloLifeGlobe Instance => instance;

    public static void Ensure()
    {
        if (instance != null) return;
        instance = new GameObject("DLifeGlobe").AddComponent<DiabloLifeGlobe>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        BuildUI();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        var health = hero != null ? hero.GetComponent<PlayerHealth>() : null;
        canvas.enabled = health != null;
        if (health == null) return;
        float max = health.MaxHP;
        float cur = health.CurrentHP;
        fill.fillAmount = max > 0f ? cur / max : 0f;
        label.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
        deathNotice.enabled = Time.time - PlayerHealth.LastDeathTime < DeathNoticeSeconds;
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("LifeGlobeCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var disc = CircleSprite();
        var rim = MakeImage(canvasGo.transform, "GlobeRim", disc, new Color(0.08f, 0.06f, 0.05f, 0.95f));
        Place(rim.rectTransform, new Vector2(0f, 0f), new Vector2(16f, 16f), Size + 8f);

        var back = MakeImage(rim.transform, "GlobeBack", disc, new Color(0.18f, 0.03f, 0.03f, 1f));
        Center(back.rectTransform, Size);

        fill = MakeImage(rim.transform, "GlobeFill", disc, new Color(0.75f, 0.07f, 0.07f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        Center(fill.rectTransform, Size);

        label = MakeText(rim.transform, "GlobeLabel", 18);
        Center(label.rectTransform, Size);

        deathNotice = MakeText(canvasGo.transform, "DeathNotice", 30);
        deathNotice.text = "You died - back at the Rogue Encampment";
        deathNotice.color = new Color(0.95f, 0.25f, 0.2f, 1f);
        var r = deathNotice.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(800f, 60f);
        r.anchoredPosition = new Vector2(0f, 120f);
        deathNotice.enabled = false;
    }

    static void Place(RectTransform r, Vector2 anchor, Vector2 offset, float size)
    {
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.sizeDelta = new Vector2(size, size);
        r.anchoredPosition = offset;
    }

    static void Center(RectTransform r, float size) => Place(r, new Vector2(0.5f, 0.5f), Vector2.zero, size);

    static Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text MakeText(Transform parent, string name, int size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        go.AddComponent<Shadow>();
        return t;
    }

    static Sprite circle;

    /// <summary>Procedural anti-aliased disc (no editor-only builtin sprites at runtime).</summary>
    static Sprite CircleSprite()
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
