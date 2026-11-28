---
goal: cinderreach-class-ability-vfx
slug: cinderreach-class-ability-vfx
branch: feat/cinderreach-class-ability-vfx
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03]
status: planned
plan_refs: [.agents/goals/cinderreach-class-ability-vfx/GOAL.md]
---

# Plan: cinderreach-class-ability-vfx

Siehe [GOAL.md](../../goals/cinderreach-class-ability-vfx/GOAL.md) für Kriterien 1–6 und Entscheidungen D1–D3.

## Schritte
1. `AbilityVfxAnim.cs`, `ClassAbilityVfxBuilder.cs` und `ClassAbilityVfx.cs` erstellen, Prefabs nach `Assets/AddressableAssets/SkillVfx/` bauen und `AddressablesSetup.Ensure` ausführen.
2. Alle 18 Klassen-Skills + Varianten + Co-op-Cosmetic-Parität in `DiabloSkills.cs`, `ClassKitSkills.cs`, `ClassCombos.cs`, `SkillVariantTargeting.cs`, `DiabloProjectile.cs` und `Wisp.cs` verdrahten.
3. `ClassAbilityVfxTests.cs` (EditMode), `VfxSpawnTests.cs` (PlayMode) und `-ability-vfx-shots` in `DiabloShotCheck.cs` ergänzen.
4. Visuelle Screenshots aller 3 Klassen aufnehmen, unter `docs/ability-vfx-shots/` ablegen und per Sichtprüfung verifizieren.
5. Alle Gates (`check-meta.py`, `check-hotpath-caching.py`, `asset-bloat-audit.py`, `check-voice-lines.py`, `pytest`, `KayKitAssets.Verify`, komplette `run-unity-tests.sh`), `CINDERREACH.md` und `PROGRESS.md` auf `100 %` abschließen und per `land` integrieren.
