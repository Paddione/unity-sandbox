# Act I run log (cinderreach-act-one S5, 2026-11-18)

Linux player on WSLg GPU, `bash scripts/run-autoplay.sh <out> -seed N [-difficulty kindled | -class lanternbearer]`,
`-autoplay -autoplay-god`. A full run is now Lantern Hold → Ashen Fen → Fen Gate → Sunken Ossuary → Hollow Abbot
→ Kiln Gate → Cinder Kiln → The Kilnmother → waypoint home. Result phases:
`town, zone1, gate, zone2, boss, zone3, actboss, complete`, plus `actOneComplete`.

## Matrix (code before the perf fix, phases and durations)

| Run | Seed 0 | Seed 1 | Seed 2 | Phases | Exceptions | Act I |
|---|---|---|---|---|---|---|
| Normal, Fenwalker | 172.1 s | 164.8 s | 167.3 s | 8/8 each | 0 | done |
| Kindled, Fenwalker | 290.2 s | 207.5 s | 212.9 s | 8/8 each | 0 | done |
| Normal, Lanternbearer | 171.4 s | 175.8 s | 180.6 s | 8/8 each | 0 | done |

Every run: Kiln cleared with 26 kills, Kilnmother killed with phase 2, Act I screen shown, waypoint travel Kiln →
Lantern Hold. Kilnmother HP: 320 on Normal, 576 on Kindled (×1.8).

Second-class acceptance up to the Kilnmother: Fenwalker mean 168.1 s, Lanternbearer mean 175.9 s, +4.6 %
(inside ±25 %).

## FPS gate and the perf fix

On that code two runs failed the gate (worst window 26.7 fps Kindled seed 2, 28.6 fps Lanternbearer seed 0); the
low windows sat in the Kiln fight and the kill burst before the Kilnmother. Causes found and fixed:

1. Every enemy, boss, field and shrine searched the whole scene with `GameObject.Find("Player")` in its own
   Update, about 50 searches per frame once the Kiln was populated (the Fen fight also dropped from about 48 to
   about 37 fps after S3). `HeroLocator` searches once per frame for everyone.
2. Each corpse is a full animated model for 10 s; the Kiln kills 26 enemies in seconds. Corpse Animators stop
   after the 1.5 s death take, and at most 16 corpses lie at once (the oldest starts sinking).

Final code, the previously weakest runs again:

| Run | Duration | Phases | Exceptions | Worst FPS window |
|---|---|---|---|---|
| Normal seed 1 | 164.5 s | 8/8 | 0 | 36.5 |
| Kindled seed 0 | 220.3 s | 8/8 | 0 | 36.9 |
| Lanternbearer seed 0 | 181.6 s | 8/8 | 0 | 38.6 |

## Balance finding for the owner playtest

The Kiln arrival (`KilnLayout.KilnArrival`, inside the thrall ring) puts the hero within aggro range of two packs
at once. The shotcheck had to keep the hero alive for its Kiln shots. Autoplay in god mode cannot judge this;
it is a candidate for the playtest, not changed here.
