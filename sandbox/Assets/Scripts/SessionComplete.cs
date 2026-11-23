using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sanctuary Act / Session Completion overlay shown after defeating the Boss or Kilnmother.
/// Built cleanly with DiabloUIFactory dark-fantasy panels, GraphicRaycaster, English KPI totals,
/// Kindled unlock callout, and functional Return to Town & Main Menu actions.
/// </summary>
public sealed class SessionComplete : MonoBehaviour
{
    /// <summary>Times the screen opened this run (test seam).</summary>
    public static int Shown;
    /// <summary>Last shown summary lines (test seam).</summary>
    public static string LastSummary = "";
    /// <summary>The boss kill of this run unlocked Kindled (replayability S4).</summary>
    public static bool KindledUnlockedThisRun;
    public static string KindledLine => Loc.T("session.kindled");
    /// <summary>Best drop line of the session (set by boss loot, tests).</summary>
    public static string BestDrop = "";

    static float sessionStart;
    static bool startStamped;

    Canvas canvas;
    Text headerTitle;
    Text timeVal;
    Text killsVal;
    Text goldVal;
    Text dropVal;
    Text kindledBanner;
    Button pressOnBtn;
    Text subLine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StampStart()
    {
        if (startStamped) return;
        startStamped = true;
        sessionStart = Time.realtimeSinceStartup;
    }

    /// <summary>act-one S4: the overlay was opened by the Kilnmother's death.</summary>
    public static bool ActOneMode;
    public static string ActOneTitle => Loc.T("session.actone.title");

    /// <summary>act-two-opening S3: the overlay was opened by the Glasswright's death.</summary>
    public static bool ZoneBossMode;
    public static string ZoneBossTitle => Loc.T("session.barrens.title");

    /// <summary>Glasswright down: same overlay, Barrens title, Press On stays (Act II goes on).</summary>
    public static void ShowZoneBossCleared(GameObject hero)
    {
        ActOneMode = false;
        ZoneBossMode = true;
        Show(hero, string.IsNullOrEmpty(BestDrop) ? Loc.T("loot.cache.glass") : BestDrop);
    }

    /// <summary>Act I end screen (Kilnmother): same overlay, act title, all three zones in the totals.</summary>
    public static void ShowActComplete(GameObject hero)
    {
        ZoneBossMode = false;
        ActOneMode = true;
        Show(hero, Loc.T("loot.cache.kiln"));
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
        try { KindledFinaleReward.OfferOnSessionScreen(hero); }
        catch (System.Exception e) { Debug.LogWarning($"[Session] Kindled reward offer failed: {e.Message}"); }
        host.Fill(hero);
        host.canvas.enabled = true;
        DiabloClickController.ExternalDrive = true;
        Debug.Log($"[Session] Complete shown (shown={Shown}).");
    }

    /// <summary>Elapsed seconds this run.</summary>
    public static float PlaySeconds() => Mathf.Max(0f, Time.realtimeSinceStartup - sessionStart);

    void Fill(GameObject hero)
    {
        int kills = EnemyHealth.HusksKilled + BossController.BossesKilled;
        int gold = hero != null ? hero.GetComponent<Wallet>()?.Gold ?? 0 : 0;
        float secs = PlaySeconds();
        string dropText = string.IsNullOrEmpty(BestDrop) ? Loc.T("session.none") : BestDrop;
        LastSummary = Loc.F("session.summary", secs, kills, gold, dropText);
        if (KindledUnlockedThisRun) LastSummary += "\n" + KindledLine;
        if (!string.IsNullOrEmpty(KindledFinaleReward.LastOfferLine)) LastSummary += "\n" + KindledFinaleReward.LastOfferLine;

        if (headerTitle != null) headerTitle.text = ActOneMode ? ActOneTitle : ZoneBossMode ? ZoneBossTitle : Loc.T("session.abbot.title");
        if (pressOnBtn != null) pressOnBtn.gameObject.SetActive(!ActOneMode);
        if (subLine != null) subLine.text = ActOneMode ? Loc.T("session.actone.sub")
            : ZoneBossMode ? Loc.T("session.barrens.sub")
            : Loc.T("session.abbot.sub");
        if (timeVal != null) timeVal.text = Loc.F("session.secs", secs);
        if (killsVal != null) killsVal.text = kills.ToString();
        if (goldVal != null) goldVal.text = Loc.F("session.gold", gold);
        if (dropVal != null) dropVal.text = dropText;
        if (kindledBanner != null)
        {
            string offer = KindledFinaleReward.LastOfferLine;
            bool hasOffer = !string.IsNullOrEmpty(offer);
            kindledBanner.text = KindledUnlockedThisRun && hasOffer ? KindledLine + "\n" + offer
                : hasOffer ? offer : KindledLine;
            kindledBanner.gameObject.SetActive(KindledUnlockedThisRun || hasOffer);
        }
    }

    void EnsureBuilt()
    {
        if (canvas != null) return;
        var root = new GameObject("SessionCanvas");
        root.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(root, sortingOrder: 22);

        var scrim = new GameObject("Scrim", typeof(RectTransform));
        scrim.transform.SetParent(root.transform, false);
        var sImg = scrim.AddComponent<Image>();
        sImg.color = new Color(0.02f, 0.015f, 0.03f, 0.78f);
        var sr = scrim.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero;
        sr.anchorMax = Vector2.one;
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        var panel = DiabloUIFactory.MakePanel(root.transform, "SessionPanel", new Vector2(680f, 470f), Vector2.zero);
        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 20, 20);
        v.spacing = 12f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        var hdr = DiabloUIFactory.MakeHeader(panel.transform, ActOneTitle, DiabloMenuArt.MenuArtSprite("CrestCinderreach"));
        headerTitle = hdr.transform.Find("BannerLabel")?.GetComponent<Text>();

