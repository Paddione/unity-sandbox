# Progress: cinderreach-inventory-ui-layout-split

Prozent: 33 %

## Erledigt

- 2026-11-26: Aus Code-Analyzer-Befund `CA-03` und `/setgoal`-Klärungsrunde als Goal auf Basis `main` (`5bd41d3`) angelegt.
- 2026-11-27: Stufenplan [tasks.md](../../plans/cinderreach-inventory-ui-layout-split/tasks.md) angelegt (3 Stufen, Basis `8e59d00`, Ist: 1.033 Zeilen, 14 Layout-Methoden mit Zeilen belegt); `plan_refs` in GOAL.md ergänzt. Prozent bleibt 0 %.

- 2026-11-27: Plan gegen `1ff1b95` geprüft (Code unverändert, 1.033 Zeilen) und als P3 priorisiert (unabhängig, parallel zur Story-Kette möglich). Prozent bleibt 0 %.

- 2026-11-27: S1-Baseline auf unverändertem `main` (`8e781a7`, Worktree `feature/cinderreach-inventory-ui-layout-split-s1`) eingefroren — 6/6 Suites grün, 0 rot: EditMode `ItemCompareTests` 6/0, `DiabloMenuTests` 9/0, `DiabloHdTests` 7/0 (22/22); PlayMode `InventoryPaperdollPlayTests` 4/0, `GearSlotsPlayTests` 5/0, `DiabloMenuPlayTests` 8/0 (17/17). Kriterium-2-API 16/16 Signaturen bestätigt (`IsOpen`, `CurrentTab`, `Paperdoll`, `SetOpen`, `ShowTab`, `EquipIndex`, `EquipById`, `DiscardById`, `UnequipSlot`, `DiscardIndex`, `DropBagOnSlot`, `DropSlotOnBag`, `RightClickBag`, `RightClickSlot`, `PaperdollSlotPosition`, `Notice`); 14/14 Layout-Methoden an den Plan-Zeilen in `DiabloInventoryUI.cs` (1.033 Zeilen, Klasse noch nicht `partial`) bestätigt. Keine roten Vorbefunde — nichts, was dem Split angelastet werden könnte. (Hinweis: Paket war auf `6bf15a6` ausgestellt; Worktree wurde extern gelöscht und auf Folge-`main` `8e781a7` neu angelegt — `DiabloInventoryUI.cs` und `Tests/` sind zwischen beiden SHAs identisch.)

- 2026-11-27 (S1-Land): Baseline als `396980a` auf `main` gelandet (ff, Push durch, Worktree aufgeräumt). Prozent jetzt 33 %.

## Nächste Schritte

- S2: `partial`-Split nach `DiabloInventoryUI.Layout.cs` (14 Methoden, `.meta` per Unity-Import), danach S3 (Vollverifikation + Sichtprüfung).

## Offene Punkte

- S2-Split und S3-Vollverifikation stehen aus; S1-Baseline (39/39) ist der Vergleichsmaßstab.
