# Second class run log (cinderreach-second-class S5, 2026-11-18)

Linux player on WSLg GPU, `bash scripts/run-autoplay.sh <out> -seed N -class <id>`, `-autoplay -autoplay-god`.

## Clear time per class (Normal, Hollow Abbot = last available boss)

| Class | Seed 0 | Seed 1 | Seed 2 | Mean | Phases | Exceptions | FPS min |
|---|---|---|---|---|---|---|---|
| Fenwalker | 112.9 s | 110.8 s | 109.7 s | 111.1 s | 6/6 each | 0 | 46.3 |
| Lanternbearer | 119.6 s | 122.5 s | 124.0 s | 122.0 s | 6/6 each | 0 | 46.8 |

Lanternbearer is 9.8 % slower than the Fenwalker, inside the ±25 % acceptance band. Every run cleared the fen
completely (22 kills, census „none left“). Autoplay profile: Wisps when ready, Searing Halo with 3+ husks (or
the boss) inside 3.5 m, else Lantern Bolt; melee strikes as before. God mode keeps HP out of the picture, so
this is a throughput check, not a balance verdict (plan R5). After `cinderreach-act-one` the same comparison
runs up to the Kilnmother.

## Co-op smoke, mixed classes

Host: Fenwalker with full autoplay (WSLg). Client: `--net-client -class lanternbearer`, headless, killed after
80 s.

- Host log: `Client connected: 1`, `[Class] mirror NetworkPlayerDiablo(Clone) (client 1) shows Lanternbearer`,
  `Client disconnected: 1`. Host autoplay exit 0, 6/6 phases.
- Client log: `[Class] command line: Lanternbearer`, `Starting client -> 127.0.0.1:7777`, host mirror shows
  Fenwalker, owned hero took over `Player`.
- Exceptions: host 0, client 0.

## Windows build and shotcheck

`BuildDiabloZoneWindows` OK, `stage-windows.sh` OK (`Games\cinderreach`, commit c85bb32 + S5 working tree,
121 MB), `shotcheck-windows.sh` 26 shots. New: `22_class_select.png` (both cards, Continue),
`23_lanternbearer_wisps.png`, `24_lanternbearer_halo.png`, `25_lanternbearer_bolt.png`. The Mage body sits on
the Knight rig (hat, robe, lantern light at the hand), the HUD shows Lantern Bolt / Wisps / Searing Halo /
Veil Step.

Side finding (not this goal): the Hollow Abbot's phase-2 summons render magenta in the player (default
material on a primitive capsule, no URP shader). Visible already in `21_abbot_phase2.png` before S5; fixed
separately.
