#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMcp.Editor.Networking
{
    /// <summary>
    /// Simple WebSocket server using raw TCP sockets to avoid HttpListener WebSocket issues on Windows.
    /// </summary>
    public class WebSocketServer
    {
        private TcpListener _listener;
        private readonly int _port;
        private bool _isRunning;
        private readonly ConcurrentDictionary<int, WebSocketClient> _clients = new ConcurrentDictionary<int, WebSocketClient>();
        private int _clientIdCounter;
        private CancellationTokenSource _cts;

        public bool IsRunning => _isRunning;
        public int ClientCount => _clients.Count;

        public WebSocketServer(int port = 17891)
        {
            _port = port;
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                _isRunning = true;
                
                Debug.Log($"[MCP] WebSocket Event Server started at ws://localhost:{_port}/mcp/events/");
                
                Task.Run(() => AcceptClients(_cts.Token));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start WebSocket server: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
            
            foreach (var client in _clients.Values)
            {
                client.Close();
            }
            _clients.Clear();
            
            _listener?.Stop();
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
            
            var deadClients = new List<int>();
            
            foreach (var kvp in _clients)
            {
                try
                {
                    await kvp.Value.SendAsync(json);
                }
                catch
                {
                    deadClients.Add(kvp.Key);
                }
            }
            
            // Clean up dead clients
            foreach (var id in deadClients)
            {
                if (_clients.TryRemove(id, out var client))
                {
                    client.Close();
                }
            }
        }

        private async Task AcceptClients(CancellationToken ct)
        {
            while (_isRunning && !ct.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(tcpClient, ct), ct);
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Debug.LogError($"[MCP WebSocket] Accept error: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(TcpClient tcpClient, CancellationToken ct)
        {
            WebSocketClient wsClient = null;
            int clientId = -1;
            
            try
            {
                var stream = tcpClient.GetStream();
                
                // Read HTTP request
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                // Parse request
                if (!request.StartsWith("GET"))
                {
                    await SendHttpResponse(stream, 400, "Bad Request");
                    tcpClient.Close();
                    return;
                }
                
                // Check path
                var pathMatch = Regex.Match(request, @"GET\s+(/[^\s]*)\s+HTTP");
                string path = pathMatch.Success ? pathMatch.Groups[1].Value.TrimEnd('/') : "";
                
                if (path != "/mcp/events")
                {
                    await SendHttpResponse(stream, 404, "Not Found");
                    tcpClient.Close();
                    return;
                }
                
                // Check for WebSocket upgrade
                if (!request.Contains("Upgrade: websocket"))
                {
                    await SendHttpResponse(stream, 400, "WebSocket upgrade required");
                    tcpClient.Close();
                    return;
                }
                
                // Get WebSocket key
                var keyMatch = Regex.Match(request, @"Sec-WebSocket-Key:\s*(.+?)\r\n");
                if (!keyMatch.Success)
                {
                    await SendHttpResponse(stream, 400, "Missing WebSocket key");
                    tcpClient.Close();
                    return;
                }
                
                string wsKey = keyMatch.Groups[1].Value.Trim();
                
                // Calculate accept key
                string acceptKey = ComputeWebSocketAcceptKey(wsKey);
                
                // Send WebSocket handshake response
                string response = 
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Connection: Upgrade\r\n" +
                    $"Sec-WebSocket-Accept: {acceptKey}\r\n" +
                    "\r\n";
                
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, ct);
                
                // Create WebSocket client wrapper
                wsClient = new WebSocketClient(tcpClient, stream);
                clientId = Interlocked.Increment(ref _clientIdCounter);
                _clients[clientId] = wsClient;
                
                Debug.Log($"[MCP WebSocket] Client {clientId} connected");
                
                // Receive loop
                await wsClient.ReceiveLoop(ct);
            }
            catch (Exception ex)
            {
                if (_isRunning && !ct.IsCancellationRequested)
                    Debug.LogWarning($"[MCP WebSocket] Client error: {ex.Message}");
            }
            finally
            {
                if (clientId >= 0)
                {
                    _clients.TryRemove(clientId, out _);
                    Debug.Log($"[MCP WebSocket] Client {clientId} disconnected");
                }
                wsClient?.Close();
                tcpClient?.Close();
            }
        }

        private async Task SendHttpResponse(NetworkStream stream, int statusCode, string message)
        {
            string status = statusCode switch
            {
                400 => "Bad Request",
                404 => "Not Found",
                _ => "Error"
            };
            
            string response = $"HTTP/1.1 {statusCode} {status}\r\nContent-Length: {message.Length}\r\nConnection: close\r\n\r\n{message}";
            byte[] bytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static string ComputeWebSocketAcceptKey(string key)
        {
            const string GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key + GUID));
                return Convert.ToBase64String(hash);
            }
        }
    }

    /// <summary>
    /// Represents a connected WebSocket client with frame encoding/decoding.
    /// </summary>
    internal class WebSocketClient
    {
        private readonly TcpClient _tcp;
        private readonly NetworkStream _stream;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private bool _closed;

        public WebSocketClient(TcpClient tcp, NetworkStream stream)
        {
            _tcp = tcp;
            _stream = stream;
        }

        public async Task SendAsync(string message)
        {
            if (_closed) return;
            
            await _sendLock.WaitAsync();
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(message);
                byte[] frame = EncodeFrame(payload);
                await _stream.WriteAsync(frame, 0, frame.Length);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task ReceiveLoop(CancellationToken ct)
        {
            byte[] buffer = new byte[4096];
            
            while (!_closed && !ct.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (bytesRead == 0)
                    {
                        break; // Connection closed
                    }
                    
                    // Decode frame
                    var (opcode, payload) = DecodeFrame(buffer, bytesRead);
                    
                    if (opcode == 0x8) // Close frame
                    {
                        // Send close response
                        byte[] closeFrame = new byte[] { 0x88, 0x00 };
                        await _stream.WriteAsync(closeFrame, 0, closeFrame.Length, ct);
                        break;
                    }
                    else if (opcode == 0x9) // Ping
                    {
                        // Send pong
                        byte[] pongFrame = EncodeFrame(payload, 0xA);
                        await _stream.WriteAsync(pongFrame, 0, pongFrame.Length, ct);
                    }
                    // Text/binary frames (0x1, 0x2) are ignored - we only broadcast, don't receive
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        public void Close()
        {
            _closed = true;
            try
            {
                _stream?.Close();
                _tcp?.Close();
            }
            catch { }
        }

        private static byte[] EncodeFrame(byte[] payload, byte opcode = 0x1)
        {
            int length = payload.Length;
            byte[] frame;

            if (length < 126)
            {
                frame = new byte[2 + length];
                frame[0] = (byte)(0x80 | opcode); // FIN + opcode
                frame[1] = (byte)length;
                Array.Copy(payload, 0, frame, 2, length);
            }
            else if (length < 65536)
            {
                frame = new byte[4 + length];
                frame[0] = (byte)(0x80 | opcode);
                frame[1] = 126;
                frame[2] = (byte)(length >> 8);
                frame[3] = (byte)(length & 0xFF);
                Array.Copy(payload, 0, frame, 4, length);
            }
            else
            {
                frame = new byte[10 + length];
                frame[0] = (byte)(0x80 | opcode);
                frame[1] = 127;
                for (int i = 0; i < 8; i++)
                {
                    frame[9 - i] = (byte)(length >> (8 * i));
                }
                Array.Copy(payload, 0, frame, 10, length);
            }

            return frame;
        }

        private static (byte opcode, byte[] payload) DecodeFrame(byte[] buffer, int length)
        {
            if (length < 2) return (0, Array.Empty<byte>());

            byte opcode = (byte)(buffer[0] & 0x0F);
            bool masked = (buffer[1] & 0x80) != 0;
            int payloadLength = buffer[1] & 0x7F;
            
            int offset = 2;
            
            if (payloadLength == 126)
            {
                if (length < 4) return (opcode, Array.Empty<byte>());
                payloadLength = (buffer[2] << 8) | buffer[3];
                offset = 4;
            }
            else if (payloadLength == 127)
            {
                if (length < 10) return (opcode, Array.Empty<byte>());
                payloadLength = 0;
                for (int i = 0; i < 8; i++)
                {
                    payloadLength = (payloadLength << 8) | buffer[2 + i];
                }
                offset = 10;
            }

            byte[] mask = null;
            if (masked)
            {
                if (length < offset + 4) return (opcode, Array.Empty<byte>());
                mask = new byte[4];
                Array.Copy(buffer, offset, mask, 0, 4);
                offset += 4;
            }

            if (length < offset + payloadLength)
            {
                payloadLength = length - offset;
            }

            byte[] payload = new byte[payloadLength];
            Array.Copy(buffer, offset, payload, 0, payloadLength);

            if (masked && mask != null)
            {
                for (int i = 0; i < payloadLength; i++)
                {
                    payload[i] ^= mask[i % 4];
                }
            }

            return (opcode, payload);
        }
    }
}
#endif
