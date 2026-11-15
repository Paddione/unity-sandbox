# Diablo P2 run log — Q6 "Ember Crypt Approach" (2026-11-15)

Branch: `feature/unity-diablo-p2-multiplayer-loot-zone` (from `main` @ `ae40013`).

## Evidence

- `SandboxBuilder.BuildDiabloZone` headless: prefab `NetworkPlayerDiablo`
  saved to `Assets/Resources`, netcode ensured, scene saved
  (`/tmp/p2-build-zone3.log`: "Saved Assets/Scenes/DiabloZone.unity").
- `SandboxBuilder.VerifyDiabloZone`: OK — player + clicker + melee +
  equipment + goal, 6 husks + elite + chest, 4 spawns, 25+ tiles, walls,
  4 torches, netcode complete (`/tmp/p2-verify-zone.log`).
- `KayKitAssets.Verify`: 5/5 packs green — Dungeon 211, Adventurers 6,
  Skeletons 4, CharacterAnimations 8 (`/tmp/p2-assets-verify.log`).
- Suites: EditMode **14/14**, PlayMode **10/10** (`scripts/run-unity-tests.sh`;
  artifacts `EditMode.xml` / `PlayMode.xml` in the worktree).
- Player build `BuildDiabloZoneBuild`: Succeeded in 84.7 s, 99 MB
  (`builds/diablo-zone/diablo-zone`, git-ignored).
- Headless run: scene `DiabloZone` loaded, `[Net] Starting host on
  127.0.0.1:7777`, **0 exceptions / 0 errors** (`/tmp/p2-zone-run.log`).

## Deviations from the staged plan (documented, no contract break)

- `DiabloMelee.cs` untouched: the ServerRpc lives in the new
  `DiabloNetworkPlayer` (NetworkBehaviour) instead of the pure melee class.
- Extra P3 file `LootNetRelay.cs` (server relay for client pickups);
  still P3-owned, partials stay disjoint.
- Goal arms at 3 pickups (`ToyTrigger.Needed` const unchanged, Q3 semantics);
  plan text said 6 — first 3 pickups arm the crypt gate.
- Client pickup without server is prediction-only; server despawn wins.

## gpu-metal deploy (2026-11-15, commit `2cb9a69`)

- Server build `BuildDiabloZoneServer`: Succeeded in 36.5 s, 86 MB
  (`builds/diablo-server/diablo-server`); local smoke: DiabloZone scene,
  `[Net] Server started, listening for clients`.
- Deployed per memorized convention (new persistent install, Greybox
  untouched): `rsync -az --delete builds/diablo-server/ gpu-metal:~/diablo-server/`
  + `BUILD_INFO.json` (commit `2cb9a69`, scene `diablo-zone`).
- Unit `diablo-server.service` (enabled, Restart=always): port **7778/udp**
  (Greybox keeps 7777), UFW `ALLOW 100.64.0.0/10`, log `~/diablo-server/server.log`.
- E2E proof: local zone client `--net-client --net-address 100.74.227.118
  --net-port 7778` → server log `[Net] Client connected: 1 (total=1)`,
  then clean disconnect. Join command below.

## Lagfix (2026-11-15, commit `8fb61ac`)

- Symptom: client movement rubber-banded on the gpu-metal session despite a
  1 ms direct tailnet path (DERP ruled out via `tailscale ping`).
- Root cause: player prefab `NetworkTransform` in default `Server` authority —
  the dedicated server replicated its stale copy back to the owner 30x/s.
  Fixed to `Owner` (zone + greybox prefabs; greybox takes effect on next rebuild).
- Same pass: husks/chest carry `NetworkObject` now (server-authoritative
  kills, no more per-client divergence); loot `Spawn()` server-gated plus
  chest relay for dedicated-server sessions.
- Proof: EditMode 14/14 + PlayMode 10/10, `VerifyDiabloZone` OK, server +
  player rebuilt, redeployed to `~/diablo-server`, join vs 7778 → connect +
  clean disconnect, no errors.
