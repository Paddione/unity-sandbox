---
title: "Gegnergruppen mit klaren Rollen"
domains: [unity, gameplay]
status: planned
goal: cinderreach-encounter-roles
slug: cinderreach-encounter-roles
branch: feature/cinderreach-encounter-roles-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
---

# cinderreach-encounter-roles — Implementation Plan

[Ziel und Erfolgskriterien](../../goals/cinderreach-encounter-roles/GOAL.md). Basis `670f2a0`. Startbedingung: Warden- und Kiln-Einstiegsfixes auf main.

## File Structure

Die Stufen nennen Dateien relativ zu `sandbox/Assets/`. Neue Assets und Ordner erhalten `.meta`-Dateien. Plane pro neuer C#-Datei höchstens 250 Zeilen. Bestehende Dateien nur an den benannten Anschlüssen erweitern. Arbeite aus dem Unity-Sandbox-Worktree gemäß [Session-Protokoll](../arpg-session-loop/tasks.md). Save-Migrationen nacheinander landen und die nächste freie Version jeweils auf aktuellem main bestimmen. Numerische Werte sind Startannahmen für den Spieltest.

### S1 · Begegnungen platzieren

Neu: `Scripts/Zones/EncounterLayout.cs`, `Editor/EncounterBuilder.cs`, `Tests/EditMode/EncounterLayoutTests.cs`. Anschlüsse: `OssuaryBuilder.cs`, `KilnBuilder.cs`, `Scripts/Zones/LayoutSeeder.cs`, Builder-Ausgabe `Scenes/DiabloZone.unity`.

`EncounterLayout.ForVariant(int variant)` liefert Gegneranker, Deckung und Rückzugsroute. Im Ossuary steht ein Warden-Paar an unterbrochenen Sichtachsen. Im Kiln lassen sich Front- und Rückgruppe an einer Engstelle trennen. Eine weitere Kiln-Gruppe kombiniert Ash Lancer und zonengebundenen Warden. Vorhandene Spawns umsetzen. Ankunft vom Tor und Wegpunkt sowie die Rückzugspfade über alle drei Varianten testen.

### S2 · Rollen im Kampf

Anschlüsse: `Scripts/Enemies/RangedCaster.cs`, `AshLancer.cs`, `Scripts/EnemyAttack.cs`. Neu: `Tests/PlayMode/EncounterRoleTests.cs`.

Die laufenden Warden-Fixes übernehmen. Der Warden prüft feste Deckung vor Warnung und Schuss. Der Lancer macht seine Ansturmrichtung sichtbar. Die vordere Kiln-Gruppe ist unabhängig auslösbar. Tests prüfen blockierte Schüsse, getrennte Aggro und Abbruch beim Zonenwechsel. Ein Windows-Spieltest belegt Deckung, Trennung und Zielpriorität.

### S3 · Abnahme

- Neue Assertions vor der Implementierung mit `PLATFORMS=PlayMode bash scripts/run-unity-tests.sh` prüfen (expected: FAIL). Danach `bash scripts/run-unity-tests.sh` für beide Suiten (Exit 0).
- `python3 scripts/check-meta.py`, Szenenverifikation `SandboxBuilder.VerifyDiabloZone` gemäß Session-Protokoll und `python3 ~/scripts/agent-workspace.py check` (Exit 0).
- `bash scripts/run-autoplay.sh artifacts/autoplay/cinderreach-encounter-roles-normal -seed 0` (8/8 Phasen, FPS-Gate grün). Finalen `G-DIABLO06`-Lauf auf main belegen.
- Manuelle Ergebnisse mit Build-Commit und beobachtetem Verhalten im `PROGRESS.md` erfassen. Goal-Lint und Plan-Goal-Gate prüfen und den Fortschritt nach belegter Abnahme abschließen.
