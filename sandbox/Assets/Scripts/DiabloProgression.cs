using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Hero experience and levels. Kills grant XP (tier-weighted, scaled by the
/// helmet's XP-gain affix); every level adds <see cref="LifePerLevel"/> max
/// life and refills the globe. Co-op: the server shares each kill's XP with
/// every client (party XP, D2 style) through <see cref="DiabloNetworkPlayer"/>.
/// The level-up is a moment, not text: gold flash, sting, banner pop,
/// ember pillar, bar refill — all unscaled, input never locked.
/// Local-only per client, like <see cref="Equipment"/>; persisted through
/// <see cref="SaveService"/> (S2) instead of living only for the run.
/// </summary>
public sealed class DiabloProgression : MonoBehaviour
{
    public const int MaxLevel = 30;
    public const float LifePerLevel = 3f;
    public const int XpNormal = 12;
    public const int XpElite = 40;
    public const int XpChest = 5;
    public const int XpBoss = 120; // S5: The Hollow Abbot (Level 6 Meile)

    /// <summary>True for AI companions (their progression mirrors the hero, never saves or HUDs).</summary>
    public bool IsCompanion =>
        companionFlag || GetComponent<PartyCompanion>() != null;

    bool companionFlag;

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }

    /// <summary>S4 point-spend sheet (solo-local; persisted via SaveGame).</summary>
    public CharacterSheet Sheet { get; private set; } = new CharacterSheet();

    public static int LevelUps;
    public static int XpGranted;

    Image xpFill;
    Text levelLabel;
    Text xpAmountLabel;
    Image flash;
    Text banner;
    Text bannerSub;
    float shownFill;
    Coroutine fillTween;

    /// <summary>XP needed to go from <paramref name="level"/> to the next.</summary>
    public static int XpToNext(int level) => 20 * level * level + 20 * level;

    public static int XpFor(LootTier tier)
    {
        switch (tier)
        {
            case LootTier.Elite: return XpElite;
            case LootTier.Chest: return XpChest;
            case LootTier.Boss: return XpBoss;
            case LootTier.ActBoss: return XpBoss * 2;
            default: return XpNormal;
        }
    }

    /// <summary>
    /// State export for <see cref="SaveService"/> (S2 persist; S4 adds the
    /// point/allocation fields on top of this level/XP pair).
    /// </summary>
    public void GetState(out int level, out int xp)
    {
        level = Level;
        xp = Xp;
    }

    /// <summary>State import for <see cref="SaveService"/> (Continue path).</summary>
    public void SetState(int level, int xp)
    {
        Level = Mathf.Clamp(level, 1, MaxLevel);
        Xp = Level >= MaxLevel ? 0 : Mathf.Max(0, xp);
        TweenFill(Fill);
    }

    /// <summary>
    /// Full state export for <see cref="SaveService"/> (S4 points included).
    /// The 2-arg <see cref="GetState"/> stays as the level/XP seam (S2 tests).
    /// </summary>
    public void GetState(out int level, out int xp, out int freeAttr, out int freeSkill,
        out int strength, out int vitality, out int focus,
        out System.Collections.Generic.List<SkillRankEntry> ranks)
    {
        GetState(out level, out xp);
        var sheet = Sheet ?? (Sheet = new CharacterSheet());
        freeAttr = sheet.freeAttributePoints;
        freeSkill = sheet.freeSkillPoints;
        strength = sheet.strength;
        vitality = sheet.vitality;
        focus = sheet.focus;
        ranks = sheet.ToEntries();
    }

    /// <summary>Full state import for <see cref="SaveService"/> (S4 points included).</summary>
    public void SetState(int level, int xp, int freeAttr, int freeSkill,
        int strength, int vitality, int focus,
        System.Collections.Generic.List<SkillRankEntry> ranks)
    {
        SetState(level, xp);
        var sheet = Sheet ?? (Sheet = new CharacterSheet());
        sheet.freeAttributePoints = Mathf.Max(0, freeAttr);
        sheet.freeSkillPoints = Mathf.Max(0, freeSkill);
        sheet.strength = Mathf.Max(0, strength);
        sheet.vitality = Mathf.Max(0, vitality);
        sheet.focus = Mathf.Max(0, focus);
        sheet.FromEntries(ranks);
        // cinderreach-levelup-empower: old saves may still carry free attribute
        // points; auto-assign them per the class template on load (D3).
        sheet.DistributeFreeAttributes(RunProfile.Class);
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.Refresh();
    }

    /// <summary>Spend one skill point on a rank (persists immediately).</summary>
    public bool SpendSkillPoint(DiabloSkills.Skill skill)
    {
        var sheet = Sheet ?? (Sheet = new CharacterSheet());
        if (!sheet.SpendSkill(skill)) return false;
        if (!IsCompanion && Diablo.IsLocalAuthority()) SaveService.SaveNow(gameObject);
        return true;
    }

    /// <summary>
    /// Bonus max life: flat +3/level base plus vitality (+5/point, S4).
    /// Read by <see cref="Equipment.GetMaxHP"/>.
    /// </summary>
    public float BonusLife => (Level - 1) * LifePerLevel + (Sheet != null ? Sheet.VitalityLife() : 0f);

    /// <summary>Fraction of the way to the next level.</summary>
    public float Fill => Level >= MaxLevel ? 1f : Mathf.Clamp01((float)Xp / XpToNext(Level));

    void Awake()
    {
        // Companions are named before their components land, so the flag is
        // already correct here (PartyCompanion itself is attached later).
        companionFlag = gameObject.name.StartsWith("Companion_");
        if (Application.isPlaying && IsLocalHero() && !IsCompanion) DiabloAmbience.Ensure(transform);
    }

    bool IsLocalHero()
    {
        var net = GetComponent<DiabloNetworkPlayer>();
        return net == null || !net.IsSpawned || net.IsOwner;
    }

    /// <summary>
    /// Kill reward entry point (server/solo, where <see cref="EnemyHealth"/> dies).
    /// Networked: broadcast through the host's player so every client levels.
    /// </summary>
    public static void AwardKill(LootTier tier, Vector3 at)
    {
        int xp = XpFor(tier);
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.IsServer && nm.SpawnManager != null)
        {
            var hostObj = nm.SpawnManager.GetLocalPlayerObject();
            var relay = hostObj != null ? hostObj.GetComponent<DiabloNetworkPlayer>() : null;
            if (relay != null && relay.IsSpawned)
            {
                relay.ShareKillXP(xp, at);
                return;
            }
        }
        GrantLocal(xp, at);
    }

    /// <summary>Give XP to this process's hero (the one named "Player").</summary>
    public static void GrantLocal(int baseXp, Vector3 at)
    {
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero == null) return;
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog == null) prog = hero.AddComponent<DiabloProgression>();
        var eq = hero.GetComponent<Equipment>();
        float mul = eq != null ? eq.GetXPMul() / 100f : 1f;
        int xp = Mathf.Max(1, Mathf.RoundToInt(baseXp * mul));
        prog.AddXp(xp);
        DiabloJuice.ShowText(at + Vector3.up * 2.4f, Loc.F("hud.xp.gain", xp), DiabloPalette.TrimGold);
    }

    public void AddXp(int amount)
    {
        if (amount <= 0 || Level >= MaxLevel) return;
        XpGranted += amount;
        Xp += amount;
        int gained = 0;
        while (Level < MaxLevel && Xp >= XpToNext(Level))
        {
            Xp -= XpToNext(Level);
            Level++;
            gained++;
        }
        if (Level >= MaxLevel) Xp = 0;
        if (Sheet == null) Sheet = new CharacterSheet();
        for (int i = 0; i < gained; i++) Sheet.AwardLevel(RunProfile.Class);
        if (gained > 0) OnLevelUp();
        else TweenFill(Fill);
    }

    void OnLevelUp()
    {
        LevelUps++;
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.Heal(hp.MaxHP);
        if (IsCompanion) return; // mirrored levels stay quiet: no save, no fanfare
        SaveService.SaveNow(gameObject); // S2 autosave: level-up
        Debug.Log($"[Diablo] Level up: {Level} (max life {(hp != null ? hp.MaxHP : 0f):0}).");
        DiabloAudio.PlayLevelUp();
        if (Application.isPlaying) StartCoroutine(LevelUpMoment());
    }

    /// <summary>
    /// Keep AI companions on the protagonist's level (party-team): quietly
    /// copy level, XP, attributes and skill ranks. No level-up fanfare, no
    /// autosave, no XP bar — the companion fights at the hero's power without
    /// touching the save. Clamps to <see cref="MaxLevel"/> like
    /// <see cref="SetState(int, int)"/>.
    /// </summary>
    public void MirrorHeroProgress(DiabloProgression hero)
    {
        if (hero == null || hero == this) return;
        Level = Mathf.Clamp(hero.Level, 1, MaxLevel);
        Xp = Level >= MaxLevel ? 0 : Mathf.Max(0, hero.Xp);
        var src = hero.Sheet;
        if (src != null)
        {
            if (Sheet == null) Sheet = new CharacterSheet();
            Sheet.freeAttributePoints = Mathf.Max(0, src.freeAttributePoints);
            Sheet.freeSkillPoints = Mathf.Max(0, src.freeSkillPoints);
            Sheet.strength = Mathf.Max(0, src.strength);
            Sheet.vitality = Mathf.Max(0, src.vitality);
            Sheet.focus = Mathf.Max(0, src.focus);
            Sheet.FromEntries(src.ToEntries());
        }
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.Refresh();
    }

    /// <summary>flash → sting → banner → pillar → refill → banner out.</summary>
    IEnumerator LevelUpMoment()
    {
        EnsureHud();
        StartCoroutine(Tween(0.1f, t => SetAlpha(flash, 0.6f * (1f - OutCubic(t)))));
        yield return new WaitForSecondsRealtime(0.05f);
        banner.text = Loc.F("hud.levelup.title", Level);
        bannerSub.text = Loc.F("hud.levelup.gains", LifePerLevel, CharacterSheet.AttributesPerLevel);
        banner.enabled = true;
        bannerSub.enabled = true;
        DiabloAmbience.LevelPillar(transform.position);
        StartCoroutine(Tween(0.35f, t => banner.rectTransform.localScale = Vector3.one * OutBack(t)));
        yield return new WaitForSecondsRealtime(0.35f);
        shownFill = 0f; // refill from empty: the new level starts its own bar
        TweenFill(Fill);
        yield return new WaitForSecondsRealtime(1.1f);
        yield return Tween(0.2f, t => banner.rectTransform.localScale = Vector3.one * (1f - OutCubic(t)));
        banner.enabled = false;
        bannerSub.enabled = false;
    }

    // ---------------------------------------------------------------- HUD

    void Update()
    {
        if (!IsLocalHero() || IsCompanion) return; // companions mirror the hero: no own XP bar
        EnsureHud();
        levelLabel.text = Loc.F("hud.xp.level", Level);
        xpAmountLabel.text = XpAmountText(Level, Xp);
    }

    /// <summary>"2.840 / 4.500 EP" under the bar, "Höchststufe" at the cap (hud-redesign).</summary>
    public static string XpAmountText(int level, int xp)
    {
        if (level >= MaxLevel) return Loc.T("hud.xp.max");
        return Loc.F("hud.xp.amount", DiabloLifeGlobe.FormatGold(xp), DiabloLifeGlobe.FormatGold(XpToNext(level)));
    }

    void TweenFill(float target)
    {
        if (!Application.isPlaying) return;
        EnsureHud();
        if (fillTween != null) StopCoroutine(fillTween);
        float from = shownFill;
        fillTween = StartCoroutine(Tween(0.3f, t =>
        {
            shownFill = Mathf.Lerp(from, target, OutCubic(t));
            xpFill.fillAmount = shownFill;
        }));
    }

    /// <summary>Shown XP-bar fill (animates toward <see cref="Fill"/>; test seam).</summary>
    public float ShownFill => xpFill != null ? xpFill.fillAmount : 0f;

    void EnsureHud()
    {
        if (xpFill != null) return;
        var canvasGo = new GameObject("XpCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var white = WhiteSprite();

        // hud-redesign: thin bar inside the skill plate, "Stufe N" left and "x / y EP" right under it.
        var back = HudChrome.Bar(canvasGo.transform, "XpBack", new Vector2(HudLayout.XpBarWidth, 8f), DiabloPalette.TrimGold, out xpFill);
        xpFill.name = "XpFill";
        xpFill.fillAmount = shownFill;
        Place(back, new Vector2(0.5f, 0f), new Vector2(HudLayout.XpBarWidth, 8f), new Vector2(0f, HudLayout.XpBarY));

        levelLabel = NewText(canvasGo.transform, "LevelLabel", 14, DiabloPalette.ParchmentDim);
        levelLabel.font = DiabloUIFactory.BodyFont;
        levelLabel.alignment = TextAnchor.MiddleLeft;
        Place(levelLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(HudLayout.XpBarWidth, 18f), new Vector2(0f, HudLayout.XpLabelY));
        xpAmountLabel = NewText(canvasGo.transform, "XpAmountLabel", 14, DiabloPalette.ParchmentDim);
        xpAmountLabel.font = DiabloUIFactory.BodyFont;
        xpAmountLabel.alignment = TextAnchor.MiddleRight;
        Place(xpAmountLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(HudLayout.XpBarWidth, 18f), new Vector2(0f, HudLayout.XpLabelY));

        flash = NewImage(canvasGo.transform, "LevelFlash", white, DiabloPalette.TrimGold);
        var flr = flash.rectTransform;
        flr.anchorMin = Vector2.zero;
        flr.anchorMax = Vector2.one;
        flr.offsetMin = flr.offsetMax = Vector2.zero;
        SetAlpha(flash, 0f);

        banner = NewText(canvasGo.transform, "LevelBanner", 64, DiabloPalette.TrimGold);
        Place(banner.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(600f, 80f), Vector2.zero);
        banner.gameObject.AddComponent<Outline>().effectColor = new Color(0.25f, 0.05f, 0.02f, 0.9f);
        // Subline is a child: it scales with the banner pop and sits right under it.
        bannerSub = NewText(banner.transform, "LevelBannerSub", 24, DiabloPalette.Parchment);
        Place(bannerSub.rectTransform, new Vector2(0.5f, 0f), new Vector2(300f, 30f), new Vector2(0f, -22f));
        banner.enabled = false;
        bannerSub.enabled = false;
    }

    static Sprite whiteSprite;

    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text NewText(Transform parent, string name, int size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        go.AddComponent<Shadow>();
        return t;
    }

    static void Place(RectTransform r, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
    }

    static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    // ---------------------------------------------------------------- easing

    static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    static float OutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>Unscaled tween: survives hit-stop and pause.</summary>
    static IEnumerator Tween(float duration, System.Action<float> apply)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            apply(t);
            yield return null;
        }
        apply(1f);
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        LevelUps = 0;
        XpGranted = 0;
    }
}
