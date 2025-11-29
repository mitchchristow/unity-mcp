#!/usr/bin/env node

const { Server } = require("@modelcontextprotocol/sdk/server/index.js");
const { StdioServerTransport } = require("@modelcontextprotocol/sdk/server/stdio.js");
const {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
} = require("@modelcontextprotocol/sdk/types.js");
const axios = require("axios");
const WebSocket = require("ws");

// Configuration
const UNITY_RPC_URL = "http://localhost:17890/mcp/rpc";
const UNITY_WS_URL = "ws://localhost:17891/mcp/events";

// Initialize MCP Server
const server = new Server(
  {
    name: "unity-mcp-gateway",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
      resources: {},
    },
  }
);

// Helper to call Unity RPC
async function callUnity(method, params) {
  try {
    const response = await axios.post(UNITY_RPC_URL, {
      jsonrpc: "2.0",
      method: method,
      params: params,
      id: Date.now(),
    });

    if (response.data.error) {
      throw new Error(response.data.error.message || "Unknown Unity Error");
    }

    return response.data.result;
  } catch (error) {
    if (error.code === "ECONNREFUSED") {
      throw new Error("Unity Editor is not running or MCP server is not started.");
    }
    throw error;
  }
}

// Define Tools
const TOOLS = [
  // === Hierarchy Tools ===
  {
    name: "unity_list_objects",
    description: "List all game objects in the scene (or children of a parent)",
    inputSchema: {
      type: "object",
      properties: {
        parentId: { type: "integer", description: "Optional parent InstanceID" },
      },
    },
  },
  {
    name: "unity_create_object",
    description: "Create a new empty Game Object",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        parentId: { type: "integer" },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_create_primitive",
    description: "Create a primitive object (Cube, Sphere, etc.)",
    inputSchema: {
      type: "object",
      properties: {
        type: { type: "string", enum: ["Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad"] },
        name: { type: "string" },
        parentId: { type: "integer" },
      },
      required: ["type"],
    },
  },
  {
    name: "unity_set_transform",
    description: "Set position, rotation, and scale of an object",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        scale: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_delete_object",
    description: "Delete a game object by ID",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the object to delete" },
      },
      required: ["id"],
    },
  },
  // === Material Tools ===
  {
    name: "unity_set_material",
    description: "Apply a material asset to an object's renderer",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        path: { type: "string", description: "Path to material asset (e.g., Assets/Mat.mat)" },
      },
      required: ["id", "path"],
    },
  },
  {
    name: "unity_create_material",
    description: "Create a new material asset",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        shader: { type: "string", default: "Standard" },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_set_material_property",
    description: "Set properties (color, float, texture) on a material asset",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string" },
        color: { type: "object", properties: { name: { type: "string" }, r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } } },
        float: { type: "object", properties: { name: { type: "string" }, value: { type: "number" } } },
      },
      required: ["path"],
    },
  },
  // === Prefab Tools ===
  {
    name: "unity_instantiate_prefab",
    description: "Instantiate a prefab from assets",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        parent: { type: "integer" },
      },
      required: ["path"],
    },
  },
  {
    name: "unity_create_prefab",
    description: "Save a game object as a prefab",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        path: { type: "string" },
      },
      required: ["id", "path"],
    },
  },
  // === Selection Tools ===
  {
    name: "unity_get_selection",
    description: "Get currently selected objects in Unity Editor",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_set_selection",
    description: "Set selection to specific objects by instance IDs",
    inputSchema: {
      type: "object",
      properties: {
        ids: { type: "array", items: { type: "integer" }, description: "Array of instance IDs to select" },
      },
      required: ["ids"],
    },
  },
  {
    name: "unity_clear_selection",
    description: "Clear the current selection",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_select_by_name",
    description: "Select objects by name (supports * wildcard)",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name pattern (e.g., 'Player', 'Enemy*', '*Light*')" },
        additive: { type: "boolean", description: "Add to current selection instead of replacing" },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_focus_selection",
    description: "Focus the Scene view camera on the current selection",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === Menu Tools ===
  {
    name: "unity_execute_menu",
    description: "Execute a Unity menu item by path (e.g., 'GameObject/3D Object/Cube')",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Menu path (e.g., 'Edit/Play', 'GameObject/Create Empty')" },
      },
      required: ["path"],
    },
  },
  {
    name: "unity_list_menu_items",
    description: "List common Unity menu items",
    inputSchema: {
      type: "object",
      properties: {
        category: { type: "string", description: "Filter by category (e.g., 'GameObject', 'Edit', 'Assets')" },
      },
    },
  },
  // === Search Tools ===
  {
    name: "unity_find_objects_by_name",
    description: "Find objects by name (supports * wildcard)",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name pattern (e.g., 'Player', '*Enemy*')" },
        includeInactive: { type: "boolean", default: true },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_find_objects_by_tag",
    description: "Find all objects with a specific tag",
    inputSchema: {
      type: "object",
      properties: {
        tag: { type: "string", description: "Tag name (e.g., 'Player', 'Enemy')" },
      },
      required: ["tag"],
    },
  },
  {
    name: "unity_find_objects_by_component",
    description: "Find all objects that have a specific component",
    inputSchema: {
      type: "object",
      properties: {
        component: { type: "string", description: "Component type name (e.g., 'Rigidbody', 'Camera', 'Light')" },
        includeInactive: { type: "boolean", default: true },
      },
      required: ["component"],
    },
  },
  {
    name: "unity_find_objects_by_layer",
    description: "Find all objects on a specific layer",
    inputSchema: {
      type: "object",
      properties: {
        layer: { type: "string", description: "Layer name or index" },
        includeInactive: { type: "boolean", default: true },
      },
      required: ["layer"],
    },
  },
  {
    name: "unity_search_assets",
    description: "Search for assets in the project",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", description: "Search query (e.g., 't:Material', 't:Prefab', 'Player')" },
        folder: { type: "string", description: "Folder to search in (default: Assets)" },
        limit: { type: "integer", default: 50 },
      },
    },
  },
  // === Component Tools ===
  {
    name: "unity_add_component",
    description: "Add a component to a game object",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        type: { type: "string", description: "Component type (e.g., 'Rigidbody', 'BoxCollider')" },
      },
      required: ["id", "type"],
    },
  },
  {
    name: "unity_get_components",
    description: "Get all components on a game object",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_get_component_properties",
    description: "Get all properties of a specific component",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        component: { type: "string", description: "Component type name" },
      },
      required: ["id", "component"],
    },
  },
  {
    name: "unity_remove_component",
    description: "Remove a component from a game object",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        component: { type: "string", description: "Component type name to remove" },
      },
      required: ["id", "component"],
    },
  },
  {
    name: "unity_get_object_details",
    description: "Get comprehensive details about a game object including all components",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        includeChildren: { type: "boolean", default: false },
      },
      required: ["id"],
    },
  },
  // === File Tools ===
  {
    name: "unity_read_file",
    description: "Read contents of a file in the project",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "File path (e.g., 'Assets/Scripts/Player.cs')" },
      },
      required: ["path"],
    },
  },
  {
    name: "unity_write_file",
    description: "Write content to a file in the project",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "File path (e.g., 'Assets/Scripts/Enemy.cs')" },
        content: { type: "string", description: "File content" },
      },
      required: ["path", "content"],
    },
  },
  {
    name: "unity_file_exists",
    description: "Check if a file or directory exists",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string" },
      },
      required: ["path"],
    },
  },
  {
    name: "unity_list_directory",
    description: "List contents of a directory",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", default: "Assets" },
        recursive: { type: "boolean", default: false },
        filter: { type: "string", description: "File filter (e.g., '*.cs', '*.prefab')" },
      },
    },
  },
  {
    name: "unity_create_directory",
    description: "Create a new directory",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Directory path to create" },
      },
      required: ["path"],
    },
  },
  // === Screenshot Tools ===
  {
    name: "unity_capture_screenshot",
    description: "Capture a screenshot from the Game view",
    inputSchema: {
      type: "object",
      properties: {
        filename: { type: "string", description: "Output filename (default: screenshot_timestamp.png)" },
        superSize: { type: "integer", default: 1, description: "Resolution multiplier" },
      },
    },
  },
  {
    name: "unity_capture_scene_view",
    description: "Capture an image from the Scene view camera",
    inputSchema: {
      type: "object",
      properties: {
        filename: { type: "string", description: "Output filename" },
        width: { type: "integer", default: 1920 },
        height: { type: "integer", default: 1080 },
      },
    },
  },
  // === Playmode Tools ===
  {
    name: "unity_play",
    description: "Enter play mode",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_stop",
    description: "Exit play mode",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_pause",
    description: "Toggle pause in play mode",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === Lighting Tools (Phase 2) ===
  {
    name: "unity_create_light",
    description: "Create a new light (Directional, Point, Spot, Area)",
    inputSchema: {
      type: "object",
      properties: {
        type: { type: "string", enum: ["Directional", "Point", "Spot", "Area"], description: "Light type" },
        name: { type: "string" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        intensity: { type: "number" },
        color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
        range: { type: "number" },
        spotAngle: { type: "number" },
      },
    },
  },
  {
    name: "unity_set_light_property",
    description: "Set properties on an existing light",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        intensity: { type: "number" },
        color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
        range: { type: "number" },
        spotAngle: { type: "number" },
        shadows: { type: "string", enum: ["None", "Hard", "Soft"] },
        enabled: { type: "boolean" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_get_lighting_settings",
    description: "Get current lighting and render settings",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_set_ambient_light",
    description: "Set ambient lighting properties",
    inputSchema: {
      type: "object",
      properties: {
        mode: { type: "string", enum: ["Skybox", "Trilight", "Flat"] },
        color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
        intensity: { type: "number" },
        skyColor: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
        equatorColor: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
        groundColor: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
      },
    },
  },
  {
    name: "unity_list_lights",
    description: "List all lights in the scene",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === Camera Tools (Phase 2) ===
  {
    name: "unity_create_camera",
    description: "Create a new camera",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        fieldOfView: { type: "number" },
        orthographic: { type: "boolean" },
        orthographicSize: { type: "number" },
        nearClip: { type: "number" },
        farClip: { type: "number" },
      },
    },
  },
  {
    name: "unity_set_camera_property",
    description: "Set properties on an existing camera",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        fieldOfView: { type: "number" },
        orthographic: { type: "boolean" },
        orthographicSize: { type: "number" },
        nearClip: { type: "number" },
        farClip: { type: "number" },
        clearFlags: { type: "string", enum: ["Skybox", "SolidColor", "Depth", "Nothing"] },
        backgroundColor: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } } },
        depth: { type: "number" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_get_camera_info",
    description: "Get detailed information about a camera",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_list_cameras",
    description: "List all cameras in the scene",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_get_scene_view_camera",
    description: "Get the Scene view camera position and settings",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_set_scene_view_camera",
    description: "Set the Scene view camera position and settings",
    inputSchema: {
      type: "object",
      properties: {
        pivot: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        rotation: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        size: { type: "number", description: "Zoom level" },
        orthographic: { type: "boolean" },
        lookAt: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Point to look at" },
      },
    },
  },
  // === Physics Tools (Phase 2) ===
  {
    name: "unity_get_physics_settings",
    description: "Get current physics settings",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_set_gravity",
    description: "Set the gravity vector",
    inputSchema: {
      type: "object",
      properties: {
        x: { type: "number" },
        y: { type: "number" },
        z: { type: "number" },
      },
    },
  },
  {
    name: "unity_set_physics_property",
    description: "Set various physics properties",
    inputSchema: {
      type: "object",
      properties: {
        defaultSolverIterations: { type: "integer" },
        bounceThreshold: { type: "number" },
        sleepThreshold: { type: "number" },
        queriesHitTriggers: { type: "boolean" },
        autoSyncTransforms: { type: "boolean" },
      },
    },
  },
  {
    name: "unity_raycast",
    description: "Perform a physics raycast",
    inputSchema: {
      type: "object",
      properties: {
        origin: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Ray origin" },
        direction: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Ray direction" },
        maxDistance: { type: "number" },
        layerMask: { type: "integer" },
      },
      required: ["origin", "direction"],
    },
  },
  {
    name: "unity_get_layer_collision_matrix",
    description: "Get the layer collision matrix",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_set_layer_collision",
    description: "Set whether two layers should collide",
    inputSchema: {
      type: "object",
      properties: {
        layer1: { type: "string", description: "Layer name or index" },
        layer2: { type: "string", description: "Layer name or index" },
        ignore: { type: "boolean", description: "True to ignore collisions" },
      },
      required: ["layer1", "layer2"],
    },
  },
  // === Tag/Layer Tools (Phase 2) ===
  {
    name: "unity_list_tags",
    description: "List all available tags",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_list_layers",
    description: "List all available layers",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_set_object_tag",
    description: "Set the tag of a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        tag: { type: "string" },
      },
      required: ["id", "tag"],
    },
  },
  {
    name: "unity_set_object_layer",
    description: "Set the layer of a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        layer: { type: "string", description: "Layer name or index" },
        includeChildren: { type: "boolean", default: false },
      },
      required: ["id", "layer"],
    },
  },
  {
    name: "unity_create_tag",
    description: "Create a new tag",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_get_sorting_layers",
    description: "Get all sorting layers (for 2D)",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === Undo Tools (Phase 2) ===
  {
    name: "unity_undo",
    description: "Perform an undo operation",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_redo",
    description: "Perform a redo operation",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_get_undo_history",
    description: "Get information about the undo history",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_clear_undo",
    description: "Clear all undo history (requires confirm: true)",
    inputSchema: {
      type: "object",
      properties: {
        confirm: { type: "boolean", description: "Must be true to confirm clearing" },
      },
      required: ["confirm"],
    },
  },
  {
    name: "unity_begin_undo_group",
    description: "Begin a new undo group",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name for the undo group" },
      },
    },
  },
  {
    name: "unity_end_undo_group",
    description: "End the current undo group",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === Animation Tools (Phase 3) ===
  {
    name: "unity_get_animator_info",
    description: "Get information about an Animator component on a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_set_animator_parameter",
    description: "Set a parameter on an Animator",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        name: { type: "string", description: "Parameter name" },
        floatValue: { type: "number" },
        intValue: { type: "integer" },
        boolValue: { type: "boolean" },
        trigger: { type: "boolean", description: "Set to true to trigger" },
      },
      required: ["id", "name"],
    },
  },
  {
    name: "unity_get_animator_parameters",
    description: "Get all parameters from an Animator",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_play_animator_state",
    description: "Play a specific animator state",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        stateName: { type: "string", description: "Name of the state to play" },
        layer: { type: "integer", description: "Animator layer (default: 0)" },
        normalizedTime: { type: "number", description: "Start time (0-1)" },
      },
      required: ["id", "stateName"],
    },
  },
  {
    name: "unity_list_animation_clips",
    description: "List all animation clips in the project",
    inputSchema: {
      type: "object",
      properties: {
        folder: { type: "string", description: "Folder to search (default: Assets)" },
        limit: { type: "integer", default: 50 },
      },
    },
  },
  {
    name: "unity_get_animation_clip_info",
    description: "Get detailed information about an animation clip",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Asset path to the animation clip" },
      },
      required: ["path"],
    },
  },
  // === Audio Tools (Phase 3) ===
  {
    name: "unity_create_audio_source",
    description: "Create an AudioSource component on a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        volume: { type: "number" },
        pitch: { type: "number" },
        loop: { type: "boolean" },
        playOnAwake: { type: "boolean" },
        spatialBlend: { type: "number", description: "0 = 2D, 1 = 3D" },
        clipPath: { type: "string", description: "Path to audio clip asset" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_set_audio_source_property",
    description: "Set properties on an AudioSource",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        volume: { type: "number" },
        pitch: { type: "number" },
        loop: { type: "boolean" },
        playOnAwake: { type: "boolean" },
        spatialBlend: { type: "number" },
        mute: { type: "boolean" },
        priority: { type: "integer" },
        minDistance: { type: "number" },
        maxDistance: { type: "number" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_get_audio_source_info",
    description: "Get information about an AudioSource",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_play_audio",
    description: "Play audio on an AudioSource (only works in Play mode)",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_stop_audio",
    description: "Stop audio on an AudioSource",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_list_audio_clips",
    description: "List all audio clips in the project",
    inputSchema: {
      type: "object",
      properties: {
        folder: { type: "string", description: "Folder to search (default: Assets)" },
        limit: { type: "integer", default: 50 },
      },
    },
  },
  {
    name: "unity_set_audio_clip",
    description: "Set the audio clip on an AudioSource",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        clipPath: { type: "string", description: "Path to audio clip asset" },
      },
      required: ["id", "clipPath"],
    },
  },
  {
    name: "unity_get_audio_settings",
    description: "Get global audio settings",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === UI Tools (Phase 3) ===
  {
    name: "unity_create_canvas",
    description: "Create a new Canvas with EventSystem",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        renderMode: { type: "string", enum: ["ScreenSpaceOverlay", "ScreenSpaceCamera", "WorldSpace"] },
      },
    },
  },
  {
    name: "unity_create_ui_element",
    description: "Create a UI element (Button, Text, Image, Panel, etc.)",
    inputSchema: {
      type: "object",
      properties: {
        type: { type: "string", enum: ["Panel", "Button", "Text", "Image", "RawImage", "InputField", "Slider", "Toggle", "Dropdown", "ScrollView"] },
        parentId: { type: "integer", description: "Parent Canvas or UI element ID" },
        name: { type: "string" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        size: { type: "object", properties: { width: { type: "number" }, height: { type: "number" } } },
      },
      required: ["type"],
    },
  },
  {
    name: "unity_set_ui_text",
    description: "Set text on a UI Text component",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        text: { type: "string" },
        fontSize: { type: "integer" },
        color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } } },
        alignment: { type: "string", enum: ["UpperLeft", "UpperCenter", "UpperRight", "MiddleLeft", "MiddleCenter", "MiddleRight", "LowerLeft", "LowerCenter", "LowerRight"] },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_set_ui_image",
    description: "Set properties on a UI Image component",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } } },
        spritePath: { type: "string", description: "Path to sprite asset" },
        fillAmount: { type: "number" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_set_rect_transform",
    description: "Set RectTransform properties on a UI element",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        anchoredPosition: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        sizeDelta: { type: "object", properties: { width: { type: "number" }, height: { type: "number" } } },
        anchorMin: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        anchorMax: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        pivot: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_get_ui_info",
    description: "Get UI information about a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_list_ui_elements",
    description: "List all UI elements in the scene",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === Build Tools (Phase 3) ===
  {
    name: "unity_get_build_settings",
    description: "Get the current build settings",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_set_build_target",
    description: "Set the active build target",
    inputSchema: {
      type: "object",
      properties: {
        target: { type: "string", description: "Build target (e.g., StandaloneWindows64, Android, iOS)" },
        targetGroup: { type: "string", description: "Build target group (optional)" },
      },
      required: ["target"],
    },
  },
  {
    name: "unity_add_scene_to_build",
    description: "Add a scene to the build settings",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Scene path (e.g., Assets/Scenes/Level1.unity)" },
        enabled: { type: "boolean", default: true },
      },
      required: ["path"],
    },
  },
  {
    name: "unity_remove_scene_from_build",
    description: "Remove a scene from the build settings",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Scene path" },
        index: { type: "integer", description: "Scene index (alternative to path)" },
      },
    },
  },
  {
    name: "unity_get_scenes_in_build",
    description: "Get all scenes in the build settings",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_build_player",
    description: "Build the player",
    inputSchema: {
      type: "object",
      properties: {
        locationPath: { type: "string", description: "Output path for the build" },
        target: { type: "string", description: "Build target (optional, uses current if not specified)" },
        development: { type: "boolean", default: false },
      },
      required: ["locationPath"],
    },
  },
  {
    name: "unity_get_build_target_list",
    description: "Get a list of available build targets",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  // === Package Tools (Phase 3) ===
  {
    name: "unity_list_packages",
    description: "List all installed packages",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_get_package_info",
    description: "Get detailed information about a specific package",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Package name (e.g., com.unity.inputsystem)" },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_add_package",
    description: "Add a package to the project",
    inputSchema: {
      type: "object",
      properties: {
        packageId: { type: "string", description: "Package ID (e.g., 'com.unity.inputsystem' or 'com.unity.inputsystem@1.0.0')" },
      },
      required: ["packageId"],
    },
  },
  {
    name: "unity_remove_package",
    description: "Remove a package from the project",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Package name" },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_search_packages",
    description: "Search for packages in the Unity registry",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", description: "Search query (leave empty to list all)" },
      },
    },
  },
];

