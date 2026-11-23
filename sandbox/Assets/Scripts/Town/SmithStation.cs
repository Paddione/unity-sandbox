using UnityEngine;

/// <summary>
/// Lantern-Hold-Schmiede (smithing S2): NPC wie <see cref="Vendor"/>, öffnet
/// das <see cref="DiabloSmithUI"/> des Helden per Klick innerhalb von
/// <see cref="InteractRadius"/>. Reine Interaktion; Transaktionen und Regeln
/// leben in <see cref="SmithingService"/> (EditMode-testbar ohne Szene).
/// </summary>
public sealed class SmithStation : MonoBehaviour
{
    /// <summary>Scene object name.</summary>
    public const string SmithObjectName = "Smith";

    /// <summary>Trade reach around the anvil (same as vendor/chest).</summary>
    public const float InteractRadius = 3.5f;

    public static bool CanInteract(Vector3 heroPos, Vector3 smithPos) =>
        Vendor.CanInteract(heroPos, smithPos, InteractRadius);

    // Perf: Camera.main sucht pro Aufruf per Tag durch die Szene — erst nach
    // dem Klick-Gate anfassen und cachen (Beleg: Zone-Contracts kostete
    // dieselbe Stelle ~2 fps im Autoplay).
    static Camera s_main;

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (s_main == null) s_main = Camera.main;
        if (s_main == null) return;
        var hero = GameObject.Find("Player"); // nur auf Klick-Frames, ok
        if (hero == null) return;
        var heroUI = hero.GetComponent<DiabloSmithUI>();
        if (heroUI != null && heroUI.IsOpen) return; // UI eats its own clicks
        var ray = s_main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f)) return;
        if (hit.collider == null || hit.collider.gameObject != gameObject) return;
        if (!CanInteract(hero.transform.position, transform.position)) return;
        if (StoryDirector.TryOpenTalk(hero, StoryIds.Jorin)) return; // story talk first, forge on the next click
        if (heroUI == null) heroUI = hero.AddComponent<DiabloSmithUI>();
        heroUI.SetOpen(true);
        Debug.Log("[Smith] Forge window opened.");
    }
}
