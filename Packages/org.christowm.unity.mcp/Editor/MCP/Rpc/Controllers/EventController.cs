#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityMcp.Editor.MCP.Rpc;
using UnityMcp.Editor.MCP.Events;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// RPC Controller for accessing event history and managing event subscriptions.
    /// </summary>
    public static class EventController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_recent_events", GetRecentEvents);
            JsonRpcDispatcher.RegisterMethod("unity.clear_event_history", ClearEventHistory);
            JsonRpcDispatcher.RegisterMethod("unity.get_event_types", GetEventTypes);
        }

        /// <summary>
        /// Get recent events from the event history.
        /// </summary>
        /// <param name="p">Parameters: limit (int, optional, default 50)</param>
        private static JObject GetRecentEvents(JObject p)
        {
            int limit = p["limit"]?.Value<int>() ?? 50;
            
            var events = EventBroadcaster.GetRecentEvents(limit);
            
            var eventArray = new JArray();
            foreach (var evt in events)
            {
                eventArray.Add(new JObject
                {
                    ["event"] = evt.EventName,
                    ["data"] = JToken.FromObject(evt.Data ?? new { }),
                    ["timestamp"] = evt.Timestamp.ToString("o")
                });
            }

            return new JObject
            {
                ["events"] = eventArray,
                ["count"] = eventArray.Count,
                ["maxHistory"] = 100
            };
        }

        /// <summary>
        /// Clear the event history.
        /// </summary>
        private static JObject ClearEventHistory(JObject p)
        {
            EventBroadcaster.ClearEventHistory();
            
            return new JObject
            {
                ["success"] = true,
                ["message"] = "Event history cleared"
            };
        }

        /// <summary>
        /// Get list of available event types.
        /// </summary>
        private static JObject GetEventTypes(JObject p)
        {
            var eventTypes = new JArray
            {
                new JObject
                {
                    ["name"] = "scene.object_created",
                    ["description"] = "Fired when a new GameObject is created",
                    ["payload"] = new JObject
                    {
                        ["id"] = "Instance ID of the created object",
                        ["name"] = "Name of the object",
                        ["path"] = "Hierarchy path",
                        ["parentId"] = "Parent object ID (if any)"
                    }
                },
                new JObject
                {
                    ["name"] = "scene.object_deleted",
                    ["description"] = "Fired when a GameObject is deleted",
                    ["payload"] = new JObject
                    {
                        ["id"] = "Instance ID of the deleted object"
                    }
                },
                new JObject
                {
                    ["name"] = "scene.selection_changed",
                    ["description"] = "Fired when the editor selection changes",
                    ["payload"] = new JObject
                    {
                        ["count"] = "Number of selected objects",
                        ["objects"] = "Array of selected object info (id, name, path)"
                    }
                },
                new JObject
                {
                    ["name"] = "scene.opened",
                    ["description"] = "Fired when a scene is opened",
                    ["payload"] = new JObject
                    {
                        ["name"] = "Scene name",
                        ["path"] = "Scene asset path",
                        ["mode"] = "Open mode (Single, Additive, etc.)"
                    }
                },
                new JObject
                {
                    ["name"] = "scene.closed",
                    ["description"] = "Fired when a scene is closed",
                    ["payload"] = new JObject
                    {
                        ["name"] = "Scene name",
                        ["path"] = "Scene asset path"
                    }
                },
                new JObject
                {
                    ["name"] = "scene.saved",
                    ["description"] = "Fired when a scene is saved",
                    ["payload"] = new JObject
                    {
                        ["name"] = "Scene name",
                        ["path"] = "Scene asset path"
                    }
                },
                new JObject
                {
                    ["name"] = "playmode.changed",
                    ["description"] = "Fired when play mode state changes",
                    ["payload"] = new JObject
                    {
                        ["state"] = "State: edit, exiting_edit, playing, exiting_play",
                        ["isPlaying"] = "Whether currently in play mode",
                        ["isPaused"] = "Whether currently paused"
                    }
                },
                new JObject
                {
                    ["name"] = "console.log",
                    ["description"] = "Fired when a message is logged to the console",
                    ["payload"] = new JObject
                    {
                        ["message"] = "Log message",
                        ["stackTrace"] = "Stack trace (if available)",
                        ["type"] = "Log type: info, warning, error, exception"
                    }
                },
                new JObject
                {
                    ["name"] = "scripts.compilation_started",
                    ["description"] = "Fired when script compilation begins",
                    ["payload"] = new JObject
                    {
                        ["timestamp"] = "When compilation started"
                    }
                },
                new JObject
                {
                    ["name"] = "scripts.compilation_finished",
                    ["description"] = "Fired when script compilation completes",
                    ["payload"] = new JObject
                    {
                        ["success"] = "Whether compilation succeeded",
                        ["timestamp"] = "When compilation finished"
                    }
                },
                new JObject
                {
                    ["name"] = "editor.undo_redo",
                    ["description"] = "Fired when an undo or redo operation is performed",
                    ["payload"] = new JObject
                    {
                        ["undoName"] = "Name of the undo group"
                    }
                }
            };

            return new JObject
            {
                ["eventTypes"] = eventTypes,
                ["count"] = eventTypes.Count,
                ["webSocketUrl"] = "ws://localhost:17891/mcp/events"
            };
        }
    }
}
#endif

