# Progress: cinderreach-loop-guard

Prozent: 35 %

## Erledigt

- 2026-11-17: Goal angelegt (Folge-Goal nach `arpg-session-loop`).
- 2026-11-17: Plan `.agents/plans/cinderreach-loop-guard/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: S1 result gelandet. `AutoplayResult` (schema 1, 6 Pflichtphasen), `SessionAutoplay`
  schreibt `autoplay-result.json` (`-autoplay-out`, `-autoplay-commit`), `FpsReporter.Windows`,
  `scripts/run-autoplay.sh` (Build → Lauf → FPS-Gate). Belege: EditMode 97/97 (8 neu in
  `AutoplayResultTests`), PlayMode 71/71, `VerifyDiabloZone` + `KayKitAssets.Verify` grün, `check-meta` OK,
  `bash scripts/run-autoplay.sh` Exit 0: Phasen town..complete, 0 Exceptions, Renderer D3D12 RTX 5070 Ti,
  FPS-Gate PASS (schlechtestes Fenster 47,5 fps @1280×720, 25 Fenster).

## Nächste Schritte

1. S2 nightly: `.github/workflows/autoplay-nightly.yml`, 3 `workflow_dispatch`-Läufe grün.
2. S3 probe: `G-DIABLO06` in dotfiles.

## Offene Punkte

- S2: Prüfen, ob der Runner-Dienst (`github-runner-unity.service`) das WSLg-Display (`/tmp/.X11-unix/X0`)
  und `/dev/dxg` erreicht. Sonst fällt `run-autoplay.sh` auf Xvfb (llvmpipe, ~6 fps) zurück und das
  FPS-Gate wird rot.
