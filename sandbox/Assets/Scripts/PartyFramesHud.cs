using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Party frames, top left (cinderreach-hud-redesign, canvas "HUD im Spiel"): the
/// own hero in a large frame (class portrait in the class accent, name, class,
/// level, life bar with shield overlay), below it one compact frame per other
/// party member — network heroes (<see cref="DiabloNetworkPlayer.Live"/>, HP from
/// the replicated <c>hpState</c>, level from <c>heroLevel</c>) and solo companions
/// (<see cref="PartyCompanion.Live"/>). Display-only, raycast-free; hidden while a
/// full-screen menu is open. Spawned next to <see cref="DiabloLifeGlobe"/>.
/// </summary>
public sealed class PartyFramesHud : MonoBehaviour
{
    public const int MaxFrames = 4;
    /// <summary>Frames repaint at 10 Hz: life bars need no per-frame precision.</summary>
    public const float RepaintInterval = 0.1f;

    /// <summary>One frame's data (pure, testable).</summary>
    public struct Row
    {
        public string name;
        public string classId;
        public int level;
        public float hp01;
        public float shield01;
        public bool own;
        public bool alive;
    }

    /// <summary>Own hero first, then the others in the given order, at most <see cref="MaxFrames"/>.</summary>
    public static List<Row> Rows(Row? own, IEnumerable<Row> others)
    {
        var list = new List<Row>(MaxFrames);
        if (own.HasValue) list.Add(own.Value);
        if (others != null)
            foreach (var r in others)
            {
                if (list.Count >= MaxFrames) break;
                if (!r.own) list.Add(r);
            }
        return list;
    }

    /// <summary>Life fraction from a replicated (current, max) pair.</summary>
    public static float Hp01(Vector2 curMax) => curMax.y > 0f ? Mathf.Clamp01(curMax.x / curMax.y) : 0f;

    Canvas canvas;
    GameObject hero;
    float nextPaint;
    readonly List<Frame> frames = new List<Frame>();
    readonly List<Row> others = new List<Row>();

    /// <summary>Rows drawn this frame (test seam).</summary>
    public readonly List<Row> Shown = new List<Row>();

    sealed class Frame
    {
        public GameObject root;
        public Image ring;
        public Image portrait;
        public Text name;
        public Text sub;
        public Image hp;
        public Image shield;
        public CanvasGroup group;
    }

    void Awake()
    {
        var go = new GameObject("PartyFramesCanvas");
        go.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(go, sortingOrder: 4, raycaster: false);
        frames.Add(BuildFrame(go.transform, 0, big: true));
        for (int i = 1; i < MaxFrames; i++) frames.Add(BuildFrame(go.transform, i, big: false));
    }

    void LateUpdate()
    {
        if (Time.unscaledTime < nextPaint) return;
        nextPaint = Time.unscaledTime + RepaintInterval;
        Shown.Clear();
        if (hero == null || !hero.activeInHierarchy || hero.name != DiabloNetworkPlayer.HeroName)
            hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        bool hidden = hero == null || ClassSelectUI.AnyOpen || PauseMenuUI.AnyOpen;
        canvas.enabled = !hidden;
        if (hidden) return;

        others.Clear();
        foreach (var p in DiabloNetworkPlayer.Live)
        {
            if (p == null || !p.isActiveAndEnabled || p.IsOwner || p.gameObject == hero) continue;
            int ci = p.classIndex.Value;
            var cls = ci >= 0 && ci < ClassCatalog.All.Length ? ClassCatalog.All[ci] : ClassCatalog.Fenwalker;
            float hp = Hp01(p.hpState.Value);
            others.Add(new Row { name = p.ShownName, classId = cls.id, level = Mathf.Max(1, p.heroLevel.Value), hp01 = hp, alive = hp > 0f });
        }
        foreach (var c in PartyCompanion.Live)
        {
            if (c == null || !c.isActiveAndEnabled) continue;
            var h = c.GetComponent<PlayerHealth>();
            var prog = c.GetComponent<DiabloProgression>();
            float hp = h != null && h.MaxHP > 0f ? Mathf.Clamp01(h.CurrentHP / h.MaxHP) : 0f;
            others.Add(new Row { name = c.DisplayName, classId = c.classId, level = prog != null ? prog.Level : 1, hp01 = hp, alive = hp > 0f });
        }

        var rows = Rows(OwnRow(hero), others);
        Shown.AddRange(rows);
        for (int i = 0; i < frames.Count; i++)
        {
            bool on = i < rows.Count;
            if (frames[i].root.activeSelf != on) frames[i].root.SetActive(on);
            if (on) Paint(frames[i], rows[i]);
        }
    }

