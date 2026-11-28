---
goal: cinderreach-inventory-redesign
slug: cinderreach-inventory-redesign
branch: feature/cinderreach-inventory-redesign
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO03, G-DIABLO05]
priority: P10 (nach cinderreach-hud-redesign gelandet als 7176e5a)
status: planned
plan_refs: [.agents/goals/cinderreach-inventory-redesign/GOAL.md]
---

# Plan: cinderreach-inventory-redesign — Inventar nach Canvas-Artboard „Inventar (I)“

Pfad: feature, ein Branch, fünf Partials (P1–P5) in Abhängigkeitsreihenfolge. Abschluss per
`python3 ~/scripts/agent-workspace.py land` (`land: ff`). Vorlage: Canvas
https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard `Inventory.dc.html` (1920×1080).
Artboard nicht abrufbar — GOAL.md-Objective + `HudChrome`/`DiabloUIFactory` sind die Vorlage.

## Goal

Siehe [GOAL.md](../../goals/cinderreach-inventory-redesign/GOAL.md), Erfolgskriterien 1–6, Entscheidungen D1–D4.

## Ausgangslage (HEAD `7176e5a`, 2026-11-28)

- `DiabloInventoryUI.Layout.cs` (~580 Zeilen): Canvas `InventoryCanvas`, Panel 1520×840 rechts verankert
  `(-18,0)`, `MakeHeader`, 3 Tabs, Seiten 690 px. `BuildInventoryPage`: Character 456 / Parameters 416 / Bag flex.
  `BuildCharacterColumn`: HeroCard 58, Paperdoll 470 (`PaperdollStage` 272×400 + `HeroPaperdollPreview.Bind`,
  6× `BuildEquipSlot` 92×92 an `PaperdollSlotPosition`, `EquipHint`, SummaryBox 128 mit `StatsSummary`).
  `BuildBagColumn`: BagTopRow (`BagCount` + `FlaskQuickButton`), `BagGrid` GridLayoutGroup 5 Spalten 104×92
  (`BagList`), `BagTooltipBox` 152 (`bagDetailText`), `BagActions` (Equip/Discard), `Notice`.
  `MakeBagCell`: `BagButton{n}`/`BagEmpty`, Icon 50×50, `ItemLabel`, `ClassLock` „!“, `UpgradeMark` aus
  `ItemCompare.Score` (▲/▼ grün/rot, existiert bereits), leere Zellen `interactable = false`.
  `BuildParametersColumn` + `MakeParamScroll` + `RebuildParamRows` + `MakeParamRow` (Test-Seam für
  `InventoryStatBreakdownPlayTests`). `BuildStatsPage`/`BuildSettingsPage`, `PaintTabs`, `MakeCard`, `Place`.
- `DiabloInventoryUI.cs`: `selectedIndex`, `CellHover` (enter: `selectedIndex`, `bagDetailText`, schwebendes
  `ItemTooltip.For(gameObject).Show(...)`; exit: `HideFor`), `EquipIndex`/`EquipById`/`DiscardById`/
  `UnequipSlot`/`DiscardIndex` (autoritativer Equip-Pfad, bleibt), Drag&Drop (`CellBeginDrag`/`CellDrag`/
  `CellEndDrag`/`CellDrop`), `RightClickBag`/`RightClickSlot`, `PaintSlotTints`.
- `Items/ItemTooltip.cs` (362 Zeilen, `ItemTooltipCanvas`, `CardWidth 320`, `For/HideFor/Show`,
  `ShiftOverride`): bleibt für Truhe/Händler/Schmied (D2). `Items/ItemCompare.cs`: `Score`/`Diff`/`UpArrow`/
  `DownArrow`, `Epsilon = 0.001f` (D4, keine neue Bewertungslogik).
- `Inventory.Capacity = 20` — wird NICHT geändert (keine Spiellogik-Änderung).
- Stilbausteine aus HUD-Goal: `HudChrome.Panel/Bar/Badge/Circle`, `DiabloUIFactory.DisplayFont` (Cinzel) /
  `BodyFont` („Cinderreach Sans“), Palette (`TabActive`/`TabInactive`, `ButtonPrimary`/`Secondary`, `TooltipBg` …).
- Tests: `InventoryPaperdollPlayTests` (Seams `BagCell(0)`, `BagButton{n}`, `tip.HoverText` mit
  `ItemCompare.UpArrow`, `IsVisible`/`IsComparing`), `InventoryStatBreakdownPlayTests`,
  `DiabloMenuTests.Menu_TabsSwitchAndBagGridFills` (assertet Zellen == `Inventory.Capacity`, dynamisch).
- Shots: `inventory_open`, `inventory_compare` (Anker `$"BagButton{bag.Count - 1}"`) existieren;
  `inventory_unique_selected` fehlt und wird ergänzt.

## File Structure

| Partial | Dateien (disjunkt) |
|---|---|
| P1 Layout | `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` |
| P2 Tooltip-Spalte | `sandbox/Assets/Scripts/DiabloInventoryUI.cs`, `sandbox/Assets/Scripts/Localization/LocCatalog.Inv.cs` |
| P3 Tests | neu `sandbox/Assets/Tests/EditMode/BagUpgradeMarkerTests.cs`, neu `sandbox/Assets/Tests/PlayMode/InventoryTooltipColumnPlayTests.cs`, `sandbox/Assets/Tests/PlayMode/InventoryPaperdollPlayTests.cs`, `sandbox/Assets/Tests/EditMode/DiabloMenuTests.cs` (nur Kommentar) |
| P4 Shots | `sandbox/Assets/Scripts/DiabloShotCheck.cs` (nur `inventory_unique_selected`-Pose) |
| P5 Verify | `docs/inventory-redesign/*`, `docs/test-runs.jsonl`, Goal-/CINDERREACH-Doku |

