using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Tests.Editor.Shared
{
  [Category("Smoke")]
  public class BlueSphereSmokeTests : EditorTestBase
  {
    [Test]
    public void BlueSphere_CreateTransformMaterialAndVerify()
    {
      var sphere = RpcTestHarness.Invoke("unity.create_primitive", new JObject
      {
        ["type"] = "Sphere",
        ["name"] = "__mcp_smoke_sphere"
      });
      Track(McpObjectReference.ToGameObject(sphere["id"]));

      RpcTestHarness.Invoke("unity.set_transform", new JObject
      {
        ["id"] = sphere["id"],
        ["position"] = new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 0 },
        ["scale"] = new JObject { ["x"] = 5, ["y"] = 5, ["z"] = 5 }
      });

      var material = RpcTestHarness.Invoke("unity.create_material", new JObject
      {
        ["name"] = "__mcp_smoke_blue_mat"
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
        ["id"] = sphere["id"],
        ["path"] = material["path"]
      });

      var details = RpcTestHarness.Invoke("unity.get_object_details", new JObject { ["id"] = sphere["id"] });
      Assert.AreEqual("__mcp_smoke_sphere", details["name"]?.ToString());

      var transform = details["components"]?[0];
      Assert.AreEqual(5f, transform?["scale"]?["x"]?.Value<float>() ?? 0f, 0.01f);

      RpcManifestRunner.CleanupEntry(
        new JObject { ["cleanup"] = "deleteAssetByPath" },
        material);
    }
  }
}
