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
/// Local-only per client, like <see cref="Equipment"/>; not saved between runs.
/// </summary>
public sealed class DiabloProgression : MonoBehaviour
{
    public const int MaxLevel = 30;
    public const float LifePerLevel = 3f;
    public const int XpNormal = 12;
    public const int XpElite = 40;
    public const int XpChest = 5;

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }

    public static int LevelUps;
    public static int XpGranted;

    Image xpFill;
    Text levelLabel;
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
            default: return XpNormal;
        }
    }

    /// <summary>Bonus max life from levels (read by <see cref="Equipment.GetMaxHP"/>).</summary>
    public float BonusLife => (Level - 1) * LifePerLevel;

    /// <summary>Fraction of the way to the next level.</summary>
    public float Fill => Level >= MaxLevel ? 1f : Mathf.Clamp01((float)Xp / XpToNext(Level));

    void Awake()
    {
        if (Application.isPlaying && IsLocalHero()) DiabloAmbience.Ensure(transform);
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
        DiabloJuice.ShowText(at + Vector3.up * 2.4f, $"+{xp} XP", DiabloPalette.TrimGold);
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
        if (gained > 0) OnLevelUp();
        else TweenFill(Fill);
    }

    void OnLevelUp()
    {
        LevelUps++;
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.Heal(hp.MaxHP);
        Debug.Log($"[Diablo] Level up: {Level} (max life {(hp != null ? hp.MaxHP : 0f):0}).");
        DiabloAudio.PlayLevelUp();
        if (Application.isPlaying) StartCoroutine(LevelUpMoment());
    }

    /// <summary>flash → sting → banner → pillar → refill → banner out.</summary>
    IEnumerator LevelUpMoment()
    {
        EnsureHud();
        StartCoroutine(Tween(0.1f, t => SetAlpha(flash, 0.6f * (1f - OutCubic(t)))));
        yield return new WaitForSecondsRealtime(0.05f);
        banner.text = $"LEVEL {Level}";
        bannerSub.text = $"+{LifePerLevel:0} Life";
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
        if (!IsLocalHero()) return;
        EnsureHud();
        levelLabel.text = $"Lv {Level}";
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

        var back = NewImage(canvasGo.transform, "XpBack", white, DiabloPalette.Abyss);
        Place(back.rectTransform, new Vector2(0.5f, 0f), new Vector2(560f, 12f), new Vector2(0f, 66f));
        var edge = back.gameObject.AddComponent<Outline>();
        edge.effectColor = DiabloPalette.PanelEdge;
        xpFill = NewImage(back.transform, "XpFill", white, DiabloPalette.TrimGold);
        xpFill.type = Image.Type.Filled; // needs a sprite, or fillAmount no-ops
        xpFill.fillMethod = Image.FillMethod.Horizontal;
        xpFill.fillAmount = shownFill;
        var fr = xpFill.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(1f, 1f);
        fr.offsetMax = new Vector2(-1f, -1f);

        levelLabel = NewText(canvasGo.transform, "LevelLabel", 18, DiabloPalette.TrimGold);
        Place(levelLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(80f, 24f), new Vector2(-325f, 72f));

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
