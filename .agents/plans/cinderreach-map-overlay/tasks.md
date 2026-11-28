---
goal: cinderreach-map-overlay
slug: cinderreach-map-overlay
branch: feature/cinderreach-map-overlay
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO03, G-DIABLO05]
priority: P11 (nach cinderreach-hud-redesign gelandet als 7176e5a)
status: planned
plan_refs: [.agents/goals/cinderreach-map-overlay/GOAL.md]
---

# Plan: cinderreach-map-overlay — Zonenkarte M mit Overlay/Vollbild

Pfad: feature, ein Branch, fünf Partials (P1–P5) in Abhängigkeitsreihenfolge. Abschluss per
`python3 ~/scripts/agent-workspace.py land` (`land: ff`). Vorlage: Canvas
https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard „Karte (M) – Overlay“ (`Map.dc.html`, 1920×1080).
Artboard nicht abrufbar — GOAL.md-Objective + `HudChrome`/`DiabloUIFactory` sind die Vorlage.

## Goal

Siehe [GOAL.md](../../goals/cinderreach-map-overlay/GOAL.md), Erfolgskriterien 1–6, Entscheidungen D1–D4.

## Ausgangslage (HEAD `d5e7731`, 2026-11-28)

- `sandbox/Assets/Scripts/Map/WorldMapUI.cs` (228 Zeilen): Canvas `WorldMapCanvas` (Order 24), Panel
  `MapPx+360 × MapPx+110` (1000×750), `MapFrame` 640×640 links (22,22) mit `RectMask2D`, `RawImage`
  (Zonenbild aus `ZoneMap.Picture`), `MapIconLayer`, rechts `MapSide` 310 px mit verfolgten Aufgaben
  (`QuestLines`) und statischer Legende (10 Einträge, keine Filter). `Update`: M toggelt, Esc schließt,
  ◀/▶ radelt über freigeschaltete Zonen (`Cycle`), Redraw alle 0,15 s. Kein Modus, kein Zoom.
- `sandbox/Assets/Scripts/Map/MapIcons.cs`: `MapIcon` (world/shape/color/size/rotation/pinToEdge) ohne
  Kategorie; `MapScene.Gather` sammelt Tore, Wegschreine, NPCs, Gegner (Elite größer), Gefährten,
  Questmarken, Held zuletzt. `MinimapHud` nutzt dieselbe Gather-Quelle (28 m, 220 px).
- `sandbox/Assets/Scripts/Map/ZoneMap.cs`: `ToMap` (0..1, y oben), `Span`, `IndexAt`, Top-down-Render 512².
- `DiabloSettings.cs`: PlayerPrefs-Präfix `DiabloMenu_`, `Load`/`Save`/`ResetDefaults`/`Clamp`;
  im Batchmode lädt `Load` nicht. HUD-Stil: `HudChrome.Panel/Img`, `DiabloUIFactory.DisplayFont` (Cinzel) /
  `BodyFont`, `DiabloPalette` (HudPanel 0,90 α, PanelEdge, TrimGold).
- Modal: `DiabloUIFactory.IsAnyModalOpen` zählt `WorldMapUI.IsOpen`; `IsWorldInputBlocked` delegiert
  dorthin. WASD läuft in `DiabloClickController.PollKeyboard` modalunabhängig, Klick-Angriffe sind
  modalblockiert. `MinimapHud` blendet bei offener Zonenkarte aus.
- Tests: `GroupQuestMapTests` (EditMode, u. a. `ZoneMap_IndicesHopsAndProjection`,
  `Catalog_MapAndShareTextsExist`), `GroupQuestMapPlayTests` (`Minimap_AndZoneMap_ShowHeroAndQuestPlaces`
  behauptet Modalität der Vollbild-Karte). Shots `map_zone_fen`/`map_zone_ossuary`/`map_minimap_tracker`
  in `DiabloShotCheck.MapShots` (`-map-shots`).

## File Structure

