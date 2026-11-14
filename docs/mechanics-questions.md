# Mechanics questions — unity-sandbox

One prototype, one question. Each question has an exit criterion and ends
in kill / pivot / continue (logged in `docs/decision-log.md`).
Greybox discipline: primitives only, fresh URP/Lit materials, code-owned
`ThirdPersonController` + `ToyTrigger` rig (no Asset-Store fetch required).

## Q1 — Movement feel (continue)

- Question: does the hand-rolled third-person controller feel good enough
  to carry mechanics tests (walk, sprint, jump across the block line)?
- Setup: `Greybox` scene — ground, 6 jump blocks (0.5–2 m), 4 walls,
  capsule player (`CharacterController` + `ThirdPersonController`:
  WASD/arrows, mouse look, Space jump, Shift sprint).
- Exit criterion: Linux player build runs the scene with zero errors and
  the controller bindings work (code-defined, Input System package).
- Headless proof: `docs/q1-run-log.md` (build run + `VerifyGreybox`).

## Q2 — Trigger interaction (continue)

- Question: do trigger volumes reliably detect the player for toy logic?
- Setup: `TriggerZone` (orange cube, `BoxCollider.isTrigger`) over the
  block line with `ToyTrigger` in `Counter` mode; every player entry logs
  `[Toy] Trigger zone entered (entries=N)`.
- Exit criterion: scene contains ≥ 1 trigger zone wired to `ToyTrigger`
  and the player build compiles and runs with zero errors.
- Headless proof: `SandboxBuilder.VerifyGreybox` counts the zone;
  live entry counting is a human-playtest follow-up.

## Q3 — Toy vs goal loop (continue)

- Question: does "collect 3 orbs, then the goal pad arms" read as a
  complete toy-vs-goal loop?
- Setup: 3 pickup orbs (`Orb_0..2`, yellow spheres, `ToyTrigger` in
  `Pickup` mode, `Needed = 3`) + `GoalPad` (red → green when armed,
  `ToyTrigger` in `Goal` mode). Locked goal logs `Goal locked (x/3)`,
  armed goal logs `Goal reached … Loop complete`.
- Exit criterion: scene contains ≥ 3 pickups + 1 goal pad wired to the
  shared counter, build runs with zero errors, decision logged.
- Headless proof: `VerifyGreybox` inventory + run log; full collect
  playthrough is a human-playtest follow-up (see `docs/playtest-protocol.md`).
