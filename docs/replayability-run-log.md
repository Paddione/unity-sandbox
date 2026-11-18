# Replayability run log (cinderreach-replayability S5, 2026-11-18)

Linux player, WSLg GPU (RTX 5070 Ti, D3D12), `bash scripts/run-autoplay.sh <out> -seed N [-difficulty kindled]`,
`-autoplay -autoplay-god` (god mode only for determinism, disclosed in the log).

## Runs

| Difficulty | Seed | Layout (husks/elite/brute/props) | Exit | Exceptions | Duration | FPS min | Abbot HP |
|---|---|---|---|---|---|---|---|
| Normal | 0 | 2/2/2/1 | 0 | 0 | 111 s | 48.7 | 150 |
| Normal | 1 | 0/0/1/2 | 0 | 0 | 130 s | 49.1 | 150 |
| Normal | 2 | 2/1/0/2 | 0 | 0 | 110 s | 49.6 | 150 |
| Kindled | 0 | 2/2/2/1 | 0 | 0 | 124 s | 50.7 | 270 |
| Kindled | 1 | 0/0/1/2 | 0 | 0 | 141 s | 49.0 | 270 |
| Kindled | 2 | 2/1/0/2 | 0 | 0 | 138 s | 54.6 | 270 |

All six runs reach town, zone1, gate, zone2, boss and complete.

## Drops over three runs per difficulty

| Difficulty | Magic | Rare | Unique | Unique share | Gold dropped |
|---|---|---|---|---|---|
| Normal | 40 | 11 | 5 | 8.9 % | 943 |
| Kindled | 22 | 8 | 2 | 6.3 % | 1256 |

The sample is too small to show the Kindled unique bonus: the god-mode autoplay kills a different number of
enemies per run (Kindled runs killed fewer, so fewer drops), and one unique more or less moves the share by
several points. The rate itself is pinned by `DifficultyTests.UniqueMultiplier_DoublesUniqueShare`
(6000 Elite rolls: about 10 % on Normal, about 18 % on Kindled, table 10/100 vs 20/110). Gold per pile
scales ×1.5 (`DifficultyTests.Scaling_RoundsAndKeepsFloors`). Balance judgement stays with the owner
playtest (plan risk R3).

## Stash

`StashChest` stands in Lantern Hold next to the vendor (`VerifyDiabloZone` `stash=True`). Store/take,
the 40-slot limit, the stash window and the save roundtrip are covered by `StashChestTests` and
`SaveMigrationTests.RunProfile_SurvivesSaveRoundtrip`.

## Nightly

`autoplay-nightly.yml` now runs Normal and Kindled with the weekday seed and stores both under
`~/unity-sandbox-autoplay/<date>` and `<date>-kindled`. dotfiles `G-DIABLO06` judges the newest run per
difficulty.

Live check after landing: dispatch https://github.com/Paddione/unity-sandbox/actions/runs/44237280581 (success),
`G-DIABLO06 achieved 0` with „Kindled …-kindled: 6 Phasen, 125 s; Normal …: 6 Phasen, 110 s“.

