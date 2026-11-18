# Progress: cinderreach-loop-guard

Prozent: 65 %

## Erledigt

- 2026-11-17: Goal angelegt (Folge-Goal nach `arpg-session-loop`).
- 2026-11-17: Plan `.agents/plans/cinderreach-loop-guard/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: S1 result gelandet. `AutoplayResult` (schema 1, 6 Pflichtphasen), `SessionAutoplay`
  schreibt `autoplay-result.json` (`-autoplay-out`, `-autoplay-commit`), `FpsReporter.Windows`,
  `scripts/run-autoplay.sh` (Build → Lauf → FPS-Gate). Belege: EditMode 97/97 (8 neu in
  `AutoplayResultTests`), PlayMode 71/71, `VerifyDiabloZone` + `KayKitAssets.Verify` grün, `check-meta` OK,
  `bash scripts/run-autoplay.sh` Exit 0: Phasen town..complete, 0 Exceptions, Renderer D3D12 RTX 5070 Ti,
  FPS-Gate PASS (schlechtestes Fenster 47,5 fps @1280×720, 25 Fenster).

- 2026-11-18: S2 nightly gelandet (`4149cee`). `autoplay-nightly.yml` (cron `30 2 * * *`, Dispatch,
  Runner `unity-licensed`), Host-Ablage `~/unity-sandbox-autoplay/<datum>[-<run_id>]/`, 30 Tage Retention.
  3 aufeinanderfolgende `workflow_dispatch`-Läufe grün (Erfolgskriterium 1):
  https://github.com/Paddione/unity-sandbox/actions/runs/44219811822 ,
  https://github.com/Paddione/unity-sandbox/actions/runs/44220861693 ,
  https://github.com/Paddione/unity-sandbox/actions/runs/44222093327 .
  Je Lauf: exit 0, 0 Exceptions, 6/6 Phasen, 131 s, schlechtestes FPS-Fenster 48,0/48,4/48,5 (WSLg-GPU
  aus dem Runner-Dienst bestätigt).

## Nächste Schritte

1. S3 probe: `G-DIABLO06` in dotfiles.

## Offene Punkte

- S3 (`G-DIABLO06`) und Fehlertest (Erfolgskriterium 5) stehen aus.
