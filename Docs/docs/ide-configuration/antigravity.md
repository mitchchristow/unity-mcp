---
sidebar_position: 1
---

# Antigravity Integration

:::caution Global Configuration Only
According to the [Antigravity MCP documentation](https://antigravity.google/docs/mcp), Antigravity **only supports global MCP configuration**. Per-project/workspace configuration is not currently supported.
:::

Antigravity can communicate with the Unity MCP server to perform complex agentic tasks.

## Configuration

Antigravity requires MCP servers to be configured **globally** in the user's home directory. There is no per-project configuration option.

### Configuration File Location

| OS | Path |
|----|------|
| **Windows** | `%USERPROFILE%\.gemini\antigravity\mcp_config.json` |
| **macOS** | `~/.gemini/antigravity/mcp_config.json` |
| **Linux** | `~/.gemini/antigravity/mcp_config.json` |

## Setup

### Step 1: Install Gateway Dependencies

```bash
cd /path/to/unity-mcp/gateway
npm install
```

### Step 2: Configure MCP Server Globally

1. Open Antigravity
2. Click the **"..."** dropdown at the top of the agent panel
3. Select **"Manage MCP Servers"**
4. Click **"View raw config"**
5. Add the Unity MCP server configuration:

```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["/absolute/path/to/unity-mcp/gateway/index.js"]
    }
  }
}
```

:::warning Absolute Path Required
You must use an **absolute path** to the `gateway/index.js` file. Relative paths and variables like `${workspaceFolder}` are not supported in the global configuration.

**Examples:**
- **Windows**: `"args": ["C:/Projects/unity-mcp/gateway/index.js"]`
- **macOS/Linux**: `"args": ["/Users/yourname/Projects/unity-mcp/gateway/index.js"]`
:::

### Step 3: Ensure Unity is Running

Open your Unity project and verify the MCP server is running by checking the Console for:
```
[MCP] HTTP Server started at http://localhost:17890/
[MCP] WebSocket Event Server started at ws://localhost:17891/
```

### Step 4: Open in Antigravity

Open any project folder in Antigravity. The Unity MCP server will be available globally for all projects.

### Step 5: Verify Connection

The agent will have access to the Unity toolset with **80 tools**, **42 resources**, and **8 prompts**.

## Example Prompts

You can ask Antigravity to perform high-level tasks:

> "Create a new scene called 'Level1'."

> "Add a Plane at (0,0,0) and a Cube at (0,1,0). Make the Cube red."

> "Write a script 'Rotator.cs' that rotates the object 90 degrees per second and attach it to the Cube."

> "Enter play mode and tell me if there are any console errors."

> "Show me all the prefabs in the project."

> "What's the current scene hierarchy?"

## Available Tools (80)

The gateway exposes 80 tools. Many use an `action` parameter for consolidated operations.

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

## Available Resources (42)

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

## Troubleshooting

### MCP Server Not Recognized

If Antigravity doesn't see the Unity MCP server:

1. **Verify the config file location**: Ensure `mcp_config.json` is in the correct directory (see table above)
2. **Check the path**: Make sure the path to `gateway/index.js` is an absolute path and the file exists
3. **Restart Antigravity**: Close and reopen Antigravity after modifying the config
4. **Check for JSON errors**: Ensure the JSON is valid (no trailing commas, proper quotes)

### Connection Errors

If the server is recognized but can't connect:

1. **Ensure Unity is running**: The Unity Editor must be open with the MCP package installed
2. **Check the Console**: Look for `[MCP] HTTP Server started` messages
3. **Verify ports**: Ports 17890 (HTTP) and 17891 (WebSocket) must be available

## Limitations

- **Global only**: Per-project configuration is not supported by Antigravity
- **Absolute paths**: You cannot use relative paths or workspace variables
- **Single Unity instance**: The server connects to the first Unity instance found on the configured ports
