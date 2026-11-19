using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blood-moor tabbed menu (UGUI, built at runtime — no prefab churn).
/// Tabs: Inventory (3 equipped slots + stats) | Bag (5x4 grid, click to
/// equip) | Stats (derived numbers) | Settings (volumes, quality, toggles,
/// controls). `I` toggles, `1-4` switch tabs, `Esc` closes. While open the
/// hero keeps moving/aiming but clicks go to the panel, never to strikes
/// (see <see cref="DiabloClickController.ShouldBlockClicks"/>).
/// Public contract preserved for tests: IsOpen, SetOpen, EquipIndex, Refresh.
/// </summary>
public sealed class DiabloInventoryUI : MonoBehaviour
{
    public enum MenuTab { Inventory, Bag, Stats, Settings }

    public bool IsOpen { get; private set; }
    public MenuTab CurrentTab { get; private set; }

    Canvas canvas;
    Text statsText;
    Text armorText;
    Text weaponText;
    Text helmetText;
    Transform bagList;
    GameObject inventoryPage;
    GameObject bagPage;
    GameObject statsPage;
    GameObject settingsPage;
    Text statsDetailText;
    Text bagDetailText;
    Text bagCountText;
    Text settingsInfoText;
    Slider masterSlider;
    Slider musicSlider;
    Slider sfxSlider;
    Text qualityLabel;
    Button[] tabButtons = new Button[4];
    Image[] equipIcons = new Image[3];
    bool building;

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
        inventory = GetComponent<Inventory>();
        equipment = GetComponent<Equipment>();
        health = GetComponent<PlayerHealth>();
        if (canvas != null) return;
        BuildUI();
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.iKey.wasPressedThisFrame || kb.bKey.wasPressedThisFrame) SetOpen(!IsOpen);
        if (kb.escapeKey.wasPressedThisFrame && IsOpen) SetOpen(false);
        if (!IsOpen) return;
        if (kb.digit1Key.wasPressedThisFrame) ShowTab(MenuTab.Inventory);
        else if (kb.digit2Key.wasPressedThisFrame) ShowTab(MenuTab.Bag);
        else if (kb.digit3Key.wasPressedThisFrame) ShowTab(MenuTab.Stats);
        else if (kb.digit4Key.wasPressedThisFrame) ShowTab(MenuTab.Settings);
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        if (open) { CurrentTab = MenuTab.Inventory; ShowTab(CurrentTab); Refresh(); }
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

    /// <summary>Equip bag[index] (button hook); refreshes stats and HP max.</summary>
    public void EquipIndex(int index)
    {
        EnsureBuilt();
        if (inventory == null || equipment == null) return;
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
        {
            var item = inventory.Get(index);
            if (item != null) relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Equip, item);
            return;
        }
        if (inventory.EquipFromBag(index, equipment))
        {
            if (health != null) health.Refresh();
            DiabloAudio.PlayUIClick();
            Refresh();
        }
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
        var relay = GetComponent<DiabloNetworkPlayer>();
        if (relay != null && relay.IsSpawned)
            return relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.Discard, item);
        if (LootDrop.SpawnItem(transform.position + transform.forward * 1.5f, item, false) == null)
            return false;
        inventory.RemoveAt(index);
        Refresh();
        return true;
    }

    /// <summary>Rebuild slot labels, stats and bag grid from state.</summary>
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
            PaintEquipIcon(0, equipment.armor);
            PaintEquipIcon(1, equipment.weapon);
            PaintEquipIcon(2, equipment.helmet);
            if (statsDetailText != null) statsDetailText.text = StatsBlock();
        }
        RebuildBag();
        RefreshSettingsLabels();
        PaintTabs();
    }

    /// <summary>S3 area tooltip line: IncreasedArea percent + radius factor (test seam).</summary>
    public static string AreaLine(Equipment eq) =>
        eq == null ? "Area  +0%" : $"Area  +{eq.AreaBonusPct():0.#}% (radius x{eq.ScaleArea(1f):0.###})";

    string StatsBlock()
    {
        float armor = 0f;
        if (equipment.armor != null) armor += equipment.armor.GetAffix("Armor");
        if (equipment.helmet != null) armor += equipment.helmet.GetAffix("MaxHP") * 0f; // helmets carry HP, not armor
        return $"Damage  {equipment.GetDamage():0.#}\n" +
               $"Max HP  {equipment.GetMaxHP():0.#}\n" +
               $"Crit  {equipment.GetCrit():0.#}%\n" +
               AreaLine(equipment) + "\n" +
               $"XP gain  {equipment.GetXPMul():0.#}%\n" +
               $"Move speed  {equipment.GetMoveSpeed():0.##}\n" +
               $"Armor  {armor:0.#}\n\n" +
               "Armor feeds Max HP, weapons feed damage.\nRare beats Magic; ember-orange Unique wins.";
    }

    static string SlotLine(string slot, ItemInstance item) =>
        item == null ? $"{slot}: empty" : $"{slot}: {LootDrop.ItemLabel(item)} [{item.rarity}]";

    static string AffixSummary(ItemInstance item)
    {
        if (item == null || item.affixes == null || item.affixes.Count == 0) return "no bonuses";
        var parts = new System.Collections.Generic.List<string>();
        foreach (var a in item.affixes) parts.Add(AffixLibrary.Describe(a));
        return string.Join(", ", parts.ToArray());
    }

    /// <summary>Bag detail line: item label plus the comparison to the equipped item.</summary>
    string DetailFor(ItemInstance item)
    {
        var equipped = equipment != null ? equipment.GetEquipped(item.slot) : null;
        string fit = Equipment.CanEquip(item, RunProfile.ClassId) ? ""
            : $"<color={WorseColor}>{ClassCatalog.Get(ItemDatabase.ClassRestriction(item.baseType)).displayName} only</color>\n";
        string effect = BuildUniqueEffects.EffectTextFor(item);
        if (!string.IsNullOrEmpty(effect)) effect = $"<color=#ff7d1a>{effect}</color>\n";
        return fit + effect + $"{LootDrop.ItemLabel(item)} vs {(equipped != null ? LootDrop.ItemLabel(equipped) : "empty slot")}:\n" +
               CompareSummary(item, equipped);
    }

    public const string BetterColor = "#6fd36f";
    public const string WorseColor = "#e05a4a";

    /// <summary>
    /// Replayability S2 tooltip: per affix key the summed value of
    /// <paramref name="item"/> and its difference to <paramref name="equipped"/>
    /// in the same slot, green when better and red when worse (rich text).
    /// Keys only on the equipped item show as losses.
    /// </summary>
    public static string CompareSummary(ItemInstance item, ItemInstance equipped)
    {
        if (item == null) return "";
        var keys = new System.Collections.Generic.List<string>();
        void Collect(ItemInstance it)
        {
            if (it?.affixes == null) return;
            foreach (var a in it.affixes)
                if (!keys.Contains(a.key)) keys.Add(a.key);
        }
        Collect(item);
        Collect(equipped);
        if (keys.Count == 0) return "no bonuses";
        var lines = new System.Collections.Generic.List<string>();
        foreach (var key in keys)
        {
            float mine = item.GetAffix(key);
            float theirs = equipped != null ? equipped.GetAffix(key) : 0f;
            float delta = mine - theirs;
            string text = AffixLibrary.Describe(new Affix(key, mine));
            if (System.Math.Abs(delta) < 0.0001f)
                lines.Add(text);
            else
                lines.Add($"{text} <color={(delta > 0f ? BetterColor : WorseColor)}>({(delta > 0f ? "+" : "")}{delta:0.##})</color>");
        }
        return string.Join("\n", lines.ToArray());
    }

    void RebuildBag()
    {
        if (bagList == null) return;
        // EditMode-safe: Destroy() logs an error outside play mode.
        foreach (Transform child in bagList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        if (inventory == null)
        {
            if (bagCountText != null) bagCountText.text = "Bag is unavailable.";
            return;
        }
        if (bagCountText != null) bagCountText.text = $"Bag {inventory.Count} of {Inventory.Capacity}. Click an item to equip it.";
        if (bagDetailText != null)
            bagDetailText.text = inventory.Count == 0
                ? "Empty. Husks and the west chest drop loot; walk over it."
                : DetailFor(inventory.Get(inventory.Count - 1));
        for (int i = 0; i < Inventory.Capacity; i++)
        {
            var item = inventory.Get(i);
            int index = i; // closure copy
            var cell = MakeBagCell(item, index);
            cell.transform.SetParent(bagList, false);
            if (item != null)
            {
                var btn = cell.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => EquipIndex(index));
            }
        }
    }

    // ---------- build ----------

    void BuildUI()
    {
        building = true;
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
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
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 620f);
        rect.anchoredPosition = new Vector2(-16f, 0f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Header banner.
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(panelGo.transform, false);
        var headerImg = headerGo.AddComponent<Image>();
        headerImg.sprite = DiabloMenuArt.HeaderBanner();
        headerImg.type = Image.Type.Sliced;
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0f, 52f);
        MakeBannerLabel(headerGo.transform, "Blood-moor menu", 20);
        MakeLabel(panelGo.transform, "Press I to close. Keys 1 to 4 switch tabs.", 13);

        // Tab bar.
        var tabGo = new GameObject("TabBar");
        tabGo.transform.SetParent(panelGo.transform, false);
        var tabs = tabGo.AddComponent<HorizontalLayoutGroup>();
        tabs.spacing = 6f;
        tabs.childControlWidth = true;
        tabs.childControlHeight = true;
        string[] names = { "Inventory", "Bag", "Stats", "Settings" };
        for (int i = 0; i < 4; i++)
        {
            int tab = i;
            var b = MakeTabButton(tabGo.transform, names[i], i == 0);
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
        building = false;
    }

    GameObject MakePage(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        return go;
    }

    void BuildInventoryPage()
    {
        MakeLabel(inventoryPage.transform, "Equipped. Click a slot to stash it.", 16);
        string[] slots = { "Armor", "Weapon", "Helmet" };
        for (int s = 0; s < 3; s++)
        {
            int slot = s;
            var row = new GameObject("EquipRow" + s);
            row.transform.SetParent(inventoryPage.transform, false);
            var hz = row.AddComponent<HorizontalLayoutGroup>();
            hz.spacing = 8f;
            hz.childControlWidth = true;

            var iconGo = new GameObject("EquipIcon" + s);
            iconGo.transform.SetParent(row.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = DiabloMenuArt.SlotBackground(null);
            icon.preserveAspect = true;
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(56f, 56f);
            equipIcons[s] = icon;

            var btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => UnequipSlot(slot));
            var label = MakeLabel(row.transform, slots[s] + ": —", 16);
            if (s == 0) armorText = label;
            else if (s == 1) weaponText = label;
            else helmetText = label;
        }
        statsText = MakeLabel(inventoryPage.transform, "", 16);
    }

    void BuildBagPage()
    {
        bagCountText = MakeLabel(bagPage.transform, "Bag", 16);
        var gridGo = new GameObject("BagGrid");
        gridGo.transform.SetParent(bagPage.transform, false);
        var grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.cellSize = new Vector2(88f, 88f);
        grid.spacing = new Vector2(6f, 6f);
        bagList = gridGo.transform;
        bagDetailText = MakeLabel(bagPage.transform, "", 14);
        // S2 compare tooltip spans several lines.
        bagDetailText.supportRichText = true;
        bagDetailText.verticalOverflow = VerticalWrapMode.Overflow;
        var detailLayout = bagDetailText.gameObject.AddComponent<LayoutElement>();
        detailLayout.minHeight = 120f;
        detailLayout.preferredHeight = 120f;
    }

    void BuildStatsPage()
    {
        MakeLabel(statsPage.transform, "Hero", 18);
        statsDetailText = MakeLabel(statsPage.transform, "", 16);
        MakeLabel(statsPage.transform, "WASD moves, mouse aims, click strikes. I opens the menu, 1 to 4 switch tabs, Esc closes.", 13);
    }

    void BuildSettingsPage()
    {
        MakeLabel(settingsPage.transform, "Settings", 18);
        masterSlider = MakeSlider(settingsPage.transform, "Master", DiabloSettings.MasterVol, v => { DiabloSettings.MasterVol = v; DiabloSettings.Apply(); });
        musicSlider = MakeSlider(settingsPage.transform, "Music", DiabloSettings.MusicVol, v => { DiabloSettings.MusicVol = v; DiabloSettings.Apply(); });
        sfxSlider = MakeSlider(settingsPage.transform, "SFX", DiabloSettings.SFXVol, v => { DiabloSettings.SFXVol = v; DiabloSettings.Apply(); });
        qualityLabel = MakeLabel(settingsPage.transform, "", 15);
        var qRow = new GameObject("QualityRow");
        qRow.transform.SetParent(settingsPage.transform, false);
        var qHz = qRow.AddComponent<HorizontalLayoutGroup>();
        qHz.spacing = 6f;
        string[] names = { "Low", "Med", "High" };
        for (int q = 0; q < 3; q++)
        {
            int level = q;
            var b = MakeSmallButton(qRow.transform, names[q]);
            b.onClick.AddListener(() => { DiabloSettings.QualityLevel = level; DiabloSettings.Save(); RefreshSettingsLabels(); DiabloAudio.PlayUIClick(); });
        }
        var tRow = new GameObject("ToggleRow");
        tRow.transform.SetParent(settingsPage.transform, false);
        var tHz = tRow.AddComponent<HorizontalLayoutGroup>();
        tHz.spacing = 6f;
        var fpsBtn = MakeSmallButton(tRow.transform, "FPS");
        fpsBtn.onClick.AddListener(() => { DiabloSettings.ShowFPS = !DiabloSettings.ShowFPS; DiabloSettings.Apply(); DiabloSettings.Save(); RefreshSettingsLabels(); });
        var shakeBtn = MakeSmallButton(tRow.transform, "Shake");
        shakeBtn.onClick.AddListener(() => { DiabloSettings.ScreenShake = !DiabloSettings.ScreenShake; DiabloSettings.Save(); RefreshSettingsLabels(); });
        var hitBtn = MakeSmallButton(tRow.transform, "HitStop");
        hitBtn.onClick.AddListener(() => { DiabloSettings.HitStop = !DiabloSettings.HitStop; DiabloSettings.Save(); RefreshSettingsLabels(); });
        // replayability S4: difficulty for the next run (Kindled after the first Abbot kill).
        difficultyLabel = MakeLabel(settingsPage.transform, "", 15);
        var dRow = new GameObject("DifficultyRow");
        dRow.transform.SetParent(settingsPage.transform, false);
        var dHz = dRow.AddComponent<HorizontalLayoutGroup>();
        dHz.spacing = 6f;
        var normalBtn = MakeSmallButton(dRow.transform, "New run (Normal)");
        normalBtn.onClick.AddListener(() => StartNewRun(RunProfile.Normal));
        kindledButton = MakeSmallButton(dRow.transform, "New run (Kindled)");
        kindledButton.onClick.AddListener(() => StartNewRun(RunProfile.Kindled));
        settingsInfoText = MakeLabel(settingsPage.transform, "", 14);
        var closeBtn = MakeSmallButton(settingsPage.transform, "Close menu (I)");
        closeBtn.onClick.AddListener(() => SetOpen(false));
        RefreshSettingsLabels();
    }

    Text difficultyLabel;
    Button kindledButton;

    /// <summary>
    /// Difficulty changes only with a new run: enemy HP scales at wake-up, so a
    /// mid-run switch would mix profiles. Reloads the zone with a fresh layout seed.
    /// </summary>
    void StartNewRun(string difficulty)
    {
        if (!Diablo.IsSolo()) return; // co-op: the host scene is shared
        DiabloAudio.PlayUIClick();
        DifficultyProfile.Select(difficulty);
        RunProfile.RunSeed = RunProfile.NewSeed();
        if (Diablo.IsLocalAuthority()) SaveService.SaveNow(gameObject);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    /// <summary>Settings line for the difficulty row (test seam).</summary>
    public static string DifficultyText() =>
        $"Difficulty: {DifficultyProfile.Current.name}" +
        (RunProfile.DifficultyUnlocked ? "" : "  (Kindled locked: defeat the Hollow Abbot)");

    void RefreshSettingsLabels()
    {
        if (building) return;
        if (difficultyLabel != null) difficultyLabel.text = DifficultyText();
        if (kindledButton != null) kindledButton.interactable = RunProfile.DifficultyUnlocked;
        if (qualityLabel != null) qualityLabel.text = $"Quality: {DiabloSettings.QualityName}  (Low/Med/High)";
        if (settingsInfoText != null)
            settingsInfoText.text =
                $"FPS {(DiabloSettings.ShowFPS ? "on" : "off")}. " +
                $"Shake {(DiabloSettings.ScreenShake ? "on" : "off")}. " +
                $"HitStop {(DiabloSettings.HitStop ? "on" : "off")}.\n" +
                "WASD moves, mouse aims, click strikes. I opens the menu, 1 to 4 switch tabs, Esc closes.";
        if (masterSlider != null && Mathf.Abs(masterSlider.value - DiabloSettings.MasterVol) > 0.001f) masterSlider.value = DiabloSettings.MasterVol;
        if (musicSlider != null && Mathf.Abs(musicSlider.value - DiabloSettings.MusicVol) > 0.001f) musicSlider.value = DiabloSettings.MusicVol;
        if (sfxSlider != null && Mathf.Abs(sfxSlider.value - DiabloSettings.SFXVol) > 0.001f) sfxSlider.value = DiabloSettings.SFXVol;
    }

    void PaintTabs()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            var b = tabButtons[i];
            if (b == null) continue;
            var img = b.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = DiabloMenuArt.TabBackground((int)CurrentTab == i);
                img.type = Image.Type.Sliced;
            }
        }
    }

    void PaintEquipIcon(int slot, ItemInstance item)
    {
        var img = equipIcons[slot];
        if (img == null) return;
        if (item == null)
        {
            img.sprite = DiabloMenuArt.SlotBackground(null);
            img.color = Color.white;
        }
        else
        {
            img.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
            img.color = Color.white;
        }
    }

    // ---------- widget helpers ----------

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

    Button MakeTabButton(Transform parent, string text, bool active)
    {
        var go = new GameObject("Tab");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = DiabloMenuArt.TabBackground(active);
        image.type = Image.Type.Sliced;
        var button = go.AddComponent<Button>();
        var label = MakeLabel(go.transform, text, 15);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = active ? DiabloPalette.TrimGold : DiabloPalette.ParchmentDim;
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
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

    Slider MakeSlider(Transform parent, string name, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        var row = new GameObject(name + "Row");
        row.transform.SetParent(parent, false);
        var hz = row.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = 8f;
        MakeLabel(row.transform, name, 15);
        var sliderGo = new GameObject(name + "Slider");
        sliderGo.transform.SetParent(row.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        var bg = sliderGo.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.SlotBackground(null);
        bg.type = Image.Type.Sliced;
        slider.targetGraphic = bg;
        slider.onValueChanged.AddListener(onChange);
        var rect = sliderGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 22f);
        return slider;
    }

    GameObject MakeBagCell(ItemInstance item, int bagIndex)
    {
        var go = new GameObject(item == null ? "BagEmpty" : $"BagButton{bagIndex}");
        var button = go.AddComponent<Button>();
        var frame = go.AddComponent<Image>();
        frame.sprite = DiabloMenuArt.SlotBackground(item.HasValue() ? item.rarity : (ItemRarity?)null);
        frame.type = Image.Type.Sliced;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(88f, 88f);
        if (item != null)
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0.5f, 0.62f);
            ir.anchorMax = new Vector2(0.5f, 0.62f);
            ir.sizeDelta = new Vector2(48f, 48f);
            ir.anchoredPosition = Vector2.zero;
            var label = MakeLabel(go.transform, $"{LootDrop.ItemLabel(item)}", 11);
            label.alignment = TextAnchor.LowerCenter;
            label.color = DiabloPalette.Rarity(item.rarity);
            var lr = label.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(2f, 2f);
            lr.offsetMax = new Vector2(-2f, 0f);
        }
        else
        {
            button.interactable = false;
        }
        return go;
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

static class DiabloMenuItemExt
{
    public static bool HasValue(this ItemInstance item) => item != null;
}
