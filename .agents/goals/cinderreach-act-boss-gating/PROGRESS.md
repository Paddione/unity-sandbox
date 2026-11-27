# Progress: cinderreach-act-boss-gating

Prozent: 100 %

## Erledigt

- 2026-11-27: Goal angelegt nach setgoal-Runde (Boss-Set Kilnmother/Glasswright/Thewes, Haupt- + Nebenquests,
  sofortige Spawn-Cutscene mit Fernschnitt ohne Captions, Host-Queststand gilt im Koop, eigenes Goal nach akt3-S4).
- 2026-11-27: Umsetzung (Branch `cinderreach-act-boss-gating-s1`): `ActBossGate` (Pflichtmenge aus Katalog,
  Dormanz, Wake 1x/Lauf, Flag `bossspawn.<akt>`, Kill-Log), `CutsceneDirector.TryStartSpawnIntro` (Fernschnitt im
  Spielwinkel, `OwnsCamera` sperrt die Click-Kamera, keine Fähigkeits-Captions, Banner im unteren Balken,
  Thewes-Intro), Koop-Maske `NotifyActBossesClientRpc`, `MaybeSpawnThewesAtH11` ersetzt, Autoplay
  (`QuestsUntilBoss`, Phase `boss4`, `actBossKills`, Quest-Modus immer an, `-autoplay-no-quests` entfernt),
  Shotcheck-Posen `*_spawn_cutscene`.
- Belege (K1–K9):
  - K1/K6: `ActBossGateTests.RequiredQuests_AreEveryOtherQuestOfTheAct` und `..._NeverWaitOnTheirOwnOrALaterBoss`.
  - K2: `Boss_StaysDormantUntilTheLastQuest_ThenWakesOnceWithCutscene`, `PlayedSpawnCutscene_DoesNotReplayAfterLoad`,
    `DefeatedAct_NeverHidesTheBoss`, PlayMode `Akt3FinaleTests.Thewes_ErscheintErstNachAllenAkt3Quests`.
  - K3/K4: `SpawnCutscene_FramesTheBossDuringEveryShowcaseAndReturnsWithoutCaptions` (Viewport 0,1–0,9 je 0,5 s,
    Banner = Bossname), `CutsceneDirectorTests.ShowcaseData_EveryBossHasAbilitiesWithoutCaptions`; Windows-Shotcheck
    1920×1080 `58_kilnmother_spawn_cutscene`, `63_glasswright_spawn_cutscene`, `66_thewes_spawn_cutscene`
    (Boss mittig, erster Cast läuft, nur Namensbanner).
  - K5: `MaybeSpawnThewesAtH11` entfernt, Akt3Finale-Test umgestellt.
  - K7: Koop-Paar Exit 0 (Host 289 s, Phasen inkl. `boss4`/`quests`; Client alle Phasen,
    Log `Spawn-Cutscene intro.kilnmother/intro.glasswright (authority=False)`).
  - K8: `run-autoplay.sh -seed 0` Exit 0, 293,6 s, 34/34 Quests, `caughtUp=[]`,
    `actBossKills=[1:Kilnmother:-, 2:Glasswright:-, 3:Thewes:-]`, Spawns 1/1/1 mit Cutscene.
  - K9: EditMode 727/727, PlayMode 410/410, `check-meta` OK, Quest-Coverage 0 uncovered, Voice 244/0.

- 2026-11-27: Gelandet `54ae7f2` (ff), Dotfiles-Probe `81b16e5c` (`AUTOPLAY_PHASES` + `boss4`). CI-Autoplay
  Run 46004537242 grün: Normal 417 s, Kindled 537 s, Lanternbearer 410 s, je Exit 0, 34/34 Quests,
  `actBossKills=[1:Kilnmother:-, 2:Glasswright:-, 3:Thewes:-]`. `G-DIABLO06`: 0 Befunde (11 Phasen je Gruppe).

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
