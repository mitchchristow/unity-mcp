using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing tags and layers in Unity.
    /// </summary>
    public static class TagLayerController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.list_tags", ListTags);
            JsonRpcDispatcher.RegisterMethod("unity.list_layers", ListLayers);
            JsonRpcDispatcher.RegisterMethod("unity.set_object_tag", SetObjectTag);
            JsonRpcDispatcher.RegisterMethod("unity.set_object_layer", SetObjectLayer);
            JsonRpcDispatcher.RegisterMethod("unity.create_tag", CreateTag);
            JsonRpcDispatcher.RegisterMethod("unity.get_sorting_layers", GetSortingLayers);
        }

        /// <summary>
        /// Lists all available tags.
        /// </summary>
        private static JObject ListTags(JObject p)
        {
            var tags = new JArray();
            foreach (var tag in InternalEditorUtility.tags)
            {
                tags.Add(tag);
            }

            return new JObject
            {
                ["tags"] = tags,
                ["count"] = tags.Count
            };
        }

        /// <summary>
        /// Lists all available layers.
        /// </summary>
        private static JObject ListLayers(JObject p)
        {
            var layers = new JArray();
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(new JObject
                    {
                        ["index"] = i,
                        ["name"] = layerName
                    });
                }
            }

            return new JObject
            {
                ["layers"] = layers,
                ["count"] = layers.Count
            };
        }

        /// <summary>
        /// Sets the tag of a GameObject.
        /// </summary>
        private static JObject SetObjectTag(JObject p)
        {
            int id = p["id"].Value<int>();
            string tag = p["tag"]?.ToString();

            if (string.IsNullOrEmpty(tag))
                throw new System.Exception("Tag is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            // Verify tag exists
            bool tagExists = false;
            foreach (var existingTag in InternalEditorUtility.tags)
            {
                if (existingTag == tag)
                {
                    tagExists = true;
                    break;
                }
            }

            if (!tagExists)
                throw new System.Exception($"Tag '{tag}' does not exist. Create it first or use an existing tag.");

            Undo.RecordObject(go, "Set Tag via MCP");
            go.tag = tag;

            return new JObject
            {
                ["ok"] = true,
                ["gameObject"] = go.name,
                ["tag"] = tag
            };
        }

        /// <summary>
        /// Sets the layer of a GameObject.
        /// </summary>
        private static JObject SetObjectLayer(JObject p)
        {
            int id = p["id"].Value<int>();
            var layerParam = p["layer"];
            bool includeChildren = p["includeChildren"]?.Value<bool>() ?? false;

            if (layerParam == null)
                throw new System.Exception("Layer is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            int layer;
            if (layerParam.Type == JTokenType.Integer)
            {
                layer = layerParam.Value<int>();
            }
            else
            {
                string layerName = layerParam.ToString();
                layer = LayerMask.NameToLayer(layerName);
                if (layer == -1)
                    throw new System.Exception($"Layer '{layerName}' not found");
            }

            Undo.RecordObject(go, "Set Layer via MCP");
            go.layer = layer;

            if (includeChildren)
            {
                SetLayerRecursive(go.transform, layer);
            }

            return new JObject
            {
                ["ok"] = true,
                ["gameObject"] = go.name,
                ["layer"] = layer,
                ["layerName"] = LayerMask.LayerToName(layer)
            };
        }

        private static void SetLayerRecursive(Transform parent, int layer)
        {
            foreach (Transform child in parent)
            {
                Undo.RecordObject(child.gameObject, "Set Layer via MCP");
                child.gameObject.layer = layer;
                SetLayerRecursive(child, layer);
            }
        }

        /// <summary>
        /// Creates a new tag.
        /// Note: This modifies the TagManager asset.
        /// </summary>
        private static JObject CreateTag(JObject p)
        {
            string tagName = p["name"]?.ToString();

            if (string.IsNullOrEmpty(tagName))
                throw new System.Exception("Tag name is required");

            // Check if tag already exists
            foreach (var existingTag in InternalEditorUtility.tags)
            {
                if (existingTag == tagName)
                {
                    return new JObject
                    {
                        ["ok"] = true,
                        ["tag"] = tagName,
                        ["alreadyExists"] = true
                    };
                }
            }

            // Add the tag using SerializedObject
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Find an empty slot or add new
            int index = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(index);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(index);
            newTag.stringValue = tagName;

            tagManager.ApplyModifiedProperties();

            return new JObject
            {
                ["ok"] = true,
                ["tag"] = tagName,
                ["created"] = true
            };
        }

        /// <summary>
        /// Gets all sorting layers (for 2D rendering).
        /// </summary>
        private static JObject GetSortingLayers(JObject p)
        {
            var layers = new JArray();
            foreach (var layer in SortingLayer.layers)
            {
                layers.Add(new JObject
                {
                    ["id"] = layer.id,
                    ["name"] = layer.name,
                    ["value"] = layer.value
                });
            }

            return new JObject
            {
                ["sortingLayers"] = layers,
                ["count"] = layers.Count
            };
        }
    }
}

