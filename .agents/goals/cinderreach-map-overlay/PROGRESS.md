# Progress: cinderreach-map-overlay

Prozent: 80 %

## Erledigt

- 2026-11-28: Plan angelegt und committet (`34a885e`, `.agents/plans/cinderreach-map-overlay/tasks.md`, P1–P5).
- 2026-11-28: Umsetzung auf Branch `feature/cinderreach-map-overlay`:
  - `99f3327` feat(map): Zonenkarte mit Overlay/Vollbild, Filtern und Zoom — `WorldMapMode`
    (Overlay/Vollbild), `DiabloSettings.MapFullscreen` (PlayerPrefs `DiabloMenu_MapFullscreen`,
    Standard Overlay, D2), Tab toggelt, Layout 1920×1080 (Kopfzeile mit ◀ ▶, Zoom −/+/Prozent,
    Modus-Knopf; Quest-Spalte links 250 px; MapFrame 640² heldenzentriert; Filter-Legende rechts
    mit fünf Toggle-Zeilen), Zoom 75–150 % über `WorldMapZoom` (Mausrad + Knöpfe), Overlay:
    Dim 55 % + halbtransparente Flächen ohne Raycast (Welt läuft weiter, D3), Vollbild: deckend
    und modal (`DiabloUIFactory.IsAnyModalOpen` zählt nur `BlocksWorld`); `MinimapHud` bleibt
    in beiden Modi ausgeblendet; Loc-Keys `map.mode.*`/`map.filter.*` (de+en) in `LocCatalog.Map.cs`.
  - `MapIcons.cs`: `MapCategory`-Flags + `MapIcon.category`, `MapScene.Gather` stempelt
    Quests/Party/Waypoints/Persons/Elites (Held = None, immer sichtbar), reine Helfer
    `WorldMapFilter` + `WorldMapZoom` (Offset klemmt so, dass 150 % die Frame abdeckt und
    75 % drin bleibt).
  - `c95afaf` feat(autoplay): Perf-Schalter `-autoplay-map` (SessionAutoplay öffnet die
    Overlay-Karte, erzwingt Overlay unabhängig von der gespeicherten Wahl).
  - `db212ad` fix(map): neue Debug-Zeilen in `loc-allowlist.txt`.
  - `e1a4d7c` chore(scripts): `SHOTCHECK_EXTRA_ARGS` auch im Autoplay-Modus durchgereicht
    (nötig für den `-autoplay-map`-Perf-Lauf).
- Belege K1–K4:
  - K1: `WorldMapModePlayTests` grün (Standard Overlay, ToggleMode → Vollbild modal,
    Modus überlebt Schließen/Öffnen, Zoom klemmt 75–150 %, Bild skaliert, Filter-Toggle
    entfernt/stellt Marker wieder her, Quest-Spalte links der Karte).
  - K2: `WorldMapFilterTests` 3/3 grün (jede Kategorie blendet genau ihre Marker aus,
    Held bleibt, volle Menge unverändert).
  - K3: `WorldMapZoomTests` 5/5 grün (Klemme 75/150, Held zentriert, Ecken bei 150 %
    abgedeckt, 75 % in der Frame, Zonenzentrum ohne Offset).
  - K4: Voll-Suiten grün auf `db212ad`: EditMode 775/775, PlayMode 425/425
    (`docs/test-runs.jsonl`), `python3 scripts/check-meta.py` OK, `python3 -m pytest scripts -q`
    64 passed. `GroupQuestMapTests`/`GroupQuestMapPlayTests` angepasst (Modal-Behauptung
    an Vollbild gebunden) und grün.

## Nächste Schritte

- Windows-Build + Stage WIP, Shotcheck 1920×1080 mit `-map-shots` (Shots `map_overlay`,
  `map_fullscreen` → `docs/map-overlay/`).
- Perf-Paar (K6): Autoplay auf Windows, Zweier-Paar gleiche Binärdatei ohne/mit
  `-autoplay-map`, schlechtestes Fenster ≥ 95 %; Ergebnis `docs/map-overlay/perf.md`.
- `PROGRESS.md` auf 100 % mit Belegen K1–K6, Stand in `CINDERREACH.md` (P11-Zeile),
  dann `python3 ~/scripts/agent-workspace.py land` (`land: ff`).

## Offene Punkte

- K5 (Windows-Shots) und K6 (Perf ≥ 95 % von main) noch unbelegt; Land steht aus.
