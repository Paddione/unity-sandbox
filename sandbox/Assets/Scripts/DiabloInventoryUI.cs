using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sanctuary Inventory, Character Codex & Settings panel (I / B toggles, 1-3 tabs, Esc closes).
/// The first page shows the live 3D hero with its equipment slots next to the bag:
/// hover shows the shared <see cref="ItemTooltip"/> (Shift compares against the worn
/// item), right click swaps instantly, and items drag between bag and slots.
/// Preserves all EditMode/PlayMode test contracts: MenuTab, IsOpen, CurrentTab, BetterColor,
/// WorseColor, SetOpen, ShowTab, EquipIndex, UnequipSlot, Refresh, CompareSummary, DifficultyText,
/// InventoryCanvas, BagGrid, BagButton{i}, and BagEmpty (20 cells).
/// </summary>
public sealed class DiabloInventoryUI : MonoBehaviour
{
    /// <summary>Inventory and Bag share the combined gear-and-bag page.</summary>
    public enum MenuTab { Inventory, Bag, Stats, Settings }

    public bool IsOpen { get; private set; }
    public MenuTab CurrentTab { get; private set; }

    const float FlashSeconds = 0.6f;
    static readonly Color RejectTint = new Color(1f, 0.35f, 0.3f);
    static readonly Color AcceptTint = new Color(0.55f, 1f, 0.55f);
    static readonly Color DimTint = new Color(0.55f, 0.55f, 0.55f);

    Canvas canvas;
    RectTransform canvasRect;
    GameObject inventoryPage;
    GameObject statsPage;
    GameObject settingsPage;

    Text heroHeaderText;
    /// <summary>Slot captions, indexed by (int)ItemSlot (Consumable stays null).</summary>
    readonly Text[] slotTexts = new Text[SlotArraySize];
    const int SlotArraySize = (int)ItemSlot.Belt + 1;
    Text statsText;
    Text bagCountText;
    Text bagDetailText;
    Text statsDetailText;
    Transform bagList;
    HeroPaperdollPreview paperdoll;
    Image dragGhost;

    readonly Image[] equipIcons = new Image[SlotArraySize];
    readonly Image[] equipFrames = new Image[SlotArraySize];
    readonly float[] flashUntil = new float[SlotArraySize];
    readonly Button[] tabButtons = new Button[3];
    readonly List<UnityEngine.Events.UnityAction> settingsRefreshes = new List<UnityEngine.Events.UnityAction>();

    Inventory inventory;
    Equipment equipment;
    PlayerHealth health;
    Wallet wallet;
    DiabloProgression prog;

    InventoryCell dragSource;
    int selectedIndex = -1;

    /// <summary>The live 3D hero view (test seam).</summary>
    public HeroPaperdollPreview Paperdoll => paperdoll;

    /// <summary>True while a bag or slot item is being dragged.</summary>
    public bool IsDragging => dragSource != null;

    /// <summary>Slot that last refused a drop (test seam; null when the last drop landed).</summary>
    public ItemSlot? LastRejectedSlot { get; private set; }

    void Awake()
    {
        EnsureBuilt();
        SetOpen(false);
    }

    void EnsureBuilt()
    {
        inventory = GetComponent<Inventory>();
        equipment = GetComponent<Equipment>();
        health = GetComponent<PlayerHealth>();
        wallet = GetComponent<Wallet>();
        prog = GetComponent<DiabloProgression>();
        if (canvas != null) return;
        BuildUI();
    }

