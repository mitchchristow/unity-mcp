using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityMcp.Editor.MCP;
using UnityMcp.Editor.MCP.Rpc.Models;

namespace UnityMcp.Tests.Editor.Shared
{
    public class JsonRpcDispatcherTests
    {
        [Test]
        public void Dispatcher_CanParseRequest()
        {
            string json = "{\"jsonrpc\": \"2.0\", \"method\": \"test\", \"params\": {}, \"id\": 1}";
            var request = JsonConvert.DeserializeObject<RpcRequest>(json);

            Assert.AreEqual("2.0", request.JsonRpc);
            Assert.AreEqual("test", request.Method);
            Assert.AreEqual(1, int.Parse(request.Id.ToString()));
        }

        [Test]
        public void Dispatcher_ReturnsError_OnInvalidJson()
        {
            string response = RpcTestHarness.InvokeRaw("invalid json");
            var respObj = JsonConvert.DeserializeObject<RpcResponse>(response);

            Assert.IsNotNull(respObj.Error);
            Assert.AreEqual(-32700, respObj.Error.Code);
        }

        [Test]
        public void Dispatcher_ReturnsError_OnMethodNotFound()
        {
            string json = "{\"jsonrpc\": \"2.0\", \"method\": \"non_existent_method\", \"params\": {}, \"id\": 1}";
            string response = RpcTestHarness.InvokeRaw(json);
            var respObj = JsonConvert.DeserializeObject<RpcResponse>(response);

            Assert.IsNotNull(respObj.Error);
            Assert.AreEqual(-32601, respObj.Error.Code);
        }
    }
}
