#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for 2D physics settings and operations in Unity.
    /// </summary>
    public static class Physics2DController
    {
        public static void Register()
        {
            // Physics 2D settings (for resource)
            JsonRpcDispatcher.RegisterMethod("unity.get_physics_2d_settings", GetPhysics2DSettings);
            JsonRpcDispatcher.RegisterMethod("unity.set_physics_2d_property", SetPhysics2DProperty);
            
            // 2D Raycasting
            JsonRpcDispatcher.RegisterMethod("unity.raycast_2d", Raycast2D);
            JsonRpcDispatcher.RegisterMethod("unity.overlap_circle_2d", OverlapCircle2D);
            JsonRpcDispatcher.RegisterMethod("unity.overlap_box_2d", OverlapBox2D);
            
            // 2D Layer collision matrix
            JsonRpcDispatcher.RegisterMethod("unity.get_layer_collision_matrix_2d", GetLayerCollisionMatrix2D);
            JsonRpcDispatcher.RegisterMethod("unity.set_layer_collision_2d", SetLayerCollision2D);
        }

        /// <summary>
        /// Gets 2D physics settings.
        /// </summary>
        private static JObject GetPhysics2DSettings(JObject p)
        {
            return new JObject
            {
                ["gravity"] = new JObject
                {
                    ["x"] = Physics2D.gravity.x,
                    ["y"] = Physics2D.gravity.y
                },
                ["velocityIterations"] = Physics2D.velocityIterations,
                ["positionIterations"] = Physics2D.positionIterations,
                ["velocityThreshold"] = Physics2D.bounceThreshold,
                ["maxLinearCorrection"] = Physics2D.maxLinearCorrection,
                ["maxAngularCorrection"] = Physics2D.maxAngularCorrection,
                ["maxTranslationSpeed"] = Physics2D.maxTranslationSpeed,
                ["maxRotationSpeed"] = Physics2D.maxRotationSpeed,
                ["baumgarteScale"] = Physics2D.baumgarteScale,
                ["baumgarteTOIScale"] = Physics2D.baumgarteTOIScale,
                ["timeToSleep"] = Physics2D.timeToSleep,
                ["linearSleepTolerance"] = Physics2D.linearSleepTolerance,
                ["angularSleepTolerance"] = Physics2D.angularSleepTolerance,
                ["queriesHitTriggers"] = Physics2D.queriesHitTriggers,
                ["queriesStartInColliders"] = Physics2D.queriesStartInColliders,
                ["callbacksOnDisable"] = Physics2D.callbacksOnDisable,
                ["reuseCollisionCallbacks"] = Physics2D.reuseCollisionCallbacks,
                ["autoSyncTransforms"] = Physics2D.autoSyncTransforms,
                ["simulationMode"] = Physics2D.simulationMode.ToString()
            };
        }

        /// <summary>
        /// Sets 2D physics properties.
        /// </summary>
        private static JObject SetPhysics2DProperty(JObject p)
        {
            if (p["gravity"] != null)
            {
                Physics2D.gravity = new Vector2(
                    p["gravity"]["x"]?.Value<float>() ?? Physics2D.gravity.x,
                    p["gravity"]["y"]?.Value<float>() ?? Physics2D.gravity.y
                );
            }

            if (p["velocityIterations"] != null) 
                Physics2D.velocityIterations = p["velocityIterations"].Value<int>();
            if (p["positionIterations"] != null) 
                Physics2D.positionIterations = p["positionIterations"].Value<int>();
            if (p["velocityThreshold"] != null) 
                Physics2D.bounceThreshold = p["velocityThreshold"].Value<float>();
            if (p["maxLinearCorrection"] != null) 
                Physics2D.maxLinearCorrection = p["maxLinearCorrection"].Value<float>();
            if (p["maxAngularCorrection"] != null) 
                Physics2D.maxAngularCorrection = p["maxAngularCorrection"].Value<float>();
            if (p["maxTranslationSpeed"] != null) 
                Physics2D.maxTranslationSpeed = p["maxTranslationSpeed"].Value<float>();
            if (p["maxRotationSpeed"] != null) 
                Physics2D.maxRotationSpeed = p["maxRotationSpeed"].Value<float>();
            if (p["timeToSleep"] != null) 
                Physics2D.timeToSleep = p["timeToSleep"].Value<float>();
            if (p["linearSleepTolerance"] != null) 
                Physics2D.linearSleepTolerance = p["linearSleepTolerance"].Value<float>();
            if (p["angularSleepTolerance"] != null) 
                Physics2D.angularSleepTolerance = p["angularSleepTolerance"].Value<float>();
            if (p["queriesHitTriggers"] != null) 
                Physics2D.queriesHitTriggers = p["queriesHitTriggers"].Value<bool>();
            if (p["queriesStartInColliders"] != null) 
                Physics2D.queriesStartInColliders = p["queriesStartInColliders"].Value<bool>();
            if (p["callbacksOnDisable"] != null) 
                Physics2D.callbacksOnDisable = p["callbacksOnDisable"].Value<bool>();
            if (p["reuseCollisionCallbacks"] != null) 
                Physics2D.reuseCollisionCallbacks = p["reuseCollisionCallbacks"].Value<bool>();
            if (p["autoSyncTransforms"] != null) 
                Physics2D.autoSyncTransforms = p["autoSyncTransforms"].Value<bool>();

            return new JObject
            {
                ["ok"] = true
            };
        }

        /// <summary>
        /// Performs a 2D raycast.
        /// </summary>
        private static JObject Raycast2D(JObject p)
        {
            Vector2 origin = new Vector2(
                p["origin"]["x"]?.Value<float>() ?? 0,
                p["origin"]["y"]?.Value<float>() ?? 0
            );

            Vector2 direction = new Vector2(
                p["direction"]["x"]?.Value<float>() ?? 0,
                p["direction"]["y"]?.Value<float>() ?? 1
            );

            float distance = p["distance"]?.Value<float>() ?? Mathf.Infinity;
            int layerMask = p["layerMask"]?.Value<int>() ?? Physics2D.AllLayers;

            var hit = Physics2D.Raycast(origin, direction, distance, layerMask);

            if (hit.collider != null)
            {
                return new JObject
                {
                    ["hit"] = true,
                    ["point"] = new JObject { ["x"] = hit.point.x, ["y"] = hit.point.y },
                    ["normal"] = new JObject { ["x"] = hit.normal.x, ["y"] = hit.normal.y },
                    ["distance"] = hit.distance,
                    ["objectId"] = hit.collider.gameObject.GetInstanceID(),
                    ["objectName"] = hit.collider.gameObject.name,
                    ["colliderType"] = hit.collider.GetType().Name
                };
            }

            return new JObject
            {
                ["hit"] = false
            };
        }

        /// <summary>
        /// Finds all colliders within a circle.
        /// </summary>
        private static JObject OverlapCircle2D(JObject p)
        {
            Vector2 center = new Vector2(
                p["center"]["x"]?.Value<float>() ?? 0,
                p["center"]["y"]?.Value<float>() ?? 0
            );

            float radius = p["radius"]?.Value<float>() ?? 1;
            int layerMask = p["layerMask"]?.Value<int>() ?? Physics2D.AllLayers;

            var colliders = Physics2D.OverlapCircleAll(center, radius, layerMask);
            var results = new JArray();

            foreach (var collider in colliders)
            {
                results.Add(new JObject
                {
                    ["objectId"] = collider.gameObject.GetInstanceID(),
                    ["objectName"] = collider.gameObject.name,
                    ["colliderType"] = collider.GetType().Name,
                    ["position"] = new JObject
                    {
                        ["x"] = collider.transform.position.x,
                        ["y"] = collider.transform.position.y
                    }
                });
            }

            return new JObject
            {
                ["colliders"] = results,
                ["count"] = results.Count,
                ["center"] = new JObject { ["x"] = center.x, ["y"] = center.y },
                ["radius"] = radius
            };
        }

        /// <summary>
        /// Finds all colliders within a box.
        /// </summary>
        private static JObject OverlapBox2D(JObject p)
        {
            Vector2 center = new Vector2(
                p["center"]["x"]?.Value<float>() ?? 0,
                p["center"]["y"]?.Value<float>() ?? 0
            );

            Vector2 size = new Vector2(
                p["size"]["x"]?.Value<float>() ?? 1,
                p["size"]["y"]?.Value<float>() ?? 1
            );

            float angle = p["angle"]?.Value<float>() ?? 0;
            int layerMask = p["layerMask"]?.Value<int>() ?? Physics2D.AllLayers;

            var colliders = Physics2D.OverlapBoxAll(center, size, angle, layerMask);
            var results = new JArray();

            foreach (var collider in colliders)
            {
                results.Add(new JObject
                {
                    ["objectId"] = collider.gameObject.GetInstanceID(),
                    ["objectName"] = collider.gameObject.name,
                    ["colliderType"] = collider.GetType().Name,
                    ["position"] = new JObject
                    {
                        ["x"] = collider.transform.position.x,
                        ["y"] = collider.transform.position.y
                    }
                });
            }

            return new JObject
            {
                ["colliders"] = results,
                ["count"] = results.Count,
                ["center"] = new JObject { ["x"] = center.x, ["y"] = center.y },
                ["size"] = new JObject { ["x"] = size.x, ["y"] = size.y },
                ["angle"] = angle
            };
        }

        /// <summary>
        /// Gets the 2D layer collision matrix.
        /// </summary>
        private static JObject GetLayerCollisionMatrix2D(JObject p)
        {
            var matrix = new JArray();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName)) continue;

                var collidesWith = new JArray();
                for (int j = 0; j < 32; j++)
                {
                    string otherLayerName = LayerMask.LayerToName(j);
                    if (string.IsNullOrEmpty(otherLayerName)) continue;

                    if (!Physics2D.GetIgnoreLayerCollision(i, j))
                    {
                        collidesWith.Add(otherLayerName);
                    }
                }

                matrix.Add(new JObject
                {
                    ["layer"] = layerName,
                    ["layerIndex"] = i,
                    ["collidesWith"] = collidesWith
                });
            }

            return new JObject
            {
                ["matrix"] = matrix
            };
        }

        /// <summary>
        /// Sets whether two layers should collide in 2D physics.
        /// </summary>
        private static JObject SetLayerCollision2D(JObject p)
        {
            string layer1Str = p["layer1"]?.Value<string>();
            string layer2Str = p["layer2"]?.Value<string>();
            bool ignore = p["ignore"]?.Value<bool>() ?? false;

            int layer1 = -1;
            int layer2 = -1;

            // Try to parse as layer name first, then as index
            if (int.TryParse(layer1Str, out int l1Index))
            {
                layer1 = l1Index;
            }
            else
            {
                layer1 = LayerMask.NameToLayer(layer1Str);
            }

            if (int.TryParse(layer2Str, out int l2Index))
            {
                layer2 = l2Index;
            }
            else
            {
                layer2 = LayerMask.NameToLayer(layer2Str);
            }

            if (layer1 < 0 || layer1 >= 32)
                throw new System.ArgumentException($"Invalid layer: {layer1Str}");
            if (layer2 < 0 || layer2 >= 32)
                throw new System.ArgumentException($"Invalid layer: {layer2Str}");

            Physics2D.IgnoreLayerCollision(layer1, layer2, ignore);

            return new JObject
            {
                ["ok"] = true,
                ["layer1"] = LayerMask.LayerToName(layer1),
                ["layer2"] = LayerMask.LayerToName(layer2),
                ["ignore"] = ignore
            };
        }
    }
}
#endif