| Partial | Dateien (disjunkt bis auf Vermerk) |
|---|---|
| P1 Settings | `sandbox/Assets/Scripts/DiabloSettings.cs` |
| P2 Kategorien | `sandbox/Assets/Scripts/Map/MapIcons.cs`, neu `sandbox/Assets/Tests/EditMode/WorldMapFilterTests.cs`, neu `sandbox/Assets/Tests/EditMode/WorldMapZoomTests.cs` |
| P3 Karte | `sandbox/Assets/Scripts/Map/WorldMapUI.cs`, `sandbox/Assets/Scripts/DiabloUIFactory.cs` (nur Modal-Zeile), `sandbox/Assets/Scripts/Localization/LocCatalog.Map.cs` |
| P4 Tests/Shots | neu `sandbox/Assets/Tests/PlayMode/WorldMapModePlayTests.cs`, `sandbox/Assets/Tests/PlayMode/GroupQuestMapPlayTests.cs` (Modal-Behauptung an Vollbild binden), `sandbox/Assets/Scripts/DiabloShotCheck.cs` (nur Map-Posen) |
| P5 Verify | `docs/map-overlay/*`, `docs/test-runs.jsonl`, Goal-/CINDERREACH-Doku |

## P1 — Kartenmodus in DiabloSettings

1. `DiabloSettings.MapFullscreen` (bool, Standard `false` = Overlay laut D2), PlayerPrefs-Key
   `DiabloMenu_MapFullscreen` (int 0/1) in `Load`/`Save`, Default in `ResetDefaults`. Kein `Clamp`-Eintrag nötig.
2. Test-Seams bleiben kopflos sicher (bestehende try/catch-Muster übernehmen).
- Verify: `PLATFORMS=EditMode TEST_FILTER=GroupQuestMapTests bash scripts/run-unity-tests.sh` grün.

## P2 — Kategorien und reine Zoom-/Filterlogik

1. `MapCategory`-Flags (`Quests`, `Party`, `Waypoints`, `Persons`, `Elites`) in `MapIcons.cs`;
   `MapIcon.category`-Feld; `MapScene.Gather` stempelt: Questmarken→Quests, Gefährten→Party,
   Tore+Wegschreine→Waypoints, NPCs→Persons, Gegner→Elites, Held→keine (immer sichtbar).
2. Reine Helfer (EditMode-testbar, keine Szene): `WorldMapFilter.IsVisible(icon, enabled)` und
   `WorldMapFilter.Apply(icons, enabled, into)`; `WorldMapZoom.ClampZoom` (0,75–1,5),
   `WorldMapZoom.Offset(heroMap01, zoom, mapPx)` (heldenzentriert, Randklemme hält die Frame-Abdeckung),
   `WorldMapZoom.ToLocal(worldMap01, centerMap01, zoom, mapPx)`.
3. Tests `WorldMapFilterTests`: jede Kategorie blendet genau ihre Marker aus, Held bleibt, volle Menge
   ohne Filter unverändert. `WorldMapZoomTests`: Klemme an beiden Enden, Held landet in der Mitte,
   Ecken bleiben bei 150 % abgedeckt, bei 75 % liegt die Karte in der Frame.
- Verify: gefilterte EditMode-Läufe je Testklasse grün (vor P3 noch ohne UI-Anbindung der Filter).

## P3 — WorldMapUI: Modi, Zoom, Filter, Quest links

1. Modus-Enum `WorldMapMode { Overlay, Fullscreen }`; `Mode`-Property, `IsFullscreen`/`IsOverlay`;
   `SetOpen(true)` liest `DiabloSettings.MapFullscreen` (D2), `ToggleMode()` schaltet, schreibt
   `DiabloSettings` + `Save()`. Tab (`tabKey`) toggelt im offenen Zustand; M/Esc wie bisher; ◀/▶ bleibt (D1).
2. Layout 1920×1080: Kopfzeile (Titel in Cinzel, ◀ ▶, Modus-Knopf, Zoom −/Prozent/+ , Schließen);
   links Quest-Spalte (~250 px, `HudChrome.Panel`, `QuestLines`-Inhalt), Mitte `MapFrame` 640×640,
   rechts Filter-Legende: fünf Toggle-Zeilen (Ikone + Name + Zustand), darunter „Du“-Zeile und Hinweis.
   Toggle-Klick schaltet die Kategorie (Daten in `enabledCategories`), Redraw filtert über `WorldMapFilter`.
