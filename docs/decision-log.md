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
- 2026-11-14, Q4 netcode stack → **NGO + Relay/Lobby (continue)**. Evidence: `com.unity.netcode.gameobjects@2.5.0` + transport 2.4.0 + lobby 1.3.0 + relay 1.2.0 in manifest, `VerifyMultiplayer` green, host/client startup logs clean, EditMode 6/6. Mirror (self-hosted zero-fee) kept as fallback, Fusion 2 as fastest-cloud alternative per deep-research; no new PUN 2, no Unity-hosted Multiplay dedicated servers.
- 2026-11-15, Q5 click-to-move kill-loot → **continue (headless)**. Evidence: `Diablo.unity` (Knight hero + click/attack-move rig, 3 Skeleton husks with HP, GoalPad extraction), `VerifyDiablo` green, `builds/diablo/diablo` player build Succeeded (36.6 s, 98 MB) with clean 45 s run (0 exceptions), EditMode 9/9 + PlayMode 4/4 (see `docs/diablo-run-log.md`). Greybox + NGO stack untouched (regression verifies green). One human feel session + visual-fit check stay follow-ups, not criteria (Q1–Q3 precedent).
- 2026-11-15, Q6 co-op kill-loot-extract with rarity chase → **continue (headless)**. Evidence: `DiabloZone.unity` ("Ember Crypt Approach": stairs, hall, elite alcove + chest, rubble, crypt-gate extraction), host-authoritative NGO (owner-only input, validated ServerRpc, server equip/despawn), Magic/Rare/Unique x Armor/Weapon/Helmet with weighted droptables (Normal 80/18/2, Elite 50/40/10, Chest Rare+), `VerifyDiabloZone` green, `builds/diablo-zone/diablo-zone` Succeeded (84.7 s, 99 MB) with clean host-start run (0 exceptions), EditMode 14/14 + PlayMode 10/10 (see `docs/diablo-p2-run-log.md`). Cross-machine Relay join + 5-person human playtest stay follow-ups, not criteria.
