# Unity MCP Gateway

This Node.js server acts as a bridge between standard MCP clients (like Cursor, Antigravity, VS Code) and the Unity Editor.

## How it Works

1. **MCP Client** (IDE) spawns this script via `stdio`.
2. **This Gateway** translates MCP tool calls into HTTP JSON-RPC requests sent to Unity (`http://localhost:17890`).
3. **This Gateway** exposes MCP resources that read Unity state.
4. **This Gateway** connects to Unity's WebSocket (`ws://localhost:17891`) to listen for events (future support).

## Setup

1. Ensure `node` (v18+) is installed.
2. Run `npm install` in this directory.

## Running

The gateway is typically spawned automatically by your IDE. To run manually:

```bash
npm start
# or
node index.js
```

## Configuration

Add this to your IDE's MCP configuration file:

### Cursor (`.cursor/mcp.json`)
```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["./gateway/index.js"],
      "cwd": "${workspaceFolder}"
    }
  }
}
```

### Antigravity / Generic (`workspace.json`)
```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["./gateway/index.js"],
      "cwd": "${workspaceFolder}"
    }
  }
}
```

## Available Tools (10)

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects in the scene |
| `unity_create_object` | Create a new empty GameObject |
| `unity_create_primitive` | Create a primitive (Cube, Sphere, etc.) |
| `unity_set_transform` | Set position, rotation, and scale |
| `unity_set_material` | Apply a material to an object |
| `unity_create_material` | Create a new material asset |
| `unity_set_material_property` | Set material properties (color, float) |
| `unity_instantiate_prefab` | Instantiate a prefab from assets |
| `unity_create_prefab` | Save a GameObject as a prefab |
| `unity_get_selection` | Get currently selected objects |

## Available Resources (6)

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Unity version, platform, project path, play state |
| `unity://scene/hierarchy` | Complete hierarchy of all GameObjects |
| `unity://scene/list` | List of loaded scenes |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets (materials, prefabs, scripts, etc.) |
| `unity://console/logs` | Recent console log entries |

## Requirements

- Node.js 18+
- Unity Editor running with the MCP package installed
- Unity MCP server listening on port 17890
