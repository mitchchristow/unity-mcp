using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp.Tests.Editor.Shared
{
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

    public static string GatewayFixturesRoot =>
      Path.GetFullPath(Path.Combine(Application.dataPath, "..", "gateway", "tests", "fixtures"));

    public static JObject LoadManifest(string fileName) =>
      JObject.Parse(File.ReadAllText(Path.Combine(FixturesRoot, fileName)));

    public static JObject LoadSharedManifest() => LoadManifest("rpc-manifest.shared.json");

    public static JObject LoadReadOnlyManifest() => LoadManifest("rpc-manifest.readonly.json");
    public static JObject LoadMutatingManifest() => LoadManifest("rpc-manifest.mutating.json");

    public static IEnumerable<JObject> LoadAllMutatingManifests()
    {
      yield return LoadMutatingManifest();
      foreach (var manifest in LoadPerControllerMutatingManifests())
        yield return manifest;
    }

    public static IEnumerable<JObject> LoadPerControllerMutatingManifests()
    {
      var mutatingDir = Path.Combine(FixturesRoot, "mutating");
      if (!Directory.Exists(mutatingDir))
        yield break;

      foreach (var file in Directory.GetFiles(mutatingDir, "*.json").OrderBy(Path.GetFileName))
        yield return JObject.Parse(File.ReadAllText(file));
    }

    public static JObject LoadVersionManifest(string unityLine)
    {
      var path = Path.Combine(FixturesRoot, "versions", $"{unityLine}.manifest.json");
      return File.Exists(path)
        ? JObject.Parse(File.ReadAllText(path))
        : new JObject { ["entries"] = new JArray() };
    }

    public static IEnumerable<JObject> LoadAllSharedManifests()
    {
      yield return LoadSharedManifest();
      yield return LoadReadOnlyManifest();
    }

    public static IEnumerable<JToken> GetRunnableEntries(JObject manifest, bool smokeOnly = false, string tagFilter = null)
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

        if (!string.IsNullOrEmpty(tagFilter))
        {
          var tags = entry["tags"] as JArray;
          if (tags == null || !tags.ToString().Contains(tagFilter))
            continue;
        }

        if (entry["skip"]?.Value<bool>() == true)
          continue;

        yield return entry;
      }
    }

    public static IEnumerable<JToken> GetAllSharedRunnableEntries(bool smokeOnly = false, string tagFilter = null)
    {
      foreach (var manifest in LoadAllSharedManifests())
      {
        foreach (var entry in GetRunnableEntries(manifest, smokeOnly, tagFilter))
          yield return entry;
      }
    }
  }
}
