#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Editor.Networking
{
    public class HttpServer
    {
        private HttpListener _listener;
        private readonly string _url;
        private bool _isRunning;
        
        // Request queue for main thread processing
        private static readonly Queue<PendingRequest> _requestQueue = new Queue<PendingRequest>();
        private static readonly object _queueLock = new object();
        private static bool _updateRegistered = false;

        private class PendingRequest
        {
            public string Body;
            public TaskCompletionSource<string> Tcs;
        }

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
                
                // Register update handler for processing queue
                if (!_updateRegistered)
                {
                    EditorApplication.update += ProcessRequestQueue;
                    _updateRegistered = true;
                }
                
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
            
            if (_updateRegistered)
            {
                EditorApplication.update -= ProcessRequestQueue;
                _updateRegistered = false;
            }
            
            // Clear any pending requests
            lock (_queueLock)
            {
                while (_requestQueue.Count > 0)
                {
                    var pending = _requestQueue.Dequeue();
                    pending.Tcs.TrySetException(new Exception("Server stopped"));
                }
            }
            
            Debug.Log("[MCP] HTTP Server stopped.");
        }

        private static void ProcessRequestQueue()
        {
            // Process all pending requests in the queue
            while (true)
            {
                PendingRequest request;
                lock (_queueLock)
                {
                    if (_requestQueue.Count == 0) break;
                    request = _requestQueue.Dequeue();
                }

                try
                {
                    var result = JsonRpcDispatcher.Handle(request.Body);
                    request.Tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    request.Tcs.TrySetException(ex);
                }
            }
        }

        private static void ForceEditorUpdate()
        {
            // Force Unity to process updates even when in background
            // This wakes up the editor to process our queued requests
            try
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            catch
            {
                // Fallback: ignored if not available
            }
        }

        private async Task HandleConnections()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(ctx); // Fire and forget
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

        private async Task ProcessRequestAsync(HttpListenerContext ctx)
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

                // Create pending request and add to queue
                var tcs = new TaskCompletionSource<string>();
                var pendingRequest = new PendingRequest { Body = body, Tcs = tcs };
                
                lock (_queueLock)
                {
                    _requestQueue.Enqueue(pendingRequest);
                }
                
                // Force Unity to wake up and process the queue
                ForceEditorUpdate();

                // Wait for result with timeout
                var timeoutTask = Task.Delay(30000); // 30 second timeout
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                string responseJson;
                if (completedTask == timeoutTask)
                {
                    responseJson = "{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32000,\"message\":\"Request timeout - Unity Editor may be unresponsive\"},\"id\":null}";
                }
                else
                {
                    responseJson = await tcs.Task;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = buffer.Length;
                await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error processing request: {ex}");
                try
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.Close();
                }
                catch
                {
                    // Response may already be closed
                }
            }
        }
    }
}
#endif
