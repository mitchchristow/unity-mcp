# Unity MCP Server

**Control the Unity Editor directly from your AI Assistant.**

This project implements a **Model Context Protocol (MCP)** server that runs inside the Unity Editor. It allows external AI agents (like **Cursor**, **Antigravity**, or **VS Code**) to manipulate scenes, write scripts, and control play mode in real-time.

![Unity MCP Banner](https://placehold.co/600x200?text=Unity+MCP+Server)

## 🚀 Features

- **Scene Control**: Create, move, delete, and inspect GameObjects.
- **Component System**: Add components and modify properties via reflection.
- **Scripting**: Write and reload C# scripts instantly.
- **Play Mode**: Start, stop, and pause the game.
- **Real-time Events**: Stream console logs and scene changes via WebSockets.
- **Secure**: Supports Named Pipes (Windows) and Unix Sockets (Mac/Linux).

## 📦 Installation

### Option 1: Install via Unity Package Manager (Recommended)

You can install this package directly from GitHub into your Unity project.

1.  Open your Unity Project.
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
4.  The project will open with the MCP server running.

## ⚠️ Performance Note

**Unity throttles background execution in Edit Mode.**
If you minimize the Unity window or put it in the background while in **Edit Mode**, the MCP server may become slow to respond (up to several seconds latency).

**For best performance:**
-   Keep the Unity window visible (e.g., on a second monitor).
-   Or, enter **Play Mode**, where the server runs at full speed even in the background.

## 🔌 Connecting Your IDE

### Cursor / Antigravity

1.  Ensure Unity is open and the server is running (check the Console for `[MCP] HTTP Server started`).
2.  Configure your IDE to use the MCP server:

    **Command**: `curl`
    **Args**:
    ```bash
    -X POST -H "Content-Type: application/json" -d @- http://localhost:17890/mcp/rpc
    ```

### VS Code

1.  Open the `ide-integrations/vscode` folder.
2.  Run `npm install` and `npm run compile`.
3.  Launch the extension to get a dedicated Unity control panel.

## 📚 Documentation

Full documentation is available in the `Docs/` directory.

- [**RPC API Reference**](Docs/docs/rpc/overview.md): List of all available commands.
- [**Event System**](Docs/docs/events/overview.md): How to listen to real-time events.
- [**Contributing**](Docs/docs/development/contributing.md): How to build and test the server.

## 🛠️ Architecture

The server runs as a background task in the Unity Editor:

- **HTTP Server** (`:17890`): Handles JSON-RPC commands.
- **WebSocket Server** (`:17891`): Streams events.
- **Named Pipe** (`\\.\pipe\unity-mcp`): Secure local IPC (Windows).

## License

MIT
