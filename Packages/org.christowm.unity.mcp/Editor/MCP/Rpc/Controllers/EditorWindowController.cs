using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing Unity Editor windows.
    /// </summary>
    public static class EditorWindowController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.open_window", OpenWindow);
            JsonRpcDispatcher.RegisterMethod("unity.close_window", CloseWindow);
            JsonRpcDispatcher.RegisterMethod("unity.list_windows", ListWindows);
            JsonRpcDispatcher.RegisterMethod("unity.focus_window", FocusWindow);
            JsonRpcDispatcher.RegisterMethod("unity.get_window_info", GetWindowInfo);
            JsonRpcDispatcher.RegisterMethod("unity.open_inspector", OpenInspector);
            JsonRpcDispatcher.RegisterMethod("unity.open_project_settings", OpenProjectSettings);
            JsonRpcDispatcher.RegisterMethod("unity.open_preferences", OpenPreferences);
        }

        /// <summary>
        /// Opens a Unity Editor window by type name.
        /// </summary>
        private static JObject OpenWindow(JObject p)
        {
            string windowType = p["type"]?.ToString();
            bool utility = p["utility"]?.Value<bool>() ?? false;

            if (string.IsNullOrEmpty(windowType))
                throw new System.Exception("Window type is required");

            EditorWindow window = null;

            // Handle common window types
            switch (windowType.ToLower())
            {
                case "scene":
                case "sceneview":
                    window = EditorWindow.GetWindow<SceneView>("Scene", !utility);
                    break;
                case "game":
                case "gameview":
                    var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                    if (gameViewType != null)
                        window = EditorWindow.GetWindow(gameViewType, utility, "Game");
                    break;
                case "hierarchy":
                    var hierarchyType = System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
                    if (hierarchyType != null)
                        window = EditorWindow.GetWindow(hierarchyType, utility, "Hierarchy");
                    break;
                case "project":
                    var projectType = System.Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
                    if (projectType != null)
                        window = EditorWindow.GetWindow(projectType, utility, "Project");
                    break;
                case "inspector":
                    var inspectorType = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
                    if (inspectorType != null)
                        window = EditorWindow.GetWindow(inspectorType, utility, "Inspector");
                    break;
                case "console":
                    var consoleType = System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
                    if (consoleType != null)
                        window = EditorWindow.GetWindow(consoleType, utility, "Console");
                    break;
                case "animation":
                    var animationType = System.Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
                    if (animationType != null)
                        window = EditorWindow.GetWindow(animationType, utility, "Animation");
                    break;
                case "animator":
                    var animatorType = System.Type.GetType("UnityEditor.Graphs.AnimatorControllerTool,UnityEditor.Graphs");
                    if (animatorType != null)
                        window = EditorWindow.GetWindow(animatorType, utility, "Animator");
                    break;
                case "profiler":
                    var profilerType = System.Type.GetType("UnityEditor.ProfilerWindow,UnityEditor");
                    if (profilerType != null)
                        window = EditorWindow.GetWindow(profilerType, utility, "Profiler");
                    break;
                case "audiomixer":
                    var audioMixerType = System.Type.GetType("UnityEditor.AudioMixerWindow,UnityEditor");
                    if (audioMixerType != null)
                        window = EditorWindow.GetWindow(audioMixerType, utility, "Audio Mixer");
                    break;
                case "lighting":
                    var lightingType = System.Type.GetType("UnityEditor.LightingWindow,UnityEditor");
                    if (lightingType != null)
                        window = EditorWindow.GetWindow(lightingType, utility, "Lighting");
                    break;
                case "occlusion":
                    var occlusionType = System.Type.GetType("UnityEditor.OcclusionCullingWindow,UnityEditor");
                    if (occlusionType != null)
                        window = EditorWindow.GetWindow(occlusionType, utility, "Occlusion");
                    break;
                case "navigation":
                    var navigationWindowType = System.Type.GetType("UnityEditor.NavMeshEditorWindow,UnityEditor");
                    if (navigationWindowType != null)
                        window = EditorWindow.GetWindow(navigationWindowType, utility, "Navigation");
                    break;
                default:
                    // Try to find by full type name
                    var customType = System.Type.GetType(windowType);
                    if (customType != null && typeof(EditorWindow).IsAssignableFrom(customType))
                    {
                        window = EditorWindow.GetWindow(customType, utility);
                    }
                    break;
            }

            if (window == null)
                throw new System.Exception($"Could not open window of type: {windowType}");

            window.Show();
            window.Focus();

            return new JObject
            {
                ["ok"] = true,
                ["windowId"] = window.GetInstanceID(),
                ["title"] = window.titleContent.text
            };
        }

        /// <summary>
        /// Closes a Unity Editor window.
        /// </summary>
        private static JObject CloseWindow(JObject p)
        {
            int? windowId = p["id"]?.Value<int>();
            string windowType = p["type"]?.ToString();

            EditorWindow window = null;

            if (windowId.HasValue)
            {
                window = EditorUtility.InstanceIDToObject(windowId.Value) as EditorWindow;
            }
            else if (!string.IsNullOrEmpty(windowType))
            {
                // Find window by type
                var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                window = windows.FirstOrDefault(w => 
                    w.GetType().Name.ToLower().Contains(windowType.ToLower()) ||
                    w.titleContent.text.ToLower().Contains(windowType.ToLower()));
            }

            if (window == null)
                throw new System.Exception("Window not found");

            window.Close();

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Lists all open Editor windows.
        /// </summary>
        private static JObject ListWindows(JObject p)
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var result = new JArray();

            foreach (var window in windows)
            {
                // Skip hidden windows
                if (window.GetType().Name.StartsWith("__"))
                    continue;

                result.Add(new JObject
                {
                    ["id"] = window.GetInstanceID(),
                    ["type"] = window.GetType().Name,
                    ["title"] = window.titleContent.text,
                    ["position"] = new JObject
                    {
                        ["x"] = window.position.x,
                        ["y"] = window.position.y,
                        ["width"] = window.position.width,
                        ["height"] = window.position.height
                    },
                    ["hasFocus"] = window.hasFocus,
                    ["maximized"] = window.maximized
                });
            }

            return new JObject
            {
                ["windows"] = result,
                ["count"] = result.Count
            };
        }

        /// <summary>
        /// Focuses an Editor window.
        /// </summary>
        private static JObject FocusWindow(JObject p)
        {
            int? windowId = p["id"]?.Value<int>();
            string windowType = p["type"]?.ToString();

            EditorWindow window = null;

            if (windowId.HasValue)
            {
                window = EditorUtility.InstanceIDToObject(windowId.Value) as EditorWindow;
            }
            else if (!string.IsNullOrEmpty(windowType))
            {
                var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                window = windows.FirstOrDefault(w => 
                    w.GetType().Name.ToLower().Contains(windowType.ToLower()) ||
                    w.titleContent.text.ToLower().Contains(windowType.ToLower()));
            }

            if (window == null)
                throw new System.Exception("Window not found");

            window.Focus();

            return new JObject
            {
                ["ok"] = true,
                ["title"] = window.titleContent.text
            };
        }

        /// <summary>
        /// Gets information about an Editor window.
        /// </summary>
        private static JObject GetWindowInfo(JObject p)
        {
            int id = p["id"].Value<int>();

            var window = EditorUtility.InstanceIDToObject(id) as EditorWindow;
            if (window == null)
                throw new System.Exception("Window not found");

            return new JObject
            {
                ["id"] = window.GetInstanceID(),
                ["type"] = window.GetType().FullName,
                ["title"] = window.titleContent.text,
                ["position"] = new JObject
                {
                    ["x"] = window.position.x,
                    ["y"] = window.position.y,
                    ["width"] = window.position.width,
                    ["height"] = window.position.height
                },
                ["hasFocus"] = window.hasFocus,
                ["maximized"] = window.maximized,
                ["docked"] = window.docked
            };
        }

        /// <summary>
        /// Opens the Inspector window for a specific object.
        /// </summary>
        private static JObject OpenInspector(JObject p)
        {
            int? objectId = p["objectId"]?.Value<int>();

            var inspectorType = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
            if (inspectorType == null)
                throw new System.Exception("Could not find Inspector window type");

            var window = EditorWindow.GetWindow(inspectorType, false, "Inspector");
            window.Show();
            window.Focus();

            // Select the object if ID provided
            if (objectId.HasValue)
            {
                var obj = EditorUtility.InstanceIDToObject(objectId.Value);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                }
            }

            return new JObject
            {
                ["ok"] = true,
                ["windowId"] = window.GetInstanceID()
            };
        }

        /// <summary>
        /// Opens a specific Project Settings panel.
        /// </summary>
        private static JObject OpenProjectSettings(JObject p)
        {
            string settingsPath = p["path"]?.ToString() ?? "Project/Player";

            SettingsService.OpenProjectSettings(settingsPath);

            return new JObject
            {
                ["ok"] = true,
                ["path"] = settingsPath
            };
        }

        /// <summary>
        /// Opens the Preferences window.
        /// </summary>
        private static JObject OpenPreferences(JObject p)
        {
            string preferencePath = p["path"]?.ToString();

            if (!string.IsNullOrEmpty(preferencePath))
            {
                SettingsService.OpenUserPreferences(preferencePath);
            }
            else
            {
                SettingsService.OpenUserPreferences();
            }

            return new JObject { ["ok"] = true };
        }
    }
}

