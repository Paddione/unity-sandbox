---
goal: cinderreach-bigworld-encounters
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
---

# Tasks: cinderreach-bigworld-encounters

## S1 — Beleghygiene + gezielte Encounter-Regression
- `docs/test-runs.jsonl` Merge-Marker (Zeilen 1–5) entfernen, JSONL-Gültigkeit prüfen (`python3 -c "import json; [json.loads(l) for l in open('docs/test-runs.jsonl') if l.strip()]"`).
- Gezielt: `PLATFORMS=EditMode TEST_FILTER=WorldEdgeTests bash scripts/run-unity-tests.sh` grün (Kriterien 1–2).
- Target files: `docs/test-runs.jsonl` (nur Marker-Entfernung, keine Belegzeilen), keine Spielcode-Änderung außer falls Tests echte Safe-Verstöße zeigen.

## S2 — Szenen-Verträge
- `SandboxBuilder.VerifyDiabloZone` grün, `KayKitAssets.Verify` grün (`repo.yaml:verify`), `python3 scripts/check-meta.py` grün.
- Falls Verify Fehler in der 4× Welt zeigt (Wandlücken, Bodenlöcher, Encounters außerhalb), minimal in Buildern beheben (`TownBuilder.cs`, `OssuaryBuilder.cs`, `KilnBuilder.cs`, `BarrensBuilder.cs`, `EncounterBuilder.cs`, `SandboxBuilder.cs`), Szene regenerieren, Builder-Seiteneffekte (LFS-Controller) revertieren.
- Target files: `sandbox/Assets/Editor/*Builder.cs`, `sandbox/Assets/Scenes/DiabloZone.unity` (nur falls nötig), `sandbox/Assets/Scripts/Zones/WorldBounds.cs` (nur falls SSOT-Korrektur nötig).

## S3 — Vollnachweis + Abschluss
- Voll: `bash scripts/run-unity-tests.sh` grün (EditMode ≥ 791, PlayMode ≥ 432, `failed == 0`), neue Zeilen in `docs/test-runs.jsonl` anhängen.
- `PROGRESS.md` je Kriterium mit Beleg nachtragen, `Prozent: 100 %`, `Nächste Schritte`/`Offene Punkte` auf `Keine.`, `node ~/scripts/goal-lint.mjs --repo unity-sandbox`.
- Stand in `.agents/goals/CINDERREACH.md` einarbeiten (§ Gebietsgrößen und Begegnungen + Qualitätsnetz-Zahlen), Goal-Verzeichnis + Plan entfernen.
- Target files: `.agents/goals/cinderreach-bigworld-encounters/PROGRESS.md`, `.agents/goals/CINDERREACH.md`, `docs/test-runs.jsonl` (nur neue Zeilen), `docs/dev-journal.md`.
