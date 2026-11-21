using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot onboarding hint at the Lantern Hold gate (Ashen Fen).
/// Logs the crisp onboarding hint cleanly and displays an unobtrusive, auto-fading
/// zone arrival toast banner (skipped in batchmode so headless tests stay fast).
/// </summary>
public sealed class ZoneEntryHint : MonoBehaviour
{
    bool shown;

    void OnTriggerEnter(Collider other)
    {
        if (shown || other == null || other.gameObject == null) return;
        var go = other.gameObject;
        if (go.name != "Player" && !go.CompareTag("Player")) return;
        shown = true;
        Debug.Log("[Diablo] Entered Lantern Hold — WASD/arrows to move, aim with the mouse, " +
                  "click to strike (click a husk to chase it). " +
                  "Clear the Ashen Fen, loot the west elite, open the chest, " +
                  "extract at the north Fen Gate. I opens your inventory. " +
                  "Warning: husks strike back — watch their crouch, dodge, the campfire heals.");

        if (Application.isPlaying && !Application.isBatchMode)
            StartCoroutine(ShowArrivalToast());
    }

    IEnumerator ShowArrivalToast()
    {
        var canvasGo = new GameObject("ZoneToastCanvas");
        var canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 8, raycaster: false);

        var card = DiabloUIFactory.MakePanel(canvasGo.transform, "ZoneToastPanel", new Vector2(560f, 76f), new Vector2(0f, -86f),
            DiabloMenuArt.TooltipBackground());
        var rt = card.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        var cg = card.AddComponent<CanvasGroup>();

        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 8, 8);
        v.spacing = 2f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        var title = DiabloUIFactory.MakeText(card.transform, "ZoneTitle", "LANTERN HOLD  •  THE ASHEN FEN", 18, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(title.gameObject, prefH: 26f, flexW: 1f);

        var sub = DiabloUIFactory.MakeText(card.transform, "ZoneHint",
            "WASD Move  •  Click Strike  •  Q/E/R Skills  •  I Bag  •  C Character  •  Alt Loot names  •  Esc Pause",
            13, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(sub.gameObject, prefH: 20f, flexW: 1f);

        cg.alpha = 0f;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            cg.alpha = t / 0.35f;
            yield return null;
        }
        cg.alpha = 1f;
        yield return new WaitForSecondsRealtime(4.2f);
        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            cg.alpha = 1f - (t / 0.6f);
            yield return null;
        }
        Destroy(canvasGo);
    }
}
