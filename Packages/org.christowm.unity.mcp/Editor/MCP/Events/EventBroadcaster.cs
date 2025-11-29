#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityMcp.Editor.Networking;

namespace UnityMcp.Editor.MCP.Events
{
    /// <summary>
    /// Broadcasts Unity Editor events via WebSocket to connected clients.
    /// Events include: scene changes, selection changes, play mode changes,
    /// console logs, and script compilation status.
    /// Note: Do NOT use [InitializeOnLoad] - McpServer.Initialize() calls us after WebSocket is ready.
    /// </summary>
    public static class EventBroadcaster
    {
        private static WebSocketServer _wsServer;
        private static bool _isInitialized;
        private static bool _wasCompiling;
        private static HashSet<int> _trackedObjects = new HashSet<int>();
        private static PlayModeStateChange _lastPlayModeState;
        
        // Event history for the resource endpoint
        private static readonly Queue<EventRecord> _eventHistory = new Queue<EventRecord>();
        private static readonly int MaxEventHistory = 100;
        private static readonly object _historyLock = new object();

        public class EventRecord
        {
            public string EventName { get; set; }
            public object Data { get; set; }
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Initialize the event broadcaster. Called by McpServer after WebSocket server is ready.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Get reference to WebSocket server
            _wsServer = McpServer.WebSocketServer;
            if (_wsServer == null)
            {
                Debug.LogWarning("[MCP Events] WebSocket server not available, events will not be broadcast.");
                return;
            }

            // Subscribe to Unity events
            SubscribeToEvents();
            
            // Initialize object tracking
            RefreshTrackedObjects();

            Debug.Log("[MCP Events] Event broadcaster initialized.");
        }

