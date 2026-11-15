# Diablo run log — Q5 click-to-move kill-loot (2026-11-15, headless)

## Packs (owner import)

- `Adventurers/Characters`: 6 heroes (Knight/Mage/Ranger/Rogue/Rogue_Hooded/Barbarian).
- `Skeletons/Characters`: 4 enemies (Warrior/Minion/Rogue/Mage).
- `CharacterAnimations/Rig_Medium`: 8 animation rigs (clip carriers).
- No `Models/` dirs here (unlike Dungeon/ForestNature) — `KayKitAssets`
  now tracks per-pack model dirs + counts.

## Build

- `SandboxBuilder.BuildDiabloScene`: Diablo room (tile floor 25, wall ring 24,
  4 torches, chest-dressed GoalPad), Knight hero (`Player` + CharacterController
  + `DiabloClickController` + `DiabloMelee`), 3 husks (Warrior/Minion/Rogue,
  `Husk` tag + CapsuleCollider + `EnemyHealth`).
- `SandboxBuilder.BuildDiablo`: **Succeeded in 36.6s, 98 MB**
  (`builds/diablo/diablo`, gitignored proof artifact).

## Verifies

- `VerifyDiablo`: `player=True clicker=True melee=True goal=True husks=3
  tiles=25 walls=24 light=True camera=True` — OK.
- `VerifyGreybox`: OK (Q1 rig + Q2 trigger + Q3 toy/goal present).
- `VerifyMultiplayer`: OK (unchanged Q4 layer).
- `KayKitAssets.Verify`: all 5 packs green
  (105 + 211 + 6 + 4 + 8 rigs, URP Lit + atlas on every renderer).

## Tests

- EditMode: **9 tests, 0 failures** (3 URP + 3 multiplayer + 3 `DiabloSceneConfigTests`).
- PlayMode: **4 tests, 0 failures** (`DiabloCombatTests`: click-move,
  attack-move kill, out-of-range/cooldown rejection). One real find/fix:
  `DiabloClickController` cached `DiabloMelee` in `Awake`, but sibling
  `AddComponent` order is not guaranteed — fixed with lazy resolve, rerun green.

## Run

- `./builds/diablo/diablo -batchmode -nographics`, 45 s (timeout kill, RC=124),
  0 exceptions, clean shutdown sequence (Unloaded physics, Input System
  Shutdown, ALSA/FMOD no-soundcard noise only — same as Q1/Q4 proofs).
- No per-frame game logs by design (as Q1): entry/kill/loop counting executes
  on live trigger contact (human playtest follow-up, not headless).

## Open follow-ups (not criteria)

- Visual fit of walls/tiles/figures (blind placement) — one in-editor look.
- Hero accessories (bow/sword parenting), husk idle animation via Rig_Medium clips.
- One human feel session before calling the loop fun.
