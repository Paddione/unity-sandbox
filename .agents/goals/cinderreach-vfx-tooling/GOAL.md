# Goal: cinderreach-vfx-tooling

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-MCP03]
plan_refs: [.agents/plans/cinderreach-vfx-tooling/tasks.md]

## Objective
Die reproduzierbare Stylized-Dark-Fantasy-VFX-Pipeline fuer Cinderreach steht bereit: Material Maker 1.8 (CLI) ergaenzt die bestehende Blender-4.2.9-MCP-Bridge, erzeugt prozedurale Noise-/Masken-Texturen und Blender liefert UV-optimierte Low-Poly-VFX-Meshes sowie 4x4-Flipbooks. Drei portierbare URP-Basisshader (Mesh-VFX, EmberGen-kompatibler Flipbook-Shader inkl. Motion-Vector-Slot, Ground-Zone-Shader) sind in Unity importiert, kompiliert und getestet, sodass alle 18 Klassen-Abilities darauf aufbauen koennen.

## Ist-Befunde
- B1 Blender 4.2.9 LTS laeuft bereits als systemd-User-Service (`blender-mcp-host.service`, `localhost:9876`, Goal `blender-mcp-harnesses` bei 100 %, `G-MCP03`).
- B2 `unity-sandbox/sandbox/` nutzt Unity `6000.0.85f2`, `com.unity.render-pipelines.universal: 17.0.4`, `com.unity.shadergraph: 17.0.4`; `com.unity.visualeffectgraph` ist bewusst ausgespart (Linux-`llvmpipe`-Portabilitaet, `VfxBuilder.cs`).
- B3 `ZoneAtmosphere.cs` aktiviert URP Bloom (`bloomIntensity` 0.6–1.2, `bloomThreshold` 0.65–0.9); `AssetPipelineImportRules.cs` erzwingt UI-Sprite-Import unter `Assets/AddressableAssets/SkillArt/`, laesst `Assets/Art/` bis 2048 px frei.
- B4 Material Maker 1.8 (`https://github.com/RodZill4/material-maker/releases/download/1.8/material_maker_1_8_linux.tar.gz`) und EmberGen sind nicht installiert; EmberGen ist kommerzielle GUI-Software ohne freie Headless-Distribution.

## Entscheidungen
- D1 O2/EmberGen (Eigner): O1 voll + Blender/Material-Maker-Flipbooks mit EmberGen-kompatiblem Unity-Flipbook-Shader (`Rows x Cols`, Crossfade, optional `_MotionTex`-Motion-Vector-Distortion).
- D2 Stil (Eigner): Stylized Dark-Fantasy passend zu KayKit und `DiabloPalette`.
- D3 Shader-Format (Eigenentscheidung): Handgeschriebene URP-17-HLSL-`.shader`-Dateien (`SRPBatcher`-kompatibel, `UniversalPipeline`, kein Compute-Shader) statt fragiler JSON-`.shadergraph`-Generierung; 100 % headless- und `llvmpipe`-sicher.
- D4 Ablage (Eigenentscheidung): VFX-Quellassets unter `sandbox/Assets/Art/SkillVfx/{Meshes,Textures,Shaders,Materials}/`, damit `AssetPipelineImportRules` sie nicht zu UI-Sprites umwandelt.

## Erfolgskriterien
1. Material Maker 1.8 installiert und per Skript reproduzierbar (`scripts/install-material-maker.sh`), CLI ausfuehrbar — Nachweis: `command -v material-maker` + CLI-Smoke-Export erzeugt gueltige PNG-Datei (`> 0 B`, nicht einfarbig).
2. Deterministischer Blender-VFX-Mesh-Generator (`docs/skill-vfx/build_vfx_meshes.py`) erzeugt alle 12 UV-/VertexColor-optimierten VFX-FBX-Meshes unter `sandbox/Assets/Art/SkillVfx/Meshes/` — Nachweis: Generatorlauf Exit 0 + 12 `.fbx`-Dateien + Unity-EditMode-Test prueft UV0 und Low-Poly-Vertex-Budget (`<= 1500` Vertices/Mesh).
3. Prozedurale Noise-/Masken-Texturen (5 Stueck, `<= 512 px`) aus Material Maker `.ptex` sowie 3 stilisierte 4x4-Flipbooks (`1024x1024` RGBA: `FireBlast`, `SmokePuff`, `MireSplash`) unter `sandbox/Assets/Art/SkillVfx/Textures/` erzeugt — Nachweis: `docs/skill-vfx/build_vfx_textures.py` + `docs/skill-vfx/build_vfx_flipbooks.py` Exit 0, Pixel-Varianz-Check + `python3 scripts/asset-bloat-audit.py` gruen.
4. Drei URP-Basisshader (`Cinderreach/VFX/AbilityMesh`, `Cinderreach/VFX/AbilityFlipbook`, `Cinderreach/VFX/AbilityGroundZone`) unter `sandbox/Assets/Art/SkillVfx/Shaders/` vorhanden, ohne Kompilierfehler und mit `ServerShaderGuardTests`-Konformitaet — Nachweis: `SkillVfxToolingTests` (EditMode) gruen + `ServerShaderGuardTests` gruen.
5. Doku (`docs/skill-vfx/README.md`) beschreibt O1-Workflow und EmberGen-Flipbook-Drop-in; Repo-Gates gruen — Nachweis: `python3 scripts/check-meta.py`, `python3 -m pytest scripts -q`, `PLATFORMS=EditMode TEST_FILTER=SkillVfxToolingTests bash scripts/run-unity-tests.sh`, `node ~/scripts/goal-lint.mjs --repo .`.

## Nicht-Ziele
- Noch keine Umstellung der 18 Klassen-Abilities in `DiabloSkills.cs`/`ClassKitSkills.cs` (folgt in `cinderreach-class-ability-vfx`).
- Keine Installation von `com.unity.visualeffectgraph` und kein Kauf/Install von EmberGen.
- Keine Aenderung der 7 bestehenden `SkillArt/`-Dateien (`G-DIABLO02`).

## Abhängigkeiten
- `blender-mcp-harnesses` (abgeschlossen, 100 %, `G-MCP03`).
