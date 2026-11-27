# Multiplayer demo plan — unity-multiplayer-demo (NGO + Relay/Lobby, co-op greybox)

Goal: ".agents/goals/unity-multiplayer-demo/GOAL.md" (0%). Location: `~/unity-sandbox`, branch `feature/unity-multiplayer-demo`.
Assumptions (repo has no `repo.yaml`): `main_branch = main`, `tickets = none`, `task_runner = none`, `generated_artifacts = false`.

## Tasks

1. Pin + packages
   - Confirm `sandbox/ProjectSettings/ProjectVersion.txt` = 6000.0.85f2, URP 17.0.4, Input System 1.20.0.
   - Add to `sandbox/Packages/manifest.json`: `com.unity.netcode.gameobjects` (2.x), `com.unity.services.relay`, `com.unity.services.lobby`, `com.unity.transport`.
   - Resolve headless (`-batchmode -nographics -quit` package resolve), fix version conflicts.

2. Networked player + manager
   - `NetworkPlayer` prefab: capsule + CharacterController + NetworkObject + ClientNetworkTransform + owner-only `ThirdPersonController` gate.
   - `NetworkManager` in Greybox scene: NetworkManager + UnityTransport (localhost 7777).
   - `sandbox/Assets/Scripts/MultiplayerAutoStarter.cs`: host in main editor, auto-connect as client when running under ParrelSync clone (`Application.dataPath` contains `_clone`) or `--net-client` arg; no hard ParrelSync dependency.

3. Scene + builder update
   - Extend `SandboxBuilder.BuildScene` to spawn NetworkManager + auto-starter (keep Q1-Q3 toys untouched).
   - Keep single-player Player for solo verify; networked player spawns via NGO player prefab.

4. Verify headless
   - EditMode: packages present, scene has NetworkManager, player prefab has NetworkObject.
   - PlayMode: host start/stop smoke (or config check if transport unavailable headless).
   - `SandboxBuilder.BuildAll` Linux build succeeds, 40s run log clean.
   - One lag-simulation note (Multiplayer Tools / artificial latency) in docs.

5. Docs + push
   - `docs/mechanics-questions.md` Q4 multiplayer, `docs/playtest-protocol.md` 2-player update, `docs/dev-journal.md` entry, `docs/decision-log.md` NGO choice.
   - Commit on `feature/unity-multiplayer-demo`, push, fast-forward `main`, push `main`.
   - Update dotfiles `PROGRESS.md` with evidence (no code in dotfiles).