    void Update()
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) return;
        if (IsOpen) PaintSlotTints();
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.iKey.wasPressedThisFrame || kb.bKey.wasPressedThisFrame)
        {
            SetOpen(!IsOpen);
            return;
        }
        if (kb.escapeKey.wasPressedThisFrame && IsOpen)
        {
            if (IsDragging) { CancelDrag(); DiabloUIFactory.MarkEscConsumed(); return; }
            SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
            return;
        }
        if (!IsOpen) return;
        if (kb.digit1Key.wasPressedThisFrame) ShowTab(MenuTab.Inventory);
        else if (kb.digit2Key.wasPressedThisFrame) ShowTab(MenuTab.Stats);
        else if (kb.digit3Key.wasPressedThisFrame) ShowTab(MenuTab.Settings);
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (open) DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
        if (canvas != null) canvas.enabled = open;
        if (!open)
        {
            CancelDrag();
            ItemTooltip.HideFor(gameObject);
        }
        if (open)
        {
            CurrentTab = MenuTab.Inventory;
            ShowTab(CurrentTab);
            Refresh();
        }
        else if (paperdoll != null) paperdoll.Hide();
    }

    public void ShowTab(MenuTab tab)
    {
        EnsureBuilt();
        CurrentTab = tab;
        bool gear = tab == MenuTab.Inventory || tab == MenuTab.Bag;
        if (inventoryPage != null) inventoryPage.SetActive(gear);
        if (statsPage != null) statsPage.SetActive(tab == MenuTab.Stats);
        if (settingsPage != null) settingsPage.SetActive(tab == MenuTab.Settings);
        if (paperdoll != null)
        {
            if (gear && IsOpen) paperdoll.Show();
            else paperdoll.Hide();
        }
        if (!gear) ItemTooltip.HideFor(gameObject);
        PaintTabs();
        if (IsOpen) DiabloAudio.PlayUIClick();
    }

    /// <summary>Equip bag[index]; refreshes stats and HP max. Legacy consumables never equip.</summary>
    public void EquipIndex(int index)
    {
        EnsureBuilt();
        if (inventory == null || equipment == null) return;
        var item = inventory.Get(index);
        if (item == null) return;
        if (!Equipment.IsGearSlot(item.slot)) return;
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
        {
            if (relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Equip, item))
                SetNotice(Loc.F("inv.notice.equipping", LootDrop.ItemLabel(item)));
            else SetNotice(Loc.F("inv.notice.equiprejected", LootDrop.ItemLabel(item)));
            Refresh();
            return;
        }
        var candidate = item;
        if (!Equipment.CanEquip(candidate, RunProfile.ClassId))
        {
            SetNotice(Loc.F("inv.notice.classlock", LootDrop.ItemLabel(candidate), ClassCatalog.Get(ItemDatabase.ClassRestriction(candidate.baseType)).displayName));
            Refresh();
            return;
        }
        if (inventory.EquipFromBag(index, equipment))
        {
            if (health != null) health.Refresh();
            DiabloAudio.PlayUIClick();
            SetNotice(candidate != null ? Loc.F("inv.notice.equipped", LootDrop.ItemLabel(candidate)) : Loc.T("inv.notice.equipped.plain"));
        }
        else SetNotice(Loc.T("inv.notice.noequip"));
        Refresh();
    }

    /// <summary>S5: equip by instance ID (visible Equip action routes here).</summary>
    public bool EquipById(string instanceId)
    {
        EnsureBuilt();
        if (inventory == null) return false;
        int index = inventory.IndexOf(instanceId);
        if (index < 0) { SetNotice(Loc.T("inv.notice.gone")); Refresh(); return false; }
        EquipIndex(index);
        return true;
    }

    /// <summary>S5: discard by instance ID; spawns the same instance (S2 rule).</summary>
    public bool DiscardById(string instanceId)
    {
        EnsureBuilt();
        if (inventory == null) return false;
        int index = inventory.IndexOf(instanceId);
        if (index < 0) { SetNotice(Loc.T("inv.notice.gone")); Refresh(); return false; }
        return DiscardIndex(index);
    }

    /// <summary>Latest action feedback (full bag / rejected action). Test seam.</summary>
    public string Notice { get; private set; } = "";
    Text noticeText;
    void SetNotice(string text)
    {
        Notice = text ?? "";
        if (noticeText != null) noticeText.text = Notice;
    }

    /// <summary>Move an equipped slot back into the bag (button hook).</summary>
    public void UnequipSlot(int slotIndex)
    {
        EnsureBuilt();
        if (inventory == null || equipment == null) return;
        var slot = (ItemSlot)slotIndex;
        if (!Equipment.IsGearSlot(slot)) return;
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
        {
            var item = equipment.GetEquipped(slot);
            if (item != null) relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Unequip, item);
            return;
        }
        var worn = equipment.GetEquipped(slot);
        if (equipment.UnequipTo(slot, inventory))
        {
            if (health != null) health.Refresh();
            DiabloAudio.PlayUIClick();
            Refresh();
        }
        else if (worn != null)
        {
            SetNotice(Loc.F("inv.notice.bagfull", LootDrop.ItemLabel(worn)));
            Flash(slot);
        }
    }

    public bool DiscardIndex(int index)
    {
        EnsureBuilt();
        var item = inventory?.Get(index);
        if (item == null) return false;
        string label = LootDrop.ItemLabel(item);
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
        {
            bool ok = relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Discard, item);
            SetNotice(ok ? Loc.F("inv.notice.discardhost", label) : Loc.F("inv.notice.discardrejected", label));
            Refresh();
            return ok;
        }
        if (LootDrop.SpawnItem(transform.position + transform.forward * 1.5f, item, false) == null)
        {
            SetNotice(Loc.F("inv.notice.nodrop", label));
            Refresh();
            return false;
        }
        inventory.RemoveAt(index);
        SetNotice(Loc.F("inv.notice.discarded", label));
        Refresh();
        return true;
    }

    // ── Drag & drop / right click ────────────────────────────────────────

    /// <summary>
    /// Drop bag[bagIndex] on an equipment slot: equips (swapping the worn item
    /// into the bag) when the item belongs to that slot, otherwise flashes the
    /// slot red and changes nothing. Returns true when the equip was issued.
    /// </summary>
    public bool DropBagOnSlot(int bagIndex, ItemSlot target)
    {
        EnsureBuilt();
        var item = inventory != null ? inventory.Get(bagIndex) : null;
        if (item == null) return false;
        if (item.slot != target)
        {
            SetNotice(Loc.F("inv.notice.wrongslot", LootDrop.ItemLabel(item), ItemText.Slot(target)));
            Reject(target);
            return false;
        }
        if (!Equipment.CanEquip(item, RunProfile.ClassId))
        {
            SetNotice(Loc.F("inv.notice.classlock", LootDrop.ItemLabel(item), ClassCatalog.Get(ItemDatabase.ClassRestriction(item.baseType)).displayName));
            Reject(target);
            return false;
        }
        LastRejectedSlot = null;
        EquipIndex(bagIndex);
        return true;
    }

    /// <summary>Drop a worn item on the bag: unequips it.</summary>
    public void DropSlotOnBag(ItemSlot slot) => UnequipSlot((int)slot);

    /// <summary>Right click on a bag cell: instant swap into its slot.</summary>
    public void RightClickBag(int index) => EquipIndex(index);

    /// <summary>Right click on a worn item: back into the bag.</summary>
    public void RightClickSlot(ItemSlot slot) => UnequipSlot((int)slot);

    internal void CellHover(InventoryCell cell, bool enter)
    {
        if (!enter)
        {
            ItemTooltip.HideFor(gameObject);
            return;
        }
        var item = ItemAt(cell);
        if (cell.kind == InventoryCell.Kind.Bag && item != null)
        {
            selectedIndex = cell.index;
            if (bagDetailText != null) bagDetailText.text = DetailFor(item);
        }
        if (item == null || IsDragging) return;
        var tip = ItemTooltip.For(gameObject);
        if (tip != null) tip.Show(item, equipment, null, cell.gameObject);
    }

    internal void CellRightClick(InventoryCell cell)
    {
        ItemTooltip.HideFor(gameObject);
        if (cell.kind == InventoryCell.Kind.Bag && ItemAt(cell) != null) RightClickBag(cell.index);
        else if (cell.kind == InventoryCell.Kind.Equip) RightClickSlot(cell.Slot);
    }

    internal void CellBeginDrag(InventoryCell cell, Vector2 screen)
    {
        if (cell.kind == InventoryCell.Kind.Doll) return;
        var item = ItemAt(cell);
        if (item == null || dragGhost == null) return;
        dragSource = cell;
        ItemTooltip.HideFor(gameObject);
        dragGhost.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
        dragGhost.gameObject.SetActive(true);
        dragGhost.transform.SetAsLastSibling();
        CellDrag(screen);
    }

    internal void CellDrag(Vector2 screen)
    {
        if (dragSource == null || dragGhost == null || canvasRect == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local))
            dragGhost.rectTransform.anchoredPosition = local;
    }

    internal void CellEndDrag() => CancelDrag();

    internal void CellDrop(InventoryCell target)
    {
        var source = dragSource;
        CancelDrag();
        if (source == null || target == null || source == target) return;
        if (source.kind == InventoryCell.Kind.Bag)
        {
            var item = ItemAt(source);
            if (item == null) return;
            if (target.kind == InventoryCell.Kind.Equip) DropBagOnSlot(source.index, target.Slot);
            else if (target.kind == InventoryCell.Kind.Doll && item.slot != ItemSlot.Consumable) DropBagOnSlot(source.index, item.slot);
        }
        else if (source.kind == InventoryCell.Kind.Equip && target.kind == InventoryCell.Kind.Bag)
        {
            DropSlotOnBag(source.Slot);
        }
    }

    void CancelDrag()
    {
        dragSource = null;
        if (dragGhost != null) dragGhost.gameObject.SetActive(false);
    }

    ItemInstance ItemAt(InventoryCell cell)
    {
        if (cell == null) return null;
        if (cell.kind == InventoryCell.Kind.Bag)
            return inventory != null && cell.index >= 0 && cell.index < inventory.Count ? inventory.Get(cell.index) : null;
        if (cell.kind == InventoryCell.Kind.Equip)
            return equipment != null ? equipment.GetEquipped(cell.Slot) : null;
        return null;
    }

    void Reject(ItemSlot slot)
    {
        LastRejectedSlot = slot;
        Flash(slot);
        DiabloAudio.PlayUIClick();
    }

    void Flash(ItemSlot slot)
    {
        int s = (int)slot;
        if (s >= 0 && s < flashUntil.Length) flashUntil[s] = Time.unscaledTime + FlashSeconds;
        PaintSlotTints();
    }

    /// <summary>Red after a refused drop, green on the dragged item's slot, dim on the others.</summary>
    void PaintSlotTints()
    {
        var dragged = dragSource != null && dragSource.kind == InventoryCell.Kind.Bag ? ItemAt(dragSource) : null;
        for (int s = 0; s < equipFrames.Length; s++)
        {
            var frame = equipFrames[s];
            if (frame == null) continue;
            Color tint = Color.white;
            if (Time.unscaledTime < flashUntil[s]) tint = RejectTint;
            else if (dragged != null && Equipment.IsGearSlot(dragged.slot))
                tint = (int)dragged.slot == s ? AcceptTint : DimTint;
            frame.color = tint;
        }
    }

    // ── Refresh ──────────────────────────────────────────────────────────

    /// <summary>Rebuild slot labels, stats and bag grid from state.</summary>
    public void Refresh()
    {
        EnsureBuilt();
        CancelDrag();
        int lv = prog != null ? prog.Level : 1;
        int gold = wallet != null ? wallet.Gold : 0;
        var flask = HealingFlask.For(gameObject);
        if (heroHeaderText != null)
            heroHeaderText.text =
                Loc.F("inv.hero.header", DiabloPalette.ToHex(DiabloPalette.GoldBright), RunProfile.Class.displayName, lv, Loc.Display(RunProfile.Difficulty)) +
                Loc.F("inv.hero.gold", DiabloPalette.ToHex(DiabloPalette.TrimGold), gold, flask.Charges, flask.MaxCharges);

        if (equipment != null)
        {
            foreach (var gear in Equipment.GearSlots)
                if (slotTexts[(int)gear] != null)
                    slotTexts[(int)gear].text = SlotCaption(ItemText.Slot(gear), equipment.GetEquipped(gear));
            statsText.text =
                Loc.T("inv.stats.combat") +
                Loc.F("inv.stats.dmg", DiabloPalette.ToHex(DiabloPalette.TrimGold), equipment.GetDamage()) +
                Loc.F("inv.stats.maxhp", BetterColor, equipment.GetMaxHP()) +
                Loc.F("inv.stats.crit", equipment.GetCrit()) +
                Loc.F("inv.stats.speed", equipment.GetMoveSpeed(), equipment.GetXPMul()) +
                Loc.F("inv.stats.cdr", Mathf.Min(Equipment.MaxCooldownReductionPct, equipment.Total(AffixLibrary.CooldownReduction)));
            foreach (var gear in Equipment.GearSlots) PaintEquipIcon((int)gear, equipment.GetEquipped(gear));
            if (statsDetailText != null) statsDetailText.text = StatsBlock();
        }
        RebuildBag();
        if (noticeText != null) noticeText.text = Notice;
        foreach (var r in settingsRefreshes) r?.Invoke();
        PaintTabs();
        PaintSlotTints();
    }

    /// <summary>S3 area tooltip line: IncreasedArea percent + radius factor (test seam).</summary>
    public static string AreaLine(Equipment eq) =>
        eq == null ? "Area  +0%" : Loc.F("inv.stats.area", eq.AreaBonusPct(), eq.ScaleArea(1f));

    string StatsBlock()
    {
        var sheet = prog != null ? prog.Sheet : null;
        int str = sheet != null ? sheet.strength : 0;
        int vit = sheet != null ? sheet.vitality : 0;
        int foc = sheet != null ? sheet.focus : 0;
        float armor = equipment.Total(AffixLibrary.Armor);
        float fireDmg = equipment.Total(AffixLibrary.FireDamage);
        float cdr = Mathf.Min(Equipment.MaxCooldownReductionPct, equipment.Total(AffixLibrary.CooldownReduction));
        float ls = equipment.LifeStealPct();
        float gf = equipment.GoldFindPct();
        float mf = equipment.MagicFindPct();
        int thorns = equipment.ThornsDamage();
        int ranks = equipment.SkillRankBonus();

        string goldHex = DiabloPalette.ToHex(DiabloPalette.TrimGold);
        return
            Loc.F("inv.stats.attributes", goldHex, str, vit, foc) +
            Loc.F("inv.stats.offdef", goldHex) +
            Loc.F("inv.stats.line1", equipment.GetDamage(), fireDmg, equipment.GetCrit()) +
            Loc.F("inv.stats.line4", equipment.GetMaxHP(), armor, thorns) +
            Loc.F("inv.stats.line3", ls, cdr, ranks) +
            $"• {AreaLine(equipment)}\n\n" +
            Loc.F("inv.stats.expedition", goldHex) +
            Loc.F("inv.stats.line5", equipment.GetMoveSpeed(), equipment.GetXPMul()) +
            Loc.F("inv.stats.line2", gf, mf) +
            Loc.F("inv.stats.line6", DifficultyText());
    }

    /// <summary>Two-line caption under a paperdoll slot: slot name, then the worn item.</summary>
    static string SlotCaption(string slot, ItemInstance item)
    {
        string dim = DiabloPalette.ToHex(DiabloPalette.ParchmentDim);
        if (item == null) return Loc.F("inv.slot.caption.empty", dim, slot);
        string rHex = DiabloPalette.ToHex(DiabloPalette.Rarity(item.rarity));
        return Loc.F("inv.slot.caption", dim, slot, rHex, ShortLabel(item, 18));
    }

    /// <summary>Bag detail line: the shared S5 item card (inventory reuse).</summary>
    string DetailFor(ItemInstance item)
    {
        if (item == null) return Loc.T("inv.baghint");
        var equipped = equipment != null ? equipment.GetEquipped(item.slot) : null;
        return ItemDetailsView.Render(item, equipped);
    }

    public const string BetterColor = "#6fd36f";
    public const string WorseColor = "#e05a4a";

    /// <summary>
    /// Replayability S2 tooltip: per affix key the summed value of
    /// <paramref name="item"/> and its difference to <paramref name="equipped"/>
    /// in the same slot, green when better and red when worse (rich text).
    /// </summary>
    public static string CompareSummary(ItemInstance item, ItemInstance equipped)
    {
        if (item == null) return "";
        var diff = ItemCompare.Diff(item, equipped);
        if (diff.Count == 0) return Loc.T("inv.nobonus");
        var lines = new List<string>();
        foreach (var d in diff)
        {
            string text = AffixLibrary.Describe(new Affix(d.key, d.mine));
            float delta = d.Delta;
            if (equipped != null && d.Trend != StatTrend.Same)
            {
                string sign = delta > 0f ? "+" : "";
                string color = delta > 0f ? BetterColor : WorseColor;
                lines.Add($"{text} <color={color}>({sign}{delta:0.#})</color>");
            }
            else lines.Add(text);
        }
        return string.Join("\n", lines.ToArray());
    }

    void RebuildBag()
    {
        if (bagList == null) return;
        foreach (Transform child in bagList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        int count = inventory != null ? inventory.Count : 0;
        var bagFlask = HealingFlask.For(gameObject);
        if (bagCountText != null)
            bagCountText.text = Loc.F("inv.bag.count", count, Inventory.Capacity, (wallet != null ? wallet.Gold : 0), bagFlask.Charges, bagFlask.MaxCharges);
        for (int i = 0; i < Inventory.Capacity; i++)
        {
            var item = inventory != null && i < inventory.Count ? inventory.Get(i) : null;
            var cell = MakeBagCell(item, i);
            cell.transform.SetParent(bagList, false);
        }
        if (selectedIndex >= count) selectedIndex = -1;
        if (bagDetailText != null)
        {
            if (count > 0) bagDetailText.text = DetailFor(inventory.Get(SelectedOrLast()));
            else bagDetailText.text = Loc.T("inv.bag.empty");
        }
    }

    int SelectedOrLast() =>
        inventory == null ? -1 : selectedIndex >= 0 && selectedIndex < inventory.Count ? selectedIndex : inventory.Count - 1;

    // ── Build ────────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 10);
        canvasRect = canvasGo.GetComponent<RectTransform>();

        var panelGo = DiabloUIFactory.MakePanel(canvasGo.transform, "Panel", new Vector2(1120f, 820f), new Vector2(-18f, 0f));
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(1f, 0.5f);
        pr.anchorMax = new Vector2(1f, 0.5f);
        pr.pivot = new Vector2(1f, 0.5f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        DiabloUIFactory.MakeHeader(panelGo.transform, Loc.T("inv.title"), DiabloMenuArt.MenuArtSprite("CrestCinderreach"), () => SetOpen(false));

        // Tab Bar: gear-and-bag, stats, settings.
        var tabGo = DiabloUIFactory.MakeHorizontalRow(panelGo.transform, "TabBar", 6f, 38f);
        string[] names = { Loc.T("inv.tab.inventory"), Loc.T("inv.tab.stats"), Loc.T("inv.tab.settings") };
        MenuTab[] targets = { MenuTab.Inventory, MenuTab.Stats, MenuTab.Settings };
        for (int i = 0; i < tabButtons.Length; i++)
        {
            var target = targets[i];
            var b = MakeTabButton(tabGo.transform, names[i], i == 0);
            DiabloUIFactory.SetLayout(b.gameObject, prefH: 36f, flexW: 1f);
            b.onClick.AddListener(() => ShowTab(target));
            tabButtons[i] = b;
        }

        inventoryPage = MakePage(panelGo.transform, "InventoryPage", 690f, columns: true);
        statsPage = MakePage(panelGo.transform, "StatsPage", 690f);
        settingsPage = MakePage(panelGo.transform, "SettingsPage", 690f);

        BuildInventoryPage();
        BuildStatsPage();
        BuildSettingsPage();

        var ghostGo = new GameObject("DragGhost", typeof(RectTransform));
        ghostGo.transform.SetParent(canvasGo.transform, false);
        dragGhost = ghostGo.AddComponent<Image>();
        dragGhost.preserveAspect = true;
        dragGhost.raycastTarget = false;
        dragGhost.color = new Color(1f, 1f, 1f, 0.85f);
        dragGhost.rectTransform.sizeDelta = new Vector2(72f, 72f);
        ghostGo.SetActive(false);
    }

    GameObject MakePage(Transform parent, string name, float height, bool columns = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefH: height, flexW: 1f);
        if (columns)
        {
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            return go;
        }
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        return go;
    }

    void BuildInventoryPage()
    {
        // Character column (left) and bag column (right) side by side.
        var charCol = MakeColumn(inventoryPage.transform, "CharacterColumn", 470f, 0f);
        var bagCol = MakeColumn(inventoryPage.transform, "BagColumn", -1f, 1f);
        BuildCharacterColumn(charCol.transform);
        BuildBagColumn(bagCol.transform);
    }

    static GameObject MakeColumn(Transform parent, string name, float width, float flexW)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefW: width, flexW: flexW);
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        v.childForceExpandHeight = false;
        return go;
    }

    void BuildCharacterColumn(Transform col)
    {
        var heroBox = MakeCard(col, "HeroCard", 58f);
        var hbV = heroBox.AddComponent<VerticalLayoutGroup>();
        hbV.padding = new RectOffset(12, 12, 6, 6);
        hbV.childControlWidth = true;
        hbV.childControlHeight = false;
        heroHeaderText = DiabloUIFactory.MakeText(heroBox.transform, "HeroHeaderText", "", 15, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(heroHeaderText.gameObject, prefH: 46f, flexW: 1f);

        // Paperdoll: the live 3D hero with Helmet/Armor/Belt left and
        // Gloves/Weapon/Boots right (gear-slots S1).
        var doll = MakeCard(col, "Paperdoll", 470f);
        var stage = new GameObject("PaperdollStage", typeof(RectTransform));
        stage.transform.SetParent(doll.transform, false);
        var glow = stage.AddComponent<Image>();
        glow.sprite = DiabloMenuArt.SlotBackground(null, 96);
        glow.type = Image.Type.Sliced;
        glow.color = new Color(0.55f, 0.45f, 0.35f, 0.35f);
        glow.raycastTarget = false;
        Place(stage.GetComponent<RectTransform>(), new Vector2(0f, 8f), new Vector2(272f, 400f));

        var previewGo = new GameObject("PaperdollImage", typeof(RectTransform));
        previewGo.transform.SetParent(doll.transform, false);
        var raw = previewGo.AddComponent<RawImage>();
        raw.color = Color.white;
        raw.enabled = false;
        Place(previewGo.GetComponent<RectTransform>(), new Vector2(0f, 8f), new Vector2(312f, 390f));
        paperdoll = previewGo.AddComponent<HeroPaperdollPreview>();
        paperdoll.Bind(gameObject);
        var dollCell = previewGo.AddComponent<InventoryCell>();
        dollCell.kind = InventoryCell.Kind.Doll;
        dollCell.owner = this;

        foreach (var gear in Equipment.GearSlots)
            BuildEquipSlot(doll.transform, (int)gear, ItemText.Slot(gear), PaperdollSlotPosition(gear));

        var hint = DiabloUIFactory.MakeText(doll.transform, "EquipHint", Loc.T("inv.equiphint"), 12, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        hint.raycastTarget = false;
        Place(hint.rectTransform, new Vector2(0f, -212f), new Vector2(440f, 34f));

        var summaryBox = MakeCard(col, "SummaryBox", 128f);
        var sv = summaryBox.AddComponent<VerticalLayoutGroup>();
        sv.padding = new RectOffset(12, 12, 8, 8);
        sv.childControlWidth = true;
        sv.childControlHeight = false;
        statsText = DiabloUIFactory.MakeText(summaryBox.transform, "StatsSummary", "", 14, DiabloPalette.Parchment, TextAnchor.UpperLeft);
        DiabloUIFactory.SetLayout(statsText.gameObject, prefH: 110f, flexW: 1f);
    }

    void BuildEquipSlot(Transform parent, int s, string slotName, Vector2 pos)
    {
        var frameGo = new GameObject("EquipFrame" + s, typeof(RectTransform));
        frameGo.transform.SetParent(parent, false);
        Place(frameGo.GetComponent<RectTransform>(), pos, new Vector2(92f, 92f));
        var fImg = frameGo.AddComponent<Image>();
        fImg.sprite = DiabloMenuArt.SlotBackground(null, 96);
        fImg.type = Image.Type.Sliced;
        equipFrames[s] = fImg;
        var btn = frameGo.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        int slot = s;
        btn.onClick.AddListener(() => UnequipSlot(slot));
        var cell = frameGo.AddComponent<InventoryCell>();
        cell.kind = InventoryCell.Kind.Equip;
        cell.index = s;
        cell.owner = this;

        var iconGo = new GameObject("EquipIcon" + s, typeof(RectTransform));
        iconGo.transform.SetParent(frameGo.transform, false);
        var icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        var ir = iconGo.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero;
        ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(10f, 10f);
        ir.offsetMax = new Vector2(-10f, -10f);
        equipIcons[s] = icon;

        var label = DiabloUIFactory.MakeText(parent, "SlotLabel" + s, slotName, 13, DiabloPalette.Parchment, TextAnchor.UpperCenter);
        label.raycastTarget = false;
        Place(label.rectTransform, pos + new Vector2(0f, -74f), new Vector2(150f, 44f));
        slotTexts[s] = label;
    }

    /// <summary>Paperdoll frame centre per slot: two columns of three (gear-slots S1).</summary>
    public static Vector2 PaperdollSlotPosition(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Helmet: return new Vector2(-170f, 155f);
            case ItemSlot.Armor: return new Vector2(-170f, 30f);
            case ItemSlot.Belt: return new Vector2(-170f, -95f);
            case ItemSlot.Gloves: return new Vector2(170f, 155f);
            case ItemSlot.Weapon: return new Vector2(170f, 30f);
            default: return new Vector2(170f, -95f);
        }
    }

    static GameObject MakeCard(Transform parent, string name, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefH: height, flexW: 1f);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TooltipBackground();
        img.type = Image.Type.Sliced;
        return go;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    void BuildBagColumn(Transform col)
    {
        var topRow = DiabloUIFactory.MakeHorizontalRow(col, "BagTopRow", 8f, 30f);
        bagCountText = DiabloUIFactory.MakeText(topRow.transform, "BagCount", "Bag", 14, DiabloPalette.TrimGold);
        DiabloUIFactory.SetLayout(bagCountText.gameObject, prefH: 28f, flexW: 1f);
        var potionBtn = DiabloUIFactory.MakeSmallButton(topRow.transform, Loc.T("inv.btn.potion"), () =>
        {
            if (HealingFlask.For(gameObject).TryDrink()) Refresh();
        }, 28f, 170f);
        potionBtn.name = "FlaskQuickButton";

        var gridGo = new GameObject("BagGrid", typeof(RectTransform));
        gridGo.transform.SetParent(col, false);
        DiabloUIFactory.SetLayout(gridGo, prefH: 400f, flexW: 1f);
        // Transparent backdrop: a worn item dropped anywhere on the grid goes back into the bag.
        var gridBg = gridGo.AddComponent<Image>();
        gridBg.color = new Color(0f, 0f, 0f, 0.18f);
        var gridCell = gridGo.AddComponent<InventoryCell>();
        gridCell.kind = InventoryCell.Kind.Bag;
        gridCell.index = -1;
        gridCell.owner = this;
        var grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.cellSize = new Vector2(104f, 92f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(4, 4, 4, 4);
        grid.childAlignment = TextAnchor.UpperCenter;
        bagList = gridGo.transform;

        var tipBox = MakeCard(col, "BagTooltipBox", 152f);
        var tv = tipBox.AddComponent<VerticalLayoutGroup>();
        tv.padding = new RectOffset(12, 12, 8, 8);
        tv.childControlWidth = true;
        tv.childControlHeight = false;
        bagDetailText = DiabloUIFactory.MakeText(tipBox.transform, "BagTooltip", "", 13, DiabloPalette.Parchment, TextAnchor.UpperLeft);
        DiabloUIFactory.FitToBox(bagDetailText, 13);
        DiabloUIFactory.SetLayout(bagDetailText.gameObject, prefH: 136f, flexW: 1f);

        // S5: visible separate actions for the selected (hovered, else newest) bag item.
        var actionRow = DiabloUIFactory.MakeHorizontalRow(col, "BagActions", 8f, 34f);
        DiabloUIFactory.MakeSmallButton(actionRow.transform, Loc.T("inv.btn.equip"), () =>
        {
            int i = SelectedOrLast();
            if (i >= 0) EquipIndex(i);
        });
        DiabloUIFactory.MakeSmallButton(actionRow.transform, Loc.T("inv.btn.discard"), () =>
        {
            int i = SelectedOrLast();
            if (i >= 0) DiscardIndex(i);
        });
        noticeText = DiabloUIFactory.MakeText(col, "Notice", "", 14, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(noticeText.gameObject, prefH: 22f, flexW: 1f);
    }

    void BuildStatsPage()
    {
        var banner = MakeCard(statsPage.transform, "StatsBanner", 76f);
        var bh = banner.AddComponent<HorizontalLayoutGroup>();
        bh.padding = new RectOffset(12, 12, 6, 6);
        bh.spacing = 12f;
        bh.childControlWidth = true;
        bh.childControlHeight = true;
        bh.childForceExpandWidth = false;
        var portrait = DiabloMenuArt.ClassPortrait(RunProfile.ClassId);
        if (portrait != null)
        {
            var pGo = new GameObject("StatsPortrait", typeof(RectTransform));
            pGo.transform.SetParent(banner.transform, false);
            DiabloUIFactory.SetLayout(pGo, prefH: 64f, prefW: 64f);
            var pImg = pGo.AddComponent<Image>();
            pImg.sprite = portrait;
            pImg.preserveAspect = true;
        }
        var title = DiabloUIFactory.MakeText(banner.transform, "StatsTitle",
            Loc.F("inv.stats.banner", DiabloPalette.ToHex(DiabloPalette.ClassAccent(RunProfile.ClassId)), RunProfile.Class.displayName),
            20, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(title.gameObject, prefH: 64f, flexW: 1f);

        var box = MakeCard(statsPage.transform, "StatsCard", 520f);
        var v = box.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 16, 16);
        v.childControlWidth = true;
        v.childControlHeight = false;
        statsDetailText = DiabloUIFactory.MakeText(box.transform, "StatsDetail", "", 16, DiabloPalette.Parchment, TextAnchor.UpperLeft);
        DiabloUIFactory.SetLayout(statsDetailText.gameObject, prefH: 488f, flexW: 1f);

        var controlsBox = DiabloUIFactory.MakeText(statsPage.transform, "ControlsHint",
            Loc.T("inv.controls"),
            13, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(controlsBox.gameObject, prefH: 44f, flexW: 1f);
    }

    void BuildSettingsPage()
    {
        DiabloUIFactory.BuildSettingsWidgets(settingsPage.transform, settingsRefreshes, onStartNewRun: StartNewRun,
            onBack: () => SetOpen(false));
    }

    /// <summary>
    /// Difficulty changes only with a new run: enemy HP scales at wake-up. Reloads zone with a fresh seed.
    /// </summary>
    public void StartNewRun(string difficulty)
    {
        if (!Diablo.IsSolo()) return;
        DiabloAudio.PlayUIClick();
        DifficultyProfile.Select(difficulty);
        RunProfile.RunSeed = RunProfile.NewSeed();
        if (Diablo.IsLocalAuthority())
        {
            SaveService.SaveNow(gameObject);
            SaveService.ContinueAfterReload = true; // the reloaded scene resumes this hero
            SaveService.ContinueAfterReloadPath = null;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    /// <summary>Settings line for the difficulty row (test seam).</summary>
    public static string DifficultyText() =>
        Loc.F("inv.diff.current", Loc.Display(DifficultyProfile.Current.name)) +
        (RunProfile.DifficultyUnlocked ? "" : Loc.T("inv.diff.locked"));

    void PaintTabs()
    {
        int active = CurrentTab == MenuTab.Stats ? 1 : CurrentTab == MenuTab.Settings ? 2 : 0;
        for (int i = 0; i < tabButtons.Length; i++)
            DiabloUIFactory.StyleButtonActive(tabButtons[i], active == i);
    }

    void PaintEquipIcon(int slot, ItemInstance item)
    {
        if (slot < 0 || slot >= equipFrames.Length) return;
        var frame = equipFrames[slot];
        if (frame != null)
        {
            frame.sprite = DiabloMenuArt.SlotBackground(item != null ? item.rarity : (ItemRarity?)null, 96);
            frame.type = Image.Type.Sliced;
        }
        var img = equipIcons[slot];
        if (img == null) return;
        if (item == null)
        {
            img.enabled = false;
        }
        else
        {
            img.enabled = true;
            img.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
            img.color = Color.white;
        }
    }

    Button MakeTabButton(Transform parent, string text, bool active)
    {
        var go = new GameObject("Tab", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = DiabloMenuArt.TabBackground(active);
        image.type = Image.Type.Sliced;
        var button = go.AddComponent<Button>();
        var label = DiabloUIFactory.MakeText(go.transform, "TabLabel", text, 14,
            active ? DiabloPalette.TrimGold : DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        return button;
    }

    GameObject MakeBagCell(ItemInstance item, int bagIndex)
    {
        var go = new GameObject(item == null ? "BagEmpty" : $"BagButton{bagIndex}", typeof(RectTransform));
        var button = go.AddComponent<Button>();
        var frame = go.AddComponent<Image>();
        frame.sprite = DiabloMenuArt.SlotBackground(item.HasValue() ? item.rarity : (ItemRarity?)null);
        frame.type = Image.Type.Sliced;
        var cell = go.AddComponent<InventoryCell>();
        cell.kind = InventoryCell.Kind.Bag;
        cell.index = item != null ? bagIndex : -1;
        cell.owner = this;
        if (item != null)
        {
            button.onClick.AddListener(() => EquipIndex(bagIndex));

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0.5f, 0.60f);
            ir.anchorMax = new Vector2(0.5f, 0.60f);
            ir.sizeDelta = new Vector2(50f, 50f);
            ir.anchoredPosition = Vector2.zero;

            var label = DiabloUIFactory.MakeText(go.transform, "ItemLabel", ShortLabel(item), 11, DiabloPalette.Rarity(item.rarity),
                TextAnchor.LowerCenter, shadow: true);
            label.raycastTarget = false;
            var lr = label.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(3f, 3f);
            lr.offsetMax = new Vector2(-3f, 0f);

            if (item.slot != ItemSlot.Consumable && !Equipment.CanEquip(item, RunProfile.ClassId))
            {
                var badge = DiabloUIFactory.MakeText(go.transform, "ClassLock", "!", 13, DiabloPalette.Blood, TextAnchor.UpperRight);
                badge.raycastTarget = false;
                var br = badge.rectTransform;
                br.anchorMin = Vector2.zero;
                br.anchorMax = Vector2.one;
                br.offsetMin = Vector2.zero;
                br.offsetMax = new Vector2(-5f, -3f);
            }
            else if (item.slot != ItemSlot.Consumable && equipment != null)
            {
                // Upgrade marker: net-better stats than the worn item in that slot.
                int score = ItemCompare.Score(item, equipment.GetEquipped(item.slot));
                if (score != 0)
                {
                    var mark = DiabloUIFactory.MakeText(go.transform, "UpgradeMark",
                        score > 0 ? ItemCompare.UpArrow : ItemCompare.DownArrow, 13,
                        score > 0 ? DiabloPalette.RogueVerdant : DiabloPalette.Blood, TextAnchor.UpperRight);
                    mark.raycastTarget = false;
                    var mr = mark.rectTransform;
                    mr.anchorMin = Vector2.zero;
                    mr.anchorMax = Vector2.one;
                    mr.offsetMin = Vector2.zero;
                    mr.offsetMax = new Vector2(-5f, -3f);
                }
            }
        }
        else
        {
            button.interactable = false;
        }
        return go;
    }

    static string ShortLabel(ItemInstance item, int max = 13)
    {
        if (item == null) return "";
        if (item.slot == ItemSlot.Consumable) return Loc.T("inv.potion");
        string full = LootDrop.ItemLabel(item);
        return full.Length > max ? full.Substring(0, max - 1) + "…" : full;
    }
}

static class DiabloMenuItemExt
{
    public static bool HasValue(this ItemInstance item) => item != null;
}
