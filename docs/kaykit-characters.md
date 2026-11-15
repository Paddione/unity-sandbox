# KayKit characters for the action-RPG prototype

Free CC0 editions by Kay Lousberg, matching the existing KayKit Dungeon and
Forest Nature packs:

| Pack | Imported content | Project folder |
| --- | --- | --- |
| Adventurers 2.0 | Barbarian, Knight, Mage, Ranger, Rogue and hooded Rogue variant; 31 accessories | `Assets/ThirdParty/KayKit/Adventurers` |
| Skeletons 1.1 | Skeleton Mage, Minion, Rogue and Warrior; 13 accessories | `Assets/ThirdParty/KayKit/Skeletons` |
| Character Animations 1.1 | Eight Rig_Medium FBX sets: melee, ranged/spells, general, movement basic/advanced, simulation, special and tools | `Assets/ThirdParty/KayKit/CharacterAnimations` |

Each pack includes its original license. Source URLs, archive versions and
SHA-256 hashes are in [kaykit-characters.json](kaykit-characters.json).
Only FBX models/animations, textures, licenses and character overview images
are imported. Paid extras and alternative engine formats are excluded.

Characters use skinned meshes, valid Generic avatars and individual URP Lit
atlas materials. The medium characters and animation sets share KayKit's
Rig_Medium skeleton. These are Generic rigs, not Unity Humanoid avatars;
retargeting to unrelated character skeletons needs additional setup.

Drag a model from a pack's `Characters` folder into a scene. Expand an animation
FBX to select its clips for an Animator Controller. Animators, combat logic,
AI, colliders, weapon attachment and network spawning are project integration
work; importing these packs does not change the existing gameplay scenes.

Run `Sandbox > KayKit > Configure Characters` after replacing upstream files.
`KayKitAssets.Verify` (also run by `agent-workspace.py land`) verifies the
environment packs plus character counts, skinned meshes/bones, avatars,
URP atlas materials and nonempty animation sets.
