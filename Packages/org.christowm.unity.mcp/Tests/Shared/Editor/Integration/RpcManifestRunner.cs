using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor.MCP;
using System.Collections.Generic;

namespace UnityMcp.Tests.Editor.Shared
{
  public static class RpcManifestRunner
  {
    public static JObject ExecuteEntry(JToken entry, IDictionary<string, JObject> context = null)
    {
      var method = entry["method"]?.ToString();
      var rawParams = entry["params"] as JObject ?? new JObject();
      var parameters = ResolveReferences(rawParams, context) as JObject ?? new JObject();
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

    private static JToken ResolveReferences(JToken token, IDictionary<string, JObject> context)
    {
      if (token == null || context == null)
        return token;

      if (token.Type == JTokenType.String)
      {
        var value = token.Value<string>();
        if (string.IsNullOrEmpty(value) || !value.StartsWith("$"))
          return token;

        var path = value.Substring(1).Split('.');
        if (path.Length < 2)
          throw new System.Exception($"Invalid reference format: {value}");

        var entryId = path[0];
        if (!context.TryGetValue(entryId, out var referenced))
          throw new System.Exception($"Unknown reference '{entryId}' in {value}");

        JToken resolved = referenced;
        for (int i = 1; i < path.Length; i++)
        {
          resolved = resolved?[path[i]];
          if (resolved == null)
            throw new System.Exception($"Reference path '{value}' not found");
        }
        return resolved.DeepClone();
      }

      if (token.Type == JTokenType.Object)
      {
        var obj = new JObject();
        foreach (var prop in (JObject)token)
          obj[prop.Key] = ResolveReferences(prop.Value, context);
        return obj;
      }

      if (token.Type == JTokenType.Array)
      {
        var arr = new JArray();
        foreach (var item in (JArray)token)
          arr.Add(ResolveReferences(item, context));
        return arr;
      }

      return token.DeepClone();
    }
  }
}
