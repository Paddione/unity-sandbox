# One-Page GDD — unity-sandbox

Template: Unity's official One-Page plan (MoSCoW + done-means + milestones).
Status: scoped. Q1–Q3 defined in `docs/mechanics-questions.md`, decisions in
`docs/decision-log.md`, headless proof in `docs/q1-run-log.md`.

## Identity (S1, landed 2026-11-17)

Working title **Cinderreach** (`productName`, `cinderreach.exe`,
`builds/cinderreach-windows`, `Games\cinderreach`). Zone 1 is the
**Ashen Fen**, its hub **Lantern Hold**, its extraction the **Fen Gate**;
elites are the **Fen Brute** and the **Ash Elite** (plain husks stay
generic). Setting: gothic ashen-fen dark fantasy — torchlit camp and
crypt seal against a cold fen-night haze.

## Must-haves

- Playable greybox scene with code-owned Third-Person controller (Q1)
- One mechanics question answered per prototype (kill/pivot/continue logged)
- Trigger toy + toy-vs-goal loop in the same greybox scene (Q2/Q3)

## Should-haves

- Headless verify (`SandboxBuilder.VerifyGreybox`) green on every scene change
- Linux player build runs the scene with zero errors

## Could-haves

- Placeholder 3D (Meshy free-tier) replacing one greybox block
- ProBuilder refinement of the block line (currently primitives only)

## Won't-have-this-time

- Final art, HDRP, shipped build, live NPC dialogue
- Vertical slice (option only — see `docs/runbook.md`)

## Done-means

- [x] Core loop playable (greybox player build runs, Q1)
- [x] Two mechanics tested + decisions logged (Q2 trigger, Q3 toy/goal)
- [ ] SFX placeholder added
- [ ] Shareable build with friends

## Milestones

1. Playable core loop — 2026-11-21 (done 2026-11-14, build `greybox` runs)
2. Two mechanics tested + decisions logged — 2026-11-28 (done 2026-11-14, see `docs/decision-log.md`)
3. Sound placeholder added — 2026-12-05
4. Shared with friends — 2026-12-12

## Runbook

Stage chain lives in `docs/runbook.md`: Concept → Prototype (1–4 weeks,
exit question "is the core mechanic fun?") → Vertical Slice only as an
option if the sandbox proves fun.

## Dev-journal

Kept per session (done / faced / learned / next) in `docs/dev-journal.md`
against scope creep.
