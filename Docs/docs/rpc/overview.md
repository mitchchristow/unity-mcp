---
sidebar_position: 2
---

# RPC API Reference

The Unity MCP Server exposes the following JSON-RPC 2.0 methods.

## Scene

- `unity.list_scenes()`: Returns all loaded scenes.
- `unity.open_scene(path)`: Opens a scene by path.
- `unity.save_scene()`: Saves all open scenes.

## Hierarchy

- `unity.list_objects(parentId?)`: Returns the hierarchy of the active scene.
- `unity.create_object(name, parentId?)`: Creates a new GameObject.
- `unity.create_primitive(type, name?, parentId?)`: Creates a primitive (Cube, Sphere, Capsule, Cylinder, Plane, Quad).
- `unity.delete_object(id)`: Destroys a GameObject.
- `unity.set_transform(id, position?, rotation?, scale?)`: Modifies transform.
- `unity.get_transform(id)`: Gets current transform.

## Components

- `unity.add_component(id, type)`: Adds a component by type name.
- `unity.set_component_property(id, component, field, value)`: Sets a public field or property.

## Materials

- `unity.set_material(id, path)`: Applies a material asset to an object's renderer.
- `unity.create_material(name, shader?)`: Creates a new material asset.
- `unity.set_material_property(path, color?, float?)`: Sets material properties.

## Prefabs

- `unity.instantiate_prefab(path, position?, rotation?, parent?)`: Instantiates a prefab.
- `unity.create_prefab(id, path)`: Saves a GameObject as a prefab.

## Playmode

- `unity.play()`: Enters play mode.
- `unity.stop()`: Exits play mode.
- `unity.pause()`: Toggles pause.

## Scripts

- `unity.write_script(path, content)`: Writes a C# file to Assets.
- `unity.reload_scripts()`: Triggers a domain reload.

## Console

- `unity.console.log(message, type?)`: Logs to the Unity Console (type: "info", "warning", "error").
- `unity.console.clear()`: Clears the console.
- `unity.get_console_logs(limit?)`: Returns recent console log entries.

## Assets

- `unity.list_assets(type?, folder?)`: Lists project assets.
  - Types: `material`, `prefab`, `script`, `texture`, `mesh`, `audio`, `scene`, `shader`, `animation`
- `unity.get_asset_info(path)`: Gets detailed info about a specific asset.

## Editor State

- `unity.get_selection()`: Returns currently selected GameObjects.
- `unity.get_project_info()`: Returns Unity version, platform, project path, and play state.

## Example Request

```json
{
  "jsonrpc": "2.0",
  "method": "unity.create_primitive",
  "params": {
    "type": "Cube",
    "name": "MyCube"
  },
  "id": 1
}
```

## Example Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "id": -12345
  },
  "id": 1
}
```
