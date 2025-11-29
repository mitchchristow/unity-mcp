#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityMcp.Editor.MCP.Progress;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for tracking operation progress.
    /// </summary>
    public static class ProgressController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_progress", GetProgress);
            JsonRpcDispatcher.RegisterMethod("unity.get_all_progress", GetAllProgress);
        }

        /// <summary>
        /// Gets the progress of a specific operation.
        /// </summary>
        private static JObject GetProgress(JObject p)
        {
            string operationId = p["operationId"]?.ToString();
            
            if (string.IsNullOrEmpty(operationId))
            {
                throw new System.ArgumentException("operationId is required");
            }

            var progress = ProgressTracker.GetProgress(operationId);
            
            if (progress == null)
            {
                return new JObject
                {
                    ["found"] = false,
                    ["operationId"] = operationId
                };
            }

            return new JObject
            {
                ["found"] = true,
                ["operationId"] = progress.OperationId,
                ["operationType"] = progress.OperationType,
                ["description"] = progress.Description,
                ["progress"] = progress.Progress,
                ["percentComplete"] = (int)(progress.Progress * 100),
                ["status"] = progress.Status,
                ["message"] = progress.Message,
                ["startTime"] = progress.StartTime,
                ["endTime"] = progress.EndTime
            };
        }

        /// <summary>
        /// Gets all active operations and their progress.
        /// </summary>
        private static JObject GetAllProgress(JObject p)
        {
            // Clean up old completed operations (older than 5 minutes)
            ProgressTracker.CleanupCompleted(System.TimeSpan.FromMinutes(5));
            
            return ProgressTracker.GetAllProgress();
        }
    }
}
#endif

