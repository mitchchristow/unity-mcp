# Copilot / AI Agent Instructions for Unity MCP

Purpose: quickly orient an AI coding agent to the repo, architecture, and developer workflows so it can be productive immediately.

- **Quick start (dev)**: install gateway deps and run the gateway
  - `cd gateway && npm install`
  - `npm start` (runs `node index.js`) — gateway connects to Unity at default URLs below.

- **Where to look first**:
  - Gateway (tool/resource/prompt definitions): [gateway/index.js](gateway/index.js)
  - Unity Editor package (RPC handlers & server): [Packages/org.christowm.unity.mcp](Packages/org.christowm.unity.mcp)
  - Repo README with integration notes: [README.md](README.md)

- **Big picture architecture**:
  - The Node gateway ([gateway/index.js](gateway/index.js)) implements the MCP-facing surface: Tools (callable actions), Resources (uri-based readables) and Prompts (workflows).
  - The Unity Editor package implements the RPC server that actually performs editor operations. Gateway calls Unity via HTTP RPC at `http://localhost:17890/mcp/rpc` and listens to events on `ws://localhost:17891/mcp/events`.
  - Communication pattern: AI → MCP SDK (stdio/transport) → gateway → Unity JSON-RPC over HTTP + WebSocket events → Unity Editor.

- **Conventions and patterns (project-specific)**:
  - Tool names are prefixed with `unity_` (e.g., `unity_create_object`). By default gateway maps `unity_foo` → `unity.foo` RPC method. See the `CallToolRequestSchema` handler in [gateway/index.js](gateway/index.js).
  - Several "consolidated" tools exist (single tool that maps actions to RPCs), e.g. `unity_playmode`, `unity_selection`, `unity_file`, `unity_capture`. Inspect their mapping logic in [gateway/index.js](gateway/index.js).
  - Resources use `unity://` URIs (e.g., `unity://scene/hierarchy`, `unity://events/recent`) and map to Unity RPC read methods inside the `ReadResourceRequestSchema` handler.
  - Prompts: reusable workflow templates are defined in `PROMPTS` and expanded by `generatePromptContent()` inside [gateway/index.js](gateway/index.js).

- **How to add or extend behavior (concrete steps)**:
  - Add RPC handler in the Unity package controllers: update/implement a method under the MCP RPC controllers in [Packages/org.christowm.unity.mcp](Packages/org.christowm.unity.mcp) and register it from the server startup (see `McpServer.cs` in the package).
  - Add a tool in the gateway: add an entry to the `TOOLS` array in [gateway/index.js](gateway/index.js) and map any custom action names to RPC method names inside the CallTool handler.
  - Add a resource: add to the `RESOURCES` array and map the `unity://` URI to an RPC in the `ReadResourceRequestSchema` handler.
  - Add a prompt: add an entry to the `PROMPTS` array and implement generation logic in `generatePromptContent()`.

- **Useful code examples (from repo)**:
  - Default RPC URLs in gateway: `UNITY_RPC_URL = "http://localhost:17890/mcp/rpc"` and `UNITY_WS_URL = "ws://localhost:17891/mcp/events"` — ensure Unity Editor MCP server binds there.
  - Generic mapping fallback: tool `unity_foo` → RPC `unity.foo` (implemented at end of the CallTool handler).
  - Event buffer: runtime WebSocket events are buffered (see `eventBuffer` and `unity://events/recent` resource) — use these for realtime editor state.

- **Developer workflows & debugging tips**:
  - Start Unity Editor first (so the Unity RPC server is available) then run `npm start` in `gateway`.
  - If the gateway returns "Unity Editor is not running or MCP server is not started", check Unity console for `[MCP] HTTP Server started` and confirm the package is installed.
  - To debug mappings, edit [gateway/index.js](gateway/index.js) and add `console.error(...)` lines — gateway logs to stdout/stderr.
  - There are no automated tests in `gateway` (see `package.json`); `npm test` is a placeholder.

- **Do's / Don'ts for AI agents**:
  - Do: Prefer small, focused PRs that add a controller in the Unity package and a corresponding tool/resource entry in the gateway.
  - Do: Reuse existing consolidated tools where possible (e.g., `unity_selection`) instead of adding many single-use tools.
  - Don't: Assume tools map 1:1 to RPC names — check the CallTool handler for custom mappings.

If any of these areas are unclear or you want the onboarding to include example PR patches (controller + gateway change), tell me which area to expand. 
