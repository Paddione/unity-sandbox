using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3-tab and paperdoll UGUI layout and styling for <see cref="DiabloInventoryUI"/>.
/// Lifecycle, drag-and-drop state, and solo/RPC item actions remain in
/// <c>DiabloInventoryUI.cs</c>.
/// </summary>
public sealed partial class DiabloInventoryUI
{
    // ── Build ────────────────────────────────────────────────────────────

    /// <summary>Footer line (P1 shell, wired in P2): gold/flask and difficulty.</summary>
    Text footerGoldText;
    Text footerDiffText;
    /// <summary>Fixed tooltip column (P1 shell, filled in P2).</summary>
    Text tcTitleText;
    Text tcMetaText;
    Text tcBodyText;
    Text tcCompareText;
    Button tcEquipButton;
    Button tcSellButton;
    Button tcDiscardButton;

    void BuildUI()
    {
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 10);
        canvasRect = canvasGo.GetComponent<RectTransform>();

        var panelGo = DiabloUIFactory.MakePanel(canvasGo.transform, "Panel", new Vector2(1520f, 840f), new Vector2(-280f, 0f));
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(1f, 0.5f);
        pr.anchorMax = new Vector2(1f, 0.5f);
        pr.pivot = new Vector2(1f, 0.5f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Header row (redesign): crest + hero name (DisplayFont) + tabs + close button.
        var headerGo = DiabloUIFactory.MakeHorizontalRow(panelGo.transform, "HeaderRow", 12f, 56f);
        var hhz = headerGo.GetComponent<HorizontalLayoutGroup>();
        hhz.childForceExpandWidth = false;
        var crestGo = new GameObject("Crest", typeof(RectTransform));
        crestGo.transform.SetParent(headerGo.transform, false);
        DiabloUIFactory.SetLayout(crestGo, prefH: 44f, prefW: 44f);
        var crest = crestGo.AddComponent<Image>();
        crest.sprite = DiabloMenuArt.MenuArtSprite("CrestCinderreach");
        crest.preserveAspect = true;
        crest.raycastTarget = false;
        heroHeaderText = DiabloUIFactory.MakeText(headerGo.transform, "HeroHeaderText", "", 20, DiabloPalette.Parchment,
            TextAnchor.MiddleLeft, font: DiabloUIFactory.DisplayFont);
        heroHeaderText.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(heroHeaderText.gameObject, prefH: 48f, flexW: 1f);
        string[] names = { Loc.T("inv.tab.inventory"), Loc.T("inv.tab.stats"), Loc.T("inv.tab.settings") };
        MenuTab[] targets = { MenuTab.Inventory, MenuTab.Stats, MenuTab.Settings };
        for (int i = 0; i < tabButtons.Length; i++)
        {
            var target = targets[i];
            var b = MakeTabButton(headerGo.transform, names[i], i == 0);
            DiabloUIFactory.SetLayout(b.gameObject, prefH: 44f, prefW: 210f);
            b.onClick.AddListener(() => ShowTab(target));
            tabButtons[i] = b;
        }
        DiabloUIFactory.MakeSmallButton(headerGo.transform, "X", () => SetOpen(false), 44f, 64f).name = "CloseButton";

        inventoryPage = MakePage(panelGo.transform, "InventoryPage", 710f, columns: true);
        statsPage = MakePage(panelGo.transform, "StatsPage", 710f);
        settingsPage = MakePage(panelGo.transform, "SettingsPage", 710f);

        BuildInventoryPage();
        BuildStatsPage();
        BuildSettingsPage();

        // Footer row (redesign): gold/flask, difficulty, control hints.
        var footerGo = DiabloUIFactory.MakeHorizontalRow(panelGo.transform, "FooterRow", 12f, 30f);
        footerGoldText = DiabloUIFactory.MakeText(footerGo.transform, "FooterGold", "", 13, DiabloPalette.TrimGold,
            TextAnchor.MiddleLeft, shadow: false, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(footerGoldText.gameObject, prefH: 28f, flexW: 1f);
        footerDiffText = DiabloUIFactory.MakeText(footerGo.transform, "FooterDifficulty", "", 13, DiabloPalette.ParchmentDim,
            TextAnchor.MiddleLeft, shadow: false, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(footerDiffText.gameObject, prefH: 28f, flexW: 1f);
        var footerHint = DiabloUIFactory.MakeText(footerGo.transform, "FooterHint", Loc.T("inv.equiphint"), 12, DiabloPalette.ParchmentDim,
            TextAnchor.MiddleRight, shadow: false, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(footerHint.gameObject, prefH: 28f, flexW: 2f);

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
        // Hero (left), bag (middle), fixed tooltip column (right); the live stat
        // breakdown lives on the Stats tab (StatsContent), not here (redesign).
        var charCol = MakeColumn(inventoryPage.transform, "CharacterColumn", 456f, 0f);
        var bagCol = MakeColumn(inventoryPage.transform, "BagColumn", -1f, 1f);
        var tipCol = MakeColumn(inventoryPage.transform, "TooltipColumn", 360f, 0f);
        BuildCharacterColumn(charCol.transform);
        BuildBagColumn(bagCol.transform);
        BuildTooltipColumn(tipCol.transform);
    }

    static Transform MakeParamScroll(Transform parent, string scrollName, string contentName, float height, float width, float flexW)
    {
        var scrollGo = new GameObject(scrollName, typeof(RectTransform));
        scrollGo.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(scrollGo, prefH: height, prefW: width, flexW: flexW);
        var scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.16f);
        scrollBg.raycastTarget = true;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewportGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(4f, 4f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        viewportGo.AddComponent<RectMask2D>();
        var vpImg = viewportGo.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.01f);
        vpImg.raycastTarget = true;
        scroll.viewport = vpRt;

        var contentGo = new GameObject(contentName, typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var cRt = contentGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;

        var cv = contentGo.AddComponent<VerticalLayoutGroup>();
        cv.spacing = 2f;
        cv.padding = new RectOffset(2, 2, 2, 4);
        cv.childControlWidth = true;
        cv.childControlHeight = false;
        cv.childForceExpandWidth = true;
        cv.childForceExpandHeight = false;

        var fit = contentGo.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = cRt;
        return contentGo.transform;
    }

    void RebuildParamRows(Transform content, CharacterStatBreakdown.CharacterStatSnapshot snapshot)
    {
        if (content == null || snapshot == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var go = content.GetChild(i).gameObject;
            go.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        CharacterStatBreakdown.StatCategory? lastCat = null;
        for (int i = 0; i < snapshot.entries.Count; i++)
        {
            var entry = snapshot.entries[i];
            if (!lastCat.HasValue || lastCat.Value != entry.category)
            {
                lastCat = entry.category;
                MakeParamCategoryHeader(content, entry.category);
            }
            MakeParamRow(content, entry);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }

    static void MakeParamCategoryHeader(Transform parent, CharacterStatBreakdown.StatCategory cat)
    {
        var go = new GameObject("ParamCat_" + cat, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefH: 22f, flexW: 1f);
        var lbl = DiabloUIFactory.MakeText(go.transform, "Label", CharacterStatBreakdown.CategoryTitle(cat), 13, DiabloPalette.TrimGold, TextAnchor.MiddleLeft);
        lbl.fontStyle = FontStyle.Bold;
        var rt = lbl.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4f, 2f);
        rt.offsetMax = new Vector2(-4f, 0f);
    }

    void MakeParamRow(Transform parent, CharacterStatBreakdown.StatEntry entry)
    {
        var rowGo = new GameObject("ParamRow_" + entry.id, typeof(RectTransform));
        rowGo.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(rowGo, prefH: 24f, flexW: 1f);

        var bg = rowGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.10f, 0.08f, 0.38f);
        bg.raycastTarget = true;

        var hz = rowGo.AddComponent<HorizontalLayoutGroup>();
        hz.padding = new RectOffset(6, 6, 2, 2);
        hz.spacing = 6f;
        hz.childAlignment = TextAnchor.MiddleLeft;
        hz.childControlWidth = true;
        hz.childControlHeight = true;
        hz.childForceExpandWidth = false;

        var nameLbl = DiabloUIFactory.MakeText(rowGo.transform, "Name", entry.name, 13, DiabloPalette.Parchment, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(nameLbl.gameObject, prefH: 20f, flexW: 1f);

        var valLbl = DiabloUIFactory.MakeText(rowGo.transform, "Value", entry.displayValue, 13, DiabloPalette.GoldBright, TextAnchor.MiddleRight);
        valLbl.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(valLbl.gameObject, prefH: 20f, prefW: 150f);

        var captured = entry;
        var hover = rowGo.AddComponent<HoverInfoSource>();
        hover.Bind(gameObject, () => (captured.name, captured.formula, captured.RenderTooltipBody(), captured.capNote), bg);
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
        // Paperdoll: the live 3D hero with Helmet/Armor/Belt left and
        // Gloves/Weapon/Boots right (gear-slots S1). The header row carries
        // the hero line now; the stat breakdown lives on the Stats tab.
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
        DiabloUIFactory.SetLayout(gridGo, prefH: 440f, flexW: 1f);
        // Transparent backdrop: a worn item dropped anywhere on the grid goes back into the bag.
        var gridBg = gridGo.AddComponent<Image>();
        gridBg.color = new Color(0f, 0f, 0f, 0.18f);
        var gridCell = gridGo.AddComponent<InventoryCell>();
        gridCell.kind = InventoryCell.Kind.Bag;
        gridCell.index = -1;
        gridCell.owner = this;
        var grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.cellSize = new Vector2(72f, 80f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(4, 4, 4, 4);
        grid.childAlignment = TextAnchor.UpperCenter;
        bagList = gridGo.transform;

        noticeText = DiabloUIFactory.MakeText(col, "Notice", "", 14, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(noticeText.gameObject, prefH: 22f, flexW: 1f);
    }

    /// <summary>
    /// Display-only locked cell: the bag holds <see cref="Inventory.Capacity"/> items
    /// (GOAL cinderreach-inventory-redesign, no gameplay change); the Artboard grid is
    /// 8x5, so remaining frames render as locked, non-interactive "BagLocked" frames.
    /// They are siblings after the usable cells, never counted as cells (tests assert
    /// BagButton*/BagEmpty == Capacity; BagLocked is intentionally distinct).
    /// </summary>
    static GameObject MakeLockedCell()
    {
        var go = new GameObject("BagLocked", typeof(RectTransform));
        var frame = go.AddComponent<Image>();
        frame.sprite = DiabloMenuArt.SlotBackground(null);
        frame.type = Image.Type.Sliced;
        frame.color = new Color(1f, 1f, 1f, 0.30f);
        frame.raycastTarget = false;
        return go;
    }

    /// <summary>Fixed tooltip column (redesign): selection detail, comparison, actions.</summary>
    void BuildTooltipColumn(Transform col)
    {
        var card = HudChrome.Panel(col, "TooltipCard", new Vector2(360f, 706f), goldTop: true);
        DiabloUIFactory.SetLayout(card.gameObject, prefH: 706f, flexW: 1f);
        var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(12, 12, 10, 10);
        v.spacing = 6f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        tcTitleText = DiabloUIFactory.MakeText(card.transform, "TooltipTitle", Loc.T("inv.tc.title"), 18,
            DiabloPalette.TrimGold, TextAnchor.MiddleLeft, font: DiabloUIFactory.DisplayFont);
        tcTitleText.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(tcTitleText.gameObject, prefH: 28f, flexW: 1f);
        tcMetaText = DiabloUIFactory.MakeText(card.transform, "TooltipMeta", "", 13, DiabloPalette.ParchmentDim,
            TextAnchor.MiddleLeft, shadow: false, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(tcMetaText.gameObject, prefH: 20f, flexW: 1f);
        var scroll = MakeParamScroll(card.transform, "TooltipScroll", "TooltipContent", 396f, -1f, 1f);
        tcBodyText = DiabloUIFactory.MakeText(scroll, "TooltipBody", Loc.T("inv.baghint"), 13, DiabloPalette.Parchment,
            TextAnchor.UpperLeft, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(tcBodyText.gameObject, prefH: 120f, flexW: 1f);
        tcCompareText = DiabloUIFactory.MakeText(card.transform, "TooltipCompare", "", 13, DiabloPalette.Parchment,
            TextAnchor.UpperLeft, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(tcCompareText.gameObject, prefH: 120f, flexW: 1f);

        var actionRow = DiabloUIFactory.MakeHorizontalRow(card.transform, "TooltipActions", 8f, 44f);
        tcEquipButton = DiabloUIFactory.MakeSmallButton(actionRow.transform, Loc.T("inv.tc.equip"), () =>
        {
            int i = SelectedOrLast();
            if (i >= 0) EquipIndex(i);
        }, 44f);
        tcEquipButton.name = "TooltipEquipButton";
        tcSellButton = DiabloUIFactory.MakeSmallButton(actionRow.transform, Loc.T("inv.tc.sell"), null, 44f);
        tcSellButton.name = "TooltipSellButton";
        tcDiscardButton = DiabloUIFactory.MakeSmallButton(actionRow.transform, Loc.T("inv.tc.discard"), () =>
        {
            int i = SelectedOrLast();
            if (i >= 0) DiscardIndex(i);
        }, 44f);
        tcDiscardButton.name = "TooltipDiscardButton";

        var legend = DiabloUIFactory.MakeText(card.transform, "TooltipLegend", Loc.T("inv.tc.legend"), 12,
            DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter, shadow: false, font: DiabloUIFactory.BodyFont);
        DiabloUIFactory.SetLayout(legend.gameObject, prefH: 20f, flexW: 1f);
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
        var sh = box.AddComponent<HorizontalLayoutGroup>();
        sh.padding = new RectOffset(16, 16, 12, 12);
        sh.spacing = 18f;
        sh.childControlWidth = true;
        sh.childControlHeight = true;
        sh.childForceExpandWidth = false;

        statsParamContent = MakeParamScroll(box.transform, "StatsScroll", "StatsContent", 496f, 680f, 0f);

        statsDetailText = DiabloUIFactory.MakeText(box.transform, "StatsDetail", "", 13, DiabloPalette.Parchment, TextAnchor.UpperLeft);
        DiabloUIFactory.FitToBox(statsDetailText, 14, 10);
        DiabloUIFactory.SetLayout(statsDetailText.gameObject, prefH: 496f, flexW: 1f);

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

    /// <summary>
    /// One bag cell: "BagButton{i}" for items, "BagEmpty" for empty usable cells
    /// (both counted against <see cref="Inventory.Capacity"/>). The remaining 8x5
    /// frames are display-only "BagLocked" (see <see cref="MakeLockedCell"/>).
    /// </summary>
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
            else
            {
                // Upgrade marker: net-better stats than the worn item in that slot.
                int score = UpgradeMarkScore(item, equipment);
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
}
