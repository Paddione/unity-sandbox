---
title: "Cinderreach Hot-Path Caching (LootDropView & DiabloClickController)"
domains: [unity, gameplay, performance]
status: in_progress
goal: cinderreach-hotpath-caching
slug: cinderreach-hotpath-caching
branch: feature/cinderreach-hotpath-caching
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
health_probe: {"id":"G-DIABLO08","title":"No per-frame Camera.main or transform.Find in LootDropView.LateUpdate and DiabloClickController.Update/UpdateCamera","cmd":"cd ~/unity-sandbox && python3 scripts/check-hotpath-caching.py >/dev/null 2>&1; echo $?","target":0}
---

# cinderreach-hotpath-caching — Implementation Plan

[Ziel und Erfolgskriterien](../../goals/cinderreach-hotpath-caching/GOAL.md). Planbasis `aaa2571`. Pfade relativ zu `sandbox/Assets/`.

| Stufe | Zuständigkeit und Dateipfade | Prüfung |
| --- | --- | --- |
| S1 Hot-Path-Caching (erledigt) | `Scripts/Loot/LootDropView.cs`, `Scripts/DiabloClickController.cs`, `Tests/PlayMode/ItemPresentationTests.cs` | `ItemPresentationTests` 6/6 grün (inkl. `GroundView_BillboardsIcon_AndFollowsCameraSwap`) |
| S2 Health-Probe `G-DIABLO08` (erledigt) | `scripts/check-hotpath-caching.py`, `scripts/test_check_hotpath-caching.py` | `python3 scripts/check-hotpath-caching.py` OK, `pytest scripts` 30 passed |
| S3 Vollabnahme (blockiert durch `71a6137` auf `main`) | EditMode + PlayMode komplett, `check-meta.py`, `goal-lint.mjs`, `KayKitAssets.Verify` | `EditMode` 665/665 grün; `PlayMode` 376/377 (vorbestehender Blocker `VfxSpawnTests.Boss_TelegraphAndPhaseTwoFire` aus `71a6137`) |
