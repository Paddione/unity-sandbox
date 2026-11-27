# Goal: cinderreach-map-overlay

health_goals: [G-DIABLO01, G-DIABLO03, G-DIABLO05]

Ausgangslage: [`.agents/goals/CINDERREACH.md`](../CINDERREACH.md). Design-Vorlage: Canvas
https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard „Karte (M) – Overlay“ (`Map.dc.html`).

## Objective
Die Zonenkarte auf M sieht aus wie das Artboard „Karte (M) – Overlay“ und hat zwei Modi: Overlay (Spielwelt sichtbar,
abgedunkelt, Karte halbtransparent darüber) und Vollbild (deckend), umschaltbar per Tab. Eine Legende blendet Kategorien
(Quests, Gruppe, Wegpunkte & Rast, Personen, Elitegegner) ein und aus, Zoom geht von 75 bis 150 %, links steht die
verfolgte Quest.

## Ist-Befunde
- B1 `sandbox/Assets/Scripts/Map/WorldMapUI.cs` (M, ◀/▶ über freigeschaltete Zonen, Legende), Zonenbild per Top-down-Render in `Map/ZoneMap.cs`, Symbole in `Map/MapIcons.cs`.
- B2 Tests `GroupQuestMapTests`, `GroupQuestMapPlayTests`; Windows-Shots `map_*`.

## Entscheidungen
- D1 ◀/▶ zwischen Zonen bleibt (Eigner, 2026-11-27).
- D2 Overlay ist Standard, Tab schaltet; der Modus wird in `DiabloSettings` (PlayerPrefs) gemerkt.
- D3 Im Overlay läuft das Spiel weiter und Eingaben für Bewegung bleiben aktiv; Vollbild verhält sich wie heute.
- D4 Fonts und Stilbausteine aus `cinderreach-hud-redesign`.

## Erfolgskriterien
1. Tab wechselt Overlay ↔ Vollbild, Auswahl bleibt nach Neustart — Nachweis: PlayMode-Test `WorldMapModePlayTests`.
2. Kategorie-Filter blenden genau ihre Marker aus — Nachweis: EditMode-Test `WorldMapFilterTests`.
3. Zoom klemmt auf 75–150 % und zentriert auf den Helden — Nachweis: EditMode-Test `WorldMapZoomTests`.
4. Bestehende Kartentests grün — Nachweis: `GroupQuestMapTests`, `GroupQuestMapPlayTests` in `bash scripts/run-unity-tests.sh`.
5. Windows-Shots `map_overlay`, `map_fullscreen` — Nachweis: Shotcheck 1920×1080, Vergleichsbild unter `docs/map-overlay/`.
6. Keine Performance-Regression im Overlay — Nachweis: Autoplay mit offener Overlay-Karte, `fps_windows` ≥ 95 % von `main`.

## Nicht-Ziele
- Selbst gesetzte Kartenmarkierungen per Klick.
- Neue Kartengeometrie oder Fog-of-War-Mechanik über das vorhandene Zonenbild hinaus.

## Abhängigkeiten
- `cinderreach-hud-redesign` (Fonts und Stilbausteine).
