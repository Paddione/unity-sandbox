# Cinderreach – Modellvergleich

Die [Aufnahme vor der Überarbeitung](DiabloZone-before.webp) und die [Aufnahme danach](DiabloZone-after.webp) zeigen die eingecheckte DiabloZone aus derselben Kamera. Die folgenden Bildtafeln zeigen jedes Modell links vorher und rechts danach. Einzelansichten verwenden eine orthografische Kamera mit festem Winkel, ein gerichtetes Licht und eine neutrale Umgebung. Die Modellansichten sind auf ihre jeweiligen Grenzen eingepasst. Die Storyfunde verwenden einen festen Abbildungsmaßstab.

## Formen und Materialien

- **BloodMoor** hat unter anderem verzweigte Baumkronen, gefaltete Halme, Bannerstoff mit Falten und modellierte Flammenzungen erhalten. Zonenmaterialien differenzieren Holz, Stein und Metall.
- **KayKit** verwendet überarbeitete Figurenproportionen und Kanten an Ausrüstung und Architektur. Die Materialatlanten steuern Metallanteil und Glätte. Der Barbarian-Atlas wird von Spielassets referenziert, auch wenn das Barbarian-FBX selbst im Inventar fehlt.
- **LocalArtPilot** zeigt Truhe, Banner und Gluthund mit den Materialien ihrer Spiel-Prefabs. Am Hund bleiben Lauf- und Angriffsanimation gebunden.
- **Ausrüstung** verwendet gefaste Beschläge, profilierte Platten und unterschiedliche Glätte für Stoff, Leder und Metall. Die Ansichten umfassen die Einträge aus `HeroGearModels.Ids` und `HeroGearModels.LimbKeys`.
- **Storyfunde** erhalten zur Laufzeit ihre Form. Alle Exemplare einer Fundart teilen ein Mesh. Das bestehende Sammelobjekt behält seinen Trigger.

## Bildtafeln

| Bestand | Modelle im Bild |
| --- | --- |
| [BloodMoor · 1](BloodMoor-1.webp) | Banner, BloodSplat_0, BloodSplat_1, BonePile, Brazier, BrokenPillar, Campfire, CryptSeal |
| [BloodMoor · 2](BloodMoor-2.webp) | DeadTree_0, DeadTree_1, Grass_0, Gravestone_0, Gravestone_1, Heather_0, Heather_1, Rock_0 |
| [BloodMoor · 3](BloodMoor-3.webp) | Rock_1, Rock_2, Stake_0, Stake_1, Stake_2, StoneStep, StoneWall_0, StoneWall_1 |
| [LocalArtPilot · 1](LocalArtPilot-1.webp) | LootChest, Quadruped, TornBanner |
| [Adventurers · 1](Adventurers-1.webp) | arrow_bow, bow_withString, sword_1handed, Knight, Mage, Ranger, Rogue, Rogue_Hooded |
| [Dungeon · 1](Dungeon-1.webp) | barrel_small_stack, chest, chest_gold, column, floor_tile_large, pillar, pillar_decorated, rubble_half |
| [Dungeon · 2](Dungeon-2.webp) | rubble_large, sword_shield_broken, torch_lit, wall, wall_broken, wall_corner |
| [Skeletons · 1](Skeletons-1.webp) | Skeleton_Mage, Skeleton_Minion, Skeleton_Rogue, Skeleton_Warrior |
| [Gear · 1](Gear-1.webp) | ChainGauntlets, Cryptwarden Plate, Emberbrand Blade, IronSabatons, Lantern of the Hold, LeatherBelt, LeatherBoots, LeatherGloves |
| [Gear · 2](Gear-2.webp) | Rogue's Keepsake, RopeSash, Seer's Circlet, SilkWraps, StuddedGirdle, WrappedSandals, ashen-hauberk, bastion-treads |
| [Gear · 3](Gear-3.webp) | bonewatch-visor, bulwark-carrier, cinderfang-cleaver, circle-of-dawn, cutpurse-wraps, emberwreath-crown, fen-wrap, ghostwalk-boots |
| [Gear · 4](Gear-4.webp) | glassthorn-bow, glasswright-prism, ironbound-grips, keen-cadence, kilnheart-mail, lancer-ward, menders-touch, mercy-shroud |
| [Gear · 5](Gear-5.webp) | moorvogt-anchor, nightfang-edge, ossuary-thurible, pilgrim-steps, prospectors-hood, quickdraw-belt, seismic-crown, shadowstride-vest |
| [Gear · 6](Gear-6.webp) | shardback-aegis, smugglers-fuse, vial-sash, wardens-girdle, wick-of-the-halo |
| [Story · 1](Story-1.webp) | fuhrbrief, fuhrfetzen, fuhrliste, glasscherbe, glutspur, grablicht, grabtafel, lanzenspitze |
| [Story · 2](Story-2.webp) | lieferbuch, russseite, sehglas, siegelband, speichenrad, spendenseite, tafelbruch |

## Abdeckung

Die 53 importierten Modellquellen sind im [Asset-Inventar mit Einsatzorten](../../cinderreach-asset-inventory.tsv) aufgeführt. Vier zusätzliche FBX-Dateien liefern Animationen. Das ProbeSamplingDebugMesh gehört zum Render-Pipeline-Paket. Für die 53 sichtbaren Modelle stimmten vor und nach dem Import alle 129 Vergleichszeilen überein. Verglichen wurden Renderername, lokale Mesh-ID, Meshname, Submesh-Anzahl und Materialreihenfolge, bei Skinned Meshes zusätzlich die Knochenliste und bei Clips Name und Dauer.
