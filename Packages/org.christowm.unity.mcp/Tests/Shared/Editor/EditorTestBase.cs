using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Tests.Editor.Shared
{
    /// <summary>
    /// Base class for EditMode tests that create scene objects or assets.
    /// </summary>
    public abstract class EditorTestBase
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        protected T Track<T>(T obj) where T : Object
        {
            if (obj != null)
                _createdObjects.Add(obj);
            return obj;
        }

        [TearDown]
        public void EditorTestBase_TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                var obj = _createdObjects[i];
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _createdObjects.Clear();
        }

        protected static void DestroyByWireId(JToken idToken)
        {
            if (idToken == null)
                return;

            var go = UnityMcp.Editor.MCP.McpObjectReference.ToGameObject(idToken);
            if (go != null)
                Object.DestroyImmediate(go);
        }
    }
}
