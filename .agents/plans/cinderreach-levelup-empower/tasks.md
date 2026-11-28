---
goal: cinderreach-levelup-empower
slug: cinderreach-levelup-empower
branch: feature/cinderreach-levelup-empower
repo_main: main
tickets: none
health_goals: [G-DIABLO03]
start_condition: main sauber; setgoal-Runde 2026-11-28 beantwortet (alle Empfehlungen übernommen)
plan_refs: [.agents/goals/cinderreach-levelup-empower/GOAL.md]
---

# Plan: cinderreach-levelup-empower — Auto-Attribute je Klasse + Empower bei Rang 5

Pfad: feature, alle Stufen in einem Worktree
(`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-levelup-empower`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).

## Goal

Siehe [GOAL.md](../../goals/cinderreach-levelup-empower/GOAL.md): 5 Attributpunkte
je Level automatisch nach Klassen-Template (D2), 1 freier Skillpunkt, Empower
bei Rang 5 (×2,5 statt ×2,0 + generische Optik-Schicht), keine Gegner-Rebalance.

## Entscheidungs-Restimee (aus GOAL.md)

- D2 Template: Fenwalker 2/2/1, Rogue 2/1/2, Laternenträger 1/1/3 (Summe 5).
- D3 Manuelle Attributpunkte entfallen komplett; Alt-Saves mit freien Punkten
  werden beim Laden automatisch verteilt.
- D4 Empower: Rang-5-Multiplikator ×2,5 statt ×2,0 (`EmpowerDamageBonus = 0.5f`).
- D7 Gear-Rangbonus empowert nicht: `IsEmpowered` liest nur `GetRank` (gesetzt),
  nicht `EffectiveRank`.
- D8 Template als Datenfeld je Klasse in `ClassDefinition` (drei ints).

## File Structure

| Datei | Änderung |
|---|---|
| `sandbox/Assets/Scripts/Classes/ClassDefinition.cs` | + `attrStrengthPerLevel`, `attrVitalityPerLevel`, `attrFocusPerLevel` (ctor-Parameter), + `ApplyLevelUpAttributes(CharacterSheet)` oder Template-Sequenz-Helper |
| `sandbox/Assets/Scripts/Classes/ClassCatalog.cs` | Template-Werte je Klasse (2/2/1, 1/1/3, 2/1/2) |
| `sandbox/Assets/Scripts/Progression/CharacterSheet.cs` | `AwardLevel(ClassDefinition cls)` (5 Punkte Template + 1 Skillpunkt, keine freien Attributpunkte), `DistributeFreeAttributes(cls)` für Alt-Saves, `EmpowerDamageBonus = 0.5f`, `IsEmpowered(skill)`, `RankMultiplier`-Empower; `SpendAttribute`/`freeAttributePoints`-Schreiben entfällt (Feld bleibt für Save-Kompatibilität) |
| `sandbox/Assets/Scripts/DiabloProgression.cs` | `AddXp`: `AwardLevel(RunProfile.Class)`; `SetState`: freie Attributpunkte via `DistributeFreeAttributes(RunProfile.Class)` verteilen; `SpendAttributePoint` entfernen; Banner-Subline mit neuen Attribut-/Skill-Zahlen |
| `sandbox/Assets/Scripts/Progression/DiabloCharacterUI.cs` | STR/VIT/FOC-Zeilen ohne +-Buttons, Template-Info-Zeile (`char.attr.auto`), Skill-Zeilen mit „Empowert"-Marker bei Rang 5 (`char.skill.empowered`), `char.header`-Text angepasst |
| `sandbox/Assets/Scripts/Localization/LocCatalog.Char.cs` | + `char.attr.auto`, + `char.skill.empowered`, `char.header` angepasst, `char.hint` angepasst |
| `sandbox/Assets/Scripts/Localization/LocCatalog.Hud.cs` | `hud.levelup.gains`: „+{0} Leben, +5 Attribute (Klasse), +1 Fertigkeitspunkt" |
| `sandbox/Assets/Scripts/Vfx/PresentationVfx.cs` | + `EmpowerBurst(Vector3 at)` (gepoolt, Weiß-Gold, größer/heller, Zähler `EmpowerBursts` als Test-Seam) |
| `sandbox/Assets/Scripts/DiabloSkills.cs` | zentraler Cast-Pfad: bei `Sheet.IsEmpowered(skill)` → `PresentationVfx.EmpowerBurst(...)` (ein gemeinsamer Helper, Aufruf an den Skill-Cast-Stellen) |
| `sandbox/Assets/Scripts/DiabloShotCheck.cs` | `AwardLevel`/`SpendAttribute`-Nutzung durch direkte Feldwerte ersetzen |
| `sandbox/Assets/Tests/EditMode/CharacterSheetTests.cs` | Tests auf `AwardLevel(cls)` umstellen; + Template-, Empower-, Gear-kein-Empower-Tests |
| `sandbox/Assets/Tests/EditMode/ClassSheetUiTests.cs` | Keine +-Buttons, Template-Zeile, Empower-Marker |
| `sandbox/Assets/Tests/PlayMode/SkillRankTests.cs` | `AwardLevel(cls)`-Signatur |
| `sandbox/Assets/Tests/PlayMode/VfxSpawnTests.cs` | + EmpowerBurst-Zähler-Test |
| `.agents/goals/CINDERREACH.md` | Z. 196 ersetzen (Auto-Attribute + Empower) |

