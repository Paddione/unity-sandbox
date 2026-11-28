# Cinderreach Skill VFX Pipeline (O1 + Flipbooks)

Reproducible stylized dark-fantasy ability VFX toolchain for `unity-sandbox` (URP 17.0.4, Unity 6000.0.85f2, Linux-`llvmpipe` portable).

## Tool Stack

1. **Blender 4.2.9 LTS (`~/.local/blender/current/blender` / MCP `localhost:9876`)**
   - Generates 12 UV0- and vertex-color-authored low-poly (`<= 1500` verts) VFX meshes in `sandbox/Assets/Art/SkillVfx/Meshes/`:
     ```bash
     ~/.local/blender/current/blender -b --factory-startup -P docs/skill-vfx/build_vfx_meshes.py -- --out sandbox/Assets/Art/SkillVfx/Meshes
     ```
   - Renders 3 deterministic 4x4 RGBA flipbook atlases (`1024x1024`, row-major top-left to bottom-right) plus `VfxFlipbook_ZeroMotion.png` in `sandbox/Assets/Art/SkillVfx/Textures/`:
     ```bash
     ~/.local/blender/current/blender -b --factory-startup -P docs/skill-vfx/build_vfx_flipbooks.py -- --out sandbox/Assets/Art/SkillVfx/Textures
     ```

2. **Material Maker 1.8 CLI (`scripts/install-material-maker.sh` -> `~/.local/bin/material-maker`)**
   - Install or verify:
     ```bash
     bash scripts/install-material-maker.sh
     ```
   - Exports 5 procedural noise and radial mask `.ptex` graphs (`docs/skill-vfx/ptex/*.ptex`) to `<= 512 px` PNGs in `sandbox/Assets/Art/SkillVfx/Textures/`:
     ```bash
     python3 docs/skill-vfx/build_vfx_textures.py --out sandbox/Assets/Art/SkillVfx/Textures
     ```

3. **Unity URP Shaders (`sandbox/Assets/Art/SkillVfx/Shaders/`)**
   - `Cinderreach/VFX/AbilityMesh`: dual UV scroll, optional polar UVs (`_UsePolar`), noise dissolve with glowing `_EdgeColor`, Fresnel rim, vertex/particle color support, configurable blend modes (`_SrcBlend`, `_DstBlend`), HDR `_Intensity`.
   - `Cinderreach/VFX/AbilityFlipbook`: 4x4 row-major flipbook playback (`_Rows`, `_Cols`, `_FPS`, or `_UseManualFrame` + `_Frame`), smooth frame crossfade, and optional EmberGen-compatible RG motion-vector UV warping (`_MotionTex`, `_MotionStrength`).
   - `Cinderreach/VFX/AbilityGroundZone`: radial telegraph rim (`_RimColor`, `_RimWidth`), animated inner swirl noise, pulse, and `_Progress` reveal/fade.

## EmberGen (O2) Drop-in Workflow

The `Cinderreach/VFX/AbilityFlipbook` shader uses EmberGen's standard 4x4 row-major top-to-bottom layout:
1. Export a 4x4 RGBA color/emissive sheet (1024x1024 or 2048x2048 max per `AssetPipelineImportRules`) and optionally a 4x4 RG motion-vector sheet from EmberGen.
2. Place them under `sandbox/Assets/Art/SkillVfx/Textures/` (never under `Assets/AddressableAssets/SkillArt/`, which forces UI Sprite import).
3. Assign the color sheet to `_MainTex` and the motion-vector sheet (imported with sRGB disabled / Linear) to `_MotionTex`, then set `_MotionStrength` to `0.02–0.06`.
