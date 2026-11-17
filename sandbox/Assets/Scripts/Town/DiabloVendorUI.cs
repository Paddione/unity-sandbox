using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lantern Hold trade window (UGUI, built at runtime like
/// <see cref="DiabloInventoryUI"/> — no prefab churn). Two columns: vendor
/// stock (click to buy) and hero bag (click to sell at 25 %), plus a potion
/// row and the gold purse. `V` toggles, `1` drinks a healing potion,
/// `Esc`/button closes. While open, click-movement is locked through the
/// existing <see cref="DiabloClickController.ExternalDrive"/> seam (same
/// effect as the inventory click guard, without touching S4-owned files).
/// Closing autosaves via <see cref="SaveService.SaveNow"/> (S2 seam).
/// Public contract for tests: IsOpen, SetOpen, BuyOffer, SellBagIndex,
/// BuyPotion, Refresh.
/// </summary>
public sealed class DiabloVendorUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Text goldText;
    Text vendorDetailText;
    Text bagDetailText;
    Transform vendorList;
    Transform bagList;
    bool building;

    VendorStock stock;
    Wallet wallet;
    Inventory inventory;

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
        wallet = GetComponent<Wallet>();
        inventory = GetComponent<Inventory>();
        stock = VendorStock.Roll(7);
        BuildUI();
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.vKey.wasPressedThisFrame) SetOpen(!IsOpen);
        if (kb.escapeKey.wasPressedThisFrame && IsOpen) SetOpen(false);
        if (kb.digit1Key.wasPressedThisFrame)
        {
            EnsureBuilt();
            if (Vendor.DrinkPotion(gameObject)) Refresh();
        }
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (IsOpen == open) return;
        if (open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        // Same effect as the inventory click guard: while the window is open
        // no click reaches movement or strikes (ExternalDrive ignores mouse
        // + WASD; the camera keeps tracking the hero).
        DiabloClickController.ExternalDrive = open;
        if (open) Refresh();
        else SaveService.SaveNow(gameObject);
    }

    /// <summary>Buy offers[index] (button hook); refreshes gold and lists.</summary>
    public void BuyOffer(int index)
    {
        EnsureBuilt();
        if (stock != null && stock.TryBuy(index, wallet, inventory))
        {
            DiabloAudio.PlayLoot();
            Refresh();
        }
    }

    /// <summary>Sell bag[bagIndex] back (button hook).</summary>
    public void SellBagIndex(int index)
    {
        EnsureBuilt();
        if (VendorStock.TrySell(index, wallet, inventory))
        {
            DiabloAudio.PlayUIClick();
            Refresh();
        }
    }

    /// <summary>Buy one healing potion (button hook).</summary>
    public void BuyPotion()
    {
        EnsureBuilt();
        if (VendorStock.BuyPotion(wallet, inventory))
        {
            DiabloAudio.PlayLoot();
            Refresh();
        }
    }

    /// <summary>Rebuild gold label and both columns from state.</summary>
    public void Refresh()
    {
        EnsureBuilt();
        if (goldText != null)
            goldText.text = wallet != null ? $"Gold: {wallet.Gold}" : "Gold: —";
        RebuildVendor();
        RebuildBag();
    }

    void RebuildVendor()
    {
        if (vendorList == null) return;
        foreach (Transform child in vendorList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        if (stock == null)
        {
            if (vendorDetailText != null) vendorDetailText.text = "No stock.";
            return;
        }
        if (vendorDetailText != null)
            vendorDetailText.text = stock.offers.Count == 0
                ? "Sold out. Leave Lantern Hold and return for fresh wares."
                : $"{stock.offers.Count} wares. Click to buy.";
        for (int i = 0; i < stock.offers.Count; i++)
        {
            int index = i; // closure copy
            var offer = stock.offers[i];
            var btn = MakeRowButton(vendorList.transform,
                $"Buy {LootDrop.ItemLabel(offer.item)} [{offer.item.rarity}] — {offer.price}g",
                DiabloPalette.Rarity(offer.item.rarity));
            btn.onClick.AddListener(() => BuyOffer(index));
        }
        var potionBtn = MakeRowButton(vendorList.transform,
            $"Buy Healing Potion (+{ItemDatabase.PotionHeal} HP, key 1) — {ItemDatabase.PotionPrice}g",
            DiabloPalette.Parchment);
        potionBtn.onClick.AddListener(BuyPotion);
    }

    void RebuildBag()
    {
        if (bagList == null) return;
        foreach (Transform child in bagList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        if (inventory == null)
        {
            if (bagDetailText != null) bagDetailText.text = "No bag.";
            return;
        }
        if (bagDetailText != null)
            bagDetailText.text = inventory.Count == 0
                ? "Bag is empty. Loot the fen, then sell here."
                : $"Bag {inventory.Count} of {Inventory.Capacity}. Click to sell (25 %).";
        for (int i = 0; i < inventory.Count; i++)
        {
            int index = i; // closure copy
            var item = inventory.Get(i);
            int price = VendorStock.SellPrice(item);
            var btn = MakeRowButton(bagList.transform,
                $"Sell {LootDrop.ItemLabel(item)} — +{price}g",
                DiabloPalette.Rarity(item.rarity));
            btn.onClick.AddListener(() => SellBagIndex(index));
        }
    }

    // ---------- build ----------

    void BuildUI()
    {
        building = true;
        var canvasGo = new GameObject("VendorCanvas");
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
        panel.color = Color.white;
        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760f, 560f);
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
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0f, 52f);
        MakeBannerLabel(headerGo.transform, "Lantern Hold vendor", 20);
        goldText = MakeLabel(panelGo.transform, "Gold: —", 17);

        var colsGo = new GameObject("Columns");
        colsGo.transform.SetParent(panelGo.transform, false);
        var cols = colsGo.AddComponent<HorizontalLayoutGroup>();
        cols.spacing = 10f;
        cols.childControlWidth = true;
        cols.childControlHeight = true;

        var vendorCol = new GameObject("VendorColumn");
        vendorCol.transform.SetParent(colsGo.transform, false);
        MakeLabel(vendorCol.transform, "Wares", 16);
        vendorDetailText = MakeLabel(vendorCol.transform, "", 13);
        var vendorRows = new GameObject("VendorRows");
        vendorRows.transform.SetParent(vendorCol.transform, false);
        vendorList = vendorRows.transform;

        var bagCol = new GameObject("BagColumn");
        bagCol.transform.SetParent(colsGo.transform, false);
        MakeLabel(bagCol.transform, "Your bag", 16);
        bagDetailText = MakeLabel(bagCol.transform, "", 13);
        var bagRows = new GameObject("BagRows");
        bagRows.transform.SetParent(bagCol.transform, false);
        bagList = bagRows.transform;

        MakeLabel(panelGo.transform, "V toggles, 1 drinks a potion, Esc closes (saves).", 13);
        var closeBtn = MakeSmallButton(panelGo.transform, "Close (saves)");
        closeBtn.onClick.AddListener(() => SetOpen(false));
        building = false;
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

    void MakeBannerLabel(Transform parent, string text, int size)
    {
        var label = MakeLabel(parent, text, size);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = DiabloPalette.TrimGold;
        var r = label.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    Button MakeRowButton(Transform parent, string text, Color accent)
    {
        var go = new GameObject("RowButton");
        go.transform.SetParent(parent, false);
        var button = go.AddComponent<Button>();
        var image = go.AddComponent<Image>();
        image.sprite = DiabloMenuArt.TabBackground(false);
        image.type = Image.Type.Sliced;
        var label = MakeLabel(go.transform, text, 14);
        label.color = accent;
        label.alignment = TextAnchor.MiddleLeft;
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(8f, 0f);
        lr.offsetMax = Vector2.zero;
        return button;
    }

    Button MakeSmallButton(Transform parent, string text)
    {
        var go = new GameObject("SmallButton");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = DiabloMenuArt.TabBackground(false);
        image.type = Image.Type.Sliced;
        var button = go.AddComponent<Button>();
        var label = MakeLabel(go.transform, text, 15);
        label.alignment = TextAnchor.MiddleCenter;
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(8f, 4f);
        lr.offsetMax = new Vector2(-8f, -4f);
        return button;
    }

}
