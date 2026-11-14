# Dev-journal — unity-sandbox

Per session: done / faced / learned / next. Against scope creep:
anything outside Q1–Q3 greybox goes to won't-have, not into the scene.

## 2026-11-14 — Greybox scene + code-owned controller (foundation)

- Done: Universal 3D (URP 17.0.4) project, `SandboxBuilder` greybox
  (ground, 6 blocks, 4 walls, light, camera, capsule player),
  hand-rolled `ThirdPersonController`; Linux player build (84 s, 91 MB)
  runs with zero errors (audio-only noise headless).
- Faced: Asset Store has no headless fetch — Starter Assets dropped in
  favor of the code-owned rig; template materials predate URP 17.
- Learned: headless batchmode builds the full loop without GUI.
- Next: trigger toy + toy/goal loop, questions written down.

## 2026-11-14 — MCP relay + keep-alive editor (agentic loop)

- Done: relay `unity-ai-relay v1.0.12` handshake (7 tools), `CommandScript`
  proof (`McpProvenCube` persists), fanned out to all harnesses;
  user service `unity-sandbox-editor.service` keeps the editor alive.
- Faced: stale sockets after editor PID died — cleaned, restarted.
- Learned: `Unity_RunCommand` needs `internal class CommandScript :
  IRunCommand` exactly; no GUI approval headless.
- Next: credit forecast, gateway check (other goal, not this one).

## 2026-11-14 — Q2/Q3 greybox + goal closeout (this goal)

- Done: `ToyTrigger` (counter/pickup/goal, `Needed = 3`) + scene wiring
  (`TriggerZone`, `Orb_0..2`, `GoalPad`) via `SandboxBuilder`;
  `VerifyGreybox` inventory proof; docs: GDD dates, questions,
  decision log, runbook, playtest protocol, run log.
- Faced: keep-alive editor holds the project lock — service stopped for
  the rebuild, restarted afterwards.
- Learned: one headless `BuildAll` covers scene + player proof for all
  three questions at once.
- Next: SFX placeholder (M3, 2026-12-05), share with friends (M4).
