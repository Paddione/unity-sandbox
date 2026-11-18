# Mechanics questions — unity-sandbox

One prototype, one question. Each question has an exit criterion and ends
in kill / pivot / continue (logged in `docs/decision-log.md`).
Greybox discipline: primitives only, fresh URP/Lit materials, code-owned
`ThirdPersonController` + `ToyTrigger` rig (no Asset-Store fetch required).

## Q1 — Movement feel (continue)

- Question: does the hand-rolled third-person controller feel good enough
  to carry mechanics tests (walk, sprint, jump across the block line)?
- Setup: `Greybox` scene — ground, 6 jump blocks (0.5–2 m), 4 walls,
  capsule player (`CharacterController` + `ThirdPersonController`:
  WASD/arrows, mouse look, Space jump, Shift sprint).
- Exit criterion: Linux player build runs the scene with zero errors and
  the controller bindings work (code-defined, Input System package).
- Headless proof: `docs/q1-run-log.md` (build run + `VerifyGreybox`).

## Q2 — Trigger interaction (continue)

- Question: do trigger volumes reliably detect the player for toy logic?
- Setup: `TriggerZone` (orange cube, `BoxCollider.isTrigger`) over the
  block line with `ToyTrigger` in `Counter` mode; every player entry logs
  `[Toy] Trigger zone entered (entries=N)`.
- Exit criterion: scene contains ≥ 1 trigger zone wired to `ToyTrigger`
  and the player build compiles and runs with zero errors.
- Headless proof: `SandboxBuilder.VerifyGreybox` counts the zone;
  live entry counting is a human-playtest follow-up.

## Q3 — Toy vs goal loop (continue)

- Question: does "collect 3 orbs, then the goal pad arms" read as a
  complete toy-vs-goal loop?
- Setup: 3 pickup orbs (`Orb_0..2`, yellow spheres, `ToyTrigger` in
  `Pickup` mode, `Needed = 3`) + `GoalPad` (red → green when armed,
  `ToyTrigger` in `Goal` mode). Locked goal logs `Goal locked (x/3)`,
  armed goal logs `Goal reached … Loop complete`.
- Exit criterion: scene contains ≥ 3 pickups + 1 goal pad wired to the
  shared counter, build runs with zero errors, decision logged.
- Headless proof: `VerifyGreybox` inventory + run log; full collect
  playthrough is a human-playtest follow-up (see `docs/playtest-protocol.md`).

## Q5 — Click-to-move kill-loot (with KayKit figures)

- Question: is the solo click-to-move kill-loot loop fun? (move → melee
  3 skeleton husks → orbs drop → extraction arms → loop complete).
- Setup: new `Diablo.unity` scene (Greybox unchanged): isometric fixed-angle
  camera + `DiabloClickController` (ground-plane click-to-move, click-husk =
  attack-move), `DiabloMelee` (range arc, cooldown), 3 `Skeletons`-pack husks
  with `EnemyHealth` (HP, death drops pickup orb), Adventurers hero named
  `Player`, orb loot + extraction pad reusing Q3 `ToyTrigger` semantics
  (`Needed = 3`).
- Exit criterion: Linux player build `builds/diablo/diablo` runs the scene
  with zero errors; `VerifyDiablo` inventory green; PlayMode combat test
  green; decision logged as Q5 continue/pivot/kill.
- Solo only: `NetworkManager` layer stays in Greybox, untouched; no NGO
  work in P1. Human feel session is follow-up, not a criterion (Q1–Q3
  precedent).
- Headless proof: `VerifyDiablo` inventory + `docs/diablo-run-log.md`;
  full loop playthrough is a human-playtest follow-up.

## Q4 — Co-op host/join (multiplayer-testable)

- Question: can two players share the greybox loop without builds (host in main editor, client in ParrelSync clone)?
- Setup: `NetworkManager` + `UnityTransport` (127.0.0.1:7777) + `MultiplayerAutoStarter` (hosts in main, connects as client in `_clone_*` paths or with `--net-client`); `NetworkPlayer` prefab (capsule + CharacterController + NetworkObject + NetworkTransform + owner-only `ThirdPersonController`); solo Player untouched.
- Exit criterion: `VerifyMultiplayer` green (manager + transport assignment + auto-starter + prefab), Linux build hosts with zero errors, `--net-client` logs client connect path.
- Headless proof: `docs/q4-run-log.md` (BuildAll + verifies + EditMode 6/6 + host/client run logs).

## Q6 — Co-op kill-loot-extract with rarity chase (P2 zone)

- Question: is the co-op kill-loot-extract loop with a Magic/Rare/Unique
  rarity chase fun in a first explorable crypt zone?
