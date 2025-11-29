---
sidebar_position: 3
---

# VS Code Integration

VS Code does not have native MCP support, but this project provides two options for integration.

## Option 1: Auto-Start Task (Recommended)

The project includes a `.vscode/tasks.json` that automatically starts the MCP gateway when you open the folder.

### Setup

1. **Install Gateway Dependencies** (first time only):
   ```bash
   cd gateway
   npm install
   ```

2. **Open in VS Code**: Open the project folder in VS Code.

3. **Allow Task**: When prompted, allow the "Start Unity MCP Gateway" task to run.

4. **Verify**: The gateway will start in a terminal panel.

### Manual Start

If the task doesn't auto-start, run it manually:
1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
2. Type "Tasks: Run Task"
3. Select "Start Unity MCP Gateway"

## Option 2: VS Code Extension

The project includes a custom VS Code extension for deeper integration.

### Installation

1. Navigate to the extension directory:
   ```bash
   cd ide-integrations/vscode/extension
   ```

2. Install dependencies and compile:
   ```bash
   npm install
   npm run compile
   ```

3. **Debug Mode**: Open the `ide-integrations/vscode/extension` folder in VS Code and press `F5` to launch the extension host.

4. **Install Permanently**: Package it into a `.vsix` file using `vsce package` and install in your main VS Code instance.

### Extension Features

- **Unity Status Bar**: Shows connection status to the Unity Editor.
- **Commands**:
  - `Unity MCP: Connect`: Retry connection.
  - `Unity MCP: Play`: Enter play mode.
  - `Unity MCP: Stop`: Exit play mode.
  - `Unity MCP: Refresh Hierarchy`: Fetch the current scene hierarchy.

## Using with AI Extensions

If you use an AI extension in VS Code that supports MCP (like Continue or similar), you can configure it to use the Unity gateway:

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

Refer to your AI extension's documentation for the correct configuration location.

## Available Functionality

When connected, you have access to:

### Tools (10)

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects |
| `unity_create_object` | Create empty GameObject |
| `unity_create_primitive` | Create primitive shapes |
| `unity_set_transform` | Modify position/rotation/scale |
| `unity_set_material` | Apply materials |
| `unity_create_material` | Create materials |
| `unity_set_material_property` | Set material properties |
| `unity_instantiate_prefab` | Instantiate prefabs |
| `unity_create_prefab` | Save as prefab |
| `unity_get_selection` | Get selected objects |

### Resources (6)

| Resource | Description |
|----------|-------------|
| `unity://project/info` | Project information |
| `unity://scene/hierarchy` | Scene object tree |
| `unity://scene/list` | Loaded scenes |
| `unity://selection` | Current selection |
| `unity://assets` | Project assets |
| `unity://console/logs` | Console logs |
