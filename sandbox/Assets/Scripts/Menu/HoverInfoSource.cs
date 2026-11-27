using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lightweight pointer hover trigger that feeds <see cref="InfoTooltip"/> and
/// optionally tints a row background or updates a secondary detail callback.
/// Works regardless of EventSystem creation order.
/// </summary>
public sealed class HoverInfoSource : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    GameObject hero;
    Func<(string title, string meta, string body, string hint)> provider;
    Action onEnterExtra;
    Image highlightTarget;
    Color normalColor;
    Color hoverColor;

    public void Bind(GameObject ownerHero,
        Func<(string title, string meta, string body, string hint)> infoProvider,
        Image rowImage = null, Color? hoverTint = null, Action onEnter = null)
    {
        hero = ownerHero;
        provider = infoProvider;
        highlightTarget = rowImage;
        if (rowImage != null) normalColor = rowImage.color;
        hoverColor = hoverTint ?? new Color(0.28f, 0.22f, 0.16f, 0.55f);
        onEnterExtra = onEnter;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightTarget != null) highlightTarget.color = hoverColor;
        onEnterExtra?.Invoke();
        if (hero == null || provider == null) return;
        if (DiabloUIFactory.IsRemoteMirror(hero.transform)) return;
        var tip = InfoTooltip.For(hero);
        if (tip == null) return;
        var data = provider();
        if (string.IsNullOrEmpty(data.title) && string.IsNullOrEmpty(data.body)) return;
        tip.Show(data.title, data.meta, data.body, gameObject, data.hint);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightTarget != null) highlightTarget.color = normalColor;
        if (hero != null) InfoTooltip.HideFor(hero);
    }

    void OnDisable()
    {
        if (highlightTarget != null) highlightTarget.color = normalColor;
    }
}
