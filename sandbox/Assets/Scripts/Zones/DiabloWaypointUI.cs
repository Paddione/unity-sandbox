using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Waypoint travel window (act-one S5), runtime UGUI with the HD scaler
/// convention: one button per destination from <see cref="WaypointShrine.Destinations"/>.
/// Movement is locked while open; Esc closes.
/// </summary>
public sealed class DiabloWaypointUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    Canvas canvas;
    Transform list;

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && IsOpen && kb.escapeKey.wasPressedThisFrame) SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        if (canvas == null) Build();
        IsOpen = open;
        canvas.enabled = open;
        DiabloClickController.ExternalDrive = open;
        if (open) Refresh();
    }

    /// <summary>Destination labels in the window (test seam).</summary>
    public string[] Labels()
    {
        var d = WaypointShrine.Destinations();
        var a = new string[d.Count];
        for (int i = 0; i < a.Length; i++) a[i] = d[i].displayName;
        return a;
    }

    public void Refresh()
    {
        if (list == null) return;
        foreach (Transform c in list)
            if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject);
        foreach (var s in WaypointShrine.Destinations())
        {
            string id = s.shrineId;
            var go = new GameObject("Waypoint_" + id, typeof(RectTransform));
            go.transform.SetParent(list, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);
            var img = go.AddComponent<Image>();
            img.sprite = DiabloMenuArt.TabBackground(false);
            img.type = Image.Type.Sliced;
            var b = go.AddComponent<Button>();
            b.onClick.AddListener(() =>
            {
                WaypointShrine.Travel(id, gameObject);
                SetOpen(false);
            });
            var label = MakeLabel(go.transform, s.displayName, 18);
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }

    void Build()
    {
        var canvasGo = new GameObject("WaypointCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasGo.transform, false);
        var img = panel.AddComponent<Image>();
        img.sprite = DiabloMenuArt.PanelBackground();
        img.type = Image.Type.Sliced;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 360f);
        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(16, 16, 14, 14);
        v.spacing = 8f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        var title = MakeLabel(panel.transform, "Waypoints", 22);
        title.color = DiabloPalette.TrimGold;
        var listGo = new GameObject("List", typeof(RectTransform));
        listGo.transform.SetParent(panel.transform, false);
        var lv = listGo.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 6f;
        lv.childControlWidth = true;
        lv.childControlHeight = false;
        list = listGo.transform;
        MakeLabel(panel.transform, "Esc closes.", 13);
    }

    static Text MakeLabel(Transform parent, string text, int size)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = DiabloPalette.Parchment;
        return t;
    }
}
