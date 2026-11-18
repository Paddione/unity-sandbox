using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schmiede für gezielte Beuteverbesserung (Lantern Hold, Solo-Station).
/// Verwertung: magisch 1, selten 3 Material. Umschmieden: 3 Material + 50 Gold,
/// pro Gegenstand dauerhaft ein Affix-Slot, übrige Affixe bleiben erhalten,
/// Uniques sind von beiden Aktionen ausgeschlossen. Vorschau und Ausführung
/// teilen sich dieselbe Validierung; die erwartete Item-Instanz (Referenz)
/// entwertet jede Bestätigung nach Erfolg oder Taschenänderung (stale offer).
/// Eine In-Flight-Sperre schützt vor Wiedereintritt, Wallet + Inventar +
/// Material werden in einem Snapshot gespeichert — bei Save-Schreibfehlern
/// bleibt alles wie vor der Aktion.
/// </summary>
public static class SmithingService
{
    public const int SalvageMagicMaterial = 1;
    public const int SalvageRareMaterial = 3;
    public const int ReforgeMaterialCost = 3;
    public const int ReforgeGoldCost = 50;

    /// <summary>
    /// Fehlerseam für Tests: true = gespeichert, false/throw = Schreibfehler.
    /// Null nutzt <see cref="SaveService.SaveNow"/>.
    /// </summary>
    public static System.Func<GameObject, bool> SaveHook;

    static readonly HashSet<ItemInstance> s_inFlight = new HashSet<ItemInstance>();
    static int s_offerGeneration;

    /// <summary>Session zurücksetzen (Szenenwechsel/Tests): Sperren + Angebote.</summary>
    public static void ResetSession()
    {
        s_inFlight.Clear();
        s_offerGeneration = 0;
    }

    /// <summary>Materialertrag der Vorschau (0 = nicht verwertbar).</summary>
    public static int SalvageYield(ItemInstance item)
    {
        if (!IsSalvageable(item)) return 0;
        return item.rarity == ItemRarity.Rare ? SalvageRareMaterial : SalvageMagicMaterial;
    }

    public static void ReforgeCost(out int material, out int gold)
    {
        material = ReforgeMaterialCost;
        gold = ReforgeGoldCost;
    }

    /// <summary>Zulässige Ersatz-Affixe: Slot-Pool ohne bereits belegte Keys.</summary>
    public static List<string> ReplacementCandidates(ItemInstance item)
    {
        var result = new List<string>();
        if (item == null) return result;
        var used = new HashSet<string>();
        if (item.affixes != null)
            foreach (var a in item.affixes)
                if (a != null) used.Add(a.key);
        foreach (var key in AffixLibrary.PoolFor(item.slot))
            if (!used.Contains(key)) result.Add(key);
        return result;
    }

    /// <summary>
    /// Vorschau: prüft wie die Ausführung und liefert die Rollspanne des
    /// Ersatzes in der Stufe des Gegenstands. Erzeugt ein sessionlokales Angebot.
    /// </summary>
    public static bool PreviewReforge(GameObject hero, int bagIndex, ItemInstance expected,
        int affixIndex, string replacementKey, out float min, out float max)
    {
        min = 0f; max = 0f;
        if (!ValidateReforge(hero, bagIndex, expected, affixIndex, replacementKey,
                out _, out _, out _)) return false;
        var def = AffixLibrary.Get(replacementKey);
        if (def == null) return false;
        (min, max) = AffixLibrary.GetRange(replacementKey, expected.itemLevel, TierOf(expected));
        s_offerGeneration++;
        return true;
    }

    /// <summary>Verwertet Tasche[bagIndex]: Gegenstand weg, Material gutgeschrieben (genau einmal).</summary>
    public static bool TrySalvage(GameObject hero, int bagIndex, ItemInstance expected)
    {
        if (!HeroParts(hero, out Inventory inv, out _)) return false;
        if (!ReferenceEquals(inv.Get(bagIndex), expected)) return false; // stale offer
        if (!IsSalvageable(expected)) return false;
        if (!s_inFlight.Add(expected)) return false; // In-Flight-Sperre
        try
        {
            int yield = SalvageYield(expected);
            int before = RunProfile.SmithingMaterial;
            if (!inv.RemoveAt(bagIndex)) return false;
            RunProfile.SmithingMaterial = before + yield;
            if (!Persist(hero))
            {
                // Rollback: Gegenstand zurück, Material wie vorher.
                inv.Add(expected);
                RunProfile.SmithingMaterial = before;
                return false;
            }
            Debug.Log($"[Smith] Salvaged {LootDrop.ItemLabel(expected)} (+{yield} material).");
            return true;
        }
        finally { s_inFlight.Remove(expected); }
    }

