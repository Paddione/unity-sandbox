# Progress: cinderreach-town-npc-identity

Prozent: 100 %

## Erledigt
- 2026-11-28: Goal angelegt nach /setgoal-Runde.
- 2026-11-28: Kriterium 1 — Beleg: `TownNpcIdentityTests.Scene_HoldFigures_UniqueMeshAndTint_AndWatchHeroes` öffnet `DiabloZone.unity`: Vendor (Ilsabe), StoryNpc_ottilie, StoryNpc_merle, SmithFigure_jorin mit paarweise verschiedener Mesh+Tönung-Signatur, je `NpcLook`, `NpcFacing`, Animator; `Catalog_EveryNamedNpc_HasOwnBodyAndTint` (vier Körper, vier Tönungen, keine untönt). Commit `71f46db`.
- 2026-11-28: Kriterium 2 — Beleg: `NpcFacingPlayTests.TurnsToHeroInReach_ReturnsToRestOutside` (Held 4 m entfernt: < 15° nach 0,5 s; Held 20 m entfernt: zurück in Ruhepose < 15°), `Look_AppliesCatalogTint`.
- 2026-11-28: Kriterium 3 — Beleg: Szene per `run-unity-build.sh SandboxBuilder.BuildDiabloZone` neu gebaut (nur `DiabloZone.unity` übernommen); `bash scripts/run-unity-tests.sh` auf `71f46db`: EditMode 799/799, PlayMode 455/455; `check-quest-coverage.py` 0; Windows-Build + Shotcheck 1920x1080 Exit 0, `docs/town-npcs/21_town_lantern_hold.png`, `22_town_npcs_facing.png` (eigene Körper: Ottilie Mage/Moos, Jorin Barbar am Amboss, Ilsabe in Kapuze). Die Drehung belegt der PlayMode-Test; im Shot verschob der Pose-Teleport den Helden nicht sichtbar.

## Nächste Schritte
- Keine.

## Offene Punkte
- Keine.