// List Tools Handler
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: TOOLS,
  };
});

// Call Tool Handler
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const toolName = request.params.name;
  const args = request.params.arguments;

  // Map MCP tool names to Unity RPC methods
  // Convention: unity_tool_name -> unity.tool_name (mostly)
  // Some might need manual mapping if names diverge.
  
  let rpcMethod = toolName.replace("unity_", "unity.");
  
  // Specific overrides if needed
  if (toolName === "unity_play_mode") rpcMethod = "unity.play"; // Example if we had a divergence

  try {
    const result = await callUnity(rpcMethod, args);
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify(result, null, 2),
        },
      ],
    };
  } catch (error) {
    return {
      content: [
        {
          type: "text",
          text: `Error: ${error.message}`,
        },
      ],
      isError: true,
    };
  }
});

// Define Resources
const RESOURCES = [
  {
    uri: "unity://project/info",
    name: "Project Info",
    description: "Unity project information including version, platform, and play state",
    mimeType: "application/json",
  },
  {
    uri: "unity://scene/hierarchy",
    name: "Scene Hierarchy",
    description: "Complete hierarchy of all GameObjects in the current scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://scene/list",
    name: "Scene List",
    description: "List of all loaded scenes",
    mimeType: "application/json",
  },
  {
    uri: "unity://selection",
    name: "Current Selection",
    description: "Currently selected GameObjects in the Unity Editor",
    mimeType: "application/json",
  },
  {
    uri: "unity://assets",
    name: "Project Assets",
    description: "List of assets in the project (materials, prefabs, scripts, etc.)",
    mimeType: "application/json",
  },
  {
    uri: "unity://console/logs",
    name: "Console Logs",
    description: "Recent Unity console log entries",
    mimeType: "application/json",
  },
  {
    uri: "unity://menu/items",
    name: "Menu Items",
    description: "List of common Unity menu items that can be executed",
    mimeType: "application/json",
  },
  {
    uri: "unity://files/scripts",
    name: "Script Files",
    description: "List of C# script files in the Assets folder",
    mimeType: "application/json",
  },
  {
    uri: "unity://lights",
    name: "Scene Lights",
    description: "List of all lights in the current scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://cameras",
    name: "Scene Cameras",
    description: "List of all cameras in the current scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://physics/settings",
    name: "Physics Settings",
    description: "Current physics configuration",
    mimeType: "application/json",
  },
  {
    uri: "unity://tags",
    name: "Tags",
    description: "List of all available tags",
    mimeType: "application/json",
  },
  {
    uri: "unity://layers",
    name: "Layers",
    description: "List of all available layers",
    mimeType: "application/json",
  },
  // Phase 3 Resources
  {
    uri: "unity://animation/clips",
    name: "Animation Clips",
    description: "List of all animation clips in the project",
    mimeType: "application/json",
  },
  {
    uri: "unity://audio/clips",
    name: "Audio Clips",
    description: "List of all audio clips in the project",
    mimeType: "application/json",
  },
  {
    uri: "unity://audio/settings",
    name: "Audio Settings",
    description: "Global audio settings",
    mimeType: "application/json",
  },
  {
    uri: "unity://ui/elements",
    name: "UI Elements",
    description: "List of all UI elements in the scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://build/settings",
    name: "Build Settings",
    description: "Current build settings and scenes",
    mimeType: "application/json",
  },
  {
    uri: "unity://build/targets",
    name: "Build Targets",
    description: "Available build targets",
    mimeType: "application/json",
  },
  {
    uri: "unity://packages",
    name: "Installed Packages",
    description: "List of all installed packages",
    mimeType: "application/json",
  },
];

