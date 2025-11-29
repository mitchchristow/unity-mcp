#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor.Networking;
using UnityMcp.Editor.MCP.Rpc.Controllers;
using UnityMcp.Editor.MCP.Events;

namespace UnityMcp.Editor.MCP
{
    [InitializeOnLoad]
    public static class McpServer
    {
        private static HttpServer _httpServer;
        private static WebSocketServer _wsServer;
#if UNITY_EDITOR_WIN
        private static UnityMcp.Editor.IPC.NamedPipeServer _pipeServer;
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        private static UnityMcp.Editor.IPC.UnixSocketServer _unixServer;
#endif

        public static WebSocketServer WebSocketServer => _wsServer;

        static McpServer()
        {
            // Ensure server starts when Editor loads
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            if (_httpServer != null) return;

            Debug.Log("[MCP] Initializing Unity MCP Server...");

            // Ensure Unity runs in background to keep server responsive
            Application.runInBackground = true;

            // Register Controllers
            ConsoleController.Register();
            SceneController.Register();
            HierarchyController.Register();
            ComponentController.Register();
            PlaymodeController.Register();
            ScriptController.Register();
            PrefabController.Register();
            ShaderController.Register();
            EditorStateController.Register();
            AssetController.Register();
            
            // Phase 1 Controllers
            MenuController.Register();
            SearchController.Register();
            FileController.Register();
            ScreenshotController.Register();
            SelectionController.Register();
            ComponentInspectorController.Register();
            
            // Phase 2 Controllers
            LightingController.Register();
            CameraController.Register();
            PhysicsController.Register();
            TagLayerController.Register();
            UndoController.Register();
            
            // Phase 3 Controllers
            AnimationController.Register();
            AudioController.Register();
            UIController.Register();
            BuildController.Register();
            PackageController.Register();
            
            // Phase 4 Controllers
            TerrainController.Register();
            ParticleController.Register();
            NavMeshController.Register();
            EditorWindowController.Register();
            SceneStatsController.Register();
            
            // Event Controller (for event history access)
            EventController.Register();
            
            // 2D Game Development Controllers
            Sprite2DController.Register();
            TilemapController.Register();
            Physics2DController.Register();
            
            // Scripting Assistance Controller
            ScriptingController.Register();

            // Start HTTP Server
            _httpServer = new HttpServer(17890);
            _httpServer.Start();

            // Start WebSocket Server
            _wsServer = new WebSocketServer(17891);
            _wsServer.Start();

            // Start IPC Server
#if UNITY_EDITOR_WIN
            _pipeServer = new UnityMcp.Editor.IPC.NamedPipeServer();
            _pipeServer.Start();
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            _unixServer = new UnityMcp.Editor.IPC.UnixSocketServer();
            _unixServer.Start();
#endif

            // Initialize Event Broadcaster (for real-time events via WebSocket)
            EventBroadcaster.Initialize();

            // Hook into assembly reload to stop server
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            Debug.Log("[MCP] Stopping server before assembly reload...");
            
            // Shutdown event broadcaster first
            EventBroadcaster.Shutdown();
            
            _httpServer?.Stop();
            _httpServer = null;
            
            _wsServer?.Stop();
            _wsServer = null;

#if UNITY_EDITOR_WIN
            _pipeServer?.Stop();
            _pipeServer = null;
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            _unixServer?.Stop();
            _unixServer = null;
#endif
        }
    }
}
#endif