## Stufen

### S1 — Daten + Logik (CharacterSheet/ClassDefinition/DiabloProgression)

1. `ClassDefinition`: drei int-Felder `attrStrengthPerLevel`/`attrVitalityPerLevel`/`attrFocusPerLevel` mit
   Ctor-Parametern; Helper `int[] LevelUpSequence()` (z. B. [S,S,V,V,F]-Folge aus den Counts) für deterministische
   Restverteilung.
2. `ClassCatalog`: Fenwalker `(2,2,1)`, Lanternbearer `(1,1,3)`, Rogue `(2,1,2)`.
3. `CharacterSheet`:
   - `public const int AttributesPerLevel = 5;` und `public const float EmpowerDamageBonus = 0.5f;`
   - `AwardLevel(ClassDefinition cls)`: Template-Sequenz einmal anwenden + `freeSkillPoints++`; kein
     `freeAttributePoints++`. Ohne `cls` (null) → Fallback auf alte 1-Punkt-Vergabe? Nein: null-sicher
     als `DistributeFreeAttributes(null)` = Punkte liegen lassen (Tests/ShotCheck nutzen Felder direkt).
   - `DistributeFreeAttributes(ClassDefinition cls)`: solange `freeAttributePoints > 0`, nächster
     Template-Schritt (`LevelUpSequence`), Punkt addieren, Zähler senken (deterministisch, idempotent).
   - `SpendAttribute`/`SpendAttributePoint`-Pfad entfernen; `freeAttributePoints`-Feld bleibt (Save-Vertrag).
   - `RankMultiplier`: `1 + RankDamageBonus * EffectiveRank + (IsEmpowered(skill) ? EmpowerDamageBonus : 0)`.
   - `IsEmpowered(skill) => GetRank(skill) >= MaxSkillRank` (bewusst ohne Gear, D7).
   - `HasUnspent` = `freeSkillPoints > 0 || freeAttributePoints > 0` (unverändert, Alt-Saves).
4. `DiabloProgression`:
   - `AddXp`: `Sheet.AwardLevel(RunProfile.Class)`.
   - `SetState(full)`: nach `freeAttributePoints`-Import `sheet.DistributeFreeAttributes(RunProfile.Class)`.
   - `SpendAttributePoint` entfernen (kein Aufrufer mehr).
   - `LevelUpMoment`: `bannerSub.text = Loc.F("hud.levelup.gains", LifePerLevel, CharacterSheet.AttributesPerLevel)`.
5. `DiabloShotCheck`: `prog.Sheet.AwardLevel()` ×4 + `SpendAttribute` ×4 ersetzen durch direkte
   Feldzuweisungen (`strength = 2; vitality = 1; focus = 1;`), damit das Shotcheck-Build unverändert bleibt.

Verifikation S1: `PLATFORMS=EditMode TEST_FILTER=CharacterSheetTests bash scripts/run-unity-tests.sh` grün
(nach Testanpassung in S3 vorläufig nur Compile via `unity --json recompile`).

### S2 — VFX-Empower-Schicht

1. `PresentationVfx.EmpowerBurst(Vector3 at)`: gepooltes System „EmpowerBurst" (SkillArt/FireAdditive,
   ~40 Partikel, helles Weiß-Gold über `DiabloPalette`, größerer Radius als CleaveArc, kurzer Aufwärts-Schub),
   statischer Zähler `EmpowerBursts++`, in `ResetRun()` zurücksetzen.
