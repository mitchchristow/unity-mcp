using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing physics settings and operations in Unity.
    /// </summary>
    public static class PhysicsController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_physics_settings", GetPhysicsSettings);
            JsonRpcDispatcher.RegisterMethod("unity.set_gravity", SetGravity);
            JsonRpcDispatcher.RegisterMethod("unity.set_physics_property", SetPhysicsProperty);
            JsonRpcDispatcher.RegisterMethod("unity.raycast", Raycast);
            JsonRpcDispatcher.RegisterMethod("unity.get_layer_collision_matrix", GetLayerCollisionMatrix);
            JsonRpcDispatcher.RegisterMethod("unity.set_layer_collision", SetLayerCollision);
        }

        /// <summary>
        /// Gets current physics settings.
        /// </summary>
        private static JObject GetPhysicsSettings(JObject p)
        {
            return new JObject
            {
                ["gravity"] = new JObject
                {
                    ["x"] = Physics.gravity.x,
                    ["y"] = Physics.gravity.y,
                    ["z"] = Physics.gravity.z
                },
                ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                ["defaultSolverVelocityIterations"] = Physics.defaultSolverVelocityIterations,
                ["bounceThreshold"] = Physics.bounceThreshold,
                ["defaultContactOffset"] = Physics.defaultContactOffset,
                ["sleepThreshold"] = Physics.sleepThreshold,
                ["queriesHitTriggers"] = Physics.queriesHitTriggers,
                ["queriesHitBackfaces"] = Physics.queriesHitBackfaces,
                ["autoSimulation"] = Physics.simulationMode.ToString(),
                ["autoSyncTransforms"] = Physics.autoSyncTransforms
            };
        }

        /// <summary>
        /// Sets the gravity vector.
        /// </summary>
        private static JObject SetGravity(JObject p)
        {
            Vector3 gravity = new Vector3(
                p["x"]?.Value<float>() ?? Physics.gravity.x,
                p["y"]?.Value<float>() ?? Physics.gravity.y,
                p["z"]?.Value<float>() ?? Physics.gravity.z
            );

            Physics.gravity = gravity;

            return new JObject
            {
                ["ok"] = true,
                ["gravity"] = new JObject
                {
                    ["x"] = gravity.x,
                    ["y"] = gravity.y,
                    ["z"] = gravity.z
                }
            };
        }

        /// <summary>
        /// Sets various physics properties.
        /// </summary>
        private static JObject SetPhysicsProperty(JObject p)
        {
            if (p["defaultSolverIterations"] != null)
            {
                Physics.defaultSolverIterations = p["defaultSolverIterations"].Value<int>();
            }

            if (p["defaultSolverVelocityIterations"] != null)
            {
                Physics.defaultSolverVelocityIterations = p["defaultSolverVelocityIterations"].Value<int>();
            }

            if (p["bounceThreshold"] != null)
            {
                Physics.bounceThreshold = p["bounceThreshold"].Value<float>();
            }

            if (p["defaultContactOffset"] != null)
            {
                Physics.defaultContactOffset = p["defaultContactOffset"].Value<float>();
            }

            if (p["sleepThreshold"] != null)
            {
                Physics.sleepThreshold = p["sleepThreshold"].Value<float>();
            }

            if (p["queriesHitTriggers"] != null)
            {
                Physics.queriesHitTriggers = p["queriesHitTriggers"].Value<bool>();
            }

            if (p["queriesHitBackfaces"] != null)
            {
                Physics.queriesHitBackfaces = p["queriesHitBackfaces"].Value<bool>();
            }

            if (p["autoSyncTransforms"] != null)
            {
                Physics.autoSyncTransforms = p["autoSyncTransforms"].Value<bool>();
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Performs a raycast and returns hit information.
        /// </summary>
        private static JObject Raycast(JObject p)
        {
            Vector3 origin = new Vector3(
                p["origin"]["x"]?.Value<float>() ?? 0,
                p["origin"]["y"]?.Value<float>() ?? 0,
                p["origin"]["z"]?.Value<float>() ?? 0
            );

            Vector3 direction = new Vector3(
                p["direction"]["x"]?.Value<float>() ?? 0,
                p["direction"]["y"]?.Value<float>() ?? -1,
                p["direction"]["z"]?.Value<float>() ?? 0
            );

            float maxDistance = p["maxDistance"]?.Value<float>() ?? Mathf.Infinity;
            int layerMask = p["layerMask"]?.Value<int>() ?? Physics.DefaultRaycastLayers;

            bool hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, maxDistance, layerMask);

            if (hit)
            {
                return new JObject
                {
                    ["hit"] = true,
                    ["point"] = new JObject
                    {
                        ["x"] = hitInfo.point.x,
                        ["y"] = hitInfo.point.y,
                        ["z"] = hitInfo.point.z
                    },
                    ["normal"] = new JObject
                    {
                        ["x"] = hitInfo.normal.x,
                        ["y"] = hitInfo.normal.y,
                        ["z"] = hitInfo.normal.z
                    },
                    ["distance"] = hitInfo.distance,
                    ["gameObject"] = new JObject
                    {
                        ["id"] = hitInfo.collider.gameObject.GetInstanceID(),
                        ["name"] = hitInfo.collider.gameObject.name
                    },
                    ["collider"] = hitInfo.collider.GetType().Name
                };
            }

            return new JObject { ["hit"] = false };
        }

        /// <summary>
        /// Gets the layer collision matrix.
        /// </summary>
        private static JObject GetLayerCollisionMatrix(JObject p)
        {
            var matrix = new JObject();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName)) continue;

                var collisions = new JArray();
                for (int j = 0; j < 32; j++)
                {
                    string otherLayerName = LayerMask.LayerToName(j);
                    if (string.IsNullOrEmpty(otherLayerName)) continue;

                    if (!Physics.GetIgnoreLayerCollision(i, j))
                    {
                        collisions.Add(otherLayerName);
                    }
                }

                matrix[layerName] = collisions;
            }

            return new JObject { ["collisionMatrix"] = matrix };
        }

        /// <summary>
        /// Sets whether two layers should collide.
        /// </summary>
        private static JObject SetLayerCollision(JObject p)
        {
            var layer1Param = p["layer1"];
            var layer2Param = p["layer2"];
            bool ignore = p["ignore"]?.Value<bool>() ?? false;

            int layer1 = ResolveLayer(layer1Param);
            int layer2 = ResolveLayer(layer2Param);

            Physics.IgnoreLayerCollision(layer1, layer2, ignore);

            return new JObject
            {
                ["ok"] = true,
                ["layer1"] = LayerMask.LayerToName(layer1),
                ["layer2"] = LayerMask.LayerToName(layer2),
                ["ignore"] = ignore
            };
        }

        private static int ResolveLayer(JToken layerParam)
        {
            if (layerParam.Type == JTokenType.Integer)
            {
                return layerParam.Value<int>();
            }
            else
            {
                string layerName = layerParam.ToString();
                int layer = LayerMask.NameToLayer(layerName);
                if (layer == -1)
                    throw new System.Exception($"Layer '{layerName}' not found");
                return layer;
            }
        }
    }
}

