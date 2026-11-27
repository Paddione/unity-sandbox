# Progress: cinderreach-skills-modularization

Prozent: 75 %

## Erledigt

- 2026-11-26: Aus Code-Analyzer-Befund `CA-02` und `/setgoal`-Klärungsrunde als Goal auf Basis `main` (`5bd41d3`) angelegt.
- 2026-11-27: Stufenplan [tasks.md](../../plans/cinderreach-skills-modularization/tasks.md) angelegt (4 Stufen, Basis `8e59d00`); `plan_refs` in GOAL.md ergänzt. Prozent bleibt 0 %.
- 2026-11-27: Drift seit Basis belegt: `DiabloSkills.cs` heute 1.414 Zeilen, Skill-Enum `0..19` (statt `0..11` in Kriterium 3), 59 referenzierende Dateien (statt 42) — Korrektur ist als S1 im Plan verankert, GOAL.md selbst noch unverändert.
- 2026-11-27 (P4, Worktree `feature/cinderreach-skills-modularization`): S1–S3 umgesetzt und verifiziert, noch ungelandet (Commit s. u., kein Push — Primary landet):
  - S1: GOAL-Kriterium 3 korrigiert (`Skill`-Enum `Fireball=0..PowderKeg=19`, Stand nach `aaa2571`; `grep -rln DiabloSkills sandbox/Assets/Scripts sandbox/Assets/Tests` = 61 Treffer = 59 Bestand + 2 neue Owned-Klassen, gezählt 2026-11-27). `cinderreach-party-roles` read-only abgeglichen: 100 %, keine offenen Punkte.
  - S2: `BowStanceVisuals.cs` (140 Zeilen, owned, kein MonoBehaviour) — `bowUntil/heldBow/bowRenderers/showBowModel/swordRenderers`, `CreateHeldBow/HandMountRotation/LongAxis/SetSwordVisible/FindDeep`, `StanceExpired`-Poll; `DiabloSkills` behält Fassaden `BowDrawn/EnterBowStance/ExitBowStance/SetStarterBowModelVisible/HandMountRotation/LongAxis`, `SwordName`-Const unverändert (liest auch `Classes/ClassAppearance.cs:313`).
  - S3: `SkillBarHud.cs` (194 Zeilen, owned, kein MonoBehaviour) — Naht nur schmale Delegates (`CooldownLeft`, aktives ClassDefinition, `lastUsed`), keine `DiabloSkills`-Referenz; `SkillBarCanvas`-Name + Child-Hierarchie byte-identisch; `HudLine()` bleibt öffentliche Fassade.
  - Zeilenbilanz: `DiabloSkills.cs` 1414 → 1158 (−256 netto; Logik byte-identisch extrahiert, nur Fassaden + Lazy-Owner bleiben).
  - Verifikation: `SandboxBuilder.CompileCheck` OK (Batchmode 6000.0.85f2, data.errors leer); EditMode **670/670**, PlayMode **377/377** (`bash scripts/run-unity-tests.sh` voll); `DiabloSkillsTests` 13/13 (u. a. `Arrow_SwapsSwordForBow_ThenBack`); `python3 scripts/check-meta.py` OK; `KayKitAssets.Verify` Exit 0; `agent-workspace check` still. Keine Testdatei angefasst.
  - Offene Fremd-Evidenz: Autoplay-/Koop-Smoke nur bei verfügbarem Player-Build (nicht behauptet, nicht gelaufen).

- 2026-11-27: Plan gegen `1ff1b95` geprüft (Code unverändert, 1.414 Zeilen, Enum 0..19) und als P4 priorisiert; `cinderreach-party-roles` ist 100 %, der S1-Abgleich ist damit nur noch lesende Kontrolle.

- 2026-11-27 (S1–S3-Land): als `71cb9eb` auf `main` gelandet (ff nach Rebase auf `a5087c8`, Push durch, Worktree aufgeräumt). Prozent jetzt 75 %.

## Nächste Schritte

- S4: Gesamtverifikation (Autoplay-/Koop-Smoke bei verfügbarem Player-Build, sonst als offene manuelle Evidenz benennen), PROGRESS-Abschluss, Goal auf 100 % mit `Offene Punkte: Keine.`

## Offene Punkte

- S1–S3 gelandet (`71cb9eb`); S4-Abnahme (Smoke + Abschluss) steht aus.
