using Newtonsoft.Json.Linq;
using UnityEditor;
using System.IO;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class AssetController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.list_assets", ListAssets);
            JsonRpcDispatcher.RegisterMethod("unity.get_asset_info", GetAssetInfo);
        }

        private static JObject ListAssets(JObject p)
        {
            string typeFilter = p["type"]?.ToString()?.ToLower();
            string folder = p["folder"]?.ToString() ?? "Assets";
            
            var assets = new JArray();
            
            // Define search filters based on type
            string searchFilter = typeFilter switch
            {
                "material" => "t:Material",
                "prefab" => "t:Prefab",
                "script" => "t:Script",
                "texture" => "t:Texture",
                "mesh" => "t:Mesh",
                "audio" => "t:AudioClip",
                "scene" => "t:Scene",
                "shader" => "t:Shader",
                "animation" => "t:AnimationClip",
                _ => "" // All assets
            };

            string[] guids = AssetDatabase.FindAssets(searchFilter, new[] { folder });
            
            // Limit results to prevent overwhelming responses
            int maxResults = 100;
            int count = 0;
            
            foreach (string guid in guids)
            {
                if (count >= maxResults) break;
                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                
                if (asset != null)
                {
                    assets.Add(new JObject
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
                ["assets"] = assets,
                ["count"] = count,
                ["truncated"] = guids.Length > maxResults
            };
        }

        private static JObject GetAssetInfo(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path)) 
                throw new System.Exception("Path is required");

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) 
                throw new System.Exception($"Asset not found at path: {path}");

            var info = new JObject
            {
                ["path"] = path,
                ["name"] = asset.name,
                ["type"] = asset.GetType().Name,
                ["guid"] = AssetDatabase.AssetPathToGUID(path)
            };

            // Add type-specific info
            if (asset is UnityEngine.Material mat)
            {
                info["shader"] = mat.shader?.name;
            }
            else if (asset is UnityEngine.GameObject go)
            {
                var components = go.GetComponents<UnityEngine.Component>();
                info["components"] = new JArray(components.Select(c => c.GetType().Name));
            }
            else if (asset is UnityEngine.Texture tex)
            {
                info["width"] = tex.width;
                info["height"] = tex.height;
            }

            return info;
        }
    }
}

