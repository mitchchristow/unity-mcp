using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityMcp.Editor.MCP.Rpc.Models;

namespace UnityMcp.Editor.MCP
{
    public static class JsonRpcDispatcher
    {
        private static readonly Dictionary<string, Func<JObject, object>> _methods = new Dictionary<string, Func<JObject, object>>();

        public static void RegisterMethod(string name, Func<JObject, object> handler)
        {
            if (_methods.ContainsKey(name))
            {
                Debug.LogWarning($"[MCP] Method '{name}' is already registered. Overwriting.");
            }
            _methods[name] = handler;
        }

        public static string Handle(string requestBody)
        {
            RpcRequest request = null;
            try
            {
                request = JsonConvert.DeserializeObject<RpcRequest>(requestBody);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(null, -32700, "Parse error", ex.Message);
            }

            if (request == null || string.IsNullOrEmpty(request.Method))
            {
                return CreateErrorResponse(request?.Id, -32600, "Invalid Request");
            }

            if (!_methods.TryGetValue(request.Method, out var handler))
            {
                return CreateErrorResponse(request.Id, -32601, "Method not found");
            }

            try
            {
                JObject paramsObj = request.Params as JObject;
                // If params is null (e.g. no params sent), pass an empty JObject or null depending on handler expectation
                // For safety, let's pass null if it's not a JObject (could be JArray, but we mainly support named params for now)
                
                var result = handler(paramsObj);
                
                return JsonConvert.SerializeObject(new RpcResponse
                {
                    Id = request.Id,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error executing {request.Method}: {ex}");
                return CreateErrorResponse(request.Id, -32000, "Server error", ex.Message);
            }
        }

        private static string CreateErrorResponse(object id, int code, string message, string data = null)
        {
            return JsonConvert.SerializeObject(new RpcResponse
            {
                Id = id,
                Error = new RpcError(code, message, data)
            });
        }
    }
}