- Setup: new `DiabloZone.unity` scene ("Ember Crypt Approach"): south stairs
  entry → crypt hall (Diablo room kit) → west elite alcove (`Husk_Elite`,
  15 HP, chest with guaranteed Rare+) → east rubble (blocked) → north
  crypt-gate extraction (`GoalPad`, Q3 semantics, `Needed = 3` unchanged).
  6 husks (`Normal`, 3 HP) + 4 `CoopSpawn` markers; hero + `NetworkPlayerDiablo`
  prefab carry `Equipment` (Armor/Weapon/Helmet). Twin-stick hybrid controls:
  WASD/arrows move camera-relative, hero faces the cursor, click strikes in
  an arc (clicked husk keeps the Q5 attack-move chase). Host-authoritative NGO
  (owner-only input, `DiabloNetworkPlayer.SubmitAttackServerRpc` with
  4 m range check, server-side equip + despawn, client prediction).
  Kills roll `LootTable` (Normal 70 % drop: 80/18/2; Elite 100 %:
  50/40/10; Chest 100 %: 0/80/20); no-drop rolls keep the legacy orb.
- Exit criterion: `VerifyDiabloZone` green, Linux player build
  `builds/diablo-zone/diablo-zone` runs with zero errors, PlayMode loot
  tests green, decision logged as Q6 continue/pivot/kill.
- Client pickup without server is prediction-only (server despawn wins);
  cross-machine Relay join + 5-person human playtest stay follow-ups
  (Q1–Q5 precedent).
- Headless proof: `VerifyDiabloZone` inventory + `docs/diablo-p2-run-log.md`.

## Q7 — Does the Blood Moor camp loop with inventory carry the prototype?

- Rogue Encampment (spawn, campfire heal, palisade) → clear 10 moor husks + brute → loot to bag (I to browse/equip) → extract at the Moor Gate. Starter sword (+2) one-shots normal husks (3 dmg vs 3 HP); elites need sustained strikes.
- Exit criterion: `VerifyDiabloZone` green, Linux/Windows/server builds run with zero errors, EditMode + PlayMode blood-moor tests green, decision logged as Q7 continue/pivot/kill.
- Husks stay passive and HP/inventory stay local in this cut; retaliation + coop inventory sync are the Q8 candidates.
- Headless proof: `VerifyDiabloZone` inventory + run log (this session).

## Q8 — Fühlt sich der Moor-Kampf wie D2 Act 1 an?

- Jeder Treffer: Schwung-Sound, Treffer-Thump, Hit-Flash, Knockback-Pop (0,6/0,2 m), HP-Balken am getroffenen Husk (3 s Fade); Kills: Death-Rattle + Loot-Blip. Whiffs: Schritt-in-den-Strike (0,5 m) + Whoosh.
- Husks schlagen zurück (0,4-s-Telegraph per Ducken, 1/2 Schaden, 1,5 s Cooldown); Tod → Respawn am Camp mit vollem HP. Kein Corpse-Run, keine Damage-Numbers, kein Screenshake (nicht D2).
- Exit criterion: `VerifyDiabloZone` grün, EditMode + PlayMode grün, Windows-GPU-Playtest mit Ein-Satz-Verdikt.
- Headless proof: `VerifyDiabloZone` inventory + Suiten grün (dieser Branch).

## Q9 — Trägt die Session-Schleife? (S6 Vollpass mit S5: JA → continue)

- Question: trägt die Cinderreach-Session-Schleife (Lantern Hold → Ashen Fen
  clearen → Loot/Gold → Vendor → Punkte vergeben → Fen Gate → Ossuary →
  Boss → Abschluss) als beweisbarer Lauf?
- Setup: `Scripts/SessionAutoplay.cs` (`-autoplay`, inert ohne Flag):
  geskripteter Held (Wegpunkte, Kampf per `DiabloMelee` + Q/E/R-Skills,
  Trank-Kauf über S3-Seam, FenGate↔OssuaryGate über `ZoneGate`,
  Bone-Warden-Kampf, Hollow-Abbot-Kampf Phase 1/2, `SessionComplete`-Overlay,
  Rückweg); Cheat nur `-autoplay-god` (im Log ausgewiesen).
- Exit criterion: Linux-Player-Lauf `-autoplay` Exit-Code 0, 0 Exceptions,
  `bossKills=1`, `overlay=1` — erfüllt (`DONE phases=7 huskKills=13
  bossKills=1 phaseTwo=1 overlay=1 exceptions=0`, `/tmp/s6-autoplay-full.log`);
  `DiabloShotCheck` mit 22 Shots inkl. Ossuary/Boss/Overlay.
- Headless proof: `docs/session-loop-run-log.md` (Vollpass-Abschnitt).
- Windows: `stage-windows.sh` → `shotcheck-windows.sh` OK, PNGs 19–22 geprüft.
- Grenze: Ossuary-Leash findet keine Ziele (Folgepass Leash/Anmarsch).

- Historie: Basis-Teil ohne S5 (Seams) siehe Git-Historie dieser Datei.
