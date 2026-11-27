---
goal: cinderreach-skills-modularization
slug: cinderreach-skills-modularization
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-skills-modularization/tasks.md]
---

# Goal: cinderreach-skills-modularization

## Objective

Die hochfrequente Kernklasse `DiabloSkills.cs` (1.304 Zeilen, 55 Methoden, 36 Commits in 12 Tagen) auf Basis von `main` (`5bd41d3`) entzerren, indem UGUI-Skillbar-Rendering (`SkillBarHud`) und Bogen-/Schwert-Stance-Visuals (`BowStanceVisuals`) aus der Kampf-Autorität herausgelöst werden, ohne Netz- oder Kampfverträge zu brechen. Ausgangslage: [CINDERREACH.md](../CINDERREACH.md).

## Erfolgskriterien

1. Bogen-/Schwert-Modellwechsel (`CreateHeldBow`, `HandMountRotation`, `LongAxis`, `SetSwordVisible`, `EnterBowStance`/`ExitBowStance`) ist in einen eigenständigen Helfer oder eine eigene Komponente (`BowStanceVisuals`) ausgelagert; `DiabloSkills.BowDrawn` und `ExitBowStance` bleiben als öffentliche Fassade erhalten.
2. UGUI-Skillbar-Aufbau und Cooldown-Anzeige (`BuildHud`, `RebuildHudSlots`, `UpdateHud`, `HudLine`) sind in eine eigene Komponente (`SkillBarHud`) ausgelagert; Canvas-Name `SkillBarCanvas` und Child-Hierarchie für bestehende Tests bleiben unverändert.
3. Alle Verträge bleiben strikt unverändert: `DiabloSkills.Skill` Wire-Int-Enum (`0..19`, Stand nach `aaa2571`), öffentliche Balance-Felder, `Cast`, `CastToward`, `ServerExecuteSkill`, `PlayCosmetic` sowie alle 59 referenzierenden Dateien (gezählt 2026-11-27 per `grep -rln DiabloSkills sandbox/Assets/Scripts sandbox/Assets/Tests`) inkl. `DiabloNetworkPlayer.SubmitSkillCastServerRpc`, `PartyCompanion` und Autoplay.
4. Vollständige Verifikation grün: `DiabloSkillsTests`, `LanternSkillTests`, `PartySkillTests`, `SkillRankTests`, `BuildUniqueEffectTests`, `PartySkillFormulaTests`, `DiabloHdPlayTests`, komplette EditMode/PlayMode-Suiten (`bash scripts/run-unity-tests.sh`), `python3 scripts/check-meta.py` und `KayKitAssets.Verify`.

## Scope

- `sandbox/Assets/Scripts/DiabloSkills.cs`
- Neue extrahierte Skripte unter `sandbox/Assets/Scripts/` (inkl. `.meta`), sowie nur falls erforderlich minimale Verdrahtung in `SandboxBuilder`/Tests.

## Nicht-Ziele

- Keine Balance-Änderungen, keine neuen Skills oder Enum-Werte und keine Änderungen an `DiabloNetworkPlayer`-RPC-Signaturen.
- Kein Umbau von `DiabloAudio.cs`.

## Abhängigkeiten

- `unity-sandbox main` (Basis `5bd41d3`), Abgleich gegen laufende Änderungen aus `cinderreach-party-roles` vor Start.
