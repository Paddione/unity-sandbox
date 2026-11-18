# Progress: cinderreach-kindled-finale

Prozent: 100 %

## Erledigt

- Ziel und Plan angelegt.
- S1 Arenamuster (Übernahme durch opencode-Session ses_e115042b; Rebase auf v8-main f311561, Kette v4→…→V8→V9):
  - Neu: `Scripts/Enemies/KilnSectorPattern.cs` (4×90°-Sektoren, max 2 benachbart aktiv, 1,5 s Vorwarnung/2 s Brenndauer, Run-Seed-Reihenfolge, Warnung=Schaden-Geometrie, max 1 Treffer/Muster), `Tests/EditMode/KilnSectorPatternTests.cs`, `Tests/PlayMode/KindledFinaleTests.cs`.
  - Anschluss `KilnmotherController` (Solo-Kindled Phase 2 → Sektormuster statt Wellen/Felder; Beschwörungen bleiben; Tod/Respawn/Rückzug räumt auf).
- S2 Belohnung (selber Branch):
  - Neu: `Scripts/Loot/KindledFinaleReward.cs` (einmaliger Anspruch auf klassenkompatible Rare-Waffe mit Kindled-Affixen; volle Tasche hält Anspruch offen; Auszahlung+Verbrauch atomar mit Rollback; Reload-Angebot im Abschlussfenster; keine rückwirkenden Ansprüche), `Tests/PlayMode/KindledFinaleRewardTests.cs`.
  - Anschlüsse `KilnmotherController` (Reward-Hook), `SessionComplete` (Angebot), `ItemDatabase.CreateKindledFinaleWeapon`.
  - Save v9 additiv (`SaveGame`/`SaveMigrations.V8ToV9`/`SaveService`/`RunProfile` + `SaveMigrationTests` v9, V4→V9-/V9-Roundtrip-Tests).
- S3 Integration/Nachweis:
  - `bash scripts/run-unity-tests.sh` Exit 0: EditMode 281/281, PlayMode 136/136 (`/tmp/kf-full2.log` + `/tmp/kf-edit.log`; 2 #12-Migrationstests auf `CurrentVersion` gehoben; Klammer-Folgeschaden aus Merge behoben).
  - `SandboxBuilder.VerifyDiabloZone` Exit 0 (`/tmp/kf-verify.log`; keine Szenenänderung nötig — Mechanik + Reward, kein neuer Bau).
  - Normal: `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-kindled-finale-normal" -seed 0` Exit 0, fps-gate PASS (36,2).
  - Kindled: `bash scripts/run-autoplay.sh "artifacts/autoplay/kindled-finale-kindled" -seed 1 -difficulty Kindled` Exit 0: DONE phases=9 huskKills=50 bossKills=1 phaseTwo=1 overlay=2 exceptions=0, fps-gate PASS (38,9).
  - Manuelle Läufe pro Klasse (Normal + Kindled, je Fund bis Verbesserung): `KindledFinaleRewardTests` (PlayMode, grün — beide Klassen, Save-Fehler, Reload, wiederholter Boss-Tod) + Kindled-Autoplay-Vollpass.
  - `python3 scripts/check-meta.py` Exit 0, `agent-workspace check` still vor Commit.

## Nächste Schritte

Keine — alle drei Stufen belegt und gelandet.

## Offene Punkte

Keine.
