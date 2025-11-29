---
sidebar_position: 3
---

# VS Code Integration

:::caution Under Construction
VS Code integration is **experimental**. VS Code does not natively support MCP, and the provided task configuration and extension may require additional setup. Full functionality is not guaranteed.
:::

VS Code does not have native MCP support, but this project provides two options for integration.

## Option 1: Auto-Start Task (Recommended)

The project includes a `.vscode/tasks.json` that automatically starts the MCP gateway when you open the folder.

### Setup

1. **Install Gateway Dependencies** (first time only):
   ```bash
   cd gateway
   npm install
   ```

2. **Open in VS Code**: Open the project folder in VS Code.

3. **Allow Task**: When prompted, allow the "Start Unity MCP Gateway" task to run.

4. **Verify**: The gateway will start in a terminal panel.

### Manual Start

If the task doesn't auto-start, run it manually:
1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
2. Type "Tasks: Run Task"
3. Select "Start Unity MCP Gateway"

## Option 2: VS Code Extension

The project includes a custom VS Code extension for deeper integration.

### Installation

1. Navigate to the extension directory:
   ```bash
   cd ide-integrations/vscode/extension
   ```

2. Install dependencies and compile:
   ```bash
   npm install
   npm run compile
   ```

3. **Debug Mode**: Open the `ide-integrations/vscode/extension` folder in VS Code and press `F5` to launch the extension host.

4. **Install Permanently**: Package it into a `.vsix` file using `vsce package` and install in your main VS Code instance.

### Extension Features

- **Unity Status Bar**: Shows connection status to the Unity Editor.
- **Commands**:
  - `Unity MCP: Connect`: Retry connection.
  - `Unity MCP: Play`: Enter play mode.
  - `Unity MCP: Stop`: Exit play mode.
  - `Unity MCP: Refresh Hierarchy`: Fetch the current scene hierarchy.

## Using with AI Extensions

If you use an AI extension in VS Code that supports MCP (like Continue or similar), you can configure it to use the Unity gateway:

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

Refer to your AI extension's documentation for the correct configuration location.

## Available Functionality

When connected, you have access to:

### Tools (80)

The gateway exposes 80 tools. Many use an `action` parameter for consolidated operations.

#### Core Tools

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects |
| `unity_create_object` | Create empty GameObject |
| `unity_create_primitive` | Create primitive shapes |
| `unity_delete_object` | Delete a GameObject |
| `unity_set_transform` | Modify position/rotation/scale |
| `unity_get_selection` | Get selected objects |

#### Consolidated Tools (action-based)

| Tool | Actions | Description |
|------|---------|-------------|
| `unity_playmode` | play, stop, pause | Control play mode |
| `unity_undo_action` | undo, redo, get_history, clear | Undo/redo operations |
| `unity_selection` | set, clear, select_by_name, focus | Selection management |
| `unity_capture` | game, scene | Screenshot capture |
| `unity_find_objects` | name, tag, component, layer | Find objects |

#### Additional Categories

- **Materials & Prefabs**: `unity_set_material`, `unity_create_material`, `unity_instantiate_prefab`, `unity_create_prefab`
- **Lighting**: `unity_create_light`, `unity_set_light_property`, `unity_set_ambient_light`
- **Cameras**: `unity_create_camera`, `unity_set_camera_property`
- **Physics**: `unity_set_gravity`, `unity_raycast`
- **UI**: `unity_create_canvas`, `unity_create_ui_element`
- **Terrain**: `unity_create_terrain`, `unity_terrain_height`
- **Particles**: `unity_create_particle_system`, `unity_particle_playback`
- **Navigation**: `unity_navmesh_build`, `unity_add_navmesh_agent`
- **Audio**: `unity_create_audio_source`, `unity_audio_playback`
- **Build**: `unity_set_build_target`, `unity_build_player`
- **Packages**: `unity_add_package`, `unity_remove_package`

### Resources (42)

Resources provide **read-only** data. The AI reads these automatically for context.

#### Core Resources

| Resource | Description |
|----------|-------------|
| `unity://project/info` | Project information |
| `unity://scene/hierarchy` | Scene object tree |
| `unity://scene/list` | Loaded scenes |
| `unity://selection` | Current selection |
| `unity://assets` | Project assets |
| `unity://console/logs` | Console logs |

#### Additional Resources

| Category | Resources |
|----------|-----------|
| Scene Analysis | `unity://scene/stats`, `unity://scene/render-stats`, `unity://scene/memory-stats` |
| Objects | `unity://lights`, `unity://cameras`, `unity://particles`, `unity://terrains` |
| Systems | `unity://physics`, `unity://tags`, `unity://layers`, `unity://navmesh/settings` |
| Project | `unity://packages`, `unity://build/settings`, `unity://editor/windows` |
