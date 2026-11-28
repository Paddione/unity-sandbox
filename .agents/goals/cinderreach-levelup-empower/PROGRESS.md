# Progress: cinderreach-levelup-empower

Prozent: 100 %

## Erledigt
- 2026-11-28: Goal angelegt (setgoal-Runde beantwortet, alle Empfehlungen übernommen; Persistenz + Ausführung auf Eigner-Wunsch).
- 2026-11-28: K1 — `AwardLevel(cls)` verteilt 5 Attributpunkte nach Klassen-Template (D2) + 1 Skillpunkt, ohne freie Attributpunkte. Beleg: `CharacterSheetTests` 11/11 grün (LevelJump/AppliesEachClassTemplate/AwardLevel_VitalityAddsLife), `PLATFORMS=EditMode TEST_FILTER=CharacterSheetTests bash scripts/run-unity-tests.sh`.
- 2026-11-28: K2 — Alt-Saves mit freien Attributpunkten werden beim Laden verteilt (`SetState` → `DistributeFreeAttributes`, deterministisch nach Template-Reihenfolge). Beleg: `CharacterSheetTests.SetState_DistributesLegacyFreeAttributePoints` grün.
- 2026-11-28: K3 — `DiabloCharacterUI` ohne Attribut-+-Buttons, mit Template-Zeile (`char.attr.auto`) und „✦ Empowert"-Marker bei Rang 5; `unity --json recompile`-Kette über `run-unity-build.sh`-Warmup (SandboxBuilder.CompileCheck OK). Beleg: `ClassSheetUiTests` 8/8 grün (HasNoAttributeSpendButtons_AndShowsTemplate, MarksEmpoweredSkillAtRankFive).
- 2026-11-28: K4 — Rang-5-Multiplikator ×2,5 statt ×2,0 (`EmpowerDamageBonus 0.5f`), Gear-Rangbonus empowert nicht (`IsEmpowered` liest nur gesetzte Ränge). Beleg: `CharacterSheetTests.RankFiveEmpowers/GearRankBonus_DoesNotEmpower/IsEmpowered_FlagsOnlySpentRanks` grün.
- 2026-11-28: K5 — Zentraler Empower-VFX: `PresentationVfx.EmpowerBurst` (Weiß-Gold-Ring + Funkenschauer, Zähler `EmpowerBursts` als Test-Seam), gefeuert aus `DiabloSkills.EmpowerFor` im zentralen `CastToward`-Pfad. Beleg: `VfxSpawnTests.EmpoweredCast_FiresBurst_PlainCastDoesNot` grün (11/11 PlayMode VfxSpawnTests).
- 2026-11-28: K6 — `CINDERREACH.md:196` ersetzt (Auto-Attribute + Empower), Banner `hud.levelup.gains` mit „+5 Attribute (Klasse), +1 Fertigkeitspunkt", Lok-Keys angepasst (`char.attr.auto`, `char.skill.empowered`, `char.header`); `python3 scripts/check-meta.py` grün, `LocalizationTests.NoUnlistedPlayerVisibleLiterals` grün.
- 2026-11-28: K7 — G-DIABLO03-Basis grün: EditMode 779/779, PlayMode 429/429 (nach Rebase auf origin/main mit Footwork-/Audio-Fixes), failed == 0 in `docs/test-runs.jsonl` (Zeilen 2026-11-28T03:54:41Z / 03:58:29Z, commit 11f1d10); `python3 -m pytest scripts -q` 67 passed; `node ~/scripts/goal-lint.mjs --repo .` still.

## Nächste Schritte
Keine.

## Offene Punkte
Keine.
