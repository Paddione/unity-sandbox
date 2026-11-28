# Progress: cinderreach-vfx-tooling

Prozent: 100 %

## Erledigt
- 2026-11-28: K1 Material Maker 1.8 per `scripts/install-material-maker.sh` nach `~/.local/material-maker/1.8/` installiert und als `~/.local/bin/material-maker` verlinkt (`command -v material-maker` + Batch-CLI-Export via `--display-driver x11`).
- 2026-11-28: K2 Deterministischer Blender-4.2.9-Generator `docs/skill-vfx/build_vfx_meshes.py` erstellt und alle 12 UV-/VertexColor-optimierten VFX-FBX-Meshes (10–148 Vertices je Mesh) unter `sandbox/Assets/Art/SkillVfx/Meshes/` exportiert.
- 2026-11-28: K3 5 Material-Maker-`.ptex`-Graphen (`docs/skill-vfx/ptex/*.ptex`) via `docs/skill-vfx/build_vfx_textures.py` als PNG (`<= 512 px`, `stddev` 51.3–97.8) sowie 3 4x4-RGBA-Flipbooks (`1024x1024`: `FireBlast`, `SmokePuff`, `MireSplash`) + `VfxFlipbook_ZeroMotion.png` via `docs/skill-vfx/build_vfx_flipbooks.py` unter `sandbox/Assets/Art/SkillVfx/Textures/` erzeugt; `python3 scripts/asset-bloat-audit.py` OK (118.2 MB total).
- 2026-11-28: K4 Drei URP-17.0.4-Basisshader (`Cinderreach/VFX/AbilityMesh`, `Cinderreach/VFX/AbilityFlipbook`, `Cinderreach/VFX/AbilityGroundZone`) unter `sandbox/Assets/Art/SkillVfx/Shaders/` angelegt; `SkillVfxToolingTests` (3/3) und `ServerShaderGuardTests` (1/1) im EditMode-Lauf bestanden (`4/4 passed, 0 failed`).
- 2026-11-28: K5 Workflow- & EmberGen-Drop-in-Doku `docs/skill-vfx/README.md` angelegt; `python3 scripts/check-meta.py` OK, `python3 -m pytest scripts -q` (`67 passed in 0.63s`), `node ~/scripts/goal-lint.mjs --repo .` still.

## Nächste Schritte
Keine.

## Offene Punkte
Keine.
