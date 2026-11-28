using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Lantern Hold trade window (V toggles, Esc closes and saves; the flask owns key 1).
/// Fixes column layout, adds 3D item icons + rarity slots + PortraitVendor + live
/// hover comparison tooltip + affordability state, while preserving every test seam
/// including reflected private <c>stock</c> and public IsOpen, SetOpen, BuyOffer,
/// SellBagIndex, Refresh, TryCloseOnOutsideClick, Notice.
/// Click outside the panel closes; co-op trades route through the host relay.
/// </summary>
public sealed class DiabloVendorUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Transform panel;
    int openedFrame = -1;
    Text goldText;
    Text vendorDetailText;
    Text bagDetailText;
    Text inspectText;
    Transform vendorList;
    Transform bagList;

    VendorStock stock; // DO NOT rename: reflected by VendorLoopTests

    /// <summary>Offers on display (automation seam for the shotcheck compare pose).</summary>
    public VendorStock Stock => stock;
    Wallet wallet;
    Inventory inventory;
    Equipment equipment;

    void Awake()
    {
        EnsureBuilt();
    }

    void EnsureBuilt()
    {
        wallet = GetComponent<Wallet>();
        inventory = GetComponent<Inventory>();
        equipment = GetComponent<Equipment>();
        if (canvas != null) return;
        stock = GetComponent<DiabloNetworkPlayer>()?.CurrentNetworkStock
            ?? VendorStock.Roll(Vendor.StockSeed(VendorZoneSeed(), VendorClientId()));
        BuildUI();
    }

    /// <summary>Current assortment (relay-approved buys apply here).</summary>
    public VendorStock CurrentStock
    {
        get { EnsureBuilt(); return stock; }
    }

    /// <summary>Zone seed of the scene vendor (7 when none is placed).</summary>
    static int VendorZoneSeed()
    {
        var go = GameObject.Find(Vendor.VendorObjectName);
        var vendor = go != null ? go.GetComponent<Vendor>() : null;
        return vendor != null ? vendor.zoneSeed : 7;
    }

    /// <summary>Owning client id (0 offline/host — keeps the zone stock).</summary>
    ulong VendorClientId()
    {
        var p = GetComponent<DiabloNetworkPlayer>();
        return p != null && p.IsSpawned ? p.OwnerClientId : 0;
    }

    /// <summary>
    /// Bind the town vendor's stock so Restock() reaches the open window.
    /// Solo only: co-op peers keep their per-player (host-validated) stock.
    /// </summary>
    public void BindStock(VendorStock vendorStock)
    {
        EnsureBuilt();
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (vendorStock == null || (relay != null && relay.IsSpawned)) return;
        stock = vendorStock;
        if (IsOpen) Refresh();
    }

    public void ApplyServerStock(VendorStock serverStock)
    {
        EnsureBuilt();
        stock = serverStock;
        if (IsOpen) Refresh();
    }

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        // Click-away closes: a click outside the panel dismisses the window.
        // (V/Esc/button stay; the opening click must not close it right away.)
        if (IsOpen && mouse != null && mouse.leftButton.wasPressedThisFrame
            && Time.frameCount > openedFrame)
            TryCloseOnOutsideClick(mouse.position.ReadValue());
        if (DiabloUIFactory.IsRemoteMirror(this)) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.vKey.wasPressedThisFrame && (!DiabloUIFactory.IsAnyModalOpen(gameObject) || IsOpen))
        {
            SetOpen(!IsOpen);
            return;
        }
        if (kb.escapeKey.wasPressedThisFrame && IsOpen)
        {
            SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
            return;
        }
    }

    /// <summary>
    /// Close when the click lands outside the panel (headless-callable with an
    /// explicit screen position; the live Update path passes the mouse). True
    /// when the click closed the window.
    /// </summary>
    public bool TryCloseOnOutsideClick(Vector2 screenPos)
    {
        // Resolve live: a cached panel Transform can report null while its
        // GameObject still stands, so fall back to the path lookup (cheap,
        // runs only on click frames).
        var root = panel != null ? panel : transform.Find("VendorCanvas/Panel");
        if (!IsOpen || root == null) return false;
        var es = EventSystem.current;
        if (es == null) return false;
        var hits = new List<RaycastResult>();
        es.RaycastAll(new PointerEventData(es) { position = screenPos }, hits);
        foreach (var h in hits)
        {
            var t = h.gameObject != null ? h.gameObject.transform : null;
            if (t == root || (t != null && t.IsChildOf(root))) return false;
        }
        SetOpen(false);
        return true;
    }

    public void SetOpen(bool open)
    {
        if (!open) ShowSellConfirm(false);
        EnsureBuilt();
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        if (IsOpen == open) return;
        if (open)
        {
            DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
            DiabloAudio.PlayUIClick();
        }
        IsOpen = open;
        if (open) openedFrame = Time.frameCount;
        canvas.gameObject.SetActive(open);
        if (!open) ItemTooltip.HideFor(gameObject);
        // Same effect as the inventory click guard: while the window is open
        // no click reaches movement or strikes (ExternalDrive ignores mouse
        // + WASD; the camera keeps tracking the hero).
        DiabloClickController.ExternalDrive = open;
        if (open) Refresh();
        else SaveService.SaveNow(gameObject);
    }

    /// <summary>Latest action feedback (funds/capacity/rejection). Test seam.</summary>
    public string Notice { get; private set; } = "";
    Text noticeText;
    void SetNotice(string text)
    {
        Notice = text ?? "";
        if (noticeText != null) noticeText.text = Notice;
    }

    /// <summary>S5: sell by instance ID (visible Sell action routes here).</summary>
    public bool SellById(string instanceId)
    {
        EnsureBuilt();
        if (inventory == null) return false;
        int index = inventory.IndexOf(instanceId);
        if (index < 0) { SetNotice(Loc.T("inv.notice.gone")); Refresh(); return false; }
        SellBagIndex(index);
        return true;
    }

    /// <summary>
    /// Buy offers[index] (button hook). Solo/offline applies instantly; with
    /// a session running the host validates (range, row, once-only) and the
    /// purse/bag check runs at apply time — here and on approval alike.
    /// </summary>
    public void BuyOffer(int index)
    {
        EnsureBuilt();
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned && CoopSessionRunning())
        {
            if (BuyPrecheck(index)) relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Buy, stock.offers[index].item);
            else SetNotice(BuyRejection(index));
            Refresh();
            return;
        }
        if (stock != null && stock.TryBuy(index, wallet, inventory))
        {
            DiabloAudio.PlayVendorBuy();
            SetNotice(Loc.F("vendor.notice.bought", LootDrop.ItemLabel(stock.offers[index].item)));
        }
        else SetNotice(BuyRejection(index));
        Refresh();
    }

    string BuyRejection(int index)
    {
        if (stock == null || index < 0 || index >= stock.offers.Count) return Loc.T("vendor.notice.gone");
        if (inventory != null && inventory.FreeSlots <= 0)
            return Loc.F("vendor.notice.bagfull", inventory != null ? inventory.Slots : Inventory.Capacity, inventory != null ? inventory.Slots : Inventory.Capacity);
        if (wallet != null) return Loc.F("vendor.notice.needgold", stock.offers[index].price);
        return Loc.T("vendor.notice.cannotbuy");
    }

    /// <summary>Local funds/bag/row pre-check before asking the host.</summary>
    bool BuyPrecheck(int index)
    {
        if (stock == null || wallet == null || inventory == null) return false;
        if (index < 0 || index >= stock.offers.Count) return false;
        if (inventory.FreeSlots <= 0) return false;
        return wallet.Gold >= stock.offers[index].price;
    }

    /// <summary>
    /// Sell bag[bagIndex] back (button hook). Same routing as buys; resale
    /// touches no shared stock, so the host only gates range and price.
    /// </summary>
    public void SellBagIndex(int index)
    {
        EnsureBuilt();
        var item = inventory?.Get(index);
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned && CoopSessionRunning())
        {
            if (item != null) relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Sell, item);
            else SetNotice(Loc.T("vendor.notice.nothing"));
            Refresh();
            return;
        }
        if (VendorStock.TrySell(index, wallet, inventory))
        {
            DiabloAudio.PlayVendorSell();
            SetNotice(item != null ? Loc.F("vendor.notice.sold", LootDrop.ItemLabel(item)) : Loc.T("vendor.notice.sold.plain"));
        }
        else SetNotice(Loc.T("vendor.notice.salerejected"));
        Refresh();
    }

    Button sellAllButton, sellYesButton, sellNoButton;
    Text sellConfirmText;

    /// <summary>True while the sell-all confirmation is showing.</summary>
    public bool SellAllConfirmVisible { get; private set; }

    void ShowSellConfirm(bool show)
    {
        SellAllConfirmVisible = show;
        if (sellConfirmText != null) sellConfirmText.gameObject.SetActive(show);
        if (sellYesButton != null) sellYesButton.gameObject.SetActive(show);
        if (sellNoButton != null) sellNoButton.gameObject.SetActive(show);
        if (sellAllButton != null) sellAllButton.gameObject.SetActive(!show);
    }

    /// <summary>
    /// "Alle Rares verkaufen": shows the confirmation with count and gold sum.
    /// Returns the previewed gold (0 = nothing to sell, a notice explains it).
    /// </summary>
    public int RequestSellAllRares()
    {
        EnsureBuilt();
        int gold = VendorStock.PreviewSellAllRares(inventory, out int count);
        if (count == 0)
        {
            ShowSellConfirm(false);
            SetNotice(Loc.T("vendor.sellall.none"));
            Refresh();
            return 0;
        }
        if (sellConfirmText != null) sellConfirmText.text = Loc.F("vendor.sellall.confirm", count, gold);
        ShowSellConfirm(true);
        return gold;
    }

    /// <summary>Confirmed: sell every Rare in the bag (co-op: the host applies it).</summary>
    public void ConfirmSellAllRares()
    {
        EnsureBuilt();
        ShowSellConfirm(false);
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned && CoopSessionRunning())
        {
            relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.SellRares);
            Refresh();
            return;
        }
        int gold = VendorStock.SellAllRares(wallet, inventory, out int count);
        if (count > 0)
        {
            DiabloAudio.PlayVendorSell();
            SetNotice(Loc.F("vendor.sellall.done", count, gold));
        }
        else SetNotice(Loc.T("vendor.sellall.none"));
        Refresh();
    }

    public void CancelSellAllRares()
    {
        ShowSellConfirm(false);
    }

    /// <summary>True while a co-op session replicates (host + client alike).</summary>
    static bool CoopSessionRunning()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return false;
        var local = nm.SpawnManager != null ? nm.SpawnManager.GetLocalPlayerObject() : null;
        return local != null;
    }

    /// <summary>Rebuild gold label, wares column, bag column and inspect bar.</summary>
    public void Refresh()
    {
        EnsureBuilt();
        if (goldText != null)
            goldText.text = wallet != null ? Loc.F("vendor.purse", wallet.Gold) : Loc.T("vendor.purse.none");
        RebuildVendor();
        RebuildBag();
        if (noticeText != null) noticeText.text = Notice;
    }

    void RebuildVendor()
    {
        if (vendorList == null) return;
        foreach (Transform child in vendorList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        if (stock == null)
        {
            if (vendorDetailText != null) vendorDetailText.text = Loc.T("vendor.notice.nostock");
            return;
        }
        if (vendorDetailText != null)
            vendorDetailText.text = stock.offers.Count == 0
                ? Loc.T("vendor.soldout")
                : Loc.F("vendor.offers.count", stock.offers.Count);
        int gold = wallet != null ? wallet.Gold : 0;
        for (int i = 0; i < stock.offers.Count; i++)
        {
            int index = i;
            var offer = stock.offers[i];
            bool afford = gold >= offer.price;
            bool classFit = Equipment.CanEquip(offer.item, RunProfile.ClassId);
            string sub = afford
                ? (classFit ? $"[{ItemText.Rarity(offer.item.rarity)}]  {ItemText.Slot(offer.item.slot)}" : Loc.F("vendor.row.otherclass", DiabloInventoryUI.WorseColor))
                : Loc.F("vendor.row.needgold", DiabloInventoryUI.WorseColor, offer.price);
            var btn = MakeItemRow(
                vendorList,
                offer.item.baseType,
                offer.item.rarity,
                LootDrop.ItemLabel(offer.item),
                sub,
                $"{offer.price}g",
                DiabloPalette.Rarity(offer.item.rarity),
                afford ? DiabloPalette.GoldBright : DiabloPalette.Blood,
                () => BuyOffer(index),
                () => InspectItem(offer.item, Loc.F("vendor.buy.offer", offer.price)));
            btn.interactable = true;
            ItemTooltip.AttachHover(btn.gameObject, gameObject, () => offer.item, Loc.F("vendor.buy.offer", offer.price));
        }
    }

    void RebuildBag()
    {
        if (bagList == null) return;
        foreach (Transform child in bagList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        if (inventory == null)
        {
            if (bagDetailText != null) bagDetailText.text = Loc.T("vendor.notice.nobag");
            return;
        }
        if (bagDetailText != null)
            bagDetailText.text = inventory.Count == 0
                ? Loc.T("vendor.bag.empty")
                : Loc.F("vendor.bag.header", inventory.Count, inventory.Slots);
        for (int i = 0; i < inventory.Count; i++)
        {
            int index = i;
            var item = inventory.Get(i);
            int price = VendorStock.SellPrice(item);
            var sellRow = MakeItemRow(
                bagList,
                item.baseType,
                item.rarity,
                LootDrop.ItemLabel(item),
                $"[{ItemText.Rarity(item.rarity)}]  {ItemText.Slot(item.slot)}",
                $"+{price}g",
                DiabloPalette.Rarity(item.rarity),
                DiabloInventoryUI.BetterColor != null ? DiabloPalette.TrimGold : DiabloPalette.Parchment,
                () => SellBagIndex(index),
                () => InspectItem(item, Loc.F("vendor.sell", price)));
            ItemTooltip.AttachHover(sellRow.gameObject, gameObject, () => item, Loc.F("vendor.sell", price));
        }
        // S5 details: the newest bag item pre-fills the compare box until the hero hovers a row.
        if (inspectText != null && inventory.Count > 0)
        {
            var last = inventory.Get(inventory.Count - 1);
            inspectText.supportRichText = true;
            inspectText.text = ItemDetailsView.Render(last, null);
        }
    }

    void InspectItem(ItemInstance item, string tradeTag)
    {
        if (inspectText == null || item == null) return;
        var eq = equipment != null ? equipment.GetEquipped(item.slot) : null;
        string eqName = eq != null ? LootDrop.ItemLabel(eq) : Loc.T("town.emptyslot");
        inspectText.text = $"<b>{LootDrop.ItemLabel(item)}</b> ({tradeTag}) vs {eqName}:\n{DiabloInventoryUI.CompareSummary(item, eq)}";
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("VendorCanvas");
        canvasGo.SetActive(false);
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 11);

        var panelGo = DiabloUIFactory.MakePanel(canvasGo.transform, "Panel", new Vector2(840f, 740f), Vector2.zero);
        panel = panelGo.transform;
        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        DiabloUIFactory.MakeHeader(panelGo.transform, Loc.T("vendor.title"), DiabloMenuArt.MenuArtSprite("PortraitVendor"), () => SetOpen(false));

        // Merchant Purse Bar
        var purseRow = DiabloUIFactory.MakeHorizontalRow(panelGo.transform, "PurseRow", 10f, 34f);
        purseRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        var coinSpr = DiabloMenuArt.MenuArtSprite("GoldCoin");
        if (coinSpr != null)
        {
            var cGo = new GameObject("CoinIcon", typeof(RectTransform));
            cGo.transform.SetParent(purseRow.transform, false);
            DiabloUIFactory.SetLayout(cGo, prefH: 28f, prefW: 28f);
            var ci = cGo.AddComponent<Image>();
            ci.sprite = coinSpr;
            ci.preserveAspect = true;
        }
        goldText = DiabloUIFactory.MakeText(purseRow.transform, "GoldLabel", Loc.T("vendor.purse.none"), 18, DiabloPalette.GoldBright);
        goldText.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(goldText.gameObject, prefH: 30f, flexW: 1f);
        var hintLbl = DiabloUIFactory.MakeText(purseRow.transform, "VendorHint", Loc.T("vendor.footer"), 13, DiabloPalette.ParchmentDim, TextAnchor.MiddleRight);
        DiabloUIFactory.SetLayout(hintLbl.gameObject, prefH: 30f, prefW: 340f);

        // Two cleanly aligned columns (Vendor Wares | Your Bag) with VerticalLayoutGroup!
        var colsGo = new GameObject("Columns", typeof(RectTransform));
        colsGo.transform.SetParent(panelGo.transform, false);
        DiabloUIFactory.SetLayout(colsGo, prefH: 404f, flexW: 1f);
        var cols = colsGo.AddComponent<HorizontalLayoutGroup>();
        cols.spacing = 12f;
        cols.childControlWidth = true;
        cols.childControlHeight = true;
        cols.childForceExpandWidth = true;
        cols.childForceExpandHeight = true;

        vendorList = MakeColumn(colsGo.transform, "VendorColumn", Loc.T("vendor.wares"), out vendorDetailText);
        bagList = MakeColumn(colsGo.transform, "BagColumn", Loc.T("vendor.yourbag"), out bagDetailText);

        // Inspection comparison box
        var inspBox = new GameObject("InspectBox", typeof(RectTransform));
        inspBox.transform.SetParent(panelGo.transform, false);
        DiabloUIFactory.SetLayout(inspBox, prefH: 88f, flexW: 1f); // 40 px went to the sell-all row
        var ibImg = inspBox.AddComponent<Image>();
        ibImg.sprite = DiabloMenuArt.TooltipBackground();
        ibImg.type = Image.Type.Sliced;
        var iv = inspBox.AddComponent<VerticalLayoutGroup>();
        iv.padding = new RectOffset(12, 12, 8, 8);
        iv.childControlWidth = true;
        iv.childControlHeight = false;
        inspectText = DiabloUIFactory.MakeText(inspBox.transform, "InspectText",
            Loc.T("vendor.hint"), 14, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(inspectText.gameObject, prefH: 72f, flexW: 1f);
        DiabloUIFactory.FitToBox(inspectText, 14);

        // cinderreach-vendor-sell-all: one button sells every Rare in the bag after a confirm with the gold sum.
        var sellRow = DiabloUIFactory.MakeHorizontalRow(panelGo.transform, "SellAllRow", 8f, 34f);
        sellAllButton = DiabloUIFactory.MakeSmallButton(sellRow.transform, Loc.T("vendor.sellall"), () => RequestSellAllRares(), 32f);
        sellAllButton.name = "SellAllRaresButton";
        sellConfirmText = DiabloUIFactory.MakeText(sellRow.transform, "SellAllConfirmText", "", 14, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(sellConfirmText.gameObject, prefH: 32f, flexW: 2f);
        sellYesButton = DiabloUIFactory.MakeSmallButton(sellRow.transform, Loc.T("vendor.sellall.yes"), () => ConfirmSellAllRares(), 32f);
        sellYesButton.name = "SellAllYesButton";
        sellNoButton = DiabloUIFactory.MakeSmallButton(sellRow.transform, Loc.T("vendor.sellall.no"), () => CancelSellAllRares(), 32f);
        sellNoButton.name = "SellAllNoButton";
        ShowSellConfirm(false);

        noticeText = DiabloUIFactory.MakeText(panelGo.transform, "Notice", "", 14, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(noticeText.gameObject, prefH: 22f, flexW: 1f);
        // Explicit footer close (direct child "SmallButton": the save-on-close contract tests click it).
        DiabloUIFactory.MakeSmallButton(panelGo.transform, Loc.T("town.close"), () => SetOpen(false), 34f);
    }

    Transform MakeColumn(Transform parent, string name, string heading, out Text subtitle)
    {
        var col = new GameObject(name, typeof(RectTransform));
        col.transform.SetParent(parent, false);
        var colBg = col.AddComponent<Image>();
        colBg.sprite = DiabloMenuArt.TooltipBackground();
        colBg.type = Image.Type.Sliced;

        var cv = col.AddComponent<VerticalLayoutGroup>();
        cv.padding = new RectOffset(10, 10, 8, 8);
        cv.spacing = 6f;
        cv.childControlWidth = true;
        cv.childControlHeight = false;

        var head = DiabloUIFactory.MakeText(col.transform, "Heading", heading, 16, DiabloPalette.TrimGold);
        head.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(head.gameObject, prefH: 22f, flexW: 1f);

        subtitle = DiabloUIFactory.MakeText(col.transform, "Subtitle", "", 12, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(subtitle.gameObject, prefH: 18f, flexW: 1f);

        // Scrollable item container so up to 20 bag items NEVER overflow!
        var scrollGo = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(col.transform, false);
        DiabloUIFactory.SetLayout(scrollGo, prefH: 334f, flexW: 1f, flexH: 1f);
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 28f;

        var vpGo = new GameObject("Viewport", typeof(RectTransform));
        vpGo.transform.SetParent(scrollGo.transform, false);
        vpGo.AddComponent<RectMask2D>();
        var vpImg = vpGo.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.01f);
        var vpr = vpGo.GetComponent<RectTransform>();
        vpr.anchorMin = Vector2.zero;
        vpr.anchorMax = Vector2.one;
        vpr.offsetMin = vpr.offsetMax = Vector2.zero;
        sr.viewport = vpr;

        var rows = new GameObject(name == "VendorColumn" ? "VendorRows" : "BagRows", typeof(RectTransform));
        rows.transform.SetParent(vpGo.transform, false);
        var rr = rows.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0f, 1f);
        rr.anchorMax = new Vector2(1f, 1f);
        rr.pivot = new Vector2(0.5f, 1f);
        rr.offsetMin = rr.offsetMax = Vector2.zero;
        var rv = rows.AddComponent<VerticalLayoutGroup>();
        rv.spacing = 4f;
        rv.childControlWidth = true;
        rv.childControlHeight = false;
        rv.childForceExpandHeight = false;
        var csf = rows.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = rr;
        return rows.transform;
    }

    Button MakeItemRow(Transform parent, string baseType, ItemRarity rarity, string title, string sub, string priceStr,
        Color titleCol, Color priceCol, UnityEngine.Events.UnityAction onClick, UnityEngine.Events.UnityAction onHover)
    {
        var go = new GameObject("TradeRow", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefH: 48f, flexW: 1f);
        var bg = go.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.ButtonBackground(false);
        bg.type = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var hz = go.AddComponent<HorizontalLayoutGroup>();
        hz.padding = new RectOffset(6, 8, 4, 4);
        hz.spacing = 8f;
        hz.childAlignment = TextAnchor.MiddleLeft;
        hz.childControlWidth = true;
        hz.childControlHeight = false;
        hz.childForceExpandWidth = false;

        var slot = new GameObject("Slot", typeof(RectTransform));
        slot.transform.SetParent(go.transform, false);
        DiabloUIFactory.SetLayout(slot, prefH: 38f, prefW: 38f);
        var sImg = slot.AddComponent<Image>();
        sImg.sprite = DiabloMenuArt.SlotBackground(rarity, 64);
        sImg.type = Image.Type.Sliced;
        sImg.raycastTarget = false;

        var iGo = new GameObject("Icon", typeof(RectTransform));
        iGo.transform.SetParent(slot.transform, false);
        var iImg = iGo.AddComponent<Image>();
        iImg.sprite = DiabloMenuArt.ItemIcon(baseType, rarity);
        iImg.preserveAspect = true;
        iImg.raycastTarget = false;
        var ir = iGo.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero;
        ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(4f, 4f);
        ir.offsetMax = new Vector2(-4f, -4f);

        var col = new GameObject("TextCol", typeof(RectTransform));
        col.transform.SetParent(go.transform, false);
        DiabloUIFactory.SetLayout(col, prefH: 40f, flexW: 1f);
        var v = col.AddComponent<VerticalLayoutGroup>();
        v.childControlWidth = true;
        v.childControlHeight = false;
        var tLbl = DiabloUIFactory.MakeText(col.transform, "Title", title, 14, titleCol);
        DiabloUIFactory.SetLayout(tLbl.gameObject, prefH: 20f, flexW: 1f);
        var sLbl = DiabloUIFactory.MakeText(col.transform, "Sub", sub, 12, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(sLbl.gameObject, prefH: 16f, flexW: 1f);

        var pLbl = DiabloUIFactory.MakeText(go.transform, "Price", priceStr, 14, priceCol, TextAnchor.MiddleRight);
        pLbl.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(pLbl.gameObject, prefH: 32f, prefW: 62f);

        if (EventSystem.current != null && onHover != null)
        {
            var trig = go.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => onHover());
            trig.triggers.Add(entry);
        }
        return btn;
    }
}
