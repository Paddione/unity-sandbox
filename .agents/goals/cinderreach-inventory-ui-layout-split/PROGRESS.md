# Progress: cinderreach-inventory-ui-layout-split

Prozent: 100 %

## Erledigt

- 2026-11-26: Aus Code-Analyzer-Befund `CA-03` und `/setgoal`-Klärungsrunde als Goal auf Basis `main` (`5bd41d3`) angelegt.
- 2026-11-27: Stufenplan [tasks.md](../../plans/cinderreach-inventory-ui-layout-split/tasks.md) angelegt (3 Stufen, Basis `8e59d00`, Ist: 1.033 Zeilen, 14 Layout-Methoden mit Zeilen belegt); `plan_refs` in GOAL.md ergänzt. Prozent bleibt 0 %.

- 2026-11-27: Plan gegen `1ff1b95` geprüft (Code unverändert, 1.033 Zeilen) und als P3 priorisiert (unabhängig, parallel zur Story-Kette möglich). Prozent bleibt 0 %.

- 2026-11-27: S1-Baseline auf unverändertem `main` (`8e781a7`, Worktree `feature/cinderreach-inventory-ui-layout-split-s1`) eingefroren — 6/6 Suites grün, 0 rot: EditMode `ItemCompareTests` 6/0, `DiabloMenuTests` 9/0, `DiabloHdTests` 7/0 (22/22); PlayMode `InventoryPaperdollPlayTests` 4/0, `GearSlotsPlayTests` 5/0, `DiabloMenuPlayTests` 8/0 (17/17). Kriterium-2-API 16/16 Signaturen bestätigt (`IsOpen`, `CurrentTab`, `Paperdoll`, `SetOpen`, `ShowTab`, `EquipIndex`, `EquipById`, `DiscardById`, `UnequipSlot`, `DiscardIndex`, `DropBagOnSlot`, `DropSlotOnBag`, `RightClickBag`, `RightClickSlot`, `PaperdollSlotPosition`, `Notice`); 14/14 Layout-Methoden an den Plan-Zeilen in `DiabloInventoryUI.cs` (1.033 Zeilen, Klasse noch nicht `partial`) bestätigt. Keine roten Vorbefunde — nichts, was dem Split angelastet werden könnte. (Hinweis: Paket war auf `6bf15a6` ausgestellt; Worktree wurde extern gelöscht und auf Folge-`main` `8e781a7` neu angelegt — `DiabloInventoryUI.cs` und Ordner `sandbox/Assets/Tests` sind zwischen beiden SHAs identisch.)

- 2026-11-27 (S1-Land): Baseline als `396980a` auf `main` gelandet (ff, Push durch, Worktree aufgeräumt). Prozent jetzt 33 %.
- 2026-11-27 (S2 split + S3 proof, Worktree `feature/cinderreach-inventory-ui-layout-split-s2`):
  - S2 Partial-Split: `DiabloInventoryUI` auf `public sealed partial class` umgestellt; alle 14 Layout-Methoden (`BuildUI`, `MakePage`, `BuildInventoryPage`, `BuildCharacterColumn`, `BuildEquipSlot`, `MakeCard`, `Place`, `BuildBagColumn`, `BuildStatsPage`, `BuildSettingsPage`, `PaintTabs`, `PaintEquipIcon`, `MakeTabButton`, `MakeBagCell`) unverändert nach `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` verschoben. Zusätzlich zog der rein von `BuildInventoryPage` genutzte `private static`-Helfer `MakeColumn` mit um (keine anderen Aufrufer in `DiabloInventoryUI.cs`). `PaperdollSlotPosition`, alle 16 öffentlichen APIs (`IsOpen`, `CurrentTab`, `Paperdoll`, `SetOpen`, `ShowTab`, `EquipIndex`, `EquipById`, `DiscardById`, `UnequipSlot`, `DiscardIndex`, `DropBagOnSlot`, `DropSlotOnBag`, `RightClickBag`, `RightClickSlot`, `PaperdollSlotPosition`, `Notice`), `StartNewRun`, `DifficultyText`, `ShortLabel` und `DiabloMenuItemExt` blieben in `DiabloInventoryUI.cs`.
  - Zeilenzahlen vorher/nachher: `sandbox/Assets/Scripts/DiabloInventoryUI.cs` 1.033 → 611; `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` 432 (neu, `.meta` via Unity-Import erzeugt, GUID `4a8b7f6109c213c7b8f568a2f4192557`).
  - S2-Vergleich gegen S1-Baseline (ohne Teständerungen): `python3 scripts/check-meta.py` OK; EditMode (`ItemCompareTests;DiabloMenuTests;DiabloHdTests`) **22/22**; PlayMode (`InventoryPaperdollPlayTests;GearSlotsPlayTests;DiabloMenuPlayTests`) **17/17** — zusammen **39/39** grün.
  - S3-Vollverifikation: `bash scripts/run-unity-tests.sh` komplett grün — EditMode **674/674** (45,8 s), PlayMode **383/383** (160,8 s); `KayKitAssets.Verify` (`repo.yaml:verify`) Exit 0; `node ~/scripts/goal-lint.mjs --repo .` und `python3 ~/scripts/agent-workspace.py check` grün.
  - Sichtprüfung (`-shotcheck` auf Linux-Player-Build, `SHOT_RC=0`): `23_inventory_open.png`, `24_inventory_compare.png`, `25_bag_open.png`, `26_settings_open.png` gegen Ordner `cinderreach-inventory-shots` unter `docs` verglichen — alle 3 Tabs, Live-3D-Paperdoll mit 6 Plätzen, 20er-Taschenraster (5×4), Vergleichs-Tooltip mit ▲/▼ und Einstellungs-Tab rendern unverändert sauber.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
