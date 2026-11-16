using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Blood Moor inventory panel (UGUI, built at runtime — no prefab churn).
/// `I` toggles the bag: 3 equipped slots + stats on top, bag buttons below,
/// click a bag item to equip it (swap goes back to the bag). While open the
/// hero keeps moving/aiming but clicks go to the panel, never to strikes
/// (see <see cref="DiabloClickController.ShouldBlockClicks"/>).
/// </summary>
public sealed class DiabloInventoryUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Text statsText;
    Text armorText;
    Text weaponText;
    Text helmetText;
    Transform bagList;

    Inventory inventory;
    Equipment equipment;
    PlayerHealth health;

    void Awake()
    {
        EnsureBuilt();
        SetOpen(false);
    }

    /// <summary>
    /// Headless EditMode `AddComponent` can skip `Awake`-time wiring, so every
    /// entry point heals itself. No-op once built (runtime `Awake` path).
    /// </summary>
    void EnsureBuilt()
    {
        if (canvas != null) return;
        inventory = GetComponent<Inventory>();
        equipment = GetComponent<Equipment>();
        health = GetComponent<PlayerHealth>();
        BuildUI();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.iKey.wasPressedThisFrame)
            SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        if (open) Refresh();
    }

    /// <summary>Equip bag[index] (button hook); refreshes stats and HP max.</summary>
    public void EquipIndex(int index)
    {
        EnsureBuilt();
        if (inventory == null || equipment == null) return;
        if (inventory.EquipFromBag(index, equipment))
        {
            if (health != null) health.Refresh();
            DiabloAudio.PlayUIClick();
            Refresh();
        }
    }

    /// <summary>Rebuild slot labels, stats line and bag buttons from state.</summary>
    public void Refresh()
    {
        EnsureBuilt();
        if (equipment != null)
        {
            armorText.text = SlotLine("Armor", equipment.armor);
            weaponText.text = SlotLine("Weapon", equipment.weapon);
            helmetText.text = SlotLine("Helmet", equipment.helmet);
            statsText.text =
                $"DMG {equipment.GetDamage():0.#}  HP {equipment.GetMaxHP():0.#}  " +
                $"CRIT {equipment.GetCrit():0.#}%  XP {equipment.GetXPMul():0.#}%";
        }
        foreach (Transform child in bagList) Destroy(child.gameObject);
        if (inventory == null) return;
        for (int i = 0; i < inventory.Count; i++)
        {
            var item = inventory.Get(i);
            int index = i; // closure copy
            var button = MakeButton($"{LootDrop.ItemLabel(item)} [{item.rarity}]", LootDrop.ColorFor(item.rarity));
            button.onClick.AddListener(() => EquipIndex(index));
            button.transform.SetParent(bagList, false);
        }
    }

    static string SlotLine(string slot, ItemInstance item) =>
        item == null ? $"{slot}: —" : $"{slot}: {LootDrop.ItemLabel(item)} [{item.rarity}]";

    void BuildUI()
    {
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panel = panelGo.AddComponent<Image>();
        panel.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(340f, 500f);
        rect.anchoredPosition = new Vector2(-16f, 0f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        MakeLabel(panelGo.transform, "Inventory  (I to close)", 22);
        statsText = MakeLabel(panelGo.transform, "", 16);
        armorText = MakeLabel(panelGo.transform, "", 16);
        weaponText = MakeLabel(panelGo.transform, "", 16);
        helmetText = MakeLabel(panelGo.transform, "", 16);
        MakeLabel(panelGo.transform, "— Bag —", 16);
        var bagGo = new GameObject("BagList");
        bagGo.transform.SetParent(panelGo.transform, false);
        bagList = bagGo.transform;
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
        return label;
    }

    Button MakeButton(string text, Color accent)
    {
        var go = new GameObject("BagButton");
        if (bagList != null) go.transform.SetParent(bagList, false);
        var button = go.AddComponent<Button>();
        var image = go.AddComponent<Image>();
        image.color = new Color(0.16f, 0.16f, 0.22f, 1f);
        var rect = go.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector2(300f, 26f);
        // Label as a child (standard uGUI button text): adding Text next to
        // Button+Image on one object returns null in headless runs.
        var label = MakeLabel(go.transform, text, 15);
        label.color = accent;
        label.alignment = TextAnchor.MiddleLeft;
        var labelRect = label.GetComponent<RectTransform>();
        if (labelRect != null)
        {
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = Vector2.zero;
        }
        return button;
    }
}
