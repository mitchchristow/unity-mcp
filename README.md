# Unity MCP Server

**Control the Unity Editor directly from your AI Assistant.**

This project implements a **Model Context Protocol (MCP)** server that runs inside the Unity Editor. It allows AI-powered IDEs like **Cursor** to manipulate scenes, write scripts, and control play mode through natural language, enabling direct GenAI usage in game development.

![Unity MCP Banner](Assets/banner.png)

---

## 📖 Table of Contents

- [For Users: Quick Start](#-for-users-quick-start)
- [For Developers: Contributing](#-for-developers-contributing)
- [Features](#-features)
- [Available Tools, Resources & Prompts](#-available-tools-resources--prompts)
- [Architecture](#-architecture)
- [Documentation](#-documentation)
- [License](#-license)

---

## 🚀 For Users: Quick Start

Want to use this MCP server to build Unity games with AI? Follow these steps:

### Prerequisites

- **Unity 6+** (6000.x)
- **Node.js 18+**
- **Cursor IDE** (recommended) or another MCP-compatible IDE

### Step 1: Install the Unity Package

**Option A: From GitHub (Recommended)**

1. Open your Unity project
2. Go to **Window > Package Manager**
3. Click **+** → **Add package from git URL...**
4. Enter:
   ```
   https://github.com/mitchchristow/unity-mcp.git?path=/Packages/org.christowm.unity.mcp
   ```

**Option B: Clone for Local Development**

```bash
git clone https://github.com/mitchchristow/unity-mcp.git
cd unity-mcp
```
Then open the folder in Unity Hub.

### Step 2: Install Gateway Dependencies

```bash
cd gateway
npm install
```

### Step 3: Connect Your IDE

#### Cursor (✅ Fully Supported)

Cursor is the recommended IDE with full MCP support.

1. Open Unity and wait for `[MCP] HTTP Server started` in the Console
2. Open the project folder in Cursor
3. The MCP server starts automatically via `.cursor/mcp.json`
4. Start chatting! Try: *"Create a red cube at position (0, 1, 0)"*

**Manual Setup** (if auto-detection fails):

Create `.cursor/mcp.json`:
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

#### Antigravity (⚠️ Global Config Only)

> **Note**: According to the [Antigravity MCP documentation](https://antigravity.google/docs/mcp), Antigravity **only supports global MCP configuration**. Per-project configuration is not available.

**Setup:**

1. Open Antigravity → Click **"..."** → **"Manage MCP Servers"** → **"View raw config"**
2. Add this configuration (update the path to your installation):

```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["/absolute/path/to/unity-mcp/gateway/index.js"],
      "cwd": "/absolute/path/to/unity-mcp"
    }
  }
}
```

See [Antigravity Integration Guide](Docs/docs/ide-configuration/antigravity.md) for detailed instructions.

#### VS Code (✅ Supported via Kilo and Copilot)

VS Code supports MCP servers through the **Kilo** and **GitHub Copilot** extension (and others).

**Automatic Setup:**
This repository includes a `.vscode/mcp.json` file. If you open this folder in VS Code with the GitHub Copilot extension installed, the Unity MCP server should be automatically detected.

**Manual Setup:**
If you are adding the Unity MCP server to a different workspace:

1. Create a `.vscode/mcp.json` file in your project root.
2. Add the configuration:
   ```json
   {
     "servers": {
       "unity": {
         "command": "node",
         "args": ["/path/to/unity-mcp/gateway/index.js"],
         "type": "stdio"
       }
     }
   }
   ```

See [VS Code Integration Guide](ide-integrations/vscode/README.md) for more details.

### Step 4: Start Building!

Once connected, you can use natural language to control Unity:

- *"Create a 2D player character with WASD movement"*
- *"Add a Rigidbody2D to the selected object"*
- *"Set up a turn-based battle system"*
- *"What objects are in my scene?"*

---

## 🛠 For Developers: Contributing

Want to extend the MCP server, fix bugs, or add new features? This section is for you.

### Development Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/mitchchristow/unity-mcp.git
   cd unity-mcp
   ```

2. **Open in Unity**: Open the folder in Unity Hub (requires Unity 6+)

3. **Install gateway dependencies**:
   ```bash
   cd gateway
   npm install
   ```

4. **Project Structure**:
   ```
   unity-mcp/
   ├── Packages/org.christowm.unity.mcp/   # Unity Editor package
   │   └── Editor/
   │       ├── MCP/                        # MCP server implementation
   │       │   ├── Rpc/Controllers/        # RPC method handlers
   │       │   ├── Events/                 # WebSocket event system
   │       │   └── Progress/               # Progress tracking
   │       ├── Networking/                 # HTTP & WebSocket servers
   │       └── IPC/                        # Named Pipe server
   ├── gateway/                            # Node.js MCP gateway
   │   └── index.js                        # Tool/Resource/Prompt definitions
   ├── Docs/                               # Documentation (Docusaurus)
   ├── .cursor/                            # Cursor IDE MCP config
   ├── ide-integrations/                   # IDE config templates
   │   ├── antigravity/                    # Global config template
   │   └── vscode/                         # VS Code extension (WIP)
   └── TODO.md                             # Future improvements roadmap
   ```

### Adding New Features

#### Adding a New Tool

1. **Create/update a Controller** in `Packages/.../Rpc/Controllers/`:
   ```csharp
   public static class MyController
   {
       public static void Register()
       {
           JsonRpcDispatcher.RegisterMethod("unity.my_method", MyMethod);
       }
       
       private static JObject MyMethod(JObject p)
       {
           // Implementation
           return new JObject { ["ok"] = true };
       }
   }
   ```

2. **Register in `McpServer.cs`**:
   ```csharp
   MyController.Register();
   ```

3. **Add tool definition in `gateway/index.js`**:
   ```javascript
   {
     name: "unity_my_tool",
     description: "What this tool does",
     inputSchema: {
       type: "object",
       properties: { /* ... */ },
       required: ["param1"],
     },
   },
   ```

4. **Add handler** in the `CallToolRequestSchema` handler if needed.

#### Adding a New Resource

1. Add RPC method in Unity (similar to tools)
2. Add resource definition to `RESOURCES` array in `gateway/index.js`
3. Add URI-to-method mapping in `ReadResourceRequestSchema` handler

#### Adding a New Prompt

Add to the `PROMPTS` array and implement in `generatePromptContent()` function in `gateway/index.js`.

### Testing

```bash
# Test RPC endpoint
curl -X POST http://localhost:17890/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"unity.get_project_info","params":{},"id":1}'

# Test WebSocket
wscat -c ws://localhost:17891/mcp/events
```

### Key Files to Know

| File | Purpose |
|------|---------|
| `gateway/index.js` | MCP gateway - all tools, resources, prompts |
| `McpServer.cs` | Unity-side server initialization |
| `HttpServer.cs` | HTTP JSON-RPC handler |
| `WebSocketServer.cs` | Real-time event streaming |
| `JsonRpcDispatcher.cs` | Routes RPC calls to controllers |

### Pull Request Guidelines

1. Create a feature branch: `feature/my-feature`
2. Follow existing code style
3. Update documentation if adding user-facing features
4. Test with Cursor to verify MCP integration
5. Update `TODO.md` if implementing a backlog item

See [TODO.md](TODO.md) for the roadmap of planned improvements.

---

## ✨ Features

| Category | Capabilities |
|----------|--------------|
| **Scene Control** | Create, move, delete, inspect GameObjects |
| **Components** | Add/remove components, modify properties via reflection |
| **Assets** | List and inspect materials, prefabs, scripts, textures |
| **Scripting** | Create scripts from templates, monitor compilation |
| **Play Mode** | Start, stop, pause the game |
| **Lighting** | Create lights, configure ambient lighting |
| **Cameras** | Create cameras, control Scene view |
| **Physics** | Gravity, raycasting, collision layers (2D & 3D) |
| **UI** | Create canvases, buttons, text, images |
| **Terrain** | Create and sculpt terrains |
| **Particles** | Create and configure particle systems |
| **Navigation** | NavMesh baking, agents, pathfinding |
| **Audio** | Audio sources, playback control |
| **Build** | Configure and execute builds |
| **2D Development** | Sprites, tilemaps, 2D physics |
| **Prompts** | 8 workflow templates for common tasks |
| **Events** | Real-time scene/selection/console streaming |

---

## 📦 Available Tools, Resources & Prompts

### Tools (80)

The server exposes **80 tools** organized by category. Many use an `action` parameter to consolidate related operations.

<details>
<summary><strong>Core Tools</strong></summary>

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects in the scene |
| `unity_create_object` | Create a new empty GameObject |
| `unity_create_primitive` | Create a primitive (Cube, Sphere, etc.) |
| `unity_delete_object` | Delete a GameObject |
| `unity_set_transform` | Set position, rotation, and scale |
| `unity_selection` | Selection: set, clear, select_by_name, focus |
| `unity_find_objects` | Find by: name, tag, component, or layer |

</details>

<details>
<summary><strong>Consolidated Tools (action-based)</strong></summary>

| Tool | Actions |
|------|---------|
| `unity_playmode` | play, stop, pause |
| `unity_undo_action` | undo, redo, get_history, clear, begin_group, end_group |
| `unity_component` | add, remove, list, get_properties |
| `unity_file` | read, write, exists, list_dir, create_dir |
| `unity_capture` | game, scene |
| `unity_animator` | get_info, get_parameters, set_parameter, play_state |
| `unity_build` | set_target, add_scene, remove_scene, get_scenes, build |
| `unity_package` | get_info, add, remove, search |
| `unity_window` | open, close, focus, get_info |
| `unity_sprite` | create, set_sprite, set_property, get_info |
| `unity_tilemap` | create, set_tile, get_tile, fill, clear_all, get_info |

</details>

<details>
<summary><strong>Additional Categories</strong></summary>

- **Lighting**: create_light, set_light_property, get_lighting_settings, set_ambient_light
- **Cameras**: create_camera, set_camera_property, get_camera_info, scene_view controls
- **Physics**: set_gravity, set_physics_property, raycast, layer_collision
- **UI**: create_canvas, create_ui_element, set_ui_text, set_ui_image, set_rect_transform
- **Terrain**: create_terrain, set_terrain_size, terrain_height, flatten_terrain
- **Particles**: create_particle_system, set_particle_module, particle_playback
- **Navigation**: navmesh_build, add_navmesh_agent, set_navmesh_destination, calculate_path
- **Audio**: create_audio_source, set_audio_source_property, audio_playback
- **2D Physics**: physics_2d_body, physics_2d_query, set_physics_2d_property
- **Scripting**: create_script, get_component_api

</details>

### Resources (42)

Resources provide **read-only context** that the AI reads automatically.

<details>
<summary><strong>All Resources</strong></summary>

| Category | Resources |
|----------|-----------|
| **Project** | `unity://project/info`, `unity://assets`, `unity://packages` |
| **Scene** | `unity://scene/hierarchy`, `unity://scene/list`, `unity://scene/stats`, `unity://scene/analysis` |
| **Selection** | `unity://selection`, `unity://console/logs` |
| **Objects** | `unity://lights`, `unity://cameras`, `unity://terrains`, `unity://particles`, `unity://ui/elements` |
| **Systems** | `unity://physics`, `unity://tags`, `unity://layers`, `unity://audio/settings` |
| **Navigation** | `unity://navmesh/settings`, `unity://navmesh/agents` |
| **Build** | `unity://build/settings`, `unity://build/targets` |
| **Events** | `unity://events/recent`, `unity://events/types`, `unity://events/status` |
| **2D** | `unity://sprites`, `unity://tilemaps`, `unity://tiles`, `unity://2d/physics` |
| **Scripting** | `unity://scripts/errors`, `unity://scripts/warnings`, `unity://scripts/templates`, `unity://components/types` |
| **Progress** | `unity://progress` |

</details>

### Prompts (8)

Pre-defined workflow templates for complex tasks:

| Prompt | Description |
|--------|-------------|
| `create_2d_character` | 2D character with sprite, physics, movement script |
| `setup_turn_based_system` | Turn manager, units, action system |
| `create_grid_map` | Grid-based map with tilemap and pathfinding |
| `create_ui_menu` | UI menu (main, pause, settings, inventory, battle) |
| `setup_unit_stats` | ScriptableObject stats system for RPG/strategy |
| `create_audio_manager` | Audio singleton with BGM/SFX pooling |
| `optimize_scene` | Scene analysis and optimization recommendations |
| `setup_save_system` | JSON-based save/load system |

---

## 🏗 Architecture

```
┌─────────────┐     stdio      ┌─────────────┐     HTTP      ┌─────────────┐
│   IDE/AI    │ ◄────────────► │   Gateway   │ ◄───────────► │    Unity    │
│  (Cursor)   │     MCP        │  (Node.js)  │   JSON-RPC    │   Editor    │
└─────────────┘                └─────────────┘               └─────────────┘
                                     │
                                     │ WebSocket
                                     ▼
                               Real-time Events
```

### Components

| Component | Port | Purpose |
|-----------|------|---------|
| **HTTP Server** | 17890 | JSON-RPC command handling |
| **WebSocket Server** | 17891 | Real-time event streaming |
| **Named Pipe** | `\\.\pipe\unity-mcp` | Secure local IPC (Windows) |
| **Node.js Gateway** | stdio | MCP protocol translation |

### Performance

- Request queue with continuous processing via `EditorApplication.update`
- Automatic editor wake-up when Unity is in background
- Typical response times: **30-175ms**

---

## 📚 Documentation

Full documentation is available in the `Docs/` directory:

- [RPC API Reference](Docs/docs/rpc/overview.md)
- [2D Development Guide](Docs/docs/2d-development/overview.md)
- [Scripting Assistance](Docs/docs/scripting/overview.md)
- [Event System](Docs/docs/events/overview.md)
- [IDE Configuration](Docs/docs/ide-configuration/cursor.md)

See also: [TODO.md](TODO.md) for planned improvements.

---

## 🐛 Issues & Feature Requests

- **Bug reports**: [Open an issue](https://github.com/mitchchristow/unity-mcp/issues) with steps to reproduce
- **Feature requests**: [Open an issue](https://github.com/mitchchristow/unity-mcp/issues) and tag it with `enhancement`
- **Questions**: Use [GitHub Discussions](https://github.com/mitchchristow/unity-mcp/discussions) or open an issue

See [TODO.md](TODO.md) for the roadmap of planned features.

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

---

**Made with ❤️ for the Unity + AI community**
