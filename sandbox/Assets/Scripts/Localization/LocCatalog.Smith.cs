/// <summary>Player text, domain 'smith' (cinderreach-deutsch-story).</summary>
public static partial class LocCatalog
{
    static readonly Entry[] Smith =
    {
        E("smith.msg.reforged", "Umgeschmiedet: {0} (Stufe {1}).", "Reforged: {0} (T{1})."),
        E("smith.bag.row", "{0} [{1}] (Stufe {2})", "{0} [{1}] (T{2})"),
        E("smith.bag.yield", "— ergibt +{0} Material", "— Salvage +{0}"),
        E("smith.bag.noyield", "— nicht verwertbar", "— no salvage"),
        E("smith.replacement.row", "{0}: {1:0.##}–{2:0.##} (Stufe {3})", "{0}: {1:0.##}–{2:0.##} (T{3})"),
        E("smith.msg.noitem", "Auf diesem Platz liegt kein Gegenstand.", "No item in this slot."),
        E("smith.msg.salvaged", "Verwertet ({0}): Material erhalten.", "Salvaged ({0}): material gained."),
        E("smith.msg.salvagerejected", "Verwerten abgelehnt (einzigartig, gebunden oder veraltetes Angebot).", "Salvage rejected (unique, bound or stale offer)."),
        E("smith.msg.pickall", "Wähle Gegenstand, Eigenschaft und Ersatz.", "Pick item, affix and replacement."),
        E("smith.msg.reforgerejected", "Umschmieden abgelehnt (Mittel, Bindung, Eigenschaft oder veraltetes Angebot).", "Reforge rejected (funds, binding, affix or stale offer)."),
        E("smith.purse", "Gold: {0}   Material: {1}   (Umschmieden: {2} Material + {3} Gold)", "Gold: {0}   Material: {1}   (Reforge: {2} + {3}g)"),
        E("smith.msg.pickitem", "Wähle einen Gegenstand.", "Pick an item."),
        E("smith.detail.bound", "{0}: Eigenschaft {1} ist gebunden.", "{0}: slot {1} is bound."),
        E("smith.detail.pickaffix", "{0}: Wähle die Eigenschaft zum Umschmieden.", "{0}: pick the affix to reforge."),
        E("smith.btn.reforge", "Umschmieden (3 Material + 50 Gold)", "Reforge (3 material + 50 gold)"),
        E("smith.title", "Jorin Hagedorns Schmiede", "Lantern Hold smithy"),
        E("smith.col.bag", "Tasche (Klick wählt, zeigt Ausbeute)", "Bag (click selects, shows salvage)"),
        E("smith.col.affix", "Eigenschaft (Klick wählt)", "Affix (click selects)"),
        E("smith.col.replacement", "Ersatz und Spanne", "Replacement + range"),
        E("smith.footer", "F öffnet und schließt, Esc schließt (speichert).", "F toggles, Esc closes (saves)."),
    };
}
