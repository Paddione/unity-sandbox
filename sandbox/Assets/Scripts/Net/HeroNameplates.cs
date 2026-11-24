using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Name above every network hero (cinderreach-accounts S1): pooled screen
/// labels, own hero in gold, allies in parchment, dead heroes dimmed.
/// Display-only (no raycasts), reads <see cref="DiabloNetworkPlayer.Live"/>
/// instead of scanning the scene, hidden while a full-screen menu is open.
/// Solo play has no network heroes and therefore no plates.
/// </summary>
public sealed class HeroNameplates : MonoBehaviour
{
    public const float Lift = 2.35f;

    Canvas canvas;
    RectTransform root;
    Camera cam;
    readonly List<Text> pool = new List<Text>();

    /// <summary>Plates shown this frame (test seam).</summary>
    public int ShownCount { get; private set; }

    /// <summary>Text of the last plate drawn for each hero this frame (test seam).</summary>
    public readonly Dictionary<DiabloNetworkPlayer, string> Shown = new Dictionary<DiabloNetworkPlayer, string>();

    void Awake()
    {
        var go = new GameObject("NameplateCanvas");
        go.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(go, sortingOrder: 3, raycaster: false);
        root = go.GetComponent<RectTransform>();
    }

    /// <summary>Plate colour for a hero (pure): own gold, dead dim, else parchment.</summary>
    public static Color ColorFor(bool own, bool alive) =>
        !alive ? DiabloPalette.ParchmentDim : own ? DiabloPalette.GoldBright : DiabloPalette.Parchment;

    void LateUpdate()
    {
        ShownCount = 0;
        Shown.Clear();
        if ((DiabloNetworkPlayer.Live.Count == 0 && PartyCompanion.Live.Count == 0)
            || ClassSelectUI.AnyOpen || PauseMenuUI.AnyOpen) { HideFrom(0); return; }
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
        if (cam == null) { HideFrom(0); return; }
        int n = 0;
        foreach (var p in DiabloNetworkPlayer.Live)
        {
            if (p == null || !p.isActiveAndEnabled) continue;
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position + Vector3.up * Lift);
            if (sp.z <= 0f) continue;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var local)) continue;
            var hp = p.GetComponent<PlayerHealth>();
            bool alive = hp == null || hp.CurrentHP > 0f;
            var label = Get(n++);
            label.text = p.ShownName;
            label.color = ColorFor(p.IsOwner, alive);
            var plate = (RectTransform)label.transform.parent;
            plate.sizeDelta = new Vector2(label.preferredWidth + 16f, 24f);
            plate.anchoredPosition = local;
            if (!plate.gameObject.activeSelf) plate.gameObject.SetActive(true);
            Shown[p] = label.text;
        }
        // AI partners walk under their class name (parchment, never gold);
        // dead companions keep a dimmed plate at their death spot.
        foreach (var c in PartyCompanion.Live)
        {
            if (c == null) continue;
            Vector3 sp = cam.WorldToScreenPoint(c.transform.position + Vector3.up * Lift);
            if (sp.z <= 0f) continue;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var local)) continue;
            var hp = c.GetComponent<PlayerHealth>();
            bool alive = c.isActiveAndEnabled && (hp == null || hp.CurrentHP > 0f);
            var label = Get(n++);
            label.text = c.DisplayName;
            label.color = ColorFor(false, alive);
            var plate = (RectTransform)label.transform.parent;
            plate.sizeDelta = new Vector2(label.preferredWidth + 16f, 24f);
            plate.anchoredPosition = local;
            if (!plate.gameObject.activeSelf) plate.gameObject.SetActive(true);
        }
        ShownCount = n;
        HideFrom(n);
    }

    Text Get(int i)
    {
        while (pool.Count <= i)
        {
            var go = new GameObject("Nameplate", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.03f, 0.05f, 0.55f);
            bg.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            var t = DiabloUIFactory.MakeText(go.transform, "Name", "", 16, Color.white, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            var tr = t.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            pool.Add(t);
        }
        return pool[i];
    }

    void HideFrom(int start)
    {
        for (int i = start; i < pool.Count; i++)
            if (pool[i].transform.parent.gameObject.activeSelf) pool[i].transform.parent.gameObject.SetActive(false);
    }
}
