---
title: Quest- und Interaktionstests für Cinderreach
domains: [unity, testing]
status: planned
goal: cinderreach-quest-interaktionen
health_goals: [G-QUESTCOVER01]
---

# cinderreach-quest-interaktionen — Implementation Plan

## File Structure

Basis 8029189. Befehle aus dem Unity-Repo-Worktree ausführen. Zielvertrag unter `.agents/goals/cinderreach-quest-interaktionen/GOAL.md`. Geplante neue Dateien sind unten je Schritt benannt. Vorhandene Assertions in `sandbox/Assets/Tests/EditMode/StoryStateTests.cs` und `SaveMigrationTests.cs` wiederverwenden. Neue Unity-Assets mit ihren .meta-Dateien einchecken.

## 1. Bestand erfassen und Lücken messbar machen

Questdefinitionen, Szenenaufbau und Laufzeit-Spawns aller spielbaren Akte erfassen. Pro Platzierung einen stabilen Schlüssel aus Szene und Platzierungskennung verwenden. Für jede Quest erwartete Auslöser, Verläufe, Sperren, Belohnungen und persistente Zustände aus den Spielregeln bestimmen. Gegen vorhandene NUnit-Fälle abgleichen.

In `scripts/test_check_quest_coverage.py` kleine Inventar- und Zuordnungsfixtures anlegen. Neuer Inhalt ohne Fall, gelöschter Test, doppelte Kennung und fehlendes Inventar müssen auffallen. Erster Lauf `python3 -m pytest scripts/test_check_quest_coverage.py -q`, expected: FAIL. Dann `scripts/check-quest-coverage.py` mit eigenständiger Bestandsableitung und Abgleich gegen konkrete Testfälle implementieren. Bei regulärem Lauf Anzahl ungedeckter Einträge ausgeben, bei unlesbarer Quelle mit Fehler abbrechen. Fixture-Suite muss grün werden. Leere Bestände dürfen kein Grün erzeugen.

## 2. Questregeln und Speicherung prüfen

In `sandbox/Assets/Tests/EditMode/QuestRuleTests.cs` Fälle je Quest-ID und Verlauf ergänzen. Vor und nach jedem relevanten Übergang Zustand und Belohnungsmenge prüfen. Gesperrten Schritt versuchen, Abschluss wiederholen und bei persistenten Zuständen über den produktiven Speicherpfad in eine frische Sitzung laden. Nach dem Laden Fortschritt und einmalige Vergabe erneut prüfen. Mit `PLATFORMS=EditMode TEST_FILTER=QuestRuleTests bash scripts/run-unity-tests.sh` alle neuen Fälle grün nachweisen.

## 3. Szenen und Spielerinteraktionen prüfen

In `sandbox/Assets/Tests/PlayMode/QuestJourneyTests.cs` pro Quest die gebaute Spielszene mit definiertem Vorzustand laden, den normalen Interaktionspfad benutzen und sichtbaren Abschluss an Dialog, Fuhrbuch oder HUD prüfen. In `WorldInteractionTests.cs` im selben Ordner pro Platzierung den erreichbaren Interaktionsbereich vom begehbaren Spielerbereich aus prüfen, das erwartete Ziel ansprechen und seine konkrete Wirkung beobachten. Direkte Questfortschrittsmutation nach Teststart würde einen defekten Auslöser verdecken. Zustand zwischen Fällen zurücksetzen. Beide Klassen einzeln mit `PLATFORMS=PlayMode TEST_FILTER=<Klasse> bash scripts/run-unity-tests.sh` ausführen.

## 4. Nachweise und Abschluss

Eine fehlende Zuordnung und eine vertauschte Zielbindung temporär simulieren. Die Probe bzw. der zugehörige PlayMode-Fall muss den konkreten Inhalt benennen. Änderungen zurücknehmen. Auf aktuellem Integrationsstand `python3 scripts/check-quest-coverage.py` (Ausgabe 0), `python3 scripts/check-meta.py`, `python3 -m pytest scripts -q`, `bash scripts/run-unity-tests.sh` und `python3 ~/scripts/agent-workspace.py check` erfolgreich ausführen. Fallzahlen je Akt, Revision und EditMode-/PlayMode-Ergebnisse aus artifacts im Fortschrittsdokument festhalten. Health-Probe dauerhaft für die Zuordnung verwenden. Vollständige Unity-Läufe dienen dem Implementierungsnachweis.
