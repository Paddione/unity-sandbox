using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D2-style target bar (top-center): name + HP of the last husk struck,
/// within range and inside the fade window. Display-only canvas (no
/// GraphicRaycaster, nothing blocks clicks). Solo-first: anchors to the
/// hero named "Player".
/// </summary>
public sealed class DiabloTargetBar : MonoBehaviour
{
    public const float MaxRange = 12f;
    public const float FadeSeconds = 3f;

    Canvas canvas;
    Text nameText;
    Image fill;

    void Awake()
    {
        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        var target = DiabloMelee.LastStruck;
        var hero = GameObject.Find("Player");
        if (target == null || hero == null)
        {
            SetVisible(false);
            return;
        }
        var health = target.GetComponent<EnemyHealth>();
        if (health == null)
        {
            SetVisible(false);
            return;
        }
        var d = target.transform.position - hero.transform.position;
        d.y = 0f;
        if (d.magnitude > MaxRange || Time.time - DiabloMelee.LastStruckTime > FadeSeconds)
        {
            SetVisible(false);
            return;
        }
        SetVisible(true);
        nameText.text = target.name;
        fill.fillAmount = health.HPFraction();
    }

    void SetVisible(bool visible)
    {
        if (canvas != null) canvas.enabled = visible;
    }

    /// <summary>Test seam (strikeCount precedent): shown fill, -1 when unbuilt.</summary>
    public float TestFillAmount => fill != null ? fill.fillAmount : -1f;

    void BuildUI()
    {
        var canvasGo = new GameObject("TargetBarCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var white = WhiteSprite();
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panel = panelGo.AddComponent<Image>();
        panel.sprite = DiabloMenuArt.TooltipBackground();
        panel.type = Image.Type.Sliced;
        panel.color = Color.white;
        panel.raycastTarget = false;
        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(360f, 54f);
        rect.anchoredPosition = new Vector2(0f, -12f);

        nameText = MakeLabel(panelGo.transform, "", 16);
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = DiabloPalette.Parchment;
        nameText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(panelGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.04f, 0.04f, 1f);
        bg.raycastTarget = false;
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.sizeDelta = new Vector2(332f, 14f);
        bgRect.anchoredPosition = new Vector2(0f, 8f);
        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        fill = fillGo.AddComponent<Image>();
        fill.sprite = DiabloMenuArt.ProgressFillSprite();
        fill.color = new Color(0.84f, 0.14f, 0.14f, 1f);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
    }

    static Sprite whiteSprite;

    /// <summary>Plain white sprite: FillMethod needs one or fillAmount no-ops.</summary>
    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    Text MakeLabel(Transform parent, string text, int size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.color = Color.white;
        label.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(8f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);
        return label;
    }
}
