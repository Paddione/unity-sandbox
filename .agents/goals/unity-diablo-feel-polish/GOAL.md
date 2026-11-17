---
goal: unity-diablo-feel-polish
slug: unity-diablo-feel-polish
health_goals: none (einmaliger Gameplay-Polish wie unity-diablo-d2-combat-feel; Verifikation über EditMode/PlayMode-Suiten und VerifyDiabloZone)
plan_refs: inline session plan 2026-11-17 (Owner-Wahl "Feel-Polish", kein Plan-File)
---

# Goal: Blood Moor feel polish (husk anims, corpses, avoidance, hit-stop)

## Objective

Close the playtest follow-ups from the D2 combat-feel cut: husks look alive,
deaths leave a body, husks walk around dressing instead of through it, and
landed hits get a short freeze-frame. Solo-first, co-op paths untouched.

## Erfolgskriterien (messbar)

1. Husks run a Mecanim controller from KayKit Rig_Medium takes (Idle, Walk
   while approaching, Attack on wind-up, Hit on damage); pinned by EditMode
   controller test + PlayMode counters.
2. Hit flash reaches the KayKit child renderers (root has none); pinned by a
   PlayMode test on a child-renderer husk.
3. Death leaves a `DiabloCorpse` (Death take, no collider, untagged) that
   sinks and self-destroys; husk object still destroyed (kill contracts kept).
4. Approaching husks steer around static colliders and keep separation from
   each other; pinned by a PlayMode wall test.
5. Landed solo hits trigger a realtime-bounded hit-stop that always restores
   `Time.timeScale`; skipped while netcode is listening.
6. EditMode + PlayMode suites green, `VerifyDiabloZone` green.

## Nicht-Ziele

Vendors/potions/gold, new zones, damage numbers, screenshake, co-op corpse
replication, NavMesh baking, gamepad.
