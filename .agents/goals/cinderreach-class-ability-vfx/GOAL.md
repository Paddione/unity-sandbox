# Goal: cinderreach-class-ability-vfx

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03]
plan_refs: [.agents/plans/cinderreach-class-ability-vfx/tasks.md]

## Objective
Alle 18 Klassen-Abilities der drei Cinderreach-Klassen (Fenwalker, Lanternbearer, Rogue) erhalten eigenstaendige Stylized-Dark-Fantasy-VFX auf Basis der neuen O1+Flipbook-Pipeline (Custom-Meshes, URP-Ability-Shader, 4x4-Flipbooks, Klassen-Farbidentitaet aus `DiabloPalette`). Primitiv-Platzhalter (`Sphere`/`Cylinder`) und das universelle Recycling von `CleaveArc`/PointLights werden durch klar lesbare, stilkoharente Effekte inkl. Skill-Varianten und Co-op-Cosmetic-Paritaet abgeloest.

## Ist-Befunde
- B1 Drei Klassen mit je 6 Skills (`CINDERREACH.md`): Fenwalker (`Cleave`, `MireHook`, `SeismicSlam`, `BogCharge`, `MireGround`, `IronVow`), Lanternbearer (`LanternBolt`, `Wisps`, `SearingHalo`, `VeilStep`, `MendingLight`, `RenewingCircle`), Rogue (`QuickShot`, `BladeFlurry`, `SmokePot`, `ShadowRoll`, `VenomVial`, `PowderKeg`).
- B2 Aktuell nutzen fast alle Skills Platzhalter: `SeismicSlam`, `SearingHalo`, `BladeFlurry`, `MendingLight`, `RenewingCircle`, `MireGround`, `SmokePot`, `VenomVial`, `BogCharge` und `PowderKeg` recyceln `CleaveArc` oder reine PointLights; `LanternBolt` und `Wisps` nutzen `PrimitiveType.Sphere`; `PowderKeg` nutzt `PrimitiveType.Cylinder`.
- B3 VFX-Tooling-Fundament aus `cinderreach-vfx-tooling` (12 VFX-Meshes, 5 Noise-/Masken-Texturen, 3 4x4-Flipbooks, 3 URP-Ability-Shader) liegt unter `sandbox/Assets/Art/SkillVfx/`.
- B4 `VfxSpawnTests` prueft `PresentationVfx`-Zaehler und `MaxBudget = 2000`; `G-DIABLO02` verlangt die 7 bestehenden `SkillArt/`-Assets; `ServerShaderGuardTests` verbietet ungeschuetztes `new Material(Shader.Find(...))`.

## Entscheidungen
- D1 Stil & Klassen-Identitaet (Eigner): Stylized Dark-Fantasy nach `DiabloPalette` — Fenwalker (Stahl/Moor-Ocker/Bernstein), Lanternbearer (Laternen-Glut/Gold/Aether-Weiss/Heil-Smaragd), Rogue (Kalt-Stahl/Giftgruen/Asche-Rauch/Schatten-Indigo/Fass-Glut).
- D2 Architektur (Eigenentscheidung): Deterministischer Editor-Builder `ClassAbilityVfxBuilder.cs` baut 18 Skill-Prefabs (+ Varianten) unter `Assets/AddressableAssets/SkillVfx/` (Registrierung via `AddressablesSetup.Ensure`); Laufzeit-Fassade `ClassAbilityVfx.cs` mit Test-Seams und Budget-Schutz, bestehende `PresentationVfx`-Zaehler bleiben kompatibel.
- D3 Visuelle Abnahme (Eigenentscheidung): Dedizierter Shotcheck-Modus `-ability-vfx-shots` in `DiabloShotCheck.cs` mit Screenshot-Inspektion aller 3 Klassen (kein Pink-Shader, lesbare Silhouetten).

