
function resolveToolCall(toolName, args) {
  let rpcMethod;
  let rpcParams = args;

  if (toolName === "unity_playmode") {
    const actionMap = { play: "unity.play", stop: "unity.stop", pause: "unity.pause" };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid playmode action: ${args.action}`);
    rpcParams = {};
  } else if (toolName === "unity_undo_action") {
    const actionMap = {
      undo: "unity.undo",
      redo: "unity.redo",
      get_history: "unity.get_undo_history",
      clear: "unity.clear_undo",
      begin_group: "unity.begin_undo_group",
      end_group: "unity.end_undo_group"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid undo action: ${args.action}`);
    if (args.action === "clear") rpcParams = { confirm: args.confirm };
    else if (args.action === "begin_group") rpcParams = { name: args.name };
    else rpcParams = {};
  } else if (toolName === "unity_selection") {
    const actionMap = {
      set: "unity.set_selection",
      clear: "unity.clear_selection",
      select_by_name: "unity.select_by_name",
      focus: "unity.focus_selection"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid selection action: ${args.action}`);
    if (args.action === "set") rpcParams = { ids: args.ids };
    else if (args.action === "select_by_name") rpcParams = { name: args.name, additive: args.additive };
    else rpcParams = {};
  } else if (toolName === "unity_capture") {
    rpcMethod = args.view === "game" ? "unity.capture_screenshot" : "unity.capture_scene_view";
    rpcParams = { filename: args.filename };
    if (args.view === "game") rpcParams.superSize = args.superSize;
    else { rpcParams.width = args.width; rpcParams.height = args.height; }
  } else if (toolName === "unity_audio_playback") {
    rpcMethod = args.action === "play" ? "unity.play_audio" : "unity.stop_audio";
    rpcParams = { id: args.id };
  } else if (toolName === "unity_particle_playback") {
    rpcMethod = args.action === "play" ? "unity.play_particle_system" : "unity.stop_particle_system";
    rpcParams = { id: args.id, withChildren: args.withChildren };
    if (args.action === "stop") rpcParams.clear = args.clear;
  } else if (toolName === "unity_window") {
    const actionMap = {
      open: "unity.open_window",
      close: "unity.close_window",
      focus: "unity.focus_window",
      get_info: "unity.get_window_info"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid window action: ${args.action}`);
    rpcParams = { type: args.type, id: args.id, utility: args.utility };
  } else if (toolName === "unity_find_objects") {
    const actionMap = {
      name: "unity.find_objects_by_name",
      tag: "unity.find_objects_by_tag",
      component: "unity.find_objects_by_component",
      layer: "unity.find_objects_by_layer"
    };
    rpcMethod = actionMap[args.by];
    if (!rpcMethod) throw new Error(`Invalid find_objects criterion: ${args.by}`);
    rpcParams = { includeInactive: args.includeInactive };
    if (args.by === "name") rpcParams.name = args.name;
    else if (args.by === "tag") rpcParams.tag = args.tag;
    else if (args.by === "component") rpcParams.component = args.component;
    else if (args.by === "layer") rpcParams.layer = args.layer;
  } else if (toolName === "unity_terrain_height") {
    rpcMethod = args.action === "get" ? "unity.get_terrain_height" : "unity.set_terrain_height";
    rpcParams = { id: args.id, x: args.x, z: args.z };
    if (args.action === "set") { rpcParams.height = args.height; rpcParams.radius = args.radius; }
  } else if (toolName === "unity_navmesh_build") {
    rpcMethod = args.action === "bake" ? "unity.bake_navmesh" : "unity.clear_navmesh";
    rpcParams = {};
  } else if (toolName === "unity_animator") {
    const actionMap = {
      get_info: "unity.get_animator_info",
      get_parameters: "unity.get_animator_parameters",
      set_parameter: "unity.set_animator_parameter",
      play_state: "unity.play_animator_state"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid animator action: ${args.action}`);
    rpcParams = { id: args.id };
    if (args.action === "set_parameter") {
      rpcParams.name = args.name;
      if (args.floatValue !== undefined) rpcParams.floatValue = args.floatValue;
      if (args.intValue !== undefined) rpcParams.intValue = args.intValue;
      if (args.boolValue !== undefined) rpcParams.boolValue = args.boolValue;
      if (args.trigger !== undefined) rpcParams.trigger = args.trigger;
    } else if (args.action === "play_state") {
      rpcParams.stateName = args.stateName;
      rpcParams.layer = args.layer;
      rpcParams.normalizedTime = args.normalizedTime;
    }
  } else if (toolName === "unity_set_particle_module") {
    const moduleMap = {
      main: "unity.set_particle_main",
      emission: "unity.set_particle_emission",
      shape: "unity.set_particle_shape"
    };
    rpcMethod = moduleMap[args.module];
    if (!rpcMethod) throw new Error(`Invalid particle module: ${args.module}`);
    rpcParams = { id: args.id };
    const props = ["duration", "loop", "startLifetime", "startSpeed", "startSize", "maxParticles",
                   "gravityModifier", "simulationSpeed", "playOnAwake", "startColor",
                   "enabled", "rateOverTime", "rateOverDistance",
                   "shapeType", "radius", "angle", "arc"];
    props.forEach(p => { if (args[p] !== undefined) rpcParams[p] = args[p]; });
  } else if (toolName === "unity_open_panel") {
    const panelMap = {
      inspector: "unity.open_inspector",
      project_settings: "unity.open_project_settings",
      preferences: "unity.open_preferences"
    };
    rpcMethod = panelMap[args.panel];
    if (!rpcMethod) throw new Error(`Invalid panel: ${args.panel}`);
    rpcParams = {};
    if (args.panel === "inspector") rpcParams.objectId = args.objectId;
    else rpcParams.path = args.path;
  } else if (toolName === "unity_component") {
    const actionMap = {
      add: "unity.add_component",
      remove: "unity.remove_component",
      list: "unity.get_components",
      get_properties: "unity.get_component_properties"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid component action: ${args.action}`);
    rpcParams = { id: args.id };
    if (args.action === "add" || args.action === "remove" || args.action === "get_properties") {
      rpcParams.type = args.type;
      if (args.action === "remove") rpcParams.component = args.type;
    }
  } else if (toolName === "unity_file") {
    const actionMap = {
      read: "unity.read_file",
      write: "unity.write_file",
      exists: "unity.file_exists",
      list_dir: "unity.list_directory",
      create_dir: "unity.create_directory"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid file action: ${args.action}`);
    rpcParams = { path: args.path };
    if (args.action === "write") rpcParams.content = args.content;
    if (args.action === "list_dir") { rpcParams.recursive = args.recursive; rpcParams.filter = args.filter; }
  } else if (toolName === "unity_sprite") {
    const actionMap = {
      create: "unity.create_sprite_object",
      set_sprite: "unity.set_sprite",
      set_property: "unity.set_sprite_renderer_property",
      get_info: "unity.get_sprite_renderer_info"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid sprite action: ${args.action}`);
    if (args.action === "create") {
      rpcParams = { name: args.name, spritePath: args.spritePath, parentId: args.parentId, position: args.position, sortingLayer: args.sortingLayerName, sortingOrder: args.sortingOrder, color: args.color };
    } else if (args.action === "set_sprite") {
      rpcParams = { id: args.id, spritePath: args.spritePath };
    } else if (args.action === "set_property") {
      rpcParams = { id: args.id, color: args.color, flipX: args.flipX, flipY: args.flipY, sortingLayerName: args.sortingLayerName, sortingOrder: args.sortingOrder, drawMode: args.drawMode };
    } else {
      rpcParams = { id: args.id };
    }
  } else if (toolName === "unity_tilemap") {
    const actionMap = {
      create: "unity.create_tilemap",
      set_tile: "unity.set_tile",
      get_tile: "unity.get_tile",
      clear_tile: "unity.clear_tile",
      fill: "unity.fill_tiles",
      box_fill: "unity.box_fill_tiles",
      clear_all: "unity.clear_all_tiles",
      get_info: "unity.get_tilemap_info"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid tilemap action: ${args.action}`);
    rpcParams = { id: args.id };
    if (args.action === "create") {
      rpcParams = { name: args.name, createGrid: args.createGrid, cellLayout: args.cellLayout, cellSize: args.cellSize, sortingLayerName: args.sortingLayerName, sortingOrder: args.sortingOrder };
    } else if (args.action === "set_tile" || args.action === "get_tile" || args.action === "clear_tile") {
      rpcParams.x = args.x; rpcParams.y = args.y; rpcParams.z = args.z;
      if (args.action === "set_tile") rpcParams.tilePath = args.tilePath;
    } else if (args.action === "fill" || args.action === "box_fill") {
      rpcParams.tilePath = args.tilePath;
      rpcParams.startX = args.startX; rpcParams.startY = args.startY;
      rpcParams.endX = args.endX; rpcParams.endY = args.endY;
      rpcParams.z = args.z;
    }
  } else if (toolName === "unity_physics_2d_body") {
    const actionMap = {
      add_collider: "unity.add_2d_collider",
      add_rigidbody: "unity.add_rigidbody_2d",
      set_rigidbody: "unity.set_rigidbody_2d_property"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid physics 2d body action: ${args.action}`);
    rpcParams = { id: args.id };
    if (args.action === "add_collider") {
      rpcParams.type = args.colliderType;
      rpcParams.isTrigger = args.isTrigger;
      rpcParams.offset = args.offset;
      rpcParams.size = args.size;
      rpcParams.radius = args.radius;
    } else {
      rpcParams.bodyType = args.bodyType;
      rpcParams.mass = args.mass;
      rpcParams.linearDamping = args.linearDamping;
      rpcParams.angularDamping = args.angularDamping;
      rpcParams.gravityScale = args.gravityScale;
      rpcParams.freezeRotation = args.freezeRotation;
      rpcParams.simulated = args.simulated;
    }
  } else if (toolName === "unity_physics_2d_query") {
    const queryMap = {
      raycast: "unity.raycast_2d",
      overlap_circle: "unity.overlap_circle_2d",
      overlap_box: "unity.overlap_box_2d"
    };
    rpcMethod = queryMap[args.query];
    if (!rpcMethod) throw new Error(`Invalid physics 2d query: ${args.query}`);
    rpcParams = { layerMask: args.layerMask };
    if (args.query === "raycast") {
      rpcParams.origin = args.origin;
      rpcParams.direction = args.direction;
      rpcParams.distance = args.distance;
    } else if (args.query === "overlap_circle") {
      rpcParams.center = args.origin;
      rpcParams.radius = args.radius;
    } else {
      rpcParams.center = args.origin;
      rpcParams.size = args.size;
      rpcParams.angle = args.angle;
    }
  } else if (toolName === "unity_build") {
    const actionMap = {
      set_target: "unity.set_build_target",
      add_scene: "unity.add_scene_to_build",
      remove_scene: "unity.remove_scene_from_build",
      get_scenes: "unity.get_scenes_in_build",
      build: "unity.build_player"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid build action: ${args.action}`);
    rpcParams = {};
    if (args.action === "set_target") {
      rpcParams.target = args.target;
      rpcParams.targetGroup = args.targetGroup;
    } else if (args.action === "add_scene") {
      rpcParams.path = args.path;
      rpcParams.enabled = args.enabled;
    } else if (args.action === "remove_scene") {
      rpcParams.path = args.path;
      rpcParams.index = args.index;
    } else if (args.action === "build") {
      rpcParams.locationPath = args.locationPath;
      rpcParams.target = args.target;
      rpcParams.development = args.development;
    }
  } else if (toolName === "unity_package") {
    const actionMap = {
      get_info: "unity.get_package_info",
      add: "unity.add_package",
      remove: "unity.remove_package",
      search: "unity.search_packages"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid package action: ${args.action}`);
    rpcParams = {};
    if (args.action === "get_info" || args.action === "remove") {
      rpcParams.name = args.name;
    } else if (args.action === "add") {
      rpcParams.packageId = args.packageId;
    } else if (args.action === "search") {
      rpcParams.query = args.query;
    }
  } else {
    rpcMethod = toolName.replace("unity_", "unity.");
  }

  return { method: rpcMethod, rpcParams };
}

module.exports = { resolveToolCall };

