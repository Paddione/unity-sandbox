# Goal: cinderreach-questkatalog

health_goals: [G-QUESTCOVER01]

## Objective
Alle vorhandenen Storyquests sind einzeln nachschlagbar. Die Übersicht folgt dem Katalog des Spiels.

## Ist-Befunde
- B1 StoryCatalog.All in StoryModel.cs enthält Questgeber, Ziele, Freigaben und Belohnungen.
- B2 LocCatalog.Story.cs enthält die deutschen Titel und Zielbeschreibungen.

## Entscheidungen
- D1 Übersicht aus dem Bestand erzeugen. Spätere redaktionelle Änderungen erfolgen Quest für Quest.

## Erfolgskriterien
1. Jede Quest erscheint mit ihren bestehenden Regeln in der Übersicht — Nachweis: python3 scripts/quest_inventory.py --check.
2. Die Questabdeckung bleibt vollständig — Nachweis: python3 scripts/check-quest-coverage.py.

## Nicht-Ziele
- Neue Dialoge, Aufnahmen, Cutscenes und Änderungen an Spielregeln.

## Abhängigkeiten
- Keine.
