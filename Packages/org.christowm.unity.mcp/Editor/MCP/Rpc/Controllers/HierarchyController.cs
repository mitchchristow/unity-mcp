using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class HierarchyController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.list_objects", ListObjects);
            JsonRpcDispatcher.RegisterMethod("unity.create_object", CreateObject);
            JsonRpcDispatcher.RegisterMethod("unity.create_primitive", CreatePrimitive);
            JsonRpcDispatcher.RegisterMethod("unity.delete_object", DeleteObject);
            JsonRpcDispatcher.RegisterMethod("unity.set_transform", SetTransform);
            JsonRpcDispatcher.RegisterMethod("unity.get_transform", GetTransform);
        }

        private static JObject ListObjects(JObject p)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var list = new JArray();

            foreach (var go in roots)
            {
                list.Add(SerializeGameObject(go));
            }

            return new JObject { ["objects"] = list };
        }

        private static JObject SerializeGameObject(GameObject go)
        {
            var obj = new JObject
            {
                ["id"] = go.GetInstanceID(),
                ["name"] = go.name,
                ["children"] = new JArray()
            };

            foreach (Transform child in go.transform)
            {
                ((JArray)obj["children"]).Add(SerializeGameObject(child.gameObject));
            }

            return obj;
        }

        private static JObject CreateObject(JObject p)
        {
            string name = p["name"]?.ToString() ?? "New Object";
            GameObject go = new GameObject(name);
            
            if (p["parentId"] != null)
            {
                int parentId = p["parentId"].Value<int>();
                var parentGo = EditorUtility.InstanceIDToObject(parentId) as GameObject;
                if (parentGo != null)
                {
                    go.transform.SetParent(parentGo.transform);
                }
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Object via MCP");
            return new JObject { ["id"] = go.GetInstanceID() };
        }

        private static JObject CreatePrimitive(JObject p)
        {
            string typeName = p["type"].ToString();
            if (!System.Enum.TryParse(typeName, true, out PrimitiveType primitiveType))
            {
                throw new System.Exception($"Unknown primitive type: {typeName}");
            }

            GameObject go = GameObject.CreatePrimitive(primitiveType);
            
            if (p["name"] != null) go.name = p["name"].ToString();
            
            if (p["parentId"] != null)
            {
                int parentId = p["parentId"].Value<int>();
                var parentGo = EditorUtility.InstanceIDToObject(parentId) as GameObject;
                if (parentGo != null)
                {
                    go.transform.SetParent(parentGo.transform);
                }
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Primitive via MCP");
            return new JObject { ["id"] = go.GetInstanceID() };
        }

        private static JObject DeleteObject(JObject p)
        {
            int id = p["id"].Value<int>();
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
                return new JObject { ["ok"] = true };
            }
            throw new System.Exception("Object not found");
        }

        private static JObject SetTransform(JObject p)
        {
            int id = p["id"].Value<int>();
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null) throw new System.Exception("Object not found");

            Undo.RecordObject(go.transform, "Set Transform via MCP");

            if (p["position"] != null)
            {
                go.transform.position = new Vector3(
                    p["position"]["x"]?.Value<float>() ?? go.transform.position.x,
                    p["position"]["y"]?.Value<float>() ?? go.transform.position.y,
                    p["position"]["z"]?.Value<float>() ?? go.transform.position.z
                );
            }

            if (p["rotation"] != null)
            {
                go.transform.eulerAngles = new Vector3(
                    p["rotation"]["x"]?.Value<float>() ?? go.transform.eulerAngles.x,
                    p["rotation"]["y"]?.Value<float>() ?? go.transform.eulerAngles.y,
                    p["rotation"]["z"]?.Value<float>() ?? go.transform.eulerAngles.z
                );
            }
            
            if (p["scale"] != null)
            {
                go.transform.localScale = new Vector3(
                    p["scale"]["x"]?.Value<float>() ?? go.transform.localScale.x,
                    p["scale"]["y"]?.Value<float>() ?? go.transform.localScale.y,
                    p["scale"]["z"]?.Value<float>() ?? go.transform.localScale.z
                );
            }

            return new JObject { ["ok"] = true };
        }

        private static JObject GetTransform(JObject p)
        {
            int id = p["id"].Value<int>();
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null) throw new System.Exception("Object not found");

            return new JObject
            {
                ["position"] = new JObject { ["x"] = go.transform.position.x, ["y"] = go.transform.position.y, ["z"] = go.transform.position.z },
                ["rotation"] = new JObject { ["x"] = go.transform.eulerAngles.x, ["y"] = go.transform.eulerAngles.y, ["z"] = go.transform.eulerAngles.z },
                ["scale"] = new JObject { ["x"] = go.transform.localScale.x, ["y"] = go.transform.localScale.y, ["z"] = go.transform.localScale.z }
            };
        }
    }
}
