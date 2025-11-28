using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityMcp.Editor.MCP.Rpc.Controllers;
using UnityEngine;
using UnityEditor;

namespace UnityMcp.Tests.Editor
{
    public class ControllerTests
    {
        [Test]
        public void HierarchyController_CreateObject_CreatesGameObject()
        {
            // Setup
            var method = typeof(HierarchyController).GetMethod("CreateObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var paramsObj = new JObject { ["name"] = "TestObject" };

            // Execute
            var result = (JObject)method.Invoke(null, new object[] { paramsObj });
            int id = result["id"].Value<int>();

            // Verify
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            Assert.IsNotNull(go);
            Assert.AreEqual("TestObject", go.name);

            // Cleanup
            Object.DestroyImmediate(go);
        }
    }
}
