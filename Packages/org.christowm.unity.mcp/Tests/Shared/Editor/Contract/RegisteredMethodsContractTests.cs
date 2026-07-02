using System.Linq;
using NUnit.Framework;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Tests.Editor.Shared
{
  [Category("Contract")]
  public class RegisteredMethodsContractTests
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      McpControllerRegistry.RegisterAll();
    }

    [Test]
    public void RegisterAll_ExposesCoreRpcMethods()
    {
      var methods = JsonRpcDispatcher.GetRegisteredMethodNames();
      Assert.GreaterOrEqual(methods.Count, 100);
      Assert.Contains("unity.create_primitive", methods.ToList());
      Assert.Contains("unity.set_material", methods.ToList());
      Assert.Contains("unity.get_object_details", methods.ToList());
    }
  }
}
