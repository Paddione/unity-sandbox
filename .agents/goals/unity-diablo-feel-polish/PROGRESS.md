# PROGRESS — unity-diablo-feel-polish

Prozent: 100 %

## Erledigt

- 2026-11-17: Owner-Wahl "Feel-Polish", Goal angelegt.
- 2026-11-17: K1 `HuskLocomotion.controller` + `HuskAnimator` auf allen Zonen-Husks (EditMode `HuskController_HasAllStatesAndParams`, `ZoneHusks_AreAnimated`; PlayMode `Windup_PlaysAttackTake`, Walk in `Approach_WalksAnimatedAroundWall`).
- 2026-11-17: K2 Flash an Kind-Renderern (PlayMode `Hit_FlashesChildRendererAndAnimates`).
- 2026-11-17: K3 `DiabloCorpse` inert, versinkt, räumt sich ab; Husk-Objekt weiter zerstört (PlayMode `Kill_LeavesInertCorpseThatSinksAway`, alle bestehenden Kill-Asserts grün).
- 2026-11-17: K4 Steering um Wand (PlayMode `Approach_WalksAnimatedAroundWall`).
- 2026-11-17: K5 Hit-Stop solo, timeScale restauriert (PlayMode `HitStop_FreezesBrieflyAndRestores`, EditMode `HitStop_StaysInertOutsidePlayMode`).
- 2026-11-17: K6 EditMode 39/39, PlayMode 33/33, `VerifyDiabloZone` OK (husks=10 armed=True).

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
