# Progress: cinderreach-risk-shrines

Prozent: 100 %

## Erledigt

- Ziel und Plan angelegt.
- S1 Zustand/Spielstand (Übernahme durch opencode-Session ses_e115042b; Rebase auf Encounter-main d91035a, Save-Kette v4→…→V7→V8):
  - Neu: `Scripts/Zones/RiskShrineProgress.cs` (Unchosen/Healed/ChallengeActive/RewardAvailable/RewardClaimed, deterministisches `RollRare`, atomares `TryClaim` mit Flip-vor-Persist + Rollback), `Tests/EditMode/RiskShrineProgressTests.cs`.
  - Save v8 additiv (`SaveGame`/`SaveMigrations.V7ToV8`/`SaveService` Capture/Apply/`RunProfile.RiskShrine` + Reset), `SaveMigrationTests` v8.
- S2 Spielpfad (selber Branch):
  - Neu: `Scripts/Zones/RiskShrine.cs` (Heilung/Challenge/Einlösung, Elite +25 % HP, Reload-Respawn, Heldentod-safe, Death-Relay), `DiabloRiskShrineUI.cs`, `Editor/RiskShrineBuilder.cs` (freier Fen-Anker 6,5/7,5), `Tests/PlayMode/RiskShrineLoopTests.cs` (6 UnityTests).
  - `SandboxBuilder` Build- + Verify-Hooks; Szene nur via Builder regeneriert.
  - Perf: `RiskShrine.Update` ohne `Camera.main` pro Frame (statisch gecacht, Klick-Gate zuerst — gleiche Stelle wie #7, ~2 fps).
  - Editor/PlayMode-Trennung: `ShrineAnchor` als Runtime-Konstante nach `RiskShrine` dupliziert (Single Source dort; Builder + Test nutzen sie — PlayMode sieht kein Editor/).
- S3 Integration/Nachweis:
  - `bash scripts/run-unity-tests.sh` Exit 0: EditMode 274/274, PlayMode 119/119 (`/tmp/rs-full3.log` + `/tmp/rs-edit.log`; 3 #9-Migrationstests auf `CurrentVersion` gehoben; `LayoutSeedTests`-Erwartung um +2 Kiln Wardens aus #10 ergänzt — in #10 nicht aufgefallen, da Suite vor Szene-Regen lief).
  - `BuildDiabloZone` + `VerifyDiabloZone` + `KayKitAssets.Verify` je Exit 0 (`/tmp/rs-bv.out`).
  - `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-risk-shrines-normal" -seed 0` Exit 0: fps-gate PASS (worst avg 36,9 ≥ 30,0; `/tmp/rs-autoplay.log`).
  - Spielpfad-Belege je Wahl: `RiskShrineLoopTests` (PlayMode, grün — Heilung vs. Challenge, Reload-vor-Kill, Tod, volle Tasche, Doppelklick, neuer Run).
  - `python3 scripts/check-meta.py` Exit 0, `agent-workspace check` still vor Commit; Builder-LFS-Seiteneffekte rever­tiert.

## Nächste Schritte

Keine — alle drei Stufen belegt und gelandet.

## Offene Punkte

Keine.
