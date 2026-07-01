using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityMcp.Tests.Editor.Shared
{
    public class RpcManifestTests : EditorTestBase
    {
        private static string FixturesRoot =>
            Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Packages",
                "org.christowm.unity.mcp",
                "Tests",
                "Fixtures"));

        [Test]
        public void SharedManifest_LoadsAndHasEntries()
        {
            var path = Path.Combine(FixturesRoot, "rpc-manifest.shared.json");
            Assert.IsTrue(File.Exists(path), $"Missing manifest: {path}");

            var manifest = JObject.Parse(File.ReadAllText(path));
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
        public void SharedManifest_CreateObjectEntry_Executes()
        {
            var path = Path.Combine(FixturesRoot, "rpc-manifest.shared.json");
            var manifest = JObject.Parse(File.ReadAllText(path));
            var entries = manifest["entries"] as JArray;

            JToken target = null;
            foreach (var entry in entries)
            {
                if (entry["id"]?.ToString() == "hierarchy_create_object")
                {
                    target = entry;
                    break;
                }
            }

            Assert.IsNotNull(target, "Expected hierarchy_create_object entry in shared manifest");

            var result = RpcTestHarness.Invoke(
                target["method"].ToString(),
                target["params"] as JObject ?? new JObject());

            var go = Track(UnityMcp.Editor.MCP.McpObjectReference.ToGameObject(result["id"]));
            Assert.IsNotNull(go);
            Assert.AreEqual("__mcp_test_object", go.name);
        }
    }
}
