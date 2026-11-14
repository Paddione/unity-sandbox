# Decision log — unity-sandbox

Append-only. One line per decision: date, question, verdict
(continue / pivot / kill), evidence.

- 2026-11-14, Q1 movement feel → **continue**. Evidence: greybox scene
  (ground, 6 blocks, 4 walls, capsule player with code-owned
  `ThirdPersonController`), Linux player build `greybox` runs the scene
  with zero errors (see `docs/q1-run-log.md`). Hand-rolled controller
  kept over Starter Assets (Asset Store has no headless fetch).
- 2026-11-14, Q2 trigger interaction → **continue**. Evidence: `TriggerZone`
  (`ToyTrigger` counter mode) in `Greybox.unity`, `VerifyGreybox`
  inventory green, player build compiles and runs with zero errors.
- 2026-11-14, Q3 toy vs goal loop → **continue**. Evidence: 3 orbs
  (`ToyTrigger` pickup mode, `Needed = 3`) + `GoalPad` (goal mode,
  red → green when armed) in `Greybox.unity`, `VerifyGreybox` inventory
  green, player build runs with zero errors. Full collect playthrough
  stays a human-playtest follow-up, not a completion criterion.
- 2026-11-14, Vertical Slice → **deferred (option only)**. Evidence:
  sandbox proves fun-first per `docs/runbook.md`; no slice work started.
