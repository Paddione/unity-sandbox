---
title: "Kindled-Finale mit eigener Mechanik"
domains: [unity, gameplay]
status: planned
goal: cinderreach-kindled-finale
slug: cinderreach-kindled-finale
branch: feature/cinderreach-kindled-finale-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
---

# cinderreach-kindled-finale — Implementation Plan

[Ziel und Erfolgskriterien](../../goals/cinderreach-kindled-finale/GOAL.md). Basis `670f2a0`. Startbedingung: #10 abgeschlossen.

## File Structure

Die Stufen nennen Dateien relativ zu `sandbox/Assets/`. Neue Assets und Ordner erhalten `.meta`-Dateien. Plane pro neuer C#-Datei höchstens 250 Zeilen. Bestehende Dateien nur an den benannten Anschlüssen erweitern. Arbeite aus dem Unity-Sandbox-Worktree gemäß [Session-Protokoll](../arpg-session-loop/tasks.md). Save-Migrationen nacheinander landen und die nächste freie Version jeweils auf aktuellem main bestimmen. Numerische Werte sind Startannahmen für den Spieltest.

### S1 · Arenamuster

Neu: `Scripts/Enemies/KilnSectorPattern.cs`, `Tests/EditMode/KilnSectorPatternTests.cs`, `Tests/PlayMode/KindledFinaleTests.cs`. Anschluss: `Scripts/Enemies/KilnmotherController.cs`.

Auf Solo-Kindled ersetzt ein Sektormuster in Phase zwei schnelle Wellen und zufällige Bodenfelder. Beschwörungen bleiben. Startwerte: vier 90°-Sektoren, bis zu zwei benachbarte aktiv, 1,5 s Vorwarnung, 2 s Brenndauer. Reihenfolge per Run-Seed. Dieselbe Geometrie treibt Markierung und Treffer, höchstens ein Treffer je Muster. Bei Tod, Respawn oder Rückzug Effekt entfernen. Teste Warnfrist, Treffer, traversierbare sichere Route und unverändertes Normal-Verhalten.

### S2 · Belohnung

Neu: `Scripts/Loot/KindledFinaleReward.cs`, `Tests/PlayMode/KindledFinaleRewardTests.cs`. Anschlüsse: `Scripts/Enemies/KilnmotherController.cs`, `Scripts/SessionComplete.cs`, `Scripts/Items/ItemDatabase.cs`, `Scripts/Save/{SaveGame,SaveMigrations,SaveService,RunProfile}.cs`.

Der bestehende Bossdrop bleibt. Beim ersten Kindled-Sieg einen gespeicherten Anspruch auf eine klassenkompatible Rare-Waffe mit Kindled-Affixen erzeugen. Volle Tasche hält den Anspruch offen. Auszahlung und Verbrauch gemeinsam speichern, bei Save-Fehler zurückrollen. Nach Reload im Abschlussfenster erneut anbieten. Ältere abgeschlossene Runs erhalten keinen rückwirkenden Anspruch. Teste beide Klassen, Save-Fehler, Reload und wiederholten Boss-Tod. Manueller Windows-Lauf pro Klasse auf Kindled.

### S3 · Abnahme

- Neue Assertions vor der Implementierung mit `PLATFORMS=PlayMode bash scripts/run-unity-tests.sh` prüfen (expected: FAIL). Danach `bash scripts/run-unity-tests.sh` für beide Suiten (Exit 0).
- `python3 scripts/check-meta.py`, Szenenverifikation `SandboxBuilder.VerifyDiabloZone` gemäß Session-Protokoll und `python3 ~/scripts/agent-workspace.py check` (Exit 0).
- `bash scripts/run-autoplay.sh artifacts/autoplay/cinderreach-kindled-finale-normal -seed 0` (8/8 Phasen, FPS-Gate grün). Finalen `G-DIABLO06`-Lauf auf main belegen.
- Zusätzlich Kindled-Autoplay mit `bash scripts/run-autoplay.sh artifacts/autoplay/kindled-finale-kindled -seed 1 -difficulty Kindled` ausführen.
- Manuelle Ergebnisse mit Build-Commit und beobachtetem Verhalten im `PROGRESS.md` erfassen. Goal-Lint und Plan-Goal-Gate prüfen und den Fortschritt nach belegter Abnahme abschließen.
