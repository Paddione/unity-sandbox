---
title: "Uniques, die den Build verändern"
domains: [unity, gameplay]
status: planned
goal: cinderreach-build-uniques
slug: cinderreach-build-uniques
branch: feature/cinderreach-build-uniques-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
---

# cinderreach-build-uniques — Implementation Plan

[Ziel und Erfolgskriterien](../../goals/cinderreach-build-uniques/GOAL.md). Basis `670f2a0`. Startbedingung: #8, #9 und #10 abgeschlossen.

## File Structure

Die Stufen nennen Dateien relativ zu `sandbox/Assets/`. Neue Assets und Ordner erhalten `.meta`-Dateien. Plane pro neuer C#-Datei höchstens 250 Zeilen. Bestehende Dateien nur an den benannten Anschlüssen erweitern. Arbeite aus dem Unity-Sandbox-Worktree gemäß [Session-Protokoll](../arpg-session-loop/tasks.md). Save-Migrationen nacheinander landen und die nächste freie Version jeweils auf aktuellem main bestimmen. Numerische Werte sind Startannahmen für den Spieltest.

### S1 · Loot, Anzeige und Spielstand

Anschlüsse: `Scripts/Items/ItemDatabase.cs`, `Scripts/Loot/DroptableDatabase.cs`, `Scripts/DiabloInventoryUI.cs`. Neu: `Tests/EditMode/BuildUniqueLootTests.cs`.

IDs `lancer-ward` und `wick-of-the-halo` über vorhandene klassengebundene Unique-Auswahl und Item-Persistenz führen. Tooltip bezieht den Effekttext aus der Definition. Teste Klassenfilter, Inventar-/Stash-Roundtrip und Schmiedeausschluss.

### S2 · Effekte

Neu: `Scripts/Items/BuildUniqueEffects.cs`, `Tests/PlayMode/BuildUniqueEffectTests.cs`. Anschlüsse: `Scripts/DiabloSkills.cs`, `Scripts/Enemies/AshLancer.cs` und die vorhandene Wisp-Zielwahl.

Cleave unterbricht mit ausgerüstetem Unique höchstens einen aktiven Lancer-Ansturm pro Cast und versetzt ihn in die bestehende Erholung. Wisps bevorzugen mit der Laterne ein gültiges Ziel in der aktiven Halo-Fläche. Suchreichweite und bestehende Sichtregeln bleiben gültig. Ohne Halo oder Unique gilt die normale Auswahl. Teste beide Skill-Varianten, Ablegen und Ablauf des Halo. Manueller Vergleich je Klasse mit und ohne Waffe.

### S3 · Abnahme

- Neue Assertions vor der Implementierung mit `PLATFORMS=PlayMode bash scripts/run-unity-tests.sh` prüfen (expected: FAIL). Danach `bash scripts/run-unity-tests.sh` für beide Suiten (Exit 0).
- `python3 scripts/check-meta.py`, Szenenverifikation `SandboxBuilder.VerifyDiabloZone` gemäß Session-Protokoll und `python3 ~/scripts/agent-workspace.py check` (Exit 0).
- `bash scripts/run-autoplay.sh artifacts/autoplay/cinderreach-build-uniques-normal -seed 0` (8/8 Phasen, FPS-Gate grün). Finalen `G-DIABLO06`-Lauf auf main belegen.
- Manuelle Ergebnisse mit Build-Commit und beobachtetem Verhalten im `PROGRESS.md` erfassen. Goal-Lint und Plan-Goal-Gate prüfen und den Fortschritt nach belegter Abnahme abschließen.