## Erfolgskriterien
1. Deterministischer Builder `sandbox/Assets/Editor/ClassAbilityVfxBuilder.cs` erzeugt dedizierte VFX-Prefabs und Materialien fuer alle 18 Klassen-Abilities (+ Varianten `CleaveCone90`, `SearingHaloWard`) unter `sandbox/Assets/AddressableAssets/SkillVfx/` und registriert sie via `AddressablesSetup.Ensure` — Nachweis: Batchmode-Build Exit 0 + `ClassAbilityVfxTests` (EditMode) prueft alle 18+ Prefabs, Materialien, Shader und keine Primitiv-Fallbacks.
2. Fenwalker-Kit (6 Skills: `Cleave` 360° & 90°-Variante + Retribution-Tint, `MireHook` Kette+Haken, `SeismicSlam` Riss-Schockwelle, `BogCharge` Pflug-Keil+Moor-Spur, `MireGround` animierte Moor-Zone, `IronVow` Runen-Schildkuppel+Taunt-Ring) im Spiel verdrahtet — Nachweis: `DiabloSkills.cs`/`ClassKitSkills.cs`/`ClassCombos.cs` + PlayMode-Test in `VfxSpawnTests`.
3. Lanternbearer-Kit (6 Skills: `LanternBolt` Flammen-Orb+Streak, `Wisps` Irrlicht-Orb+Strike-Flash, `SearingHalo` Gold-Runenring & persistenter Ward, `VeilStep` Aether-Riss+Rauch-Flipbook, `MendingLight` Ziel-Heilsaeule, `RenewingCircle` Boden-Sanctuary-Kreis) im Spiel verdrahtet — Nachweis: `DiabloSkills.cs`/`Wisp.cs`/`DiabloProjectile.cs`/`SkillVariantTargeting.cs` + PlayMode-Test.
4. Rogue-Kit (6 Skills: `QuickShot` Stahl-Streak + 3er-Faecher, `BladeFlurry` frontaler Doppel-Klingenbogen, `SmokePot` persistente Flipbook-Rauchwolke, `ShadowRoll` Schatten-Keil+Rauchspur, `VenomVial` Gift-Splash-Flipbook+Pfuetze, `PowderKeg` 3D-Fass+Zundschnur-Funken+FireBlast-Flipbook-Explosion) im Spiel verdrahtet — Nachweis: `DiabloSkills.cs`/`ClassKitSkills.cs`/`ClassCombos.cs`/`DiabloProjectile.cs` + PlayMode-Test.
5. Co-op-Cosmetic-Paritaet, Budget & Guards gruen: `ShowRemoteCast`/`ClassKitSkills.ShowRemote` nutzen die neuen VFX ohne Schaden; `PresentationVfx.ActiveParticles() <= 2000`; `ServerShaderGuardTests`, `check-hotpath-caching.py`, `check-meta.py`, `asset-bloat-audit.py`, `KayKitAssets.Verify`, komplette EditMode- & PlayMode-Suites (`docs/test-runs.jsonl`, `failed == 0`) — Nachweis: `bash scripts/run-unity-tests.sh` + `repo.yaml:verify`.
6. Visuelle Abnahme per Screenshot fuer alle 3 Klassen (Fenwalker, Lanternbearer, Rogue) erstellt und geprueft (keine Magenta-Shader, klare Silhouetten/Farbidentitaet) — Nachweis: `-ability-vfx-shots`-Capture + Bildpfade unter `docs/ability-vfx-shots/` (kompakte Kontaktboegen/Shots) und Sichtpruefung protokolliert.

## Nicht-Ziele
- Keine Gameplay-, Balance-, Damage-, Cooldown- oder Enum-Int-Aenderungen (`DiabloSkills.Skill` unveraendert).
- Keine Boss-Ability-Neugestaltung (nur die 3 Spielerklassen).
- Keine neuen SFX/Voice-Clips (`check-voice-lines.py` bleibt gruen).

## Abhängigkeiten
- `cinderreach-vfx-tooling` (100 %, auf `main` gelandet).
