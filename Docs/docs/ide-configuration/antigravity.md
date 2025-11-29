---
sidebar_position: 1
---

# Antigravity Integration

Antigravity can natively communicate with the Unity MCP server to perform complex agentic tasks.

## Configuration

The project includes configuration files that Antigravity automatically detects:

- `workspace.json` in the project root
- `ide-integrations/antigravity/workspace.json`

## Setup

1. **Install Gateway Dependencies** (first time only):
   ```bash
   cd gateway
   npm install
   ```

2. **Ensure Unity is Running**: Open your Unity project and verify the MCP server is running (check Console for `[MCP] HTTP Server started`).

3. **Open in Antigravity**: Open the project folder in Antigravity.

4. **Verify Connection**: The agent will automatically have access to the Unity toolset with **79 tools** and **29 resources**.

## Example Prompts

You can ask Antigravity to perform high-level tasks:

> "Create a new scene called 'Level1'."

> "Add a Plane at (0,0,0) and a Cube at (0,1,0). Make the Cube red."

> "Write a script 'Rotator.cs' that rotates the object 90 degrees per second and attach it to the Cube."

> "Enter play mode and tell me if there are any console errors."

> "Show me all the prefabs in the project."

> "What's the current scene hierarchy?"

## Available Tools (79)

The gateway exposes 79 tools. Many use an `action` parameter for consolidated operations.

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

| Tool | Actions | Description |
|------|---------|-------------|
| `unity_playmode` | play, stop, pause | Control play mode |
| `unity_undo_action` | undo, redo, get_history, clear | Undo/redo operations |
| `unity_selection` | set, clear, select_by_name, focus | Selection management |
| `unity_capture` | game, scene | Screenshot capture |
| `unity_find_objects` | name, tag, component, layer | Find objects by criteria |

### Additional Categories

- **Materials & Prefabs**: Create materials, apply to objects, instantiate prefabs
- **Lighting**: Create lights, configure ambient lighting
- **Cameras**: Create and configure cameras
- **Physics**: Configure gravity, raycasting, collisions
- **UI**: Create canvases and UI elements
- **Terrain**: Create and sculpt terrains
- **Particles**: Create and control particle systems
- **Navigation**: NavMesh baking, agents, pathfinding
- **Audio**: Audio sources and playback
- **Build**: Build configuration and player builds
- **Packages**: Package management

## Available Resources (29)

Resources provide **read-only** context about the Unity project. The AI reads these automatically.

### Core Resources

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Unity version, platform, project path, play state |
| `unity://scene/hierarchy` | Complete hierarchy of all GameObjects |
| `unity://scene/list` | List of loaded scenes with paths |
| `unity://selection` | Currently selected objects with positions |
| `unity://assets` | Project assets (materials, prefabs, scripts) |
| `unity://console/logs` | Recent console log entries |

### Additional Resources

| Category | Resources |
|----------|-----------|
| Scene Analysis | `unity://scene/stats`, `unity://scene/render-stats`, `unity://scene/memory-stats`, `unity://scene/analysis` |
| Objects | `unity://lights`, `unity://cameras`, `unity://terrains`, `unity://particles`, `unity://ui/elements` |
| Systems | `unity://physics`, `unity://tags`, `unity://layers`, `unity://audio/settings`, `unity://navmesh/settings` |
| Project | `unity://packages`, `unity://build/settings`, `unity://build/targets`, `unity://editor/windows` |

## Manual Configuration

If automatic detection doesn't work, ensure `workspace.json` exists in your project root:

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
