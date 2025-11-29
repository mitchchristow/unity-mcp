using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class ConsoleController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.console.log", Log);
            JsonRpcDispatcher.RegisterMethod("unity.console.clear", Clear);
            JsonRpcDispatcher.RegisterMethod("unity.get_console_logs", GetConsoleLogs);
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
            var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            clearMethod.Invoke(null, null);

            return new JObject { ["ok"] = true };
        }

        private static JObject GetConsoleLogs(JObject p)
        {
            int maxEntries = p["limit"]?.Value<int>() ?? 50;
            
            var logs = new JArray();
            
            // Access Unity's internal LogEntries class via reflection
            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            
            if (logEntriesType == null)
            {
                return new JObject 
                { 
                    ["logs"] = logs,
                    ["error"] = "Could not access Unity log entries"
                };
            }

            // Get the count of log entries
            var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
            int count = (int)getCountMethod.Invoke(null, null);

            // StartGettingEntries
            var startMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
            startMethod?.Invoke(null, null);

            try
            {
                // GetEntryInternal method
                var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                
                // LogEntry type
                var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
                
                if (getEntryMethod != null && logEntryType != null)
                {
                    int entriesToGet = System.Math.Min(count, maxEntries);
                    int startIndex = System.Math.Max(0, count - entriesToGet);
                    
                    for (int i = startIndex; i < count; i++)
                    {
                        var entry = System.Activator.CreateInstance(logEntryType);
                        bool success = (bool)getEntryMethod.Invoke(null, new object[] { i, entry });
                        
                        if (success)
                        {
                            var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                            var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
                            
                            string message = messageField?.GetValue(entry)?.ToString() ?? "";
                            int mode = (int)(modeField?.GetValue(entry) ?? 0);
                            
                            // Determine log type from mode flags
                            string logType = "Log";
                            if ((mode & 2) != 0) logType = "Warning";
                            if ((mode & 4) != 0 || (mode & 8) != 0 || (mode & 16) != 0) logType = "Error";
                            
                            logs.Add(new JObject
                            {
                                ["message"] = message,
                                ["type"] = logType,
                                ["index"] = i
                            });
                        }
                    }
                }
            }
            finally
            {
                // EndGettingEntries
                var endMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
                endMethod?.Invoke(null, null);
            }

            return new JObject 
            { 
                ["logs"] = logs,
                ["totalCount"] = count
            };
        }
    }
}
