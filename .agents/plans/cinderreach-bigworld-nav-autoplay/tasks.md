---
goal: cinderreach-bigworld-nav-autoplay
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
---

# Tasks: cinderreach-bigworld-nav-autoplay

## S1 — Erreichbarkeit alle Varianten
- Gezielt: `PLATFORMS=EditMode TEST_FILTER=LayoutSeedTests bash scripts/run-unity-tests.sh` grün (Kriterium 1, 81 Varianten inkl. neuer Außenflächen).
- Bei Flood-Lücken: Fix in `LayoutSeedTests.cs` (Flood berücksichtigt neue Anker) oder Builder (fehlende begehbare Verbindung), mit Begründung im Commit.
- Target files: `sandbox/Assets/Tests/EditMode/LayoutSeedTests.cs`, bei Bedarf `sandbox/Assets/Editor/*Builder.cs` + `sandbox/Assets/Scenes/DiabloZone.unity`.

## S2 — Autoplay Route seed 0
- `bash scripts/run-autoplay.sh <out> -seed 0` Exit 0 bis `complete`; prüfen: alle Story-Quests `Done`, `questsCaughtUp == 0`, `actBossKills` je Akt `-`.
- Bei Hängern: erst `navPlans`/`navWarps`/Stall-Stelle aus `autoplay-result.json` + Logs lesen, dann minimal in `AutoplayMover`/`SessionAutoplay` beheben (keine KI-Umstellung, nur Robustheit für große Böden).
- Target files: `sandbox/Assets/Scripts/Autoplay/*` (nur bei Befund), sonst nur Belegdateien unter `docs/`.

## S3 — Vollnachweis + Abschluss
- Voll: `bash scripts/run-unity-tests.sh` grün (EditMode ≥ 791, PlayMode ≥ 432), neue Zeilen in `docs/test-runs.jsonl`; `SandboxBuilder.VerifyDiabloZone` + `KayKitAssets.Verify` + `python3 scripts/check-meta.py` grün.
- `PROGRESS.md` je Kriterium mit Beleg nachtragen, `Prozent: 100 %`, Lint `node ~/scripts/goal-lint.mjs --repo unity-sandbox` grün.
- Stand in `.agents/goals/CINDERREACH.md` einarbeiten (Qualitätsnetz/Autoplay-Abschnitt: 4×-Beleg), Goal-Verzeichnis + Plan entfernen.
- Target files: `.agents/goals/cinderreach-bigworld-nav-autoplay/PROGRESS.md`, `.agents/goals/CINDERREACH.md`, `docs/test-runs.jsonl` (nur neue Zeilen), `docs/dev-journal.md`.
