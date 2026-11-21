using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Ground loot names while Alt is held: one rarity-colored label above each
/// item drop near the hero, pooled, display-only (no raycasts). Reads the
/// <see cref="LootDrop.Live"/> registry instead of scanning the scene.
/// </summary>
public sealed class DiabloLootLabels : MonoBehaviour
{
    public const float Range = 18f;
    public const float LabelLift = 1.7f; // above the item art, not on it
    /// <summary>Show without Alt (tests, shotcheck).</summary>
    public static bool ForceShow;

    Canvas canvas;
    RectTransform root;
    Camera cam;
    readonly List<Text> pool = new List<Text>();
    readonly List<(LootDrop drop, Vector2 pos)> visible = new List<(LootDrop, Vector2)>();

    /// <summary>Labels shown this frame (test seam).</summary>
    public int ShownCount { get; private set; }

    public static bool Wanted()
    {
        if (ForceShow) return true;
        var kb = Keyboard.current;
        return kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
    }

    /// <summary>Label text and color for a drop (null text = no label: chests, empty drops).</summary>
    public static (string text, Color color) LabelFor(LootDrop drop)
    {
        if (drop == null || drop.isChest || drop.item == null || Equipment.IsBlank(drop.item)) return (null, Color.white);
        return (LootDrop.ItemLabel(drop.item), DiabloPalette.Rarity(drop.item.rarity));
    }

    void Awake()
    {
        var go = new GameObject("LootLabelCanvas");
        go.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(go, sortingOrder: 4, raycaster: false);
        root = go.GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        ShownCount = 0;
        if (!Wanted() || LootDrop.Live.Count == 0) { HideFrom(0); return; }
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
        if (hero == null || cam == null) { HideFrom(0); return; }

        visible.Clear();
        foreach (var drop in LootDrop.Live)
        {
            if (drop == null) continue;
            var d = drop.transform.position - hero.transform.position;
            d.y = 0f;
            if (d.magnitude > Range) continue;
            if (LabelFor(drop).text == null) continue;
            Vector3 sp = cam.WorldToScreenPoint(drop.transform.position + Vector3.up * LabelLift);
            if (sp.z <= 0f) continue;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var local)) continue;
            visible.Add((drop, local));
        }
        // Top to bottom, nudging overlapping labels upward so stacked drops stay readable.
        visible.Sort((a, b) => a.pos.y.CompareTo(b.pos.y));
        for (int i = 1; i < visible.Count; i++)
        {
            var prev = visible[i - 1].pos;
            var cur = visible[i].pos;
            if (Mathf.Abs(cur.x - prev.x) < 140f && cur.y - prev.y < 28f)
                visible[i] = (visible[i].drop, new Vector2(cur.x, prev.y + 28f));
        }
        for (int i = 0; i < visible.Count; i++)
        {
            var label = Get(i);
            var (text, color) = LabelFor(visible[i].drop);
            label.text = text;
            label.color = color;
            var plate = (RectTransform)label.transform.parent;
            // Size the plate from the text itself (no layout pass needed for a pooled label).
            plate.sizeDelta = new Vector2(label.preferredWidth + 18f, 26f);
            plate.anchoredPosition = visible[i].pos;
            if (!plate.gameObject.activeSelf) plate.gameObject.SetActive(true);
        }
        ShownCount = visible.Count;
        HideFrom(visible.Count);
    }

    Text Get(int i)
    {
        while (pool.Count <= i)
        {
            var go = new GameObject("LootLabel", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.03f, 0.05f, 0.78f);
            bg.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            var t = DiabloUIFactory.MakeText(go.transform, "Name", "", 17, Color.white, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tr = t.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            pool.Add(t);
        }
        // The pooled handle is the text; position/visibility live on its plate (parent).
        return pool[i];
    }

    void HideFrom(int start)
    {
        for (int i = start; i < pool.Count; i++)
            if (pool[i].transform.parent.gameObject.activeSelf) pool[i].transform.parent.gameObject.SetActive(false);
    }
}