2. `DiabloSkills`: privater Helper `EmpowerFor(DiabloSkills.Skill skill)` — liest
   `CharacterSheet.For(gameObject)` und feuert bei `IsEmpowered(skill)` den Burst am Helden;
   Aufruf im zentralen Cast-Einstieg jeder Skill-Fähigkeit (ein gemeinsamer Dispatcher-Aufruf, falls
   vorhanden, sonst pro Cast-Methode an der Stelle der bestehenden `PresentationVfx`-Calls).

Verifikation S2: `PLATFORMS=PlayMode TEST_FILTER=VfxSpawnTests bash scripts/run-unity-tests.sh` grün.

### S3 — UI + Lok

1. `DiabloCharacterUI.BuildUI`: STR/VIT/FOC-Zeilen ohne +-Button (neue `AddLabelRow`-Variante ohne
   Button), `attrPointsText` → `char.attr.auto`-Zeile („+2 Stärke / +2 Vitalität / +1 Fokus pro Stufe"
   aus dem Klassen-Template).
2. `Refresh`: keine Button-Zustände für Attribute; Skill-Zeilen bekommen bei Rang 5 das Suffix
   `char.skill.empowered` (Gold), z. B. „Cleave ●●●●● ✦ Empowert".
3. Lok: `char.attr.auto`, `char.skill.empowered`, `char.header`-Unterzeile („Investiere Fertigkeitspunkte …"),
   `char.hint`, `hud.levelup.gains` mit neuen Platzhaltern; deutsche + englische Texte.

Verifikation S3: `PLATFORMS=EditMode TEST_FILTER=ClassSheetUiTests bash scripts/run-unity-tests.sh` grün +
`unity --json recompile` ohne Fehler.

### S4 — Tests komplett + Doku + Gates

1. `CharacterSheetTests`: Umstellung auf `AwardLevel(cls)`; neue Tests:
   - `AwardLevel_AppliesClassTemplate` (alle drei Klassen, Summe 5, 0 freie Attributpunkte, +1 Skillpunkt)
   - `DistributeFreeAttributes_MigratesLegacyPoints` (Alt-Save-Simulation via `SetState`)
   - `RankFive_EmpowersMultiplier` (×2,5; Rang 4 ×1,8)
   - `GearRankBonus_DoesNotEmpower` (Rang 4 + Gear 1 → ×2,0; Rang 5 + Gear 1 → ×2,7)
   - `IsEmpowered_FlagsOnlySpentRanks`
2. `ClassSheetUiTests`: Button-frei, Template-Zeile, Empower-Marker (Rang-5-Zustand setzen).
3. `SkillRankTests`/`VfxSpawnTests`: Signaturen + EmpowerBurst-Zähler.
4. `CINDERREACH.md` Z. 196 ersetzen: „Level-Up: 5 Attributpunkte automatisch nach Klassen-Template
   (Fenwalker 2/2/1, Rogue 2/1/2, Laternenträger 1/1/3) + 1 Skillpunkt (Rang 1–5, +20 %/Rang,
   Rang 5 = Empowert: ×2,5 statt ×2,0 + Optik-Burst)".
5. `docs/dev-journal.md`: Eintrag mit Done/Faced/Learned/Next.

### S5 — Voll-Gates + Land

1. `bash scripts/run-unity-tests.sh` (EditMode + PlayMode komplett) → failed == 0 in `docs/test-runs.jsonl`.
2. `python3 -m pytest scripts -q`, `python3 scripts/check-meta.py`.
3. `unity --json recompile` sauber (falls Editor am Worktree läuft, sonst Batchmode-Verify aus `repo.yaml`).
4. PROGRESS.md auf 100 % (jedes Kriterium mit Beleg), `node ~/scripts/goal-lint.mjs --repo .` still.
5. `python3 ~/scripts/agent-workspace.py land` im Worktree (ff), danach Done-Meldung.

## Verify (final)

- `bash scripts/run-unity-tests.sh` — beide Plattformen grün (G-DIABLO03-Basis)
- `python3 -m pytest scripts -q` — Skript-Suites grün
- `python3 scripts/check-meta.py` — Meta-Check grün
- `node ~/scripts/goal-lint.mjs --repo .` — still
