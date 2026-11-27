# Cinderreach-Asset-Inventar

[Inventar als TSV](cinderreach-asset-inventory.tsv). Jede Zeile führt einen Unity-Assetpfad, dessen Herkunft und die Einstiegspunkte auf, die ihn referenzieren. Die Zeilen enthalten auch technische Abhängigkeiten wie Shader, MonoScripts und Eingabeprofile.

| Einstiegspunkt | Abdeckung |
| --- | --- |
| `sandbox/Assets/Scenes/DiabloZone.unity` | Cinderreach-Player mit eingebetteten Zonen, Helden, Gegnern, Licht, Effekten und Menü |
| `sandbox/Assets/Scenes/Diablo.unity` und `sandbox/Assets/Scenes/Greybox.unity` | Zusätzlich aktivierte Projekt- und Testspiel-Szenen mit eigenen Zeilenmarkierungen |
| `Resources` und `StreamingAssets` | Assets für dynamisches Laden einschließlich Menü-Icons, Skill-Effekten, Netzwerk-Prefabs, Animation und Heldenkleidung |
| Build-Konfiguration, Grafik, Qualität und Player-Preloads | Unity- und Paketabhängigkeiten, einschließlich URP-Assets |

`Assets/ThirdParty/KayKit` enthält die verwendeten Teile der KayKit-Pakete. Die übrigen Projektpfade gehören zum Cinderreach-Projekt. Unter Ordner `Packages` steht die jeweilige Paket-ID als Herkunft. Paket- und Projektassets können mehrere Einstiegspunkte haben.

Der Export folgt Unitys transitiven Asset-Referenzen. Ein `Resources`-Eintrag benennt eine verfügbare Laufzeitressource. Ob er in einer bestimmten Spielsitzung tatsächlich geladen wird, hängt von Szenario, Menüpfad und Klasse ab. Unitys eingebaute Geometrie und prozedural erzeugte Renderobjekte haben keinen eigenen importierten Dateipfad. Hierzu gehören die Würfelbasis für Ausrüstungsteile und die zur Laufzeit erzeugten Unique-Meshes aus `sandbox/Assets/Scripts/Classes/HeroGearModels.cs`. Ein Player-Build kann Shader-Varianten entfernen und weitere interne Engine-Daten hinzufügen.

## Laufzeit-Aufrufer

- `DiabloMenuArt` löst Menü- und Gegenstandssprites unter `Resources/MenuArt` auf.
- `DiabloSkills`, `DiabloProjectile` und die Darstellungs-VFX laden Waffen, Projektile, Material- und Effekt-Prefabs unter `Resources/SkillArt`.
- `DiabloNetworkBootstrap` registriert die Player- und Loot-Prefabs. `LootDrop` verwendet das Loot-Prefab beim Erzeugen von Gegenständen.
- `ClassAppearance` lädt `Resources/CinderreachHeroArt`. Dessen Ranger-Modell referenziert Rig und Material. Die Ausrüstungsmodelle werden aus dem Katalog erzeugt.

## Charaktere und Uniques

Jede Klasse trägt ihr eigenes KayKit-Klassen- und Geschlechtsmodell (Knight, Ranger, Mage, Rogue, Rogue_Hooded). Nur der Fenwalker trägt den gelben Brustschutz als Startrüstung. Die Startwaffen-Darstellung bleibt. Das Slot-Modell wird für ein ausgerüstetes Unique anhand der stabilen Item-ID ausgewählt.

| Slot | Eigenständige Modelle |
| --- | --- |
| Waffe | Emberbrand Blade, Lantern of the Hold, Lancer Ward, Wick of the Halo, Nightfang Edge, Rogue’s Keepsake |
| Rüstung | Cryptwarden Plate, Bulwark Carrier, Mercy Shroud, Shadowstride Vest, Shardback Aegis |
| Helm | Seer’s Circlet, Seismic Crown, Circle of Dawn, Keen Cadence, Glasswright Prism |

Der Unity-Verifikationslauf prüft die Katalogabdeckung, den korrekten Slot, gültige Meshes und Materialien sowie doppelte Geometrie. Der Health-Check G-DIABLO02 prüft zusätzlich die Zuordnung auf dem Checkout, ohne einen Editor zu starten.

## Kameraaufnahmen

Die Bildserie in Ordner `cinderreach-equipment-shots` unter `docs` zeigt alle drei Basischaraktere und jedes ausgerüstete Unique. Beispiele sind [Fenwalker](cinderreach-equipment-shots/01_base_fenwalker.png), [Seismic Crown](cinderreach-equipment-shots/12_seismic-crown.png) und [Shardback Aegis](cinderreach-equipment-shots/17_shardback-aegis.png).

## Reproduzieren

Im Unity-Editor `Sandbox → Cinderreach → Export Asset Inventory` aufrufen oder den Batch-Einstiegspunkt `CinderreachAssetAudit.Export` ausführen. `CINDERREACH_ASSET_INVENTORY` kann einen anderen Ausgabepfad angeben.

Der Linux-Player erzeugt mit `-shotcheck -equipment-shots -shotdir <leeres-verzeichnis> -save-file <testname>.json` Referenzaufnahmen aus der Spielkamera. Die drei Basisaufnahmen verwenden die Standarddistanz. Für die 16 Detailaufnahmen fährt dieselbe Kamera näher an den Charakter.
