---
sidebar_position: 2
---

# Cursor Integration

Cursor has native MCP support and can automatically spawn the Unity MCP gateway.

## Automatic Configuration

The project includes a `.cursor/mcp.json` file that automatically configures the MCP server when you open the project in Cursor.

1. **Install Gateway Dependencies** (first time only):
   ```bash
   cd gateway
   npm install
   ```

2. **Open in Cursor**: Open the project folder in Cursor.

3. **Verify Connection**: Check Cursor's MCP panel - you should see:
   - 10 tools (unity_create_object, unity_set_transform, etc.)
   - 6 resources (unity://project/info, unity://scene/hierarchy, etc.)

4. **Start Using**: Ask Cursor to interact with Unity:
   > "Create a cube and position it at (0, 5, 0)"
   
   > "List all objects in the scene"
   
   > "Read the console logs and tell me if there are any errors"

## Manual Configuration

If automatic configuration doesn't work, create `.cursor/mcp.json` in your project root:

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

## Troubleshooting

### MCP Server Not Starting

1. Check that Node.js is installed: `node --version`
2. Ensure dependencies are installed: `cd gateway && npm install`
3. Check Cursor's MCP logs for errors

### "Method not found" Errors

Ensure you're using the latest gateway code. The gateway should report "10 tools, 0 prompts, 0 resources" (or "6 resources" if resources are enabled).

### Slow Response Times

If responses are slow when Unity is in the background:
1. This project includes optimizations for background execution
2. Typical response times should be 30-175ms
3. If still slow, try clicking on Unity once to wake it up

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

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Project version, platform, play state |
| `unity://scene/hierarchy` | Complete scene object tree |
| `unity://scene/list` | List of loaded scenes |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets (use `?type=material` to filter) |
| `unity://console/logs` | Recent console log entries |
