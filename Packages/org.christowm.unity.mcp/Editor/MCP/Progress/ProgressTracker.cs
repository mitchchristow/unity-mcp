#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Progress
{
    /// <summary>
    /// Tracks progress of long-running operations and broadcasts updates.
    /// </summary>
    public static class ProgressTracker
    {
        private static readonly Dictionary<string, ProgressInfo> _activeOperations = new Dictionary<string, ProgressInfo>();
        private static readonly object _lock = new object();

        public class ProgressInfo
        {
            public string OperationId { get; set; }
            public string OperationType { get; set; }
            public string Description { get; set; }
            public float Progress { get; set; } // 0.0 to 1.0
            public string Status { get; set; } // "running", "completed", "failed"
            public string Message { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
        }

        /// <summary>
        /// Starts tracking a new operation.
        /// </summary>
        public static string StartOperation(string operationType, string description)
        {
            var operationId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var info = new ProgressInfo
            {
                OperationId = operationId,
                OperationType = operationType,
                Description = description,
                Progress = 0f,
                Status = "running",
                Message = "Starting...",
                StartTime = DateTime.UtcNow
            };

            lock (_lock)
            {
                _activeOperations[operationId] = info;
            }

            BroadcastProgress(info);
            return operationId;
        }

        /// <summary>
        /// Updates the progress of an operation.
        /// </summary>
        public static void UpdateProgress(string operationId, float progress, string message = null)
        {
            lock (_lock)
            {
                if (_activeOperations.TryGetValue(operationId, out var info))
                {
                    info.Progress = Mathf.Clamp01(progress);
                    if (message != null) info.Message = message;
                    BroadcastProgress(info);
                }
            }
        }

        /// <summary>
        /// Marks an operation as completed.
        /// </summary>
        public static void CompleteOperation(string operationId, string message = "Completed")
        {
            lock (_lock)
            {
                if (_activeOperations.TryGetValue(operationId, out var info))
                {
                    info.Progress = 1f;
                    info.Status = "completed";
                    info.Message = message;
                    info.EndTime = DateTime.UtcNow;
                    BroadcastProgress(info);
                    
                    // Keep completed operations briefly for status queries
                    // They'll be cleaned up after a delay
                }
            }
        }

        /// <summary>
        /// Marks an operation as failed.
        /// </summary>
        public static void FailOperation(string operationId, string error)
        {
            lock (_lock)
            {
                if (_activeOperations.TryGetValue(operationId, out var info))
                {
                    info.Status = "failed";
                    info.Message = error;
                    info.EndTime = DateTime.UtcNow;
                    BroadcastProgress(info);
                }
            }
        }

        /// <summary>
        /// Gets the current progress of an operation.
        /// </summary>
        public static ProgressInfo GetProgress(string operationId)
        {
            lock (_lock)
            {
                return _activeOperations.TryGetValue(operationId, out var info) ? info : null;
            }
        }

        /// <summary>
        /// Gets all active operations.
        /// </summary>
        public static JObject GetAllProgress()
        {
            lock (_lock)
            {
                var operations = new JArray();
                foreach (var kvp in _activeOperations)
                {
                    operations.Add(ProgressToJson(kvp.Value));
                }

                return new JObject
                {
                    ["operations"] = operations,
                    ["count"] = operations.Count
                };
            }
        }

        /// <summary>
        /// Cleans up completed operations older than the specified age.
        /// </summary>
        public static void CleanupCompleted(TimeSpan maxAge)
        {
            var cutoff = DateTime.UtcNow - maxAge;
            var toRemove = new List<string>();

            lock (_lock)
            {
                foreach (var kvp in _activeOperations)
                {
                    if (kvp.Value.EndTime.HasValue && kvp.Value.EndTime.Value < cutoff)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var id in toRemove)
                {
                    _activeOperations.Remove(id);
                }
            }
        }

        private static void BroadcastProgress(ProgressInfo info)
        {
            var wsServer = McpServer.WebSocketServer;
            if (wsServer != null && wsServer.IsRunning)
            {
                _ = wsServer.BroadcastEvent("operation.progress", new
                {
                    operationId = info.OperationId,
                    operationType = info.OperationType,
                    description = info.Description,
                    progress = info.Progress,
                    status = info.Status,
                    message = info.Message,
                    startTime = info.StartTime,
                    endTime = info.EndTime,
                    percentComplete = (int)(info.Progress * 100)
                });
            }
        }

        private static JObject ProgressToJson(ProgressInfo info)
        {
            return new JObject
            {
                ["operationId"] = info.OperationId,
                ["operationType"] = info.OperationType,
                ["description"] = info.Description,
                ["progress"] = info.Progress,
                ["percentComplete"] = (int)(info.Progress * 100),
                ["status"] = info.Status,
                ["message"] = info.Message,
                ["startTime"] = info.StartTime,
                ["endTime"] = info.EndTime,
                ["elapsedSeconds"] = (DateTime.UtcNow - info.StartTime).TotalSeconds
            };
        }
    }
}
#endif

