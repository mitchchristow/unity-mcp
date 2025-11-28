#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMcp.Editor.Networking
{
    public class WebSocketServer
    {
        private HttpListener _listener;
        private readonly string _url;
        private bool _isRunning;
        private readonly ConcurrentDictionary<int, WebSocket> _clients = new ConcurrentDictionary<int, WebSocket>();
        private int _clientIdCounter;

        public WebSocketServer(int port = 17891)
        {
            _url = $"http://localhost:{port}/mcp/events/";
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
                
                Debug.Log($"[MCP] WebSocket Event Server started at {_url.Replace("http", "ws")}");
                
                Task.Run(HandleConnections);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start WebSocket server: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            foreach (var client in _clients.Values)
            {
                if (client.State == WebSocketState.Open)
                    client.Abort();
            }
            _clients.Clear();
            
            _listener?.Stop();
            _listener?.Close();
            _listener = null;
        }

        public async Task BroadcastEvent(string eventName, object data)
        {
            if (_clients.IsEmpty) return;

            var message = new
            {
                type = "event",
                @event = eventName,
                data = data,
                timestamp = DateTime.UtcNow
            };

            string json = JsonConvert.SerializeObject(message);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(buffer);

            foreach (var kvp in _clients)
            {
                var ws = kvp.Value;
                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        // Client disconnected, will be cleaned up in loop
                    }
                }
            }
        }

        private async Task HandleConnections()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    if (ctx.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(ctx);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 400;
                        ctx.Response.Close();
                    }
                }
                catch (HttpListenerException) { }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Debug.LogError($"[MCP] Error accepting WebSocket connection: {ex}");
                }
            }
        }

        private async void ProcessWebSocketRequest(HttpListenerContext ctx)
        {
            WebSocketContext wsContext = null;
            try
            {
                wsContext = await ctx.AcceptWebSocketAsync(subProtocol: null);
                int id = Interlocked.Increment(ref _clientIdCounter);
                _clients[id] = wsContext.WebSocket;
                
                // Debug.Log($"[MCP] Client {id} connected to event stream.");

                await ReceiveLoop(id, wsContext.WebSocket);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] WebSocket error: {ex}");
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }

        private async Task ReceiveLoop(int id, WebSocket ws)
        {
            var buffer = new byte[1024];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                }
            }
            catch
            {
                // Connection lost
            }
            finally
            {
                _clients.TryRemove(id, out _);
                // Debug.Log($"[MCP] Client {id} disconnected.");
            }
        }
    }
}
#endif
