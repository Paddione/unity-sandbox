# Progress: cinderreach-replayability

Prozent: 15 %

## Erledigt

- 2026-11-17: Goal angelegt (Folge-Goal nach `arpg-session-loop`).
- 2026-11-17: Plan `.agents/plans/cinderreach-replayability/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: Startbedingung erfüllt (`arpg-session-loop` und `cinderreach-loop-guard` 100 %).
- 2026-11-18: S1 save-v2 gelandet. `SaveGame` v2 (`difficultyUnlocked`, `difficulty`, `runSeed`, `stash`),
  `SaveMigrations` (v1 → v2, Backup `*.v1.bak` vor dem ersten v2-Schreiben, neuere Version wird nicht
  geladen und bleibt unberührt), `Stash` (40 Plätze), `RunProfile` als Laufzeit-Halter der v2-Felder.
  Belege: EditMode 106/106 (9 neu in `SaveMigrationTests`, `FutureVersion_NotLoadedAndLeftUntouched`
  ersetzt den alten Corrupt-Pfad), PlayMode 71/71, `VerifyDiabloZone` + `KayKitAssets.Verify` grün,
  `check-meta` OK, `run-autoplay.sh` Exit 0 (6/6 Phasen, 0 Exceptions, FPS-Minimum 49,1).

## Nächste Schritte

1. S2 affix und S3 layouts (parallel möglich, disjunkt).

## Offene Punkte

- S2 bis S5 offen.
