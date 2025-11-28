using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class ConsoleController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.console.log", Log);
            JsonRpcDispatcher.RegisterMethod("unity.console.clear", Clear);
        }

        private static JObject Log(JObject p)
        {
            string message = p["message"]?.ToString() ?? "";
            string type = p["type"]?.ToString()?.ToLower() ?? "info";

            switch (type)
            {
                case "warning":
                    Debug.LogWarning($"[MCP Remote] {message}");
                    break;
                case "error":
                    Debug.LogError($"[MCP Remote] {message}");
                    break;
                default:
                    Debug.Log($"[MCP Remote] {message}");
                    break;
            }

            return new JObject { ["ok"] = true };
        }

        private static JObject Clear(JObject p)
        {
            // Clearing console via reflection
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);

            return new JObject { ["ok"] = true };
        }
    }
}
