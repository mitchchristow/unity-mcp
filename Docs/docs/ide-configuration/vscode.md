---
sidebar_position: 3
---

# VS Code integration

The project includes a custom VS Code extension to control Unity directly from the editor.

## Installation

1.  Navigate to the extension directory:
    ```bash
    cd ide-integrations/vscode
    ```
2.  Install dependencies and compile:
    ```bash
    npm install
    npm run compile
    ```
3.  **Debug Mode**: Open the folder in VS Code and press `F5` to launch the extension host.
4.  **Install**: You can package it into a `.vsix` file using `vsce package` and install it in your main VS Code instance.

## Features

- **Unity Status Bar**: Shows connection status to the Unity Editor.
- **Commands**:
    - `Unity MCP: Connect`: Retry connection.
    - `Unity MCP: Play`: Enter play mode.
    - `Unity MCP: Stop`: Exit play mode.
    - `Unity MCP: Refresh Hierarchy`: Fetch the current scene hierarchy.
