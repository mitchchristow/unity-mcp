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
- `unity.get_object_details(id, includeChildren?)`: Gets comprehensive object information.

## Components

- `unity.add_component(id, type)`: Adds a component by type name.
- `unity.set_component_property(id, component, field, value)`: Sets a public field or property.
- `unity.get_components(id)`: Lists all components on an object.
- `unity.get_component_properties(id, type)`: Gets all properties of a component.
- `unity.remove_component(id, type)`: Removes a component from an object.

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

## Selection

- `unity.get_selection()`: Returns currently selected GameObjects.
- `unity.set_selection(ids)`: Sets selection by instance IDs.
- `unity.clear_selection()`: Clears the selection.
- `unity.select_by_name(name, additive?)`: Selects objects by name pattern.
- `unity.focus_selection()`: Frames selection in scene view.

## Search

- `unity.find_objects_by_name(name, includeInactive?)`: Finds objects by name pattern.
- `unity.find_objects_by_tag(tag, includeInactive?)`: Finds objects by tag.
- `unity.find_objects_by_component(component, includeInactive?)`: Finds objects with a component.
- `unity.find_objects_by_layer(layer, includeInactive?)`: Finds objects on a layer.
- `unity.search_assets(query, folder?, limit?)`: Searches project assets.

## Scripts

- `unity.write_script(path, content)`: Writes a C# file to Assets.
- `unity.reload_scripts()`: Triggers a domain reload.

## Console

- `unity.console.log(message, type?)`: Logs to the Unity Console.
- `unity.console.clear()`: Clears the console.
- `unity.get_console_logs(limit?)`: Returns recent console log entries.

## Assets

- `unity.list_assets(type?, folder?)`: Lists project assets.
- `unity.get_asset_info(path)`: Gets detailed info about a specific asset.

## Files

- `unity.read_file(path)`: Reads a file from the project.
- `unity.write_file(path, content)`: Writes a file to the project.
- `unity.file_exists(path)`: Checks if a file exists.
- `unity.list_directory(path, filter?, recursive?)`: Lists directory contents.
- `unity.create_directory(path)`: Creates a directory.

## Screenshots

- `unity.capture_screenshot(filename?, superSize?)`: Captures Game view.
- `unity.capture_scene_view(filename?, width?, height?)`: Captures Scene view.

## Lighting

- `unity.create_light(type, name?, position?, rotation?, color?, intensity?)`: Creates a light.
- `unity.set_light_property(id, intensity?, color?, range?, spotAngle?, shadows?, enabled?)`: Sets light properties.
- `unity.get_lighting_settings()`: Gets ambient and lighting settings.
- `unity.set_ambient_light(mode?, color?, intensity?, skyColor?, equatorColor?, groundColor?)`: Sets ambient lighting.

## Cameras

- `unity.create_camera(name?, position?, rotation?, fieldOfView?, nearClip?, farClip?, orthographic?, orthographicSize?)`: Creates a camera.
- `unity.set_camera_property(id, fieldOfView?, nearClip?, farClip?, orthographic?, orthographicSize?, depth?, backgroundColor?, clearFlags?)`: Sets camera properties.
- `unity.get_camera_info(id)`: Gets camera information.
- `unity.get_scene_view_camera()`: Gets Scene view camera state.
- `unity.set_scene_view_camera(pivot?, rotation?, size?, orthographic?, lookAt?)`: Sets Scene view camera.

## Physics

- `unity.set_gravity(x, y, z)`: Sets the gravity vector.
- `unity.set_physics_property(property, value)`: Sets physics properties.
- `unity.raycast(origin, direction, maxDistance?, layerMask?)`: Performs a raycast.
- `unity.get_layer_collision_matrix()`: Gets the layer collision matrix.
- `unity.set_layer_collision(layer1, layer2, ignore)`: Sets layer collision behavior.

## Tags & Layers

- `unity.set_object_tag(id, tag)`: Sets an object's tag.
- `unity.set_object_layer(id, layer, includeChildren?)`: Sets an object's layer.
- `unity.create_tag(name)`: Creates a new tag.
- `unity.get_sorting_layers()`: Gets 2D sorting layers.

## Undo

- `unity.undo()`: Performs undo.
- `unity.redo()`: Performs redo.
- `unity.get_undo_history()`: Gets undo history.
- `unity.clear_undo(confirm)`: Clears undo history (requires confirm: true).
- `unity.begin_undo_group(name)`: Begins an undo group.
- `unity.end_undo_group()`: Ends an undo group.

## Animation

- `unity.get_animator_info(id)`: Gets Animator component info.
- `unity.get_animator_parameters(id)`: Gets Animator parameters.
- `unity.set_animator_parameter(id, name, value)`: Sets an Animator parameter.
- `unity.play_animator_state(id, stateName, layer?, normalizedTime?)`: Plays an Animator state.
- `unity.get_animation_clip_info(path)`: Gets animation clip details.

