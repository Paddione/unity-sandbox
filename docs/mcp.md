# MCP access to the sandbox editor

Official Unity MCP server inside `com.unity.ai.assistant` (verified 2026-11-14
against Editor 6000.0.85f2, relay `unity-ai-relay v1.0.12`, server
`unity-mcp-server 1.0.0`).

## Relay

- Binary: `~/.unity/relay/relay_linux` (installed automatically when the
  Editor opens a project with the Assistant package).
- Always pass `--mcp`, plus `--project-path` to target this project:

```
~/.unity/relay/relay_linux --mcp --project-path /home/patrick/unity-sandbox/sandbox
```

- The relay needs a running Editor. A headless batchmode Editor is enough —
  the Assistant package auto-spawns its relay inside the Editor:

```
/home/patrick/Unity/Hub/Editor/6000.0.85f2/Editor/Unity -batchmode -nographics \
  -projectPath /home/patrick/unity-sandbox/sandbox -logFile /tmp/unity-sandbox-editor.log
```

- Proof runs (both against Editor 6000.0.85f2, relay `unity-ai-relay v1.0.12`,
  server `1.0.0`): handshake OK, `tools/list` returned the 7 tools below,
  `Unity_GetConsoleLogs` call succeeded — all with no GUI approval step.

## Tools seen

`Unity_AssetGeneration_GenerateAsset`, `Unity_AssetGeneration_GetModels`,
`Unity_Camera_Capture`, `Unity_GetConsoleLogs`, `Unity_RunCommand`,
`Unity_SceneView_Capture2DScene`,
`Unity_SceneView_CaptureMultiAngleSceneView`

## Client entries

Managed by the dotfiles MCP registry: `unity-sandbox` lives in
`agentic-resources/runtimes/mcp-servers.json` (all six `harnesses` lists) and is
rendered by `scripts/render-mcp-configs.py` (pin in `mcp.yaml`). Never hand-edit
the stanzas below — change the manifest, re-render, update the pin.

| Harness | Config file | Entry |
|---|---|---|
| opencode | `.config/opencode/opencode.jsonc` | `mcp.servers.unity-sandbox`, local |
| codex | `.codex/config.toml` | `[mcp_servers.unity-sandbox]`, 60s/180s timeouts |
| claude | `.claude/settings.json` | `mcpServers.unity-sandbox` |
| claude-global | `~/.claude.json` | `mcpServers.unity-sandbox` |
| gemini | `.gemini/config/mcp_config.json` | `mcpServers.unity-sandbox` |
| qwen | `.qwen/settings.json` | `mcpServers.unity-sandbox` |
| muse | `.config/muse/settings.json` | `mcpServers.unity-sandbox`, stdio |

Opencode (`.config/opencode/opencode.jsonc`, inside `mcp.servers`):

```json
"unity-sandbox": {
  "type": "local",
  "command": [
    "/home/patrick/.unity/relay/relay_linux",
    "--mcp",
    "--project-path",
    "/home/patrick/unity-sandbox/sandbox"
  ]
}
```

Codex (`~/.codex/config.toml`, inside the managed block):

```toml
[mcp_servers.unity-sandbox]
command = "/home/patrick/.unity/relay/relay_linux"
args = [
    "--mcp",
    "--project-path",
    "/home/patrick/unity-sandbox/sandbox",
]
startup_timeout_sec = 60.0
tool_timeout_sec = 180.0
```

Claude Code (`.claude/settings.json`, project `.mcp.json`, `~/.claude.json`,
`.gemini/config/mcp_config.json`, `.qwen/settings.json` — same shape):

```json
{
  "mcpServers": {
    "unity-sandbox": {
      "command": "/home/patrick/.unity/relay/relay_linux",
      "args": ["--mcp", "--project-path", "/home/patrick/unity-sandbox/sandbox"]
    }
  }
}
```

Muse (`.config/muse/settings.json` — same, plus transport):

```json
{
  "mcpServers": {
    "unity-sandbox": {
      "command": "/home/patrick/.unity/relay/relay_linux",
      "args": ["--mcp", "--project-path", "/home/patrick/unity-sandbox/sandbox"],
      "transport": "stdio"
    }
  }
}
```

Harnesses without a render target (`pi`, `openclaw`, `zcode` — skills only via
`skills.yaml`) reuse the Claude-shape stanza in their own MCP config; their
guidelines otherwise come from `~/AGENTS.md` and the linked skills.

## Approval

Verified 2026-11-14 (twice): no approval step needed. Read-only calls
(`tools/list`, `Unity_GetConsoleLogs`) and the mutating `Unity_RunCommand`
proof (Greybox cube) all succeeded headless without any GUI approval.
If a call ever reports "not approved", approve once in
Edit → Project Settings → AI → Unity MCP Server (Hub/Editor GUI via WSLg)
and retry. AI Gateway connections are auto-approved.

## Track P3 notes (2026-11-17)

- `unity mcp configure` evaluated and deliberately NOT applied: every harness
  config already carries the `unity-sandbox` relay entry (e.g. codex
  `[mcp_servers.unity-sandbox]` → `relay_linux --mcp --project-path …`),
  verified live again 2026-11-17 (7-tool groups via `unity command`, console
  read with 0 errors). Re-running configure would duplicate working entries
  and shell out to client CLIs (`claude mcp add`) for no gain.
- `--caller plugin --skill` convention dropped: CLI 1.0.1 has no `--caller`
  flag on `unity command` (verified via `unity command --help`). The live
  convention is skill-guided discovery (`unity commands --grep <term>`) plus
  direct `unity command <group> <name>` invocation.
- Official plugin skill mirror: `unity skills` already inventories the local
  `.claude/skills` set for `claude-code` (incl. `unity-cli`, `new-unity-project`).
  `unity skill install <client>` stays available per client on explicit request;
  no client was force-migrated in this track.

## Stale-socket cleanup

Symptom: `tools/list` hangs in all harnesses after the editor PID died
(observed 2026-11-14, PID 568443). The relay socket outlives its editor and
never recovers on its own.

Recovery (headless, no GUI):

```bash
systemctl --user stop unity-sandbox-editor.service
# if stop hangs (Unity shutdown spin): it is SIGKILLed after the unit timeout;
# then: systemctl --user reset-failed unity-sandbox-editor.service
systemctl --user start unity-sandbox-editor.service
unity status --until-ready --project-path ~/unity-sandbox/sandbox --format json
```

`unity status` success with `state: ready` is the validity proof (7-tool
`unity command` listing answers again). Keep-alive vs build-lock stays a
standing conflict: batch builds/tests stop the service first (trap-guarded,
see `scripts/run-unity-build.sh` and `scripts/run-unity-tests.sh`).
