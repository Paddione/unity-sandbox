# Goal: cinderreach-levelup-empower

health_goals: [G-DIABLO03]
plan_refs: [.agents/plans/cinderreach-levelup-empower/tasks.md]

## Objective
Der Level-Up in Cinderreach vergibt Attribute nicht mehr manuell: Jede Stufe verteilt automatisch 5 Attributpunkte nach einem festen Klassen-Template, der Spieler behält 1 frei vergebbaren Skillpunkt pro Stufe. Wer 5 Skillpunkte (Rang 5) in einen einzelnen Skill steckt, „empowert" ihn: spürbar mehr Schaden und eine aufgewertete Optik, damit die volle Investition sich fühlbar auszahlt.

## Ist-Befunde
- B1 `CharacterSheet.AwardLevel()` (sandbox/Assets/Scripts/Progression/CharacterSheet.cs:82-87) vergibt 1 freien Attribut- + 1 freien Skillpunkt; `SpendAttribute` (Z. 89-101) verteilt manuell; Ränge 1–5, `RankDamageBonus = 0.20f`, `RankMultiplier` = 1 + 0,2·Rang (Z. 154-156).
- B2 Attributwirkung: Strength +1 Nahkampf/Punkt, Vitality +5 Leben/Punkt, Focus +8 % Skillschaden und +3 % CDR/Punkt (CharacterSheet.cs:19-28). Klassenfaktoren in `ClassCatalog` (sandbox/Assets/Scripts/Classes/ClassCatalog.cs:17-37): Fenwalker (Tank) 1f/1f, Laternenträger (Healer) 1,5/0,5, Rogue (Dps) 1,5/1,5; je 6 Kit-Skills auf Q/E/R/Space/F/G.
- B3 `DiabloCharacterUI` (sandbox/Assets/Scripts/Progression/DiabloCharacterUI.cs, Taste C): +-Buttons für STR/VIT/FOC (BuildUI Z. 344-349, Refresh Z. 124-127), Skill-Zeilen mit Rang-Pips (`RankPips`, Z. 179-185), Lok-Keys `char.attrpoints`/`char.skillpoints` (Z. 119-122).
- B4 Level-Up-Fluss: `DiabloProgression.AddXp` ruft `Sheet.AwardLevel()` je Stufe (sandbox/Assets/Scripts/DiabloProgression.cs:209), `OnLevelUp` (Z. 214) spielt Banner `hud.levelup.gains` (LocCatalog.Hud.cs:7: „+X Leben +1 Attributpunkt, +1 Fertigkeitspunkt"); Save persistiert `freeAttributePoints` über `GetState`/`SetState` (Z. 86-115), Autosave bei Level-Up.
- B5 VFX je Skill laufen zentral über `DiabloSkills` (sandbox/Assets/Scripts/DiabloSkills.cs: PresentationVfx.*, ComboVfx.*, Spawn*-Visuals) und die gepoolte API `PresentationVfx` (sandbox/Assets/Scripts/Vfx/PresentationVfx.cs: statische Zähler als Test-Seams, `VfxSpawnTests` vorhanden). Gear kann „+N auf alle Skills" geben (`CharacterSheet.EffectiveRank`, Z. 150-152).
- B6 Design-Stand in `.agents/goals/CINDERREACH.md:196` („Level-Up: 1 Attributpunkt … + 1 Skillpunkt (Rang 1–5, +20 %/Rang)") wird von diesem Goal ersetzt und muss aktualisiert werden.
- B7 Save-Version v15 (`RunProfile`); Suites via `bash scripts/run-unity-tests.sh` (EditMode/PlayMode, schreibt `docs/test-runs.jsonl`), Probe G-DIABLO03 wertet die NUnit-XMLs aus.
- B8 Kein aktives Goal (< 100 %) in `.agents/goals/` überschneidet sich (nur `cinderreach-map-overlay` mit 0 %, themenfremd).

## Entscheidungen
- D1 Modus „Anlegen + planen + umsetzen" (Eigner, setgoal-Runde 2026-11-28).
- D2 Attribut-Template je Stufe (Eigner): Fenwalker 2 Stärke/2 Vitalität/1 Fokus, Rogue 2 Stärke/1 Vitalität/2 Fokus, Laternenträger 1 Stärke/1 Vitalität/3 Fokus.
- D3 Auto-Verteilung ersetzt das manuelle Ausgeben komplett (Eigner): +-Buttons im Charakterbogen entfallen; vorhandene freie Attributpunkte alter Saves werden beim Laden automatisch nach Klassen-Template verteilt.
- D4 Empower bei Rang 5 (Eigner): Schadensmultiplikator ×2,5 statt ×2,0 bei Rang 5; unter Rang 5 bleibt +20 %/Rang.
- D5 Empower-Optik generisch (Eigner): eine Empower-Schicht im zentralen VFX-Spawn-Pfad (hellerer, größerer Burst mit Weiß-Gold-Farb-Shift) für alle Skills statt 20 Einzel-VFX-Bauten.
- D6 Keine Gegner-Rebalance in diesem Goal (Eigner); Balancing als Folge-Goal nach Eigner-Spieltest.
- D7 Eigenentscheidung: Gear-Bonus „+N auf alle Skills" empowert nicht — nur echte Rang-5-Investition (5 Skillpunkte) löst Empower aus; Gear bleibt reiner Schadensbonus über `EffectiveRank`.
- D8 Eigenentscheidung: Template als Datenfeld je Klasse in `ClassDefinition`/`ClassCatalog` (drei ints mit Summe 5), damit Bogen, UI-Text, AwardLevel und Tests eine Quelle lesen.

## Erfolgskriterien
1. `CharacterSheet.AwardLevel(cls)` verteilt 5 Attributpunkte gemäß D2 und 1 Skillpunkt, ohne freie Attributpunkte zu erzeugen — Nachweis: neue/angepasste EditMode-Tests in `CharacterSheetTests` grün im lokalen Lauf `bash scripts/run-unity-tests.sh` (Zeile in `docs/test-runs.jsonl` mit failed == 0).
2. Alte Save-Stände mit freien Attributpunkten werden beim Laden automatisch nach Klassen-Template verteilt (D3) — Nachweis: neuer Test am `DiabloProgression.SetState`-Pfad grün.
3. `DiabloCharacterUI` zeigt keine Attribut-+-Buttons mehr, zeigt das Klassen-Template und markiert Skills mit Rang 5 als „Empowert" — Nachweis: angepasste `ClassSheetUiTests` grün, Compile via `unity --json recompile` ohne Fehler.
4. Rang-5-Skills rechnen ×2,5 statt ×2,0 (D4); Gear-Rangbonus empowert nicht (D7) — Nachweis: neue Tests für `RankMultiplier`/`IsEmpowered` grün.
5. Empower-Optik greift zentral für alle Skills (D5) — Nachweis: `PresentationVfx.EmpowerBurst`-Zähler als Test-Seam, `VfxSpawnTests`-Erweiterung grün; Aufruf über einen zentralen Cast-Pfad in `DiabloSkills`.
6. `CINDERREACH.md:196`, Level-Up-Banner (`hud.levelup.gains`) und betroffene Lok-Keys sind aktualisiert — Nachweis: Diff enthält die Doku-/Lok-Änderungen, `python3 scripts/check-meta.py` grün.
7. G-DIABLO03 grün: EditMode- und PlayMode-Suites vollständig bestanden — Nachweis: lokaler `bash scripts/run-unity-tests.sh` (beide Plattformen) mit failed == 0, letzte Zeile in `docs/test-runs.jsonl`.

## Nicht-Ziele
- Keine Gegner-HP-/Kurven-Rebalance (D6).
- Keine individuell gestalteten Empower-VFX je Skill (D5).
- Keine Änderung der Skillpunkt-Ökonomie (weiter 1 Punkt/Stufe, Rang 1–5) und keine neuen Skills oder Skill-Varianten.
- Keine neue Save-Version: Es entstehen keine neuen Save-Felder (Verteilung läuft beim Laden, D3).

## Abhängigkeiten
- Keine. Lokaler Unity-Editor (6000.0.85f2) für Tests/Builds vorhanden; G-DIABLO03-Probe existiert in `~/scripts/health_probes/diablo_unity.py`.
