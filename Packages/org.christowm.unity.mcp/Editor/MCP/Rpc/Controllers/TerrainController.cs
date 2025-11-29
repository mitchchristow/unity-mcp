using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing terrains in Unity.
    /// </summary>
    public static class TerrainController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.create_terrain", CreateTerrain);
            JsonRpcDispatcher.RegisterMethod("unity.get_terrain_info", GetTerrainInfo);
            JsonRpcDispatcher.RegisterMethod("unity.set_terrain_size", SetTerrainSize);
            JsonRpcDispatcher.RegisterMethod("unity.set_terrain_height", SetTerrainHeight);
            JsonRpcDispatcher.RegisterMethod("unity.get_terrain_height", GetTerrainHeight);
            JsonRpcDispatcher.RegisterMethod("unity.list_terrains", ListTerrains);
            JsonRpcDispatcher.RegisterMethod("unity.set_terrain_layer", SetTerrainLayer);
            JsonRpcDispatcher.RegisterMethod("unity.flatten_terrain", FlattenTerrain);
        }

        /// <summary>
        /// Creates a new terrain.
        /// </summary>
        private static JObject CreateTerrain(JObject p)
        {
            string name = p["name"]?.ToString() ?? "Terrain";
            float width = p["width"]?.Value<float>() ?? 500f;
            float length = p["length"]?.Value<float>() ?? 500f;
            float height = p["height"]?.Value<float>() ?? 100f;
            int heightmapResolution = p["heightmapResolution"]?.Value<int>() ?? 513;

            // Create terrain data
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = new Vector3(width, height, length);

            // Save terrain data as asset
            string assetPath = $"Assets/{name}_Data.asset";
            AssetDatabase.CreateAsset(terrainData, assetPath);

            // Create terrain game object
            var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = name;

            // Set position if provided
            if (p["position"] != null)
            {
                terrainGO.transform.position = new Vector3(
                    p["position"]["x"]?.Value<float>() ?? 0,
                    p["position"]["y"]?.Value<float>() ?? 0,
                    p["position"]["z"]?.Value<float>() ?? 0
                );
            }

            Undo.RegisterCreatedObjectUndo(terrainGO, "Create Terrain via MCP");

            return new JObject
            {
                ["id"] = terrainGO.GetInstanceID(),
                ["name"] = terrainGO.name,
                ["dataPath"] = assetPath,
                ["size"] = new JObject
                {
                    ["width"] = width,
                    ["height"] = height,
                    ["length"] = length
                }
            };
        }

        /// <summary>
        /// Gets information about a terrain.
        /// </summary>
        private static JObject GetTerrainInfo(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                throw new System.Exception("GameObject does not have a Terrain component");

            var data = terrain.terrainData;

            return new JObject
            {
                ["name"] = go.name,
                ["id"] = go.GetInstanceID(),
                ["position"] = new JObject
                {
                    ["x"] = go.transform.position.x,
                    ["y"] = go.transform.position.y,
                    ["z"] = go.transform.position.z
                },
                ["size"] = new JObject
                {
                    ["width"] = data.size.x,
                    ["height"] = data.size.y,
                    ["length"] = data.size.z
                },
                ["heightmapResolution"] = data.heightmapResolution,
                ["alphamapResolution"] = data.alphamapResolution,
                ["detailResolution"] = data.detailResolution,
                ["treeInstanceCount"] = data.treeInstanceCount,
                ["terrainLayerCount"] = data.terrainLayers?.Length ?? 0
            };
        }

        /// <summary>
        /// Sets the size of a terrain.
        /// </summary>
        private static JObject SetTerrainSize(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                throw new System.Exception("GameObject does not have a Terrain component");

            Undo.RecordObject(terrain.terrainData, "Set Terrain Size via MCP");

            var currentSize = terrain.terrainData.size;
            terrain.terrainData.size = new Vector3(
                p["width"]?.Value<float>() ?? currentSize.x,
                p["height"]?.Value<float>() ?? currentSize.y,
                p["length"]?.Value<float>() ?? currentSize.z
            );

            return new JObject
            {
                ["ok"] = true,
                ["size"] = new JObject
                {
                    ["width"] = terrain.terrainData.size.x,
                    ["height"] = terrain.terrainData.size.y,
                    ["length"] = terrain.terrainData.size.z
                }
            };
        }

        /// <summary>
        /// Sets terrain height at a specific point.
        /// </summary>
        private static JObject SetTerrainHeight(JObject p)
        {
            int id = p["id"].Value<int>();
            float x = p["x"]?.Value<float>() ?? 0;
            float z = p["z"]?.Value<float>() ?? 0;
            float height = p["height"]?.Value<float>() ?? 0;
            int radius = p["radius"]?.Value<int>() ?? 1;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                throw new System.Exception("GameObject does not have a Terrain component");

            var data = terrain.terrainData;
            Undo.RecordObject(data, "Set Terrain Height via MCP");

            // Convert world position to heightmap coordinates
            int resolution = data.heightmapResolution;
            int hx = Mathf.RoundToInt((x / data.size.x) * resolution);
            int hz = Mathf.RoundToInt((z / data.size.z) * resolution);

            // Normalize height (0-1)
            float normalizedHeight = height / data.size.y;

            // Apply height in a circular area
            float[,] heights = data.GetHeights(
                Mathf.Max(0, hx - radius),
                Mathf.Max(0, hz - radius),
                Mathf.Min(radius * 2, resolution - hx + radius),
                Mathf.Min(radius * 2, resolution - hz + radius)
            );

            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    float dist = Vector2.Distance(new Vector2(i, j), new Vector2(radius, radius));
                    if (dist <= radius)
                    {
                        float falloff = 1f - (dist / radius);
                        heights[i, j] = Mathf.Lerp(heights[i, j], normalizedHeight, falloff);
                    }
                }
            }

            data.SetHeights(
                Mathf.Max(0, hx - radius),
                Mathf.Max(0, hz - radius),
                heights
            );

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Gets terrain height at a specific point.
        /// </summary>
        private static JObject GetTerrainHeight(JObject p)
        {
            int id = p["id"].Value<int>();
            float x = p["x"]?.Value<float>() ?? 0;
            float z = p["z"]?.Value<float>() ?? 0;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                throw new System.Exception("GameObject does not have a Terrain component");

            // Get world-space height
            float height = terrain.SampleHeight(new Vector3(x, 0, z));

            return new JObject
            {
                ["height"] = height,
                ["worldPosition"] = new JObject
                {
                    ["x"] = x,
                    ["y"] = height + go.transform.position.y,
                    ["z"] = z
                }
            };
        }

        /// <summary>
        /// Lists all terrains in the scene.
        /// </summary>
        private static JObject ListTerrains(JObject p)
        {
            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var result = new JArray();

            foreach (var terrain in terrains)
            {
                result.Add(new JObject
                {
                    ["id"] = terrain.gameObject.GetInstanceID(),
                    ["name"] = terrain.gameObject.name,
                    ["position"] = new JObject
                    {
                        ["x"] = terrain.transform.position.x,
                        ["y"] = terrain.transform.position.y,
                        ["z"] = terrain.transform.position.z
                    },
                    ["size"] = new JObject
                    {
                        ["width"] = terrain.terrainData.size.x,
                        ["height"] = terrain.terrainData.size.y,
                        ["length"] = terrain.terrainData.size.z
                    }
                });
            }

            return new JObject
            {
                ["terrains"] = result,
                ["count"] = result.Count
            };
        }

        /// <summary>
        /// Sets a terrain layer (texture).
        /// </summary>
        private static JObject SetTerrainLayer(JObject p)
        {
            int id = p["id"].Value<int>();
            int layerIndex = p["layerIndex"]?.Value<int>() ?? 0;
            string texturePath = p["texturePath"]?.ToString();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                throw new System.Exception("GameObject does not have a Terrain component");

            if (string.IsNullOrEmpty(texturePath))
                throw new System.Exception("Texture path is required");

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new System.Exception($"Texture not found at path: {texturePath}");

            var data = terrain.terrainData;
            Undo.RecordObject(data, "Set Terrain Layer via MCP");

            // Create or get terrain layers
            var layers = data.terrainLayers ?? new TerrainLayer[0];
            
            // Expand array if needed
            if (layerIndex >= layers.Length)
            {
                var newLayers = new TerrainLayer[layerIndex + 1];
                layers.CopyTo(newLayers, 0);
                layers = newLayers;
            }

            // Create new terrain layer
            var layer = new TerrainLayer();
            layer.diffuseTexture = texture;
            layer.tileSize = new Vector2(
                p["tileSize"]?.Value<float>() ?? 10f,
                p["tileSize"]?.Value<float>() ?? 10f
            );

            // Save layer as asset
            string layerPath = $"Assets/TerrainLayer_{layerIndex}.terrainlayer";
            AssetDatabase.CreateAsset(layer, layerPath);

            layers[layerIndex] = layer;
            data.terrainLayers = layers;

            return new JObject
            {
                ["ok"] = true,
                ["layerIndex"] = layerIndex,
                ["texturePath"] = texturePath
            };
        }

        /// <summary>
        /// Flattens the entire terrain to a specific height.
        /// </summary>
        private static JObject FlattenTerrain(JObject p)
        {
            int id = p["id"].Value<int>();
            float height = p["height"]?.Value<float>() ?? 0;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var terrain = go.GetComponent<Terrain>();
            if (terrain == null)
                throw new System.Exception("GameObject does not have a Terrain component");

            var data = terrain.terrainData;
            Undo.RecordObject(data, "Flatten Terrain via MCP");

            int resolution = data.heightmapResolution;
            float normalizedHeight = height / data.size.y;

            float[,] heights = new float[resolution, resolution];
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    heights[i, j] = normalizedHeight;
                }
            }

            data.SetHeights(0, 0, heights);

            return new JObject
            {
                ["ok"] = true,
                ["height"] = height
            };
        }
    }
}

