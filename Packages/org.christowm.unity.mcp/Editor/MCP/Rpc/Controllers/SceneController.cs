using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class SceneController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.list_scenes", ListScenes);
            JsonRpcDispatcher.RegisterMethod("unity.open_scene", OpenScene);
            JsonRpcDispatcher.RegisterMethod("unity.save_scene", SaveScene);
        }

        private static JObject ListScenes(JObject p)
        {
            var scenes = new JArray();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes.Add(new JObject
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded,
                    ["isDirty"] = scene.isDirty
                });
            }
            return new JObject { ["scenes"] = scenes };
        }

        private static JObject OpenScene(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path)) throw new System.Exception("Path is required");

            var scene = EditorSceneManager.OpenScene(path);
            return new JObject { ["ok"] = scene.IsValid() };
        }

        private static JObject SaveScene(JObject p)
        {
            EditorSceneManager.SaveOpenScenes();
            return new JObject { ["ok"] = true };
        }
    }
}
