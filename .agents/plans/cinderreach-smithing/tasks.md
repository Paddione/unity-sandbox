---
title: "Schmiede für gezielte Beuteverbesserung"
domains: [unity, gameplay]
status: planned
goal: cinderreach-smithing
slug: cinderreach-smithing
branch: feature/cinderreach-smithing-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
---

# cinderreach-smithing — Implementation Plan

Ziel und Abnahme stehen im [GOAL.md](../../goals/cinderreach-smithing/GOAL.md). Arbeitsverzeichnis ist der Root des Unity-Sandbox-Worktrees. Referenzstand ist `58a6977`. Unity-Version aus `sandbox/ProjectSettings/ProjectVersion.txt`.

## File Structure

Die Spieldateien und Tests stehen bei ihrer jeweiligen Stufe. Gemeinsame Save-Dateien und Verifikationsbefehle folgen unten. Aktualisiere nach jeder Stufe das verlinkte `PROGRESS.md`.

## S1 · Gegenstandsmetadaten und Transaktionen

Neu: `Scripts/Items/SmithingService.cs`, `Tests/EditMode/SmithingServiceTests.cs`. Ändern: `Scripts/Items/ItemDefinition.cs`, `Scripts/Loot/LootTable.cs`, `Scripts/Town/VendorStock.cs` und die Save-Dateien aus der gemeinsamen Liste.

- `ItemInstance` erhält `affixTier` (1/2) und `reforgeIndexPlusOne` (0 = ungebunden). Erfasse die Tier-Information am Erzeugungspunkt von Drops und Händlerware. Migriere alte Einträge in Tasche, Stash und Ausrüstung auf Tier 1, behalte vorhandene Rollwerte und notiere die Annahme für historische Kindled-Funde.
- `SmithingService.TrySalvage(GameObject hero, int bagIndex, ItemInstance expected)` und `TryReforge(GameObject hero, int bagIndex, ItemInstance expected, int affixIndex, string replacementKey)` verwenden dieselbe Validierung wie die Vorschau. Sessionlokale Angebotsgeneration plus In-Flight-Sperre entwerten jede Bestätigung nach Erfolg. Prüfe auch belegte Affix-Keys, Item-Slot, Rarität, Kosten und erwartete Item-Instanz erneut.
- Startwerte fürs Balancing: magisch 1, selten 3 Material. Umschmieden kostet 3 Material und 50 Gold. Kandidat entsteht als Kopie inklusive aller übrigen Affixe. Bei Save-Schreibfehler bleiben Gegenstand, Geld und Material wie vor der Aktion. Wallet und Inventar werden gemeinsam mit dem Material in einem Snapshot gespeichert.

## S2 · Schmiedestation und Spielpfad

Neu: `Scripts/Town/DiabloSmithUI.cs`, `Scripts/Town/SmithStation.cs`, `Tests/PlayMode/SmithingLoopTests.cs`. Ändern: `Editor/TownBuilder.cs`, `sandbox/Assets/Scenes/DiabloZone.unity` (über den Builder).

- Im Lantern Hold listet die Station Gegenstände aus der Tasche und zeigt Preis, bisherige Eigenschaft, möglichen Ersatz und Rollspanne. Nach Erfolg zeigt sie den tatsächlichen Rollwert und aktualisiert Kontostände. Die Bedienung nutzt die bestehende HD-UI-Skalierung. Bei Stash-Transfers und Aus-/Anlegen bleibt der bearbeitete Slot erhalten.
- PlayMode-Verträge: `Salvage_RemovesItemAndGrantsMaterialOnce`, `Reforge_KeepsOtherAffixesAndLocksSlotAfterReload`, `Reforge_RejectsStaleOfferAndInsufficientFunds`, `Reforge_FailedSaveRestoresAllState`. Spiele zusätzlich auf Normal und Kindled je einen Fund bis zur Verbesserung durch.

## Gemeinsame Dateiliste und Umsetzung

Pfade mit `Scripts/`, `Editor/` oder `Tests/` liegen unter `sandbox/Assets/`. Neue Unity-Assets und Ordner erhalten ihre `.meta`-Dateien. Für Save-Änderungen sind `Scripts/Save/SaveGame.cs`, `SaveMigrations.cs`, `SaveService.cs`, `RunProfile.cs` und `Tests/EditMode/SaveMigrationTests.cs` vorgesehen. Baseline der vier Save-Klassen: 54, 114, 249 und 37 Zeilen in dieser Reihenfolge. Änderungen bleiben an diesen Integrationspunkten klein, die Spiellogik kommt in die neuen Dateien. Für neue C#-Dateien ist ein Planbudget von 250 Zeilen je Datei vorgesehen.

Stufen nacheinander nach dem [Session-Protokoll](../arpg-session-loop/tasks.md) ausführen. Vor S1 die nächste freie Save-Version auf `origin/main` ermitteln. Kompatibilität vom bisherigen Save bis zum aktuellen Format und Defaults für fehlende Felder absichern. Die empfohlene Roadmap-Reihenfolge serialisiert die drei Save- und UI-Änderungen.

## S3 · Integration und Nachweis

- Neue Assertions zunächst gegen den Stand ohne das jeweilige Verhalten laufen lassen (expected: FAIL), danach beide Unity-Suiten via `bash scripts/run-unity-tests.sh` ausführen (Exit 0, beide XML-Suiten ohne Failures).
- `python3 scripts/check-meta.py` und `python3 /home/patrick/scripts/agent-workspace.py check` müssen mit Exit 0 abschließen. `VerifyDiabloZone` nach dem Session-Protokoll ausführen.
- `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-smithing-normal" -seed 0` (Exit 0, acht Akt-I-Phasen, keine Exceptions, FPS-Gate grün). Den bestehenden `G-DIABLO06`-Workflow auf dem finalen Stand kontrollieren und Run-Link festhalten.
- Ergebnis der neuen Tests, manuelle Spielbelege und Save-Migration im Goal-`PROGRESS.md` festhalten. Vor 100 % Goal-Lint und Plan-Goal-Gate erneut ausführen.
