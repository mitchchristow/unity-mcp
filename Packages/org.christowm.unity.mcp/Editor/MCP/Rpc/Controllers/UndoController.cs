using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing undo/redo operations in Unity.
    /// </summary>
    public static class UndoController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.undo", PerformUndo);
            JsonRpcDispatcher.RegisterMethod("unity.redo", PerformRedo);
            JsonRpcDispatcher.RegisterMethod("unity.get_undo_history", GetUndoHistory);
            JsonRpcDispatcher.RegisterMethod("unity.clear_undo", ClearUndo);
            JsonRpcDispatcher.RegisterMethod("unity.begin_undo_group", BeginUndoGroup);
            JsonRpcDispatcher.RegisterMethod("unity.end_undo_group", EndUndoGroup);
            JsonRpcDispatcher.RegisterMethod("unity.collapse_undo", CollapseUndo);
        }

        /// <summary>
        /// Performs an undo operation.
        /// </summary>
        private static JObject PerformUndo(JObject p)
        {
            Undo.PerformUndo();
            return new JObject
            {
                ["ok"] = true,
                ["action"] = "undo"
            };
        }

        /// <summary>
        /// Performs a redo operation.
        /// </summary>
        private static JObject PerformRedo(JObject p)
        {
            Undo.PerformRedo();
            return new JObject
            {
                ["ok"] = true,
                ["action"] = "redo"
            };
        }

        /// <summary>
        /// Gets information about the undo history.
        /// Note: Unity's Undo API has limited access to history details.
        /// </summary>
        private static JObject GetUndoHistory(JObject p)
        {
            // Unity doesn't expose the full undo history, but we can get some info
            string currentGroupName = Undo.GetCurrentGroupName();
            int currentGroup = Undo.GetCurrentGroup();

            return new JObject
            {
                ["currentGroupName"] = currentGroupName,
                ["currentGroupId"] = currentGroup,
                ["note"] = "Unity's Undo API has limited history access. Use undo/redo to navigate."
            };
        }

        /// <summary>
        /// Clears all undo history.
        /// Warning: This cannot be undone!
        /// </summary>
        private static JObject ClearUndo(JObject p)
        {
            bool confirm = p["confirm"]?.Value<bool>() ?? false;
            
            if (!confirm)
            {
                return new JObject
                {
                    ["ok"] = false,
                    ["error"] = "Must set 'confirm: true' to clear undo history. This action cannot be undone!"
                };
            }

            Undo.ClearAll();
            return new JObject
            {
                ["ok"] = true,
                ["cleared"] = true
            };
        }

        /// <summary>
        /// Begins a new undo group with a name.
        /// All operations until EndUndoGroup will be grouped together.
        /// </summary>
        private static JObject BeginUndoGroup(JObject p)
        {
            string name = p["name"]?.ToString() ?? "MCP Operation";
            
            Undo.SetCurrentGroupName(name);
            int groupId = Undo.GetCurrentGroup();

            return new JObject
            {
                ["ok"] = true,
                ["groupName"] = name,
                ["groupId"] = groupId
            };
        }

        /// <summary>
        /// Ends the current undo group and increments the group index.
        /// </summary>
        private static JObject EndUndoGroup(JObject p)
        {
            Undo.IncrementCurrentGroup();
            return new JObject
            {
                ["ok"] = true,
                ["newGroupId"] = Undo.GetCurrentGroup()
            };
        }

        /// <summary>
        /// Collapses all undo operations in the current group into one.
        /// </summary>
        private static JObject CollapseUndo(JObject p)
        {
            int groupId = p["groupId"]?.Value<int>() ?? Undo.GetCurrentGroup();
            string name = p["name"]?.ToString();
            
            if (!string.IsNullOrEmpty(name))
            {
                Undo.SetCurrentGroupName(name);
            }
            
            Undo.CollapseUndoOperations(groupId);

            return new JObject
            {
                ["ok"] = true,
                ["collapsedGroupId"] = groupId
            };
        }
    }
}

