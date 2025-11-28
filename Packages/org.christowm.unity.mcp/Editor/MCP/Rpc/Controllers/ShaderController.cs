using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class ShaderController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.create_material", CreateMaterial);
            JsonRpcDispatcher.RegisterMethod("unity.set_material_property", SetMaterialProperty);
        }

        private static JObject CreateMaterial(JObject p)
        {
            string name = p["name"]?.ToString() ?? "New Material";
            string shaderName = p["shader"]?.ToString() ?? "Standard";
            string path = p["path"]?.ToString();

            if (string.IsNullOrEmpty(path))
            {
                path = $"Assets/{name}.mat";
            }
            if (!path.EndsWith(".mat")) path += ".mat";

            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception($"Shader not found: {shaderName}");

            Material material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();

            return new JObject { ["path"] = path, ["id"] = material.GetInstanceID() };
        }

        private static JObject SetMaterialProperty(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path)) throw new System.Exception("Path is required");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) throw new System.Exception($"Material not found at path: {path}");

            Undo.RecordObject(material, "Set Material Property via MCP");

            if (p["color"] != null)
            {
                string propName = p["color"]["name"]?.ToString() ?? "_Color";
                Color color = new Color(
                    p["color"]["r"]?.Value<float>() ?? 1,
                    p["color"]["g"]?.Value<float>() ?? 1,
                    p["color"]["b"]?.Value<float>() ?? 1,
                    p["color"]["a"]?.Value<float>() ?? 1
                );
                material.SetColor(propName, color);
            }

            if (p["float"] != null)
            {
                string propName = p["float"]["name"]?.ToString();
                float value = p["float"]["value"]?.Value<float>() ?? 0;
                if (!string.IsNullOrEmpty(propName))
                {
                    material.SetFloat(propName, value);
                }
            }

            if (p["texture"] != null)
            {
                string propName = p["texture"]["name"]?.ToString() ?? "_MainTex";
                string texPath = p["texture"]["path"]?.ToString();
                if (!string.IsNullOrEmpty(texPath))
                {
                    Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                    if (texture != null)
                    {
                        material.SetTexture(propName, texture);
                    }
                }
            }

            return new JObject { ["ok"] = true };
        }
    }
}
