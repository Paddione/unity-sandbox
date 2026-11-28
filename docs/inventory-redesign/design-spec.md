# Design-Spec: Inventar (I) (lokale Ablage des Canvas-Artboards)

Quelle: Canvas https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard
„Inventar (I)“ (`Inventory.dc.html`, 1920×1080). Das Artboard ist außerhalb von
Claude.ai nicht abrufbar — **diese Datei ist die lokale Source of Truth** für
das Design. Umsetzung: Goal `cinderreach-inventory-redesign` (100 %),
Belege `InventoryTooltipColumnPlayTests`, `BagUpgradeMarkerTests`,
`InventoryPaperdollPlayTests`, Shots `inventory_unique_selected.jpg`,
`inventory_compare.jpg`, `compare.jpg` (alle in diesem Ordner). Stil/Farben/
Fonts siehe [HUD-Spec](../hud-redesign/design-spec.md) (D3).

## Layout (drei Spalten)

- **Kopfzeile:** Heldenname in Cinzel, Tabs, Schließen-Knopf (≥ 44 px).
- **Links:** drehbarer 3D-Held (keine Silhouette, D1) mit sechs Plätzen und
  Werteübersicht.
- **Mitte:** Tasche mit Raritätsflächen, Stapelzahl, ▲-Markierung für bessere
  Items (per `ItemCompare` gegen den angelegten Platz, keine neue
  Bewertungslogik, D4) und Legende. Grid optisch 8×5, 20 Zellen nutzbar, Rest
  gesperrte Rahmen; Kapazität 20 unverändert.
- **Rechts:** feste Tooltip-Spalte — zeigt gewähltes oder überfahrenes Item mit
  Name, Rarität, Platz, Affixen, Unique-Effekt, Vergleich mit dem Angelegten
  sowie Aktionen Anlegen/Ablegen, Verkaufen, Wegwerfen (über den autoritativen
  Equip-Pfad). Truhe, Händler und Schmied behalten den schwebenden Tooltip (D2).
- **Fußzeile:** Gold, Schwierigkeit, Bedienhinweise.

## Nicht-Ziele (bleiben so)

- Inhalte der Tabs „Werte“ und „Einstellungen“ über den Stil hinaus.
- Keine neuen Item-Aktionen oder Sortierlogik.
