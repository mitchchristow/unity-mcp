# Antigravity Unity MCP Integration

This integration allows Antigravity to control the Unity Editor.

## Important: Global Configuration Only

According to the [Antigravity MCP documentation](https://antigravity.google/docs/mcp), Antigravity **only supports global MCP configuration**. Per-project/workspace configuration is not available.

## Setup

1. **Configure the MCP server globally:**
   - Open Antigravity
   - Click the **"..."** dropdown at the top of the agent panel
   - Select **"Manage MCP Servers"**
   - Click **"View raw config"**
   - Add the configuration from `mcp_config.json` (update the path to your local installation)

2. **Ensure the Unity project is open** and the MCP server is running (check Console for `[MCP] HTTP Server started`).

3. **Use the MCP tools** to control Unity.

## Configuration Template

See `mcp_config.json` in this directory for a template. You must update the path to an absolute path on your system.

## Capabilities

- **Scene Control**: Create, move, delete objects
- **Scripting**: Write and reload C# scripts
- **Playmode**: Start/Stop playmode
- **Events**: Listen to console logs and scene changes
- **2D Development**: Sprites, tilemaps, 2D physics
- **Prompts**: Pre-built workflow templates
