# Progress: cinderreach-second-class

Prozent: 80 %

## Erledigt

- 2026-11-18: Plan `.agents/plans/cinderreach-second-class/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: Goal angelegt (Folge-Goal #6).

- 2026-11-18: Startbedingung erfüllt (`cinderreach-loop-guard` 100 %).
- 2026-11-18: S1 class-data gelandet. `ClassDefinition` + `ClassCatalog` (Fenwalker = bisheriger Held,
  Knight, Q/E/R; Lanternbearer, Mage, Q/E/R/Space, Focus-Faktor 1,5), `DiabloSkills.Skill` mit festen Werten
  (0–2 unverändert, Lanternbearer 3–6), Save v3 mit `classId` in `RunProfile`, Migration v1/v2 → v3 nach
  Fenwalker, unbekannte Id fällt mit Warnung auf Fenwalker zurück. Belege: EditMode 143/143 (6 neu
  `ClassCatalogTests`, Migrationstests auf v3), PlayMode 74/74, Verifies grün, Autoplay exit 0.

- 2026-11-18: S2 skills gelandet. Lantern Bolt (Q, 4 Schaden, 1,2 s, durchdringt 1 Ziel, eigene Lichtkugel),
  Wisps (E, 2 Begleiter 20 s, 1 Schaden / 0,8 s im Umkreis 6 m, ab Rang 3 ein dritter, 14 s), Searing Halo
  (R, 3,5 m, 3 Schaden + 0,6 m Pop, 7 s), Veil Step (Space, 6 m Blink per CapsuleCast, stoppt vor Wänden, 5 s).
  Klassen-Gate in `CastToward`, Tasten und HUD aus `ClassCatalog`, Focus-Faktor der Klasse (Fenwalker 1),
  drei synthetisierte Klänge, Co-op-Spiegel für alle vier Skills (Wisps kosmetisch). Belege: EditMode 157/157
  (8 neu `LanternSkillFormulaTests`), PlayMode 78/78 (4 neu `LanternSkillTests`), `DiabloD2Feel*` grün,
  Verifies grün, Autoplay Seed 0 (Fenwalker) 6/6 Phasen.

- 2026-11-18: S3 sheet+ui gelandet. `CharacterSheet` mit Rängen für alle 7 Skills (Speicherung aller Ränge,
  die der anderen Klasse bleiben erhalten und wirken nicht), Klassen-Focus-Faktor in `SkillMultiplier`,
  `DiabloCharacterUI` baut die Rang-Zeilen aus `ClassCatalog` (3 bzw. 4, Neuaufbau bei Klassenwechsel,
  Wisps-Zeile zeigt die Begleiterzahl). Belege: EditMode 163/163 (6 neu `ClassSheetUiTests`), PlayMode 78/78,
  Verifies grün, Autoplay Seed 1 6/6 Phasen.

- 2026-11-18: S4 select+items gelandet. `ClassAppearance` re-skinnt den Mage per Knochennamen auf das Knight-Rig
  (Knight-Meshes und Schwert aus, prozedurale Laterne mit Licht, Starter-Waffe je Klasse), Co-op-Spiegel über
  `DiabloNetworkPlayer.classIndex`. `ClassSelectUI` beim Start (zwei Karten, „Continue saved hero“ bei
  vorhandenem Save, übersprungen für Autoplay/Shotcheck/Co-op-Peers/`-class`). Waffen klassengebunden
  (`ItemDatabase.ClassRestriction`, `Equipment.CanEquip`, Bag lehnt ab, Tooltip-Hinweis), Unique „Lantern of
  the Hold“, Waffen-Drops 70 % passend zum Finder. Belege: EditMode 168/168 (5 neu `ClassItemTests`, u. a.
  Re-Skin am echten Knight/Mage-Rig), PlayMode 80/80 (2 neu `ClassSelectTests`), Verify `classLook=True`,
  Autoplay Fenwalker 6/6, Lanternbearer-Smoke 0 Exceptions (Fen ohne Kampfprofil nicht geräumt, folgt in S5).

## Nächste Schritte

1. S5 proof (Lanternbearer-Kampfprofil, ±25 %-Abnahme, Co-op gemischt, Shotcheck).

## Offene Punkte

- S2 bis S5 offen.
