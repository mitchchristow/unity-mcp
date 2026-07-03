#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityMcp.Editor.MCP
{
    /// <summary>
    /// Version-safe wrappers for Unity object discovery APIs.
    /// Unity 6.3+ uses FindObjectsInactive; 6.2 uses FindObjectsSortMode / legacy FindObjectsOfType.
    /// </summary>
    public static class McpFindObjects
    {
        public static T[] FindByType<T>(bool includeInactive = true) where T : Object
        {
#if UNITY_6000_3_OR_NEWER
            return Object.FindObjectsByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
            if (includeInactive)
            {
#pragma warning disable CS0618
                return Object.FindObjectsOfType<T>(true);
#pragma warning restore CS0618
            }

            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#endif
        }

        public static Object[] FindByType(Type type, bool includeInactive = true)
        {
#if UNITY_6000_3_OR_NEWER
            return Object.FindObjectsByType(
                type,
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
#pragma warning disable CS0618
            return Object.FindObjectsOfType(type, includeInactive);
#pragma warning restore CS0618
#endif
        }
    }
}
#endif
