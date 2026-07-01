using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Tests.Editor.Shared
{
  public static class RpcManifestRunner
  {
    public static JObject ExecuteEntry(JToken entry)
    {
      var method = entry["method"]?.ToString();
      var parameters = entry["params"] as JObject ?? new JObject();
      return RpcTestHarness.Invoke(method, parameters);
    }

    public static void CleanupEntry(JToken entry, JObject result)
    {
      var cleanup = entry["cleanup"]?.ToString();
      if (string.IsNullOrEmpty(cleanup) || result == null)
        return;

      switch (cleanup)
      {
        case "destroyById":
          var go = McpObjectReference.ToGameObject(result["id"]);
          if (go != null)
            Object.DestroyImmediate(go);
          break;
        case "deleteAssetByPath":
          var path = result["path"]?.ToString();
          if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
          break;
      }
    }

    public static void AssertExpectations(JToken entry, JObject result)
    {
      var expect = entry["expect"] as JObject;
      if (expect == null)
        return;

      var hasKeys = expect["hasKeys"] as JArray;
      if (hasKeys != null)
      {
        foreach (var key in hasKeys)
        {
          Assert.IsTrue(result.ContainsKey(key.ToString()), $"Missing key '{key}' in result for {entry["id"]}");
        }
      }
    }
  }
}
