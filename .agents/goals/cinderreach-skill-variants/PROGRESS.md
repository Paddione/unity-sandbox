# Progress: cinderreach-skill-variants

Prozent: 60 %

## Erledigt

- Ziel und Stufenplan angelegt.
- S1 umgesetzt: `SkillVariantCatalog` (je 2 Varianten pro Klassen-Skill mit stabiler String-ID, Vorschau == Laufzeitwerte), `RunProfile`-Wahl, Charakterbogen-Auswahl nur solo im Lantern-Hold (räumliche Town-Abgrenzung; Fen lehnt ab), Save v7 additiv (`SaveGame`/`SaveMigrations` V4→V7 direkt + tolerante V5/V6→V7, `SaveService` Capture/Apply, `RunProfile.Reset` leert Wahl). Alte Saves starten mit der bisherigen Variante.
- S2 umgesetzt: `SkillVariantTargeting.InCleaveArea` planar (frontal 90°/1,5× Reichweite, rundum Bestand; gleiche Damage-/Cooldown-Pfade), ortsfeste Halo-Zone am Cast-Punkt mit doppelter Dauer bei gleichem Tick-Intervall und gleicher Fläche, mitlaufend = Bestand. Recasts ersetzen, Tod (via LastDeathTime-Marke, kein PlayerHealth-Eingriff) und Szenenwechsel räumen auf. Co-op läuft über den Basis-Skillpfad (`ActiveVariantId` fällt bei gespawntem NetPlayer auf Default zurück).
- Neue Tests: `SkillVariantTests` (EditMode, 8 Tests), `SkillVariantCombatTests` (PlayMode: Cleave_BaselineHitsBehindAndConeHitsFarAhead, Halo_MovesOrRemainsAtCastPoint, Variant_ReloadAndDefaults, Kegelrand, Co-op-Basis). `SaveMigrationTests` auf v7 gehoben + V4→V7-, V5→V7- und Roundtrip-Tests.
- `scripts/check-meta.py` und `agent-workspace check`: Exit 0.

## Nächste Schritte

- Unity-Suiten (`scripts/run-unity-tests.sh`) und `VerifyDiabloZone` auswerten; bei Grün `run-autoplay.sh` (8 Phasen, keine Exceptions) fahren.
- Manueller Lauf-Beleg pro Klasse (beide Varianten, gleiche Ausrüstung, zwei Kampfsituationen) als Notiz festhalten.
- S3-Integrationsnachweis laut Plan.

## Offene Punkte

- Test-/Autoplay-Ergebnisse stehen aus (Unity-Lauf angestoßen); keine 100 % ohne S3.
