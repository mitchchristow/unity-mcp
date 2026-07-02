using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace UnityMcp.Tests.Editor.Shared
{
  public class RpcManifestTests : EditorTestBase
  {
    [Test]
    public void SharedManifest_LoadsAndHasEntries()
    {
      var manifest = RpcManifestLoader.LoadSharedManifest();
      var entries = manifest["entries"] as Newtonsoft.Json.Linq.JArray;

      Assert.IsNotNull(entries);
      Assert.Greater(entries.Count, 0);

      foreach (var entry in entries)
      {
        Assert.IsFalse(string.IsNullOrEmpty(entry["id"]?.ToString()));
        Assert.IsFalse(string.IsNullOrEmpty(entry["method"]?.ToString()));
      }
    }

    [Test]
    public void ReadOnlyManifest_HasEntries()
    {
      var manifest = RpcManifestLoader.LoadReadOnlyManifest();
      var entries = manifest["entries"] as Newtonsoft.Json.Linq.JArray;
      Assert.IsNotNull(entries);
      Assert.GreaterOrEqual(entries.Count, 50);
    }

    [Test]
    public void AllSharedManifestEntries_ExecuteSuccessfully()
    {
      foreach (var entry in RpcManifestLoader.GetAllSharedRunnableEntries())
      {
        JObject result = null;
        try
        {
          result = RpcManifestRunner.ExecuteEntry(entry);
          RpcManifestRunner.AssertExpectations(entry, result);

          if (entry["cleanup"]?.ToString() == "destroyById" && result?["id"] != null)
            Track(UnityMcp.Editor.MCP.McpObjectReference.ToGameObject(result["id"]));
        }
        finally
        {
          if (result != null)
            RpcManifestRunner.CleanupEntry(entry, result);
        }
      }
    }

    [Test]
    public void MutatingManifest_HasEntries()
    {
      var manifest = RpcManifestLoader.LoadMutatingManifest();
      var entries = manifest["entries"] as JArray;
      Assert.IsNotNull(entries);
      Assert.GreaterOrEqual(entries.Count, 5);
    }

    [Test]
    public void MutatingManifestEntries_ExecuteInOrder_WithReferences()
    {
      RpcManifestRunner.RunManifest(RpcManifestLoader.LoadMutatingManifest());
    }

    [Test]
    public void PerControllerMutatingManifestFiles_ExecuteInOrder_WithReferences()
    {
      foreach (var manifest in RpcManifestLoader.LoadPerControllerMutatingManifests())
        RpcManifestRunner.RunManifest(manifest);
    }

    [Test]
    public void ReadOnlyManifestEntries_ExecuteSuccessfully()
    {
      foreach (var entry in RpcManifestLoader.GetRunnableEntries(
        RpcManifestLoader.LoadReadOnlyManifest(), tagFilter: "readonly"))
      {
        RpcManifestRunner.ExecuteEntry(entry);
      }
    }

    [Test]
    public void SmokeTaggedEntries_Exist()
    {
      var count = 0;
      foreach (var _ in RpcManifestLoader.GetAllSharedRunnableEntries(smokeOnly: true))
        count++;
      Assert.GreaterOrEqual(count, 1);
    }
  }
}
