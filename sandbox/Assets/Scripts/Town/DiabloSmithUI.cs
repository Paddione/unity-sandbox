using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lantern-Hold-Schmiedefenster (UGUI, zur Laufzeit gebaut wie
/// <see cref="DiabloVendorUI"/> — kein Prefab-Churn, HD-Skalierung 1920×1080).
/// Listet die Tasche mit Materialertrag, zeigt je Auswahl Preis, bisherige
/// Eigenschaft, möglichen Ersatz und Rollspanne; nach Erfolg den tatsächlichen
/// Rollwert plus Kontostände. `F` toggelt, `Esc`/Button schließt (saves).
/// Stash-Transfers und Aus-/Anlegen ändern nichts am Verfahren: Die Station
/// arbeitet immer auf Taschen-Referenzen, der bearbeitete Slot bleibt erhalten.
/// Public contract für Tests: IsOpen, SetOpen, SalvageIndex, ReforgeSelected,
/// SelectBag, SelectAffix, SelectReplacement, Refresh, LastMessage.
/// </summary>
public sealed class DiabloSmithUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    /// <summary>Letzte Rückmeldung (Erfolg mit Rollwert / Ablehnungsgrund).</summary>
    public string LastMessage { get; private set; } = "";

    Canvas canvas;
    Text purseText;
    Text detailText;
    Text messageText;
    Transform bagList;
    Transform affixList;
    Transform replacementList;
    bool building;

    Inventory inventory;
    Wallet wallet;
    int selectedBag = -1;
    int selectedAffix = -1;
    string selectedReplacement;

    void Awake()
    {
        EnsureBuilt();
        SetOpen(false);
    }

    void EnsureBuilt()
    {
        if (canvas != null) return;
        wallet = GetComponent<Wallet>();
        inventory = GetComponent<Inventory>();
        BuildUI();
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.fKey.wasPressedThisFrame) SetOpen(!IsOpen);
        if (kb.escapeKey.wasPressedThisFrame && IsOpen) SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (IsOpen == open) return;
        if (open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        if (!open) ItemTooltip.HideFor(gameObject);
        DiabloClickController.ExternalDrive = open;
        if (open) Refresh();
        else SaveService.SaveNow(gameObject);
    }

    public void SelectBag(int index)
    {
        EnsureBuilt();
        selectedBag = index;
        selectedAffix = -1;
        selectedReplacement = null;
        Refresh();
    }

    public void SelectAffix(int index)
    {
        EnsureBuilt();
        selectedAffix = index;
        selectedReplacement = null;
        Refresh();
    }

    public void SelectReplacement(string key)
    {
        EnsureBuilt();
        selectedReplacement = key;
        Refresh();
    }

    /// <summary>Verwertet Tasche[index] (Button-Hook).</summary>
    public void SalvageIndex(int index)
    {
        EnsureBuilt();
        var item = inventory != null ? inventory.Get(index) : null;
        if (item == null) { Say(Loc.T("smith.msg.noitem")); return; }
        int before = inventory.Count;
        if (SmithingService.TrySalvage(gameObject, index, item))
        {
            Say(Loc.F("smith.msg.salvaged", LootDrop.ItemLabel(item)));
            if (selectedBag >= before - 1) selectedBag = -1;
            DiabloAudio.PlayLoot();
        }
        else Say(Loc.T("smith.msg.salvagerejected"));
        Refresh();
    }

    /// <summary>Schmiedet die Auswahl um (Button-Hook).</summary>
    public void ReforgeSelected()
    {
        EnsureBuilt();
        var item = inventory != null ? inventory.Get(selectedBag) : null;
        if (item == null || selectedAffix < 0 || string.IsNullOrEmpty(selectedReplacement))
        { Say(Loc.T("smith.msg.pickall")); return; }
        if (SmithingService.TryReforge(gameObject, selectedBag, item, selectedAffix, selectedReplacement))
        {
            float v = item.affixes[selectedAffix].value;
            Say(Loc.F("smith.msg.reforged", AffixLibrary.Describe(new Affix(selectedReplacement, v)), item.affixTier));
            DiabloAudio.PlayLoot();
        }
        else Say(Loc.T("smith.msg.reforgerejected"));
        Refresh();
    }

    /// <summary>Gold/Material und alle Listen aus dem State neu aufbauen.</summary>
    public void Refresh()
    {
        EnsureBuilt();
        SmithingService.ReforgeCost(out int mat, out int gold);
        if (purseText != null)
            purseText.text = wallet != null
                ? Loc.F("smith.purse", wallet.Gold, RunProfile.SmithingMaterial, mat, gold)
                : Loc.T("smith.purse.none");
        RebuildBag();
        RebuildAffixes();
        RebuildReplacements();
        if (messageText != null) messageText.text = LastMessage;
    }

    void Say(string msg) { LastMessage = msg; }

    void RebuildBag()
    {
        if (bagList == null) return;
        foreach (Transform child in bagList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        if (inventory == null) return;
        for (int i = 0; i < inventory.Count; i++)
        {
            int index = i;
            var item = inventory.Get(i);
            int yield = SmithingService.SalvageYield(item);
            string mark = index == selectedBag ? "» " : "";
            var btn = MakeRowButton(bagList,
                mark + Loc.F("smith.bag.row", LootDrop.ItemLabel(item), ItemText.Rarity(item.rarity), item.affixTier) +
                (yield > 0 ? Loc.F("smith.bag.yield", yield) : Loc.T("smith.bag.noyield")),
                DiabloPalette.Rarity(item.rarity));
            btn.onClick.AddListener(() => SelectBag(index));
            ItemTooltip.AttachHover(btn.gameObject, gameObject, () => item);
        }
    }

    void RebuildAffixes()
    {
        if (affixList == null) return;
        foreach (Transform child in affixList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        var item = inventory != null ? inventory.Get(selectedBag) : null;
        if (detailText != null)
            detailText.text = item == null ? Loc.T("smith.msg.pickitem") :
                item.reforgeIndexPlusOne != 0
                    ? Loc.F("smith.detail.bound", LootDrop.ItemLabel(item), item.reforgeIndexPlusOne)
                    : Loc.F("smith.detail.pickaffix", LootDrop.ItemLabel(item));
        if (item == null || item.affixes == null) return;
        for (int i = 0; i < item.affixes.Count; i++)
        {
            int index = i;
            var a = item.affixes[i];
            string mark = index == selectedAffix ? "» " : "";
            var btn = MakeRowButton(affixList, $"{mark}{AffixLibrary.Describe(a)}", DiabloPalette.Parchment);
            btn.onClick.AddListener(() => SelectAffix(index));
        }
    }

    void RebuildReplacements()
    {
        if (replacementList == null) return;
        foreach (Transform child in replacementList)
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        var item = inventory != null ? inventory.Get(selectedBag) : null;
        if (item == null || selectedAffix < 0) return;
        foreach (var key in SmithingService.ReplacementCandidates(item))
        {
            var def = AffixLibrary.Get(key);
            def.Range(item.affixTier, out float min, out float max);
            string mark = key == selectedReplacement ? "» " : "";
            string label = mark + Loc.F("smith.replacement.row", AffixLibrary.StatName(key), min, max, item.affixTier);
            var btn = MakeRowButton(replacementList, label, DiabloPalette.Parchment);
            btn.onClick.AddListener(() => SelectReplacement(key));
        }
        var go = MakeRowButton(replacementList, Loc.T("smith.btn.reforge"), DiabloPalette.Parchment);
        go.onClick.AddListener(ReforgeSelected);
    }

    // ---------- build (HD-Skalierung wie VendorUI) ----------

    void BuildUI()
    {
        building = true;
        var canvasGo = new GameObject("SmithCanvas");
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
        headerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
        MakeBannerLabel(headerGo.transform, Loc.T("smith.title"), 20);
        purseText = MakeLabel(panelGo.transform, Loc.T("smith.purse.none"), 17);
        detailText = MakeLabel(panelGo.transform, "", 13);
        messageText = MakeLabel(panelGo.transform, "", 13);

        var colsGo = new GameObject("Columns");
        colsGo.transform.SetParent(panelGo.transform, false);
        var cols = colsGo.AddComponent<HorizontalLayoutGroup>();
        cols.spacing = 10f;
        cols.childControlWidth = true;
        cols.childControlHeight = true;

        bagList = MakeColumn(colsGo.transform, Loc.T("smith.col.bag"));
        affixList = MakeColumn(colsGo.transform, Loc.T("smith.col.affix"));
        replacementList = MakeColumn(colsGo.transform, Loc.T("smith.col.replacement"));

        MakeLabel(panelGo.transform, Loc.T("smith.footer"), 13);
        var closeBtn = MakeSmallButton(panelGo.transform, Loc.T("town.close"));
        closeBtn.onClick.AddListener(() => SetOpen(false));
        building = false;
    }

    Transform MakeColumn(Transform parent, string title)
    {
        var col = new GameObject(title);
        col.transform.SetParent(parent, false);
        MakeLabel(col.transform, title, 14);
        var rows = new GameObject("Rows");
        rows.transform.SetParent(col.transform, false);
        return rows.transform;
    }

    static Text MakeLabel(Transform parent, string text, int size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = Color.white;
        return t;
    }

    static Text MakeBannerLabel(Transform parent, string text, int size)
    {
        var t = MakeLabel(parent, text, size);
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    static Button MakeRowButton(Transform parent, string text, Color color)
    {
        var go = new GameObject("Row");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        var label = MakeBannerLabel(go.transform, text, 13);
        label.color = Color.black;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 26f);
        return btn;
    }

    static Button MakeSmallButton(Transform parent, string text)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = DiabloPalette.Parchment;
        var btn = go.AddComponent<Button>();
        var label = MakeBannerLabel(go.transform, text, 14);
        label.color = Color.black;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 30f);
        return btn;
    }
}
