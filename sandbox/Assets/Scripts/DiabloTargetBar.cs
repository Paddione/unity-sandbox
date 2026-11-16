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
        canvasGo.AddComponent<CanvasScaler>();

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panel = panelGo.AddComponent<Image>();
        panel.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        panel.raycastTarget = false;
        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(300f, 44f);
        rect.anchoredPosition = new Vector2(0f, -12f);

        nameText = MakeLabel(panelGo.transform, "", 15);
        nameText.alignment = TextAnchor.MiddleCenter;
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(panelGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.05f, 0.05f, 1f);
        bg.raycastTarget = false;
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.sizeDelta = new Vector2(280f, 12f);
        bgRect.anchoredPosition = new Vector2(0f, 6f);
        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.8f, 0.15f, 0.15f, 1f);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
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
