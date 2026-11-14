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

- The relay needs a running Editor. Proof run: headless
  `Editor/Unity -batchmode -nographics -openProject sandbox` → handshake OK,
  `tools/list` returned 7 tools, `Unity_GetConsoleLogs` call succeeded.

## Tools seen

`Unity_AssetGeneration_GenerateAsset`, `Unity_AssetGeneration_GetModels`,
`Unity_Camera_Capture`, `Unity_GetConsoleLogs`, `Unity_RunCommand`,
`Unity_SceneView_Capture2DScene`,
`Unity_SceneView_CaptureMultiAngleSceneView`

## Client entries

Claude Code (`~/.claude.json` / project `.mcp.json`):

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

Codex (`~/.codex/config.toml`):

```toml
[mcp_servers.unity-sandbox]
command = "/home/patrick/.unity/relay/relay_linux"
args = ["--mcp", "--project-path", "/home/patrick/unity-sandbox/sandbox"]
startup_timeout_sec = 60.0
tool_timeout_sec = 180.0
```

## Approval

Direct external clients may ask for one manual approval in
Edit → Project Settings → AI → Unity MCP Server (per project). Read-only
calls already worked without it; if a mutating call ever reports
"not approved", approve once in the Hub/Editor GUI (WSLg) and retry.
AI Gateway connections are auto-approved.
