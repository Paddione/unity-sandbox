# Dev-journal — unity-sandbox

Per session: done / faced / learned / next. Against scope creep:
anything outside Q1–Q3 greybox goes to won't-have, not into the scene.

## 2026-11-14 — Greybox scene + code-owned controller (foundation)

- Done: Universal 3D (URP 17.0.4) project, `SandboxBuilder` greybox
  (ground, 6 blocks, 4 walls, light, camera, capsule player),
  hand-rolled `ThirdPersonController`; Linux player build (84 s, 91 MB)
  runs with zero errors (audio-only noise headless).
- Faced: Asset Store has no headless fetch — Starter Assets dropped in
  favor of the code-owned rig; template materials predate URP 17.
- Learned: headless batchmode builds the full loop without GUI.
- Next: trigger toy + toy/goal loop, questions written down.

## 2026-11-14 — MCP relay + keep-alive editor (agentic loop)

- Done: relay `unity-ai-relay v1.0.12` handshake (7 tools), `CommandScript`
  proof (`McpProvenCube` persists), fanned out to all harnesses;
  user service `unity-sandbox-editor.service` keeps the editor alive.
- Faced: stale sockets after editor PID died — cleaned, restarted.
- Learned: `Unity_RunCommand` needs `internal class CommandScript :
  IRunCommand` exactly; no GUI approval headless.
- Next: credit forecast, gateway check (other goal, not this one).

## 2026-11-14 — Q2/Q3 greybox + goal closeout (this goal)

- Done: `ToyTrigger` (counter/pickup/goal, `Needed = 3`) + scene wiring
  (`TriggerZone`, `Orb_0..2`, `GoalPad`) via `SandboxBuilder`;
  `VerifyGreybox` inventory proof; docs: GDD dates, questions,
  decision log, runbook, playtest protocol, run log.
- Faced: keep-alive editor holds the project lock — service stopped for
  the rebuild, restarted afterwards.
- Learned: one headless `BuildAll` covers scene + player proof for all
  three questions at once.
- Next: SFX placeholder (M3, 2026-12-05), share with friends (M4).

## 2026-11-14 — Q4 multiplayer-testable co-op (goal unity-multiplayer-demo)

- Done: NGO 2.5.0 + transport/lobby/relay pinned; `MultiplayerAutoStarter` (path-based clone detect, no ParrelSync dep); `ThirdPersonController` owner gate (solo unaffected); `SandboxBuilder.BuildNetworkLayer` (NetworkManager + transport assignment + NetworkPlayer prefab + prefabs list); `VerifyMultiplayer`; `MultiplayerConfigTests` (3 tests); `BuildAll` green (15.9s, 95MB); EditMode 6/6; host + `--net-client` run logs clean.
- Faced: keep-alive editor locked the project (stopped service for batch work); services lobby/relay 2.0.0 don't exist (used 1.3.0/1.2.0); `NetworkConfig.NetworkTransport` must be assigned explicitly ("No transport has been selected" + NRE in StartHost until fixed); Sandbox.Runtime needed `Unity.Networking.Transport` asmref.
- Learned: base manifest already fails Unity resolve when the wrong project is open — always pass `-projectPath`; `NetworkTransform` (not ClientNetworkTransform) ships in NGO 2.5 Components.
- Next: ParrelSync 2-editor live join, lag pass, silent playtest triage; commit + push + restart editor service.

## 2026-11-15 — Q5 Diablo click-to-move kill-loot (goal unity-diablo-prototype)

- Done: Q5 mechanics question; `DiabloClickController` (iso click + attack-move) + `DiabloMelee` (range/cooldown) + `EnemyHealth` (HP/flash/death orb-drop); `ToyTrigger` activator widened to tag fallback; `Husk` tag; `SandboxBuilder.BuildDiabloScene/VerifyDiablo/BuildDiablo` (Knight hero, 3 Skeleton husks, tile floor 25, wall ring 24, torches, chest GoalPad); `KayKitAssets` extended to 5 packs (Adventurers 6, Skeletons 4, Rig_Medium 8 anim rigs); EditMode 9/9 + PlayMode 4/4; diablo player build Succeeded (36.6s, 98MB), 45s run clean (0 exceptions).
- Faced: `-quit` kills `-runTests` before execution (omit `-quit`; runner exits itself); controller cached sibling `DiabloMelee` in `Awake` before it existed (lazy resolve fix, test-found); KayKit figure packs use `Characters/` not `Models/` + need URP check without atlas remap; type is `EditorBuildSettingsScene`.
- Learned: PlayMode `InputTestFixture` virtual devices work headless; `VerifyDiablo` inventory pattern scales; owner GUI import mapped URP materials correctly (Verify green first try).
- Next: visual-fit check in editor, hero accessories, Rig_Medium animation wiring, one human feel session (follow-ups, not criteria).

## 2026-11-15 — Q6 Diablo P2 co-op loot-zone (plan unity-diablo-p2-multiplayer-loot-zone)

- Done: P1 host-authoritative netcode (`DiabloNetworkPlayer` owner-gate + validated `SubmitAttackServerRpc`, `DiabloNetworkBootstrap` Resources-prefab wiring, `MultiplayerAutoStarter` scene log); P2 item data (`ItemDefinition/AffixLibrary/ItemDatabase/Equipment`, 3x3 combos + 3 fixed uniques); P3 loot (`LootTable/DroptableDatabase/LootDrop/LootNetRelay`, `EnemyHealth` tier + `RequestDamage` + item-drop hook, Q5 orb compat kept); P4 `BuildDiabloZone/VerifyDiabloZone/BuildDiabloZoneBuild` + `NetworkPlayerDiablo` prefab + scene (6 husks, elite 15 HP, chest Rare+, 4 coop spawns); P5 `DiabloP2ConfigTests` + `DiabloP2LoopTests` + Q6 docs; EditMode 14/14 + PlayMode 10/10; zone build Succeeded (84.7s, 99MB), host-start run 0 exceptions.
- Faced: `NetworkPrefabs` not enumerable (dedup via PlayerPrefab-compare); `StandaloneBuildSubtotal` typo (fixed to `Subtarget`); `Destroy` applies at frame end — kill asserts need one yielded frame (Q5 waited 6s; mine checked immediately).
- Learned: `NetworkManager.Singleton` safe-guards (`null`/`IsListening`/`IsServer`) keep solo path exact; `.meta` files generate on import and must be committed; `DefaultNetworkPrefabs.asset` dirties on Prefabs.Add (same as Q4 commit).
- Next: land via `agent-workspace.py land` (ff); follow-ups (not criteria): Relay cross-machine join, server cooldown on relay swings, client-pickup convergence pass, 5-person playtest.
