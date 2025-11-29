using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing cameras in Unity.
    /// </summary>
    public static class CameraController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.create_camera", CreateCamera);
            JsonRpcDispatcher.RegisterMethod("unity.set_camera_property", SetCameraProperty);
            JsonRpcDispatcher.RegisterMethod("unity.get_camera_info", GetCameraInfo);
            JsonRpcDispatcher.RegisterMethod("unity.list_cameras", ListCameras);
            JsonRpcDispatcher.RegisterMethod("unity.get_scene_view_camera", GetSceneViewCamera);
            JsonRpcDispatcher.RegisterMethod("unity.set_scene_view_camera", SetSceneViewCamera);
        }

        /// <summary>
        /// Creates a new camera in the scene.
        /// </summary>
        private static JObject CreateCamera(JObject p)
        {
            string name = p["name"]?.ToString() ?? "New Camera";
            
            var go = new GameObject(name);
            var camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();

            // Set position if provided
            if (p["position"] != null)
            {
                go.transform.position = new Vector3(
                    p["position"]["x"]?.Value<float>() ?? 0,
                    p["position"]["y"]?.Value<float>() ?? 0,
                    p["position"]["z"]?.Value<float>() ?? 0
                );
            }

            // Set rotation if provided
            if (p["rotation"] != null)
            {
                go.transform.eulerAngles = new Vector3(
                    p["rotation"]["x"]?.Value<float>() ?? 0,
                    p["rotation"]["y"]?.Value<float>() ?? 0,
                    p["rotation"]["z"]?.Value<float>() ?? 0
                );
            }

            // Set field of view
            if (p["fieldOfView"] != null)
            {
                camera.fieldOfView = p["fieldOfView"].Value<float>();
            }

            // Set orthographic mode
            if (p["orthographic"] != null)
            {
                camera.orthographic = p["orthographic"].Value<bool>();
            }

            // Set orthographic size
            if (p["orthographicSize"] != null)
            {
                camera.orthographicSize = p["orthographicSize"].Value<float>();
            }

            // Set near clip
            if (p["nearClip"] != null)
            {
                camera.nearClipPlane = p["nearClip"].Value<float>();
            }

            // Set far clip
            if (p["farClip"] != null)
            {
                camera.farClipPlane = p["farClip"].Value<float>();
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Camera via MCP");

            return new JObject
            {
                ["id"] = go.GetInstanceID(),
                ["cameraId"] = camera.GetInstanceID()
            };
        }

        /// <summary>
        /// Sets properties on an existing camera.
        /// </summary>
        private static JObject SetCameraProperty(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var camera = go.GetComponent<Camera>();
            if (camera == null)
                throw new System.Exception("GameObject does not have a Camera component");

            Undo.RecordObject(camera, "Set Camera Property via MCP");

            // Field of view
            if (p["fieldOfView"] != null)
            {
                camera.fieldOfView = p["fieldOfView"].Value<float>();
            }

            // Orthographic
            if (p["orthographic"] != null)
            {
                camera.orthographic = p["orthographic"].Value<bool>();
            }

            // Orthographic size
            if (p["orthographicSize"] != null)
            {
                camera.orthographicSize = p["orthographicSize"].Value<float>();
            }

            // Near clip plane
            if (p["nearClip"] != null)
            {
                camera.nearClipPlane = p["nearClip"].Value<float>();
            }

            // Far clip plane
            if (p["farClip"] != null)
            {
                camera.farClipPlane = p["farClip"].Value<float>();
            }

            // Clear flags
            if (p["clearFlags"] != null)
            {
                string flagsName = p["clearFlags"].ToString();
                if (System.Enum.TryParse(flagsName, true, out CameraClearFlags flags))
                {
                    camera.clearFlags = flags;
                }
            }

            // Background color
            if (p["backgroundColor"] != null)
            {
                camera.backgroundColor = new Color(
                    p["backgroundColor"]["r"]?.Value<float>() ?? 0,
                    p["backgroundColor"]["g"]?.Value<float>() ?? 0,
                    p["backgroundColor"]["b"]?.Value<float>() ?? 0,
                    p["backgroundColor"]["a"]?.Value<float>() ?? 1
                );
            }

            // Depth
            if (p["depth"] != null)
            {
                camera.depth = p["depth"].Value<float>();
            }

            // Culling mask
            if (p["cullingMask"] != null)
            {
                camera.cullingMask = p["cullingMask"].Value<int>();
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Gets detailed information about a camera.
        /// </summary>
        private static JObject GetCameraInfo(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var camera = go.GetComponent<Camera>();
            if (camera == null)
                throw new System.Exception("GameObject does not have a Camera component");

            return new JObject
            {
                ["id"] = go.GetInstanceID(),
                ["name"] = go.name,
                ["fieldOfView"] = camera.fieldOfView,
                ["orthographic"] = camera.orthographic,
                ["orthographicSize"] = camera.orthographicSize,
                ["nearClipPlane"] = camera.nearClipPlane,
                ["farClipPlane"] = camera.farClipPlane,
                ["clearFlags"] = camera.clearFlags.ToString(),
                ["backgroundColor"] = new JObject
                {
                    ["r"] = camera.backgroundColor.r,
                    ["g"] = camera.backgroundColor.g,
                    ["b"] = camera.backgroundColor.b,
                    ["a"] = camera.backgroundColor.a
                },
                ["depth"] = camera.depth,
                ["cullingMask"] = camera.cullingMask,
                ["targetDisplay"] = camera.targetDisplay,
                ["pixelRect"] = new JObject
                {
                    ["x"] = camera.pixelRect.x,
                    ["y"] = camera.pixelRect.y,
                    ["width"] = camera.pixelRect.width,
                    ["height"] = camera.pixelRect.height
                }
            };
        }

        /// <summary>
        /// Lists all cameras in the scene.
        /// </summary>
        private static JObject ListCameras(JObject p)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var result = new JArray();

            foreach (var camera in cameras)
            {
                result.Add(new JObject
                {
                    ["id"] = camera.gameObject.GetInstanceID(),
                    ["name"] = camera.gameObject.name,
                    ["fieldOfView"] = camera.fieldOfView,
                    ["orthographic"] = camera.orthographic,
                    ["depth"] = camera.depth,
                    ["enabled"] = camera.enabled,
                    ["isMain"] = camera == Camera.main
                });
            }

            return new JObject
            {
                ["cameras"] = result,
                ["count"] = result.Count,
                ["mainCamera"] = Camera.main?.gameObject.name
            };
        }

        /// <summary>
        /// Gets the current Scene view camera position and rotation.
        /// </summary>
        private static JObject GetSceneViewCamera(JObject p)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                throw new System.Exception("No active Scene View found");

            var cam = sceneView.camera;
            var pivot = sceneView.pivot;
            var rotation = sceneView.rotation.eulerAngles;

            return new JObject
            {
                ["pivot"] = new JObject
                {
                    ["x"] = pivot.x,
                    ["y"] = pivot.y,
                    ["z"] = pivot.z
                },
                ["rotation"] = new JObject
                {
                    ["x"] = rotation.x,
                    ["y"] = rotation.y,
                    ["z"] = rotation.z
                },
                ["size"] = sceneView.size,
                ["orthographic"] = sceneView.orthographic,
                ["cameraPosition"] = new JObject
                {
                    ["x"] = cam.transform.position.x,
                    ["y"] = cam.transform.position.y,
                    ["z"] = cam.transform.position.z
                }
            };
        }

        /// <summary>
        /// Sets the Scene view camera position and rotation.
        /// </summary>
        private static JObject SetSceneViewCamera(JObject p)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                throw new System.Exception("No active Scene View found");

            // Set pivot
            if (p["pivot"] != null)
            {
                sceneView.pivot = new Vector3(
                    p["pivot"]["x"]?.Value<float>() ?? sceneView.pivot.x,
                    p["pivot"]["y"]?.Value<float>() ?? sceneView.pivot.y,
                    p["pivot"]["z"]?.Value<float>() ?? sceneView.pivot.z
                );
            }

            // Set rotation
            if (p["rotation"] != null)
            {
                sceneView.rotation = Quaternion.Euler(
                    p["rotation"]["x"]?.Value<float>() ?? 0,
                    p["rotation"]["y"]?.Value<float>() ?? 0,
                    p["rotation"]["z"]?.Value<float>() ?? 0
                );
            }

            // Set size (zoom level)
            if (p["size"] != null)
            {
                sceneView.size = p["size"].Value<float>();
            }

            // Set orthographic
            if (p["orthographic"] != null)
            {
                sceneView.orthographic = p["orthographic"].Value<bool>();
            }

            // Look at a specific point
            if (p["lookAt"] != null)
            {
                var lookAt = new Vector3(
                    p["lookAt"]["x"]?.Value<float>() ?? 0,
                    p["lookAt"]["y"]?.Value<float>() ?? 0,
                    p["lookAt"]["z"]?.Value<float>() ?? 0
                );
                sceneView.LookAt(lookAt);
            }

            sceneView.Repaint();

            return new JObject { ["ok"] = true };
        }
    }
}

