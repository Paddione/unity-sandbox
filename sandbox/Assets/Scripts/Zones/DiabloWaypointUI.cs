using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Waypoint travel window (act-one S5), upgraded with WaypointSigil artwork,
/// non-overlapping rows, zone current-location markers, empty-state copy, and close button.
/// Movement is locked while open; Esc closes.
/// </summary>
public sealed class DiabloWaypointUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    Canvas canvas;
    Transform list;
    Text emptyHint;

    void Update()
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) return;
        var kb = Keyboard.current;
        if (kb != null && IsOpen && kb.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
        }
    }

    public void SetOpen(bool open)
    {
        if (canvas == null) Build();
        if (DiabloUIFactory.IsRemoteMirror(this)) open = false;
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (open) DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
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
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);

        var dests = WaypointShrine.Destinations();
        if (emptyHint != null) emptyHint.gameObject.SetActive(dests.Count == 0);
        foreach (var s in dests)
        {
            string id = s.shrineId;
            bool nearby = Vector3.Distance(transform.position, s.transform.position) < 8f;
            string caption = nearby ? $"{s.displayName}  (Here)" : s.displayName;
            BuildRow(id, caption, nearby ? RowState.Here : RowState.Travel);
        }
        // Unattuned shrines stay visible as locked rows: the window doubles as act progress.
        foreach (var s in WaypointShrine.Locked())
            BuildRow(s.shrineId, $"{s.displayName}  —  {LockedReason(s)}", RowState.Locked);
    }

    enum RowState { Travel, Here, Locked }

    /// <summary>Why a shrine is not a destination yet (zone sealed or shrine not reached).</summary>
    public static string LockedReason(WaypointShrine s) =>
        RunProfile.Act.IsUnlocked(s.zone) ? "not attuned" : "sealed";

    void BuildRow(string id, string caption, RowState state)
    {
        bool nearby = state == RowState.Here;
        {

            var go = new GameObject("Waypoint_" + id, typeof(RectTransform));
            go.transform.SetParent(list, false);
            DiabloUIFactory.SetLayout(go, prefH: 48f, flexW: 1f);
            var img = go.AddComponent<Image>();
            img.sprite = DiabloMenuArt.ButtonBackground(!nearby);
            img.type = Image.Type.Sliced;
            var b = go.AddComponent<Button>();
            b.interactable = state == RowState.Travel;
            if (state == RowState.Locked) img.color = new Color(0.55f, 0.52f, 0.5f, 0.75f);
            b.onClick.AddListener(() =>
            {
                DiabloAudio.PlayUIClick();
                WaypointShrine.Travel(id, gameObject);
                SetOpen(false);
            });

            var hz = go.AddComponent<HorizontalLayoutGroup>();
            hz.padding = new RectOffset(12, 12, 4, 4);
            hz.spacing = 10f;
            hz.childAlignment = TextAnchor.MiddleLeft;
            hz.childControlWidth = true;
            hz.childControlHeight = false;
            hz.childForceExpandWidth = false;

            var sigil = DiabloMenuArt.MenuArtSprite("WaypointSigil");
            if (sigil != null)
            {
                var iGo = new GameObject("Sigil", typeof(RectTransform));
                iGo.transform.SetParent(go.transform, false);
                DiabloUIFactory.SetLayout(iGo, prefH: 32f, prefW: 32f);
                var iImg = iGo.AddComponent<Image>();
                iImg.sprite = sigil;
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
            }

            var label = DiabloUIFactory.MakeText(go.transform, "DestLabel", caption, 17,
                state == RowState.Travel ? DiabloPalette.GoldBright
                : state == RowState.Locked ? DiabloPalette.DisabledText : DiabloPalette.ParchmentDim);
            DiabloUIFactory.SetLayout(label.gameObject, prefH: 36f, flexW: 1f);
        }
    }

    void Build()
    {
        var canvasGo = new GameObject("WaypointCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 12);

        var panel = DiabloUIFactory.MakePanel(canvasGo.transform, "Panel", new Vector2(480f, 440f), Vector2.zero);
        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 16, 16);
        v.spacing = 10f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        DiabloUIFactory.MakeHeader(panel.transform, "SANCTIFIED WAYPOINTS", DiabloMenuArt.MenuArtSprite("WaypointSigil"), () => SetOpen(false));

        var sub = DiabloUIFactory.MakeText(panel.transform, "WaypointSub", "Choose a discovered shrine to attune & travel:", 14, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(sub.gameObject, prefH: 22f, flexW: 1f);

        emptyHint = DiabloUIFactory.MakeText(panel.transform, "EmptyHint", "Explore the world to discover additional Waypoint Shrines.", 14, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(emptyHint.gameObject, prefH: 36f, flexW: 1f);
        emptyHint.gameObject.SetActive(false);

        var listGo = new GameObject("List", typeof(RectTransform));
        listGo.transform.SetParent(panel.transform, false);
        DiabloUIFactory.SetLayout(listGo, prefH: 256f, flexW: 1f);
        var lv = listGo.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 8f;
        lv.childControlWidth = true;
        lv.childControlHeight = false;
        list = listGo.transform;

        var hint = DiabloUIFactory.MakeText(panel.transform, "EscHint", "Click shrine to travel  •  Esc closes", 13, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(hint.gameObject, prefH: 22f, flexW: 1f);
    }
}
