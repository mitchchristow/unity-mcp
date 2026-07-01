#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP
{
    /// <summary>
    /// Bridges MCP wire object IDs across Unity versions.
    /// Unity 6.5+ uses EntityId; earlier versions use InstanceID (int).
    /// </summary>
    public static class McpObjectReference
    {
        public static JToken ToJToken(Object obj)
        {
            if (obj == null)
                return 0;

#if UNITY_6000_5_OR_NEWER
            return JToken.FromObject(EntityId.ToULong(obj.GetEntityId()));
#else
            return obj.GetInstanceID();
#endif
        }

        /// <summary>Wire value for anonymous/event payloads (int on 6.2–6.4, ulong on 6.5+).</summary>
        public static object ToWireValue(Object obj)
        {
            if (obj == null)
                return null;

#if UNITY_6000_5_OR_NEWER
            return EntityId.ToULong(obj.GetEntityId());
#else
            return obj.GetInstanceID();
#endif
        }

        public static ulong GetWireKey(Object obj)
        {
            if (obj == null)
                return 0;

#if UNITY_6000_5_OR_NEWER
            return EntityId.ToULong(obj.GetEntityId());
#else
            return unchecked((ulong)(uint)obj.GetInstanceID());
#endif
        }

        public static Object ToObject(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

#if UNITY_6000_5_OR_NEWER
            return FromWireKey(ParseWireUlong(token));
#else
            return EditorUtility.InstanceIDToObject(token.Value<int>());
#endif
        }

        public static Object ToObject(int instanceId)
        {
#if UNITY_6000_5_OR_NEWER
            return FromWireKey(unchecked((ulong)(uint)instanceId));
#else
            return EditorUtility.InstanceIDToObject(instanceId);
#endif
        }

        public static GameObject ToGameObject(JToken token) => ToObject(token) as GameObject;

        public static GameObject RequireGameObject(JToken token, string paramName = "id")
        {
            var go = ToGameObject(token);
            if (go == null)
                throw new System.Exception($"Object not found for '{paramName}'");
            return go;
        }

        public static GameObject ToGameObject(int instanceId) => ToObject(instanceId) as GameObject;

        public static Object ToObject(ulong wireKey) => FromWireKey(wireKey);

        public static object WireKeyToWireValue(ulong wireKey)
        {
#if UNITY_6000_5_OR_NEWER
            return wireKey;
#else
            return unchecked((int)(uint)wireKey);
#endif
        }

        public static Object FromWireKey(ulong wireKey)
        {
            if (wireKey == 0)
                return null;

#if UNITY_6000_5_OR_NEWER
            return EditorUtility.EntityIdToObject(EntityId.FromULong(wireKey));
#else
            return EditorUtility.InstanceIDToObject(unchecked((int)(uint)wireKey));
#endif
        }

#if UNITY_6000_5_OR_NEWER
        private static ulong ParseWireUlong(JToken token)
        {
            if (token.Type != JTokenType.Integer)
                throw new System.Exception("Invalid object id");

            long value = token.Value<long>();
            if (value < 0)
                return unchecked((ulong)(uint)(int)value);

            return (ulong)value;
        }
#endif
    }
}
#endif
