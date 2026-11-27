using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D2-style life globe (bottom cluster, left of the skill bar): red orb drains with
/// the local hero's <see cref="PlayerHealth"/>, "cur / max" label, a shield chip,
/// and a centered death notice after a respawn. Also owns the flask slot (drawn as
/// the last cell of the skill row, <see cref="HudLayout.FlaskCenterX"/>) and the
/// gold/menu column right of the skill bar (cinderreach-hud-redesign).
/// Self-spawned once per scene by the first PlayerHealth (no scene rebuild).
/// Anchors to the hero named "Player" like <see cref="DiabloTargetBar"/>.
/// </summary>
public sealed class DiabloLifeGlobe : MonoBehaviour
{
    public const float DeathNoticeSeconds = 3f;
    const float Size = HudLayout.GlobeSize - 10f;

    static DiabloLifeGlobe instance;

    Canvas canvas;
    Image fill;
    Text label;
    Text deathNotice;
    Text deathSub;
    Image deathVeil;
    Text hudStatusText;
    Text goldText;
    GameObject shieldChip;
    Text shieldText;
    Image flaskIcon;
    Image flaskSweep;
    Text flaskText;
    Text fpsText;
    float fpsSmooth = 60f;

    /// <summary>Test seams: shown fill (-1 unbuilt) and notice visibility.</summary>
    public float TestFillAmount => fill != null ? fill.fillAmount : -1f;
    public bool TestDeathNoticeShown => deathNotice != null && deathNotice.enabled;
    /// <summary>Flask HUD seams (gear-slots S2): cooldown sweep fill and the caption.</summary>
    public float TestFlaskSweep => flaskSweep != null ? flaskSweep.fillAmount : -1f;
    public string TestFlaskText => flaskText != null ? flaskText.text : null;
    /// <summary>HUD redesign seams: shield chip visibility and the gold amount text.</summary>
    public bool TestShieldChipShown => shieldChip != null && shieldChip.activeSelf;
    public string TestGoldText => goldText != null ? goldText.text : null;
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
        if (GetComponent<PartyFramesHud>() == null) gameObject.AddComponent<PartyFramesHud>(); // hud-redesign P4
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

        float shield = health.ShieldAmount;
        if (shieldChip != null)
        {
            shieldChip.SetActive(shield > 0f);
            if (shield > 0f) shieldText.text = Loc.F("hud.shield.chip", Mathf.CeilToInt(shield), Mathf.CeilToInt(health.ShieldSecondsLeft));
        }

