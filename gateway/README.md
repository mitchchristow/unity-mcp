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

## Available Tools (79)

The gateway exposes 79 tools organized by category. Many tools use an `action` parameter for consolidated operations.

### Core Tools

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects in the scene |
| `unity_create_object` | Create a new empty GameObject |
| `unity_create_primitive` | Create a primitive (Cube, Sphere, etc.) |
| `unity_delete_object` | Delete a GameObject |
| `unity_set_transform` | Set position, rotation, and scale |
| `unity_get_selection` | Get currently selected objects |

### Consolidated Tools (action-based)

These tools combine related operations into a single tool with an `action` parameter:

| Tool | Actions | Description |
|------|---------|-------------|
| `unity_playmode` | play, stop, pause | Control play mode |
| `unity_undo_action` | undo, redo, get_history, clear, begin_group, end_group | Undo/redo |
| `unity_selection` | set, clear, select_by_name, focus | Selection management |
| `unity_capture` | game, scene | Screenshot capture |
| `unity_find_objects` | name, tag, component, layer | Find objects |
| `unity_component` | add, remove, list, get_properties | Component management |
| `unity_file` | read, write, exists, list_dir, create_dir | File operations |
| `unity_animator` | get_info, get_parameters, set_parameter, play_state | Animation |
| `unity_window` | open, close, focus, get_info | Editor windows |
| `unity_open_panel` | inspector, project_settings, preferences | Editor panels |

### Additional Categories

- **Materials**: `unity_set_material`, `unity_create_material`, `unity_set_material_property`
- **Prefabs**: `unity_instantiate_prefab`, `unity_create_prefab`
- **Lighting**: `unity_create_light`, `unity_set_light_property`, `unity_get_lighting_settings`, `unity_set_ambient_light`
- **Cameras**: `unity_create_camera`, `unity_set_camera_property`, `unity_get_camera_info`, scene view controls
- **Physics**: `unity_set_gravity`, `unity_set_physics_property`, `unity_raycast`, layer collision tools
- **UI**: Canvas and UI element creation, text/image/rect transform tools
- **Terrain**: Create, sculpt, and configure terrains
- **Particles**: Create and control particle systems
- **Navigation**: NavMesh baking, agents, and pathfinding
- **Audio**: Audio source creation and playback control
- **Build**: Build target and scene configuration
- **Packages**: Package management (add, remove, search)

## Available Resources (29)

Resources provide **read-only** data for AI context. The AI reads these automatically instead of calling tools.

### Core Resources

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Unity version, platform, project path, play state |
| `unity://scene/hierarchy` | Complete hierarchy of all GameObjects |
| `unity://scene/list` | List of loaded scenes |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets (materials, prefabs, scripts, etc.) |
| `unity://console/logs` | Recent console log entries |

### Scene Analysis

| Resource URI | Description |
|--------------|-------------|
| `unity://scene/stats` | Scene statistics |
| `unity://scene/render-stats` | Rendering statistics |
| `unity://scene/memory-stats` | Memory usage |
| `unity://scene/object-counts` | Object counts by type |
| `unity://scene/asset-stats` | Asset statistics |
| `unity://scene/analysis` | Optimization analysis |

### Objects & Components

| Resource URI | Description |
|--------------|-------------|
| `unity://lights` | All lights in scene |
| `unity://cameras` | All cameras in scene |
| `unity://terrains` | All terrains |
| `unity://particles` | All particle systems |
| `unity://ui/elements` | All UI elements |
| `unity://animations` | Animation clips |

### Settings & Configuration

| Resource URI | Description |
|--------------|-------------|
| `unity://physics` | Physics settings |
| `unity://tags` | Available tags |
| `unity://layers` | Available layers |
| `unity://audio/settings` | Audio settings |
| `unity://audio/clips` | Audio clips |
| `unity://navmesh/settings` | NavMesh settings |
| `unity://navmesh/agents` | NavMesh agents |
| `unity://packages` | Installed packages |
| `unity://build/settings` | Build settings |
| `unity://build/targets` | Build targets |
| `unity://editor/windows` | Open editor windows |

## Requirements

- Node.js 18+
- Unity Editor running with the MCP package installed
- Unity MCP server listening on port 17890
