# Progress: cinderreach-loop-guard

Prozent: 100 %

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

- 2026-11-18: S3 probe gelandet (dotfiles `892f79e4`): `G-DIABLO06` in `scripts/health_probes/diablo_unity.py`
  (Priority A, Kategorie Diablo-Game, Baseline 0: jeder Verstoß = critical), `NON_AGENT_GOALS` erweitert,
  7 neue Tests (achieved, je ein critical-Fall für exit/Phasen/Exceptions/FPS, zu alt, Dauer > 1,5× Median,
  unlesbar, unknown ohne Ablage/abseits des Hosts). `pytest scripts/test_diablo_unity_health.py` 20 passed,
  `pytest scripts/` 1105 passed, `check` still. Live-Collector: `G-DIABLO01`–`06` achieved.
- 2026-11-18: Fehlertest (Kriterium 5): Branch `test/g06-invulnerable-boss` (Boss ignoriert Schaden),
  Dispatch https://github.com/Paddione/unity-sandbox/actions/runs/44223809241 → exit 1, Phasen bis zone2,
  369 s. Probe: `G-DIABLO06 critical 3` („exit=1, Phasen fehlen: boss/complete, Dauer 369 s > 1.5x Median 131 s“).
  Fehler war nie auf `main`. Neuer Lauf auf `main` https://github.com/Paddione/unity-sandbox/actions/runs/44225225804
  → `G-DIABLO06 achieved 0` (6 Phasen, 111 s).

Kriterien-Abgleich: 1 (Nachtlauf + 3 grüne Läufe) S2, 2 (`G-DIABLO06` Bedingungen, Priority A, Unknown-Anker) S3,
3 (FPS-Gate im Lauf, Unterschreiten → critical) S1 + Test `FPS 5.6`, 4 (Unit-Tests + `NON_AGENT_GOALS`) S3,
5 (bewusster Fehler färbt rot, entfernt) Fehlertest oben.

## Nächste Schritte

- Keine. Folge-Goals laut `.agents/goals/ROADMAP.md`: `cinderreach-replayability`, `cinderreach-second-class`.

## Offene Punkte

Keine.
