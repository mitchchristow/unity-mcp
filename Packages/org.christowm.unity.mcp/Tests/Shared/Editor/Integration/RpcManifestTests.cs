using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityMcp.Tests.Editor.Shared
{
  public class RpcManifestTests : EditorTestBase
  {
    [Test]
    public void SharedManifest_LoadsAndHasEntries()
    {
      var manifest = RpcManifestLoader.LoadSharedManifest();
      var entries = manifest["entries"] as JArray;

      Assert.IsNotNull(entries);
      Assert.Greater(entries.Count, 0);

      foreach (var entry in entries)
      {
        Assert.IsFalse(string.IsNullOrEmpty(entry["id"]?.ToString()));
        Assert.IsFalse(string.IsNullOrEmpty(entry["method"]?.ToString()));
      }
    }

    [Test]
    public void SharedManifest_AllRunnableEntries_ExecuteSuccessfully()
    {
      var manifest = RpcManifestLoader.LoadSharedManifest();

      foreach (var entry in RpcManifestLoader.GetRunnableEntries(manifest))
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
    public void SharedManifest_SmokeTaggedEntries_Exist()
    {
      var manifest = RpcManifestLoader.LoadSharedManifest();
      var smokeEntries = RpcManifestLoader.GetRunnableEntries(manifest, smokeOnly: true);
      var count = 0;
      foreach (var _ in smokeEntries)
        count++;
      Assert.GreaterOrEqual(count, 1);
    }
  }
}
