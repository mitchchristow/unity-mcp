---
sidebar_position: 1
---

# Antigravity Integration

Antigravity can natively communicate with the Unity MCP server to perform complex agentic tasks.

## Configuration

The project includes configuration files that Antigravity automatically detects:

- `workspace.json` in the project root
- `ide-integrations/antigravity/workspace.json`

## Setup

1. **Install Gateway Dependencies** (first time only):
   ```bash
   cd gateway
   npm install
   ```

2. **Ensure Unity is Running**: Open your Unity project and verify the MCP server is running (check Console for `[MCP] HTTP Server started`).

3. **Open in Antigravity**: Open the project folder in Antigravity.

4. **Verify Connection**: The agent will automatically have access to the Unity toolset with 10 tools and 6 resources.

## Example Prompts

You can ask Antigravity to perform high-level tasks:

> "Create a new scene called 'Level1'."

> "Add a Plane at (0,0,0) and a Cube at (0,1,0). Make the Cube red."

> "Write a script 'Rotator.cs' that rotates the object 90 degrees per second and attach it to the Cube."

> "Enter play mode and tell me if there are any console errors."

> "Show me all the prefabs in the project."

> "What's the current scene hierarchy?"

## Available Tools

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

## Available Resources

Resources provide read-only context about the Unity project:

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Unity version, platform, project path, play state |
| `unity://scene/hierarchy` | Complete hierarchy of all GameObjects |
| `unity://scene/list` | List of loaded scenes with paths |
| `unity://selection` | Currently selected objects with positions |
| `unity://assets` | Project assets (materials, prefabs, scripts) |
| `unity://console/logs` | Recent console log entries |

## Manual Configuration

If automatic detection doesn't work, ensure `workspace.json` exists in your project root:

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
