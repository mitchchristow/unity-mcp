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

// Event buffer for storing recent WebSocket events
const MAX_EVENT_BUFFER = 100;
const eventBuffer = [];
let wsConnected = false;

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
  // === Selection Tools (consolidated) ===
  // unity_get_selection removed - use unity://selection resource
  {
    name: "unity_selection",
    description: "Manage selection: set (by IDs), clear, select_by_name, focus (frame in scene view)",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["set", "clear", "select_by_name", "focus"], description: "Selection action" },
        ids: { type: "array", items: { type: "integer" }, description: "Instance IDs for 'set' action" },
        name: { type: "string", description: "Name pattern for 'select_by_name' (e.g., 'Player', 'Enemy*')" },
        additive: { type: "boolean", description: "Add to selection for 'select_by_name'" },
      },
      required: ["action"],
    },
  },
  // === Menu Tools ===
  // unity_list_menu_items removed - use unity://menu/items resource
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
  // === Search Tools (consolidated) ===
  {
    name: "unity_find_objects",
    description: "Find objects by: name (wildcard *), tag, component type, or layer",
    inputSchema: {
      type: "object",
      properties: {
        by: { type: "string", enum: ["name", "tag", "component", "layer"], description: "Search criterion" },
        name: { type: "string", description: "Name pattern for 'name' search (e.g., 'Player', '*Enemy*')" },
        tag: { type: "string", description: "Tag for 'tag' search" },
        component: { type: "string", description: "Component type for 'component' search (e.g., 'Rigidbody')" },
        layer: { type: "string", description: "Layer for 'layer' search" },
        includeInactive: { type: "boolean", default: true },
      },
      required: ["by"],
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
  // === Component Tools (consolidated) ===
  {
    name: "unity_component",
    description: "Manage components: add, remove, list (get all), or get_properties",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        action: { type: "string", enum: ["add", "remove", "list", "get_properties"], description: "Component action" },
        type: { type: "string", description: "Component type (for add/remove/get_properties)" },
      },
      required: ["id", "action"],
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
  // === File Tools (consolidated) ===
  {
    name: "unity_file",
    description: "File operations: read, write, exists, list_dir, create_dir",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["read", "write", "exists", "list_dir", "create_dir"], description: "File action" },
        path: { type: "string", description: "File or directory path" },
        content: { type: "string", description: "File content (for write)" },
        recursive: { type: "boolean", description: "Recursive (for list_dir)" },
        filter: { type: "string", description: "File filter for list_dir (e.g., '*.cs')" },
      },
      required: ["action", "path"],
    },
  },
  // === Screenshot Tools (consolidated) ===
  {
    name: "unity_capture",
    description: "Capture screenshot from Game view (game) or Scene view (scene)",
    inputSchema: {
      type: "object",
      properties: {
        view: { type: "string", enum: ["game", "scene"], description: "Which view to capture" },
        filename: { type: "string", description: "Output filename" },
        superSize: { type: "integer", default: 1, description: "Resolution multiplier (game view only)" },
        width: { type: "integer", default: 1920, description: "Width (scene view only)" },
        height: { type: "integer", default: 1080, description: "Height (scene view only)" },
      },
      required: ["view"],
    },
  },
  // === Playmode Tools (consolidated) ===
  {
    name: "unity_playmode",
    description: "Control Unity play mode - play (enter), stop (exit), or pause (toggle)",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["play", "stop", "pause"], description: "Play mode action" },
      },
      required: ["action"],
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
  // unity_list_lights removed - use unity://lights resource
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
  // unity_list_cameras removed - use unity://cameras resource
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
  // unity_get_physics_settings removed - use unity://physics/settings resource
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
  // unity_list_tags removed - use unity://tags resource
  // unity_list_layers removed - use unity://layers resource
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
  // === Undo Tools (consolidated) ===
  {
    name: "unity_undo_action",
    description: "Manage undo/redo: undo, redo, get_history, clear (requires confirm), begin_group, end_group",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["undo", "redo", "get_history", "clear", "begin_group", "end_group"], description: "Undo action to perform" },
        name: { type: "string", description: "Name for begin_group action" },
        confirm: { type: "boolean", description: "Required true for clear action" },
      },
      required: ["action"],
    },
  },
  // === Animation Tools (Phase 3, consolidated) ===
  {
    name: "unity_animator",
    description: "Animator control: get_info, get_parameters, set_parameter, play_state",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        action: { type: "string", enum: ["get_info", "get_parameters", "set_parameter", "play_state"], description: "Animator action" },
        name: { type: "string", description: "Parameter name (for set_parameter)" },
        floatValue: { type: "number", description: "Float value (for set_parameter)" },
        intValue: { type: "integer", description: "Int value (for set_parameter)" },
        boolValue: { type: "boolean", description: "Bool value (for set_parameter)" },
        trigger: { type: "boolean", description: "Trigger (for set_parameter)" },
        stateName: { type: "string", description: "State name (for play_state)" },
        layer: { type: "integer", description: "Layer (for play_state, default: 0)" },
        normalizedTime: { type: "number", description: "Start time 0-1 (for play_state)" },
      },
      required: ["id", "action"],
    },
  },
  // unity_list_animation_clips removed - use unity://animation/clips resource
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
    name: "unity_audio_playback",
    description: "Control audio playback: play or stop (Play mode only for play)",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        action: { type: "string", enum: ["play", "stop"], description: "Playback action" },
      },
      required: ["id", "action"],
    },
  },
  // unity_list_audio_clips removed - use unity://audio/clips resource
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
  // unity_get_audio_settings removed - use unity://audio/settings resource
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
  // unity_list_ui_elements removed - use unity://ui/elements resource
  // === Build Tools (Phase 3) ===
  // unity_get_build_settings removed - use unity://build/settings resource
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
  // unity_get_build_target_list removed - use unity://build/targets resource
  // === Package Tools (Phase 3) ===
  // unity_list_packages removed - use unity://packages resource
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
  // === Terrain Tools (Phase 4) ===
  {
    name: "unity_create_terrain",
    description: "Create a new terrain",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        width: { type: "number", description: "Terrain width (default: 500)" },
        length: { type: "number", description: "Terrain length (default: 500)" },
        height: { type: "number", description: "Terrain height (default: 100)" },
        heightmapResolution: { type: "integer", description: "Heightmap resolution (default: 513)" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
      },
    },
  },
  {
    name: "unity_get_terrain_info",
    description: "Get information about a terrain",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the terrain GameObject" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_set_terrain_size",
    description: "Set the size of a terrain",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the terrain GameObject" },
        width: { type: "number" },
        length: { type: "number" },
        height: { type: "number" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_terrain_height",
    description: "Get or set terrain height at a point: action 'get' or 'set'",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the terrain GameObject" },
        action: { type: "string", enum: ["get", "set"], description: "Get or set height" },
        x: { type: "number", description: "X position" },
        z: { type: "number", description: "Z position" },
        height: { type: "number", description: "Target height (for set)" },
        radius: { type: "integer", description: "Brush radius (for set, default: 1)" },
      },
      required: ["id", "action"],
    },
  },
  // unity_list_terrains removed - use unity://terrains resource
  {
    name: "unity_set_terrain_layer",
    description: "Set a terrain layer (texture)",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the terrain GameObject" },
        layerIndex: { type: "integer", description: "Layer index (0-based)" },
        texturePath: { type: "string", description: "Path to texture asset" },
        tileSize: { type: "number", description: "Texture tile size (default: 10)" },
      },
      required: ["id", "texturePath"],
    },
  },
  {
    name: "unity_flatten_terrain",
    description: "Flatten the entire terrain to a specific height",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the terrain GameObject" },
        height: { type: "number", description: "Target height (default: 0)" },
      },
      required: ["id"],
    },
  },
  // === Particle System Tools (Phase 4) ===
  {
    name: "unity_create_particle_system",
    description: "Create a new particle system",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        parentId: { type: "integer" },
        duration: { type: "number" },
        startLifetime: { type: "number" },
        startSpeed: { type: "number" },
        startSize: { type: "number" },
        maxParticles: { type: "integer" },
        loop: { type: "boolean" },
        startColor: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } } },
      },
    },
  },
  {
    name: "unity_get_particle_system_info",
    description: "Get information about a particle system",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_set_particle_module",
    description: "Set particle system module: main, emission, or shape",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        module: { type: "string", enum: ["main", "emission", "shape"], description: "Which module to configure" },
        // Main module properties
        duration: { type: "number" },
        loop: { type: "boolean" },
        startLifetime: { type: "number" },
        startSpeed: { type: "number" },
        startSize: { type: "number" },
        maxParticles: { type: "integer" },
        gravityModifier: { type: "number" },
        simulationSpeed: { type: "number" },
        playOnAwake: { type: "boolean" },
        startColor: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } } },
        // Emission module properties
        enabled: { type: "boolean" },
        rateOverTime: { type: "number" },
        rateOverDistance: { type: "number" },
        // Shape module properties
        shapeType: { type: "string", enum: ["Sphere", "Hemisphere", "Cone", "Box", "Mesh", "Circle", "Edge"] },
        radius: { type: "number" },
        angle: { type: "number" },
        arc: { type: "number" },
      },
      required: ["id", "module"],
    },
  },
  {
    name: "unity_particle_playback",
    description: "Control particle system: play or stop",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        action: { type: "string", enum: ["play", "stop"], description: "Playback action" },
        withChildren: { type: "boolean", default: true },
        clear: { type: "boolean", description: "Clear particles on stop" },
      },
      required: ["id", "action"],
    },
  },
  // unity_list_particle_systems removed - use unity://particle-systems resource
  // === NavMesh Tools (Phase 4) ===
  {
    name: "unity_navmesh_build",
    description: "Build or clear the navigation mesh",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["bake", "clear"], description: "Bake or clear the NavMesh" },
      },
      required: ["action"],
    },
  },
  // unity_get_navmesh_settings removed - use unity://navmesh/settings resource
  {
    name: "unity_add_navmesh_agent",
    description: "Add a NavMeshAgent component to a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        speed: { type: "number" },
        angularSpeed: { type: "number" },
        acceleration: { type: "number" },
        stoppingDistance: { type: "number" },
        radius: { type: "number" },
        height: { type: "number" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_set_navmesh_agent",
    description: "Set properties on a NavMeshAgent",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        speed: { type: "number" },
        angularSpeed: { type: "number" },
        acceleration: { type: "number" },
        stoppingDistance: { type: "number" },
        radius: { type: "number" },
        height: { type: "number" },
        baseOffset: { type: "number" },
        autoTraverseOffMeshLink: { type: "boolean" },
        autoBraking: { type: "boolean" },
        autoRepath: { type: "boolean" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_get_navmesh_agent_info",
    description: "Get information about a NavMeshAgent",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
      },
      required: ["id"],
    },
  },
  {
    name: "unity_add_navmesh_obstacle",
    description: "Add a NavMeshObstacle component to a GameObject",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        carve: { type: "boolean" },
        shape: { type: "string", enum: ["Box", "Capsule"] },
        size: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
      },
      required: ["id"],
    },
  },
  // Note: NavMesh tools kept separate for clarity since they have different parameters
  {
    name: "unity_set_navmesh_destination",
    description: "Set destination for a NavMeshAgent (Play mode only)",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer", description: "Instance ID of the game object" },
        destination: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
      },
      required: ["id", "destination"],
    },
  },
  // unity_list_navmesh_agents removed - use unity://navmesh/agents resource
  {
    name: "unity_calculate_path",
    description: "Calculate a path between two points on the NavMesh",
    inputSchema: {
      type: "object",
      properties: {
        start: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "Start position" },
        end: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } }, description: "End position" },
      },
      required: ["start", "end"],
    },
  },
  // === Editor Window Tools (consolidated) ===
  // unity_list_windows removed - use unity://editor/windows resource
  {
    name: "unity_window",
    description: "Manage editor windows: open, close, focus, or get_info",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["open", "close", "focus", "get_info"], description: "Window action" },
        type: { type: "string", description: "Window type (Scene, Game, Hierarchy, Project, Inspector, Console, etc.)" },
        id: { type: "integer", description: "Window instance ID (for close/focus/get_info)" },
        utility: { type: "boolean", description: "Open as utility window" },
      },
      required: ["action"],
    },
  },
  {
    name: "unity_open_panel",
    description: "Open Inspector (for object), Project Settings, or Preferences panel",
    inputSchema: {
      type: "object",
      properties: {
        panel: { type: "string", enum: ["inspector", "project_settings", "preferences"], description: "Which panel to open" },
        objectId: { type: "integer", description: "Object to inspect (for inspector panel)" },
        path: { type: "string", description: "Settings/preferences path" },
      },
      required: ["panel"],
    },
  },
  // Scene Stats Tools removed - use resources instead:
  // unity://scene/stats, unity://scene/analysis, unity://memory/stats, unity://assets/stats
  
  // === 2D Game Development Tools ===
  // Consolidated sprite tool
  {
    name: "unity_sprite",
    description: "Sprite operations: create (new sprite object), set_sprite, set_property, get_info",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["create", "set_sprite", "set_property", "get_info"], description: "Sprite action" },
        id: { type: "integer", description: "Instance ID (for set_sprite, set_property, get_info)" },
        name: { type: "string", description: "Name for new sprite object (create)" },
        spritePath: { type: "string", description: "Path to sprite asset" },
        parentId: { type: "integer", description: "Parent object ID (create)" },
        position: { type: "object", properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } } },
        color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } } },
        flipX: { type: "boolean" },
        flipY: { type: "boolean" },
        sortingLayerName: { type: "string" },
        sortingOrder: { type: "integer" },
        drawMode: { type: "string", enum: ["Simple", "Sliced", "Tiled"] },
      },
      required: ["action"],
    },
  },
  // Consolidated tilemap tool
  {
    name: "unity_tilemap",
    description: "Tilemap operations: create, set_tile, get_tile, clear_tile, fill, box_fill, clear_all, get_info",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["create", "set_tile", "get_tile", "clear_tile", "fill", "box_fill", "clear_all", "get_info"], description: "Tilemap action" },
        id: { type: "integer", description: "Tilemap GameObject ID (for tile operations)" },
        name: { type: "string", description: "Name for new tilemap (create action)" },
        tilePath: { type: "string", description: "Path to tile asset" },
        x: { type: "integer", description: "Cell X position" },
        y: { type: "integer", description: "Cell Y position" },
        z: { type: "integer", description: "Cell Z position (default 0)" },
        startX: { type: "integer", description: "Start X for fill operations" },
        startY: { type: "integer", description: "Start Y for fill operations" },
        endX: { type: "integer", description: "End X for fill operations" },
        endY: { type: "integer", description: "End Y for fill operations" },
        createGrid: { type: "boolean", default: true, description: "Create parent Grid (for create action)" },
        cellLayout: { type: "string", enum: ["Rectangle", "Hexagon", "Isometric", "IsometricZAsY"], description: "Grid cell layout" },
        cellSize: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        sortingLayerName: { type: "string" },
        sortingOrder: { type: "integer" },
      },
      required: ["action"],
    },
  },
  // Consolidated 2D physics body tool
  {
    name: "unity_physics_2d_body",
    description: "2D physics body operations: add_collider, add_rigidbody, set_rigidbody",
    inputSchema: {
      type: "object",
      properties: {
        action: { type: "string", enum: ["add_collider", "add_rigidbody", "set_rigidbody"], description: "Physics body action" },
        id: { type: "integer", description: "Instance ID of the GameObject" },
        colliderType: { type: "string", enum: ["Box", "Circle", "Capsule", "Polygon", "Edge"], description: "For add_collider" },
        isTrigger: { type: "boolean" },
        offset: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        size: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        radius: { type: "number" },
        bodyType: { type: "string", enum: ["Dynamic", "Kinematic", "Static"] },
        mass: { type: "number" },
        linearDamping: { type: "number" },
        angularDamping: { type: "number" },
        gravityScale: { type: "number" },
        freezeRotation: { type: "boolean" },
        simulated: { type: "boolean" },
      },
      required: ["action", "id"],
    },
  },
  // 2D Physics queries
  {
    name: "unity_physics_2d_query",
    description: "2D physics queries: raycast, overlap_circle, overlap_box",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", enum: ["raycast", "overlap_circle", "overlap_box"], description: "Query type" },
        origin: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } }, description: "Ray origin or circle/box center" },
        direction: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } }, description: "For raycast" },
        distance: { type: "number", description: "For raycast" },
        radius: { type: "number", description: "For overlap_circle" },
        size: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } }, description: "For overlap_box" },
        angle: { type: "number", description: "Rotation for overlap_box" },
        layerMask: { type: "integer" },
      },
      required: ["query"],
    },
  },
  {
    name: "unity_set_physics_2d_property",
    description: "Set 2D physics settings (gravity, iterations, etc.)",
    inputSchema: {
      type: "object",
      properties: {
        gravity: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } } },
        velocityIterations: { type: "integer" },
        positionIterations: { type: "integer" },
        velocityThreshold: { type: "number" },
        queriesHitTriggers: { type: "boolean" },
        queriesStartInColliders: { type: "boolean" },
        autoSyncTransforms: { type: "boolean" },
      },
    },
  },
  {
    name: "unity_set_sorting_layer",
    description: "Set sorting layer and order on a renderer",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "integer" },
        layerName: { type: "string" },
        order: { type: "integer" },
      },
      required: ["id"],
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
  const args = request.params.arguments || {};

  // Map MCP tool names to Unity RPC methods
  let rpcMethod;
  let rpcParams = args;

  // Handle consolidated tools
  if (toolName === "unity_playmode") {
    // Map action to individual RPC methods
    const actionMap = { play: "unity.play", stop: "unity.stop", pause: "unity.pause" };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid playmode action: ${args.action}`);
    rpcParams = {};
  } else if (toolName === "unity_undo_action") {
    // Map action to individual RPC methods
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
    // Pass relevant params based on action
    if (args.action === "clear") rpcParams = { confirm: args.confirm };
    else if (args.action === "begin_group") rpcParams = { name: args.name };
    else rpcParams = {};
  } else if (toolName === "unity_selection") {
    // Map action to individual RPC methods
    const actionMap = {
      set: "unity.set_selection",
      clear: "unity.clear_selection",
      select_by_name: "unity.select_by_name",
      focus: "unity.focus_selection"
    };
    rpcMethod = actionMap[args.action];
    if (!rpcMethod) throw new Error(`Invalid selection action: ${args.action}`);
    // Pass relevant params based on action
    if (args.action === "set") rpcParams = { ids: args.ids };
    else if (args.action === "select_by_name") rpcParams = { name: args.name, additive: args.additive };
    else rpcParams = {};
  } else if (toolName === "unity_capture") {
    // Map view to RPC method
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
    // Copy relevant properties based on module
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
      if (args.action === "remove") rpcParams.component = args.type; // API uses 'component' param
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
    // Consolidated sprite tool
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
    // Consolidated tilemap tool
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
    // Consolidated 2D physics body tool
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
    // Consolidated 2D physics query tool
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
  } else {
    // Standard tool name to RPC method mapping
    rpcMethod = toolName.replace("unity_", "unity.");
  }

  try {
    const result = await callUnity(rpcMethod, rpcParams);
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
  // Phase 4 Resources
  {
    uri: "unity://terrains",
    name: "Terrains",
    description: "List of all terrains in the scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://particle-systems",
    name: "Particle Systems",
    description: "List of all particle systems in the scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://navmesh/agents",
    name: "NavMesh Agents",
    description: "List of all NavMesh agents in the scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://navmesh/settings",
    name: "NavMesh Settings",
    description: "Current NavMesh settings",
    mimeType: "application/json",
  },
  {
    uri: "unity://editor/windows",
    name: "Editor Windows",
    description: "List of all open editor windows",
    mimeType: "application/json",
  },
  {
    uri: "unity://scene/stats",
    name: "Scene Statistics",
    description: "Comprehensive scene statistics",
    mimeType: "application/json",
  },
  {
    uri: "unity://scene/analysis",
    name: "Scene Analysis",
    description: "Scene analysis with optimization suggestions",
    mimeType: "application/json",
  },
  {
    uri: "unity://memory/stats",
    name: "Memory Statistics",
    description: "Memory usage statistics",
    mimeType: "application/json",
  },
  {
    uri: "unity://assets/stats",
    name: "Asset Statistics",
    description: "Project asset counts and statistics",
    mimeType: "application/json",
  },
  // Real-time Events Resources
  {
    uri: "unity://events/recent",
    name: "Recent Events",
    description: "Recent Unity events captured via WebSocket (selection, playmode, console, etc.)",
    mimeType: "application/json",
  },
  {
    uri: "unity://events/types",
    name: "Event Types",
    description: "List of all available event types and their payloads",
    mimeType: "application/json",
  },
  {
    uri: "unity://events/status",
    name: "Event Stream Status",
    description: "WebSocket connection status and event buffer info",
    mimeType: "application/json",
  },
  // 2D Game Development Resources
  {
    uri: "unity://sprites",
    name: "Sprite Assets",
    description: "List of all sprite assets in the project",
    mimeType: "application/json",
  },
  {
    uri: "unity://tilemaps",
    name: "Tilemaps",
    description: "List of all tilemaps in the scene",
    mimeType: "application/json",
  },
  {
    uri: "unity://tiles",
    name: "Tile Assets",
    description: "List of all tile assets in the project",
    mimeType: "application/json",
  },
  {
    uri: "unity://2d/physics",
    name: "2D Physics Settings",
    description: "2D physics configuration (gravity, iterations, etc.)",
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
      // Phase 4 Resources
      case "unity://terrains":
        rpcMethod = "unity.list_terrains";
        break;
      case "unity://particle-systems":
        rpcMethod = "unity.list_particle_systems";
        break;
      case "unity://navmesh/agents":
        rpcMethod = "unity.list_navmesh_agents";
        break;
      case "unity://navmesh/settings":
        rpcMethod = "unity.get_navmesh_settings";
        break;
      case "unity://editor/windows":
        rpcMethod = "unity.list_windows";
        break;
      case "unity://scene/stats":
        rpcMethod = "unity.get_scene_stats";
        break;
      case "unity://scene/analysis":
        rpcMethod = "unity.analyze_scene";
        break;
      case "unity://memory/stats":
        rpcMethod = "unity.get_memory_stats";
        break;
      case "unity://assets/stats":
        rpcMethod = "unity.get_asset_stats";
        break;
      // Event resources are handled locally in the gateway
      case "unity://events/recent":
        // Return locally buffered events
        return {
          contents: [
            {
              uri: uri,
              mimeType: "application/json",
              text: JSON.stringify({
                events: getBufferedEvents(50),
                count: eventBuffer.length,
                maxBuffer: MAX_EVENT_BUFFER,
                wsConnected: wsConnected
              }, null, 2),
            },
          ],
        };
      case "unity://events/types":
        rpcMethod = "unity.get_event_types";
        break;
      case "unity://events/status":
        // Return local WebSocket status
        return {
          contents: [
            {
              uri: uri,
              mimeType: "application/json",
              text: JSON.stringify({
                connected: wsConnected,
                url: UNITY_WS_URL,
                bufferedEvents: eventBuffer.length,
                maxBuffer: MAX_EVENT_BUFFER,
                oldestEvent: eventBuffer.length > 0 ? eventBuffer[0].timestamp : null,
                newestEvent: eventBuffer.length > 0 ? eventBuffer[eventBuffer.length - 1].timestamp : null
              }, null, 2),
            },
          ],
        };
      // 2D Resources
      case "unity://sprites":
        rpcMethod = "unity.list_sprites";
        break;
      case "unity://tilemaps":
        rpcMethod = "unity.list_tilemaps";
        break;
      case "unity://tiles":
        rpcMethod = "unity.list_tiles";
        break;
      case "unity://2d/physics":
        rpcMethod = "unity.get_physics_2d_settings";
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

// WebSocket Event Listener - Captures Unity events in real-time
let wsInstance = null;

function connectWebSocket() {
  if (wsInstance && wsInstance.readyState === WebSocket.OPEN) {
    return; // Already connected
  }

  const ws = new WebSocket(UNITY_WS_URL);
  wsInstance = ws;

  ws.on("open", () => {
    wsConnected = true;
    console.error("[MCP Gateway] Connected to Unity WebSocket events");
  });

  ws.on("message", (data) => {
    try {
      const event = JSON.parse(data.toString());
      
      // Add to event buffer
      eventBuffer.push({
        ...event,
        receivedAt: new Date().toISOString()
      });
      
      // Keep buffer size limited
      while (eventBuffer.length > MAX_EVENT_BUFFER) {
        eventBuffer.shift();
      }

      // Log important events (can be disabled in production)
      if (event.event === "console.log" && event.data?.type === "error") {
        console.error(`[Unity Error] ${event.data.message}`);
      } else if (event.event === "playmode.changed") {
        console.error(`[Unity] Play mode: ${event.data.state}`);
      } else if (event.event === "scripts.compilation_started") {
        console.error("[Unity] Script compilation started...");
      } else if (event.event === "scripts.compilation_finished") {
        console.error("[Unity] Script compilation finished");
      }
      
      // In the future, we can forward these as MCP Notifications
      // server.sendNotification({ method: "unity/event", params: event });
    } catch (err) {
      // Ignore parse errors
    }
  });

  ws.on("error", (err) => {
    wsConnected = false;
    // Don't log connection refused errors during reconnect attempts
    if (err.code !== "ECONNREFUSED") {
      console.error("[MCP Gateway] WebSocket error:", err.message);
    }
  });

  ws.on("close", () => {
    wsConnected = false;
    wsInstance = null;
    // Reconnect after delay
    setTimeout(connectWebSocket, 5000);
  });
}

// Helper to get buffered events
function getBufferedEvents(limit = 50, eventType = null) {
  let events = eventBuffer;
  
  // Filter by event type if specified
  if (eventType) {
    events = events.filter(e => e.event === eventType || e.event?.startsWith(eventType + "."));
  }
  
  // Return most recent events up to limit
  return events.slice(-limit);
}

// Helper to clear event buffer
function clearEventBuffer() {
  eventBuffer.length = 0;
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
