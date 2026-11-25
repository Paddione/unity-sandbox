# Fortschritt – cinderreach-group-quest-map

Prozent: 100 %

## Erledigt

- K1 Geteilte Dialoge: `StoryDialogShare` (Protokoll Open/Line/Close, Zeilenkodierung, Host-Prüfung `Valid`),
  `DiabloNetworkPlayer.Story.cs` (Owner → ServerRpc → ClientRpc an alle außer den Sprecher), Zuhörerfenster in
  `StoryDialogUI` (`ShowShared`/`SharedLine`/`SharedClose`/`HideShared`, nicht modal, Stimme läuft mit, eigener Dialog
  hat Vorrang). Belege `GroupQuestMapTests.DialogShare_*`, `GroupQuestMapPlayTests.Coop_ClientDialogReachesGroup_HostBarkReachesClient`
  (NGO Host+Client, gefälschte Zeilen werden vom Host verworfen), Windows-Shot `dialog_shared_listener` (sha16 `9b8b864704a34bcb`).
- K2 Zurufe: `StoryBarks.Relay` → `BroadcastBark` → `SayRemote` auf Clients. Koop-Paar-Beleg: der Client erhielt alle
  7 Zurufe (abbot/kilnmother/glasswright phase2+death, h7.done). Beleg `GroupQuestMapTests.Barks_LocalSayRelaysRemoteSayDoesNot`.
- K3 Tracking: `StoryTracking` (Flag `untrack:<id>`, max. 4, Haupt vor Neben), Tracker-Text mit Zielen, Zählern, Abgabe und
  Meter/Zone (`StoryDirector.TrackerText`), Fortschritts-Toasts (`ProgressToast`), Fuhrbuch mit Angeboten, Verfolgen-Schalter
  und Zielzone. Belege `GroupQuestMapTests.Tracking_*`, `Tracker_*`, `ProgressToast_*`, Shot `quest_log_tracking` (`cdfa6776de13f2ee`).
- K4 Zielorte: `QuestDestinations` + `SceneQuestWorld`, Routing über `ZoneMap.NextHop`. Belege `Destinations_*`, `Route_*`.
- K5 Karten: `ZoneMap` (Top-down-Render einmal pro Zone, Projektion), `MapIcons`/`MapScene`, `MinimapHud`, `WorldMapUI` (M).
  Belege `ZoneMap_IndicesHopsAndProjection`, `Minimap_AndZoneMap_ShowHeroAndQuestPlaces`, Shots `map_minimap_tracker`
  (`03a6516761e5b8c7`), `map_zone_fen` (`479f630777395d3f`), `map_zone_ossuary` (`e70440d31537d02a`).
- Nebenbefund behoben: Heldenklone (`CompanionFactory.CloneHeroVisual`, KI-Begleiter und Paperdoll) trugen `StoryDirector`
  und bauten ein zweites Story-HUD (sichtbar als überlagerte Entfernungen). Strip-Liste erweitert, Beleg
  `HeroClone_CarriesNoStoryHudOrMaps`.
- Testleck behoben: `VfxSpawnTests.LootDrop_RarePlusSpawnsBeam` ließ 40 Goldhaufen am Ursprung liegen, die
  `WorldInteractionTests.ContractBoard_ReachOpenClaimAndRollback` einsammelte (8369 statt 130 Gold, 2 von 3 Volläufen).
- K6 Abnahme: EditMode 598/598, PlayMode 357/357, `check-meta.py` OK. Autoplay (Xvfb) DONE, Phasen town … complete,
  0 Exceptions. Koop-Paar Host und Client Exit 0, je 0 Exceptions. Windows-Shotcheck `-map-shots` 1920×1080 Exit 0,
  Ablage `~/unity-sandbox/artifacts/group-quest-map/`.
- Health: G-DIABLO01 (`check-meta` OK), G-DIABLO02/03 (Suiten grün), G-DIABLO06 (Autoplay DONE, 0 Exceptions).

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
