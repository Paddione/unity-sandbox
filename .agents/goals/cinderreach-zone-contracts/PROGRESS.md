# Progress: cinderreach-zone-contracts

Prozent: 100 %

## Erledigt

- S1 Angebote/Zustände/Save (2026-11-18, Worktree feature/cinderreach-zone-contracts-s1):
  - Neu: `sandbox/Assets/Scripts/Contracts/ContractCatalog.cs` (Offers(seed, act) → je 1 Hunt + 1 Recovery, stabile IDs, Fen-Fallback, eigener Seed-Kanal `seed ^ Salt`, LayoutSeeder-ForSeed unangetastet), `ContractProgress.cs` (TryAccept/TryComplete, Active/Complete/Claimed, ein Auftrag pro Run, Snapshot-Roundtrip).
  - Save v5 (beansprucht, origin/main war v4): `SaveGame.contract` (ContractSnapshot, aktive Position bleibt beim Laden erhalten), `SaveMigrations.V4ToV5` (leerer Zustand), `SaveService` Capture/Apply/DeleteSave, `RunProfile.Reset`.
  - Neu: `Tests/EditMode/ContractProgressTests.cs` (8 Tests: gleiche Seeds, Freischaltfilter, falsche Ziel-ID, Reload bei verändertem Freischaltstand, Claim beendet Run).
  - `Tests/EditMode/SaveMigrationTests.cs`: Version auf 5, bestehende Cases erhalten, plus V4-Migration und aktive-Positions-Roundtrip.
- S2 Spielpfad/Brett/Auszahlung (2026-11-18, selber Worktree):
  - Neu: `Contracts/ContractBoard.cs` (TryClaim mit Reichweite+Status, Gold+Claimed atomar im selben Save, Rollback bei Schreibfehler, WriteHook für Tests), `Contracts/ContractObjective.cs` (genau ein Ziel pro Auftrag, Hunt via IEnemyDeathHook, Reliquie via Reichweite, Solo-Pfad), `Town/DiabloContractUI.cs` (Refresh erst nach Abschluss).
  - Neu: `Tests/PlayMode/ContractLoopTests.cs` (Hunt_KillReturnClaim, Recovery_ReachCollectReturnClaim, Claim_ReloadCannotPayAgain, Claim_WriteFailureKeepsRewardAvailable).
  - `Editor/TownBuilder.SpawnContractBoard` + Aufruf in `SandboxBuilder.BuildDiabloZone`, `VerifyDiabloZone` prüft das Brett (Szene nur via Builder regeneriert).
- S3 Integration/Nachweis (2026-11-18, Übernahme durch opencode-Session ses_e115042b):
  - `bash scripts/run-unity-tests.sh` Exit 0: EditMode 201/201, PlayMode 88/88 (`/tmp/zc-s1s2-full2.log`).
  - `SandboxBuilder.BuildDiabloZone` + `VerifyDiabloZone` + `KayKitAssets.Verify` je Exit 0 (`/tmp/zc-buildverify.out`, BUILDVERIFY_EXIT=0).
  - `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-zone-contracts-normal" -seed 0` Exit 0: 9/9 Phasen, huskKills=50, bossKills=1, phaseTwo=1, overlay, exceptions=0, fps-gate PASS (worst avg 31,1 fps ≥ 30,0; `/tmp/zc-autoplay3.log`).
  - Perf-Regression gefunden+behoben: `GameObject.Find("Player")` pro Frame in `ContractObjective.Update` und `Camera.main` pro Frame in `ContractBoard.Update` kosteten ~2 fps (Branch 29,4/29,8 FAIL vs. `origin/main`-Baseline 32,1 PASS, `/tmp/fps-baseline.log`). Fix: Hero-Transform gecacht, Kamera statisch gecacht, Klick-Gate zuerst. Gleiches Muster in O3 (`RiskShrine.Update`) proaktiv mitgefixt.
  - `python3 scripts/check-meta.py` Exit 0, `python3 ~/scripts/agent-workspace.py check` still vor Commit.
  - Drei Builder-Seiteneffekte rever­tiert (`*Locomotion.controller`, `VendorIdle.controller`: reine LFS-Re-Serialisierung, nicht zum Goal gehörig).

## Nächste Schritte

Keine — alle drei Stufen belegt und gelandet.

## Offene Punkte

Keine.
