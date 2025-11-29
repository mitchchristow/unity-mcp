---
sidebar_position: 1
slug: /
---

# Introduction

Welcome to the **Unity MCP Server** documentation.

This project allows external AI agents and IDEs (like Cursor, VS Code, and Antigravity) to directly control the Unity Editor via the [Model Context Protocol (MCP)](https://modelcontextprotocol.io).

## Features

- **Scene Manipulation**: Create, move, and delete GameObjects.
- **Component Management**: Add components and modify properties via reflection.
- **Asset Management**: List and inspect project assets (materials, prefabs, scripts, textures).
- **Scripting**: Write and reload C# scripts on the fly.
- **Playmode Control**: Start, stop, and pause the game.
- **Lighting & Cameras**: Create and configure lights, cameras, and ambient settings.
- **Physics**: Configure gravity, raycasting, and collision layers.
- **Animation & Audio**: Control animators, audio sources, and playback.
- **UI System**: Create canvases and UI elements.
- **Terrain & Particles**: Create terrains and particle systems.
- **Navigation**: Bake NavMesh, configure agents, and calculate paths.
- **Build Pipeline**: Configure build settings and create builds.
- **2D Game Development**: Sprites, tilemaps, 2D physics - ideal for strategy games.
- **MCP Resources**: 36 resources expose project state for AI context.
- **Real-time Events**: Stream scene changes, selection, play mode, and console logs via WebSockets.
- **Secure IPC**: Named Pipes (Windows) and Unix Sockets (Mac/Linux) support.

## Getting Started

1. **Install the Package**: Add the Unity MCP package to your Unity 6+ project.
2. **Install Gateway Dependencies**: Run `npm install` in the `gateway/` folder.
3. **Connect your IDE**: Open the project in Cursor, Antigravity, or VS Code.
4. **Start Coding**: Use your AI assistant to build scenes and scripts!

## How It Works

The system consists of two components:

1. **Unity MCP Package**: Runs inside the Unity Editor, exposing HTTP and WebSocket servers.
2. **Node.js Gateway**: Spawned by your IDE, translates MCP protocol to Unity JSON-RPC.

```
┌─────────────┐     stdio      ┌─────────────┐     HTTP      ┌─────────────┐
│   IDE/AI    │ ◄───────────► │   Gateway   │ ◄───────────► │    Unity    │
│  (Cursor)   │     MCP        │  (Node.js)  │   JSON-RPC    │   Editor    │
└─────────────┘                └─────────────┘                └─────────────┘
```

## Available Tools (79)

The server exposes 79 tools organized by category. Many tools use an `action` parameter for consolidated operations.

### Core Tools

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects in the scene |
| `unity_create_object` | Create a new empty GameObject |
| `unity_create_primitive` | Create a primitive (Cube, Sphere, etc.) |
| `unity_delete_object` | Delete a GameObject |
| `unity_set_transform` | Set position, rotation, and scale |
| `unity_get_selection` | Get currently selected objects |

### Consolidated Tools

These tools use an `action` parameter to combine related operations:

| Tool | Actions | Description |
|------|---------|-------------|
| `unity_playmode` | play, stop, pause | Control play mode |
| `unity_undo_action` | undo, redo, get_history, clear, begin_group, end_group | Undo/redo |
| `unity_selection` | set, clear, select_by_name, focus | Selection management |
| `unity_capture` | game, scene | Screenshot capture |
| `unity_find_objects` | name, tag, component, layer | Find objects |
| `unity_component` | add, remove, list, get_properties | Component management |
| `unity_file` | read, write, exists, list_dir, create_dir | File operations |

### Categories

- **Materials & Prefabs**: Create and apply materials, instantiate prefabs
- **Lighting**: Create lights, configure ambient lighting
- **Cameras**: Create and configure cameras, scene view controls
- **Physics**: Gravity, raycasting, layer collisions
- **UI**: Canvases, buttons, text, images
- **Terrain**: Create, sculpt, configure terrains
- **Particles**: Create and control particle systems
- **Navigation**: NavMesh baking, agents, pathfinding
- **Audio**: Audio sources and playback
- **Build**: Build targets and configuration
- **Packages**: Package management

## Available Resources (29)

Resources provide **read-only** data. The AI reads these automatically for context instead of calling tools.

### Core Resources

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Project version, platform, play state |
| `unity://scene/hierarchy` | Complete scene object tree |
| `unity://scene/list` | List of loaded scenes |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets |
| `unity://console/logs` | Recent console log entries |

### Additional Resources

| Category | Resources |
|----------|-----------|
| Scene Analysis | `unity://scene/stats`, `unity://scene/render-stats`, `unity://scene/memory-stats`, `unity://scene/analysis` |
| Objects | `unity://lights`, `unity://cameras`, `unity://terrains`, `unity://particles`, `unity://ui/elements` |
| Systems | `unity://physics`, `unity://tags`, `unity://layers`, `unity://audio/settings`, `unity://navmesh/settings` |
| Project | `unity://packages`, `unity://build/settings`, `unity://build/targets`, `unity://editor/windows` |
| Events | `unity://events/recent`, `unity://events/types`, `unity://events/status` |
| 2D Development | `unity://sprites`, `unity://tilemaps`, `unity://tiles`, `unity://2d/physics` |

## Architecture

The server runs inside the Unity Editor as a background task, exposing:
- **HTTP JSON-RPC** (`:17890`): For command execution.
- **WebSocket** (`:17891`): For event streaming.
- **Named Pipe** (`\\.\pipe\unity-mcp`): For secure local IPC (Windows).

## Performance

The server includes optimizations for responsive background execution:
- Request queue with continuous processing
- Automatic editor wake-up when requests arrive
- Typical response times: **30-175ms** even with Unity in background
