# Goal: cinderreach-inventory-redesign

health_goals: [G-DIABLO01, G-DIABLO03, G-DIABLO05]
plan_refs: [.agents/plans/cinderreach-inventory-redesign/tasks.md]

Ausgangslage: [`.agents/goals/CINDERREACH.md`](../CINDERREACH.md). Design-Vorlage: Canvas
https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard „Inventar (I)“ (`Inventory.dc.html`).

## Objective
Das Inventar (I/B) sieht aus wie das Artboard „Inventar (I)“: Kopfzeile mit Heldenname, Tabs und Schließen-Knopf; links
der drehbare 3D-Held mit sechs Plätzen und einer Werteübersicht; Mitte die Tasche mit Raritätsflächen, Stapelzahl,
▲-Markierung für bessere Items und Legende; rechts eine feste Tooltip-Spalte mit Affixen, Unique-Effekt, Vergleich mit
dem Angelegten und Aktionen Anlegen/Ablegen, Verkaufen, Wegwerfen; Fußzeile mit Gold, Schwierigkeit und Bedienhinweisen.

## Ist-Befunde
- B1 Panel 1520×840 rechts verankert in `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs`, Tabs `inv.tab.inventory|stats|settings`.
- B2 3D-Held über `Menu/HeroPaperdollPreview.cs` (RenderTexture, drehbar); sechs Plätze in zwei Spalten.
- B3 Tooltip schwebend in `Items/ItemTooltip.cs`, Vergleich über `ItemCompare` (▲/▼ mit Delta), auch für Truhe, Händler, Schmied.
- B4 Bestehende Tests: `InventoryPaperdollPlayTests`, `InventoryStatBreakdownPlayTests`.

## Entscheidungen
- D1 3D-Held bleibt statt Silhouette (Eigner, 2026-11-27).
- D2 Im Inventar wird der Tooltip eine feste rechte Spalte, die das gewählte oder überfahrene Item zeigt; Truhe, Händler und Schmied behalten den schwebenden Tooltip (Eigner, 2026-11-27).
- D3 Fonts, Panelrahmen und Farben aus `cinderreach-hud-redesign` wiederverwenden.
- D4 ▲ in der Tasche nutzt `ItemCompare` gegen den angelegten Platz; keine neue Bewertungslogik.

## Erfolgskriterien
1. Feste Tooltip-Spalte zeigt Name, Rarität, Platz, Affixe, Unique-Effekt und Vergleich für Klick und Hover — Nachweis: PlayMode-Test `InventoryTooltipColumnPlayTests`.
2. ▲-Markierung genau bei Items mit positivem Gesamtdelta — Nachweis: EditMode-Test `BagUpgradeMarkerTests`.
3. Anlegen/Ablegen/Verkaufen/Wegwerfen laufen über den autoritativen Equip-Pfad — Nachweis: bestehende und erweiterte `InventoryPaperdollPlayTests` grün.
4. Truhe/Händler/Schmied unverändert schwebend — Nachweis: bestehende Tooltip-Tests grün.
5. Voll-Suiten und Meta grün — Nachweis: `bash scripts/run-unity-tests.sh`, `python3 scripts/check-meta.py`.
6. Windows-Shots `inventory_unique_selected`, `inventory_compare` zeigen das neue Layout — Nachweis: Shotcheck 1920×1080, Vergleichsbild unter `docs/inventory-redesign/`.

## Nicht-Ziele
- Inhalte der Tabs „Werte“ und „Einstellungen“ über den Stil hinaus.
- Neue Item-Aktionen oder Sortierlogik.

## Abhängigkeiten
- `cinderreach-hud-redesign` (Fonts und Stilbausteine).
