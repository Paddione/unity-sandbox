# Progress: cinderreach-bigworld-nav-autoplay

Prozent: 100 %

## Erledigt
- 2026-11-28: Kriterium 1 — `PLATFORMS=EditMode TEST_FILTER=LayoutSeedTests bash scripts/run-unity-tests.sh` 5/5 grün (81 Varianten: Flood erreicht Fen-Tor, Truhe und alle Gegneranker inkl. neuer Außenflächen).
- 2026-11-28: Kriterium 2 — `bash scripts/run-autoplay.sh /tmp/autoplay-bigworld -seed 0` Exit 0: Phasen `town … complete` + `boss4` + `quests`, 34/34 Quests `Done` ohne Catch-up (`questsCaughtUp: []`), `actBossKills` je Akt `-`, `exceptions: 0`, `navPlans: 126`/`navWarps: 4` (`/tmp/autoplay-bigworld/autoplay-result.json`). Hinweis: Lauf aus dem Encounter-Worktree (Spielcode identisch zu `main`, nur Docs/`.agents` unterschieden sich); das Linux-fps-gate ist advisory und entfiel wegen Worktree-Abräumung nach `land` (Datei-Pfadfehler, kein Spielbefund).
- 2026-11-28: Kriterium 3 — Voll-Suiten grün: EditMode 791/791, PlayMode 432/432, `failed == 0` (Belegzeilen Commit `a27d2d5` in `docs/test-runs.jsonl`); `SandboxBuilder.VerifyDiabloZone` OK, `KayKitAssets.Verify` Exit 0, `python3 scripts/check-meta.py` OK, `python3 -m pytest scripts -q` 67 passed.

## Nächste Schritte
- Keine.

## Offene Punkte
- Keine.
