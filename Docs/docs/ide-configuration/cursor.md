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
   - **79 tools** (unity_create_object, unity_playmode, unity_create_light, etc.)
   - **29 resources** (unity://project/info, unity://scene/hierarchy, unity://lights, etc.)

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

Ensure you're using the latest gateway code. The gateway should report "79 tools, 0 prompts, 29 resources".

### Slow Response Times

If responses are slow when Unity is in the background:
1. This project includes optimizations for background execution
2. Typical response times should be 30-175ms
3. If still slow, try clicking on Unity once to wake it up

## Available Tools (79)

The gateway exposes 79 tools. Many use an `action` parameter for consolidated operations.

### Core Tools

| Tool | Description |
|------|-------------|
| `unity_list_objects` | List all GameObjects in the scene |
| `unity_create_object` | Create a new empty GameObject |
| `unity_create_primitive` | Create a primitive (Cube, Sphere, etc.) |
| `unity_delete_object` | Delete a GameObject |
| `unity_set_transform` | Set position, rotation, and scale |
| `unity_get_selection` | Get currently selected objects |

### Consolidated Tools (action-based)

| Tool | Actions | Description |
|------|---------|-------------|
| `unity_playmode` | play, stop, pause | Control play mode |
| `unity_undo_action` | undo, redo, get_history, clear, begin_group, end_group | Undo/redo |
| `unity_selection` | set, clear, select_by_name, focus | Selection management |
| `unity_capture` | game, scene | Screenshot capture |
| `unity_find_objects` | name, tag, component, layer | Find objects |
| `unity_component` | add, remove, list, get_properties | Component management |
| `unity_file` | read, write, exists, list_dir, create_dir | File operations |

### Additional Categories

- **Materials & Prefabs**: `unity_set_material`, `unity_create_material`, `unity_instantiate_prefab`, `unity_create_prefab`
- **Lighting**: `unity_create_light`, `unity_set_light_property`, `unity_get_lighting_settings`, `unity_set_ambient_light`
- **Cameras**: `unity_create_camera`, `unity_set_camera_property`, `unity_get_camera_info`
- **Physics**: `unity_set_gravity`, `unity_set_physics_property`, `unity_raycast`
- **UI**: `unity_create_canvas`, `unity_create_ui_element`, `unity_set_ui_text`, `unity_set_ui_image`
- **Terrain**: `unity_create_terrain`, `unity_terrain_height`, `unity_set_terrain_layer`
- **Particles**: `unity_create_particle_system`, `unity_set_particle_module`, `unity_particle_playback`
- **Navigation**: `unity_navmesh_build`, `unity_add_navmesh_agent`, `unity_calculate_path`
- **Audio**: `unity_create_audio_source`, `unity_audio_playback`, `unity_set_audio_clip`
- **Build**: `unity_set_build_target`, `unity_add_scene_to_build`, `unity_build_player`
- **Packages**: `unity_add_package`, `unity_remove_package`, `unity_search_packages`

## Available Resources (29)

Resources provide **read-only** data. The AI reads these automatically for context.

### Core Resources

| Resource URI | Description |
|--------------|-------------|
| `unity://project/info` | Project version, platform, play state |
| `unity://scene/hierarchy` | Complete scene object tree |
| `unity://scene/list` | List of loaded scenes |
| `unity://selection` | Currently selected objects |
| `unity://assets` | Project assets (use `?type=material` to filter) |
| `unity://console/logs` | Recent console log entries |

### Scene Analysis

| Resource URI | Description |
|--------------|-------------|
| `unity://scene/stats` | Scene statistics |
| `unity://scene/render-stats` | Rendering statistics |
| `unity://scene/memory-stats` | Memory usage |
| `unity://scene/analysis` | Optimization analysis |

### Objects & Systems

| Resource URI | Description |
|--------------|-------------|
| `unity://lights` | All lights in scene |
| `unity://cameras` | All cameras in scene |
| `unity://particles` | All particle systems |
| `unity://physics` | Physics settings |
| `unity://packages` | Installed packages |
| `unity://build/settings` | Build settings |