        private static void SubscribeToEvents()
        {
            // Selection changes
            Selection.selectionChanged += OnSelectionChanged;

            // Play mode changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Scene changes
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.sceneSaved += OnSceneSaved;

            // Hierarchy changes (objects created/deleted)
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            // Console logs
            Application.logMessageReceived += OnLogMessageReceived;

            // Script compilation
            EditorApplication.update += CheckCompilationStatus;
            
            // Undo/Redo (can indicate object changes)
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public static void Shutdown()
        {
            if (!_isInitialized) return;

            // Unsubscribe from all events
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            Application.logMessageReceived -= OnLogMessageReceived;
            EditorApplication.update -= CheckCompilationStatus;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            _isInitialized = false;
            Debug.Log("[MCP Events] Event broadcaster shutdown.");
        }

        /// <summary>
        /// Get recent events for the resource endpoint.
        /// </summary>
        public static List<EventRecord> GetRecentEvents(int limit = 50)
        {
            lock (_historyLock)
            {
                var result = new List<EventRecord>();
                var events = _eventHistory.ToArray();
                
                int start = Math.Max(0, events.Length - limit);
                for (int i = start; i < events.Length; i++)
                {
                    result.Add(events[i]);
                }
                
                return result;
            }
        }

        /// <summary>
        /// Clear event history.
        /// </summary>
        public static void ClearEventHistory()
        {
            lock (_historyLock)
            {
                _eventHistory.Clear();
            }
        }

        private static void BroadcastEvent(string eventName, object data)
        {
            if (_wsServer == null) return;

            // Add to history
            lock (_historyLock)
            {
                _eventHistory.Enqueue(new EventRecord
                {
                    EventName = eventName,
                    Data = data,
                    Timestamp = DateTime.UtcNow
                });

                while (_eventHistory.Count > MaxEventHistory)
                {
                    _eventHistory.Dequeue();
                }
            }

            // Broadcast via WebSocket
            _ = _wsServer.BroadcastEvent(eventName, data);
        }

        // === Event Handlers ===

        private static void OnSelectionChanged()
        {
            var selection = Selection.gameObjects;
            var selectionData = new List<object>();

            foreach (var go in selection)
            {
                if (go != null)
                {
                    selectionData.Add(new
                    {
                        id = go.GetInstanceID(),
                        name = go.name,
                        path = GetGameObjectPath(go)
                    });
                }
            }

            BroadcastEvent("scene.selection_changed", new
            {
                count = selectionData.Count,
                objects = selectionData
            });
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Only broadcast on actual state changes
            if (state == _lastPlayModeState) return;
            _lastPlayModeState = state;

            string stateString = state switch
            {
                PlayModeStateChange.EnteredEditMode => "edit",
                PlayModeStateChange.ExitingEditMode => "exiting_edit",
                PlayModeStateChange.EnteredPlayMode => "playing",
                PlayModeStateChange.ExitingPlayMode => "exiting_play",
                _ => "unknown"
            };

            BroadcastEvent("playmode.changed", new
            {
                state = stateString,
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused
            });
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            BroadcastEvent("scene.opened", new
            {
                name = scene.name,
                path = scene.path,
                mode = mode.ToString(),
                buildIndex = scene.buildIndex
            });

            // Refresh tracked objects when scene opens
            RefreshTrackedObjects();
        }

        private static void OnSceneClosed(Scene scene)
        {
            BroadcastEvent("scene.closed", new
            {
                name = scene.name,
                path = scene.path
            });
        }

        private static void OnSceneSaved(Scene scene)
        {
            BroadcastEvent("scene.saved", new
            {
                name = scene.name,
                path = scene.path
            });
        }

        private static void OnHierarchyChanged()
        {
            // Detect created and deleted objects by comparing with tracked set
            var currentObjects = new HashSet<int>();
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var go in allObjects)
            {
                currentObjects.Add(go.GetInstanceID());
            }

            // Find new objects (created)
            foreach (var id in currentObjects)
            {
                if (!_trackedObjects.Contains(id))
                {
                    var go = EditorUtility.InstanceIDToObject(id) as GameObject;
                    if (go != null)
                    {
                        BroadcastEvent("scene.object_created", new
                        {
                            id = id,
                            name = go.name,
                            path = GetGameObjectPath(go),
                            parentId = go.transform.parent?.gameObject.GetInstanceID()
                        });
                    }
                }
            }

            // Find removed objects (deleted)
            foreach (var id in _trackedObjects)
            {
                if (!currentObjects.Contains(id))
                {
                    BroadcastEvent("scene.object_deleted", new
                    {
                        id = id
                    });
                }
            }

            // Update tracked objects
            _trackedObjects = currentObjects;
        }

        private static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            string logType = type switch
            {
                LogType.Error => "error",
                LogType.Assert => "assert",
                LogType.Warning => "warning",
                LogType.Log => "info",
                LogType.Exception => "exception",
                _ => "unknown"
            };

            BroadcastEvent("console.log", new
            {
                message = message,
                stackTrace = stackTrace,
                type = logType,
                timestamp = DateTime.UtcNow
            });
        }

        private static void CheckCompilationStatus()
        {
            bool isCompiling = EditorApplication.isCompiling;

            if (isCompiling && !_wasCompiling)
            {
                // Compilation started
                BroadcastEvent("scripts.compilation_started", new
                {
                    timestamp = DateTime.UtcNow
                });
            }
            else if (!isCompiling && _wasCompiling)
            {
                // Compilation finished
                BroadcastEvent("scripts.compilation_finished", new
                {
                    success = true, // We can't easily detect compile errors here
                    timestamp = DateTime.UtcNow
                });
            }

            _wasCompiling = isCompiling;
        }

        private static void OnUndoRedoPerformed()
        {
            BroadcastEvent("editor.undo_redo", new
            {
                undoName = Undo.GetCurrentGroupName(),
                timestamp = DateTime.UtcNow
            });
        }

        // === Utility Methods ===

        private static void RefreshTrackedObjects()
        {
            _trackedObjects.Clear();
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var go in allObjects)
            {
                _trackedObjects.Add(go.GetInstanceID());
            }
        }

        private static string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
#endif

