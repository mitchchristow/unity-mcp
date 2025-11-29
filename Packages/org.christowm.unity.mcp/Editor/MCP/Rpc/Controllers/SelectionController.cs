using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for enhanced selection management in the Unity Editor.
    /// </summary>
    public static class SelectionController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.set_selection", SetSelection);
            JsonRpcDispatcher.RegisterMethod("unity.add_to_selection", AddToSelection);
            JsonRpcDispatcher.RegisterMethod("unity.remove_from_selection", RemoveFromSelection);
            JsonRpcDispatcher.RegisterMethod("unity.clear_selection", ClearSelection);
            JsonRpcDispatcher.RegisterMethod("unity.select_by_name", SelectByName);
            JsonRpcDispatcher.RegisterMethod("unity.select_children", SelectChildren);
            JsonRpcDispatcher.RegisterMethod("unity.select_parent", SelectParent);
            JsonRpcDispatcher.RegisterMethod("unity.focus_selection", FocusSelection);
        }

        /// <summary>
        /// Sets the selection to specific objects by their instance IDs.
        /// </summary>
        private static JObject SetSelection(JObject p)
        {
            var idsToken = p["ids"];
            if (idsToken == null)
                throw new System.Exception("Object IDs are required");

            var objects = new List<Object>();
            
            if (idsToken.Type == JTokenType.Array)
            {
                foreach (var id in idsToken)
                {
                    var obj = EditorUtility.InstanceIDToObject(id.Value<int>());
                    if (obj != null) objects.Add(obj);
                }
            }
            else
            {
                var obj = EditorUtility.InstanceIDToObject(idsToken.Value<int>());
                if (obj != null) objects.Add(obj);
            }

            Selection.objects = objects.ToArray();

            return new JObject
            {
                ["ok"] = true,
                ["selectedCount"] = objects.Count
            };
        }

        /// <summary>
        /// Adds objects to the current selection.
        /// </summary>
        private static JObject AddToSelection(JObject p)
        {
            var idsToken = p["ids"];
            if (idsToken == null)
                throw new System.Exception("Object IDs are required");

            var currentSelection = new List<Object>(Selection.objects);
            
            if (idsToken.Type == JTokenType.Array)
            {
                foreach (var id in idsToken)
                {
                    var obj = EditorUtility.InstanceIDToObject(id.Value<int>());
                    if (obj != null && !currentSelection.Contains(obj))
                        currentSelection.Add(obj);
                }
            }
            else
            {
                var obj = EditorUtility.InstanceIDToObject(idsToken.Value<int>());
                if (obj != null && !currentSelection.Contains(obj))
                    currentSelection.Add(obj);
            }

            Selection.objects = currentSelection.ToArray();

            return new JObject
            {
                ["ok"] = true,
                ["selectedCount"] = currentSelection.Count
            };
        }

        /// <summary>
        /// Removes objects from the current selection.
        /// </summary>
        private static JObject RemoveFromSelection(JObject p)
        {
            var idsToken = p["ids"];
            if (idsToken == null)
                throw new System.Exception("Object IDs are required");

            var idsToRemove = new HashSet<int>();
            
            if (idsToken.Type == JTokenType.Array)
            {
                foreach (var id in idsToken)
                {
                    idsToRemove.Add(id.Value<int>());
                }
            }
            else
            {
                idsToRemove.Add(idsToken.Value<int>());
            }

            var newSelection = Selection.objects
                .Where(obj => !idsToRemove.Contains(obj.GetInstanceID()))
                .ToArray();

            Selection.objects = newSelection;

            return new JObject
            {
                ["ok"] = true,
                ["selectedCount"] = newSelection.Length
            };
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        private static JObject ClearSelection(JObject p)
        {
            Selection.objects = new Object[0];

            return new JObject
            {
                ["ok"] = true
            };
        }

        /// <summary>
        /// Selects objects by name (supports wildcards).
        /// </summary>
        private static JObject SelectByName(JObject p)
        {
            string name = p["name"]?.ToString();
            if (string.IsNullOrEmpty(name))
                throw new System.Exception("Name is required");

            bool additive = p["additive"]?.Value<bool>() ?? false;
            
            // Use SearchController's find functionality
            var findParams = new JObject { ["name"] = name, ["includeInactive"] = true };
            
            // Find all matching objects
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var matches = new List<Object>();
            
            string pattern = "^" + System.Text.RegularExpressions.Regex.Escape(name).Replace("\\*", ".*") + "$";
            var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var go in allObjects)
            {
                if (regex.IsMatch(go.name))
                {
                    matches.Add(go);
                }
            }

            if (additive)
            {
                var currentSelection = new List<Object>(Selection.objects);
                currentSelection.AddRange(matches.Where(m => !currentSelection.Contains(m)));
                Selection.objects = currentSelection.ToArray();
            }
            else
            {
                Selection.objects = matches.ToArray();
            }

            return new JObject
            {
                ["ok"] = true,
                ["selectedCount"] = Selection.objects.Length,
                ["matchedCount"] = matches.Count
            };
        }

        /// <summary>
        /// Selects all children of the currently selected objects.
        /// </summary>
        private static JObject SelectChildren(JObject p)
        {
            bool includeParent = p["includeParent"]?.Value<bool>() ?? true;
            bool recursive = p["recursive"]?.Value<bool>() ?? true;
            
            var children = new List<Object>();
            
            foreach (var obj in Selection.gameObjects)
            {
                if (includeParent)
                    children.Add(obj);
                    
                CollectChildren(obj.transform, children, recursive);
            }

            Selection.objects = children.ToArray();

            return new JObject
            {
                ["ok"] = true,
                ["selectedCount"] = children.Count
            };
        }

        private static void CollectChildren(Transform parent, List<Object> results, bool recursive)
        {
            foreach (Transform child in parent)
            {
                results.Add(child.gameObject);
                if (recursive)
                {
                    CollectChildren(child, results, true);
                }
            }
        }

        /// <summary>
        /// Selects the parent of each currently selected object.
        /// </summary>
        private static JObject SelectParent(JObject p)
        {
            var parents = new HashSet<Object>();
            
            foreach (var obj in Selection.gameObjects)
            {
                if (obj.transform.parent != null)
                {
                    parents.Add(obj.transform.parent.gameObject);
                }
            }

            Selection.objects = parents.ToArray();

            return new JObject
            {
                ["ok"] = true,
                ["selectedCount"] = parents.Count
            };
        }

        /// <summary>
        /// Focuses the Scene view on the current selection.
        /// </summary>
        private static JObject FocusSelection(JObject p)
        {
            if (Selection.activeGameObject == null)
            {
                return new JObject
                {
                    ["ok"] = false,
                    ["error"] = "No object selected"
                };
            }

            SceneView.lastActiveSceneView?.FrameSelected();

            return new JObject
            {
                ["ok"] = true
            };
        }
    }
}