    static Row OwnRow(GameObject hero)
    {
        var net = hero.GetComponent<DiabloNetworkPlayer>();
        var look = hero.GetComponent<ClassAppearance>();
        var skills = hero.GetComponent<DiabloSkills>();
        var prog = hero.GetComponent<DiabloProgression>();
        var h = hero.GetComponent<PlayerHealth>();
        string name = net != null && net.IsSpawned ? net.ShownName
            : look != null ? look.AppliedProfile.heroName : "";
        var cls = skills != null ? skills.ActiveClass : RunProfile.Class;
        float max = h != null ? h.MaxHP : 0f;
        float hp = h != null && max > 0f ? Mathf.Clamp01(h.CurrentHP / max) : 0f;
        float sh = h != null && max > 0f ? Mathf.Clamp01(h.ShieldAmount / max) : 0f;
        return new Row
        {
            name = name, classId = cls.id, level = prog != null ? prog.Level : 1,
            hp01 = hp, shield01 = sh, own = true, alive = hp > 0f,
        };
    }

    static void Paint(Frame f, Row r)
    {
        var cls = ClassCatalog.Get(r.classId);
        var accent = DiabloPalette.ClassAccent(r.classId);
        f.ring.color = accent;
        var portrait = DiabloMenuArt.ClassPortrait(r.classId);
        f.portrait.sprite = portrait;
        f.portrait.enabled = portrait != null;
        f.name.text = r.name;
        f.sub.text = Loc.F("hud.party.level", cls.displayName, r.level);
        if (f.sub.fontSize >= 14) f.sub.color = accent;
        f.hp.fillAmount = r.hp01;
        if (f.shield != null)
        {
            var sr = f.shield.rectTransform;
            float w = ((RectTransform)f.hp.transform.parent).rect.width;
            sr.anchoredPosition = new Vector2(w * r.hp01, 0f);
            sr.sizeDelta = new Vector2(w * Mathf.Min(r.shield01, 1f - r.hp01), 0f);
            f.shield.enabled = r.shield01 > 0f;
        }
        f.group.alpha = r.alive ? 1f : 0.5f;
    }

    static Frame BuildFrame(Transform parent, int index, bool big)
    {
        var f = new Frame();
        float w = big ? 330f : 306f;
        float h = big ? 88f : 60f;
        float y = big ? -28f : -28f - 88f - 12f - (index - 1) * (60f + 10f);
        var panel = HudChrome.Panel(parent, big ? "PartyFrameOwn" : "PartyFrame" + index, new Vector2(w, h));
        HudChrome.Anchor(panel, new Vector2(0f, 1f), new Vector2(big ? 28f : 52f, y), new Vector2(w, h));
        f.root = panel.gameObject;
        f.group = f.root.AddComponent<CanvasGroup>();
        f.group.blocksRaycasts = false;

        float disc = big ? 64f : 40f;
        f.ring = HudChrome.Img(panel, "PortraitRing", HudChrome.Circle(), DiabloPalette.LanternEmber);
        HudChrome.Anchor(f.ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(disc, disc));
        var well = HudChrome.Img(f.ring.transform, "PortraitWell", HudChrome.Circle(), new Color(0.11f, 0.08f, 0.125f, 1f));
        HudChrome.Anchor(well.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(disc - 4f, disc - 4f));
        // No stencil mask (it breaks UI batching): the bust sits inset inside the round well.
        f.portrait = HudChrome.Img(well.transform, "Portrait", null, Color.white);
        f.portrait.preserveAspect = true;
        HudChrome.Stretch(f.portrait.rectTransform, disc * 0.12f);

        float left = 12f + disc + 14f;
        float right = 14f;
        f.name = DiabloUIFactory.MakeText(panel, "Name", "", big ? 18 : 15, DiabloPalette.Parchment, TextAnchor.UpperLeft,
            font: big ? DiabloUIFactory.DisplayFont : DiabloUIFactory.BodyFont);
        f.name.fontStyle = FontStyle.Bold;
        f.name.horizontalOverflow = HorizontalWrapMode.Overflow;
        Inset(f.name.rectTransform, left, right, big ? 10f : 7f, 22f);

        f.sub = DiabloUIFactory.MakeText(panel, "Class", "", big ? 14 : 13, DiabloPalette.ParchmentDim,
            big ? TextAnchor.UpperLeft : TextAnchor.UpperRight, shadow: false, font: DiabloUIFactory.BodyFont);
        f.sub.horizontalOverflow = HorizontalWrapMode.Overflow;
        Inset(f.sub.rectTransform, left, right, big ? 34f : 8f, 18f);

        var bar = HudChrome.Bar(panel, "LifeBar", new Vector2(w - left - right, big ? 10f : 7f), DiabloPalette.Blood, out f.hp);
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 0f);
        bar.anchoredPosition = new Vector2(left, big ? 14f : 12f);
        if (big)
        {
            f.shield = HudChrome.Img(bar, "ShieldFill", null, new Color(DiabloPalette.Shield.r, DiabloPalette.Shield.g, DiabloPalette.Shield.b, 0.85f));
            var sr = f.shield.rectTransform;
            sr.anchorMin = new Vector2(0f, 0f); sr.anchorMax = new Vector2(0f, 1f); sr.pivot = new Vector2(0f, 0.5f);
            sr.sizeDelta = Vector2.zero;
            f.shield.enabled = false;
        }
        f.root.SetActive(false);
        return f;
    }

    static void Inset(RectTransform r, float left, float right, float top, float height)
    {
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.offsetMin = new Vector2(left, -top - height);
        r.offsetMax = new Vector2(-right, -top);
    }
}
