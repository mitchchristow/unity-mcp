using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Models
{
    [Serializable]
    public class RpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public RpcError Error { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }
    }

    [Serializable]
    public class RpcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }

        public RpcError(int code, string message, object data = null)
        {
            Code = code;
            Message = message;
            Data = data;
        }
    }
}
