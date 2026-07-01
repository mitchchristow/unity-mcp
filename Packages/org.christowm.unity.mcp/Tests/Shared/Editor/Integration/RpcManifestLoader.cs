using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp.Tests.Editor.Shared
{
  /// <summary>
  /// Loads RPC manifest JSON from Tests/Fixtures.
  /// </summary>
  public static class RpcManifestLoader
  {
    public static string FixturesRoot =>
      Path.GetFullPath(Path.Combine(
        Application.dataPath,
        "..",
        "Packages",
        "org.christowm.unity.mcp",
        "Tests",
        "Fixtures"));

    public static JObject LoadSharedManifest() =>
      JObject.Parse(File.ReadAllText(Path.Combine(FixturesRoot, "rpc-manifest.shared.json")));

    public static JObject LoadVersionManifest(string unityLine)
    {
      var path = Path.Combine(FixturesRoot, "versions", $"{unityLine}.manifest.json");
      return File.Exists(path)
        ? JObject.Parse(File.ReadAllText(path))
        : new JObject { ["entries"] = new JArray() };
    }

    public static IEnumerable<JToken> GetRunnableEntries(JObject manifest, bool smokeOnly = false)
    {
      var entries = manifest["entries"] as JArray;
      if (entries == null)
        yield break;

      foreach (var entry in entries)
      {
        if (smokeOnly)
        {
          var tags = entry["tags"] as JArray;
          if (tags == null || !tags.ToString().Contains("smoke"))
            continue;
        }

        if (entry["skip"]?.Value<bool>() == true)
          continue;

        yield return entry;
      }
    }
  }
}
