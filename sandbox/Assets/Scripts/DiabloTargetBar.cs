using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D2-style target bar (top-center): name + HP of the last husk struck,
/// within range and inside the fade window. Display-only canvas (no
/// GraphicRaycaster, nothing blocks clicks). Solo-first: anchors to the
/// hero named "Player".
/// </summary>
public sealed class DiabloTargetBar : MonoBehaviour
{
    public const float MaxRange = 12f;
    public const float FadeSeconds = 3f;

    Canvas canvas;
    Text nameText;
    Image fill;
    RectTransform targetPanel;
    CanvasGroup targetGroup;

    // Boss bar: wide, above the target bar, while a living boss is near.
    public const float BossRange = 24f;
    const float BossScanInterval = 0.5f;
    GameObject bossPanel;
    Text bossNameText;
    Text bossPhaseText;
    Image bossFill;
    EnemyHealth trackedBoss;
    float nextBossScan;

    /// <summary>Boss currently shown on the boss bar (null when hidden). Test seam.</summary>
    public EnemyHealth ShownBoss => bossPanel != null && bossPanel.activeSelf ? trackedBoss : null;

    void Awake()
    {
        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        var target = DiabloMelee.LastStruck;
        var hero = GameObject.Find("Player");
        bool bossShown = UpdateBossBar(hero);
        if (target != null && trackedBoss != null && bossShown && target == trackedBoss.gameObject)
            target = null; // the boss bar already shows it
        if (target == null || hero == null)
        {
            SetVisible(false);
            return;
        }
        var health = target.GetComponent<EnemyHealth>();
        if (health == null)
        {
            SetVisible(false);
            return;
        }
        var d = target.transform.position - hero.transform.position;
        d.y = 0f;
        if (d.magnitude > MaxRange || Time.time - DiabloMelee.LastStruckTime > FadeSeconds)
        {
            SetVisible(false);
            return;
        }
        SetVisible(true);
        nameText.text = DisplayName(target.name);
        nameText.color = TierColor(health.tier);
        fill.fillAmount = health.HPFraction();
    }

