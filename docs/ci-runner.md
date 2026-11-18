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

## Autoplay Nightly (cinderreach-loop-guard)

`.github/workflows/autoplay-nightly.yml` runs nightly at 02:30 UTC (03:30 CET) and on
`workflow_dispatch` on the same `unity-licensed` runner. It calls
`bash scripts/run-autoplay.sh artifacts/autoplay/<date>-<run_id>`: Linux player build into
`builds/autoplay-player/` of the runner checkout, full session with
`-autoplay -autoplay-god`, then the FPS gate (`--min-fps 30 --resolution 1280x720 --skip-first 1`).

- Display: the runner shares `/tmp/.X11-unix/X0` and `/dev/dxg` with the WSL session, so
  `run-autoplay.sh` renders through WSLg with `GALLIUM_DRIVER=d3d12` on the GPU. A window
  shows on the desktop for about two minutes. Without WSLg it falls back to Xvfb (llvmpipe,
  ~6 fps), which proves the session but fails the FPS gate.
- Results: artifact `autoplay-<run_id>` (14 days) and a host copy under
  `~/unity-sandbox-autoplay/<YYYY-MM-DD>/` (a second run on the same day gets a
  `-<run_id>` suffix). Host folders older than 30 days are deleted by the job.
  The dotfiles probe `G-DIABLO06` reads only that host folder.
- Serialisation with `Unity Tests`: both jobs use the same runner and the same runner
  Library. A single runner executes jobs one after another, which is the protection; the
  `unity-autoplay` concurrency group only prevents two autoplay runs queueing in parallel.
