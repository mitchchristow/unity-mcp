# Unity MCP Server

**Control the Unity Editor directly from your AI Assistant.**

This project implements a **Model Context Protocol (MCP)** server that runs inside the Unity Editor. It allows external AI agents (like **Cursor**, **Antigravity**, or **VS Code**) to manipulate scenes, write scripts, and control play mode in real-time.

![Unity MCP Banner](https://placehold.co/600x200?text=Unity+MCP+Server)

## Features

- **Scene Control**: Create, move, delete, and inspect GameObjects.
- **Component System**: Add components and modify properties via reflection.
- **Asset Management**: List and inspect project assets (materials, prefabs, scripts, etc.).
- **Scripting**: Write and reload C# scripts instantly.
- **Play Mode**: Start, stop, and pause the game.
- **Real-time Events**: Stream console logs and scene changes via WebSockets.
- **MCP Resources**: Expose project info, scene hierarchy, assets, and console logs as readable resources.
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
3.  The MCP server will start automatically and provide 10 tools + 6 resources.

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

## Available Tools

The MCP server exposes 10 tools for controlling Unity:

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

## Available Resources

The MCP server exposes 6 resources for reading Unity state:

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Project version, platform, and play state |
| `unity://scene/hierarchy` | Complete scene object tree |
| `unity://scene/list` | List of loaded scenes |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets (materials, prefabs, scripts) |
| `unity://console/logs` | Recent console log entries |

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