    /// <summary>
    /// Player-facing enemy name from the scene object name: drops builder
    /// indices ("Ash Lancer_0", "Kiln Summon_1_2") and maps internal names.
    /// </summary>
    public static string DisplayName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return "";
        string n = objectName.Replace("(Clone)", "").Trim();
        n = System.Text.RegularExpressions.Regex.Replace(n, @"(_\d+)+$", "");
        switch (n)
        {
            case "Kiln Summon": return "Cinder Thrall";
            case "Hollow Abbot": return "The Hollow Abbot";
            case "Kilnmother": return "The Kilnmother";
            default: return n.Replace('_', ' ');
        }
    }

    /// <summary>Name tint: elites blue-steel, bosses gold, the rest parchment.</summary>
    public static Color TierColor(LootTier tier)
    {
        switch (tier)
        {
            case LootTier.Elite: return DiabloPalette.FenSteel;
            case LootTier.Boss:
            case LootTier.ActBoss: return DiabloPalette.GoldBright;
            default: return DiabloPalette.Parchment;
        }
    }

    bool targetVisible;

    void SetVisible(bool visible)
    {
        targetVisible = visible;
        // Alpha, not SetActive: the panel stays findable while hidden (scene lookups by name).
        if (targetGroup != null) targetGroup.alpha = visible ? 1f : 0f;
        RefreshCanvas();
    }

    void RefreshCanvas()
    {
        if (canvas != null) canvas.enabled = targetVisible || (bossPanel != null && bossPanel.activeSelf);
    }

    /// <summary>Boss tier enemies get the wide bar.</summary>
    public static bool IsBossTier(LootTier tier) => tier == LootTier.Boss || tier == LootTier.ActBoss;

    /// <summary>Phase label for the boss bar (both boss controllers split at 50 %).</summary>
    public static string PhaseLabel(EnemyHealth boss)
    {
        if (boss == null) return "";
        var abbot = boss.GetComponent<BossController>();
        if (abbot != null) return abbot.Phase.Value >= 2 ? "PHASE II" : "PHASE I";
        var kiln = boss.GetComponent<KilnmotherController>();
        if (kiln != null) return kiln.PhaseTwo ? "PHASE II" : "PHASE I";
        var glass = boss.GetComponent<GlasswrightController>();
        if (glass != null) return glass.PhaseTwo ? "PHASE II" : "PHASE I";
        return "";
    }

    bool UpdateBossBar(GameObject hero)
    {
        if (bossPanel == null) return false;
        if (hero == null) { ShowBoss(false); return false; }
        bool alive = trackedBoss != null && trackedBoss.isActiveAndEnabled && trackedBoss.HPFraction() > 0f;
        if (!alive) trackedBoss = null;
        if (Time.unscaledTime >= nextBossScan) // scene scan at 2 Hz, never per frame
        {
            nextBossScan = Time.unscaledTime + BossScanInterval;
            trackedBoss = null;
            float best = BossRange;
            foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (e == null || !IsBossTier(e.tier) || !e.isActiveAndEnabled || e.HPFraction() <= 0f) continue;
                var d = e.transform.position - hero.transform.position;
                d.y = 0f;
                if (d.magnitude <= best) { best = d.magnitude; trackedBoss = e; }
            }
        }
        if (trackedBoss == null) { ShowBoss(false); return false; }
        ShowBoss(true);
        bossNameText.text = DisplayName(trackedBoss.name).ToUpperInvariant();
        bossPhaseText.text = PhaseLabel(trackedBoss);
        bossFill.fillAmount = trackedBoss.HPFraction();
        return true;
    }

    void ShowBoss(bool on)
    {
        if (bossPanel == null) return;
        if (bossPanel.activeSelf != on) bossPanel.SetActive(on);
        if (targetPanel != null) targetPanel.anchoredPosition = new Vector2(0f, on ? -84f : -12f);
        RefreshCanvas();
    }

    /// <summary>Test seam (strikeCount precedent): shown fill, -1 when unbuilt.</summary>
    public float TestFillAmount => fill != null ? fill.fillAmount : -1f;

    void BuildUI()
    {
        var canvasGo = new GameObject("TargetBarCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var white = WhiteSprite();
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panel = panelGo.AddComponent<Image>();
        panel.sprite = DiabloMenuArt.TooltipBackground();
        panel.type = Image.Type.Sliced;
        panel.color = Color.white;
        panel.raycastTarget = false;
        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(360f, 54f);
        rect.anchoredPosition = new Vector2(0f, -12f);
        targetPanel = rect;
        targetGroup = panelGo.AddComponent<CanvasGroup>();
        targetGroup.blocksRaycasts = false;

        nameText = MakeLabel(panelGo.transform, "", 16);
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = DiabloPalette.Parchment;
        nameText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(panelGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.04f, 0.04f, 1f);
        bg.raycastTarget = false;
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.sizeDelta = new Vector2(332f, 14f);
        bgRect.anchoredPosition = new Vector2(0f, 8f);
        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        fill = fillGo.AddComponent<Image>();
        fill.sprite = DiabloMenuArt.ProgressFillSprite();
        fill.color = new Color(0.84f, 0.14f, 0.14f, 1f);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        BuildBossBar(canvasGo.transform);
    }

    void BuildBossBar(Transform parent)
    {
        bossPanel = new GameObject("BossPanel");
        bossPanel.transform.SetParent(parent, false);
        var img = bossPanel.AddComponent<Image>();
        img.sprite = DiabloMenuArt.HeaderBanner();
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        var r = bossPanel.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(760f, 64f);
        r.anchoredPosition = new Vector2(0f, -10f);

        bossNameText = MakeLabel(bossPanel.transform, "", 20);
        bossNameText.name = "BossName";
        bossNameText.fontStyle = FontStyle.Bold;
        bossNameText.alignment = TextAnchor.UpperCenter;
        bossNameText.color = DiabloPalette.GoldBright;
        bossNameText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        var nr = bossNameText.rectTransform;
        nr.anchorMin = new Vector2(0f, 1f); nr.anchorMax = new Vector2(1f, 1f); nr.pivot = new Vector2(0.5f, 1f);
        nr.sizeDelta = new Vector2(-40f, 28f); nr.anchoredPosition = new Vector2(0f, -6f);

        bossPhaseText = MakeLabel(bossPanel.transform, "", 13);
        bossPhaseText.name = "BossPhase";
        bossPhaseText.alignment = TextAnchor.UpperRight;
        bossPhaseText.color = DiabloPalette.ParchmentDim;
        var pr = bossPhaseText.rectTransform;
        pr.anchorMin = new Vector2(0f, 1f); pr.anchorMax = new Vector2(1f, 1f); pr.pivot = new Vector2(0.5f, 1f);
        pr.sizeDelta = new Vector2(-36f, 22f); pr.anchoredPosition = new Vector2(0f, -9f);

        var bg = new GameObject("BossBarBg");
        bg.transform.SetParent(bossPanel.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.16f, 0.04f, 0.04f, 1f);
        bgImg.raycastTarget = false;
        var br = bg.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0f);
        br.sizeDelta = new Vector2(700f, 16f);
        br.anchoredPosition = new Vector2(0f, 10f);
        var f = new GameObject("BossBarFill");
        f.transform.SetParent(bg.transform, false);
        bossFill = f.AddComponent<Image>();
        bossFill.sprite = DiabloMenuArt.ProgressFillSprite();
        bossFill.color = new Color(0.92f, 0.36f, 0.10f, 1f); // ember: reads apart from the red target bar
        bossFill.type = Image.Type.Filled;
        bossFill.fillMethod = Image.FillMethod.Horizontal;
        bossFill.raycastTarget = false;
        var fr = f.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(1f, 1f); fr.offsetMax = new Vector2(-1f, -1f);
        bossPanel.SetActive(false);
    }

    static Sprite whiteSprite;

    /// <summary>Plain white sprite: FillMethod needs one or fillAmount no-ops.</summary>
    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    Text MakeLabel(Transform parent, string text, int size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.color = Color.white;
        label.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(8f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);
        return label;
    }
}
