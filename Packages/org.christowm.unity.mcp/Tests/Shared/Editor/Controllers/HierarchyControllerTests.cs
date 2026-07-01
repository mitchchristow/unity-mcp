using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Tests.Editor.Shared
{
    public class HierarchyControllerTests : EditorTestBase
    {
        [Test]
        public void CreateObject_ReturnsResolvableGameObject()
        {
            var result = RpcTestHarness.Invoke("unity.create_object", new JObject { ["name"] = "__mcp_test_object" });
            var go = Track(McpObjectReference.ToGameObject(result["id"]));

            Assert.IsNotNull(go);
            Assert.AreEqual("__mcp_test_object", go.name);
        }
    }
}
