---
goal: cinderreach-vfx-tooling
slug: cinderreach-vfx-tooling
branch: feat/cinderreach-vfx-tooling
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-MCP03]
status: planned
plan_refs: [.agents/goals/cinderreach-vfx-tooling/GOAL.md]
---

# Plan: cinderreach-vfx-tooling

Siehe [GOAL.md](../../goals/cinderreach-vfx-tooling/GOAL.md) für Kriterien 1–5 und Entscheidungen D1–D4.

## Schritte
1. `scripts/install-material-maker.sh` anlegen und Material Maker 1.8 CLI testen.
2. `docs/skill-vfx/build_vfx_meshes.py` anlegen und 12 UV-/VC-optimierte FBX-Meshes nach `sandbox/Assets/Art/SkillVfx/Meshes/` exportieren.
3. Material Maker `.ptex` + `docs/skill-vfx/build_vfx_textures.py` (5 Texturen) und `docs/skill-vfx/build_vfx_flipbooks.py` (3 Flipbooks + ZeroMotion) nach `sandbox/Assets/Art/SkillVfx/Textures/` ausführen.
4. Drei URP-17-HLSL-Shader (`AbilityMesh`, `AbilityFlipbook`, `AbilityGroundZone`) unter `sandbox/Assets/Art/SkillVfx/Shaders/` anlegen.
5. `docs/skill-vfx/README.md`, `sandbox/Assets/Tests/EditMode/SkillVfxToolingTests.cs` und `scripts/test_vfx_pipeline.py` anlegen, `AddressablesSetup.Ensure` sowie alle Gates (`check-meta.py`, `asset-bloat-audit.py`, `pytest`, EditMode-Tests, `goal-lint.mjs`) ausführen und per `land` abschließen.
