using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for capturing screenshots from the Unity Editor.
    /// </summary>
    public static class ScreenshotController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.capture_screenshot", CaptureScreenshot);
            JsonRpcDispatcher.RegisterMethod("unity.capture_scene_view", CaptureSceneView);
        }

        /// <summary>
        /// Captures a screenshot from the Game view.
        /// </summary>
        private static JObject CaptureScreenshot(JObject p)
        {
            string filename = p["filename"]?.ToString() ?? $"screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            int superSize = p["superSize"]?.Value<int>() ?? 1;
            
            // Ensure filename has extension
            if (!filename.EndsWith(".png") && !filename.EndsWith(".jpg"))
                filename += ".png";
            
            // Default to Assets folder if no path specified
            string path;
            if (Path.IsPathRooted(filename) || filename.StartsWith("Assets/"))
            {
                path = filename;
            }
            else
            {
                path = Path.Combine("Assets", filename);
            }
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Get full path for ScreenCapture
            string fullPath = Path.IsPathRooted(path) 
                ? path 
                : Path.Combine(Application.dataPath.Replace("/Assets", ""), path);

            ScreenCapture.CaptureScreenshot(fullPath, superSize);
            
            // Note: The screenshot is captured asynchronously, so we can't immediately verify it exists
            return new JObject
            {
                ["ok"] = true,
                ["path"] = path,
                ["note"] = "Screenshot capture initiated. File will be created after the next frame renders."
            };
        }

        /// <summary>
        /// Captures an image from the Scene view camera.
        /// </summary>
        private static JObject CaptureSceneView(JObject p)
        {
            string filename = p["filename"]?.ToString() ?? $"sceneview_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            int width = p["width"]?.Value<int>() ?? 1920;
            int height = p["height"]?.Value<int>() ?? 1080;
            
            // Ensure filename has extension
            if (!filename.EndsWith(".png"))
                filename += ".png";
            
            // Default to Assets folder if no path specified
            string path;
            if (Path.IsPathRooted(filename) || filename.StartsWith("Assets/"))
            {
                path = filename;
            }
            else
            {
                path = Path.Combine("Assets", filename);
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                throw new System.Exception("No active Scene View found. Please open a Scene View first.");
            }

            var camera = sceneView.camera;
            if (camera == null)
            {
                throw new System.Exception("Scene View camera not available.");
            }

            // Create a render texture
            var renderTexture = new RenderTexture(width, height, 24);
            var previousTargetTexture = camera.targetTexture;
            
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                
                // Read pixels
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                
                // Encode and save
                byte[] bytes = texture.EncodeToPNG();
                
                string fullPath = Path.IsPathRooted(path) 
                    ? path 
                    : Path.Combine(Application.dataPath.Replace("/Assets", ""), path);
                    
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllBytes(fullPath, bytes);
                
                // Cleanup
                Object.DestroyImmediate(texture);
                
                // Import asset if in Assets folder
                if (path.StartsWith("Assets"))
                {
                    AssetDatabase.ImportAsset(path);
                }
                
                return new JObject
                {
                    ["ok"] = true,
                    ["path"] = path,
                    ["width"] = width,
                    ["height"] = height
                };
            }
            finally
            {
                // Restore camera state
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = null;
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}

