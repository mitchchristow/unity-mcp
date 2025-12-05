# Unity MCP Server for VS Code

This directory contains configuration for using the Unity MCP Server with Visual Studio Code and GitHub Copilot.

## Prerequisites

1. **Unity Editor**: Ensure your Unity project is open and the Unity MCP server is running (usually on port 17890/17891).
2. **Node.js**: Ensure Node.js is installed.
3. **Dependencies**: Run `npm install` in the `gateway` directory of this repository to install the necessary dependencies.

   ```bash
   cd gateway
   npm install
   ```

## Configuration

To enable the Unity MCP server in VS Code, you need to configure it in `mcp.json`.

### Option 1: Workspace Configuration (Recommended for Development)

We have provided a `mcp.json` file in the `.vscode` directory of this repository. If you open this repository in VS Code, the Unity MCP server should be automatically detected.

If it is not detected, or if you are working in a different workspace, you can create a `.vscode/mcp.json` file in your workspace root with the following content:

```json
{
  "servers": {
    "unity": {
      "command": "node",
      "args": ["/absolute/path/to/unity-mcp/gateway/index.js"],
      "type": "stdio"
    }
  }
}
```

**Note**: Replace `/absolute/path/to/unity-mcp/gateway/index.js` with the actual absolute path to the `gateway/index.js` file. If you are in the `unity-mcp` workspace, you can use `${workspaceFolder}/gateway/index.js`.

### Option 2: Global Configuration

You can also add the server to your global VS Code MCP configuration.

1. In VS Code, run the command **MCP: Manage MCP Servers** (or open your global `mcp.json`).
2. Add the Unity server configuration:

```json
{
  "servers": {
    "unity": {
      "command": "node",
      "args": ["C:/Users/mitch/Documents/Projects/unity-mcp/gateway/index.js"],
      "type": "stdio"
    }
  }
}
```

(Adjust the path as necessary).

## Usage

Once configured, you can use GitHub Copilot Chat to interact with Unity.
For example:
- "List all objects in the scene"
- "Create a cube at 0,0,0"
- "Change the material of the Player object"
