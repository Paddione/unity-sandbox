# Playtest protocol — unity-sandbox

Human 5-person playtests are follow-up, not a completion criterion.
The agent-completable proof is the headless Q1 run (build runs, input
path smoke-tested via scripted review where headless input is absent).

## Headless Q1 run (done, see `docs/q1-run-log.md`)

1. `SandboxBuilder.VerifyGreybox` opens `Greybox.unity` and asserts the
   inventory: player + `ThirdPersonController`, 6 blocks, 4 walls,
   1 trigger zone, 3 orbs, 1 goal pad.
2. Linux player build `greybox` launches headless, loads the scene,
   runs with zero errors (audio-only noise is expected headless).
3. Input path: bindings are code-defined (`Keyboard`/`Mouse` via the
   Input System package, no `.inputactions` asset); headless has no
   keyboard/mouse, so scripted key injection is not possible — the smoke
   test asserts the Input System initializes and the build logs no input
   errors. Live movement feel stays a human-playtest item below.

## Human follow-up (not a blocker)

- 5 players, 10 minutes each: walk / sprint / jump the block line,
  walk through the trigger zone, collect 3 orbs, reach the goal pad.
- Observe: do jumps read fair? Is the goal state (red → green) obvious?
- Record per player: fun (1–5), confusion points, one kill/pivot/continue
  vote for the loop. Results append to `docs/decision-log.md`.
