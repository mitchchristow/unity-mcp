#if UNITY_EDITOR && (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Editor.IPC
{
    public class UnixSocketServer
    {
        private readonly string _socketPath = "/tmp/unity-mcp.sock";
        private Socket _socket;
        private bool _isRunning;

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            try
            {
                if (File.Exists(_socketPath)) File.Delete(_socketPath);

                _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                _socket.Bind(new UnixDomainSocketEndPoint(_socketPath));
                _socket.Listen(5);

                Debug.Log($"[MCP] Unix Socket Server started at {_socketPath}");
                Task.Run(ListenLoop);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start Unix Socket: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _socket?.Close();
            _socket = null;
            if (File.Exists(_socketPath)) File.Delete(_socketPath);
        }

        private async Task ListenLoop()
        {
            while (_isRunning && _socket != null)
            {
                try
                {
                    var client = await _socket.AcceptAsync();
                    _ = HandleConnection(client);
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Debug.LogError($"[MCP] Unix Socket accept error: {ex.Message}");
                }
            }
        }

        private async Task HandleConnection(Socket client)
        {
            try
            {
                using (var stream = new NetworkStream(client))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    while (client.Connected && _isRunning)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line == null) break;

                        var tcs = new TaskCompletionSource<string>();
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            try { tcs.SetResult(JsonRpcDispatcher.Handle(line)); }
                            catch (Exception ex) { tcs.SetException(ex); }
                        };

                        string response = await tcs.Task;
                        await writer.WriteLineAsync(response);
                    }
                }
            }
            catch { }
            finally
            {
                client.Close();
            }
        }
    }
}
#endif
