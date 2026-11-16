---
goal: unity-diablo-d2-combat-feel
slug: unity-diablo-d2-combat-feel
health_goals: none (einmaliger Gameplay-Prototyp wie P2; Verifikation über EditMode/PlayMode-Suiten, VerifyDiabloZone und Windows-Live-Playtests)
plan_refs: [.agents/plans/unity-diablo-d2-combat-feel/tasks.md]
---

# Goal: D2 combat feel for the Blood Moor (solo-first)

## Objective

Make solo moment-to-moment combat in the Blood Moor feel like Diablo 2
Act 1 — weighty single hits with audible, visible consequences — while
co-op keeps working unchanged.

## Erfolgskriterien (messbar)

1. Every landed swing plays animation + impact sound + hit flash and knocks
   the husk back; proven by a PlayMode test (swing counter + measured
   displacement), not just by watching.
2. Husks retaliate: telegraphed wind-up, melee counterattack that reduces
   player HP; player death respawns at the Rogue Encampment. Co-op
   join/kill/loot keeps passing its existing proofs.
3. The currently targeted husk shows a D2-style HP bar (no floating damage
   numbers — D2 doesn't have them).
4. Strikes connect reliably at melee range: small step-in toward the cursor
   on strike so open-moor whiffs feel fair, not broken.
5. Full suites green (EditMode + PlayMode), `VerifyDiabloZone` green, and a
   Windows GPU playtest with a one-sentence D2-feel confirmation.

## Nicht-Ziele

Potions/vendors/economy, new zones, classes/skills, procedural content,
procedural art changes (KayKit stays), damage numbers, screenshake,
co-op-specific systems (shared loot, revive). Candidates for follow-up
goals, not this one.

## Abhängigkeiten / Randbedingungen

- Unity 6000.0.85f2, KayKit assets only, no new art pipeline.
- Sound-asset sourcing is decided at plan time (repo has no audio pipeline
  yet).
- All builds via `scripts/run-unity-build.sh`; Conventional Commits,
  targeted pathspecs, `land: ff`, no PRs.
- Never break the `NetworkPlayerDiablo` prefab contract (owner authority,
  Ignore Raycast, Equipment/Inventory/Health/UI components).
