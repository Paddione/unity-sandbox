using UnityEngine;

/// <summary>
/// S5 ground presentation for one <see cref="ItemInstance"/>: the item's own
/// sprite on the floor with the rarity glow behind it (Magic blue, Rare
/// gold, Unique orange — <see cref="DiabloPalette"/>). Derived purely from
/// the S2 snapshot, so host, client and late join render identically.
/// </summary>
public sealed class LootDropView : MonoBehaviour
{
    public ItemInstance item;

    /// <summary>Rarity glow color (test seam; host == client == late join).</summary>
    public static Color GlowColor(ItemRarity rarity) => DiabloPalette.Rarity(rarity);

    /// <summary>Sprite key identity for host/client parity tests.</summary>
    public static string ArtKey(ItemInstance it) => DiabloMenuArt.ArtKeyFor(it);

    /// <summary>Attach-or-reuse the view on a drop root and show the item.</summary>
    public static LootDropView ShowOn(GameObject root, ItemInstance rolled)
    {
        if (root == null) return null;
        var view = root.GetComponent<LootDropView>() ?? root.AddComponent<LootDropView>();
        view.Show(rolled);
        return view;
    }

    public void Show(ItemInstance rolled)
    {
        item = rolled;
        if (item == null) return;
        // Remove the legacy cube visual if present.
        var legacy = transform.Find("LootVisual");
        if (legacy != null) DestroyImmediate(legacy.gameObject);

        var glowGo = transform.Find("RarityGlow");
        if (glowGo == null)
        {
            glowGo = new GameObject("RarityGlow").transform;
            glowGo.SetParent(transform, false);
            glowGo.localPosition = new Vector3(0f, 0.10f, 0f);
        }
        var glow = glowGo.GetComponent<SpriteRenderer>();
        if (glow == null) glow = glowGo.gameObject.AddComponent<SpriteRenderer>();
        glow.sprite = DiabloMenuArt.SlotBackground(item.rarity, 96);
        glow.color = new Color(1f, 1f, 1f, 0.9f);
        glowGo.localScale = Vector3.one * 1.1f;

        var iconGo = transform.Find("ItemIcon");
        if (iconGo == null)
        {
            iconGo = new GameObject("ItemIcon").transform;
            iconGo.SetParent(transform, false);
            iconGo.localPosition = new Vector3(0f, 0.55f, 0f);
        }
        var icon = iconGo.GetComponent<SpriteRenderer>();
        if (icon == null) icon = iconGo.gameObject.AddComponent<SpriteRenderer>();
        icon.sprite = DiabloMenuArt.ItemIcon(item);
        iconGo.localScale = Vector3.one * 1.4f;
        // Gentle bob so the drop reads as loot, not scenery.
        var bob = GetComponent<LootBob>() ?? gameObject.AddComponent<LootBob>();
        bob.target = iconGo;
    }

    /// <summary>Billboard both sprites at the camera (readable at any angle).</summary>
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var rot = cam.transform.rotation;
        var glow = transform.Find("RarityGlow");
        if (glow != null) glow.rotation = rot;
        var icon = transform.Find("ItemIcon");
        if (icon != null) icon.rotation = rot;
    }
}

/// <summary>Idle bob for the ground icon (visual only, no gameplay).</summary>
public sealed class LootBob : MonoBehaviour
{
    public Transform target;
    float t;

    void Update()
    {
        if (target == null) return;
        t += Time.deltaTime;
        target.localPosition = new Vector3(0f, 0.55f + Mathf.Sin(t * 2f) * 0.06f, 0f);
    }
}
