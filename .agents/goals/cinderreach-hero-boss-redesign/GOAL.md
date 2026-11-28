# Goal: cinderreach-hero-boss-redesign

health_goals: [G-DIABLO02]

## Objective
Die drei Klassen (Fenwalker, Lanternbearer, Rogue) und die drei Akt-Bosse (Kilnmother, Glasswright, Thewes) bekommen eigene, sofort unterscheidbare 3D-Modelle statt KayKit-Standardfiguren. Entworfen wird live in einem sichtbaren Blender-Fenster über das Blender-MCP, damit der Eigner jede Iteration mitverfolgt und freigibt. Freigegebene Modelle laufen im Spiel mit den bestehenden Animationen.

## Ist-Befunde
- B1 „~/scripts/blender-mcp-host.sh“ startet Blender nur headless unter `xvfb-run` (Bridge `localhost:9876`, Addon mcp-for-blender 2.2.6). Laut Skriptkommentar beendet WSLg ein übergroßes Wayland-Fenster mit Protokollfehler 4; ein headed Start braucht daher X11 auf `DISPLAY=:0` mit fester Fenstergröße.
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
- D7 (2026-11-28, Eigner) Boss-Redesign: Die Akt-Bosse sind Karikaturen der Chefs großer KI-Firmen — A1 Elon Musk, A2 Sam Altman, A3 Dario Amodei, A4 Jensen Huang. Erkennbarkeit über überzeichnete Merkmale im Low-Poly-Stil, keine Fototexturen. Ersetzt die Boss-Reihenfolge aus D4; die v1-Bosse (Ofenmutter, Glasmacher, Thewes) sind verworfen.
- D8 (2026-11-28, Eigner) Jeder Boss hat eine Menschform und eine neu entworfene Monsterform (auch A1–A3, Jensen als Monster in Lederjacke). Verwandlung im Spawn-Intro: Menschform erscheint, sagt eine Voiceline, verwandelt sich; gekämpft wird nur gegen die Monsterform.
- D9 (2026-11-28, Eigner) Voicelines: Agent schreibt Parodie-Entwürfe DE+EN zur Auswahl, Vertonung mit generischen Stimmen der Voice-Pipeline, keine Imitation echter Stimmen, keine erfundenen Tatsachenbehauptungen über die Personen.
- D10 (2026-11-28, Eigner) Akt IV (Zone, Quests, Story-Anschluss) wird ein eigenes Folge-Goal; hier entstehen nur Jensens Modelle und Verwandlung. Hinweis: Erkennbare Abbilder realer Personen sind bei öffentlicher oder kommerzieller Verbreitung rechtlich heikel (Persönlichkeitsrecht); der Stand gilt für private Builds.
- D6 Blender-Quelldateien (`.blend`) und Export-FBX liegen im Repo unter „sandbox/Assets/Art/Characters/“ bzw. „art-src/“; kein Fremd-IP, keine Asset-Store-Käufe.

## Erfolgskriterien
1. Headed Blender startet sichtbar auf dem Windows-Desktop und ist über das Blender-MCP steuerbar — Nachweis: Startbefehl + `get_scene_info` über MCP liefert die Szene, Screenshot des Fensters.
2. Je Klasse ein eigenes, auf das KayKit-Rig gewichtetes Modell, vom Eigner im Blender-Fenster freigegeben — Nachweis: Eigner-Abnahme (Datum je Modell) + Blender-Render unter „docs/hero-boss-redesign/“.
3. Je Akt-Boss (A1–A4) eine Menschform und eine Monsterform auf dem Skeleton-Rig, vom Eigner freigegeben (Kriterium geändert 2026-11-28, siehe D7/D8) — Nachweis: Eigner-Abnahme + Blender-Render unter „docs/hero-boss-redesign/“.
6. Spawn-Intro je Boss A1–A3 zeigt Menschform, Voiceline (DE+EN, vertont) und Verwandlung in die Monsterform; A4 ist als Prefab mit gleicher Verwandlung vorbereitet (neu 2026-11-28, siehe D8–D10) — Nachweis: PlayMode-Test der Verwandlung, Windows-Shot je Boss vor/nach der Verwandlung, Voice-QA (`scripts/voice/qa_transcribe.py`).
4. Spiel nutzt die neuen Modelle mit funktionierenden Animationen, Haar, Build-Proportionen und Uniques — Nachweis: EditMode/PlayMode grün, `KayKitAssets.Verify` und `G-DIABLO02` grün, Autoplay `-seed 0` Exit 0 bis `complete`.
5. Im Spiel sichtbar — Nachweis: Windows-Equipment-Shotcheck mit drei Klassenbildern und je einem Boss-Shot unter „docs/hero-boss-redesign/“.

## Nicht-Ziele
- Neue Skelette oder Animationen.
- Redesign normaler Gegner, NPCs, Begleiter-Logik oder Unique-Geometrie (nur Sitz an neuen Körpern anpassen).
- Gameplay-, Balance- oder Save-Änderungen.
- Akt IV als spielbarer Akt (Zone, Quests, Story): eigenes Folge-Goal (D10).

## Abhängigkeiten
- Blender-MCP (`hub_blender`) und WSLg-Display; Eigner-Verfügbarkeit für Live-Abnahmen.
- Windows-Shotcheck-Pfad (`Games\cinderreach-wip`).
