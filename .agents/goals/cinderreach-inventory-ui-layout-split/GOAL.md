---
goal: cinderreach-inventory-ui-layout-split
slug: cinderreach-inventory-ui-layout-split
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-inventory-ui-layout-split/tasks.md]
---

# Goal: cinderreach-inventory-ui-layout-split

## Objective

`sandbox/Assets/Scripts/DiabloInventoryUI.cs` (1.014 Zeilen, 53 Methoden, 19 Commits inkl. `5bd41d3`) analog zum bestehenden `DiabloNetworkPlayer.*.cs`-Muster sauber trennen, sodass der 3-Tab-/Paperdoll-UGUI-Layoutbau in einer dedizierten `partial`-Datei (`sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs`) liegt und von Drag&Drop-Zustand sowie Solo-/RPC-Item-Aktionen entkoppelt ist. Ausgangslage: [CINDERREACH.md](../CINDERREACH.md).

## Erfolgskriterien

1. Die UGUI-Aufbau- und Styling-Methoden (`BuildUI`, `MakePage`, `BuildInventoryPage`, `BuildCharacterColumn`, `BuildEquipSlot`, `BuildBagColumn`, `BuildStatsPage`, `BuildSettingsPage`, `MakeBagCell`, `MakeCard`, `Place`, `PaintTabs`, `PaintEquipIcon`, `MakeTabButton`) liegen in `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` (mit `.meta`), während `sandbox/Assets/Scripts/DiabloInventoryUI.cs` Fokus auf Lebenszyklus, Drag-State und Item-Aktionen behält.
2. Alle öffentlichen Schnittstellen und Hierarchienamen (`IsOpen`, `CurrentTab`, `Paperdoll`, `SetOpen`, `ShowTab`, `EquipIndex`, `EquipById`, `DiscardById`, `UnequipSlot`, `DiscardIndex`, `DropBagOnSlot`, `DropSlotOnBag`, `RightClickBag`, `RightClickSlot`, `PaperdollSlotPosition`, `Notice`) bleiben ohne Bruch erhalten.
3. Alle 6 direkt betroffenen Suites (`InventoryPaperdollPlayTests`, `GearSlotsPlayTests`, `ItemCompareTests`, `DiabloMenuPlayTests`, `DiabloMenuTests`, `DiabloHdTests`) sowie vollständige EditMode/PlayMode-Suiten (`bash scripts/run-unity-tests.sh`) und `python3 scripts/check-meta.py` bestehen.

## Scope

- `sandbox/Assets/Scripts/DiabloInventoryUI.cs`
- `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` (inkl. `.meta`)

## Nicht-Ziele

- Keine visuelle Neugestaltung und keine Änderungen an `ItemTooltip`, `HeroPaperdollPreview`, `sandbox/Assets/Scripts/DiabloNetworkPlayer.Items.cs` oder `DiabloUIFactory.CloseSecondaryPanels`.

## Abhängigkeiten

- `unity-sandbox main` (Basis `5bd41d3`).
