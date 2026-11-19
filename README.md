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

The current state of the game (Cinderreach) and the starting point for new work is
[.agents/goals/CINDERREACH.md](.agents/goals/CINDERREACH.md). Active goals sit next to it under
`.agents/goals/`. Tooling goals for this project (foundation, agentic loop, mechanics testbed, health
probes) live in the dotfiles repo (`~/.agents/goals/`).

See [docs/one-page-gdd.md](docs/one-page-gdd.md) for the game design.

## Status

Skeleton only. Unity 6 project + first greybox scene still open (see foundation goal).
