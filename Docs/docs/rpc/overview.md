---
sidebar_position: 2
---

# RPC API reference

The Unity MCP Server exposes the following JSON-RPC 2.0 methods.

## Scene

- `unity.list_scenes()`: Returns all scenes in build settings.
- `unity.open_scene(path)`: Opens a scene by path.
- `unity.save_scene()`: Saves all open scenes.

## Hierarchy

- `unity.list_objects()`: Returns the hierarchy of the active scene.
- `unity.create_object(name, parentId?)`: Creates a new GameObject.
- `unity.delete_object(id)`: Destroys a GameObject.
- `unity.set_transform(id, position?, rotation?, scale?)`: Modifies transform.
- `unity.get_transform(id)`: Gets current transform.

## Components

- `unity.add_component(id, type)`: Adds a component by type name.
- `unity.set_component_property(id, component, field, value)`: Sets a public field or property.

## Playmode

- `unity.play()`: Enters play mode.
- `unity.stop()`: Exits play mode.
- `unity.pause()`: Toggles pause.

## Scripts

- `unity.write_script(path, content)`: Writes a C# file to Assets.
- `unity.reload_scripts()`: Triggers a domain reload.

## Console

- `unity.console.log(message, type?)`: Logs to the Unity Console.
- `unity.console.clear()`: Clears the console.
