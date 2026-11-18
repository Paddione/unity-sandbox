using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Session-complete overlay (S5): the Hollow Abbot's death opens a panel
/// with play time, kills, gold and the best drop, plus "Weiter (Town)"
/// (teleport back to Lantern Hold) and "Menü" buttons. Built at runtime
/// like <c>DiabloVendorUI</c>, same HD scaler convention (1920×1080,
/// MatchWidthOrHeight 0.5).
/// </summary>
public sealed class SessionComplete : MonoBehaviour
{
    /// <summary>Times the screen opened this run (test seam).</summary>
    public static int Shown;
    /// <summary>Last shown summary lines (test seam).</summary>
    public static string LastSummary = "";
    /// <summary>The boss kill of this run unlocked Kindled (replayability S4).</summary>
    public static bool KindledUnlockedThisRun;
    public const string KindledLine = "KINDLED FREIGESCHALTET: Gegner härter, Beute besser (Einstellungen → Schwierigkeit).";
    /// <summary>Best drop line of the session (set by boss loot, tests).</summary>
    public static string BestDrop = "";

    static float sessionStart;
    static bool startStamped;

    Canvas canvas;
    Text body;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StampStart()
    {
        if (startStamped) return;
        startStamped = true;
        sessionStart = Time.realtimeSinceStartup;
    }

    /// <summary>Open the overlay for the hero (headless-callable).</summary>
    public static void Show(GameObject hero, string bestDrop)
    {
        if (!string.IsNullOrEmpty(bestDrop)) BestDrop = bestDrop;
        Shown++;
        var host = hero != null ? hero.GetComponent<SessionComplete>() : null;
        if (host == null && hero != null) host = hero.AddComponent<SessionComplete>();
        if (host == null)
        {
            var go = new GameObject("SessionComplete");
            host = go.AddComponent<SessionComplete>();
        }
        host.EnsureBuilt();
        host.Fill(hero);
        host.canvas.enabled = true;
        Debug.Log($"[Session] Complete shown (shown={Shown}).");
    }

    /// <summary>Play time in seconds (test seam).</summary>
    public static float PlaySeconds() => Mathf.Max(0f, Time.realtimeSinceStartup - sessionStart);

    void Fill(GameObject hero)
    {
        int kills = EnemyHealth.HusksKilled + BossController.BossesKilled;
        int gold = hero != null ? hero.GetComponent<Wallet>()?.Gold ?? 0 : 0;
        float secs = PlaySeconds();
        LastSummary = $"Zeit {secs:0}s  Kills {kills}  Gold {gold}  Beute {BestDrop}";
        if (KindledUnlockedThisRun) LastSummary += "\n" + KindledLine;
        if (body != null) body.text = "SESSION GESCHAFFT\n" + LastSummary;
    }

    void EnsureBuilt()
    {
        if (canvas != null) return;
        var root = new GameObject("SessionCanvas");
        root.transform.SetParent(transform, false);
        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("SessionPanel");
        panel.transform.SetParent(root.transform, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.03f, 0.04f, 0.92f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700f, 420f);
        rt.anchoredPosition = Vector2.zero;

        body = NewText(panel.transform, "SessionBody", 28, Color.white);

        var townBtn = NewButton(panel.transform, "TownButton", "Weiter (Town)");
        townBtn.onClick.AddListener(ReturnToTown);
        var menuBtn = NewButton(panel.transform, "MenuButton", "Menü");
        menuBtn.onClick.AddListener(BackToMenu);
        canvas.enabled = false;
    }

    Text NewText(Transform parent, string name, int size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.6f);
        rt.anchorMax = new Vector2(0.5f, 0.6f);
        rt.sizeDelta = new Vector2(640f, 200f);
        rt.anchoredPosition = Vector2.zero;
        return t;
    }

    Button NewButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.2f, 0.12f, 1f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.2f);
        rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.sizeDelta = new Vector2(260f, 60f);
        rt.anchoredPosition = new Vector2(name == "TownButton" ? -150f : 150f, 0f);
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var t = labelGo.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 24;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.text = label;
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        return btn;
    }

    void ReturnToTown()
    {
        var hero = gameObject.name == "Player" ? gameObject : GameObject.Find("Player");
        if (hero != null)
        {
            var cc = hero.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            hero.transform.position = new Vector3(0f, 0.05f, 11f);
            if (cc != null) cc.enabled = true;
        }
        if (canvas != null) canvas.enabled = false;
        Debug.Log("[Session] Returned to Lantern Hold.");
    }

    void BackToMenu()
    {
        if (canvas != null) canvas.enabled = false;
        Debug.Log("[Session] Back to menu (no menu scene yet; overlay closed).");
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Shown = 0;
        LastSummary = "";
        KindledUnlockedThisRun = false;
        BestDrop = "";
        sessionStart = Time.realtimeSinceStartup;
        startStamped = true;
    }
}
