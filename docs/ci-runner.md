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
