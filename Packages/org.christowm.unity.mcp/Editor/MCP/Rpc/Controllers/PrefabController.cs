using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class PrefabController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.instantiate_prefab", InstantiatePrefab);
            JsonRpcDispatcher.RegisterMethod("unity.create_prefab", CreatePrefab);
        }

        private static JObject InstantiatePrefab(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path)) throw new System.Exception("Path is required");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new System.Exception($"Prefab not found at path: {path}");

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab via MCP");

            if (p["position"] != null)
            {
                instance.transform.position = new Vector3(
                    p["position"]["x"]?.Value<float>() ?? 0,
                    p["position"]["y"]?.Value<float>() ?? 0,
                    p["position"]["z"]?.Value<float>() ?? 0
                );
            }

            if (p["rotation"] != null)
            {
                instance.transform.eulerAngles = new Vector3(
                    p["rotation"]["x"]?.Value<float>() ?? 0,
                    p["rotation"]["y"]?.Value<float>() ?? 0,
                    p["rotation"]["z"]?.Value<float>() ?? 0
                );
            }

            if (p["parent"] != null)
            {
                var parent = McpObjectReference.ToGameObject(p["parent"]) as GameObject;
                if (parent != null)
                {
                    instance.transform.SetParent(parent.transform);
                }
            }

            return new JObject { ["id"] = McpObjectReference.ToJToken(instance) };
        }

        private static JObject CreatePrefab(JObject p)
        {
            var idToken = p["id"];
            string path = p["path"]?.ToString();
            
            if (string.IsNullOrEmpty(path)) throw new System.Exception("Path is required");
            if (!path.EndsWith(".prefab")) path += ".prefab";

            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null) throw new System.Exception("Object not found");

            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.UserAction);
            
            return new JObject { ["ok"] = true, ["path"] = path };
        }
    }
}
