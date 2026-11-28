# Progress: cinderreach-inventory-redesign

Prozent: 100 %

## Erledigt

- 2026-11-27: Goal angelegt (`cae286b`), Plan gestagt (`147a1ef`, `.agents/plans/cinderreach-inventory-redesign/tasks.md`, P1–P5).
- 2026-11-28: Umsetzung auf Branch `feature/cinderreach-inventory-redesign`:
  - `deeea3d` feat(inventory): Dreispalten-Layout (Held 456 / Tasche flex / Tooltip 360),
    HeaderRow (Heldenname DisplayFont, Tabs, Close ≥ 44 px), FooterRow (Gold, Schwierigkeit,
    Hinweise), Taschen-Grid optisch 8×5 (20 nutzbare Zellen + gesperrte `BagLocked`-Rahmen,
    `Capacity = 20` unverändert), ParametersColumn entfernt (Breakdown lebt auf Stats-Tab).
    Neue `inv.tc.*`-/`inv.footer.*`-/`inv.notice.sold|nosell`-Keys.
  - `142718e` feat(inventory): Tooltip-Spalte füllen (Name/Rarität/Platz/Affixe/Unique-Effekt/
    Vergleich/Aktionen), `CellHover` ohne Floater (Truhe/Händler/Schmied unberührt), Verkaufen
    über `VendorStock.TrySell`, Anlegen/Ablegen-Kontext, Footer-/Gold-Refresh.
  - `a37dda9` test(inventory): `BagUpgradeMarkerTests` + `InventoryTooltipColumnPlayTests`,
    Fix `RebuildBag` (Kinder-Rückwärts-Löschung), `UpgradeMarkScore`-Hülle.
  - `13b6aab` + `ad56d19` test(inventory): Shot-Posen (Spalten-Vergleich statt Floater, neu
    `inventory_unique_selected` mit Volltaschen-Schutz), StatBreakdown-Pin auf Stats-Tab.
  - `c519a18` fix(inventory): Panel-Anker -18 → -280 px (Minimap/Tracker-Überlappung beseitigt).
- Belege (K1–K6):
  - K1: `InventoryTooltipColumnPlayTests` (Hover/Klick/Verkauf/Unique-Effekt, Buttons ≥ 44 px)
    + `InventoryPaperdollPlayTests.Hover_FillsTooltipColumnWithComparison` grün.
  - K2: `BagUpgradeMarkerTests` grün (Score-Regel, Grid-▲ nur bei positivem Delta, ▼ sonst,
    Locked-Rahmen keine Zellen).
  - K3: `InventoryPaperdollPlayTests` grün (autoritativer Equip-Pfad: Equip/Unequip/Swap/Drag).
  - K4: Truhe/Händler/Schmied-Tooltip-Tests grün (ungeänderte Floater-Pfade; Voll-Suite grün).
  - K5: `bash scripts/run-unity-tests.sh` voll grün (EditMode 767/767, PlayMode 422/422),
    `python3 scripts/check-meta.py` OK, `pytest scripts` 64 passed, `KayKitAssets.Verify` Exit 0.
  - K6: Windows-Shotcheck 1920×1080: `docs/inventory-redesign/inventory_compare.jpg`,
    `inventory_unique_selected.jpg` + `compare.jpg` (Side-by-Side; Canvas-Artboard ist
    außerhalb von Claude.ai nicht abrufbar). Hinweis: `NEW_HERO FAIL` tritt auch auf `main`
    auf (HUD-Goal belegt) — vorbestehend, nicht vom Branch verursacht.
- 2026-11-28: Stand in `.agents/goals/CINDERREACH.md` (P10-Zeile) eingearbeitet.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
