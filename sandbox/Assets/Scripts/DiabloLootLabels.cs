using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Ground loot names: one rarity-colored plate above each item drop near the
/// hero, pooled. Always shown by default (<see cref="DiabloSettings.LootLabelsAlways"/>),
/// otherwise while Alt is held. The loot filter hides non-Unique plates
/// (<see cref="DiabloSettings.LootFilterUniqueOnly"/>). Plates are click targets
/// for <see cref="LootClick"/> via <see cref="PlateAt"/> (screen rects, no UI raycasts).
/// Reads the <see cref="LootDrop.Live"/> registry instead of scanning the scene.
/// </summary>
public sealed class DiabloLootLabels : MonoBehaviour
{
    public const float Range = 18f;
    public const float LabelLift = 1.7f; // above the item art, not on it
    /// <summary>Show without Alt (tests, shotcheck).</summary>
    public static bool ForceShow;

    /// <summary>Screen rects of the plates shown last frame (click targets).</summary>
    static readonly List<(LootDrop drop, Rect rect)> plates = new List<(LootDrop, Rect)>();
    static readonly Vector3[] corners = new Vector3[4];

    /// <summary>Plates shown last frame (test seam).</summary>
    public static int PlateCount => plates.Count;
    /// <summary>Screen rect of plate <paramref name="i"/> (test seam).</summary>
    public static Rect PlateRect(int i) => plates[i].rect;

    /// <summary>The drop whose visible plate contains <paramref name="screen"/>, else null.</summary>
    public static LootDrop PlateAt(Vector2 screen)
    {
        for (int i = plates.Count - 1; i >= 0; i--)
            if (plates[i].rect.Contains(screen) && LootClick.Pickable(plates[i].drop)) return plates[i].drop;
        return null;
    }

    Canvas canvas;
    RectTransform root;
    Camera cam;
    readonly List<Text> pool = new List<Text>();
    readonly List<(LootDrop drop, Vector2 pos)> visible = new List<(LootDrop, Vector2)>();

    /// <summary>Labels shown this frame (test seam).</summary>
    public int ShownCount { get; private set; }

    public static bool Wanted()
    {
        if (ForceShow || DiabloSettings.LootLabelsAlways) return true;
        var kb = Keyboard.current;
        return kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
    }

    /// <summary>Label text and color for a drop (null text = no label: chests, empty drops).</summary>
    public static (string text, Color color) LabelFor(LootDrop drop)
    {
        if (drop == null || drop.isChest || drop.collected || drop.item == null || Equipment.IsBlank(drop.item)) return (null, Color.white);
        if (!PassesFilter(drop.item.rarity, DiabloSettings.LootFilterUniqueOnly)) return (null, Color.white);
        return (LootDrop.ItemLabel(drop.item), DiabloPalette.Rarity(drop.item.rarity));
    }

    /// <summary>Loot filter rule: "Unique only" hides every other rarity's plate.</summary>
    public static bool PassesFilter(ItemRarity rarity, bool uniqueOnly) =>
        !uniqueOnly || rarity == ItemRarity.Unique;

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
        LootClick.PollKeyCapture();
        plates.Clear();
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
            // Overlay canvas: world corners are screen pixels.
            plate.GetWorldCorners(corners);
            plates.Add((visible[i].drop, Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y)));
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
