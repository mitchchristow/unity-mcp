using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor.MCP;
using UnityMcp.Tests.Editor.Shared;

namespace UnityMcp.Tests.Editor.V6000_2
{
    /// <summary>
    /// Instance ID wire-format tests for Unity 6.2.x only.
    /// </summary>
    public class McpObjectReferenceInstanceIdTests : EditorTestBase
    {
        [Test]
        public void InstanceId_RoundTripsThroughWireFormat()
        {
            var go = Track(new GameObject("__mcp_instance_id_6000_2"));
            var token = McpObjectReference.ToJToken(go);
            var resolved = McpObjectReference.ToGameObject(token);

            Assert.AreSame(go, resolved);
            Assert.AreEqual(UnityEngine.Object.GetInstanceID(go), token.Value<int>());
        }
    }
}
