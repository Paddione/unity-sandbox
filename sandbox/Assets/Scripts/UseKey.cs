using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Something the hero can use with the use key: talk, trade, open, read.</summary>
public interface IUsable
{
    /// <summary>Reach on the ground plane around the object.</summary>
    float UseRadius { get; }
    /// <summary>Prompt text after the key, e.g. "Sprechen: Ottilie Wachs"; null hides the object.</summary>
    string UseLabel { get; }
    /// <summary>Does the use (same effect as the click).</summary>
    void Use(GameObject hero);
}

/// <summary>Live usables, registered by the components in OnEnable/OnDisable.</summary>
public static class Usables
{
    static readonly List<MonoBehaviour> live = new List<MonoBehaviour>();

    public static void Register(MonoBehaviour usable)
    {
        if (usable is IUsable && !live.Contains(usable)) live.Add(usable);
    }

    public static void Unregister(MonoBehaviour usable) => live.Remove(usable);

    /// <summary>The nearest usable in reach of <paramref name="heroPos"/> that currently has a label.</summary>
    public static IUsable Nearest(Vector3 heroPos, out MonoBehaviour owner)
    {
        owner = null;
        IUsable best = null;
        float bestSq = float.MaxValue;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var mb = live[i];
            if (mb == null) { live.RemoveAt(i); continue; }
            if (!mb.isActiveAndEnabled) continue;
            var u = (IUsable)mb;
            var d = mb.transform.position - heroPos;
            d.y = 0f;
            float sq = d.sqrMagnitude;
            if (sq > u.UseRadius * u.UseRadius || sq >= bestSq) continue;
            if (string.IsNullOrEmpty(u.UseLabel)) continue;
            best = u;
            bestSq = sq;
            owner = mb;
        }
        return best;
    }

    public static int Count => live.Count;
}

/// <summary>
/// Keyboard use key (T) on the local hero: shows "[T] …" for the nearest
/// usable in reach and uses it on press. Mouse clicks keep working; this is
/// the same action without aiming. Q/E/R/Space/F/G stay skill slots.
/// </summary>
public sealed class UseKey : MonoBehaviour
{
    public const Key Binding = Key.T;

    Canvas canvas;
    Text prompt;
    float poll;
    IUsable current;

    void Update()
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) return;
        poll -= Time.unscaledDeltaTime;
        if (poll <= 0f)
        {
            poll = 0.15f;
            current = DiabloUIFactory.IsAnyModalOpen(gameObject) || StoryDialogUI.AnyOpen
                ? null
                : Usables.Nearest(transform.position, out _);
            ShowPrompt(current);
        }
        var kb = Keyboard.current;
        if (kb == null || current == null || !kb[Binding].wasPressedThisFrame) return;
        if (DiabloUIFactory.IsAnyModalOpen(gameObject) || StoryDialogUI.AnyOpen) return;
        var target = current;
        current = null;
        ShowPrompt(null);
        poll = 0.3f;
        target.Use(gameObject);
    }

    /// <summary>Uses the nearest usable now (tests, automation). False when nothing is in reach.</summary>
    public bool UseNearest()
    {
        var u = Usables.Nearest(transform.position, out _);
        if (u == null) return false;
        u.Use(gameObject);
        return true;
    }

    void ShowPrompt(IUsable u)
    {
        if (u == null) { if (canvas != null) canvas.enabled = false; return; }
        if (canvas == null)
        {
            var go = new GameObject("UseKeyCanvas");
            go.transform.SetParent(transform, false);
            canvas = DiabloUIFactory.ConfigureOverlayCanvas(go, sortingOrder: 24, raycaster: false);
            prompt = DiabloUIFactory.MakeText(go.transform, "UsePrompt", "", 18, DiabloPalette.Parchment, TextAnchor.MiddleCenter);
            var rt = prompt.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 150f); rt.sizeDelta = new Vector2(600f, 32f);
        }
        prompt.text = Loc.F("use.prompt", Binding.ToString(), u.UseLabel);
        canvas.enabled = true;
    }
}
