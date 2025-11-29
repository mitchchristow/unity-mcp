using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for searching and finding objects in the scene and project.
    /// </summary>
    public static class SearchController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.find_objects_by_name", FindByName);
            JsonRpcDispatcher.RegisterMethod("unity.find_objects_by_tag", FindByTag);
            JsonRpcDispatcher.RegisterMethod("unity.find_objects_by_component", FindByComponent);
            JsonRpcDispatcher.RegisterMethod("unity.find_objects_by_layer", FindByLayer);
            JsonRpcDispatcher.RegisterMethod("unity.search_assets", SearchAssets);
        }

        /// <summary>
        /// Find objects by name with optional wildcard support.
        /// Supports * wildcard (e.g., "Player*", "*Enemy*", "Cube*")
        /// </summary>
        private static JObject FindByName(JObject p)
        {
            string pattern = p["name"]?.ToString();
            if (string.IsNullOrEmpty(pattern))
                throw new System.Exception("Name pattern is required");

            bool includeInactive = p["includeInactive"]?.Value<bool>() ?? true;
            
            var results = new JArray();
            var allObjects = GetAllGameObjects(includeInactive);
            
            // Convert wildcard pattern to regex
            string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            foreach (var go in allObjects)
            {
                if (regex.IsMatch(go.name))
                {
                    results.Add(SerializeGameObject(go));
                }
            }

            return new JObject 
            { 
                ["objects"] = results,
                ["count"] = results.Count
            };
        }

        /// <summary>
        /// Find all objects with a specific tag.
        /// </summary>
        private static JObject FindByTag(JObject p)
        {
            string tag = p["tag"]?.ToString();
            if (string.IsNullOrEmpty(tag))
                throw new System.Exception("Tag is required");

            var results = new JArray();
            
            try
            {
                var objects = GameObject.FindGameObjectsWithTag(tag);
                foreach (var go in objects)
                {
                    results.Add(SerializeGameObject(go));
                }
            }
            catch (UnityException)
            {
                // Tag doesn't exist
                return new JObject 
                { 
                    ["objects"] = results,
                    ["count"] = 0,
                    ["error"] = $"Tag '{tag}' does not exist"
                };
            }

            return new JObject 
            { 
                ["objects"] = results,
                ["count"] = results.Count
            };
        }

        /// <summary>
        /// Find all objects that have a specific component type.
        /// </summary>
        private static JObject FindByComponent(JObject p)
        {
            string componentName = p["component"]?.ToString();
            if (string.IsNullOrEmpty(componentName))
                throw new System.Exception("Component name is required");

            bool includeInactive = p["includeInactive"]?.Value<bool>() ?? true;
            
            var results = new JArray();
            
            // Try to find the component type
            System.Type componentType = FindComponentType(componentName);
            if (componentType == null)
            {
                return new JObject 
                { 
                    ["objects"] = results,
                    ["count"] = 0,
                    ["error"] = $"Component type '{componentName}' not found"
                };
            }

            var components = Object.FindObjectsByType(componentType, includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var addedObjects = new HashSet<int>();
            
            foreach (var comp in components)
            {
                if (comp is Component c)
                {
                    int id = c.gameObject.GetInstanceID();
                    if (!addedObjects.Contains(id))
                    {
                        addedObjects.Add(id);
                        results.Add(SerializeGameObject(c.gameObject));
                    }
                }
            }

            return new JObject 
            { 
                ["objects"] = results,
                ["count"] = results.Count
            };
        }

        /// <summary>
        /// Find all objects on a specific layer.
        /// </summary>
        private static JObject FindByLayer(JObject p)
        {
            var layerParam = p["layer"];
            int layerIndex;
            
            if (layerParam.Type == JTokenType.Integer)
            {
                layerIndex = layerParam.Value<int>();
            }
            else
            {
                string layerName = layerParam?.ToString();
                if (string.IsNullOrEmpty(layerName))
                    throw new System.Exception("Layer name or index is required");
                    
                layerIndex = LayerMask.NameToLayer(layerName);
                if (layerIndex == -1)
                    throw new System.Exception($"Layer '{layerName}' not found");
            }

            bool includeInactive = p["includeInactive"]?.Value<bool>() ?? true;
            
            var results = new JArray();
            var allObjects = GetAllGameObjects(includeInactive);

            foreach (var go in allObjects)
            {
                if (go.layer == layerIndex)
                {
                    results.Add(SerializeGameObject(go));
                }
            }

            return new JObject 
            { 
                ["objects"] = results,
                ["count"] = results.Count,
                ["layerIndex"] = layerIndex,
                ["layerName"] = LayerMask.LayerToName(layerIndex)
            };
        }

        /// <summary>
        /// Search for assets in the project using Unity's search syntax.
        /// </summary>
        private static JObject SearchAssets(JObject p)
        {
            string query = p["query"]?.ToString() ?? "";
            string folder = p["folder"]?.ToString();
            int limit = p["limit"]?.Value<int>() ?? 50;
            
            var results = new JArray();
            
            string[] searchFolders = string.IsNullOrEmpty(folder) 
                ? new[] { "Assets" } 
                : new[] { folder };
            
            string[] guids = AssetDatabase.FindAssets(query, searchFolders);
            
            int count = 0;
            foreach (string guid in guids)
            {
                if (count >= limit) break;
                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                
                if (asset != null)
                {
                    results.Add(new JObject
                    {
                        ["path"] = path,
                        ["name"] = asset.name,
                        ["type"] = asset.GetType().Name,
                        ["guid"] = guid
                    });
                    count++;
                }
            }

            return new JObject 
            { 
                ["assets"] = results,
                ["count"] = count,
                ["totalFound"] = guids.Length,
                ["truncated"] = guids.Length > limit
            };
        }

        private static List<GameObject> GetAllGameObjects(bool includeInactive)
        {
            var results = new List<GameObject>();
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                
                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    CollectGameObjects(root, results, includeInactive);
                }
            }
            
            return results;
        }

        private static void CollectGameObjects(GameObject go, List<GameObject> results, bool includeInactive)
        {
            if (!includeInactive && !go.activeInHierarchy) return;
            
            results.Add(go);
            
            foreach (Transform child in go.transform)
            {
                CollectGameObjects(child.gameObject, results, includeInactive);
            }
        }

        private static System.Type FindComponentType(string name)
        {
            // Try common Unity namespaces first
            string[] prefixes = new[] 
            { 
                "", 
                "UnityEngine.", 
                "UnityEngine.UI.", 
                "TMPro.",
                "UnityEngine.Rendering."
            };

            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var prefix in prefixes)
                {
                    var type = assembly.GetType(prefix + name);
                    if (type != null && typeof(Component).IsAssignableFrom(type))
                        return type;
                }
            }
            
            return null;
        }

        private static JObject SerializeGameObject(GameObject go)
        {
            return new JObject
            {
                ["id"] = go.GetInstanceID(),
                ["name"] = go.name,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["layerName"] = LayerMask.LayerToName(go.layer),
                ["active"] = go.activeInHierarchy,
                ["position"] = new JObject
                {
                    ["x"] = go.transform.position.x,
                    ["y"] = go.transform.position.y,
                    ["z"] = go.transform.position.z
                }
            };
        }
    }
}

