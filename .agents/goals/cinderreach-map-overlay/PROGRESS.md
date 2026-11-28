# Progress: cinderreach-map-overlay

Prozent: 100 %

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
  - `e1a4d7c` chore(scripts): `SHOTCHECK_EXTRA_ARGS` auch im Autoplay-Modus durchgereicht.
  - `9508874` fix(map): Header-Zoomknöpfe überlappen nicht mehr (Review-Befund); Welt-Tracker
    (`StoryDirector.RefreshTracker`) blendet unter der offenen Karte aus.
  - `128b32a` perf(map): Redraw-Takt 0,3 s, Bild-Rect/Quest-Text nur bei Änderung,
    Icon-Pool skippt unveränderte Eigenschaften (Overlay-Kosten senken, K6).
- Belege K1–K6:
  - K1: `WorldMapModePlayTests` grün (Standard Overlay, ToggleMode → Vollbild modal,
    Modus überlebt Schließen/Öffnen, Zoom klemmt 75–150 %, Bild skaliert, Filter-Toggle
    entfernt/stellt Marker wieder her, Quest-Spalte links der Karte, `Header_ControlsDoNotOverlap`).
  - K2: `WorldMapFilterTests` 3/3 grün (jede Kategorie blendet genau ihre Marker aus,
    Held bleibt, volle Menge unverändert).
  - K3: `WorldMapZoomTests` 5/5 grün (Klemme 75/150, Held zentriert, Ecken bei 150 %
    abgedeckt, 75 % in der Frame, Zonenzentrum ohne Offset).
  - K4: Voll-Suiten grün: EditMode 775/775, PlayMode 426/426 (`docs/test-runs.jsonl`),
    `python3 scripts/check-meta.py` OK, `python3 -m pytest scripts -q` 64 passed,
    `KayKitAssets.Verify` Exit 0. `GroupQuestMapTests`/`GroupQuestMapPlayTests` angepasst
    (Modal-Behauptung an Vollbild gebunden, Tracker-verborgen-Assert) und grün.
  - K5: Windows-Shotcheck 1920×1080 mit `-map-shots` auf dem Fix-Build grün (7 Shots,
    Exit 0); `map_overlay` + `map_fullscreen` → `docs/map-overlay/` (JPG + `compare.jpg`
    Side-by-Side). Sichtprüfung (Vision-Agent) auf der Fix-Binärdatei: Kopfzeile ohne
    Überlappung, kein Welt-Quest-Tracker über der Karte, Panel ohne Überlappungen.
    Hinweis: Canvas-Artboard ist außerhalb von Claude.ai nicht abrufbar; die Shots sind der Beleg.
    „Vollbild“ = deckendes, modales Panel wie bisher (D3/P3.3), nicht bildschirmfüllend gestreckt.
  - K6: Perf-Paar auf der finalen Binärdatei (nach `128b32a`), identische Autoplay-Pfade
    (je `DONE phases=11 huskKills=50 quests=13/34`): schlechtestes Fenster Karte zu 70,0 fps,
    Overlay offen 69,0 fps → **98,6 %** (Schwelle 95 %), fps-gate beidseitig PASS;
    Median 151,0 → 170,0 fps. Details und Streuung der Vorläufer-Paare: `docs/map-overlay/perf.md`.
- 2026-11-28: Stand in `.agents/goals/CINDERREACH.md` (P11-Zeile) eingearbeitet.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