    /// <summary>
    /// Schmiedet genau einen Affix-Slot um. Kandidat entsteht als Kopie des
    /// Gegenstands inklusive aller übrigen Affixe; der Ersatz wird in der
    /// Stufe des Gegenstands neu gerollt und der Slot dauerhaft gebunden.
    /// </summary>
    public static bool TryReforge(GameObject hero, int bagIndex, ItemInstance expected,
        int affixIndex, string replacementKey)
    {
        if (!ValidateReforge(hero, bagIndex, expected, affixIndex, replacementKey,
                out Inventory inv, out Wallet wallet, out Affix kept)) return false;
        if (!s_inFlight.Add(expected)) return false; // In-Flight-Sperre
        try
        {
            var def = AffixLibrary.Get(replacementKey);
            int tier = TierOf(expected);
            float rolled = def.Roll(tier, expected.itemLevel, new System.Random());
            // Snapshot: Gold, Material, alter Affix, Bindung.
            int goldBefore = wallet.Gold;
            int matBefore = RunProfile.SmithingMaterial;
            var oldAffix = expected.affixes[affixIndex];
            float oldValue = oldAffix.value;
            string oldKey = oldAffix.key;
            if (!wallet.TrySpend(ReforgeGoldCost)) return false;
            RunProfile.SmithingMaterial = matBefore - ReforgeMaterialCost;
            expected.affixes[affixIndex] = new Affix(replacementKey, rolled);
            expected.reforgeIndexPlusOne = affixIndex + 1;
            if (!Persist(hero))
            {
                wallet.SetGold(goldBefore);
                RunProfile.SmithingMaterial = matBefore;
                expected.affixes[affixIndex] = new Affix(oldKey, oldValue);
                expected.reforgeIndexPlusOne = 0;
                return false;
            }
            s_offerGeneration++;
            Debug.Log($"[Smith] Reforged {LootDrop.ItemLabel(expected)}: {oldKey} → " +
                      $"{replacementKey} {AffixLibrary.Describe(new Affix(replacementKey, rolled))}.");
            return true;
        }
        finally { s_inFlight.Remove(expected); }
    }

    // ---------- Validierung (Vorschau = Ausführung) ----------

    static bool ValidateReforge(GameObject hero, int bagIndex, ItemInstance expected,
        int affixIndex, string replacementKey, out Inventory inv, out Wallet wallet, out Affix kept)
    {
        inv = null; wallet = null; kept = null;
        if (!HeroParts(hero, out inv, out wallet)) return false;
        if (expected == null || !ReferenceEquals(inv.Get(bagIndex), expected)) return false; // stale
        if (expected.rarity == ItemRarity.Unique) return false;
        if (expected.rarity != ItemRarity.Magic && expected.rarity != ItemRarity.Rare) return false;
        if (expected.slot == ItemSlot.Consumable) return false;
        if (expected.affixes == null || affixIndex < 0 || affixIndex >= expected.affixes.Count) return false;
        if (expected.reforgeIndexPlusOne != 0) return false; // Slot bereits gebunden
        var def = AffixLibrary.Get(replacementKey);
        if (def == null || !def.AllowedOn(expected.slot)) return false;
        foreach (var a in expected.affixes) // belegte Keys prüfen
            if (a != null && a != expected.affixes[affixIndex] && a.key == replacementKey) return false;
        if (RunProfile.SmithingMaterial < ReforgeMaterialCost) return false;
        if (wallet.Gold < ReforgeGoldCost) return false;
        kept = expected.affixes[affixIndex];
        return true;
    }

    static bool IsSalvageable(ItemInstance item)
    {
        if (item == null || item.slot == ItemSlot.Consumable) return false;
        if (item.rarity == ItemRarity.Unique) return false;
        return item.rarity == ItemRarity.Magic || item.rarity == ItemRarity.Rare;
    }

    static int TierOf(ItemInstance item) =>
        item != null && item.affixTier >= 2 ? 2 : 1;

    static bool HeroParts(GameObject hero, out Inventory inv, out Wallet wallet)
    {
        inv = null; wallet = null;
        if (hero == null) return false;
        inv = hero.GetComponent<Inventory>();
        wallet = hero.GetComponent<Wallet>();
        return inv != null && wallet != null;
    }

    static bool Persist(GameObject hero)
    {
        try
        {
            if (SaveHook != null) return SaveHook(hero);
            SaveService.SaveNow(hero);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Smith] Save failed, rolled back: {e.Message}");
            return false;
        }
    }
}
