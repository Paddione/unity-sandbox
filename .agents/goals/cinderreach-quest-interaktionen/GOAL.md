---
goal: cinderreach-quest-interaktionen
slug: cinderreach-quest-interaktionen
plan_refs: [.agents/plans/cinderreach-quest-interaktionen/tasks.md]
health_probe: {"id":"G-QUESTCOVER01","title":"Ungetestete Cinderreach-Inhalte","cmd":"cd ~/unity-sandbox && python3 scripts/check-quest-coverage.py","target":0}
---

# Spielbare Quests und Weltinteraktionen absichern

## Objective

Für jeden spielbaren Akt von Cinderreach automatisch nachweisen, dass Spieler jede Haupt- und Nebenquest auslösen und beenden sowie jede platzierte Weltinteraktion erreichen und bestimmungsgemäß benutzen können.

## Erfolgskriterien

1. Ein aus den Spielinhalten gewonnenes Inventar erfasst auch neue Quests und einzelne Platzierungen desselben Prefabs. Jede Zeile verweist auf einen ausführbaren Testfall.
2. Für jede Quest führt ein PlayMode-Test vom echten Auslöser in der Spielszene bis zum für Spieler sichtbaren Abschluss. Jede Platzierung wird auf Erreichbarkeit, richtiges Ziel und beobachtbare Wirkung geprüft.
3. EditMode-Fälle prüfen jeden vorgesehenen Verlauf, gesperrte Schritte, einmalige Belohnungen und die Wiederherstellung persistenter Zwischen- und Endzustände nach Speichern und Laden, soweit jeweils anwendbar.
4. Beide vollständigen Testsuiten bestehen. Die dauerhafte Health-Probe zählt ungedeckte Inhalte und meldet null. Ein neuer ungetesteter Inhalt macht die Prüfung rot.

## Scope und Abhängigkeiten

Maßgeblich ist der spielbare Bestand auf main. Neue Inhalte aus den drei aktiven Akt-Plänen werden mit ihrer Integration erfasst. Benötigt werden Projekt-Editor, gebaute Spielszenen und der vorhandene Unity-Test-Runner.

## Nicht-Ziele

Eigenständige Menü- und Inventartests, neue Spielinhalte und eine vollständige PlayMode-Wiederholung jedes alternativen Verlaufs liegen außerhalb dieses Ziels.
