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
- **MCP Resources**: Expose project state as readable resources for AI context.
- **Real-time Events**: Listen to console logs and scene changes via WebSockets.
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

## Available Tools (10)

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects in the scene |
| `unity_create_object` | Create a new empty GameObject |
| `unity_create_primitive` | Create a primitive (Cube, Sphere, etc.) |
| `unity_set_transform` | Set position, rotation, and scale |
| `unity_set_material` | Apply a material to an object |
| `unity_create_material` | Create a new material asset |
| `unity_set_material_property` | Set material properties |
| `unity_instantiate_prefab` | Instantiate a prefab |
| `unity_create_prefab` | Save a GameObject as a prefab |
| `unity_get_selection` | Get currently selected objects |

## Available Resources (6)

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Project version, platform, play state |
| `unity://scene/hierarchy` | Complete scene object tree |
| `unity://scene/list` | List of loaded scenes |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets |
| `unity://console/logs` | Recent console log entries |

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