        if (hudStatusText != null)
        {
            var w = hero.GetComponent<Wallet>();
            var prog = hero.GetComponent<DiabloProgression>();
            int gold = w != null ? w.Gold : 0;
            var flask = HealingFlask.For(hero);
            PaintFlask(flask);
            goldText.text = FormatGold(gold);
            var sheet = prog != null ? prog.Sheet : null;
            int pts = sheet != null ? sheet.freeAttributePoints + sheet.freeSkillPoints : 0;
            hudStatusText.text = pts > 0 ? Loc.F("hud.globe.points", DiabloPalette.ToHex(DiabloPalette.GoldBright), pts).Trim() : "";
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

    /// <summary>Gold with a thousands dot ("1.284"), as on the canvas.</summary>
    public static string FormatGold(int gold) =>
        gold.ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

    void BuildUI()
    {
        var canvasGo = new GameObject("LifeGlobeCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7; // above the skill plate (5) and XP bar (6): flask sits in the row
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>(); // menu buttons only; everything else is raycast-free

        BuildGlobe(canvasGo.transform);
        BuildFlask(canvasGo.transform);
        BuildSideColumn(canvasGo.transform);

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
        deathNotice.font = DiabloUIFactory.DisplayFont;
        deathNotice.text = Loc.T("hud.death.title");
        deathNotice.fontStyle = FontStyle.Bold;
        deathNotice.color = new Color(0.92f, 0.22f, 0.18f, 1f);
        var r = deathNotice.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(1000f, 80f);
        r.anchoredPosition = new Vector2(0f, 60f);
        deathNotice.enabled = false;

        deathSub = MakeText(canvasGo.transform, "DeathSub", 20);
        deathSub.text = Loc.T("hud.death.sub");
        var sr = deathSub.rectTransform;
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
        sr.sizeDelta = new Vector2(1000f, 36f);
        sr.anchoredPosition = new Vector2(0f, 0f);
        deathSub.enabled = false;
    }

    void BuildGlobe(Transform parent)
    {
        var disc = HudChrome.Circle();
        var bottomCenter = new Vector2(0.5f, 0f);
        // Gold ring → bronze rim → dark well → red liquid → glass sheen.
        var outerTrim = MakeImage(parent, "GlobeGoldRim", disc, DiabloPalette.TrimGold);
        HudChrome.Anchor(outerTrim.rectTransform, bottomCenter,
            new Vector2(HudLayout.GlobeCenterX, HudLayout.PanelBottom), new Vector2(HudLayout.GlobeSize, HudLayout.GlobeSize));
        outerTrim.rectTransform.pivot = new Vector2(0.5f, 0f);

        var rim = MakeImage(outerTrim.transform, "GlobeRim", disc, DiabloPalette.PanelEdge);
        Center(rim.rectTransform, HudLayout.GlobeSize - 4f);

        var back = MakeImage(rim.transform, "GlobeBack", disc, DiabloPalette.Abyss);
        Center(back.rectTransform, Size);

        fill = MakeImage(rim.transform, "GlobeFill", disc, DiabloPalette.Blood);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        Center(fill.rectTransform, Size);

        var sheen = MakeImage(rim.transform, "GlobeSheen", disc, new Color(1f, 0.92f, 0.82f, 0.22f));
        Place(sheen.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-Size * 0.17f, Size * 0.2f), Size * 0.34f);

        label = MakeText(rim.transform, "GlobeLabel", 26);
        label.font = DiabloUIFactory.DisplayFont;
        label.fontStyle = FontStyle.Bold;
        Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), Size);
        label.rectTransform.sizeDelta = new Vector2(Size, 34f);

