# Progress: cinderreach-vendor-sell-all

Prozent: 100 %

## Erledigt
- 2026-11-28: Goal angelegt nach /setgoal-Runde.
- 2026-11-28: Kriterium 1 — Beleg: `VendorSellAllTests.SellAllRares_SellsOnlyRares_GoldIsSumOfSinglePrices` (EditMode, Gold = Summe der Einzelpreise, Unique/Ausrüstung bleiben), `VendorSellAllPlayTests.Button_ConfirmShowsSum_ThenSellsAllRares`, `Cancel_KeepsEverything_NoRares_ShowsNotice` (PlayMode, Button → Bestätigung → Verkauf). Commits `9970f2c`, `ddd96bc`.
- 2026-11-28: Kriterium 2 — Beleg: `NetworkItemLifecycleTests.SellAllRares_IsHostValidatedAndSellsOnce` (`ItemAction.SellRares`: außer Reichweite abgelehnt, einmal verkauft, zweiter Aufruf ohne Wirkung).
- 2026-11-28: Kriterium 3 — Beleg: `bash scripts/run-unity-tests.sh` auf `ddd96bc`: EditMode 797/797, PlayMode 453/453; Windows-Shotcheck 1920x1080 Exit 0, `docs/vendor-sell-all/79_vendor_sell_all_confirm.png` („2 Rares für 44 Gold verkaufen?“, Verkaufen/Abbrechen, Schließen im Rahmen) und `78_s5_vendor.png`.

## Nächste Schritte
- Keine.

## Offene Punkte
- Keine.
