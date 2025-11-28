using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Models
{
    [Serializable]
    public class RpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JToken Params { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }
    }
}
