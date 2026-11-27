# Progress: cinderreach-skills-modularization

Prozent: 100 %

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
- 2026-11-27 (S4 proof, Worktree `feature/cinderreach-skills-modularization-s4`): Gesamtverifikation und Verhaltens-Smokes grün abgeschlossen:
  - Zeilenzahlen vorher/nachher: `sandbox/Assets/Scripts/DiabloSkills.cs` 1.414 → 1.158 (−256), `sandbox/Assets/Scripts/BowStanceVisuals.cs` 140 (neu), `sandbox/Assets/Scripts/SkillBarHud.cs` 194 (neu).
  - Verträge per `grep`: `DiabloSkills.Skill` Wire-Int-Enum `Fireball = 0 .. PowderKeg = 19` unverändert; `DiabloNetworkPlayer.SubmitSkillCastServerRpc(int skillId, Vector3 origin, Vector3 dir, ...)` unverändert (Zeile 624, Aufruf `DiabloSkills.cs:417`); `grep -rln DiabloSkills sandbox/Assets/Scripts sandbox/Assets/Tests` = 61 Dateien (59 Bestand + `BowStanceVisuals.cs` + `SkillBarHud.cs`).
  - Gezielte Kriterium-Suites: EditMode (`SkillRankTests;BuildUniqueEffectTests;PartySkillFormulaTests;LanternSkillFormulaTests`) **16/16**, PlayMode (`DiabloSkillsTests;LanternSkillTests;PartySkillTests;DiabloHdPlayTests`) **24/24**.
  - Voll-Suiten (`bash scripts/run-unity-tests.sh`): EditMode **673/673** (29,2 s), PlayMode **383/383** (162,6 s); `python3 scripts/check-meta.py` OK; `KayKitAssets.Verify` (`repo.yaml:verify`) Exit 0.
  - Autoplay-Smoke (`bash scripts/run-autoplay.sh artifacts/skills-s4-autoplay -seed 0`): Exit **0**, `exceptions: 0`, `duration_s: 305.0`, alle Phasen `town,zone1,gate,zone2,boss,zone3,actboss,zone4,boss3,complete`, `combos: [FenShackle=16, Retribution=10, Shatter=12]`, `fps-gate` 33,9 fps PASS.
  - Koop-Paar-Smoke (`AUTOPLAY_SKIP_BUILD=1 bash scripts/run-coop-pair.sh artifacts/skills-s4-coop`): Exit **0**, Host (`exit=0`, `exceptions=0`, `duration=253s`) und Client (`exit=0`, `exceptions=0`, `duration=244s`, `hits=43`, `roleCasts=155`, `deaths=0`) synchron durch alle Zonen bis `boss3`/`complete`.
  - Sichtprüfung (`-shotcheck`): `08_bow_aim_front.png` (Bogen gezeichnet in Bogenhaltung), `09_bow_put_away.png` (Schwert zurück in der Hand), `33_flask_cooldown_hud.png` (Skillbar-HUD und Cooldown-Veil `0.1` auf Q sauber gerendert).

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
