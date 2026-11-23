using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sanctuary Inventory, Bag, Character Codex & Settings panel (I / B toggles, 1-4 tabs, Esc closes).
/// Preserves all EditMode/PlayMode test contracts: MenuTab, IsOpen, CurrentTab, BetterColor,
/// WorseColor, SetOpen, ShowTab, EquipIndex, UnequipSlot, Refresh, CompareSummary, DifficultyText,
/// InventoryCanvas, BagGrid, BagButton{i}, and BagEmpty (20 cells).
/// </summary>
public sealed class DiabloInventoryUI : MonoBehaviour
{
    public enum MenuTab { Inventory, Bag, Stats, Settings }

    public bool IsOpen { get; private set; }
    public MenuTab CurrentTab { get; private set; }

    Canvas canvas;
    GameObject inventoryPage;
    GameObject bagPage;
    GameObject statsPage;
    GameObject settingsPage;

    Text heroHeaderText;
    Text armorText;
    Text weaponText;
    Text helmetText;
    Text statsText;
    Text bagCountText;
    Text bagDetailText;
    Text statsDetailText;
    Transform bagList;

    readonly Image[] equipIcons = new Image[3];
    readonly Image[] equipFrames = new Image[3];
    readonly Button[] tabButtons = new Button[4];
    readonly List<UnityEngine.Events.UnityAction> settingsRefreshes = new List<UnityEngine.Events.UnityAction>();

