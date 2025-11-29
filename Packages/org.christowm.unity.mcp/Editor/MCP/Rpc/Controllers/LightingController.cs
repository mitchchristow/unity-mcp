using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing lights and lighting settings in Unity.
    /// </summary>
    public static class LightingController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.create_light", CreateLight);
            JsonRpcDispatcher.RegisterMethod("unity.set_light_property", SetLightProperty);
            JsonRpcDispatcher.RegisterMethod("unity.get_lighting_settings", GetLightingSettings);
            JsonRpcDispatcher.RegisterMethod("unity.set_ambient_light", SetAmbientLight);
            JsonRpcDispatcher.RegisterMethod("unity.list_lights", ListLights);
        }

        /// <summary>
        /// Creates a new light in the scene.
        /// </summary>
        private static JObject CreateLight(JObject p)
        {
            string typeName = p["type"]?.ToString() ?? "Point";
            string name = p["name"]?.ToString();
            
            if (!System.Enum.TryParse(typeName, true, out LightType lightType))
            {
                throw new System.Exception($"Invalid light type: {typeName}. Valid types: Directional, Point, Spot, Area");
            }

            var go = new GameObject(name ?? $"{typeName} Light");
            var light = go.AddComponent<Light>();
            light.type = lightType;

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

            // Set intensity if provided
            if (p["intensity"] != null)
            {
                light.intensity = p["intensity"].Value<float>();
            }

            // Set color if provided
            if (p["color"] != null)
            {
                light.color = new Color(
                    p["color"]["r"]?.Value<float>() ?? 1,
                    p["color"]["g"]?.Value<float>() ?? 1,
                    p["color"]["b"]?.Value<float>() ?? 1
                );
            }

            // Set range for point/spot lights
            if (p["range"] != null && (lightType == LightType.Point || lightType == LightType.Spot))
            {
                light.range = p["range"].Value<float>();
            }

            // Set spot angle for spot lights
            if (p["spotAngle"] != null && lightType == LightType.Spot)
            {
                light.spotAngle = p["spotAngle"].Value<float>();
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Light via MCP");

            return new JObject
            {
                ["id"] = go.GetInstanceID(),
                ["lightId"] = light.GetInstanceID(),
                ["type"] = lightType.ToString()
            };
        }

        /// <summary>
        /// Sets a property on an existing light component.
        /// </summary>
        private static JObject SetLightProperty(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var light = go.GetComponent<Light>();
            if (light == null)
                throw new System.Exception("GameObject does not have a Light component");

            Undo.RecordObject(light, "Set Light Property via MCP");

            // Set intensity
            if (p["intensity"] != null)
            {
                light.intensity = p["intensity"].Value<float>();
            }

            // Set color
            if (p["color"] != null)
            {
                light.color = new Color(
                    p["color"]["r"]?.Value<float>() ?? light.color.r,
                    p["color"]["g"]?.Value<float>() ?? light.color.g,
                    p["color"]["b"]?.Value<float>() ?? light.color.b
                );
            }

            // Set range
            if (p["range"] != null)
            {
                light.range = p["range"].Value<float>();
            }

            // Set spot angle
            if (p["spotAngle"] != null)
            {
                light.spotAngle = p["spotAngle"].Value<float>();
            }

            // Set shadows
            if (p["shadows"] != null)
            {
                string shadowType = p["shadows"].ToString();
                if (System.Enum.TryParse(shadowType, true, out LightShadows shadows))
                {
                    light.shadows = shadows;
                }
            }

            // Set enabled
            if (p["enabled"] != null)
            {
                light.enabled = p["enabled"].Value<bool>();
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Gets the current lighting/render settings.
        /// </summary>
        private static JObject GetLightingSettings(JObject p)
        {
            return new JObject
            {
                ["ambientMode"] = RenderSettings.ambientMode.ToString(),
                ["ambientLight"] = new JObject
                {
                    ["r"] = RenderSettings.ambientLight.r,
                    ["g"] = RenderSettings.ambientLight.g,
                    ["b"] = RenderSettings.ambientLight.b
                },
                ["ambientIntensity"] = RenderSettings.ambientIntensity,
                ["ambientSkyColor"] = new JObject
                {
                    ["r"] = RenderSettings.ambientSkyColor.r,
                    ["g"] = RenderSettings.ambientSkyColor.g,
                    ["b"] = RenderSettings.ambientSkyColor.b
                },
                ["ambientEquatorColor"] = new JObject
                {
                    ["r"] = RenderSettings.ambientEquatorColor.r,
                    ["g"] = RenderSettings.ambientEquatorColor.g,
                    ["b"] = RenderSettings.ambientEquatorColor.b
                },
                ["ambientGroundColor"] = new JObject
                {
                    ["r"] = RenderSettings.ambientGroundColor.r,
                    ["g"] = RenderSettings.ambientGroundColor.g,
                    ["b"] = RenderSettings.ambientGroundColor.b
                },
                ["fog"] = RenderSettings.fog,
                ["fogColor"] = new JObject
                {
                    ["r"] = RenderSettings.fogColor.r,
                    ["g"] = RenderSettings.fogColor.g,
                    ["b"] = RenderSettings.fogColor.b
                },
                ["fogMode"] = RenderSettings.fogMode.ToString(),
                ["fogDensity"] = RenderSettings.fogDensity,
                ["skybox"] = RenderSettings.skybox?.name
            };
        }

        /// <summary>
        /// Sets ambient lighting properties.
        /// </summary>
        private static JObject SetAmbientLight(JObject p)
        {
            // Note: RenderSettings changes are saved with the scene

            // Set ambient mode
            if (p["mode"] != null)
            {
                string modeName = p["mode"].ToString();
                if (System.Enum.TryParse(modeName, true, out AmbientMode mode))
                {
                    RenderSettings.ambientMode = mode;
                }
            }

            // Set ambient color (for flat mode)
            if (p["color"] != null)
            {
                RenderSettings.ambientLight = new Color(
                    p["color"]["r"]?.Value<float>() ?? RenderSettings.ambientLight.r,
                    p["color"]["g"]?.Value<float>() ?? RenderSettings.ambientLight.g,
                    p["color"]["b"]?.Value<float>() ?? RenderSettings.ambientLight.b
                );
            }

            // Set ambient intensity
            if (p["intensity"] != null)
            {
                RenderSettings.ambientIntensity = p["intensity"].Value<float>();
            }

            // Set sky color (for gradient mode)
            if (p["skyColor"] != null)
            {
                RenderSettings.ambientSkyColor = new Color(
                    p["skyColor"]["r"]?.Value<float>() ?? 0.5f,
                    p["skyColor"]["g"]?.Value<float>() ?? 0.5f,
                    p["skyColor"]["b"]?.Value<float>() ?? 0.5f
                );
            }

            // Set equator color (for gradient mode)
            if (p["equatorColor"] != null)
            {
                RenderSettings.ambientEquatorColor = new Color(
                    p["equatorColor"]["r"]?.Value<float>() ?? 0.5f,
                    p["equatorColor"]["g"]?.Value<float>() ?? 0.5f,
                    p["equatorColor"]["b"]?.Value<float>() ?? 0.5f
                );
            }

            // Set ground color (for gradient mode)
            if (p["groundColor"] != null)
            {
                RenderSettings.ambientGroundColor = new Color(
                    p["groundColor"]["r"]?.Value<float>() ?? 0.5f,
                    p["groundColor"]["g"]?.Value<float>() ?? 0.5f,
                    p["groundColor"]["b"]?.Value<float>() ?? 0.5f
                );
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Lists all lights in the scene.
        /// </summary>
        private static JObject ListLights(JObject p)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var result = new JArray();

            foreach (var light in lights)
            {
                result.Add(new JObject
                {
                    ["id"] = light.gameObject.GetInstanceID(),
                    ["name"] = light.gameObject.name,
                    ["type"] = light.type.ToString(),
                    ["intensity"] = light.intensity,
                    ["color"] = new JObject
                    {
                        ["r"] = light.color.r,
                        ["g"] = light.color.g,
                        ["b"] = light.color.b
                    },
                    ["range"] = light.range,
                    ["spotAngle"] = light.spotAngle,
                    ["shadows"] = light.shadows.ToString(),
                    ["enabled"] = light.enabled
                });
            }

            return new JObject
            {
                ["lights"] = result,
                ["count"] = result.Count
            };
        }
    }
}

