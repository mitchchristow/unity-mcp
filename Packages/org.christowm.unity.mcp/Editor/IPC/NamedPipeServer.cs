#if UNITY_EDITOR && UNITY_EDITOR_WIN
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Editor.IPC
{
    public class NamedPipeServer
    {
        private readonly string _pipeName = "unity-mcp";
        private bool _isRunning;
        private NamedPipeServerStream _serverStream;

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Task.Run(ListenLoop);
            Debug.Log($"[MCP] Named Pipe Server started at \\\\.\\pipe\\{_pipeName}");
        }

        public void Stop()
        {
            _isRunning = false;
            _serverStream?.Close();
            _serverStream = null;
        }

        private async Task ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    _serverStream = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await _serverStream.WaitForConnectionAsync();

                    // Handle connection in a separate task to allow new connections (if max instances > 1)
                    // But here we only support 1 concurrent pipe connection for simplicity or loop
                    await HandleConnection(_serverStream);
                }
                catch (ObjectDisposedException) { }
                catch (IOException) { } // Pipe broken
                catch (Exception ex)
                {
                    Debug.LogError($"[MCP] Named Pipe error: {ex.Message}");
                    await Task.Delay(1000); // Backoff
                }
            }
        }

        private async Task HandleConnection(NamedPipeServerStream stream)
        {
            try
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true) { AutoFlush = true })
                {
                    while (stream.IsConnected && _isRunning)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line == null) break;

                        // Execute on main thread
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
            catch (Exception)
            {
                // Client disconnected
            }
            finally
            {
                if (stream.IsConnected) stream.Disconnect();
                stream.Close();
            }
        }
    }
}
#endif
