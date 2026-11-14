# unity-sandbox

Unity 6 (URP) 3D sandbox for testing gameplay mechanics with agentic tooling.
Greybox prototypes, one question per prototype, kill/pivot/continue, 5-person playtests.

## Stack

- Unity 6 via Hub, Universal 3D template (URP, never mix pipelines)
- Git + Git LFS, only `Assets/` + `ProjectSettings/` versioned
- Starter Assets Third-Person as player basis
- Agent control via official Unity MCP + AI Gateway; Unity AI bundle credits for in-editor generation only
- CI via GameCI (`unity-test-runner@v4`, `testMode: all`)

## Tracking

Goals live in the dotfiles repo (`.agents/goals/`):

- `unity-sandbox-foundation` — project, LFS, Starter Assets, CI
- `unity-agentic-loop` — MCP wiring, credit forecast, human review
- `unity-mechanics-testbed` — greybox prototypes, playtests, One-Page GDD

See [docs/one-page-gdd.md](docs/one-page-gdd.md) for the current plan.

## Status

Skeleton only. Unity 6 project + first greybox scene still open (see foundation goal).
