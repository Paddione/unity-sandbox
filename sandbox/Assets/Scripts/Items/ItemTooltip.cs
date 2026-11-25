using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Floating item tooltip shared by inventory, stash, vendor and smith. It
/// follows the pointer on its own overlay canvas (no raycaster, so it never
/// steals hover). Holding Shift over an equippable item adds the card of the
/// item worn in the same slot and switches the hovered card to compare lines:
/// green ▲ for gains, red ▼ for losses (see <see cref="ItemCompare"/>).
/// One instance lives on the hero (<see cref="For"/>).
/// </summary>
public sealed class ItemTooltip : MonoBehaviour
{
    public const string CanvasName = "ItemTooltipCanvas";
    const float CardWidth = 320f;
    const float PointerOffset = 22f;

    /// <summary>Test seam: forces the Shift state (null reads the keyboard).</summary>
    public static bool? ShiftOverride;

    /// <summary>Automation seam (shotcheck): anchor at this screen point instead of the pointer.</summary>
    public Vector2? PinnedScreen;

    Canvas canvas;
    RectTransform canvasRect;
    RectTransform root;
    GameObject equippedCard;
    Text equippedTitle;
    Text equippedBody;
    Text hoverTag;
    Text hoverTitle;
    Text hoverMeta;
    Text hoverBody;
    Text hoverHint;

    ItemInstance item;
    Equipment equipment;
    GameObject source;
    bool tracksSource;
    string tradeTag;
    bool rendered;

    public bool IsVisible => root != null && root.gameObject.activeSelf;
    public bool IsComparing { get; private set; }
    public ItemInstance Item => item;

    /// <summary>Hovered card body (test seam).</summary>
    public string HoverText => hoverBody != null ? hoverBody.text : "";

    /// <summary>Equipped card body while comparing (test seam).</summary>
    public string EquippedText => equippedBody != null && equippedCard.activeSelf ? equippedBody.text : "";

    public static bool ShiftHeld()
    {
        if (ShiftOverride.HasValue) return ShiftOverride.Value;
        var kb = Keyboard.current;
        return kb != null && kb.shiftKey.isPressed;
    }

    /// <summary>The hero's tooltip (created on first use).</summary>
    public static ItemTooltip For(GameObject hero)
    {
        if (hero == null) return null;
        var tip = hero.GetComponent<ItemTooltip>();
        return tip != null ? tip : hero.AddComponent<ItemTooltip>();
    }

    /// <summary>Hide the hero's tooltip if it exists (menus call this on close).</summary>
    public static void HideFor(GameObject hero)
    {
        var tip = hero != null ? hero.GetComponent<ItemTooltip>() : null;
        if (tip != null) tip.Hide();
    }

