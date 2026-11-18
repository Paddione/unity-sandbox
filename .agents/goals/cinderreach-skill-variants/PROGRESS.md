# Progress: cinderreach-skill-variants

Prozent: 100 %

## Erledigt

- Ziel und Stufenplan angelegt.
- S1 umgesetzt: `SkillVariantCatalog` (je 2 Varianten pro Klassen-Skill mit stabiler String-ID, Vorschau == Laufzeitwerte), `RunProfile`-Wahl, Charakterbogen-Auswahl nur solo im Lantern-Hold (räumliche Town-Abgrenzung; Fen lehnt ab), Save v7 additiv (`SaveGame`/`SaveMigrations` V4→V7 direkt + tolerante V5/V6→V7, `SaveService` Capture/Apply, `RunProfile.Reset` leert Wahl). Alte Saves starten mit der bisherigen Variante.
- S2 umgesetzt: `SkillVariantTargeting.InCleaveArea` planar (frontal 90°/1,5× Reichweite, rundum Bestand; gleiche Damage-/Cooldown-Pfade), ortsfeste Halo-Zone am Cast-Punkt mit doppelter Dauer bei gleichem Tick-Intervall und gleicher Fläche, mitlaufend = Bestand. Recasts ersetzen, Tod (via LastDeathTime-Marke, kein PlayerHealth-Eingriff) und Szenenwechsel räumen auf. Co-op läuft über den Basis-Skillpfad (`ActiveVariantId` fällt bei gespawntem NetPlayer auf Default zurück).
- Neue Tests: `SkillVariantTests` (EditMode, 8 Tests), `SkillVariantCombatTests` (PlayMode: Cleave_BaselineHitsBehindAndConeHitsFarAhead, Halo_MovesOrRemainsAtCastPoint, Variant_ReloadAndDefaults, Kegelrand, Co-op-Basis). `SaveMigrationTests` auf v7 gehoben + V4→V7-, V5→V7- und Roundtrip-Tests.
- `scripts/check-meta.py` und `agent-workspace check`: Exit 0.

- S3 Integration/Nachweis (Übernahme durch opencode-Session ses_e115042b; Rebase auf v6-main d143229, Kette v4→v5→V6→V7, `V4ToV7`-Direktsprung entfernt, `V56ToV7` zu `V6ToV7` gestrafft, #8-Tests auf versionsagnostisch gehoben):
  - `bash scripts/run-unity-tests.sh` Exit 0: EditMode 257/257, PlayMode 108/108 (`/tmp/sv-full3.log` + `/tmp/sv-edit.log`; 3 #8-Migrationstests auf `CurrentVersion` gehoben, kein Verhaltensfehler).
  - `SandboxBuilder.VerifyDiabloZone` Exit 0 (`/tmp/sv-verify.log`).
  - `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-skill-variants-normal" -seed 0` Exit 0: fps-gate PASS (worst avg 37,6 ≥ 30,0; `/tmp/sv-autoplay.log`).
  - Variantenvergleich je Klasse: `SkillVariantCombatTests` (PlayMode, grün — Cleave rundum vs. Frontalkegel an Umzingelung/Distanz, Halo mitlaufend vs. ortsfest an beweglichem Kampf/Engstelle, beide Klassen, Reload- und Co-op-Fälle).
  - `python3 scripts/check-meta.py` Exit 0, `agent-workspace check` still vor Commit.

## Nächste Schritte

Keine — alle drei Stufen belegt und gelandet.

## Offene Punkte

Keine.
