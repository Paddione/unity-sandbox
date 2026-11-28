# Goal: cinderreach-inventory-cursor-item

health_goals: [G-DIABLO03]

## Objective
Bei offenem Inventar hebt ein Linksklick auf ein Item es an den Mauszeiger. Ein weiterer Klick legt es auf einen Taschenplatz, tauscht mit dem dortigen Item oder legt es an einem passenden Ausrüstungsplatz an.

## Ist-Befunde
- B1 `DiabloInventoryUI` (+ `.Layout.cs`) kennt Drag & Drop Tasche ↔ Platz/Held und Rechtsklick-Tausch über den autoritativen Equip-Pfad.
- B2 Tooltip-Spalte rechts mit Vergleich/Aktionen (inventory-redesign, `d5e7731`).

## Entscheidungen
- D0 Modus: Anlegen + planen + umsetzen in der Reihenfolge loot-click-filter → inventory-cursor-item → bag-quest-upgrades → vendor-sell-all → town-npc-identity (Eigner, /setgoal 2026-11-28).
- D1 Klick außerhalb des Inventars legt das Item an seinen Ursprungsplatz zurück, nichts wird weggeworfen (Q5).
- D2 Drag & Drop und Rechtsklick bleiben zusätzlich erhalten; Schließen des Inventars mit Cursor-Item legt es zurück.
- D3 Falscher Platz blinkt rot wie beim Ziehen, das Item bleibt am Cursor.

## Erfolgskriterien
1. Klick hebt Item an den Cursor (Icon folgt der Maus), Klick auf leeren Platz legt ab, auf belegten Platz tauscht (getauschtes Item hängt am Cursor) — Nachweis: PlayMode-Test.
2. Klick auf passenden Ausrüstungsplatz oder den Helden legt an, falscher Platz lehnt ab — Nachweis: PlayMode-Test über den Equip-Pfad, auch als Koop-Client.
3. Klick außerhalb und Schließen des Inventars legen zurück, kein Item geht verloren — Nachweis: PlayMode-Test (Taschen-/Gear-Summe unverändert).
4. Voll-Suiten grün, Windows-Shot mit Item am Cursor — Nachweis: `bash scripts/run-unity-tests.sh` failed 0, Shot unter `docs/inventory-cursor/`.

## Nicht-Ziele
- Kein Wegwerfen auf den Boden.
- Keine Änderung an Truhe/Händler/Schmied außer gemeinsam genutzter Zellen.

## Abhängigkeiten
- Keine.
