# Q4 run log — multiplayer-testable co-op (2026-11-14)

## Packages
- `com.unity.netcode.gameobjects@2.5.0`, `com.unity.transport@2.4.0`,
  `com.unity.services.lobby@1.3.0`, `com.unity.services.relay@1.2.0`
  in `sandbox/Packages/manifest.json`; `packages-lock.json` contains all four.
- Note: initial lobby/relay `2.0.0` do not exist on the Unity registry; corrected to 1.3.0/1.2.0.

## Build
- `SandboxBuilder.BuildAll`: **Succeeded in 15.9s, 95 MB** (`builds/greybox/greybox`).
- Scene: `Greybox.unity` rebuilt with network layer (NetworkManager + UnityTransport 127.0.0.1:7777 + NetworkPlayer prefab), Q1-Q3 toys untouched.

## Verifies
- `VerifyGreybox`: `player=True controller=True blocks=6 walls=4 triggerZones=1 orbs=3 goalPads=1` — OK.
- `VerifyMultiplayer`: NetworkManager + transport + auto-starter + NetworkPlayer prefab (+ NetworkTransport assignment) — OK.

## Tests
- EditMode: **6 tests, 0 failures** (3 URP + 3 `MultiplayerConfigTests`).

## Run
- Host: `./builds/greybox/greybox -batchmode -nographics` logs `[Net] Starting host on 127.0.0.1:7777.` with no exception (only ALSA/FMOD no-soundcard noise, no game errors).
- Client: same binary with `--net-client` logs `[Net] Starting client -> 127.0.0.1:7777 (clone=False).`
- Fixed during session: `NetworkConfig.NetworkTransport` must be assigned (was "No transport has been selected!" + NRE in `StartHost`).

## Lag / ParrelSync notes (follow-up, not headless)
- Lag pass: use Multiplayer Tools latency/jitter/loss presets because localhost looks perfect; not run headless.
- ParrelSync: clone detection is path-based (`_clone` in `Application.dataPath`), no ParrelSync package dependency; live 2-editor join is a manual follow-up (`IsClone()` auto host/join).
