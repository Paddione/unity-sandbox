# Cinderreach-Inventar mit Live-Charakter, Vergleich und Swap

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]

## Objective

Das Cinderreach-Inventar wird zu einer kombinierten Ansicht umgebaut: Live-3D-Charakter mit Ausrüstungsslots und Tasche gleichzeitig sichtbar, mit Shift-Vergleich, Drag & Drop und Rechtsklick-Swap. Truhe, Händler und Schmied nutzen denselben Tooltip- und Vergleichspfad. Pause- und Stats-Seite werden visuell nachgezogen.

## Erfolgskriterien

1. Ein Inventarfenster (`DiabloInventoryUI`) zeigt links das Live-3D-Modell des Spielers (RenderTexture, eigener Layer, per Maus drehbar) mit den Slots Helm, Rüstung und Waffe und rechts das Taschenraster. Zwischen Ausrüstung und Tasche ist kein Tab-Wechsel nötig.
2. Das 3D-Modell zeigt angelegte Equipment-Art und aktualisiert sich nach jedem An- und Ablegen.
3. Hover über ein Taschen-Item mit gehaltener Shift-Taste zeigt den Tooltip neben dem des im selben Slot angelegten Items. Abweichende Stats tragen einen grünen ↑-Pfeil (besser) oder roten ↓-Pfeil (schlechter) mit Delta. Bei leerem Slot sind alle Stats grün, gleiche Stats bleiben ohne Pfeil.
4. Drag & Drop Tasche → passender Slot legt an und tauscht das alte Item in die Tasche. Ein Drop auf einen falschen Slot wird ohne Statewechsel abgewiesen und rot markiert. Slot → Tasche legt ab.
5. Rechtsklick auf ein Taschen-Item tauscht es sofort in seinen Slot. Rechtsklick auf einen Trank trinkt ihn.
6. Alle Swaps laufen über den autoritativen Pfad in `DiabloNetworkPlayer.Items.cs` und funktionieren im Multiplayer.
7. Truhe, Händler und Schmied verwenden die gemeinsame Tooltip- und Vergleichskomponente (Shift-Vergleich auch dort).
8. Visuelles Polish: einheitliche Rarity-Rahmen für Magic/Rare/Unique, Hover- und Drag-Feedback, konsistente Abstände nach `DiabloPalette`/`DiabloUIFactory`, überarbeitete Pause- und Stats-Seite.
9. EditMode-Tests decken die Stat-Diff-Logik ab (besser, schlechter, gleich, leerer Slot). PlayMode-Tests decken Rechtsklick-Swap, Drag-Swap und abgewiesenen Fehl-Drop ab.
10. Windows-GPU-Shotcheck liefert Screenshots von kombiniertem Inventar, Shift-Vergleich mit beiden Pfeilfarben und Händler-Vergleich.
11. `G-DIABLO01/02/03/06` grün, Session-Autoplay ohne Regression.

## Nicht-Ziele

Keine neuen Ausrüstungsslots, Items, Affixe oder Balance-Änderungen. Klassenwahl, Einstellungen und Risk-Shrine-UI bleiben unverändert. Kein Gamepad-Support für Drag & Drop.

## Abhängigkeiten

- Equipment-Art aus `cinderreach-equipment-art` (100 %).
- Windows-Shotcheck über die WIP-Stage `Games\cinderreach-wip`; Perf-Nachweis nur auf Windows.
- Die Vorschau-Kamera rendert nur bei offenem Inventar. Die UI sendet nur Requests, der Server bleibt autoritativ.