    Inventory inventory;
    Equipment equipment;
    PlayerHealth health;
    Wallet wallet;
    DiabloProgression prog;

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
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.iKey.wasPressedThisFrame || kb.bKey.wasPressedThisFrame)
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
        if (!IsOpen) return;
        if (kb.digit1Key.wasPressedThisFrame) ShowTab(MenuTab.Inventory);
        else if (kb.digit2Key.wasPressedThisFrame) ShowTab(MenuTab.Bag);
        else if (kb.digit3Key.wasPressedThisFrame) ShowTab(MenuTab.Stats);
        else if (kb.digit4Key.wasPressedThisFrame) ShowTab(MenuTab.Settings);
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (open) DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
        if (canvas != null) canvas.enabled = open;
        if (open)
        {
            CurrentTab = MenuTab.Inventory;
            ShowTab(CurrentTab);
            Refresh();
        }
    }

    public void ShowTab(MenuTab tab)
    {
        EnsureBuilt();
        CurrentTab = tab;
        if (inventoryPage != null) inventoryPage.SetActive(tab == MenuTab.Inventory);
        if (bagPage != null) bagPage.SetActive(tab == MenuTab.Bag);
        if (statsPage != null) statsPage.SetActive(tab == MenuTab.Stats);
        if (settingsPage != null) settingsPage.SetActive(tab == MenuTab.Settings);
        PaintTabs();
        if (IsOpen) DiabloAudio.PlayUIClick();
    }

    /// <summary>Equip bag[index] (or drink potion when consumable); refreshes stats and HP max.</summary>
    public void EquipIndex(int index)
    {
        EnsureBuilt();
        if (inventory == null || equipment == null) return;
        var item = inventory.Get(index);
        if (item == null) return;
        if (item.slot == ItemSlot.Consumable)
        {
            DrinkBagPotion(index);
            return;
        }
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
            SetNotice(candidate != null ? $"Equipped {LootDrop.ItemLabel(candidate)}." : "Equipped.");
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
        int before = equipment != null && equipment.GetEquipped(inventory.Get(index).slot) != null ? 1 : 0;
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

    void DrinkBagPotion(int index)
    {
        var item = inventory != null ? inventory.Get(index) : null;
        if (item == null || item.slot != ItemSlot.Consumable) return;
        if (!inventory.RemoveAt(index)) return;
        if (health != null) health.Heal(ItemDatabase.PotionHeal);
        DiabloAudio.PlayUIClick();
        Debug.Log($"[Inventory] Drank healing potion (+{ItemDatabase.PotionHeal} HP).");
        Refresh();
    }

    /// <summary>Move an equipped slot back into the bag (button hook).</summary>
    public void UnequipSlot(int slotIndex)
    {
        EnsureBuilt();
        if (inventory == null || equipment == null) return;
        var slot = (ItemSlot)slotIndex;
        if (slot != ItemSlot.Weapon && slot != ItemSlot.Armor && slot != ItemSlot.Helmet) return;
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
        {
            var item = equipment.GetEquipped(slot);
            if (item != null) relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Unequip, item);
            return;
        }
        if (equipment.UnequipTo(slot, inventory))
        {
            if (health != null) health.Refresh();
            DiabloAudio.PlayUIClick();
            Refresh();
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

    /// <summary>Rebuild slot labels, stats and bag grid from state.</summary>
    public void Refresh()
    {
        EnsureBuilt();
        int lv = prog != null ? prog.Level : 1;
        int gold = wallet != null ? wallet.Gold : 0;
        int potions = CountPotions();
        if (heroHeaderText != null)
            heroHeaderText.text =
                Loc.F("inv.hero.header", DiabloPalette.ToHex(DiabloPalette.GoldBright), RunProfile.Class.displayName, lv, Loc.Display(RunProfile.Difficulty)) +
                Loc.F("inv.hero.gold", DiabloPalette.ToHex(DiabloPalette.TrimGold), gold, potions);

        if (equipment != null)
        {
            armorText.text = SlotLine(Loc.T("item.slot.armor"), equipment.armor);
            weaponText.text = SlotLine(Loc.T("item.slot.weapon"), equipment.weapon);
            helmetText.text = SlotLine(Loc.T("item.slot.helmet"), equipment.helmet);
            statsText.text =
                Loc.T("inv.stats.combat") +
                Loc.F("inv.stats.dmg", DiabloPalette.ToHex(DiabloPalette.TrimGold), equipment.GetDamage()) +
                Loc.F("inv.stats.maxhp", BetterColor, equipment.GetMaxHP()) +
                $"Crit {equipment.GetCrit():0.#}%\n" +
                Loc.F("inv.stats.speed", equipment.GetMoveSpeed(), equipment.GetXPMul()) +
                $"CDR {Mathf.Min(Equipment.MaxCooldownReductionPct, equipment.Total(AffixLibrary.CooldownReduction)):0.#}%";
            PaintEquipIcon(0, equipment.armor);
            PaintEquipIcon(1, equipment.weapon);
            PaintEquipIcon(2, equipment.helmet);
            if (statsDetailText != null) statsDetailText.text = StatsBlock();
        }
        RebuildBag();
        if (noticeText != null) noticeText.text = Notice;
        foreach (var r in settingsRefreshes) r?.Invoke();
        PaintTabs();
    }

    /// <summary>S3 area tooltip line: IncreasedArea percent + radius factor (test seam).</summary>
    public static string AreaLine(Equipment eq) =>
        eq == null ? "Area  +0%" : Loc.F("inv.stats.area", eq.AreaBonusPct(), eq.ScaleArea(1f));

    int CountPotions()
    {
        if (inventory == null) return 0;
        int n = 0;
        for (int i = 0; i < inventory.Count; i++)
            if (inventory.Get(i)?.slot == ItemSlot.Consumable) n++;
        return n;
    }

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

    static string SlotLine(string slot, ItemInstance item)
    {
        if (item == null) return Loc.F("inv.slot.empty", DiabloPalette.ToHex(DiabloPalette.ParchmentDim), slot);
        string rHex = DiabloPalette.ToHex(DiabloPalette.Rarity(item.rarity));
        return Loc.F("inv.slot.item", rHex, LootDrop.ItemLabel(item), DiabloPalette.ToHex(DiabloPalette.ParchmentDim), ItemText.Rarity(item.rarity)) +
               $"<size=13>{AffixSummary(item)}</size>";
    }

    static string AffixSummary(ItemInstance item)
    {
        if (item == null || item.affixes == null || item.affixes.Count == 0) return Loc.T("inv.nobonus.cap");
        var parts = new List<string>();
        foreach (var a in item.affixes) parts.Add(AffixLibrary.Describe(a));
        return string.Join(", ", parts.ToArray());
    }

    /// <summary>Bag detail line: the shared S5 item card (inventory reuse).</summary>
    string DetailFor(ItemInstance item)
    {
        if (item == null) return Loc.T("inv.baghint");
        if (item.slot == ItemSlot.Consumable)
            return Loc.F("inv.potion.tip", LootDrop.ItemLabel(item), BetterColor, ItemDatabase.PotionHeal);
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
        var keys = new List<string>();
        void Collect(ItemInstance it)
        {
            if (it?.affixes == null) return;
            foreach (var a in it.affixes)
                if (!keys.Contains(a.key)) keys.Add(a.key);
        }
        Collect(item);
        Collect(equipped);
        if (keys.Count == 0) return Loc.T("inv.nobonus");
        var lines = new List<string>();
        foreach (var key in keys)
        {
            float mine = item.GetAffix(key);
            float theirs = equipped != null ? equipped.GetAffix(key) : 0f;
            float delta = mine - theirs;
            string text = AffixLibrary.Describe(new Affix(key, mine));
            if (equipped != null && Mathf.Abs(delta) > 0.001f)
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
        int potions = CountPotions();
        if (bagCountText != null)
            bagCountText.text = Loc.F("inv.bag.count", count, Inventory.Capacity, (wallet != null ? wallet.Gold : 0), potions);
        for (int i = 0; i < Inventory.Capacity; i++)
        {
            var item = inventory != null && i < inventory.Count ? inventory.Get(i) : null;
            var cell = MakeBagCell(item, i);
            cell.transform.SetParent(bagList, false);
        }
        if (bagDetailText != null)
        {
            if (count > 0)
            {
                var latest = inventory.Get(count - 1);
                bagDetailText.text = DetailFor(latest);
            }
            else
            {
                bagDetailText.text = Loc.T("inv.bag.empty");
            }
        }
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 10);

        var panelGo = DiabloUIFactory.MakePanel(canvasGo.transform, "Panel", new Vector2(540f, 760f), new Vector2(-18f, 0f));
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(1f, 0.5f);
        pr.anchorMax = new Vector2(1f, 0.5f);
        pr.pivot = new Vector2(1f, 0.5f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        DiabloUIFactory.MakeHeader(panelGo.transform, Loc.T("inv.title"), DiabloMenuArt.MenuArtSprite("CrestCinderreach"), () => SetOpen(false));

        // Tab Bar
        var tabGo = DiabloUIFactory.MakeHorizontalRow(panelGo.transform, "TabBar", 6f, 38f);
        string[] names = { Loc.T("inv.tab.inventory"), Loc.T("inv.tab.bag"), Loc.T("inv.tab.stats"), Loc.T("inv.tab.settings") };
        for (int i = 0; i < 4; i++)
        {
            int tab = i;
            var b = MakeTabButton(tabGo.transform, names[i], i == 0);
            DiabloUIFactory.SetLayout(b.gameObject, prefH: 36f, flexW: 1f);
            b.onClick.AddListener(() => ShowTab((MenuTab)tab));
            tabButtons[i] = b;
        }

        inventoryPage = MakePage(panelGo.transform, "InventoryPage");
        bagPage = MakePage(panelGo.transform, "BagPage");
        statsPage = MakePage(panelGo.transform, "StatsPage");
        settingsPage = MakePage(panelGo.transform, "SettingsPage");

        BuildInventoryPage();
        BuildBagPage();
        BuildStatsPage();
        BuildSettingsPage();
    }

    GameObject MakePage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefH: 540f, flexW: 1f);
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        return go;
    }

    void BuildInventoryPage()
    {
        // Hero summary bar with 3D Portrait
        var heroBox = new GameObject("HeroCard", typeof(RectTransform));
        heroBox.transform.SetParent(inventoryPage.transform, false);
        DiabloUIFactory.SetLayout(heroBox, prefH: 82f, flexW: 1f);
        var hbBg = heroBox.AddComponent<Image>();
        hbBg.sprite = DiabloMenuArt.TooltipBackground();
        hbBg.type = Image.Type.Sliced;
        var hbHz = heroBox.AddComponent<HorizontalLayoutGroup>();
        hbHz.padding = new RectOffset(10, 10, 8, 8);
        hbHz.spacing = 12f;
        hbHz.childControlWidth = true;
        hbHz.childControlHeight = true;
        hbHz.childForceExpandWidth = false;

        var pFrame = new GameObject("HeroPortraitFrame", typeof(RectTransform));
        pFrame.transform.SetParent(heroBox.transform, false);
        DiabloUIFactory.SetLayout(pFrame, prefH: 64f, prefW: 64f);
        var pfImg = pFrame.AddComponent<Image>();
        pfImg.sprite = DiabloMenuArt.SlotBackground(ItemRarity.Rare, 64);
        pfImg.type = Image.Type.Sliced;
        var pSpr = DiabloMenuArt.ClassPortrait(RunProfile.ClassId);
        if (pSpr != null)
        {
            var pi = new GameObject("PortraitImg", typeof(RectTransform));
            pi.transform.SetParent(pFrame.transform, false);
            var iImg = pi.AddComponent<Image>();
            iImg.sprite = pSpr;
            iImg.preserveAspect = true;
            var pir = pi.GetComponent<RectTransform>();
            pir.anchorMin = Vector2.zero;
            pir.anchorMax = Vector2.one;
            pir.offsetMin = new Vector2(4f, 4f);
            pir.offsetMax = new Vector2(-4f, -4f);
        }
        heroHeaderText = DiabloUIFactory.MakeText(heroBox.transform, "HeroHeaderText", "", 15, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(heroHeaderText.gameObject, prefH: 64f, flexW: 1f);

        var hint = DiabloUIFactory.MakeText(inventoryPage.transform, "EquipHint", Loc.T("inv.equiphint"), 14, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(hint.gameObject, prefH: 20f, flexW: 1f);

        string[] slots = { Loc.T("item.slot.armor"), Loc.T("item.slot.weapon"), Loc.T("item.slot.helmet") };
        for (int s = 0; s < 3; s++)
        {
            int slot = s;
            var row = new GameObject("EquipRow" + s, typeof(RectTransform));
            row.transform.SetParent(inventoryPage.transform, false);
            DiabloUIFactory.SetLayout(row, prefH: 68f, flexW: 1f);
            var rowImg = row.AddComponent<Image>();
            rowImg.sprite = DiabloMenuArt.ButtonBackground(false);
            rowImg.type = Image.Type.Sliced;
            var btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => UnequipSlot(slot));

            var hz = row.AddComponent<HorizontalLayoutGroup>();
            hz.padding = new RectOffset(8, 10, 6, 6);
            hz.spacing = 12f;
            hz.childAlignment = TextAnchor.MiddleLeft;
            hz.childControlWidth = true;
            hz.childControlHeight = false;
            hz.childForceExpandWidth = false;

            var frameGo = new GameObject("EquipFrame" + s, typeof(RectTransform));
            frameGo.transform.SetParent(row.transform, false);
            DiabloUIFactory.SetLayout(frameGo, prefH: 54f, prefW: 54f);
            var fImg = frameGo.AddComponent<Image>();
            fImg.sprite = DiabloMenuArt.SlotBackground(null, 64);
            fImg.type = Image.Type.Sliced;
            fImg.raycastTarget = false;
            equipFrames[s] = fImg;

            var iconGo = new GameObject("EquipIcon" + s, typeof(RectTransform));
            iconGo.transform.SetParent(frameGo.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = DiabloMenuArt.SlotBackground(null, 64);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(5f, 5f);
            ir.offsetMax = new Vector2(-5f, -5f);
            equipIcons[s] = icon;

            var label = DiabloUIFactory.MakeText(row.transform, "SlotLabel", slots[s] + ": —", 15, DiabloPalette.Parchment);
            DiabloUIFactory.SetLayout(label.gameObject, prefH: 52f, flexW: 1f);
            if (s == 0) armorText = label;
            else if (s == 1) weaponText = label;
            else helmetText = label;
        }

        DiabloUIFactory.MakeDivider(inventoryPage.transform);

        var summaryBox = new GameObject("SummaryBox", typeof(RectTransform));
        summaryBox.transform.SetParent(inventoryPage.transform, false);
        DiabloUIFactory.SetLayout(summaryBox, prefH: 96f, flexW: 1f);
        var sbImg = summaryBox.AddComponent<Image>();
        sbImg.sprite = DiabloMenuArt.TooltipBackground();
        sbImg.type = Image.Type.Sliced;
        var sv = summaryBox.AddComponent<VerticalLayoutGroup>();
        sv.padding = new RectOffset(12, 12, 8, 8);
        sv.childControlWidth = true;
        sv.childControlHeight = false;
        statsText = DiabloUIFactory.MakeText(summaryBox.transform, "StatsSummary", "", 15, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(statsText.gameObject, prefH: 76f, flexW: 1f);
    }

    void BuildBagPage()
    {
        var topRow = DiabloUIFactory.MakeHorizontalRow(bagPage.transform, "BagTopRow", 8f, 30f);
        bagCountText = DiabloUIFactory.MakeText(topRow.transform, "BagCount", "Bag", 15, DiabloPalette.TrimGold);
        DiabloUIFactory.SetLayout(bagCountText.gameObject, prefH: 28f, flexW: 1f);
        var potionBtn = DiabloUIFactory.MakeSmallButton(topRow.transform, Loc.T("inv.btn.potion"), () =>
        {
            if (Vendor.DrinkPotion(gameObject)) Refresh();
        }, 28f, 150f);
        potionBtn.name = "PotionQuickButton";

        var gridGo = new GameObject("BagGrid", typeof(RectTransform));
        gridGo.transform.SetParent(bagPage.transform, false);
        DiabloUIFactory.SetLayout(gridGo, prefH: 350f, flexW: 1f);
        var grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.cellSize = new Vector2(92f, 82f);
        grid.spacing = new Vector2(8f, 6f);
        bagList = gridGo.transform;

        var tipBox = new GameObject("BagTooltipBox", typeof(RectTransform));
        tipBox.transform.SetParent(bagPage.transform, false);
        DiabloUIFactory.SetLayout(tipBox, prefH: 132f, flexW: 1f);
        var tbImg = tipBox.AddComponent<Image>();
        tbImg.sprite = DiabloMenuArt.TooltipBackground();
        tbImg.type = Image.Type.Sliced;
        var tv = tipBox.AddComponent<VerticalLayoutGroup>();
        tv.padding = new RectOffset(12, 12, 8, 8);
        tv.childControlWidth = true;
        tv.childControlHeight = false;

        bagDetailText = DiabloUIFactory.MakeText(tipBox.transform, "BagTooltip", "", 14, DiabloPalette.Parchment);
        DiabloUIFactory.FitToBox(bagDetailText, 14);
        DiabloUIFactory.SetLayout(bagDetailText.gameObject, prefH: 114f, flexW: 1f);

        // S5: visible separate actions for the selected (last) bag item.
        var actionRow = DiabloUIFactory.MakeHorizontalRow(bagPage.transform, "BagActions", 8f, 34f);
        DiabloUIFactory.MakeSmallButton(actionRow.transform, Loc.T("inv.btn.equip"), () =>
        {
            if (inventory != null && inventory.Count > 0) EquipIndex(inventory.Count - 1);
        });
        DiabloUIFactory.MakeSmallButton(actionRow.transform, Loc.T("inv.btn.discard"), () =>
        {
            if (inventory != null && inventory.Count > 0) DiscardIndex(inventory.Count - 1);
        });
        noticeText = DiabloUIFactory.MakeText(bagPage.transform, "Notice", "", 14, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(noticeText.gameObject, prefH: 22f, flexW: 1f);
    }

    void BuildStatsPage()
    {
        var box = new GameObject("StatsCard", typeof(RectTransform));
        box.transform.SetParent(statsPage.transform, false);
        DiabloUIFactory.SetLayout(box, prefH: 430f, flexW: 1f);
        var img = box.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TooltipBackground();
        img.type = Image.Type.Sliced;
        var v = box.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(16, 16, 14, 14);
        v.childControlWidth = true;
        v.childControlHeight = false;

        statsDetailText = DiabloUIFactory.MakeText(box.transform, "StatsDetail", "", 15, DiabloPalette.Parchment, TextAnchor.UpperLeft);
        DiabloUIFactory.SetLayout(statsDetailText.gameObject, prefH: 390f, flexW: 1f);

        var controlsBox = DiabloUIFactory.MakeText(statsPage.transform, "ControlsHint",
            Loc.T("inv.controls"),
            13, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(controlsBox.gameObject, prefH: 40f, flexW: 1f);
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
        for (int i = 0; i < tabButtons.Length; i++)
            DiabloUIFactory.StyleButtonActive(tabButtons[i], (int)CurrentTab == i);
    }

    void PaintEquipIcon(int slot, ItemInstance item)
    {
        var frame = equipFrames[slot];
        if (frame != null)
        {
            frame.sprite = DiabloMenuArt.SlotBackground(item != null ? item.rarity : (ItemRarity?)null, 64);
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
        if (item != null)
        {
            button.onClick.AddListener(() => EquipIndex(bagIndex));
            AttachHover(go, () =>
            {
                if (bagDetailText != null) bagDetailText.text = DetailFor(item);
            });

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0.5f, 0.60f);
            ir.anchorMax = new Vector2(0.5f, 0.60f);
            ir.sizeDelta = new Vector2(46f, 46f);
            ir.anchoredPosition = Vector2.zero;

            var label = DiabloUIFactory.MakeText(go.transform, "ItemLabel", ShortLabel(item), 11, DiabloPalette.Rarity(item.rarity),
                TextAnchor.LowerCenter, shadow: true);
            var lr = label.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(3f, 3f);
            lr.offsetMax = new Vector2(-3f, 0f);

            if (item.slot != ItemSlot.Consumable && !Equipment.CanEquip(item, RunProfile.ClassId))
            {
                var badge = DiabloUIFactory.MakeText(go.transform, "ClassLock", "!", 13, DiabloPalette.Blood, TextAnchor.UpperRight);
                var br = badge.rectTransform;
                br.anchorMin = Vector2.zero;
                br.anchorMax = Vector2.one;
                br.offsetMin = Vector2.zero;
                br.offsetMax = new Vector2(-5f, -3f);
            }
        }
        else
        {
            button.interactable = false;
        }
        return go;
    }

    static string ShortLabel(ItemInstance item)
    {
        if (item == null) return "";
        if (item.slot == ItemSlot.Consumable) return Loc.T("inv.potion");
        string full = LootDrop.ItemLabel(item);
        return full.Length > 13 ? full.Substring(0, 12) + "…" : full;
    }

    static void AttachHover(GameObject go, UnityEngine.Events.UnityAction onHover)
    {
        if (EventSystem.current == null) return;
        var trig = go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => onHover());
        trig.triggers.Add(entry);
    }
}

static class DiabloMenuItemExt
{
    public static bool HasValue(this ItemInstance item) => item != null;
}
