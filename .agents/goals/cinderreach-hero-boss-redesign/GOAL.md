# Goal: cinderreach-hero-boss-redesign

health_goals: [G-DIABLO02]

## Objective
Die drei Klassen (Fenwalker, Lanternbearer, Rogue) und die drei Akt-Bosse (Kilnmother, Glasswright, Thewes) bekommen eigene, sofort unterscheidbare 3D-Modelle statt KayKit-Standardfiguren. Entworfen wird live in einem sichtbaren Blender-Fenster über das Blender-MCP, damit der Eigner jede Iteration mitverfolgt und freigibt. Freigegebene Modelle laufen im Spiel mit den bestehenden Animationen.

## Ist-Befunde
- B1 `~/scripts/blender-mcp-host.sh` startet Blender nur headless unter `xvfb-run` (Bridge `localhost:9876`, Addon mcp-for-blender 2.2.6). Laut Skriptkommentar beendet WSLg ein übergroßes Wayland-Fenster mit Protokollfehler 4; ein headed Start braucht daher X11 auf `DISPLAY=:0` mit fester Fenstergröße.
- B2 Klassen nutzen KayKit-Adventurers (`sandbox/Assets/ThirdParty/KayKit/Adventurers/Characters/Knight.fbx`, `Mage.fbx`, `Rogue.fbx`, `Rogue_Hooded.fbx`), geladen über `ClassAppearance` (`AddressableAssets/CinderreachHeroArt`, Gender-Basen, Haar am `head`-Knochen, Build-Faktoren an chest/spine/shoulder/upperarm).
- B3 Bosse sind KayKit-Skelette: Kilnmother `Skeleton_Brute.fbx` mit Warrior-Fallback (`sandbox/Assets/Editor/KilnBuilder.cs:193`), Glasswright und Thewes `Skeleton_Warrior.fbx` (`sandbox/Assets/Editor/BarrensBuilder.cs:135`, `:167`).
- B4 Unique-Ausrüstung hängt an Körperstellen (`HeroGearModels.cs`), geprüft durch `KayKitAssets.Verify` und Health `G-DIABLO02`.
- B5 Equipment-Shotcheck (`-shotcheck -equipment-shots`) erzeugt Referenzbilder je Klasse; Performance-Nachweis nur per Windows-GPU-Shotcheck.

## Entscheidungen
- D1 Modus: anlegen, planen, umsetzen (Eigner 2026-11-28).
- D2 Eigene Meshes, gewichtet auf das vorhandene KayKit-Skelett (Adventurers-Rig für Klassen, Skeletons-Rig für Bosse); Animationen, Haar-/Gear-Aufsätze und Build-Proportionen bleiben funktionsfähig.
- D3 Stil: stylized Low-Poly, dunkel, Farbcode je Akt (Moor/Glut/Glas), lesbar in der isometrischen Kamera.
- D4 Abnahme je Modell: Eigner gibt im Blender-Fenster frei, dann Unity-Import und Shot im Spiel. Reihenfolge Fenwalker, Lanternbearer, Rogue, Kilnmother, Glasswright, Thewes.
- D5 Headed-Blender als eigener Startpfad neben dem headless Dienst (gleicher Port 9876, nie beide gleichzeitig), damit Automationen den headless Pfad behalten.
- D6 Blender-Quelldateien (`.blend`) und Export-FBX liegen im Repo unter `sandbox/Assets/Art/Characters/` bzw. `art-src/`; kein Fremd-IP, keine Asset-Store-Käufe.

## Erfolgskriterien
1. Headed Blender startet sichtbar auf dem Windows-Desktop und ist über das Blender-MCP steuerbar — Nachweis: Startbefehl + `get_scene_info` über MCP liefert die Szene, Screenshot des Fensters.
2. Je Klasse ein eigenes, auf das KayKit-Rig gewichtetes Modell, vom Eigner im Blender-Fenster freigegeben — Nachweis: Eigner-Abnahme (Datum je Modell) + Blender-Render unter `docs/hero-boss-redesign/`.
3. Je Akt-Boss ein eigenes, auf das Skeleton-Rig gewichtetes Modell, vom Eigner freigegeben — Nachweis: Eigner-Abnahme + Blender-Render unter `docs/hero-boss-redesign/`.
4. Spiel nutzt die neuen Modelle mit funktionierenden Animationen, Haar, Build-Proportionen und Uniques — Nachweis: EditMode/PlayMode grün, `KayKitAssets.Verify` und `G-DIABLO02` grün, Autoplay `-seed 0` Exit 0 bis `complete`.
5. Im Spiel sichtbar — Nachweis: Windows-Equipment-Shotcheck mit drei Klassenbildern und je einem Boss-Shot unter `docs/hero-boss-redesign/`.

## Nicht-Ziele
- Neue Skelette oder Animationen.
- Redesign normaler Gegner, NPCs, Begleiter-Logik oder Unique-Geometrie (nur Sitz an neuen Körpern anpassen).
- Gameplay-, Balance- oder Save-Änderungen.

## Abhängigkeiten
- Blender-MCP (`hub_blender`) und WSLg-Display; Eigner-Verfügbarkeit für Live-Abnahmen.
- Windows-Shotcheck-Pfad (`Games\cinderreach-wip`).
