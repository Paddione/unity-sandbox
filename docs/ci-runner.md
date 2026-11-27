# Licensed host CI

`Unity Tests` runs on the repository's `wsl-unity` self-hosted runner, labelled
`unity-licensed`. It runs as the Linux user who activated Unity locally, so
no Unity password, license file upload, GameCI CLI or container is required.
The editor path follows `sandbox/ProjectSettings/ProjectVersion.txt` under
`~/Unity/Hub/Editor/`; `UNITY_EDITOR` can override the executable path.

`scripts/run-unity-tests.sh` runs EditMode and PlayMode separately. An editor
failure, missing XML, empty suite or failed test fails the job. Logs and XML
are uploaded as artifacts even on failure. The runner has its own checkout
and Library cache, separate from the interactive/MCP project.

Because the repository is public, only pushes, manual dispatches and PRs
whose branch belongs to this repository run on the host. Fork PRs require
separate review and testing before integration. Workflow permissions are
read-only and checkout does not retain the job token.

Host provisioning lives in the dotfiles repository:
`python3 ~/scripts/setup-unity-runner.py --install` registers the runner and
starts `github-runner-unity.service` as a user service. Run without arguments
to inspect its GitHub status. Runner credentials stay in the ignored,
owner-only `~/.local/share/github-runner-unity` directory. Registration is
idempotent and refuses to replace an existing remote runner name.

## Session autoplay (cinderreach-team-autoplay)

The `autoplay` job in `.github/workflows/unity-tests.yml` runs only on demand: dispatch
**Unity Tests** with the input `autoplay` checked (`gh workflow run unity-tests.yml -f autoplay=true`).
The nightly schedule was removed on 2026-11-25: the unattended runs opened game windows on
the desktop and failed whenever those were closed. It uses the same `unity-licensed` runner
and calls `bash scripts/run-team-autoplay.sh artifacts/autoplay/<date>-<run_id>`. There is no
solo route any more: a headless dedicated server (`-server`, UTP on 127.0.0.1) and three
Linux clients play as a team.

- Team: client 1 fenwalker leads (`-autoplay -autoplay-god -coop-autoplay lead`, the full solo
  route with every quest, all act bosses and Thewes), client 2 lanternbearer and client 3 rogue
  are mortal and follow (`-coop-autoplay follow`). Each client runs with its own HOME/XDG
  profile (`COOP_CLIENT_HOMES`) and its own save file. The server is the story authority:
  clients forward finds and zone entries, the server books kills and broadcasts the quest state.
- Sessions: `normal` starts from fresh saves. The Thewes kill unlocks Kindled for every client,
  and each client then starts its next run on Kindled (campaign restarts, hero kept,
  `RunProfile.StartCampaign`). `kindled` reuses the same three saves without a `-difficulty`
  flag; the server adopts the difficulty of the first joining hero (`[Difficulty] server adopts
  Kindled ... hp x1.8`). The Kindled session is skipped when the Normal session fails.
- Verdict: `scripts/team_autoplay_verdict.py` writes `<session>/team-verdict.json`: every client
  exit 0 with 0 exceptions, Thewes down, Kindled unlocked and saved; three different classes; the
  lead with every quest done and the act bosses proven last (`fellLast`); the expected difficulty;
  the server ends the session by itself.
- Display: the job sets `COOP_DISPLAY=xvfb-gpu` and `AUTOPLAY_GPU_ADAPTER=3060`: each client
  renders off-screen in its own Xvfb through Mesa d3d12 (`/dev/dxg`) on the RTX 3060 Ti and opens
  no window on the desktop. Three clients share the GPU, so there is no FPS gate.
- Results: artifact `autoplay-<run_id>` (14 days) and a host copy under
  `~/unity-sandbox-autoplay/<YYYY-MM-DD>[-<run_id>]/{normal,kindled}/{server,client1,client2,client3}/`.
  Host folders older than 30 days are deleted by the job. The dotfiles probe `G-DIABLO06` reads
  the newest run there (both `team-verdict.json` files).
- Serialisation with the `test` job: both jobs use the same runner and the same runner
  Library. A single runner executes jobs one after another, which is the protection; the
  `unity-autoplay` concurrency group only prevents two autoplay runs queueing in parallel.
- Solo checks stay available locally: `bash scripts/run-autoplay.sh` (one player, `-autoplay`).

## Unity Scene Captures

Run **Unity Scene Captures** on `main` via workflow dispatch. The licensed
WSLg runner opens the checked-in `DiabloZone`, frames the Ashen Fen in the
Scene View and captures the scene and the game camera at 1280 × 720 through
the Unity command pipeline. Download both PNGs from
`unity-scene-captures-<run_id>` within 14 days.

For a local run, use a dedicated worktree with its editor closed and run
`bash scripts/capture-unity-scene.sh`. The script locks that checkout for captures
and writes into a fresh `artifacts/scene-captures.*` directory. The renderer
must report a D3D12 GPU through WSLg. The Unity CLI defaults to
`~/.local/bin/unity` and can be set with `UNITY_CLI`.
