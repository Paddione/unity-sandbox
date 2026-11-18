---
title: "Skill-Varianten mit spürbaren Trade-offs"
domains: [unity, gameplay]
status: planned
goal: cinderreach-skill-variants
slug: cinderreach-skill-variants
branch: feature/cinderreach-skill-variants-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
---

# cinderreach-skill-variants — Implementation Plan

Ziel und Abnahme stehen im [GOAL.md](../../goals/cinderreach-skill-variants/GOAL.md). Arbeitsverzeichnis ist der Root des Unity-Sandbox-Worktrees. Referenzstand ist `58a6977`. Unity-Version aus `sandbox/ProjectSettings/ProjectVersion.txt`.

## File Structure

Die Spieldateien und Tests stehen bei ihrer jeweiligen Stufe. Gemeinsame Save-Dateien und Verifikationsbefehle folgen unten. Aktualisiere nach jeder Stufe das verlinkte `PROGRESS.md`.

## S1 · Variantenwahl, Save und Charakterbogen

Neu: `Scripts/Progression/SkillVariantCatalog.cs`, `Tests/EditMode/SkillVariantTests.cs`. Ändern: `Scripts/Progression/DiabloCharacterUI.cs` und die Save-Dateien aus der gemeinsamen Liste.

- `SkillVariantCatalog` definiert pro Klassen-ID und `DiabloSkills.Skill` die beiden Varianten mit stabiler String-ID und gemeinsamen Werten für Vorschau und Laufzeit. `RunProfile` hält die Wahl. Fehlende, unbekannte und klassenfremde IDs fallen auf die bisherige Variante zurück.
- Der Charakterbogen bietet die Auswahl nur im Solo-Lantern-Hold an. Verwende die räumliche Town-Abgrenzung, weil der Akt das Fen demselben Fortschrittsbereich zurechnet. Ein Test prüft den Wechsel im Hold und dessen Ablehnung im Fen. Fortschritts- und Rangpunkte bleiben beim Wechsel erhalten, Save und Reload bewahren die Wahl.

## S2 · Kampfverhalten und Spielvergleich

Neu: `Scripts/Progression/SkillVariantTargeting.cs`, `Tests/PlayMode/SkillVariantCombatTests.cs`. Ändern: `Scripts/DiabloSkills.cs`, mit kleinem Anschluss an `DoCleave` und `DoHalo`.

- `SkillVariantTargeting.InCleaveArea(Vector3 origin, Vector3 forward, Vector3 target, float radius, float arcDegrees)` arbeitet planar. Startwerte: Frontalschlag 90° Gesamtwinkel und 1,5-fache Reichweite; Rundumschlag behält die aktuelle Reichweite. Gleiche Schadens- und Cooldown-Pfade. Der Spieltest vergleicht Umzingelung und Gegner auf Distanz.
- Ortsfeste Halo-Zone verankert sich am Cast-Punkt und nutzt die doppelte bisherige Dauer bei unverändertem Tick-Intervall und gleicher Fläche. Die mitlaufende Variante übernimmt das bestehende Verhalten. Der Spieltest vergleicht beweglichen Kampf und Kontrolle einer Engstelle. Wiederholte Casts ersetzen den alten Effekt, Tod und Szenenwechsel räumen ihn auf.
- PlayMode-Verträge: `Cleave_BaselineHitsBehindAndConeHitsFarAhead`, `Halo_MovesOrRemainsAtCastPoint`, `Variant_ReloadAndDefaults`. Prüfe auch Ziel am Kegelrand und dass im Co-op der vorhandene Basis-Skillpfad greift.

## Gemeinsame Dateiliste und Umsetzung

Pfade mit `Scripts/`, `Editor/` oder `Tests/` liegen unter `sandbox/Assets/`. Neue Unity-Assets und Ordner erhalten ihre `.meta`-Dateien. Für Save-Änderungen sind `Scripts/Save/SaveGame.cs`, `SaveMigrations.cs`, `SaveService.cs`, `RunProfile.cs` und `Tests/EditMode/SaveMigrationTests.cs` vorgesehen. Baseline der vier Save-Klassen: 54, 114, 249 und 37 Zeilen in dieser Reihenfolge. Änderungen bleiben an diesen Integrationspunkten klein, die Spiellogik kommt in die neuen Dateien. Für neue C#-Dateien ist ein Planbudget von 250 Zeilen je Datei vorgesehen.

Stufen nacheinander nach dem [Session-Protokoll](../arpg-session-loop/tasks.md) ausführen. Vor S1 die nächste freie Save-Version auf `origin/main` ermitteln. Kompatibilität vom bisherigen Save bis zum aktuellen Format und Defaults für fehlende Felder absichern. Die empfohlene Roadmap-Reihenfolge serialisiert die drei Save- und UI-Änderungen.

## S3 · Integration und Nachweis

- Neue Assertions zunächst gegen den Stand ohne das jeweilige Verhalten laufen lassen (expected: FAIL), danach beide Unity-Suiten via `bash scripts/run-unity-tests.sh` ausführen (Exit 0, beide XML-Suiten ohne Failures).
- `python3 scripts/check-meta.py` und `python3 /home/patrick/scripts/agent-workspace.py check` müssen mit Exit 0 abschließen. `VerifyDiabloZone` nach dem Session-Protokoll ausführen.
- `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-skill-variants-normal" -seed 0` (Exit 0, acht Akt-I-Phasen, keine Exceptions, FPS-Gate grün). Den bestehenden `G-DIABLO06`-Workflow auf dem finalen Stand kontrollieren und Run-Link festhalten.
- Ergebnis der neuen Tests, manuelle Spielbelege und Save-Migration im Goal-`PROGRESS.md` festhalten. Vor 100 % Goal-Lint und Plan-Goal-Gate erneut ausführen.