// List Resources Handler
server.setRequestHandler(ListResourcesRequestSchema, async () => {
  return {
    resources: RESOURCES,
  };
});

// Read Resource Handler
server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const uri = request.params.uri;

  try {
    let result;
    let rpcMethod;
    let params = {};

    // Map resource URIs to Unity RPC methods
    switch (uri) {
      case "unity://project/info":
        rpcMethod = "unity.get_project_info";
        break;
      case "unity://scene/hierarchy":
        rpcMethod = "unity.list_objects";
        break;
      case "unity://scene/list":
        rpcMethod = "unity.list_scenes";
        break;
      case "unity://selection":
        rpcMethod = "unity.get_selection";
        break;
      case "unity://assets":
        rpcMethod = "unity.list_assets";
        break;
      case "unity://console/logs":
        rpcMethod = "unity.get_console_logs";
        break;
      case "unity://menu/items":
        rpcMethod = "unity.list_menu_items";
        break;
      case "unity://files/scripts":
        rpcMethod = "unity.list_directory";
        params = { path: "Assets", filter: "*.cs", recursive: true };
        break;
      case "unity://lights":
        rpcMethod = "unity.list_lights";
        break;
      case "unity://cameras":
        rpcMethod = "unity.list_cameras";
        break;
      case "unity://physics/settings":
        rpcMethod = "unity.get_physics_settings";
        break;
      case "unity://tags":
        rpcMethod = "unity.list_tags";
        break;
      case "unity://layers":
        rpcMethod = "unity.list_layers";
        break;
      // Phase 3 Resources
      case "unity://animation/clips":
        rpcMethod = "unity.list_animation_clips";
        break;
      case "unity://audio/clips":
        rpcMethod = "unity.list_audio_clips";
        break;
      case "unity://audio/settings":
        rpcMethod = "unity.get_audio_settings";
        break;
      case "unity://ui/elements":
        rpcMethod = "unity.list_ui_elements";
        break;
      case "unity://build/settings":
        rpcMethod = "unity.get_build_settings";
        break;
      case "unity://build/targets":
        rpcMethod = "unity.get_build_target_list";
        break;
      case "unity://packages":
        rpcMethod = "unity.list_packages";
        break;
      default:
        throw new Error(`Unknown resource: ${uri}`);
    }

    result = await callUnity(rpcMethod, params);

    return {
      contents: [
        {
          uri: uri,
          mimeType: "application/json",
          text: JSON.stringify(result, null, 2),
        },
      ],
    };
  } catch (error) {
    throw new Error(`Failed to read resource ${uri}: ${error.message}`);
  }
});

// WebSocket Event Listener (Optional: Forward logs to MCP logging if supported)
// For now, we just keep the connection alive to show we can.
function connectWebSocket() {
  const ws = new WebSocket(UNITY_WS_URL);

  ws.on("open", () => {
    // console.error("Connected to Unity Events");
  });

  ws.on("message", (data) => {
    // In the future, we can forward these as MCP Notifications
    // server.sendNotification(...)
  });

  ws.on("error", (err) => {
    // console.error("WebSocket error:", err.message);
  });

  ws.on("close", () => {
    setTimeout(connectWebSocket, 5000);
  });
}

connectWebSocket();

// Start Server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  // console.error("Unity MCP Gateway running on Stdio");
}

main().catch((error) => {
  console.error("Fatal Error:", error);
  process.exit(1);
});
