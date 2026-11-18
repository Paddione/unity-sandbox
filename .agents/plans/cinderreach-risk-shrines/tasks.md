---
title: "Entscheidungen an Schreinen"
domains: [unity, gameplay]
status: planned
goal: cinderreach-risk-shrines
slug: cinderreach-risk-shrines
branch: feature/cinderreach-risk-shrines-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
---

# cinderreach-risk-shrines — Implementation Plan

[Ziel und Erfolgskriterien](../../goals/cinderreach-risk-shrines/GOAL.md). Basis `670f2a0`. Startbedingung: #7 und #10 abgeschlossen.

## File Structure

Die Stufen nennen Dateien relativ zu `sandbox/Assets/`. Neue Assets und Ordner erhalten `.meta`-Dateien. Plane pro neuer C#-Datei höchstens 250 Zeilen. Bestehende Dateien nur an den benannten Anschlüssen erweitern. Arbeite aus dem Unity-Sandbox-Worktree gemäß [Session-Protokoll](../arpg-session-loop/tasks.md). Save-Migrationen nacheinander landen und die nächste freie Version jeweils auf aktuellem main bestimmen. Numerische Werte sind Startannahmen für den Spieltest.

### S1 · Zustand und Spielstand

Neu: `Scripts/Zones/RiskShrineProgress.cs`, `Tests/EditMode/RiskShrineProgressTests.cs`. Anschlüsse: `Scripts/Save/{SaveGame,SaveMigrations,SaveService,RunProfile}.cs`, `Tests/EditMode/SaveMigrationTests.cs`.

Zustände `Unchosen`, `Healed`, `ChallengeActive`, `RewardAvailable`, `RewardClaimed`. Ältere Saves starten bei `Unchosen`. Neuer Run setzt den Zustand zurück. Bei der Auszahlung werden Gegenstand und `RewardClaimed` zusammen gespeichert. Bei Schreibfehler bleibt der Anspruch offen, und eine vorläufige Inventaränderung wird zurückgesetzt.

### S2 · Spielpfad

Neu: `Scripts/Zones/RiskShrine.cs`, `DiabloRiskShrineUI.cs`, `Editor/RiskShrineBuilder.cs`, `Tests/PlayMode/RiskShrineLoopTests.cs`. Anschlüsse: `Editor/SandboxBuilder.cs`, `Scenes/DiabloZone.unity` über den Builder.

UI zeigt Heilung bis Maximal-HP oder eine vorab bezeichnete Elite an einem freien Fen-Anker. Startwert +25 % Elite-HP. Nur die aktive Herausforderung spawnt den Gegner, auch nach Reload. Der Tod des Helden verbraucht die Herausforderung nicht. Seltenen Gegenstand deterministisch aus Run-Seed erzeugen und am Schrein einzeln abholen. Teste beide Wahlen, Reload vor Kill, Tod, volle Tasche, Doppelklick und neuen Run. Spiele beide Pfade manuell.

### S3 · Abnahme

- Neue Assertions vor der Implementierung mit `PLATFORMS=PlayMode bash scripts/run-unity-tests.sh` prüfen (expected: FAIL). Danach `bash scripts/run-unity-tests.sh` für beide Suiten (Exit 0).
- `python3 scripts/check-meta.py`, Szenenverifikation `SandboxBuilder.VerifyDiabloZone` gemäß Session-Protokoll und `python3 ~/scripts/agent-workspace.py check` (Exit 0).
- `bash scripts/run-autoplay.sh artifacts/autoplay/cinderreach-risk-shrines-normal -seed 0` (8/8 Phasen, FPS-Gate grün). Finalen `G-DIABLO06`-Lauf auf main belegen.
- Manuelle Ergebnisse mit Build-Commit und beobachtetem Verhalten im `PROGRESS.md` erfassen. Goal-Lint und Plan-Goal-Gate prüfen und den Fortschritt nach belegter Abnahme abschließen.
