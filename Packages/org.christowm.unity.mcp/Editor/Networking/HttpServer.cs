#if UNITY_EDITOR
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Editor.Networking
{
    public class HttpServer
    {
        private HttpListener _listener;
        private readonly string _url;
        private bool _isRunning;

        public HttpServer(int port = 17890)
        {
            _url = $"http://localhost:{port}/mcp/rpc/";
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(_url);
                _listener.Start();
                _isRunning = true;
                
                Debug.Log($"[MCP] HTTP Server started at {_url}");
                
                Task.Run(HandleConnections);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start HTTP server: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            _listener = null;
            Debug.Log("[MCP] HTTP Server stopped.");
        }

        private async Task HandleConnections()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    ProcessRequest(ctx);
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                }
                catch (ObjectDisposedException)
                {
                    // Listener disposed
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MCP] Error accepting connection: {ex}");
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext ctx)
        {
            try
            {
                if (ctx.Request.HttpMethod != "POST")
                {
                    ctx.Response.StatusCode = 405; // Method Not Allowed
                    ctx.Response.Close();
                    return;
                }

                string body;
                using (var reader = new System.IO.StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                // Dispatch to JSON-RPC handler
                // We need to run this on the main thread if it touches Unity API
                // But for now, let's assume the Dispatcher handles threading or we use a MainThreadDispatcher
                // Actually, Unity API calls MUST be on main thread.
                // We'll use a simple MainThreadDispatcher pattern or EditorApplication.delayCall if needed.
                // For this MVP, let's just try to run it. If it fails, we'll add MainThreadDispatcher.
                // NOTE: Most Unity API calls will fail if run from this Task.
                // We will implement a simple MainThread execution mechanism in McpServer or here.
                
                string responseJson = null;
                
                // Execute on main thread
                var tcs = new TaskCompletionSource<string>();
                
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    try
                    {
                        var res = JsonRpcDispatcher.Handle(body);
                        tcs.SetResult(res);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                };

                responseJson = await tcs.Task;

                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = buffer.Length;
                await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error processing request: {ex}");
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }
    }
}
#endif