        var caption = MakeText(rim.transform, "GlobeCaption", 13);
        caption.font = DiabloUIFactory.BodyFont;
        caption.text = Loc.T("hud.globe.life");
        caption.color = DiabloPalette.Parchment;
        Place(caption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), Size);
        caption.rectTransform.sizeDelta = new Vector2(Size, 18f);

        // Shield chip above the globe (shown while an absorb shield is live).
        var chip = HudChrome.Panel(parent, "ShieldChip", new Vector2(150f, 24f), fill: new Color(0.078f, 0.063f, 0.102f, 0.92f));
        chip.GetComponent<Outline>().effectColor = DiabloPalette.Shield;
        HudChrome.Anchor(chip, bottomCenter,
            new Vector2(HudLayout.GlobeCenterX, HudLayout.PanelBottom + HudLayout.GlobeSize + 8f), new Vector2(150f, 24f));
        chip.pivot = new Vector2(0.5f, 0f);
        shieldText = DiabloUIFactory.MakeText(chip, "ShieldText", "", 13, DiabloPalette.Shield, TextAnchor.MiddleCenter, shadow: false, font: DiabloUIFactory.BodyFont);
        HudChrome.Stretch(shieldText.rectTransform, 2f);
        shieldChip = chip.gameObject;
        shieldChip.SetActive(false);
    }

    void BuildFlask(Transform parent)
    {
        // gear-slots S2: flask slot, now the last cell of the skill row, dark radial sweep while it refills.
        var flaskFrame = MakeImage(parent, "FlaskFrame", DiabloMenuArt.SlotBackground(ItemRarity.Rare, 96), Color.white);
        flaskFrame.type = Image.Type.Sliced;
        flaskFrame.color = new Color(1f, 0.55f, 0.55f, 1f); // blood-tinted border, as on the canvas
        HudChrome.Anchor(flaskFrame.rectTransform, new Vector2(0.5f, 0f),
            new Vector2(HudLayout.FlaskCenterX, HudLayout.SlotY), new Vector2(HudLayout.SlotSize, HudLayout.SlotSize));
        flaskFrame.rectTransform.pivot = new Vector2(0.5f, 0f);
        flaskIcon = MakeImage(flaskFrame.transform, "FlaskIcon", DiabloMenuArt.ItemIcon("HealingPotion", ItemRarity.Rare), Color.white);
        flaskIcon.preserveAspect = true;
        Stretch(flaskIcon.rectTransform, 10f);
        flaskSweep = MakeImage(flaskFrame.transform, "FlaskCooldownSweep", HudChrome.Circle(), DiabloPalette.CooldownVeil);
        flaskSweep.type = Image.Type.Filled;
        flaskSweep.fillMethod = Image.FillMethod.Radial360;
        flaskSweep.fillOrigin = (int)Image.Origin360.Top;
        flaskSweep.fillClockwise = false;
        flaskSweep.fillAmount = 0f;
        Stretch(flaskSweep.rectTransform, 4f);
        flaskText = MakeText(flaskFrame.transform, "FlaskCaption", 15);
        flaskText.font = DiabloUIFactory.BodyFont;
        flaskText.fontStyle = FontStyle.Bold;
        flaskText.alignment = TextAnchor.LowerRight;
        Stretch(flaskText.rectTransform, 5f);
        var flaskKey = MakeText(flaskFrame.transform, "FlaskKey", 13);
        flaskKey.font = DiabloUIFactory.BodyFont;
        flaskKey.fontStyle = FontStyle.Bold;
        flaskKey.text = "1";
        flaskKey.alignment = TextAnchor.UpperLeft;
        flaskKey.color = DiabloPalette.TrimGold;
        Stretch(flaskKey.rectTransform, 5f);

        var name = DiabloUIFactory.MakeText(parent, "FlaskName", Loc.T("hud.flask.name"), 13, DiabloPalette.ParchmentDim,
            TextAnchor.MiddleCenter, shadow: false, font: DiabloUIFactory.BodyFont);
        name.horizontalOverflow = HorizontalWrapMode.Overflow;
        HudChrome.Anchor(name.rectTransform, new Vector2(0.5f, 0f),
            new Vector2(HudLayout.FlaskCenterX, HudLayout.SlotNameY), new Vector2(HudLayout.SlotSize + 10f, 16f));
        name.rectTransform.pivot = new Vector2(0.5f, 0f);
    }

    void BuildSideColumn(Transform parent)
    {
        var bottomCenter = new Vector2(0.5f, 0f);
        float x = HudLayout.SideCenterX;

        // 2×2 menu keys (real buttons, ≥ 44 px), then the gold card above them.
        var grid = new GameObject("HudMenuKeys", typeof(RectTransform));
        grid.transform.SetParent(parent, false);
        var gr = (RectTransform)grid.transform;
        HudChrome.Anchor(gr, bottomCenter, new Vector2(x, HudLayout.PanelBottom + 6f), new Vector2(HudLayout.SideWidth, 94f));
        gr.pivot = new Vector2(0.5f, 0f);
        var gl = grid.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2((HudLayout.SideWidth - 6f) * 0.5f, 44f);
        gl.spacing = new Vector2(6f, 6f);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 2;
        MenuKey(grid.transform, "I", "hud.menu.bag", ToggleInventory);
        MenuKey(grid.transform, "C", "hud.menu.character", ToggleCharacter);
        MenuKey(grid.transform, "M", "hud.menu.map", ToggleMap);
        MenuKey(grid.transform, "Esc", "hud.menu.pause", TogglePause);

        var card = HudChrome.Panel(parent, "HudGoldCard", new Vector2(HudLayout.SideWidth, 64f));
        HudChrome.Anchor(card, bottomCenter, new Vector2(x, HudLayout.PanelBottom + 108f), new Vector2(HudLayout.SideWidth, 64f));
        card.pivot = new Vector2(0.5f, 0f);
        var cap = DiabloUIFactory.MakeText(card, "GoldCaption", Loc.T("hud.gold.caption"), 13, DiabloPalette.ParchmentDim,
            TextAnchor.UpperLeft, shadow: false, font: DiabloUIFactory.BodyFont);
        HudChrome.Stretch(cap.rectTransform, 8f);
        goldText = DiabloUIFactory.MakeText(card, "GoldAmount", "0", 22, DiabloPalette.GoldCoin,
            TextAnchor.LowerLeft, font: DiabloUIFactory.DisplayFont);
        goldText.fontStyle = FontStyle.Bold;
        HudChrome.Stretch(goldText.rectTransform, 8f);

        // Unspent points hint (was part of the old purse line).
        hudStatusText = DiabloUIFactory.MakeText(parent, "HudStatusText", "", 14, DiabloPalette.GoldBright,
            TextAnchor.MiddleCenter, font: DiabloUIFactory.BodyFont);
        hudStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        HudChrome.Anchor(hudStatusText.rectTransform, bottomCenter, new Vector2(x, HudLayout.PanelBottom + 178f), new Vector2(HudLayout.SideWidth, 20f));
        hudStatusText.rectTransform.pivot = new Vector2(0.5f, 0f);
    }

    static void MenuKey(Transform parent, string key, string locKey, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("MenuKey_" + key, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = DiabloPalette.ButtonPlain;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = DiabloPalette.PanelEdge;
        outline.effectDistance = new Vector2(1f, -1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var t = DiabloUIFactory.MakeText(go.transform, "Label", Loc.F("hud.menu.key", DiabloPalette.ToHex(DiabloPalette.TrimGold), key, Loc.T(locKey)), 14,
            DiabloPalette.Parchment, TextAnchor.MiddleCenter, shadow: false, font: DiabloUIFactory.BodyFont);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        HudChrome.Stretch(t.rectTransform, 2f);
    }

    static GameObject Hero() => GameObject.Find(DiabloNetworkPlayer.HeroName);

    static void ToggleInventory()
    {
        var ui = Hero() != null ? Hero().GetComponent<DiabloInventoryUI>() : null;
        if (ui != null) ui.SetOpen(!ui.IsOpen);
    }

    static void ToggleCharacter()
    {
        var ui = Hero() != null ? Hero().GetComponent<DiabloCharacterUI>() : null;
        if (ui != null) ui.SetOpen(!ui.IsOpen);
    }

    static void ToggleMap()
    {
        var ui = Hero() != null ? Hero().GetComponent<WorldMapUI>() : null;
        if (ui != null) ui.SetOpen(!ui.IsOpen);
    }

    static void TogglePause()
    {
        var pm = Object.FindAnyObjectByType<PauseMenu>();
        if (pm != null) pm.SetOpen(!pm.IsOpen);
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

    void PaintFlask(HealingFlask flask)
    {
        if (flaskSweep == null || flask == null) return;
        bool empty = flask.Charges <= 0;
        flaskSweep.fillAmount = flask.IsFull ? 0f : 1f - flask.ReadyFraction;
        flaskIcon.color = empty ? new Color(0.45f, 0.45f, 0.45f, 1f) : flask.IsHealing ? new Color(0.7f, 1f, 0.7f, 1f) : Color.white;
        flaskText.text = flask.IsFull || !empty
            ? (flask.MaxCharges > 1 ? $"{flask.Charges}/{flask.MaxCharges}" : "")
            : $"{Mathf.CeilToInt(flask.CooldownRemaining)}s";
    }

    static void Stretch(RectTransform r, float inset) => HudChrome.Stretch(r, inset);

    static Image MakeImage(Transform parent, string name, Sprite sprite, Color color) => HudChrome.Img(parent, name, sprite, color);

    static Text MakeText(Transform parent, string name, int size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = DiabloUIFactory.BodyFont;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        go.AddComponent<Shadow>();
        return t;
    }
}
