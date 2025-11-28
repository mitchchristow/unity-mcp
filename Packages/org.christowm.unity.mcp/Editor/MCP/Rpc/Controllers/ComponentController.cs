using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class ComponentController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.add_component", AddComponent);
            JsonRpcDispatcher.RegisterMethod("unity.set_component_property", SetProperty);
            JsonRpcDispatcher.RegisterMethod("unity.set_material_color", SetMaterialColor);
            JsonRpcDispatcher.RegisterMethod("unity.set_material", SetMaterial);
        }

        private static JObject AddComponent(JObject p)
        {
            int id = p["id"].Value<int>();
            string typeName = p["type"].ToString();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null) throw new Exception("Object not found");

            // Try to find the type
            // This is a simple lookup, might need more robust assembly searching
            var type = GetTypeByName(typeName);
            if (type == null) throw new Exception($"Type '{typeName}' not found");

            var component = Undo.AddComponent(go, type);
            return new JObject { ["componentId"] = component.GetInstanceID() };
        }

        private static JObject SetProperty(JObject p)
        {
            int id = p["id"].Value<int>();
            string componentName = p["component"].ToString();
            string fieldName = p["field"].ToString();
            var valueToken = p["value"];

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null) throw new Exception("Object not found");

            var component = go.GetComponent(componentName);
            if (component == null) throw new Exception($"Component '{componentName}' not found on object");

            Undo.RecordObject(component, $"Set {fieldName}");

            var type = component.GetType();
            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(component, ConvertJTokenToType(valueToken, prop.PropertyType));
            }
            else if (field != null)
            {
                field.SetValue(component, ConvertJTokenToType(valueToken, field.FieldType));
            }
            else
            {
                throw new Exception($"Property/Field '{fieldName}' not found or not writable");
            }

            return new JObject { ["ok"] = true };
        }

        private static Type GetTypeByName(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
                // Try adding UnityEngine prefix if missing
                type = assembly.GetType($"UnityEngine.{name}");
                if (type != null) return type;
            }
            return null;
        }

        private static JObject SetMaterialColor(JObject p)
        {
            int id = p["id"].Value<int>();
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null) throw new Exception("Object not found");

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) throw new Exception("Object has no Renderer");

            Undo.RecordObject(renderer, "Set Material Color");

            // We need to modify the material. In Editor, accessing .material creates a leak if not careful,
            // but .sharedMaterial modifies the asset.
            // For a "red cube" instance, we usually want a new instance material or use MaterialPropertyBlock.
            // But for simplicity in this MCP, let's just modify the material instance (which Unity handles by creating a copy on access).
            
            Color color = Color.white;
            if (p["color"] != null)
            {
                color = new Color(
                    p["color"]["r"]?.Value<float>() ?? 0f,
                    p["color"]["g"]?.Value<float>() ?? 0f,
                    p["color"]["b"]?.Value<float>() ?? 0f,
                    p["color"]["a"]?.Value<float>() ?? 1f
                );
            }

            // Fix for "Instantiating material due to calling renderer.material during edit mode"
            // We explicitly create a new material to avoid the warning and ensure we don't modify the default asset.
            // In a real production environment, you might want to check if the current material is already a custom instance.
            
            var newMaterial = new Material(Shader.Find("Standard"));
            newMaterial.color = color;
            
            // Assign to sharedMaterial to avoid the leak warning
            renderer.sharedMaterial = newMaterial;

            return new JObject { ["ok"] = true };
        }

        private static JObject SetMaterial(JObject p)
        {
            int id = p["id"].Value<int>();
            string path = p["path"]?.ToString();
            
            if (string.IsNullOrEmpty(path)) throw new Exception("Path is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null) throw new Exception("Object not found");

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) throw new Exception("Object has no Renderer");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) throw new Exception($"Material not found at path: {path}");

            Undo.RecordObject(renderer, "Set Material");
            renderer.sharedMaterial = material;

            return new JObject { ["ok"] = true };
        }

        private static object ConvertJTokenToType(JToken token, Type type)
        {
            return token.ToObject(type);
        }
    }
}
