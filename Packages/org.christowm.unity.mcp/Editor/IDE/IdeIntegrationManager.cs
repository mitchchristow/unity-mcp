using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace UnityMcp.Editor.IDE
{
    [InitializeOnLoad]
    public static class IdeIntegrationManager
    {
        static IdeIntegrationManager()
        {
            // Generate manifest on load if it doesn't exist or if needed
            // For now, let's add a menu item to force regeneration
        }

        [MenuItem("Tools/MCP/Generate IDE Manifests")]
        public static void GenerateManifests()
        {
            GenerateMcpManifest();
            Debug.Log("[MCP] IDE Manifests generated.");
        }

        private static void GenerateMcpManifest()
        {
            var manifest = new JObject
            {
                ["name"] = "unity-mcp",
                ["version"] = "1.0.0",
                ["description"] = "Unity Editor Control via MCP",
                ["tools"] = new JObject
                {
                    ["unity"] = new JObject
                    {
                        ["type"] = "json_rpc",
                        ["endpoint"] = "http://localhost:17890/mcp/rpc"
                    }
                }
            };

            string path = Path.Combine(Directory.GetCurrentDirectory(), "mcp-manifest.json");
            File.WriteAllText(path, manifest.ToString(Newtonsoft.Json.Formatting.Indented));
            Debug.Log($"[MCP] Wrote manifest to {path}");
        }
    }
}
