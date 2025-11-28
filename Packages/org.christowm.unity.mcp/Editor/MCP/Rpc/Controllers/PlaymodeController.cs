using Newtonsoft.Json.Linq;
using UnityEditor;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class PlaymodeController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.play", Play);
            JsonRpcDispatcher.RegisterMethod("unity.stop", Stop);
            JsonRpcDispatcher.RegisterMethod("unity.pause", Pause);
        }

        private static JObject Play(JObject p)
        {
            EditorApplication.EnterPlaymode();
            return new JObject { ["ok"] = true };
        }

        private static JObject Stop(JObject p)
        {
            EditorApplication.ExitPlaymode();
            return new JObject { ["ok"] = true };
        }

        private static JObject Pause(JObject p)
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
            return new JObject { ["paused"] = EditorApplication.isPaused };
        }
    }
}
