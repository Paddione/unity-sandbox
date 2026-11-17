# Blood-moor starter art pack (free-local, 2026-11-17)

Art bible (from `DiabloPalette.cs`): gothic blood-moor dark fantasy —
charred stone panels, ember-gold trim, parchment text, blood-red life.
Mood: torchlight ember key with cool moor-night shadow bounce.
Palette: `#14101A` crypt · `#3A2E22` bronze · `#F2B134` ember ·
`#E8D9BE` parchment · `#0B0910` abyss · `#B31217` blood.

## Shipped sprites (`sandbox/Assets/Resources/MenuArt/`, 256px RGBA)

Rebuilt from one script each; rerun to reproduce exactly.

- 9 item icons (existing): ShortSword, HeavyAxe, Dagger, ClothTunic,
  LeatherVest, Breastplate, Hood, Helm, Circlet —
  `docs/menu-art/build_icons.py` (`--final`).
- 3 dungeon props (new): TorchSconce, LootChest, BloodBanner —
  `docs/menu-art/build_props.py` (`--final`, EEVEE 64sp, transparent).
  Fix history: chest lid rotated to horizontal, banner cloth moved in
  front of pole, per-prop hide during framing, flame `#E84E00` @ 1.5.

## Reference concepts (`docs/menu-art/refs/`, NOT shipped)

Per visual-coding rules AI images are look targets only, never Unity
assets: `blood-moor-keyart.jpg` (crypt mouth on the moor),
`hero-knight-portrait.jpg`, `husk-warrior-portrait.jpg`.

## Agentic resources used

Blender `bpy_helpers` renders, `craft-game-floors` art-direction floor,
`game-3d-visuals` direction only (no workflow pipeline — no workflow tool
in session), Unity MCP not needed (no scene mutation this pass).

## Blocked: Unity AI paid generation

Org `unity_9ADFCB8F33F44554CC95` has 0/0 credits, no default org set.
`unity ai` paid steps (sprite sheets, PBR, 3D, SFX) queue behind a credit
top-up + `unity cloud orgs set-default`. Re-quote then.
