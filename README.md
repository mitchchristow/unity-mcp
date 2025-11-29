# Unity MCP Server

**Control the Unity Editor directly from your AI Assistant.**

This project implements a **Model Context Protocol (MCP)** server that runs inside the Unity Editor. It allows external AI agents (like **Cursor**, **Antigravity**, or **VS Code**) to manipulate scenes, write scripts, and control play mode in real-time.

![Unity MCP Banner](https://placehold.co/600x200?text=Unity+MCP+Server)

## Features

- **Scene Control**: Create, move, delete, and inspect GameObjects.
- **Component System**: Add components and modify properties via reflection.
- **Asset Management**: List and inspect project assets (materials, prefabs, scripts, etc.).
- **Scripting**: Write and reload C# scripts on the fly.
- **Play Mode**: Start, stop, and pause the game.
- **Lighting & Cameras**: Create and configure lights, cameras, and ambient lighting.
- **Physics**: Configure gravity, raycasting, and layer collisions.
- **Animation & Audio**: Control animators, audio sources, and playback.
- **UI System**: Create canvases and UI elements (buttons, text, images).
- **Terrain**: Create and sculpt terrains with height manipulation.
- **Particles**: Create and configure particle systems.
- **Navigation**: Bake NavMesh, add agents, and calculate paths.
- **Build Pipeline**: Configure build settings and build players.
- **Real-time Events**: Stream console logs and scene changes via WebSockets.
- **MCP Resources**: 29 resources expose project state for AI context.
- **Secure**: Supports Named Pipes (Windows) and Unix Sockets (Mac/Linux).

## Installation

### Option 1: Install via Unity Package Manager (Recommended)

You can install this package directly from GitHub into your Unity project.

1.  Open your Unity Project (Unity 6+).
2.  Go to **Window > Package Manager**.
3.  Click the **+** icon in the top left.
4.  Select **Add package from git URL...**.
5.  Enter the following URL:
    ```
    https://github.com/yourusername/unity-mcp.git?path=/Packages/org.christowm.unity.mcp
    ```
    *(Replace `yourusername` with your actual GitHub username)*

### Option 2: Local Development

1.  Clone this repository.
2.  Open Unity Hub.
3.  Click **Add** and select the cloned folder.
4.  The project will open with the MCP server running automatically.

## Connecting Your IDE

This project includes a **Node.js gateway** (`gateway/`) that translates MCP protocol calls into Unity RPC commands. Most IDEs spawn this gateway automatically.

### Cursor

The project includes a `.cursor/mcp.json` configuration file that automatically configures the MCP server.

1.  Ensure Unity is open and the server is running (check the Console for `[MCP] HTTP Server started`).
2.  Open the project folder in Cursor.
3.  The MCP server will start automatically and provide **79 tools + 29 resources**.

**Manual Configuration** (if needed):
Create `.cursor/mcp.json` in your project root:
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

### Antigravity

Antigravity detects the `workspace.json` file in the project root.

1.  Ensure Unity is open and the MCP server is running.
2.  Open the project in Antigravity.
3.  The agent will automatically have access to the Unity toolset.

### VS Code

VS Code requires a task to start the gateway (no native MCP support).

1.  Open the project in VS Code.
2.  The included `.vscode/tasks.json` will auto-start the gateway on folder open.
3.  Alternatively, use the included VS Code extension in `ide-integrations/vscode/`.

## Gateway Setup

Before first use, install the gateway dependencies:

```bash
cd gateway
npm install
```

The gateway can also be started manually:
```bash
npm start
# or
node index.js
```

## Available Tools (79)

The MCP server exposes 79 tools organized by category. Many tools use an `action` parameter to consolidate related operations.

### Core Tools

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects in the scene |
| `unity_create_object` | Create a new empty GameObject |
| `unity_create_primitive` | Create a primitive (Cube, Sphere, etc.) |
| `unity_delete_object` | Delete a GameObject |
| `unity_set_transform` | Set position, rotation, and scale |
| `unity_get_selection` | Get currently selected objects |
| `unity_selection` | Selection actions: set, clear, select_by_name, focus |
| `unity_find_objects` | Find objects by: name, tag, component, or layer |

### Materials & Prefabs

| Tool | Description |
|------|-------------|
| `unity_set_material` | Apply a material to an object |
| `unity_create_material` | Create a new material asset |
| `unity_set_material_property` | Set material properties (color, float) |
| `unity_instantiate_prefab` | Instantiate a prefab from assets |
| `unity_create_prefab` | Save a GameObject as a prefab |

### Consolidated Tools (action-based)

| Tool | Actions | Description |
|------|---------|-------------|
| `unity_playmode` | play, stop, pause | Control Unity play mode |
| `unity_undo_action` | undo, redo, get_history, clear, begin_group, end_group | Undo/redo operations |
| `unity_selection` | set, clear, select_by_name, focus | Selection management |
| `unity_capture` | game, scene | Capture screenshots |
| `unity_component` | add, remove, list, get_properties | Component management |
| `unity_file` | read, write, exists, list_dir, create_dir | File operations |
| `unity_animator` | get_info, get_parameters, set_parameter, play_state | Animation control |
| `unity_window` | open, close, focus, get_info | Editor window management |
| `unity_open_panel` | inspector, project_settings, preferences | Open editor panels |

### Additional Categories

- **Lighting**: `unity_create_light`, `unity_set_light_property`, `unity_get_lighting_settings`, `unity_set_ambient_light`
- **Cameras**: `unity_create_camera`, `unity_set_camera_property`, `unity_get_camera_info`, `unity_get_scene_view_camera`, `unity_set_scene_view_camera`
- **Physics**: `unity_set_gravity`, `unity_set_physics_property`, `unity_raycast`, `unity_get_layer_collision_matrix`, `unity_set_layer_collision`
- **UI**: `unity_create_canvas`, `unity_create_ui_element`, `unity_set_ui_text`, `unity_set_ui_image`, `unity_set_rect_transform`, `unity_get_ui_info`
- **Terrain**: `unity_create_terrain`, `unity_get_terrain_info`, `unity_set_terrain_size`, `unity_terrain_height`, `unity_set_terrain_layer`, `unity_flatten_terrain`
- **Particles**: `unity_create_particle_system`, `unity_get_particle_system_info`, `unity_set_particle_module`, `unity_particle_playback`
- **Navigation**: `unity_navmesh_build`, `unity_add_navmesh_agent`, `unity_set_navmesh_agent`, `unity_get_navmesh_agent_info`, `unity_add_navmesh_obstacle`, `unity_set_navmesh_destination`, `unity_calculate_path`
- **Audio**: `unity_create_audio_source`, `unity_set_audio_source_property`, `unity_get_audio_source_info`, `unity_audio_playback`, `unity_set_audio_clip`
- **Build**: `unity_set_build_target`, `unity_add_scene_to_build`, `unity_remove_scene_from_build`, `unity_get_scenes_in_build`, `unity_build_player`
- **Packages**: `unity_get_package_info`, `unity_add_package`, `unity_remove_package`, `unity_search_packages`

## Available Resources (29)

MCP Resources provide **read-only** context about your Unity project. The AI automatically reads these for context without needing tool calls.

### Project & Scene

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Project version, platform, and play state |
| `unity://scene/hierarchy` | Complete scene object tree |
| `unity://scene/list` | List of loaded scenes |
| `unity://scene/stats` | Scene statistics (object counts, etc.) |
| `unity://scene/render-stats` | Rendering statistics |
| `unity://scene/memory-stats` | Memory usage statistics |
| `unity://scene/object-counts` | Object counts by type |
| `unity://scene/asset-stats` | Asset statistics |
| `unity://scene/analysis` | Scene optimization analysis |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets (materials, prefabs, scripts) |
| `unity://console/logs` | Recent console log entries |

### Components & Objects

| Resource URI | Description |
|--------------|-------------|
| `unity://lights` | All lights in the scene |
| `unity://cameras` | All cameras in the scene |
| `unity://terrains` | All terrains in the scene |
| `unity://particles` | All particle systems |
| `unity://ui/elements` | All UI elements |
| `unity://animations` | Animation clips in project |

### Systems

| Resource URI | Description |
|--------------|-------------|
| `unity://physics` | Physics settings |
| `unity://tags` | Available tags |
| `unity://layers` | Available layers |
| `unity://audio/settings` | Audio settings |
| `unity://audio/clips` | Audio clips in project |
| `unity://navmesh/settings` | NavMesh settings |
| `unity://navmesh/agents` | NavMesh agents in scene |
| `unity://packages` | Installed packages |
| `unity://build/settings` | Build settings |
| `unity://build/targets` | Available build targets |
| `unity://editor/windows` | Open editor windows |

## Documentation

Full documentation is available in the `Docs/` directory.

- [**RPC API Reference**](Docs/docs/rpc/overview.md): List of all available commands.
- [**Event System**](Docs/docs/events/overview.md): How to listen to real-time events.
- [**IDE Configuration**](Docs/docs/ide-configuration/cursor.md): Setup guides for each IDE.
- [**Contributing**](Docs/docs/development/contributing.md): How to build and test the server.

## Architecture

The system consists of two main components:

### 1. Unity MCP Package (runs inside Unity Editor)
- **HTTP Server** (`:17890`): Handles JSON-RPC commands.
- **WebSocket Server** (`:17891`): Streams events.
- **Named Pipe** (`\\.\pipe\unity-mcp`): Secure local IPC (Windows).

### 2. Node.js Gateway (spawned by IDE)
- Translates MCP protocol to Unity JSON-RPC.
- Communicates via stdio with the IDE.
- Forwards requests to Unity's HTTP endpoint.

```
┌─────────────┐     stdio      ┌─────────────┐     HTTP      ┌─────────────┐
│   IDE/AI    │ ◄───────────► │   Gateway   │ ◄───────────► │    Unity    │
│  (Cursor)   │     MCP        │  (Node.js)  │   JSON-RPC    │   Editor    │
└─────────────┘                └─────────────┘                └─────────────┘
```

## Performance

The server includes optimizations for responsive background execution:
- Request queue with continuous processing via `EditorApplication.update`
- Automatic editor wake-up using `RepaintAllViews()` when requests arrive
- Typical response times: 30-175ms even with Unity in background

## License

MIT
