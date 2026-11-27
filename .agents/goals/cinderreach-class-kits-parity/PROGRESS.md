# Progress: cinderreach-class-kits-parity

Prozent: 100 %

## Erledigt

- 2026-11-26: setgoal-Runde beantwortet (6 eigene Skills, klasseninterne Combos + Varianten, Uniques umschreiben + je 1 neues, volle Art/Audio-Abnahme, health_goals G-DIABLO02/03/06/07). Goal angelegt, Worktree `feat/class-kits-parity`.
- K1: Kits Moorgänger (Spalten, Moorhaken, Erdstoß, Moorsturm, Sumpfgrund, Eisenschwur) und Schmuggler (Schnellschuss, Klingenwirbel, Rauchtopf, Schattenrolle, Giftphiole, Pulverfass); Enum 12–19 angehängt. Beleg `ClassKitParityTests.EveryClass_CarriesSixOwnSkills_OnTheSameKeys`, `SkillEnum_AppendsKitSkillsWithoutMovingOldValues`.
- K2: 7 Combos mit Formeltests (`Formulas_MatchTheTooltips`, `Shatter_*`, `FenShackle_*`, `Retribution_*`, `VenomBurn_*`, `Ambush_*`, `ThroatCut_*`, PlayMode `SparkShot_*`); im Autoplay ausgelöst (`docs/class-kits-run-log.md`).
- K3: Schnellschuss Einzelschuss/Fächer (`QuickShotVariants_FanTradesDamageForBolts`), Spalten-Varianten bleiben im Moorgänger-Kit; Sheet-Text aus `SkillVariantCatalog.Describe`.
- K4: 6 Matrix-Uniques umgeschrieben (IDs gleich), neu `moorvogt-anchor` und `smugglers-fuse` (Loot 50/50 Build-Anteil, Tooltip DE/EN, Gear-Modell, Tests `ComboUniques_*`, `BuildWeaponPool_*`, `MoorvogtAnchor_*`, `SmugglersFuse_*`, `Shadowstride_*`, `KeenCadence_*`, `LancerWard_*`, `SeismicCrown_*`). Laterne-Uniques geprüft, unverändert.
- K5: 8 Blender-Icons (`docs/menu-art/build_menu_pack.py`), 8 Synth-Cues im Manifest (`check-audio-manifest` OK, 43 Cues), Namen + Beschreibungen DE/EN (`LocCatalog.Skill`), Glossar.
- K6: Autoplay je Klasse Exit 0, alle Phasen, 0 Exceptions; Windows-Shotcheck mit 9 Kit-Posen inkl. beider Varianten je Klasse; Windows-Autoplay fps-gate PASS (63,0 fps worst @1080p).
- K7: `CharacterSheet.RefundForeignRanks` beim Laden (`OldSave_RefundsRanksOutsideTheKit`, `CharacterSheetTests.Points_SurviveSaveRoundtrip` über `SaveService.TryLoad`), Kit-Ränge im Save (`KitRanks_RoundTripThroughTheSave`).
- K8: Koop-Intent akzeptiert Kit-Skills nur für ihre Klasse (`CoopIntent_AcceptsKitSkillsForTheirClassOnly`, `CoopPartyTests.IsCastSkillId_CoversTheWholeEnum`); Status/Schaden nur auf Host/Solo.

- Deploy 2026-11-27: gelandet `aaa2571`; Server und Client aus `443d7c0` gebaut, Image `sha256:46dd1baa…` in dotfiles `9fbe8bf` gepinnt, Pod läuft ohne Exception, Windows-Client gestaget (`docs/class-kits-run-log.md`).

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