## P1 — Layout-Umbau (Ziel: Held / Tasche / Tooltip-Spalte)

1. `BuildInventoryPage`: drei Spalten — links Held (bestehende `BuildCharacterColumn`: HeroCard, Paperdoll,
   SummaryBox; darunter `MakeParamScroll`-Inhalt aus `RebuildParamRows` als Werteübersicht, Seam-Namen behalten),
   Mitte Tasche (`BuildBagColumn`), rechts NEUE feste Tooltip-Spalte (~360 px, `HudChrome.Panel`, Name
   `TooltipColumn`). `ParametersColumn` als eigene Mittelspalte entfällt; `BuildStatsPage`/`BuildSettingsPage`
   nur Stil (Nicht-Ziel).
2. Kopfzeile: Heldenname (DisplayFont) + Tabs + Schließen-Knopf (echter Button ≥ 44 px). Fußzeile: Gold,
   Schwierigkeit, Bedienhinweise (Loc-Keys, P2).
3. Taschen-Grid optisch 8×5 wie Artboard: 20 Zellen nutzbar (`BagButton{n}`, Seams behalten), Rest gesperrte
   Leerzellen (deaktiviert, eigener Stil, nicht klickbar). Zellennamen-/Seam-Konvention für P3 dokumentieren
   (Kommentar in `MakeBagCell`).
4. Alles Raycast-sparsam, Schriften Display/Body, Buttons ≥ 44 px.
- Verify: `PLATFORMS=EditMode TEST_FILTER=DiabloMenuTests bash scripts/run-unity-tests.sh` grün.

## P2 — Feste Tooltip-Spalte

1. `CellHover` füllt im Inventar die `TooltipColumn` (Name, Rarität, Platz, Affixe, Unique-Effekt, Vergleich
   mit Angelegtem via bestehendem Vergleichs-Summary, Aktionen Anlegen/Ablegen über `EquipIndex`/`UnequipSlot`,
   Verkaufen/Wegwerfen über `DiscardById`) statt des schwebenden `ItemTooltip`; Klick (`selectedIndex`) und
   Hover zeigen dasselbe Item. `ItemTooltip.For/Show/HideFor` für Truhe/Händler/Schmied unverändert lassen.
2. Loc-Keys in `LocCatalog.Inv.cs` für Kopf-/Fußzeile, Tooltip-Spalte, Legende, Hinweise (de+en).
3. `BagTooltipBox` (152 px) entfällt zugunsten der Spalte; `BagActions` wandern in die Spalte.
- Verify: `PLATFORMS=PlayMode TEST_FILTER=InventoryPaperdollPlayTests bash scripts/run-unity-tests.sh` grün.

## P3 — Tests (▲-Marker, Tooltip-Spalte)

1. `BagUpgradeMarkerTests` (EditMode): ▲ exakt bei Items mit positivem `ItemCompare`-Gesamtdelta, ▼/keins sonst;
   gegen `MakeBagCell`-UpgradeMark (bestehendes `score != 0`-Verhalten erst per Test bestätigen).
2. `InventoryTooltipColumnPlayTests` (PlayMode, 1920×1080): Spalte zeigt Name/Rarität/Platz/Affixe/Unique/Vergleich
   für Klick UND Hover; Aktionen laufen über autoritativen Pfad.
3. `InventoryPaperdollPlayTests` an neue Spalte anpassen (Seams aktuell halten); `DiabloMenuTests`-Kommentar
   („20 cells“) auf 8×5-Darstellung bei `Capacity = 20` korrigieren.
- Verify: gefilterte Läufe je Testklasse grün.

## P4 — Shot-Pose

1. `DiabloShotCheck.cs`: Pose `inventory_unique_selected` (Unique-Item in Tasche wählen → Tooltip-Spalte
   sichtbar, 1920×1080). `inventory_compare`-Anker an neues Grid prüfen.
- Verify: `TEST_FILTER=DiabloShotCheck`-relevanter Lauf bzw. Shotcheck-Dry-Run nach P5-Build.

## P5 — Verifikation und Abschluss

1. `bash scripts/run-unity-tests.sh` (EditMode + PlayMode) grün, Zeilen in `docs/test-runs.jsonl`.
2. `python3 scripts/check-meta.py`, `python3 -m pytest scripts -q`, KayKit-Verify aus `repo.yaml`.
3. Windows-Build + Stage WIP + `SHOTCHECK_SIZE=1920x1080 bash scripts/shotcheck-windows.sh`; Shots
   `inventory_unique_selected`, `inventory_compare` nach `docs/inventory-redesign/`, Vergleichsbild
   Shot ↔ Artboard daneben. `NEW_HERO FAIL` ggf. per main-Baseline gegenprüfen (vorbestehend).
4. `PROGRESS.md` 100 % mit Belegen K1–K6, `## Offene Punkte: Keine.`; Stand in CINDERREACH.md (P10-Zeile);
   generierte `AddressableAssetsData/Windows*/link.xml` + `*Locomotion.controller`-Re-Serialisierungen
   vor Commit reverten/löschen, nie stagen.
5. `python3 ~/scripts/agent-workspace.py land` (`land: ff`); `test-runs.jsonl`-Konflikt per Union lösen.
