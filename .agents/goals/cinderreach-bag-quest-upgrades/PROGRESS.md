# Progress: cinderreach-bag-quest-upgrades

Prozent: 100 %

## Erledigt
- 2026-11-28: Goal angelegt nach /setgoal-Runde.
- 2026-11-28: Kriterium 1 — Beleg: n1, n9 (Akt I), n4 (Akt II), n6 (Akt III) tragen `bagUpgrade`; `BagQuestUpgradeTests.Catalog_FourSideQuestsAcrossActs_CarryTheBagReward`, `Slots_GrowBy20_CapAt100_NeverShrink`, `BagQuestsDone_CountsOnlyFinishedBagQuests` grün; Spielpfad `Akt1OfenTests` (quest:n1 → 40 Plätze über `StoryDirector.SyncBagSlots`). Commits `d0ac9dc`, `dc50bd0`.
- 2026-11-28: Kriterium 2 — Beleg: `BagQuestUpgradeTests.Save_V16MigratesTo20Slots_V17RoundTripKeepsSlotsAndItems` (v16 → v17 mit 20 Plätzen, 60 Plätze + 45 Items überleben Save/Load), `NetworkSnapshot_CarriesBagSlots` (Koop-Snapshot trägt `bagSlots`, alte Clients senden 0 → bleibt), `SaveMigrationTests.CurrentVersion_IsSeventeen`.
- 2026-11-28: Kriterium 3 — Beleg: `BagUpgradeMarkerTests.LockedFramesAreNotCells` (20 nutzbar + 80 gesperrt, nach Erweiterung 60 + 40); Windows-Shotcheck 1920x1080 `docs/bag-upgrades/77_bag_slots_100.png` (10×10 voll nutzbar, Toast „Tasche erweitert“) und `75_s5_inventory_details.png`.
- 2026-11-28: Kriterium 4 — Beleg: `python3 scripts/check-quest-coverage.py` Exit 0; `bash scripts/run-unity-tests.sh`: EditMode 796/796 auf `dc50bd0`, PlayMode 450/450 auf `903702b` (`docs/test-runs.jsonl`).

## Entscheidungen
- 2026-11-28: Linksklick auf Taschenzellen legt nicht mehr an (Rest-`onClick` aus der Zeit vor dem Cursor-Item kollidierte mit Goal 2); Tastatur/Gamepad-Submit legt weiter an (`InventoryCell.OnSubmit`).

## Nächste Schritte
- Keine.

## Offene Punkte
- Keine.
