using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lantern Hold stash window (replayability S5), built at runtime like
/// <see cref="DiabloVendorUI"/> with the same HD scaler convention. Two
/// columns: chest (click to take into the bag) and bag (click to store).
/// While open, click movement is locked via
/// <see cref="DiabloClickController.ExternalDrive"/>; closing autosaves.
/// Public contract for tests: IsOpen, SetOpen, StoreBagIndex, TakeStashIndex, Refresh.
/// </summary>
public sealed class DiabloStashUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Text countText;
    Transform stashList;
    Transform bagList;
    Inventory inventory;

    void Awake()
    {
        EnsureBuilt();
        SetOpen(false);
    }

    void EnsureBuilt()
    {
        inventory = GetComponent<Inventory>();
        if (canvas != null) return;
        BuildUI();
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame && IsOpen) SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (IsOpen == open) return;
        if (open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        DiabloClickController.ExternalDrive = open;
        if (open) Refresh();
        else if (Diablo.IsLocalAuthority()) SaveService.SaveNow(gameObject);
    }

    public bool StoreBagIndex(int index)
    {
        EnsureBuilt();
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
        {
            var item = inventory?.Get(index);
            return item != null && relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Store, item);
        }
        bool ok = StashChest.Store(inventory, RunProfile.Stash, index);
        if (ok) { DiabloAudio.PlayUIClick(); Refresh(); }
        return ok;
    }

    public bool TakeStashIndex(int index)
    {
        EnsureBuilt();
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
        {
            var item = RunProfile.Stash.Get(index);
            return item != null && relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Take, item);
        }
        bool ok = StashChest.Take(RunProfile.Stash, inventory, index);
        if (ok) { DiabloAudio.PlayLoot(); Refresh(); }
        return ok;
    }

    /// <summary>Header line (test seam).</summary>
    public string CountLine() =>
        $"Chest {RunProfile.Stash.Count} of {Stash.Capacity}   Bag {(inventory != null ? inventory.Count : 0)} of {Inventory.Capacity}";

    public void Refresh()
    {
        EnsureBuilt();
        if (countText != null) countText.text = CountLine();
        Rebuild(stashList, RunProfile.Stash.Count, i => RunProfile.Stash.Get(i), "Take", i => TakeStashIndex(i));
        Rebuild(bagList, inventory != null ? inventory.Count : 0, i => inventory.Get(i), "Store", i => StoreBagIndex(i));
    }

    void Rebuild(Transform list, int count, System.Func<int, ItemInstance> get, string verb, System.Action<int> onClick)
    {
        if (list == null) return;
        foreach (Transform child in list)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var item = get(i);
            if (item == null) continue;
            var btn = MakeRowButton(list, $"{verb} {LootDrop.ItemLabel(item)}", DiabloPalette.Rarity(item.rarity));
            btn.onClick.AddListener(() => onClick(index));
        }
    }

    // ---------- build ----------

    void BuildUI()
    {
        var canvasGo = new GameObject("StashCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panel = panelGo.AddComponent<Image>();
        panel.sprite = DiabloMenuArt.PanelBackground();
        panel.type = Image.Type.Sliced;
        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760f, 640f);
        rect.anchoredPosition = Vector2.zero;
        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(panelGo.transform, false);
        var headerImg = headerGo.AddComponent<Image>();
        headerImg.sprite = DiabloMenuArt.HeaderBanner();
        headerImg.type = Image.Type.Sliced;
        headerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
        var title = MakeLabel(headerGo.transform, "Lantern Hold stash", 20);
        title.alignment = TextAnchor.MiddleCenter;
        title.color = DiabloPalette.TrimGold;
        var tr = title.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        countText = MakeLabel(panelGo.transform, "", 16);

        var colsGo = new GameObject("Columns");
        colsGo.transform.SetParent(panelGo.transform, false);
        var cols = colsGo.AddComponent<HorizontalLayoutGroup>();
        cols.spacing = 10f;
        cols.childControlWidth = true;
        cols.childControlHeight = true;
        var colsLayout = colsGo.AddComponent<LayoutElement>();
        colsLayout.preferredHeight = 480f;
        stashList = MakeColumn(colsGo.transform, "Chest (click to take)");
        bagList = MakeColumn(colsGo.transform, "Your bag (click to store)");

        MakeLabel(panelGo.transform, "Esc closes (saves).", 13);
        var close = MakeRowButton(panelGo.transform, "Close (saves)", DiabloPalette.Parchment);
        close.onClick.AddListener(() => SetOpen(false));
    }

    Transform MakeColumn(Transform parent, string caption)
    {
        var col = new GameObject("Column");
        col.transform.SetParent(parent, false);
        var v = col.AddComponent<VerticalLayoutGroup>();
        v.spacing = 2f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        v.childForceExpandHeight = false;
        MakeLabel(col.transform, caption, 15);
        var rows = new GameObject("Rows");
        rows.transform.SetParent(col.transform, false);
        var rv = rows.AddComponent<VerticalLayoutGroup>();
        rv.spacing = 1f;
        rv.childControlWidth = true;
        rv.childControlHeight = false;
        rv.childForceExpandHeight = false;
        return rows.transform;
    }

    Text MakeLabel(Transform parent, string text, int size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.color = DiabloPalette.Parchment;
        return label;
    }

    Button MakeRowButton(Transform parent, string text, Color accent)
    {
        var go = new GameObject("RowButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);
        var image = go.AddComponent<Image>();
        image.sprite = DiabloMenuArt.TabBackground(false);
        image.type = Image.Type.Sliced;
        var button = go.AddComponent<Button>();
        var label = MakeLabel(go.transform, text, 13);
        label.color = accent;
        label.alignment = TextAnchor.MiddleLeft;
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(8f, 0f);
        lr.offsetMax = Vector2.zero;
        return button;
    }
}
