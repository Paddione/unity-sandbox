# Progress: cinderreach-loot-click-filter

Prozent: 100 %

## Erledigt
- 2026-11-28: Goal angelegt nach /setgoal-Runde.
- 2026-11-28: Kriterium 1 — Beleg: `LootClickPlayTests.WalkingOverItem_DoesNotPickUp`, `ClickOrder_InReach_PicksUpAtOnce`, `ClickOrder_OutOfReach_WalksThenPicksUp`, `SymbolClick_FindsDrop_PlateClick_FindsDrop` grün (Commits `1dba011`, `af3fc6f`).
- 2026-11-28: Kriterium 2 — Beleg: `LootClickPlayTests.KeyGate_RequiresHeldKeyOnlyWhenEnabled`, `KeyBinding_PersistsAndRejectsEscape` grün.
- 2026-11-28: Kriterium 3 — Beleg: `LootClickPlayTests.LootFilter_UniqueOnly_HidesRarePlates_KeepsUnique`, `FilteredItem_StaysClickableBySymbol`, `DiabloMenuPlayTests.LootLabels_NameNearbyDropsInRarityColor` grün; Shot `docs/loot-click/74_loot_filter_unique.png` (Rare-Schilder weg, Unique bleibt).
- 2026-11-28: Kriterium 4 — Beleg: `EconomyLoopTests.Kill_AwardsGoldToKiller_AtNormalRate` (keine GoldPile, Wallet = vergebenes Gold), `KillGold_AppliesGoldFind`, `BossLoopTests.Boss_DeathOpensSessionComplete`; `LootNetRelay`-Goldpfad entfernt, Koop-Gutschrift über `GrantPickupGold`.
- 2026-11-28: Kriterium 5 — Beleg: `bash scripts/run-unity-tests.sh` auf `af3fc6f`: EditMode 791/791, PlayMode 442/442 (`docs/test-runs.jsonl`); Windows-Shotcheck 1920x1080 Exit 0, Shots `docs/loot-click/` (Schilder immer an, Filter, Einstellungszeile „Schilder/Filter/Nur mit Taste/Taste: Alt“).

## Nächste Schritte
- Keine.

## Offene Punkte
- Keine.
