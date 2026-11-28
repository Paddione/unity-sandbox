# Goal: cinderreach-bag-quest-upgrades

health_goals: [G-DIABLO03, G-QUESTCOVER01]

## Objective
Vier Nebenquests belohnen je +20 Taschenplätze, von 20 auf höchstens 100. Die Erweiterung wird gespeichert, im Koop pro Held geführt und im Inventar sichtbar.

## Ist-Befunde
- B1 `Inventory.Capacity` ist `const 20` (`Items/Inventory.cs:12`); `DiabloInventoryUI` zeigt 8×5 mit gesperrten `BagLocked`-Rahmen; `ValidateList` kappt Joins/Saves auf `Capacity`.
- B2 Save-Version aktuell v16; Nebenquests Akt I: n1–n3, n8–n11; Akt II: n4, n5, n12–n15; Akt III: n6, n7 (`ActBossGate`).
- B3 Quest-Abdeckung ist über `scripts/check-quest-coverage.py` / G-QUESTCOVER01 gesperrt (jede Belohnung braucht Testfall).

## Entscheidungen
- D0 Modus: Anlegen + planen + umsetzen in der Reihenfolge loot-click-filter → inventory-cursor-item → bag-quest-upgrades → vendor-sell-all → town-npc-identity (Eigner, /setgoal 2026-11-28).
- D1 Belohnungen an vier bestehenden Nebenquests, über die Akte verteilt (Eigner: „have it on sidequests“, 2026-11-28); konkrete Auswahl im Plan, Vorschlag n1, n9, n4, n6.
- D2 Kapazität wird pro Held gespeichert (Save v17, Migration: alte Saves 20 + 20 je bereits abgeschlossener Belohnungsquest). Raster 10×10, nicht freigeschaltete Felder als `BagLocked`.
- D3 Belohnung einmalig, Anzeige als Toast und im Questtext.

## Erfolgskriterien
1. Abschluss jeder der vier Quests erhöht die Kapazität um 20, Obergrenze 100, keine Doppelvergabe — Nachweis: `QuestRuleTests`/`QuestJourneyTests`-Fälle.
2. Save v17 speichert die Kapazität, v16-Saves migrieren korrekt, Koop-Join übernimmt Kapazität des Helden — Nachweis: EditMode-Save-Tests inkl. Migration.
3. Inventar zeigt freigeschaltete und gesperrte Felder passend zur Kapazität — Nachweis: PlayMode-Test zählt BagButton/BagEmpty vs. BagLocked; Windows-Shot bei 20 und 100 unter `docs/bag-upgrades/`.
4. Quest-Abdeckung und Voll-Suiten grün — Nachweis: `python3 scripts/check-quest-coverage.py` 0, `bash scripts/run-unity-tests.sh` failed 0.

## Nicht-Ziele
- Keine neuen Quests oder NPCs.
- Truhenkapazität (40) bleibt.

## Abhängigkeiten
- cinderreach-inventory-cursor-item (gleiches Taschenraster).
