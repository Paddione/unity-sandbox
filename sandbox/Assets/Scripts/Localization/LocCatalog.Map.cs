/// <summary>Player text, domain 'map' (cinderreach-group-quest-map): zone map, legend, minimap.</summary>
public static partial class LocCatalog
{
    static readonly Entry[] Map =
    {
        E("map.title", "KARTE — {0}", "MAP — {0}"),
        E("map.quests", "Verfolgte Aufgaben", "Tracked tasks"),
        E("map.noquest", "Keine Aufgabe verfolgt. Das Fuhrbuch (J) schaltet Aufgaben ein und aus.", "No task tracked. The log (J) turns tasks on and off."),
        E("map.legend", "Legende", "Legend"),
        E("map.legend.you", "Du", "You"),
        E("map.legend.main", "Ziel einer Hauptaufgabe", "Main task goal"),
        E("map.legend.side", "Ziel einer Nebenaufgabe", "Side task goal"),
        E("map.legend.turnin", "Abgabe", "Hand in"),
        E("map.legend.offer", "Neue Aufgabe", "New task"),
        E("map.legend.ally", "Gefährte", "Ally"),
        E("map.legend.npc", "Bewohner", "Resident"),
        E("map.legend.enemy", "Gegner", "Enemy"),
        E("map.legend.gate", "Tor", "Gate"),
        E("map.legend.waypoint", "Wegschrein", "Waypoint"),
        E("map.hint", "◀ ▶ wechselt die Zone  •  Tab wechselt Overlay/Vollbild  •  Mausrad zoomt  •  M oder Esc schließt", "◀ ▶ switch zone  •  Tab switches overlay/fullscreen  •  Mouse wheel zooms  •  M or Esc closes"),
        E("map.mode.overlay", "Overlay", "Overlay"),
        E("map.mode.fullscreen", "Vollbild", "Fullscreen"),
        E("map.filter.quests", "Aufgaben", "Quests"),
        E("map.filter.party", "Gruppe", "Party"),
        E("map.filter.waypoints", "Wegpunkte & Rast", "Waypoints & rest"),
        E("map.filter.persons", "Personen", "People"),
        E("map.filter.elites", "Elitegegner", "Elite enemies"),
    };
}
