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
    Text deathSub;
    Image deathVeil;
    Text hudStatusText;
    Text fpsText;
    float fpsSmooth = 60f;

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
        if (GetComponent<DiabloLootLabels>() == null) gameObject.AddComponent<DiabloLootLabels>();
        if (GetComponent<HeroNameplates>() == null) gameObject.AddComponent<HeroNameplates>(); // accounts S1
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
        UpdateDeathVeil(Time.time - PlayerHealth.LastDeathTime);

        if (hudStatusText != null)
        {
            var w = hero.GetComponent<Wallet>();
            var inv = hero.GetComponent<Inventory>();
            var prog = hero.GetComponent<DiabloProgression>();
            int gold = w != null ? w.Gold : 0;
            int pot = 0;
            if (inv != null)
                for (int i = 0; i < inv.Count; i++)
                    if (inv.Get(i)?.slot == ItemSlot.Consumable) pot++;
            var sheet = prog != null ? prog.Sheet : null;
            int pts = sheet != null ? sheet.freeAttributePoints + sheet.freeSkillPoints : 0;
            string ptsTag = pts > 0 ? $"   <color={DiabloPalette.ToHex(DiabloPalette.GoldBright)}>+{pts} pts [C]</color>" : "";
            hudStatusText.text = $"<color={DiabloPalette.ToHex(DiabloPalette.TrimGold)}>{gold}g</color>   •   Potions [1]: {pot}{ptsTag}";
        }
        if (fpsText != null)
        {
            fpsText.enabled = DiabloSettings.ShowFPS;
            if (DiabloSettings.ShowFPS && Time.unscaledDeltaTime > 0.0001f)
            {
                fpsSmooth = Mathf.Lerp(fpsSmooth, 1f / Time.unscaledDeltaTime, 0.1f);
                fpsText.text = $"{Mathf.RoundToInt(fpsSmooth)} FPS";
            }
        }
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
        var outerTrim = MakeImage(canvasGo.transform, "GlobeGoldRim", disc, DiabloPalette.PanelEdge);
        Place(outerTrim.rectTransform, new Vector2(0f, 0f), new Vector2(18f, 16f), Size + 14f);

        var rim = MakeImage(outerTrim.transform, "GlobeRim", disc, new Color(0.06f, 0.04f, 0.05f, 0.98f));
        Center(rim.rectTransform, Size + 6f);

        var back = MakeImage(rim.transform, "GlobeBack", disc, new Color(0.16f, 0.025f, 0.03f, 1f));
        Center(back.rectTransform, Size);

        fill = MakeImage(rim.transform, "GlobeFill", disc, new Color(0.78f, 0.07f, 0.08f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        Center(fill.rectTransform, Size);

        var sheen = MakeImage(rim.transform, "GlobeSheen", disc, new Color(1f, 0.72f, 0.68f, 0.12f));
        Center(sheen.rectTransform, Size * 0.78f);

        label = MakeText(rim.transform, "GlobeLabel", 18);
        Center(label.rectTransform, Size);

        // Compact gold/potion status card next to the life globe
        var statusCard = new GameObject("HudStatusCard", typeof(RectTransform));
        statusCard.transform.SetParent(canvasGo.transform, false);
        var scImg = statusCard.AddComponent<Image>();
        scImg.sprite = DiabloMenuArt.TooltipBackground();
        scImg.type = Image.Type.Sliced;
        scImg.raycastTarget = false;
        var scr = statusCard.GetComponent<RectTransform>();
        scr.anchorMin = scr.anchorMax = scr.pivot = new Vector2(0f, 0f);
        scr.sizeDelta = new Vector2(310f, 34f);
        scr.anchoredPosition = new Vector2(158f, 22f);

        hudStatusText = MakeText(statusCard.transform, "HudStatusText", 14);
        hudStatusText.supportRichText = true;
        hudStatusText.alignment = TextAnchor.MiddleCenter;
        var hsr = hudStatusText.rectTransform;
        hsr.anchorMin = Vector2.zero;
        hsr.anchorMax = Vector2.one;
        hsr.offsetMin = new Vector2(8f, 2f);
        hsr.offsetMax = new Vector2(-8f, -2f);

        fpsText = MakeText(canvasGo.transform, "FpsOverlay", 15);
        fpsText.alignment = TextAnchor.UpperRight;
        fpsText.color = DiabloPalette.TrimGold;
        var fr = fpsText.rectTransform;
        fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(1f, 1f);
        fr.sizeDelta = new Vector2(120f, 28f);
        fr.anchoredPosition = new Vector2(-14f, -10f);
        fpsText.enabled = DiabloSettings.ShowFPS;

        deathVeil = MakeImage(canvasGo.transform, "DeathVeil", null, new Color(0.09f, 0.01f, 0.015f, 0f));
        var vr = deathVeil.rectTransform;
        vr.anchorMin = Vector2.zero;
        vr.anchorMax = Vector2.one;
        vr.offsetMin = vr.offsetMax = Vector2.zero;
        deathVeil.raycastTarget = false;
        deathVeil.enabled = false;

        deathNotice = MakeText(canvasGo.transform, "DeathNotice", 54);
        deathNotice.text = "YOU HAVE FALLEN";
        deathNotice.fontStyle = FontStyle.Bold;
        deathNotice.color = new Color(0.92f, 0.22f, 0.18f, 1f);
        var r = deathNotice.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(1000f, 80f);
        r.anchoredPosition = new Vector2(0f, 60f);
        deathNotice.enabled = false;

        deathSub = MakeText(canvasGo.transform, "DeathSub", 20);
        deathSub.text = "The lantern light carries you back to Lantern Hold.";
        var sr = deathSub.rectTransform;
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
        sr.sizeDelta = new Vector2(1000f, 36f);
        sr.anchoredPosition = new Vector2(0f, 0f);
        deathSub.enabled = false;
    }

    /// <summary>
    /// Death beat: the screen snaps to a blood-dark veil with the title, holds,
    /// then lifts over the respawn at Lantern Hold. Pure presentation; the
    /// respawn itself stays instant (<see cref="PlayerHealth.DieAndRespawn"/>).
    /// </summary>
    public static float DeathVeilAlpha(float sinceDeath)
    {
        if (sinceDeath < 0f || sinceDeath >= DeathNoticeSeconds) return 0f;
        const float hold = 1.1f;
        if (sinceDeath <= hold) return 0.88f;
        return Mathf.Lerp(0.88f, 0f, Mathf.SmoothStep(0f, 1f, (sinceDeath - hold) / (DeathNoticeSeconds - hold)));
    }

    void UpdateDeathVeil(float sinceDeath)
    {
        bool shown = sinceDeath >= 0f && sinceDeath < DeathNoticeSeconds;
        deathNotice.enabled = shown;
        if (deathSub != null) deathSub.enabled = shown;
        if (deathVeil != null) deathVeil.enabled = shown;
        if (!shown) return;
        float a = DeathVeilAlpha(sinceDeath);
        if (deathVeil != null) deathVeil.color = new Color(0.09f, 0.01f, 0.015f, a);
        float textA = Mathf.Clamp01(a / 0.88f * 1.4f);
        deathNotice.color = new Color(0.92f, 0.22f, 0.18f, textA);
        if (deathSub != null) deathSub.color = new Color(DiabloPalette.Parchment.r, DiabloPalette.Parchment.g, DiabloPalette.Parchment.b, textA * 0.85f);
        // Title settles from slightly large to rest size: a short "impact" read.
        float s = 1f + 0.12f * Mathf.Clamp01(1f - sinceDeath / 0.5f);
        deathNotice.rectTransform.localScale = new Vector3(s, s, 1f);
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
