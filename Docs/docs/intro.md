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
- **Scripting**: Write and reload C# scripts on the fly.
- **Playmode Control**: Start, stop, and pause the game.
- **Real-time Events**: Listen to console logs and scene changes via WebSockets.
- **Secure IPC**: Named Pipes (Windows) and Unix Sockets (Mac/Linux) support.

## Getting Started

1. **Install the Package**: Add the package to your Unity project.
2. **Connect your IDE**: Configure Cursor or VS Code to connect to `http://localhost:17890`.
3. **Start Coding**: Use your AI assistant to build scenes and scripts!

:::warning Performance Note
**Unity throttles background execution in Edit Mode.**
If you minimize the Unity window or put it in the background while in **Edit Mode**, the MCP server may become slow to respond.

**For best performance:**
-   Keep the Unity window visible (e.g., on a second monitor).
-   Or, enter **Play Mode**, where the server runs at full speed even in the background.
:::

## Architecture

The server runs inside the Unity Editor as a background task, exposing:
- **HTTP JSON-RPC** (`:17890`): For command execution.
- **WebSocket** (`:17891`): For event streaming.
