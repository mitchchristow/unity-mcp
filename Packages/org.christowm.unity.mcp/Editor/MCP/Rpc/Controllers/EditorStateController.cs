using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    public static class EditorStateController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_selection", GetSelection);
            JsonRpcDispatcher.RegisterMethod("unity.get_project_info", GetProjectInfo);
        }

        private static JObject GetSelection(JObject p)
        {
            var selection = Selection.gameObjects;
            var list = new JArray();

            foreach (var go in selection)
            {
                list.Add(new JObject
                {
                    ["id"] = go.GetInstanceID(),
                    ["name"] = go.name,
                    ["position"] = new JObject 
                    { 
                        ["x"] = go.transform.position.x, 
                        ["y"] = go.transform.position.y, 
                        ["z"] = go.transform.position.z 
                    }
                });
            }

            return new JObject { ["selection"] = list };
        }

        private static JObject GetProjectInfo(JObject p)
        {
            return new JObject
            {
                ["unityVersion"] = Application.unityVersion,
                ["platform"] = Application.platform.ToString(),
                ["projectPath"] = Application.dataPath.Replace("/Assets", ""),
                ["isPlaying"] = Application.isPlaying,
                ["isCompiling"] = EditorApplication.isCompiling
            };
        }
    }
}