        var sub = DiabloUIFactory.MakeText(panel.transform, "SessionSub", "", 16, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        subLine = sub;
        DiabloUIFactory.SetLayout(sub.gameObject, prefH: 24f, flexW: 1f);
        DiabloUIFactory.MakeDivider(panel.transform);

        var gridGo = new GameObject("StatsGrid", typeof(RectTransform));
        gridGo.transform.SetParent(panel.transform, false);
        DiabloUIFactory.SetLayout(gridGo, prefH: 150f, flexW: 1f);
        var grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(304f, 68f);
        grid.spacing = new Vector2(12f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        timeVal = MakeStatBox(gridGo.transform, "Time Elapsed", Loc.T("session.box.time"), Loc.F("session.secs", 0), DiabloPalette.Parchment);
        killsVal = MakeStatBox(gridGo.transform, "Foes Slain (Kills)", Loc.T("session.box.kills"), "0", DiabloPalette.Blood);
        goldVal = MakeStatBox(gridGo.transform, "Gold Purse", Loc.T("session.box.gold"), Loc.F("session.gold", 0), DiabloPalette.GoldBright);
        dropVal = MakeStatBox(gridGo.transform, "Best Relic", Loc.T("session.box.best"), Loc.T("session.none"), DiabloPalette.Rarity(ItemRarity.Rare));

        kindledBanner = DiabloUIFactory.MakeText(panel.transform, "KindledBanner", KindledLine, 15, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(kindledBanner.gameObject, prefH: 38f, flexW: 1f);
        kindledBanner.gameObject.SetActive(false);

        DiabloUIFactory.MakeDivider(panel.transform);

        var btnRow = DiabloUIFactory.MakeHorizontalRow(panel.transform, "SessionButtons", 16f, 52f);
        // Mid-act boss (Abbot): the Kiln still waits, so staying on the spot is the natural next step.
        pressOnBtn = DiabloUIFactory.MakeButton(btnRow.transform, "PressOnButton", Loc.T("session.btn.presson"), 18, false, DiabloPalette.Parchment, PressOn);
        DiabloUIFactory.SetLayout(pressOnBtn.gameObject, prefH: 48f, flexW: 1f);
        var townBtn = DiabloUIFactory.MakeButton(btnRow.transform, "TownButton", Loc.T("session.btn.town"), 18, true, DiabloPalette.GoldBright, ReturnToTown);
        DiabloUIFactory.SetLayout(townBtn.gameObject, prefH: 48f, flexW: 1f);
        var menuBtn = DiabloUIFactory.MakeButton(btnRow.transform, "MenuButton", Loc.T("session.btn.menu"), 18, false, DiabloPalette.Parchment, BackToMenu);
        DiabloUIFactory.SetLayout(menuBtn.gameObject, prefH: 48f, flexW: 1f);

        canvas.enabled = false;
    }

    static Text MakeStatBox(Transform parent, string id, string label, string initial, Color valueCol)
    {
        var box = new GameObject("StatBox_" + id, typeof(RectTransform));
        box.transform.SetParent(parent, false);
        var img = box.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TooltipBackground();
        img.type = Image.Type.Sliced;
        var v = box.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(12, 12, 6, 6);
        v.spacing = 2f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        var lbl = DiabloUIFactory.MakeText(box.transform, "StatName", label, 13, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(lbl.gameObject, prefH: 18f, flexW: 1f);
        var val = DiabloUIFactory.MakeText(box.transform, "StatValue", initial, 20, valueCol);
        val.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(val.gameObject, prefH: 28f, flexW: 1f);
        return val;
    }

    /// <summary>Close the overlay and keep playing where the hero stands (mid-act only).</summary>
    public void PressOn()
    {
        DiabloAudio.PlayUIClick();
        if (canvas != null) canvas.enabled = false;
        DiabloClickController.ExternalDrive = false;
        Debug.Log("[Session] Pressing on.");
    }

    void ReturnToTown()
    {
        DiabloAudio.PlayUIClick();
        var hero = gameObject.name == "Player" ? gameObject : GameObject.Find("Player");
        if (hero != null)
        {
            // Arrive at the Lantern Hold waypoint like a waypoint trip (not inside the campfire),
            // then autosave so a quit right after the overlay keeps the town position.
            var shrine = WaypointShrine.Find("Town");
            ZoneTravel.TeleportHero(hero, shrine != null ? shrine.Arrival : new Vector3(0f, 0.05f, 11f));
            Physics.SyncTransforms();
            if (Diablo.IsLocalAuthority()) SaveService.SaveNow(hero, zoneReached: ActProgress.Town);
        }
        if (canvas != null) canvas.enabled = false;
        DiabloClickController.ExternalDrive = false;
        Debug.Log("[Session] Returned to Lantern Hold.");
    }

    void BackToMenu()
    {
        DiabloAudio.PlayUIClick();
        var hero = gameObject.name == "Player" ? gameObject : GameObject.Find("Player");
        if (hero != null) SaveService.SaveNow(hero);
        if (canvas != null) canvas.enabled = false;
        DiabloClickController.ExternalDrive = false;
        var cls = Object.FindFirstObjectByType<ClassSelectUI>(FindObjectsInactive.Include);
        if (cls == null) cls = new GameObject("ClassSelect").AddComponent<ClassSelectUI>();
        cls.SetOpen(true);
        Debug.Log("[Session] Returned to Main Menu.");
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Shown = 0;
        LastSummary = "";
        KindledUnlockedThisRun = false;
        ActOneMode = false;
        ZoneBossMode = false;
        BestDrop = "";
        DiabloClickController.ExternalDrive = false;
        sessionStart = Time.realtimeSinceStartup;
        startStamped = true;
    }
}
