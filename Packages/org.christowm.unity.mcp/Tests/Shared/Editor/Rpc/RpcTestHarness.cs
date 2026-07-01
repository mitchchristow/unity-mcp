using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityMcp.Editor.MCP;
using UnityMcp.Editor.MCP.Rpc.Models;

namespace UnityMcp.Tests.Editor.Shared
{
    /// <summary>
    /// Invokes JSON-RPC methods in-process for EditMode integration tests.
    /// </summary>
    public static class RpcTestHarness
    {
        public static void EnsureRegistered()
        {
            McpControllerRegistry.RegisterAll();
        }

        public static JObject Invoke(string method, JObject parameters = null, object requestId = 1)
        {
            EnsureRegistered();

            var request = JsonConvert.SerializeObject(new RpcRequest
            {
                JsonRpc = "2.0",
                Method = method,
                Params = parameters ?? new JObject(),
                Id = requestId
            });

            var responseJson = JsonRpcDispatcher.Handle(request);
            var response = JsonConvert.DeserializeObject<RpcResponse>(responseJson);

            if (response.Error != null)
            {
                throw new System.Exception(
                    $"RPC '{method}' failed ({response.Error.Code}): {response.Error.Message} {response.Error.Data}");
            }

            if (response.Result is JObject jobj)
                return jobj;

            if (response.Result == null)
                return new JObject();

            return JObject.FromObject(response.Result);
        }

        public static string InvokeRaw(string requestJson)
        {
            EnsureRegistered();
            return JsonRpcDispatcher.Handle(requestJson);
        }
    }
}
