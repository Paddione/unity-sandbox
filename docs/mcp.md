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
