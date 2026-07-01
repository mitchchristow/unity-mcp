using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor.MCP;
using UnityMcp.Tests.Editor.Shared;

namespace UnityMcp.Tests.Editor.V6000_5
{
    /// <summary>
    /// EntityId / ulong wire-format tests for Unity 6.5.x only.
    /// Guards against OverflowException regressions when resolving MCP object IDs.
    /// </summary>
    public class McpObjectReferenceEntityIdTests : EditorTestBase
    {
        [Test]
        public void EntityId_RoundTripsThroughWireFormat()
        {
            var go = Track(new GameObject("__mcp_entity_id_6000_5"));
            var token = McpObjectReference.ToJToken(go);
            var resolved = McpObjectReference.ToGameObject(token);

            Assert.AreSame(go, resolved);
            Assert.AreNotEqual(JTokenType.Null, token.Type);
        }

        [Test]
        public void SetMaterial_AcceptsWireIdFromCreatePrimitive()
        {
            var create = RpcTestHarness.Invoke("unity.create_primitive", new JObject
            {
                ["type"] = "Sphere",
                ["name"] = "__mcp_entity_material_sphere"
            });

            var material = RpcTestHarness.Invoke("unity.create_material", new JObject
            {
                ["name"] = "__mcp_entity_test_mat"
            });

            RpcTestHarness.Invoke("unity.set_material_property", new JObject
            {
                ["path"] = material["path"],
                ["color"] = new JObject
                {
                    ["name"] = "_Color",
                    ["r"] = 0,
                    ["g"] = 0,
                    ["b"] = 1,
                    ["a"] = 1
                }
            });

            RpcTestHarness.Invoke("unity.set_material", new JObject
            {
                ["id"] = create["id"],
                ["path"] = material["path"]
            });

            var details = RpcTestHarness.Invoke("unity.get_object_details", new JObject { ["id"] = create["id"] });
            Assert.AreEqual("__mcp_entity_material_sphere", details["name"]?.ToString());

            DestroyByWireId(create["id"]);
        }
    }
}
