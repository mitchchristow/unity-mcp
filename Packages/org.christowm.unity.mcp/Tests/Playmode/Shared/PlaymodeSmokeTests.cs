using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityMcp.Tests.Playmode.Shared
{
    public class PlaymodeSmokeTests
    {
        [UnityTest]
        public IEnumerator PlayThenStop_ViaRpc_EntersAndExitsPlayMode()
        {
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();

            yield return null;

            var playResult = PlaymodeRpcTestHarness.Invoke("unity.play");
            Assert.IsTrue(playResult["ok"]?.Value<bool>() ?? false);

            yield return new WaitUntil(() => EditorApplication.isPlaying);

            var stopResult = PlaymodeRpcTestHarness.Invoke("unity.stop");
            Assert.IsTrue(stopResult["ok"]?.Value<bool>() ?? false);

            yield return new WaitUntil(() => !EditorApplication.isPlaying);
        }
    }
}
