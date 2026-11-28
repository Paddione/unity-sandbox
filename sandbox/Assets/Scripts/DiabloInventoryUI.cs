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
public sealed partial class DiabloInventoryUI : MonoBehaviour
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
    Text statsDetailText;
    Transform paramContent;
    Transform statsParamContent;
    Transform bagList;

    /// <summary>Tooltip column selection (redesign): shown bag index or equipped slot.</summary>
    int tcBagIndex = -1;
    ItemSlot? tcSlot;
    bool tcWired;

    /// <summary>Latest computed character parameter breakdown (test seam).</summary>
    public CharacterStatBreakdown.CharacterStatSnapshot LastStatSnapshot { get; private set; }
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
            InfoTooltip.HideFor(gameObject);
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
        if (tab == MenuTab.Settings) InfoTooltip.HideFor(gameObject);
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
        // Redesign: hover/click fills the fixed tooltip column (no floating
        // ItemTooltip in the inventory; stash/vendor/smith keep it). The column
        // persists after hover exit; only dragging hides the floaters.
        if (!enter) return;
        var item = ItemAt(cell);
        if (cell.kind == InventoryCell.Kind.Bag && item != null) selectedIndex = cell.index;
        if (item == null || IsDragging) return;
        ItemTooltip.HideFor(gameObject);
        InfoTooltip.HideFor(gameObject);
        if (cell.kind == InventoryCell.Kind.Bag) FillTooltipColumn(item, cell.index, null);
        else if (cell.kind == InventoryCell.Kind.Equip) FillTooltipColumn(item, -1, cell.Slot);
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

        LastStatSnapshot = CharacterStatBreakdown.BuildFor(gameObject);
        RebuildParamRows(paramContent, LastStatSnapshot);
        RebuildParamRows(statsParamContent, LastStatSnapshot);

        if (equipment != null)
        {
            foreach (var gear in Equipment.GearSlots)
                if (slotTexts[(int)gear] != null)
                    slotTexts[(int)gear].text = SlotCaption(ItemText.Slot(gear), equipment.GetEquipped(gear));
            var sheet = prog != null ? prog.Sheet : null;
            int strBonus = sheet != null ? sheet.MeleeBonus() : 0;
            float meleeDmg = Mathf.Max(1, Mathf.RoundToInt(equipment.GetDamage()) + strBonus);
            float focusCd = sheet != null ? sheet.CooldownMultiplier() : 1f;
            float totalCdr = (1f - focusCd * equipment.CooldownFactor()) * 100f;
            statsText.text =
                Loc.T("inv.stats.combat") +
                Loc.F("inv.stats.dmg", DiabloPalette.ToHex(DiabloPalette.TrimGold), meleeDmg) +
                Loc.F("inv.stats.maxhp", BetterColor, equipment.GetMaxHP()) +
                Loc.F("inv.stats.crit", equipment.GetCrit()) +
                Loc.F("inv.stats.speed", equipment.GetMoveSpeed(), equipment.GetXPMul()) +
                Loc.F("inv.stats.cdr", totalCdr);
            foreach (var gear in Equipment.GearSlots) PaintEquipIcon((int)gear, equipment.GetEquipped(gear));
        }
        if (statsDetailText != null && LastStatSnapshot != null)
            statsDetailText.text = LastStatSnapshot.RenderPlainSummary() + "\n\n" + Loc.F("inv.stats.line6", DifficultyText());
        RebuildBag();
        if (noticeText != null) noticeText.text = Notice;
        WireTooltipActions();
        if (footerGoldText != null)
            footerGoldText.text = Loc.F("inv.hero.gold", DiabloPalette.ToHex(DiabloPalette.TrimGold), gold, flask.Charges, flask.MaxCharges);
        if (footerDiffText != null) footerDiffText.text = DifficultyText();
        FillTooltipColumnForSelection();
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

    /// <summary>
    /// Upgrade marker score (redesign, testable): net stats of <paramref name="item"/> vs. the
    /// worn item, 0 when the class-lock badge, consumables, or nothing equipped apply instead.
    /// </summary>
    public static int UpgradeMarkScore(ItemInstance item, Equipment eq)
    {
        if (item == null || eq == null || item.slot == ItemSlot.Consumable) return 0;
        if (!Equipment.CanEquip(item, RunProfile.ClassId)) return 0;
        return ItemCompare.Score(item, eq.GetEquipped(item.slot));
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

    /// <summary>Wire the tooltip column Sell button once (Equip/Discard are wired at build).</summary>
    void WireTooltipActions()
    {
        if (tcWired || tcSellButton == null) { tcWired = tcSellButton != null; return; }
        tcWired = true;
        tcSellButton.onClick.AddListener(() =>
        {
            int i = tcBagIndex;
            if (i < 0 || inventory == null || wallet == null) return;
            var it = inventory.Get(i);
            if (it == null) { SetNotice(Loc.T("inv.notice.gone")); Refresh(); return; }
            int price = VendorStock.SellPrice(it);
            if (VendorStock.TrySell(i, wallet, inventory))
            {
                DiabloAudio.PlayVendorSell();
                SetNotice(Loc.F("inv.notice.sold", LootDrop.ItemLabel(it), price));
            }
            else SetNotice(Loc.F("inv.notice.nosell", LootDrop.ItemLabel(it)));
            Refresh();
        });
    }

    /// <summary>Show the current bag selection in the fixed tooltip column.</summary>
    void FillTooltipColumnForSelection()
    {
        if (inventory != null && inventory.Count > 0)
        {
            int i = SelectedOrLast();
            if (i >= 0)
            {
                var it = inventory.Get(i);
                if (it != null) { FillTooltipColumn(it, i, null); return; }
            }
        }
        FillTooltipColumn(null, -1, null);
    }

    /// <summary>
    /// Fill the fixed tooltip column: title, rarity/slot/level, detail card,
    /// comparison against the worn item, and context actions (Anlegen/Ablegen,
    /// Verkaufen, Wegwerfen) on the authoritative equip path.
    /// </summary>
    void FillTooltipColumn(ItemInstance item, int bagIndex, ItemSlot? slot)
    {
        tcBagIndex = bagIndex;
        tcSlot = slot;
        if (item == null)
        {
            if (tcTitleText != null)
            {
                tcTitleText.text = Loc.T("inv.tc.title");
                tcTitleText.color = DiabloPalette.ParchmentDim;
            }
            if (tcMetaText != null) tcMetaText.text = "";
            if (tcBodyText != null) tcBodyText.text = Loc.T("inv.tc.empty");
            if (tcCompareText != null) tcCompareText.text = "";
            SetTcLabel(tcEquipButton, Loc.T("inv.tc.equip"));
            return;
        }
        var equipped = equipment != null && item.slot != ItemSlot.Consumable
            ? equipment.GetEquipped(item.slot) : null;
        if (tcTitleText != null)
        {
            tcTitleText.text = LootDrop.ItemLabel(item);
            tcTitleText.color = DiabloPalette.Rarity(item.rarity);
        }
        if (tcMetaText != null)
        {
            string level = item.itemLevel <= 0 ? Loc.T("item.card.ilvl_legacy") : Loc.F("item.card.ilvl", item.itemLevel);
            tcMetaText.text = Loc.F("inv.tip.meta", DiabloPalette.ToHex(DiabloPalette.ParchmentDim),
                ItemText.Rarity(item.rarity), ItemText.Slot(item.slot), level);
        }
        if (tcBodyText != null) tcBodyText.text = ItemDetailsView.Render(item, equipped);
        if (tcCompareText != null)
        {
            string cmp = CompareSummary(item, equipped);
            tcCompareText.text = string.IsNullOrEmpty(cmp)
                ? Loc.T("inv.nobonus")
                : Loc.T("inv.tc.compare") + "\n" + cmp;
        }
        bool worn = slot.HasValue;
        SetTcLabel(tcEquipButton, Loc.T(worn ? "inv.tc.unequip" : "inv.tc.equip"));
        var equipBtn = tcEquipButton;
        if (equipBtn != null)
        {
            equipBtn.onClick.RemoveAllListeners();
            if (worn) equipBtn.onClick.AddListener(() => UnequipSlot((int)slot.Value));
            else equipBtn.onClick.AddListener(() => { if (tcBagIndex >= 0) EquipIndex(tcBagIndex); });
        }
    }

    static void SetTcLabel(Button b, string text)
    {
        if (b == null) return;
        var label = b.GetComponentInChildren<Text>();
        if (label != null) label.text = text;
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
        for (int i = bagList.childCount - 1; i >= 0; i--)
        {
            var child = bagList.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        int count = inventory != null ? inventory.Count : 0;
        if (bagCountText != null)
            bagCountText.text = Loc.F("inv.bag.countshort", count, Inventory.Capacity);
        for (int i = 0; i < Inventory.Capacity; i++)
        {
            var item = inventory != null && i < inventory.Count ? inventory.Get(i) : null;
            var cell = MakeBagCell(item, i);
            cell.transform.SetParent(bagList, false);
        }
        // Artboard grid is 8x5 with Capacity usable cells; the rest renders locked.
        for (int i = Inventory.Capacity; i < 40; i++)
            MakeLockedCell().transform.SetParent(bagList, false);
        if (selectedIndex >= count) selectedIndex = -1;
    }

    int SelectedOrLast() =>
        inventory == null ? -1 : selectedIndex >= 0 && selectedIndex < inventory.Count ? selectedIndex : inventory.Count - 1;

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

    /// <summary>
    /// Difficulty changes only with a new run: enemy HP scales at wake-up. The run restarts
    /// the campaign and keeps the hero (<see cref="RunProfile.StartCampaign"/>), reloads the zone.
    /// </summary>
    public void StartNewRun(string difficulty)
    {
        if (!Diablo.IsSolo()) return;
        DiabloAudio.PlayUIClick();
        RunProfile.StartCampaign(difficulty);
        if (Diablo.IsLocalAuthority())
        {
            SaveService.SaveNow(gameObject);
            SaveService.ContinueAfterReload = true; // the reloaded scene resumes this hero
            SaveService.ContinueAfterReloadPath = null;
        }
        SceneReload.ReloadActiveScene();
    }

    /// <summary>Settings line for the difficulty row (test seam).</summary>
    public static string DifficultyText() =>
        Loc.F("inv.diff.current", Loc.Display(DifficultyProfile.Current.name)) +
        (RunProfile.DifficultyUnlocked ? "" : Loc.T("inv.diff.locked"));

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
