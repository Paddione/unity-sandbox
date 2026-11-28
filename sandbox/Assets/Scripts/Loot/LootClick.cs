using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Click-to-loot (cinderreach-loot-click-filter): a left click on an item's
/// ground symbol or its name plate picks it up; walking over items no longer
/// does. Optional gate: with <see cref="DiabloSettings.LootRequiresKey"/> the
/// click only loots while <see cref="DiabloSettings.LootKey"/> is held.
/// </summary>
public static class LootClick
{
    /// <summary>Screen radius (at 1080p) around a drop's ground symbol that counts as a hit.</summary>
    public const float SymbolPickPixels = 42f;
    /// <summary>Pick up once the hero is this close (inside the host reach check).</summary>
    public static float CollectReach => TradeValidator.PickupRange * 0.8f;

    /// <summary>Settings panel is waiting for the next key press to rebind the loot key.</summary>
    public static bool CapturingKey;
    /// <summary>Held-key override for tests (null = read the keyboard).</summary>
    public static bool? ForceKeyHeld;

    /// <summary>Pure gate rule: no key needed, or the key is held.</summary>
    public static bool GateOpen(bool requireKey, bool keyHeld) => !requireKey || keyHeld;

    public static bool KeyHeld()
    {
        if (ForceKeyHeld.HasValue) return ForceKeyHeld.Value;
        var kb = Keyboard.current;
        if (kb == null || !IsBindable(DiabloSettings.LootKey)) return false;
        var k = DiabloSettings.LootKey;
        if (kb[k].isPressed) return true;
        // Modifier pairs count as one binding (left/right Alt, Ctrl, Shift).
        var twin = Twin(k);
        return twin != Key.None && kb[twin].isPressed;
    }

    public static bool GateOpen() => GateOpen(DiabloSettings.LootRequiresKey, KeyHeld());

    static Key Twin(Key k)
    {
        switch (k)
        {
            case Key.LeftAlt: return Key.RightAlt;
            case Key.RightAlt: return Key.LeftAlt;
            case Key.LeftCtrl: return Key.RightCtrl;
            case Key.RightCtrl: return Key.LeftCtrl;
            case Key.LeftShift: return Key.RightShift;
            case Key.RightShift: return Key.LeftShift;
            default: return Key.None;
        }
    }

    /// <summary>Keys the loot binding may use (no Escape, no IME/OEM placeholders).</summary>
    public static bool IsBindable(Key k) =>
        k != Key.None && k != Key.Escape && k != Key.IMESelected && (int)k > 0 && (int)k < (int)Key.OEM1;

    /// <summary>
    /// Player-facing key name from the enum (no <c>KeyControl.displayName</c>:
    /// that queries the OS keymap and crashes headless editors).
    /// </summary>
    public static string KeyName(Key k)
    {
        switch (k)
        {
            case Key.LeftAlt: case Key.RightAlt: return "Alt";
            case Key.LeftCtrl: case Key.RightCtrl: return "Strg";
            case Key.LeftShift: case Key.RightShift: return "Umschalt";
            case Key.Space: return "Leertaste";
            case Key.Tab: return "Tab";
            case Key.Backquote: return "^";
        }
        string raw = k.ToString();
        if (raw.StartsWith("Digit")) return raw.Substring(5);
        if (raw.StartsWith("Numpad")) return "Num " + raw.Substring(6);
        return raw;
    }

    /// <summary>Called every frame while the settings panel captures: first pressed key binds, Escape cancels.</summary>
    public static void PollKeyCapture()
    {
        if (!CapturingKey) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame) { CapturingKey = false; return; }
        foreach (var key in kb.allKeys)
        {
            if (key == null || !key.wasPressedThisFrame || !IsBindable(key.keyCode)) continue;
            DiabloSettings.LootKey = key.keyCode;
            DiabloSettings.Save();
            CapturingKey = false;
            return;
        }
    }

    /// <summary>
    /// The item drop under <paramref name="screen"/>: a visible name plate first,
    /// then the nearest ground symbol within <see cref="SymbolPickPixels"/>.
    /// Filtered (label-less) items stay clickable by their symbol.
    /// </summary>
    public static LootDrop DropAt(Camera cam, Vector2 screen)
    {
        var labeled = DiabloLootLabels.PlateAt(screen);
        if (labeled != null) return labeled;
        if (cam == null) return null;
        float radius = SymbolPickPixels * Mathf.Max(0.5f, Screen.height / 1080f);
        float bestSq = radius * radius;
        LootDrop best = null;
        foreach (var drop in LootDrop.Live)
        {
            if (!Pickable(drop)) continue;
            Vector3 sp = cam.WorldToScreenPoint(drop.transform.position);
            if (sp.z <= 0f) continue;
            float sq = ((Vector2)sp - screen).sqrMagnitude;
            if (sq <= bestSq) { bestSq = sq; best = drop; }
        }
        return best;
    }

    public static bool Pickable(LootDrop drop) =>
        drop != null && drop.isActiveAndEnabled && !drop.isChest && !drop.collected
        && drop.item != null && !Equipment.IsBlank(drop.item);

    public static float PlanarDistance(Vector3 a, Vector3 b)
    {
        var d = a - b;
        d.y = 0f;
        return d.magnitude;
    }
}
