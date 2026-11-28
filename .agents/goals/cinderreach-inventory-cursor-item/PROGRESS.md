# Progress: cinderreach-inventory-cursor-item

Prozent: 100 %

## Erledigt
- 2026-11-28: Goal angelegt nach /setgoal-Runde.
- 2026-11-28: Kriterium 1 — Beleg: `InventoryCursorItemPlayTests.Click_LiftsItem_GhostFollows_ClickEmptyCellMovesToEnd`, `ClickOccupiedCell_Swaps_DisplacedItemRidesCursor` grün (Commit `cf8b098`).
- 2026-11-28: Kriterium 2 — Beleg: `InventoryCursorItemPlayTests.HeldBagItem_OnMatchingSlot_Equips_WrongSlotRejects`, `HeldBagItem_OnHero_EquipsIntoOwnSlot_SwappedItemRidesCursor`, `WornItem_LiftAndClickBag_Unequips`; Netzpfad `NetworkItemLifecycleTests.CursorItem_EquipsThroughTheAuthoritativeItemPath` (Host-Spieler, `RequestItemAction`, `visualArmor` gesetzt). Clients laufen über denselben `EquipIndex`→`RequestItemAction`-Pfad.
- 2026-11-28: Kriterium 3 — Beleg: `InventoryCursorItemPlayTests.ClickOutside_AndClosing_PutItemBack_NothingLost`, `OutsideBackdrop_OnlyActiveWhileHolding` (Item bleibt bis zur Ablage im Ursprungsplatz).
- 2026-11-28: Kriterium 4 — Beleg: `bash scripts/run-unity-tests.sh` auf `cd57f00`: EditMode 791/791, PlayMode 450/450; Windows-Shotcheck 1920x1080 Exit 0, Shot `docs/inventory-cursor/76_inventory_cursor_item.png` (Leinenkittel am Cursor).

## Nächste Schritte
- Keine.

## Offene Punkte
- Keine.
