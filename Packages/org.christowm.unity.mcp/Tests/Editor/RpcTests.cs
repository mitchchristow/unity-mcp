using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityMcp.Editor.MCP;
using UnityMcp.Editor.MCP.Rpc.Models;
using Newtonsoft.Json;

namespace UnityMcp.Tests.Editor
{
    public class RpcTests
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
            string response = JsonRpcDispatcher.Handle("invalid json");
            var respObj = JsonConvert.DeserializeObject<RpcResponse>(response);
            
            Assert.IsNotNull(respObj.Error);
            Assert.AreEqual(-32700, respObj.Error.Code);
        }

        [Test]
        public void Dispatcher_ReturnsError_OnMethodNotFound()
        {
            string json = "{\"jsonrpc\": \"2.0\", \"method\": \"non_existent_method\", \"params\": {}, \"id\": 1}";
            string response = JsonRpcDispatcher.Handle(json);
            var respObj = JsonConvert.DeserializeObject<RpcResponse>(response);
            
            Assert.IsNotNull(respObj.Error);
            Assert.AreEqual(-32601, respObj.Error.Code);
        }
    }
}
