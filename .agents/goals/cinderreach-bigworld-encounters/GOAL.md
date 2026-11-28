# Goal: cinderreach-bigworld-encounters

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]

plan_refs: [.agents/plans/cinderreach-bigworld-encounters/tasks.md]

## Objective
In der vergrößerten Cinderreach-Welt greift kein neuer Gegner in sichere Orte ein. Alle 70 zusätzlichen Gegner stehen außerhalb ihrer Aggro-Reichweite zu Toren, Händlerplätzen, Ankünften und Wegpunkten, und Regressionen sperren das dauerhaft.

## Ist-Befunde
- B1 Erweiterung ist gelandet (Commit 2578868): begehbare Quadrate Ashen Fen 70 m, Sunken Ossuary 56 m, Cinder Kiln 56 m, Glass Barrens 76 m (`sandbox/Assets/Scripts/Zones/WorldBounds.cs`, halfExtents 35/28/28/38); Lantern-Hold-Palisade Radius 12 m (`sandbox/Assets/Editor/TownBuilder.cs:21,37`).
- B2 WorldEdgeTests zählen je Zone 16/18/18/18 zusätzliche Gegner und prüfen Aggro-Abstände zu sicheren Orten (`sandbox/Assets/Tests/EditMode/WorldEdgeTests.cs:52-110`, `RangedCaster.AggroRange = 14 m`, Boss-Ranges 11–18 m in `sandbox/Assets/Scripts/Enemies/`).
- B3 Letzter Stand grün für 2578868: EditMode 791/791, PlayMode 432/432 (`docs/test-runs.jsonl`), Linux-Spielerbuild 209 MB/379,9 s (`docs/build-metrics.jsonl`); alle 20 Goal-Verzeichnisse in `.agents/goals/` stehen auf `Prozent: 100 %`.
- B4 Konfliktspuren in `docs/test-runs.jsonl` Zeilen 1–5 (Merge-Marker `<<<<<<<`, `>>>>>>> d7b382a/0068433/128b32a`) — vor neuen Belegzeilen zu bereinigen.

## Entscheidungen
- D1 Modus: Anlegen + planen + umsetzen (Eigner: Empfehlung übernommen).
- D2 Zuschnitt: eigenes Encounter-Goal, unabhängig abnehmbar (Eigner: Empfehlung übernommen).
- D3 Vorrang: Safe-Abstände gleichrangig mit Erreichbarkeit/Autoplay, dieses Goal sichert den Encounter-Anteil (Eigner: Empfehlung übernommen).
- D4 Abnahme: Voll-Suiten + Verify + Autoplay seed 0 grün (Eigner: Empfehlung übernommen).

## Erfolgskriterien
1. Alle 70 neuen Gegner (16/18/18/18) stehen außerhalb der Aggro-Reichweite zu jedem Tor, jeder Ankunft, jedem Wegschrein, jedem Händler und jedem Coop-Spawn — Nachweis: `PLATFORMS=EditMode TEST_FILTER=WorldEdgeTests bash scripts/run-unity-tests.sh` grün plus `SandboxBuilder.VerifyDiabloZone` grün.
2. Vier geschlossene Begrenzungen je Zone auf der neuen Kante und alle Tore/Spawns/Ankünfte innerhalb der Wände — Nachweis: `WorldEdgeTests.GatesShrinesSpawnsAndArrivals_AreInsideTheWalls` und `EveryZone_HasFourSolidEdgeWallsOnItsRim` grün in der generierten `DiabloZone.unity`.
3. Keine Regression der Suiten in der größeren Welt — Nachweis: `bash scripts/run-unity-tests.sh` vollständig grün (EditMode ≥ 791, PlayMode ≥ 432, `failed == 0`), neue Zeilen in `docs/test-runs.jsonl` ohne Merge-Marker, `KayKitAssets.Verify` grün.

## Nicht-Ziele
- Keine neuen Encounter-Positionen entwerfen; nur absichern, was 2578868 gesetzt hat.
- Kein Autoplay-Routing und keine Performance-Wertung (eigenes Folge-Goal).
- Keine Balance-Änderung (HP/Schaden/Aggro-Zahlen bleiben unangetastet).

## Abhängigkeiten
- Keine.
