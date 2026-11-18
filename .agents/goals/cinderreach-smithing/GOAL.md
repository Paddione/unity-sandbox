---
goal: cinderreach-smithing
slug: cinderreach-smithing
health_goals: [G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-smithing/tasks.md]
---

# Schmiede für gezielte Beuteverbesserung

## Objective

Spieler verwandeln überschüssige Beute in Material und verbessern einen fast passenden Fund gezielt mit Material und Gold.

## Erfolgskriterien

1. Die Schmiede im Lantern Hold zeigt Materialertrag, zulässige Ersatz-Affixe und Gesamtkosten vor der Bestätigung.
2. Verwertung entfernt einen magischen oder seltenen Gegenstand aus der Tasche. Umschmieden beschränkt sich pro Gegenstand dauerhaft auf einen gewählten Affix-Slot, erhält die übrigen Eigenschaften und verwendet die Affix-Pools und Wertebereiche der ursprünglichen Gegenstandsstufe. Uniques sind von beiden Aktionen ausgeschlossen.
3. Gegenstand, Gold und Material werden gemeinsam gespeichert. Tests belegen den Erfolg beider Aktionen, unzureichende Mittel, erneute Bestätigung, Schreibfehler und Save-Roundtrip.
4. Unity-Suiten und der vollständige Akt-I-Autoplay sind grün. Die neuen Systemtests belegen die Ziele unabhängig vom regulären Akt-I-Lauf.

## Scope und Nicht-Ziele

Ein Material, zwei Aktionen und eine Solo-Station. Keine neuen Ausrüstungsplätze und keine Co-op-Transaktionen. Aufträge sind für Erwerb und Nutzung des Materials optional.

## Abhängigkeiten

Empfohlener Start nach `cinderreach-zone-contracts`. Der [Stufenplan](../../plans/cinderreach-smithing/tasks.md) beschreibt die Integrationspunkte und Nachweise.
