using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for executing Unity Editor menu items.
    /// Allows AI to trigger any menu command programmatically.
    /// </summary>
    public static class MenuController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.execute_menu", ExecuteMenu);
            JsonRpcDispatcher.RegisterMethod("unity.list_menu_items", ListMenuItems);
        }

        /// <summary>
        /// Executes a Unity menu item by path.
        /// Example: "GameObject/3D Object/Cube", "Edit/Play", "Assets/Create/Folder"
        /// </summary>
        private static JObject ExecuteMenu(JObject p)
        {
            string menuPath = p["path"]?.ToString();
            if (string.IsNullOrEmpty(menuPath))
                throw new System.Exception("Menu path is required");

            bool success = EditorApplication.ExecuteMenuItem(menuPath);
            
            if (!success)
                throw new System.Exception($"Menu item not found or failed to execute: {menuPath}");

            return new JObject 
            { 
                ["ok"] = true,
                ["executed"] = menuPath
            };
        }

        /// <summary>
        /// Lists commonly used menu items for reference.
        /// Note: Unity doesn't expose a full menu item list, so this returns known useful items.
        /// </summary>
        private static JObject ListMenuItems(JObject p)
        {
            string category = p["category"]?.ToString()?.ToLower();
            
            var menuItems = new JArray();
            
            var allItems = GetCommonMenuItems();
            
            foreach (var item in allItems)
            {
                if (string.IsNullOrEmpty(category) || item.Key.ToLower().StartsWith(category))
                {
                    menuItems.Add(new JObject
                    {
                        ["path"] = item.Key,
                        ["description"] = item.Value
                    });
                }
            }

            return new JObject { ["menuItems"] = menuItems };
        }

        private static Dictionary<string, string> GetCommonMenuItems()
        {
            return new Dictionary<string, string>
            {
                // File menu
                ["File/New Scene"] = "Create a new empty scene",
                ["File/Save"] = "Save the current scene",
                ["File/Save As..."] = "Save scene with a new name",
                ["File/Build Settings..."] = "Open build settings window",
                
                // Edit menu
                ["Edit/Undo"] = "Undo last action",
                ["Edit/Redo"] = "Redo last undone action",
                ["Edit/Play"] = "Enter play mode",
                ["Edit/Pause"] = "Pause play mode",
                ["Edit/Step"] = "Step one frame in play mode",
                ["Edit/Select All"] = "Select all objects",
                ["Edit/Deselect All"] = "Deselect all objects",
                ["Edit/Project Settings..."] = "Open project settings",
                
                // GameObject menu
                ["GameObject/Create Empty"] = "Create an empty GameObject",
                ["GameObject/Create Empty Child"] = "Create empty child of selected",
                ["GameObject/3D Object/Cube"] = "Create a cube primitive",
                ["GameObject/3D Object/Sphere"] = "Create a sphere primitive",
                ["GameObject/3D Object/Capsule"] = "Create a capsule primitive",
                ["GameObject/3D Object/Cylinder"] = "Create a cylinder primitive",
                ["GameObject/3D Object/Plane"] = "Create a plane primitive",
                ["GameObject/3D Object/Quad"] = "Create a quad primitive",
                ["GameObject/Light/Directional Light"] = "Create a directional light",
                ["GameObject/Light/Point Light"] = "Create a point light",
                ["GameObject/Light/Spot Light"] = "Create a spot light",
                ["GameObject/Light/Area Light"] = "Create an area light",
                ["GameObject/Camera"] = "Create a camera",
                ["GameObject/UI/Canvas"] = "Create a UI canvas",
                ["GameObject/UI/Panel"] = "Create a UI panel",
                ["GameObject/UI/Button"] = "Create a UI button",
                ["GameObject/UI/Text - TextMeshPro"] = "Create TextMeshPro text",
                ["GameObject/UI/Image"] = "Create a UI image",
                ["GameObject/Audio/Audio Source"] = "Create an audio source",
                
                // Component menu
                ["Component/Physics/Rigidbody"] = "Add Rigidbody component",
                ["Component/Physics/Box Collider"] = "Add Box Collider component",
                ["Component/Physics/Sphere Collider"] = "Add Sphere Collider",
                ["Component/Physics/Capsule Collider"] = "Add Capsule Collider",
                ["Component/Physics/Mesh Collider"] = "Add Mesh Collider",
                ["Component/Audio/Audio Source"] = "Add Audio Source component",
                ["Component/Audio/Audio Listener"] = "Add Audio Listener",
                ["Component/Rendering/Camera"] = "Add Camera component",
                ["Component/Rendering/Light"] = "Add Light component",
                
                // Assets menu
                ["Assets/Create/Folder"] = "Create a new folder",
                ["Assets/Create/C# Script"] = "Create a new C# script",
                ["Assets/Create/Material"] = "Create a new material",
                ["Assets/Create/Prefab Variant"] = "Create a prefab variant",
                ["Assets/Create/Scene"] = "Create a new scene",
                ["Assets/Create/Shader/Standard Surface Shader"] = "Create a surface shader",
                ["Assets/Create/Shader/Unlit Shader"] = "Create an unlit shader",
                ["Assets/Refresh"] = "Refresh the asset database",
                
                // Window menu
                ["Window/General/Scene"] = "Open Scene view",
                ["Window/General/Game"] = "Open Game view",
                ["Window/General/Inspector"] = "Open Inspector",
                ["Window/General/Hierarchy"] = "Open Hierarchy",
                ["Window/General/Project"] = "Open Project window",
                ["Window/General/Console"] = "Open Console",
                ["Window/Package Manager"] = "Open Package Manager",
                ["Window/Asset Store"] = "Open Asset Store",
            };
        }
    }
}

