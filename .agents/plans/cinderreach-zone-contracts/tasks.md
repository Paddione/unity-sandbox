---
title: "Optionale Zonenaufträge"
domains: [unity, gameplay]
status: planned
goal: cinderreach-zone-contracts
slug: cinderreach-zone-contracts
branch: feature/cinderreach-zone-contracts-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
---

# cinderreach-zone-contracts — Implementation Plan

Ziel und Abnahme stehen im [GOAL.md](../../goals/cinderreach-zone-contracts/GOAL.md). Arbeitsverzeichnis ist der Root des Unity-Sandbox-Worktrees. Referenzstand ist `58a6977`. Unity-Version aus `sandbox/ProjectSettings/ProjectVersion.txt`.

## File Structure

Die Spieldateien und Tests stehen bei ihrer jeweiligen Stufe. Gemeinsame Save-Dateien und Verifikationsbefehle folgen unten. Aktualisiere nach jeder Stufe das verlinkte `PROGRESS.md`.

## S1 · Angebote, Zustände und Save

Dateien unter `Scripts/Contracts/` neu: `ContractCatalog.cs`, `ContractProgress.cs`. Dazu die Save-Dateien aus der gemeinsamen Dateiliste und `Tests/EditMode/ContractProgressTests.cs` (neu).

- `ContractCatalog.Offers(int seed, ActProgress act)` liefert zwei `ContractOffer`-Einträge mit stabiler ID, Typ, Zone, geprüfter Zielposition und Goldbetrag. Je Typ eine feste Kandidatenliste. Der Fen dient als Fallback. Eigener deterministischer Seed-Kanal, damit `LayoutSeeder.ForSeed` seine bestehende Zuordnung behält.
- `ContractProgress` hält ein bei Annahme eingefrorenes Angebot mit Status `Active`, `Complete` oder `Claimed`. Erlaubt sind `TryAccept(ContractOffer offer)` und `TryComplete(string objectiveId)`. Nach Claim endet das Brett für diesen Run. Neues Spiel bzw. neuer Run initialisiert den Zustand, Reload stellt ihn wieder her. Teste gleiche Seeds, fehlende Freischaltung, ungültige Ziel-ID und Reload mit verändertem Freischaltstand.
- Save v4 erhält bei der Migration einen leeren Zustand. Ein aktiver Auftrag behält beim Laden seine ursprüngliche Position. Erledigte Ziele werden beim Szenenaufbau nicht erneut erzeugt.

## S2 · Spielpfad, Brett und Auszahlung

Neu: `Scripts/Contracts/ContractBoard.cs`, `ContractObjective.cs`, `DiabloContractUI.cs`, `Tests/PlayMode/ContractLoopTests.cs`. Ändern: `Editor/TownBuilder.cs`, `sandbox/Assets/Scenes/DiabloZone.unity` (über den Builder).

- Das Brett verwendet die bestehende Interaktion im Town-Hub. Pro aktivem Auftrag entsteht genau ein Ziel. Die Jagd verwendet einen vorhandenen Elite-Gegneraufbau und meldet den Tod über `IEnemyDeathHook`. Die Reliquie meldet die Interaktion in Reichweite. Der Laufzeitaufbau bleibt im Solo-Pfad.
- `ContractBoard.TryClaim(GameObject hero)` prüft Reichweite und Status, persistiert Gold und `Claimed` im selben Save und stellt bei Schreibfehler beide Live-Werte zurück. UI aktualisiert sich erst nach Abschluss der Aktion.
- PlayMode-Verträge: `Hunt_KillReturnClaim`, `Recovery_ReachCollectReturnClaim`, `Claim_ReloadCannotPayAgain`, `Claim_WriteFailureKeepsRewardAvailable`. Räume gegnerische Spawnpunkte und Zielwege für die drei Layouts ab. Validierung umfasst Tor- und Wegpunktzugang.

## Gemeinsame Dateiliste und Umsetzung

Pfade mit `Scripts/`, `Editor/` oder `Tests/` liegen unter `sandbox/Assets/`. Neue Unity-Assets und Ordner erhalten ihre `.meta`-Dateien. Für Save-Änderungen sind `Scripts/Save/SaveGame.cs`, `SaveMigrations.cs`, `SaveService.cs`, `RunProfile.cs` und `Tests/EditMode/SaveMigrationTests.cs` vorgesehen. Baseline der vier Save-Klassen: 54, 114, 249 und 37 Zeilen in dieser Reihenfolge. Änderungen bleiben an diesen Integrationspunkten klein, die Spiellogik kommt in die neuen Dateien. Für neue C#-Dateien ist ein Planbudget von 250 Zeilen je Datei vorgesehen.

Stufen nacheinander nach dem [Session-Protokoll](../arpg-session-loop/tasks.md) ausführen. Vor S1 die nächste freie Save-Version auf `origin/main` ermitteln. Kompatibilität vom bisherigen Save bis zum aktuellen Format und Defaults für fehlende Felder absichern. Die empfohlene Roadmap-Reihenfolge serialisiert die drei Save- und UI-Änderungen.

## S3 · Integration und Nachweis

- Neue Assertions zunächst gegen den Stand ohne das jeweilige Verhalten laufen lassen (expected: FAIL), danach beide Unity-Suiten via `bash scripts/run-unity-tests.sh` ausführen (Exit 0, beide XML-Suiten ohne Failures).
- `python3 scripts/check-meta.py` und `python3 /home/patrick/scripts/agent-workspace.py check` müssen mit Exit 0 abschließen. `VerifyDiabloZone` nach dem Session-Protokoll ausführen.
- `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-zone-contracts-normal" -seed 0` (Exit 0, acht Akt-I-Phasen, keine Exceptions, FPS-Gate grün). Den bestehenden `G-DIABLO06`-Workflow auf dem finalen Stand kontrollieren und Run-Link festhalten.
- Ergebnis der neuen Tests, manuelle Spielbelege und Save-Migration im Goal-`PROGRESS.md` festhalten. Vor 100 % Goal-Lint und Plan-Goal-Gate erneut ausführen.