3. Overlay: Vollbild-Abdunklung (schwarz ~55 %) hinter dem Panel, Panel-/Bild-Alpha < 1 (Welt scheint
   durch), interaktive Elemente raycasten, Flächen nicht (Weltklicks fallen durch). Vollbild: deckende
   Flächen wie heute (Abyss-Backing, Alpha 1), kein Dim. `ApplyMode()` stellt alle Alphas/Dim um.
4. Zoom: Mausrad + Knöpfe rufen `SetZoom(float)` (Klemme aus P2); `Redraw` positioniert Bild und Ikonen
   heldenzentriert über `WorldMapZoom` (Ansichts-Zone ≠ Heldenzone: Zonenzentrum). Redraw-Takt 0,15 s bleibt (Perf).
5. `DiabloUIFactory.IsAnyModalOpen`: Zonenkarte zählt nur im Vollbild als modal; Overlay lässt
   Bewegung/Welt offen (D3). `MinimapHud`-Ausblendung bei offener Karte bleibt in beiden Modi.
6. Loc-Keys in `LocCatalog.Map.cs`: Modusnamen, Filterkategorien, Zoom, Hinweise (de+en); bestehende
   `map.legend.*`-Keys bleiben (Minimap/Katalogtest).
- Verify: `PLATFORMS=EditMode TEST_FILTER=WorldMapFilterTests bash scripts/run-unity-tests.sh`,
  `PLATFORMS=PlayMode TEST_FILTER=GroupQuestMapPlayTests bash scripts/run-unity-tests.sh` grün.

## P4 — PlayMode-Nachweis und Shot-Posen

1. `WorldMapModePlayTests`: Standard Overlay nach Öffnen; Tab-Äquivalent `ToggleMode()` → Vollbild;
   Auswahl überlebt Schließen/Öffnen (Settings); Overlay ist nicht modal (`IsAnyModalOpen` falsch,
   `IsWorldInputBlocked` falsch), Vollbild ist modal; Zoom-Seam klemmt sichtbar (Bildgröße/Iconlage).
2. `GroupQuestMapPlayTests.Minimap_AndZoneMap_ShowHeroAndQuestPlaces`: Modal-Behauptung erst nach
   Umschalten auf Vollbild prüfen; Rest unverändert.
3. `DiabloShotCheck.MapShots`: Posen `map_overlay` (Standard) und `map_fullscreen` (nach Toggle)
   bei Fen-Ansicht ergänzen; `map_zone_*` bleiben.
- Verify: `PLATFORMS=PlayMode TEST_FILTER=WorldMapModePlayTests bash scripts/run-unity-tests.sh` grün.

## P5 — Verifikation und Abschluss

1. `bash scripts/run-unity-tests.sh` (EditMode + PlayMode) grün, Zeilen in `docs/test-runs.jsonl`.
2. `python3 scripts/check-meta.py`, `python3 -m pytest scripts -q`, KayKit-Verify aus `repo.yaml`.
3. Windows-Build + Stage WIP + `SHOTCHECK_SIZE=1920x1080 bash scripts/shotcheck-windows.sh` mit
   `-map-shots`; Shots `map_overlay`, `map_fullscreen` nach `docs/map-overlay/`, Vergleichsbild
   Shot ↔ Artboard daneben. `NEW_HERO FAIL` ggf. per main-Baseline gegenprüfen (vorbestehend).
4. Perf (Kriterium 6): Autoplay mit offener Overlay-Karte auf Windows (`fps_windows` ≥ 95 % von `main`,
   Paar-Messung wie `docs/hud-redesign/perf.md`); Ergebnis in `docs/map-overlay/perf.md`.
   Ist kein Windows-Lauf möglich, bleibt das Kriterium in `PROGRESS.md` offen benannt.
5. `PROGRESS.md` mit Belegen K1–K6, `## Offene Punkte` erst „Keine.“ bei 100 %; Stand in
   CINDERREACH.md (P11-Zeile); generierte `*Locomotion.controller`-Re-Serialisierungen und
   `AddressableAssetsData/Windows*/link.xml` vor Commit reverten, nie stagen.
6. `python3 ~/scripts/agent-workspace.py land` (`land: ff`); `test-runs.jsonl`-Konflikt per Union lösen.
