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
}
