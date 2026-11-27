using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shared floating tooltip for character parameters and skills. Mirrors
/// <see cref="ItemTooltip"/>: own overlay canvas without GraphicRaycaster so it
/// never steals hover, and closes automatically when its source hides or dies.
/// </summary>
public sealed class InfoTooltip : MonoBehaviour
{
    public const string CanvasName = "InfoTooltipCanvas";
    const float CardWidth = 360f;
    const float PointerOffset = 22f;

    /// <summary>Automation seam (shotcheck): anchor at this screen point instead of the pointer.</summary>
    public Vector2? PinnedScreen;

    Canvas canvas;
    RectTransform canvasRect;
    RectTransform root;
    Text titleText;
    Text metaText;
    Text bodyText;
    Text hintText;

    GameObject source;
    bool tracksSource;

    public bool IsVisible => root != null && root.gameObject.activeSelf;
    public string TitleText => titleText != null ? titleText.text : "";
    public string MetaText => metaText != null ? metaText.text : "";
    public string BodyText => bodyText != null ? bodyText.text : "";
    public string HintText => hintText != null && hintText.gameObject.activeSelf ? hintText.text : "";

    public static InfoTooltip For(GameObject hero)
    {
        if (hero == null) return null;
        var tip = hero.GetComponent<InfoTooltip>();
        return tip != null ? tip : hero.AddComponent<InfoTooltip>();
    }

    public static void HideFor(GameObject hero)
    {
        var tip = hero != null ? hero.GetComponent<InfoTooltip>() : null;
        if (tip != null) tip.Hide();
    }

    public void Show(string title, string meta, string body, GameObject from = null, string hint = null)
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) { Hide(); return; }
        ItemTooltip.HideFor(gameObject);
        EnsureBuilt();
        source = from;
        tracksSource = from != null;

        titleText.text = title ?? "";
        metaText.gameObject.SetActive(!string.IsNullOrEmpty(meta));
        metaText.text = meta ?? "";
        bodyText.text = body ?? "";
        hintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        hintText.text = hint ?? "";

        root.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        FollowPointer();
    }

    public void Hide()
    {
        source = null;
        tracksSource = false;
        if (root != null) root.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsVisible) return;
        if (tracksSource && (source == null || !source.activeInHierarchy)) { Hide(); return; }
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
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 61, raycaster: false);
        canvasRect = canvasGo.GetComponent<RectTransform>();

        var rootGo = new GameObject("InfoTooltipRoot", typeof(RectTransform));
        rootGo.transform.SetParent(canvasGo.transform, false);
        root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 1f);

        var bg = rootGo.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.TooltipBackground();
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;
        var outline = rootGo.AddComponent<Outline>();
        var edge = DiabloPalette.TrimGold;
        outline.effectColor = new Color(edge.r, edge.g, edge.b, 0.55f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var v = rootGo.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 10, 12);
        v.spacing = 4f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var fit = rootGo.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        DiabloUIFactory.SetLayout(rootGo, prefW: CardWidth);

        titleText = CardText(rootGo.transform, "Title", 18, DiabloPalette.GoldBright, FontStyle.Bold);
        metaText = CardText(rootGo.transform, "Meta", 13, DiabloPalette.ParchmentDim, FontStyle.Normal);
        bodyText = CardText(rootGo.transform, "Body", 14, DiabloPalette.Parchment, FontStyle.Normal);
        hintText = CardText(rootGo.transform, "Hint", 12, DiabloPalette.ParchmentDim, FontStyle.Italic);
        rootGo.SetActive(false);
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
