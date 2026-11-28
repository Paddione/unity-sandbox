# Goal: cinderreach-town-npc-identity

health_goals: [G-DIABLO02, G-DIABLO03]

## Objective
Händler, Schmied Jorin, Ilsabe und die Story-NPCs sehen jeweils eigenständig aus. Alle drehen sich zum Helden, sobald er in etwa 6 m Reichweite ist.

## Ist-Befunde
- B1 Händler nutzt KayKit `Adventurers/Characters/Rogue_Hooded.fbx` (`Editor/TownBuilder.cs:15`), ohne Drehung zum Helden.
- B2 `Story/StoryNpc.FaceHero` dreht animierte Figuren nur bei Klick oder `T` (540°/s, nur Yaw).

## Entscheidungen
- D0 Modus: Anlegen + planen + umsetzen in der Reihenfolge loot-click-filter → inventory-cursor-item → bag-quest-upgrades → vendor-sell-all → town-npc-identity (Eigner, /setgoal 2026-11-28).
- D1 Umfang: Händler, Schmied, Ilsabe und alle Story-NPCs; je eigenes Modell oder eigene Farb-/Ausrüstungsvariante aus vorhandenen KayKit-Assets (Q7).
- D2 Drehung automatisch in ca. 6 m Reichweite zum nächsten Helden, zurück in Ruheausrichtung, wenn niemand nah ist.

## Erfolgskriterien
1. Kein Paar der genannten NPCs teilt Modell und Material — Nachweis: EditMode-Test über die Szenen-NPCs (Mesh+Material-Signatur eindeutig).
2. NPCs drehen sich in Reichweite zum Helden (Winkel < 15° nach 0,5 s), außerhalb zurück — Nachweis: PlayMode-Test.
3. Szenenvertrag, Voll-Suiten grün, Windows-Shot der Lantern Hold mit den NPCs — Nachweis: `bash scripts/run-unity-build.sh`, `bash scripts/run-unity-tests.sh` failed 0, Shot unter `docs/town-npcs/`.

## Nicht-Ziele
- Keine neuen Dialoge oder Animationen über vorhandene Idle-Takes hinaus.
- Keine generierten 3D-Modelle.

## Abhängigkeiten
- Keine.
