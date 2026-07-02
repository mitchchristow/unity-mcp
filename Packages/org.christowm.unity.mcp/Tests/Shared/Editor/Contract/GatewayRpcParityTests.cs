using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Tests.Editor.Shared
{
  [Category("Contract")]
  public class GatewayRpcParityTests
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      McpControllerRegistry.RegisterAll();
    }

    [Test]
    public void GatewayRpcMap_AllMappedMethods_AreRegisteredInUnity()
  {
    var mapPath = Path.Combine(RpcManifestLoader.GatewayFixturesRoot, "gateway-rpc-map.json");
    Assert.IsTrue(File.Exists(mapPath), $"Missing gateway contract map: {mapPath}");

    var map = JObject.Parse(File.ReadAllText(mapPath));
    var rpcMethods = map["rpcMethods"] as JArray;
    Assert.IsNotNull(rpcMethods);
    Assert.GreaterOrEqual(rpcMethods.Count, 75);

    var registered = new HashSet<string>(JsonRpcDispatcher.GetRegisteredMethodNames());

    foreach (var rpc in rpcMethods)
    {
      var method = rpc.ToString();
      Assert.IsTrue(registered.Contains(method), $"Gateway maps to '{method}' but Unity has no RPC handler");
    }
  }

  [Test]
  public void GatewayRpcMap_HasOneToOneToolMapping()
  {
    var mapPath = Path.Combine(RpcManifestLoader.GatewayFixturesRoot, "gateway-rpc-map.json");
    var map = JObject.Parse(File.ReadAllText(mapPath));
    var entries = map["entries"] as JArray;

    Assert.AreEqual(map["mappedToolCount"]?.Value<int>(), entries?.Count);
    Assert.AreEqual(entries?.Count, map["rpcMethodCount"]?.Value<int>(),
      "Phase 2 contract expects each MCP tool to map to a distinct RPC method");
    }
  }
}
