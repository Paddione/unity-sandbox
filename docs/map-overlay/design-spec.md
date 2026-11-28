# Design-Spec: Karte (M) – Overlay (lokale Ablage des Canvas-Artboards)

Quelle: Canvas https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard
„Karte (M) – Overlay“ (`Map.dc.html`, 1920×1080). Das Artboard ist außerhalb
von Claude.ai nicht abrufbar — **diese Datei ist die lokale Source of Truth**
für das Design. Umsetzung: Goal `cinderreach-map-overlay` (100 %, gelandet
`002f485`), Belege `WorldMapModePlayTests`, `WorldMapFilterTests`,
`WorldMapZoomTests`, Shots `map_overlay.jpg`, `map_fullscreen.jpg`,
`compare.jpg`, Perf `perf.md` (alle in diesem Ordner). Stil/Farben/Fonts siehe
[HUD-Spec](../hud-redesign/design-spec.md) (D4).

## Modi

- **Overlay (Standard):** Spielwelt sichtbar, schwarz ~55 % abgedunkelt, Karte
  halbtransparent darüber, nicht modal — das Spiel läuft weiter, Bewegung und
  Welt-Eingaben bleiben aktiv.
- **Vollbild:** deckende Flächen (wie bisher), modal.
- Umschaltung per **Tab**, Persistenz in `DiabloSettings.MapFullscreen`
  (PlayerPrefs `DiabloMenu_MapFullscreen`). M öffnet/schließt, Esc schließt.

## Layout 1920×1080

- **Kopfzeile:** Titel in Cinzel, ◀/▶ über freigeschaltete Zonen (D1),
  Modus-Knopf, Zoom −/Prozent/+, Schließen. Knöpfe überlappen nicht
  (Regressionstest `Header_ControlsDoNotOverlap`).
- **Links:** Quest-Spalte ~250 px (`HudChrome.Panel`) mit verfolgten Aufgaben.
- **Mitte:** `MapFrame` 640×640, heldenzentriert, Zonenbild + Marker-Layer.
- **Rechts:** Filter-Legende mit fünf Toggle-Zeilen (Ikone + Name + Zustand),
  darunter „Du“-Zeile und Hinweis. Kategorien: Quests, Gruppe, Wegpunkte
  (Tore + Wegschreine), Personen (NPCs), Elitegegner. Held immer sichtbar.
- **Zoom 75–150 %** über Mausrad + Knöpfe, heldenzentriert; 150 % deckt die
  Frame ab, 75 % bleibt drin. Redraw-Takt 0,3 s, Bild-/Quest-Text nur bei
  Änderung, Icon-Pool skippt unveränderte Eigenschaften (Perf).
- `MinimapHud` bleibt in beiden Modi ausgeblendet; der Welt-Quest-Tracker wird
  unter der offenen Karte ausgeblendet. Loc-Keys `map.mode.*`/`map.filter.*`
  (de+en).

## Nicht-Ziele (bleiben so)

- Keine selbst gesetzten Kartenmarkierungen per Klick.
- Keine neue Kartengeometrie / kein Fog-of-War über das Zonenbild hinaus.
