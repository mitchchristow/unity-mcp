using Newtonsoft.Json.Linq;
using UnityEditor;
using System.IO;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class ScriptController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.write_script", WriteScript);
            JsonRpcDispatcher.RegisterMethod("unity.reload_scripts", ReloadScripts);
        }

        private static JObject WriteScript(JObject p)
        {
            string path = p["path"]?.ToString();
            string content = p["content"]?.ToString();

            if (string.IsNullOrEmpty(path) || content == null)
                throw new System.Exception("Path and content are required");

            // Ensure path is relative to Assets if not absolute
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine("Assets", path);
            }

            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(path, content);
            AssetDatabase.ImportAsset(path);

            return new JObject { ["ok"] = true };
        }

        private static JObject ReloadScripts(JObject p)
        {
            AssetDatabase.Refresh();
            return new JObject { ["ok"] = true };
        }
    }
}
