using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Lantern Hold shared vault / stash chest UI with two scrollable icon-backed columns
/// (Vault Chest 40 slots <-> Hero Bag 20 slots) and a bottom hover inspection bar.
/// </summary>
public sealed class DiabloStashUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Text countText;
    Text inspectText;
    Transform stashList;
    Transform bagList;
    Inventory inventory;
    Equipment equipment;

    void Awake()
    {
        EnsureBuilt();
        SetOpen(false);
    }

    void EnsureBuilt()
    {
        inventory = GetComponent<Inventory>();
        equipment = GetComponent<Equipment>();
        if (canvas != null) return;
        BuildUI();
    }

    void Update()
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame && IsOpen)
        {
            SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
        }
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        if (IsOpen == open) return;
        if (open)
        {
            DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
            DiabloAudio.PlayUIClick();
        }
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
        Loc.F("stash.count", RunProfile.Stash.Count, Stash.Capacity, (inventory != null ? inventory.Count : 0), Inventory.Capacity);

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
            MakeRowButton(list, item, verb, () => onClick(index));
        }
    }

    void InspectItem(ItemInstance item)
    {
        if (inspectText == null || item == null) return;
        if (item.slot == ItemSlot.Consumable)
        {
            inspectText.text = Loc.F("stash.potion.tip", LootDrop.ItemLabel(item), ItemDatabase.PotionHeal);
            return;
        }
        var eq = equipment != null ? equipment.GetEquipped(item.slot) : null;
        string eqName = eq != null ? LootDrop.ItemLabel(eq) : Loc.T("town.emptyslot");
        inspectText.text = $"<b>{LootDrop.ItemLabel(item)}</b> vs {eqName}:\n{DiabloInventoryUI.CompareSummary(item, eq)}";
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("StashCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 11);

        var panelGo = DiabloUIFactory.MakePanel(canvasGo.transform, "Panel", new Vector2(820f, 650f), Vector2.zero);
        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        DiabloUIFactory.MakeHeader(panelGo.transform, Loc.T("stash.title"), DiabloMenuArt.MenuArtSprite("StashChestIcon"), () => SetOpen(false));

        countText = DiabloUIFactory.MakeText(panelGo.transform, "CountLine", Loc.T("stash.count.zero"), 16, DiabloPalette.TrimGold, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(countText.gameObject, prefH: 26f, flexW: 1f);

        var colsGo = new GameObject("Columns", typeof(RectTransform));
        colsGo.transform.SetParent(panelGo.transform, false);
        DiabloUIFactory.SetLayout(colsGo, prefH: 416f, flexW: 1f);
        var cols = colsGo.AddComponent<HorizontalLayoutGroup>();
        cols.spacing = 12f;
        cols.childControlWidth = true;
        cols.childControlHeight = true;
        cols.childForceExpandWidth = true;
        cols.childForceExpandHeight = true;

        stashList = MakeColumn(colsGo.transform, "StashColumn", Loc.T("stash.chest"));
        bagList = MakeColumn(colsGo.transform, "BagColumn", Loc.T("stash.bag"));

        var inspBox = new GameObject("InspectBox", typeof(RectTransform));
        inspBox.transform.SetParent(panelGo.transform, false);
        DiabloUIFactory.SetLayout(inspBox, prefH: 92f, flexW: 1f);
        var ibImg = inspBox.AddComponent<Image>();
        ibImg.sprite = DiabloMenuArt.TooltipBackground();
        ibImg.type = Image.Type.Sliced;
        var iv = inspBox.AddComponent<VerticalLayoutGroup>();
        iv.padding = new RectOffset(12, 12, 8, 8);
        inspectText = DiabloUIFactory.MakeText(inspBox.transform, "InspectText",
            Loc.T("stash.hint"), 14, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(inspectText.gameObject, prefH: 74f, flexW: 1f);
    }

    static Transform MakeColumn(Transform parent, string name, string heading)
    {
        var col = new GameObject(name, typeof(RectTransform));
        col.transform.SetParent(parent, false);
        var bg = col.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.TooltipBackground();
        bg.type = Image.Type.Sliced;
        var cv = col.AddComponent<VerticalLayoutGroup>();
        cv.padding = new RectOffset(10, 10, 8, 8);
        cv.spacing = 6f;
        cv.childControlWidth = true;
        cv.childControlHeight = false;

        var lbl = DiabloUIFactory.MakeText(col.transform, "Heading", heading, 15, DiabloPalette.TrimGold);
        lbl.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(lbl.gameObject, prefH: 24f, flexW: 1f);

        var scrollGo = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(col.transform, false);
        DiabloUIFactory.SetLayout(scrollGo, prefH: 366f, flexW: 1f, flexH: 1f);
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 28f;

        var vp = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(scrollGo.transform, false);
        vp.AddComponent<RectMask2D>();
        var vpImg = vp.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.01f);
        var vpr = vp.GetComponent<RectTransform>();
        vpr.anchorMin = Vector2.zero;
        vpr.anchorMax = Vector2.one;
        vpr.offsetMin = vpr.offsetMax = Vector2.zero;
        sr.viewport = vpr;

        var rows = new GameObject("Rows", typeof(RectTransform));
        rows.transform.SetParent(vp.transform, false);
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

    Button MakeRowButton(Transform parent, ItemInstance item, string verb, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("StashButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefH: 46f, flexW: 1f);
        var bg = go.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.ButtonBackground(false);
        bg.type = Image.Type.Sliced;
        var button = go.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var hz = go.AddComponent<HorizontalLayoutGroup>();
        hz.padding = new RectOffset(6, 8, 4, 4);
        hz.spacing = 8f;
        hz.childAlignment = TextAnchor.MiddleLeft;
        hz.childControlWidth = true;
        hz.childControlHeight = false;
        hz.childForceExpandWidth = false;

        var slot = new GameObject("Slot", typeof(RectTransform));
        slot.transform.SetParent(go.transform, false);
        DiabloUIFactory.SetLayout(slot, prefH: 36f, prefW: 36f);
        var sImg = slot.AddComponent<Image>();
        sImg.sprite = DiabloMenuArt.SlotBackground(item.rarity, 64);
        sImg.type = Image.Type.Sliced;
        sImg.raycastTarget = false;

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(slot.transform, false);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var ir = iconGo.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero;
        ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(3f, 3f);
        ir.offsetMax = new Vector2(-3f, -3f);

        var lbl = DiabloUIFactory.MakeText(go.transform, "ItemLabel",
            $"<b>{LootDrop.ItemLabel(item)}</b>", 14, DiabloPalette.Rarity(item.rarity));
        DiabloUIFactory.SetLayout(lbl.gameObject, prefH: 34f, flexW: 1f);

        var tag = DiabloUIFactory.MakeText(go.transform, "ActionTag", verb, 13, DiabloPalette.TrimGold, TextAnchor.MiddleRight);
        DiabloUIFactory.SetLayout(tag.gameObject, prefH: 30f, prefW: 48f);

        if (EventSystem.current != null)
        {
            var trig = go.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => InspectItem(item));
            trig.triggers.Add(entry);
        }
        return button;
    }
}
