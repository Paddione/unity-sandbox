# KayKit environment assets

Imported free editions by Kay Lousberg, licensed CC0 (original `License.txt`
included in each pack):

- Forest Nature 1.0: 105 models, `Assets/ThirdParty/KayKit/ForestNature/Models`.
- Dungeon 1.1: 211 models, `Assets/ThirdParty/KayKit/Dungeon/Models`.
- Adventurers: 6 hero models, `Assets/ThirdParty/KayKit/Adventurers/Characters`
  (Knight, Mage, Ranger, Rogue, Rogue_Hooded, Barbarian; per-class materials).
- Skeletons: 4 enemy models, `Assets/ThirdParty/KayKit/Skeletons/Characters`
  (Warrior, Minion, Rogue, Mage; shared skeleton material).
- CharacterAnimations: 8 `Rig_Medium` animation rigs (clip carriers, no meshes).

Sources, archive SHA-256 hashes and counts: [kaykit-assets.json](kaykit-assets.json).
Only the original `fbx(unity)` models, each pack's gradient atlas, overview
image and license are imported. Other engine formats and paid extras are omitted.

Drag the FBX models from the Project window into a scene. Each model uses its
pack's shared `KayKitURP.mat` with the original atlas and URP Lit shader.
Materials are matte and double-sided for foliage. No gameplay colliders are
added automatically. Existing gameplay scenes and build settings are unchanged.

After replacing upstream models, run `Sandbox > KayKit > Configure URP Materials`.
To check all 326 models plus 8 animation rigs for meshes, renderers, URP materials and atlas references,
run `Sandbox > KayKit > Verify Imported Assets`, or invoke
`-executeMethod KayKitAssets.Verify` in Unity batch mode.
