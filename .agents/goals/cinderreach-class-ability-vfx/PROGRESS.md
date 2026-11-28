# Progress: cinderreach-class-ability-vfx

Prozent: 100 %

## Erledigt
- 2026-11-28: K1 Deterministischer Builder `sandbox/Assets/Editor/ClassAbilityVfxBuilder.cs` + `AbilityVfxAnim.cs` + `ClassAbilityVfx.cs` angelegt (Commit `db559d9`); alle 22 VFX-Prefabs unter `sandbox/Assets/AddressableAssets/SkillVfx/` sowie Materialien unter `sandbox/Assets/Art/SkillVfx/Materials/` gebaut und per `AddressablesSetup.Ensure` registriert (`402 entries, 0 pruned`). `ClassAbilityVfxTests` (2/2 EditMode) grün.
- 2026-11-28: K2 Fenwalker-Kit (6 Skills: `Cleave` 360° & 90°-Variante + Retribution-Tint, `MireHook`, `SeismicSlam`, `BogCharge`, `MireGround`, `IronVow`) vollständig in `DiabloSkills.cs`, `ClassKitSkills.cs` und `ClassCombos.cs` verdrahtet.
- 2026-11-28: K3 Lanternbearer-Kit (6 Skills: `LanternBolt`, `Wisps` + Strike, `SearingHalo` + `HaloZone`-Ward, `VeilStep`, `MendingLight`, `RenewingCircle`) in `DiabloSkills.cs`, `DiabloProjectile.cs`, `Wisp.cs` und `SkillVariantTargeting.cs` verdrahtet.
- 2026-11-28: K4 Rogue-Kit (6 Skills: `QuickShot` + 3er-Fächer, `BladeFlurry` Doppel-Klingenbogen, `SmokePot`, `ShadowRoll`, `VenomVial`, `PowderKeg` 3D-Fass + FireBlast-Explosion) in `DiabloSkills.cs`, `ClassKitSkills.cs`, `ClassCombos.cs` und `DiabloProjectile.cs` verdrahtet.
- 2026-11-28: K5 Co-op-Cosmetic-Parität (`ShowRemoteCast`/`ShowRemote`, 0 Schaden), Budget & Gates grün: `VfxSpawnTests.All18ClassAbilities_SpawnDedicatedVfxAndStayUnderBudget` + volle Suiten auf Commit `db559d9`: `EditMode: 772/772 passed, 0 failed`, `PlayMode: 424/424 passed, 0 failed` (`docs/test-runs.jsonl`), `check-meta.py` OK, `check-hotpath-caching.py` OK, `asset-bloat-audit.py` OK (121.1 MB), `check-voice-lines.py` (244 Zeilen, 0 Probleme), `pytest scripts -q` (67 passed).
- 2026-11-28: K6 Visuelle Abnahme per `-ability-vfx-shots` auf WSLg-GPU (`DiabloShotCheck.cs`): 18 Screenshots erzeugt, in 3 Kontaktbögen unter `docs/ability-vfx-shots/` (`fenwalker-abilities.png`, `lanternbearer-abilities.png`, `rogue-abilities.png`) gebündelt und per Sichtprüfung verifiziert (keine Magenta-Shader, korrekte Vorwärtsausrichtung, klare Klassenfarben/Silhouetten).

## Nächste Schritte
Keine.

## Offene Punkte
Keine.
