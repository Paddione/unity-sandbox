# Goal: cinderreach-vendor-sell-all

health_goals: [G-DIABLO03]

## Objective
Das Händlerfenster bekommt einen Button „Alle Rares verkaufen“. Er verkauft nach Bestätigung mit Goldsumme alle Rare-Items der Tasche, angelegte Ausrüstung und Uniques bleiben.

## Ist-Befunde
- B1 Verkauf einzeln über `VendorStock.TrySell` (25 % Wert), host-validiert im Koop.
- B2 Blaue Items existieren nicht (siehe loot-click-filter B3), „blau/rare“ entspricht damit allen Rares.

## Entscheidungen
- D0 Modus: Anlegen + planen + umsetzen in der Reihenfolge loot-click-filter → inventory-cursor-item → bag-quest-upgrades → vendor-sell-all → town-npc-identity (Eigner, /setgoal 2026-11-28).
- D1 Bestätigungsdialog zeigt Anzahl und Goldsumme (Eigenentscheidung, schützt vor Fehlklick).
- D2 Betrifft nur die Tasche, nicht Truhe und angelegte Ausrüstung; Magic würde, falls je vorhanden, mitverkauft.

## Erfolgskriterien
1. Button verkauft alle Rares der Tasche, Gold steigt um die Summe der Einzelpreise, Uniques und Ausrüstung bleiben — Nachweis: EditMode-Test auf `VendorStock` + PlayMode-Test des Buttons.
2. Koop-Client: Sammelverkauf host-validiert, kein Doppelverkauf — Nachweis: PlayMode-Netztest.
3. Voll-Suiten grün, Windows-Shot des Händlerfensters mit Button und Dialog — Nachweis: `bash scripts/run-unity-tests.sh` failed 0, Shot unter `docs/vendor-sell-all/`.

## Nicht-Ziele
- Keine Rückkauf-Funktion.
- Keine Preisänderung.

## Abhängigkeiten
- cinderreach-bag-quest-upgrades (größere Tasche, gleiches Fenster).