    /// <summary>Wire pointer enter/exit on a row so it shows this hero's tooltip.</summary>
    public static void AttachHover(GameObject go, GameObject hero, System.Func<ItemInstance> get, string tag = null)
    {
        if (go == null || hero == null || EventSystem.current == null) return;
        var trig = go.GetComponent<EventTrigger>();
        if (trig == null) trig = go.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            var tip = For(hero);
            if (tip != null) tip.Show(get(), hero.GetComponent<Equipment>(), tag, go);
        });
        trig.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideFor(hero));
        trig.triggers.Add(exit);
    }

    /// <param name="from">Row or cell under the pointer; the tooltip closes when it is destroyed or hidden (menu rebuilds send no pointer exit).</param>
    public void Show(ItemInstance shown, Equipment worn, string tag = null, GameObject from = null)
    {
        if (shown == null) { Hide(); return; }
        EnsureBuilt();
        item = shown;
        source = from;
        tracksSource = from != null;
        equipment = worn;
        tradeTag = tag;
        rendered = false;
        root.gameObject.SetActive(true);
        Render();
        FollowPointer();
    }

    public void Hide()
    {
        item = null;
        source = null;
        tracksSource = false;
        IsComparing = false;
        if (root != null) root.gameObject.SetActive(false);
    }

    /// <summary>Re-render now (tests call this after changing <see cref="ShiftOverride"/>).</summary>
    public void Render()
    {
        if (item == null || root == null) return;
        bool equippable = item.slot != ItemSlot.Consumable;
        var worn = equippable && equipment != null ? equipment.GetEquipped(item.slot) : null;
        bool compare = equippable && ShiftHeld() && !ReferenceEquals(worn, item);
        if (rendered && compare == IsComparing) return;
        rendered = true;
        IsComparing = compare;

        hoverTag.gameObject.SetActive(!string.IsNullOrEmpty(tradeTag));
        hoverTag.text = tradeTag ?? "";
        hoverTitle.text = LootDrop.ItemLabel(item);
        hoverTitle.color = DiabloPalette.Rarity(item.rarity);
        hoverMeta.text = MetaLine(item);
        hoverBody.text = compare ? CompareBody(item, worn) : PlainBody(item);
        hoverHint.gameObject.SetActive(equippable && !compare && !ReferenceEquals(worn, item));
        hoverHint.text = Loc.T("inv.tip.shifthint");

        equippedCard.SetActive(compare);
        if (compare)
        {
            if (worn != null)
            {
                equippedTitle.text = LootDrop.ItemLabel(worn);
                equippedTitle.color = DiabloPalette.Rarity(worn.rarity);
                equippedBody.text = MetaLine(worn) + "\n" + PlainBody(worn);
            }
            else
            {
                equippedTitle.text = ItemText.Slot(item.slot);
                equippedTitle.color = DiabloPalette.ParchmentDim;
                equippedBody.text = Loc.T("inv.tip.emptyslot");
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    static string MetaLine(ItemInstance it)
    {
        string level = it.itemLevel <= 0 ? Loc.T("item.card.ilvl_legacy") : Loc.F("item.card.ilvl", it.itemLevel);
        return Loc.F("inv.tip.meta", DiabloPalette.ToHex(DiabloPalette.ParchmentDim), ItemText.Rarity(it.rarity), ItemText.Slot(it.slot), level);
    }

    static string PlainBody(ItemInstance it)
    {
        var sb = new System.Text.StringBuilder();
        AppendClassLock(sb, it);
        if (it.affixes == null || it.affixes.Count == 0) sb.AppendLine(Loc.T("item.card.norolls"));
        else
            foreach (var a in it.affixes)
                sb.AppendLine(AffixLibrary.Describe(a));
        AppendUnique(sb, it);
        return sb.ToString().TrimEnd();
    }

    static string CompareBody(ItemInstance it, ItemInstance worn)
    {
        var sb = new System.Text.StringBuilder();
        AppendClassLock(sb, it);
        sb.AppendLine(ItemCompare.ArrowLines(it, worn));
        AppendUnique(sb, it);
        return sb.ToString().TrimEnd();
    }

    static void AppendClassLock(System.Text.StringBuilder sb, ItemInstance it)
    {
        if (Equipment.CanEquip(it, RunProfile.ClassId)) return;
        string who = ClassCatalog.Get(ItemDatabase.ClassRestriction(it.baseType)).displayName;
        sb.AppendLine(Loc.F("inv.tip.colored", DiabloInventoryUI.WorseColor, Loc.F("item.card.classlock", who)));
    }

    static void AppendUnique(System.Text.StringBuilder sb, ItemInstance it)
    {
        string effect = BuildUniqueEffects.EffectTextFor(it);
        if (!string.IsNullOrEmpty(effect))
            sb.AppendLine(Loc.F("inv.tip.colored", DiabloPalette.ToHex(DiabloPalette.GoldBright), effect));
    }

    void Update()
    {
        if (!IsVisible) return;
        if (item == null || (tracksSource && (source == null || !source.activeInHierarchy))) { Hide(); return; }
        Render();
        FollowPointer();
    }

    void OnDisable() => Hide();

    void FollowPointer()
    {
        var mouse = Mouse.current;
        if (canvasRect == null || (mouse == null && !PinnedScreen.HasValue)) return;
        Vector2 screen = PinnedScreen ?? mouse.position.ReadValue();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local)) return;
        Vector2 size = root.rect.size;
        Vector2 half = canvasRect.rect.size * 0.5f;
        // Right of and below the pointer; flip when the card would leave the screen.
        float x = local.x + PointerOffset;
        if (x + size.x > half.x) x = local.x - PointerOffset - size.x;
        float y = local.y - PointerOffset;
        if (y - size.y < -half.y) y = -half.y + size.y;
        x = Mathf.Clamp(x, -half.x, Mathf.Max(-half.x, half.x - size.x));
        y = Mathf.Clamp(y, Mathf.Min(half.y, -half.y + size.y), half.y);
        root.anchoredPosition = new Vector2(x, y);
    }

    void EnsureBuilt()
    {
        if (root != null) return;
        var canvasGo = new GameObject(CanvasName);
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 60, raycaster: false);
        canvasRect = canvasGo.GetComponent<RectTransform>();

        var rootGo = new GameObject("TooltipRoot", typeof(RectTransform));
        rootGo.transform.SetParent(canvasGo.transform, false);
        root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 1f);
        var row = rootGo.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 8f;
        row.childAlignment = TextAnchor.UpperLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        var fit = rootGo.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        equippedCard = MakeCard(root, "EquippedCard", DiabloPalette.ParchmentDim);
        var eqTag = CardText(equippedCard.transform, "Tag", 12, DiabloPalette.TrimGold, FontStyle.Bold);
        eqTag.text = Loc.T("inv.tip.equipped");
        equippedTitle = CardText(equippedCard.transform, "Title", 17, DiabloPalette.Parchment, FontStyle.Bold);
        equippedBody = CardText(equippedCard.transform, "Body", 14, DiabloPalette.Parchment, FontStyle.Normal);

        var hoverCard = MakeCard(root, "HoverCard", DiabloPalette.TrimGold);
        hoverTag = CardText(hoverCard.transform, "Tag", 12, DiabloPalette.TrimGold, FontStyle.Bold);
        hoverTitle = CardText(hoverCard.transform, "Title", 18, DiabloPalette.Parchment, FontStyle.Bold);
        hoverMeta = CardText(hoverCard.transform, "Meta", 13, DiabloPalette.ParchmentDim, FontStyle.Normal);
        hoverBody = CardText(hoverCard.transform, "Body", 14, DiabloPalette.Parchment, FontStyle.Normal);
        hoverHint = CardText(hoverCard.transform, "Hint", 12, DiabloPalette.ParchmentDim, FontStyle.Italic);
        rootGo.SetActive(false);
    }

    static GameObject MakeCard(Transform parent, string name, Color edge)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var bg = go.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.TooltipBackground();
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(edge.r, edge.g, edge.b, 0.55f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 10, 12);
        v.spacing = 4f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        DiabloUIFactory.SetLayout(go, prefW: CardWidth);
        return go;
    }

    static Text CardText(Transform parent, string name, int size, Color color, FontStyle style)
    {
        var t = DiabloUIFactory.MakeText(parent, name, "", size, color, TextAnchor.UpperLeft);
        t.fontStyle = style;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
