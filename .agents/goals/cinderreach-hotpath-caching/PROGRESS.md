# Progress: cinderreach-hotpath-caching

Prozent: 95 %

## Erledigt

- 2026-11-26: Goal aus Code-Analyzer-Befund `CA-01` und `/setgoal`-Klärungsrunde auf Basis `main` (`5bd41d3`) angelegt und auf `aaa2571` gerebased.
- 2026-11-27 (S1): Hot-Path-Caching in `sandbox/Assets/Scripts/Loot/LootDropView.cs` (`cachedBall`, `cachedIcon`, `cachedCam` + `ResolveCamera()`, `LateUpdate()` ohne `Camera.main`/`.Find`) und `sandbox/Assets/Scripts/DiabloClickController.cs` (`cachedCam`, `cachedInventoryUi` + `ResolveCamera()`/`ResolveInventoryUI()`, `UpdateCamera(Camera cam)`) umgesetzt; `ItemPresentationTests` (`6/6 passed`, inkl. neuem `GroundView_BillboardsIcon_AndFollowsCameraSwap`) grün.
- 2026-11-27 (S2): Statische Health-Probe `scripts/check-hotpath-caching.py` (`G-DIABLO08`) und `scripts/test_check_hotpath-caching.py` erstellt; `python3 scripts/check-hotpath-caching.py` (`OK`), `python3 -m pytest scripts -q` (`30 passed`) und `python3 scripts/check-meta.py` (`OK`) grün.
- 2026-11-27 (S3 teilweise): Voller Lauf auf Basis `aaa2571` liefert `EditMode: 665/665 passed` und `PlayMode: 376/377 passed` (einziger Fehlschlag vorbestehend auf `main`, siehe Offene Punkte).

## Nächste Schritte

- Nach Fix des vorbestehenden `main`-Fehlers `VfxSpawnTests.Boss_TelegraphAndPhaseTwoFire` (`71a6137`) vollen `PlayMode`-Lauf nachmessen und Goal auf 100 % schließen.

## Offene Punkte

- Vorbestehender `main`-Blocker `VfxSpawnTests.Boss_TelegraphAndPhaseTwoFire` (`71a6137`: `StoryDirector.Update` → `CutsceneDirector.Tick` setzt `HoldBosses = true` bei `"Hollow Abbot"` ≤ 12 m und blockiert `BossController.Update:70`, weshalb `PresentationVfx.BossTelegraphs == 0` bleibt; außerhalb `cinderreach-hotpath-caching`).
