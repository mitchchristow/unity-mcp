#if UNITY_EDITOR
using UnityMcp.Editor.MCP.Rpc.Controllers;

namespace UnityMcp.Editor.MCP
{
    /// <summary>
    /// Registers all MCP JSON-RPC controller methods. Used by McpServer and test harnesses.
    /// </summary>
    public static class McpControllerRegistry
    {
        private static bool _registered;

        public static void RegisterAll()
        {
            if (_registered)
                return;

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

            MenuController.Register();
            SearchController.Register();
            FileController.Register();
            ScreenshotController.Register();
            SelectionController.Register();
            ComponentInspectorController.Register();

            LightingController.Register();
            CameraController.Register();
            PhysicsController.Register();
            TagLayerController.Register();
            UndoController.Register();

            AnimationController.Register();
            AudioController.Register();
            UIController.Register();
            BuildController.Register();
            PackageController.Register();

            TerrainController.Register();
            ParticleController.Register();
            NavMeshController.Register();
            EditorWindowController.Register();
            SceneStatsController.Register();

            EventController.Register();

            Sprite2DController.Register();
            TilemapController.Register();
            Physics2DController.Register();

            ScriptingController.Register();
            ProgressController.Register();

            _registered = true;
        }

        /// <summary>Test-only reset when domain reload is disabled.</summary>
        internal static void ResetForTests()
        {
            _registered = false;
        }
    }
}
#endif