## Audio

- `unity.create_audio_source(id, clipPath?, volume?, pitch?, loop?, playOnAwake?, spatialBlend?)`: Creates an AudioSource.
- `unity.set_audio_source_property(id, volume?, pitch?, loop?, etc.)`: Sets AudioSource properties.
- `unity.get_audio_source_info(id)`: Gets AudioSource information.
- `unity.play_audio(id)`: Plays audio (Play mode only).
- `unity.stop_audio(id)`: Stops audio.
- `unity.set_audio_clip(id, clipPath)`: Sets the audio clip.

## UI

- `unity.create_canvas(name?, renderMode?)`: Creates a Canvas with EventSystem.
- `unity.create_ui_element(type, parentId?, name?, position?, size?)`: Creates a UI element.
- `unity.set_ui_text(id, text?, fontSize?, color?, alignment?)`: Sets UI Text properties.
- `unity.set_ui_image(id, spritePath?, color?, fillAmount?)`: Sets UI Image properties.
- `unity.set_rect_transform(id, anchoredPosition?, sizeDelta?, anchorMin?, anchorMax?, pivot?)`: Sets RectTransform.
- `unity.get_ui_info(id)`: Gets UI component information.

## Build

- `unity.set_build_target(target, targetGroup?)`: Sets the build target.
- `unity.add_scene_to_build(path, enabled?)`: Adds a scene to build settings.
- `unity.remove_scene_from_build(path?, index?)`: Removes a scene from build settings.
- `unity.get_scenes_in_build()`: Gets scenes in build settings.
- `unity.build_player(locationPath, target?, development?)`: Builds the player.

## Packages

- `unity.get_package_info(name)`: Gets package details.
- `unity.add_package(packageId)`: Adds a package.
- `unity.remove_package(name)`: Removes a package.
- `unity.search_packages(query?)`: Searches Unity registry.

## Terrain

- `unity.create_terrain(name?, position?, width?, height?, length?, heightmapResolution?)`: Creates a terrain.
- `unity.get_terrain_info(id)`: Gets terrain information.
- `unity.set_terrain_size(id, width?, height?, length?)`: Sets terrain size.
- `unity.set_terrain_height(id, x, z, height, radius?)`: Sets terrain height at a point.
- `unity.get_terrain_height(id, x, z)`: Gets terrain height at a point.
- `unity.set_terrain_layer(id, texturePath, layerIndex?, tileSize?)`: Sets a terrain texture layer.
- `unity.flatten_terrain(id, height?)`: Flattens the entire terrain.

## Particles

- `unity.create_particle_system(name?, position?, etc.)`: Creates a particle system.
- `unity.get_particle_system_info(id)`: Gets particle system information.
- `unity.set_particle_main(id, duration?, loop?, startLifetime?, etc.)`: Sets main module.
- `unity.set_particle_emission(id, rateOverTime?, rateOverDistance?, enabled?)`: Sets emission module.
- `unity.set_particle_shape(id, shapeType?, radius?, angle?, arc?)`: Sets shape module.
- `unity.play_particle_system(id, withChildren?)`: Plays particle system.
- `unity.stop_particle_system(id, withChildren?, clear?)`: Stops particle system.

## Navigation

- `unity.bake_navmesh()`: Bakes the NavMesh.
- `unity.clear_navmesh()`: Clears all NavMeshes.
- `unity.add_navmesh_agent(id, speed?, angularSpeed?, acceleration?, etc.)`: Adds NavMeshAgent.
- `unity.set_navmesh_agent(id, speed?, angularSpeed?, etc.)`: Sets NavMeshAgent properties.
- `unity.get_navmesh_agent_info(id)`: Gets NavMeshAgent information.
- `unity.add_navmesh_obstacle(id, shape?, size?, carve?)`: Adds NavMeshObstacle.
- `unity.set_navmesh_destination(id, destination)`: Sets agent destination (Play mode).
- `unity.calculate_path(start, end)`: Calculates a path between points.

## Editor Windows

- `unity.open_window(type, utility?)`: Opens an editor window.
- `unity.close_window(id)`: Closes an editor window.
- `unity.focus_window(id)`: Focuses an editor window.
- `unity.get_window_info(id)`: Gets window information.
- `unity.open_inspector(objectId?)`: Opens Inspector for an object.
- `unity.open_project_settings(path?)`: Opens Project Settings.
- `unity.open_preferences(path?)`: Opens Preferences.

## Menu

- `unity.execute_menu(path)`: Executes a menu item by path.

## Events

- `unity.get_recent_events(limit?)`: Returns recent events from the event history.
- `unity.clear_event_history()`: Clears the event history buffer.
- `unity.get_event_types()`: Returns list of available event types and their payloads.

## Editor State

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
