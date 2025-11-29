using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for getting scene statistics and performance info.
    /// </summary>
    public static class SceneStatsController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_scene_stats", GetSceneStats);
            JsonRpcDispatcher.RegisterMethod("unity.get_render_stats", GetRenderStats);
            JsonRpcDispatcher.RegisterMethod("unity.get_memory_stats", GetMemoryStats);
            JsonRpcDispatcher.RegisterMethod("unity.get_object_counts", GetObjectCounts);
            JsonRpcDispatcher.RegisterMethod("unity.get_asset_stats", GetAssetStats);
            JsonRpcDispatcher.RegisterMethod("unity.analyze_scene", AnalyzeScene);
        }

        /// <summary>
        /// Gets comprehensive scene statistics.
        /// </summary>
        private static JObject GetSceneStats(JObject p)
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            
            int totalObjects = 0;
            int totalComponents = 0;
            int totalMeshFilters = 0;
            int totalRenderers = 0;
            int totalColliders = 0;
            int totalRigidbodies = 0;
            int totalLights = 0;
            int totalCameras = 0;
            int totalAudioSources = 0;
            int totalParticleSystems = 0;
            long totalVertices = 0;
            long totalTriangles = 0;

            foreach (var root in rootObjects)
            {
                CountObjectsRecursive(root.transform, ref totalObjects, ref totalComponents,
                    ref totalMeshFilters, ref totalRenderers, ref totalColliders,
                    ref totalRigidbodies, ref totalLights, ref totalCameras,
                    ref totalAudioSources, ref totalParticleSystems,
                    ref totalVertices, ref totalTriangles);
            }

            return new JObject
            {
                ["sceneName"] = scene.name,
                ["scenePath"] = scene.path,
                ["rootObjectCount"] = rootObjects.Length,
                ["totalGameObjects"] = totalObjects,
                ["totalComponents"] = totalComponents,
                ["meshFilters"] = totalMeshFilters,
                ["renderers"] = totalRenderers,
                ["colliders"] = totalColliders,
                ["rigidbodies"] = totalRigidbodies,
                ["lights"] = totalLights,
                ["cameras"] = totalCameras,
                ["audioSources"] = totalAudioSources,
                ["particleSystems"] = totalParticleSystems,
                ["geometry"] = new JObject
                {
                    ["totalVertices"] = totalVertices,
                    ["totalTriangles"] = totalTriangles
                }
            };
        }

        private static void CountObjectsRecursive(Transform transform, ref int totalObjects,
            ref int totalComponents, ref int totalMeshFilters, ref int totalRenderers,
            ref int totalColliders, ref int totalRigidbodies, ref int totalLights,
            ref int totalCameras, ref int totalAudioSources, ref int totalParticleSystems,
            ref long totalVertices, ref long totalTriangles)
        {
            totalObjects++;
            
            var components = transform.GetComponents<Component>();
            totalComponents += components.Length;

            foreach (var component in components)
            {
                if (component == null) continue;

                if (component is MeshFilter mf)
                {
                    totalMeshFilters++;
                    if (mf.sharedMesh != null)
                    {
                        totalVertices += mf.sharedMesh.vertexCount;
                        totalTriangles += mf.sharedMesh.triangles.Length / 3;
                    }
                }
                else if (component is Renderer) totalRenderers++;
                else if (component is Collider) totalColliders++;
                else if (component is Rigidbody) totalRigidbodies++;
                else if (component is Light) totalLights++;
                else if (component is Camera) totalCameras++;
                else if (component is AudioSource) totalAudioSources++;
                else if (component is ParticleSystem) totalParticleSystems++;
            }

            foreach (Transform child in transform)
            {
                CountObjectsRecursive(child, ref totalObjects, ref totalComponents,
                    ref totalMeshFilters, ref totalRenderers, ref totalColliders,
                    ref totalRigidbodies, ref totalLights, ref totalCameras,
                    ref totalAudioSources, ref totalParticleSystems,
                    ref totalVertices, ref totalTriangles);
            }
        }

        /// <summary>
        /// Gets render-related statistics (requires Play mode for some stats).
        /// </summary>
        private static JObject GetRenderStats(JObject p)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            int visibleRenderers = renderers.Count(r => r.isVisible);
            int shadowCastingLights = lights.Count(l => l.shadows != LightShadows.None);

            // Get quality settings info
            var qualityLevel = QualitySettings.GetQualityLevel();
            var qualityNames = QualitySettings.names;

            return new JObject
            {
                ["cameras"] = cameras.Length,
                ["lights"] = lights.Length,
                ["shadowCastingLights"] = shadowCastingLights,
                ["renderers"] = renderers.Length,
                ["visibleRenderers"] = visibleRenderers,
                ["qualitySettings"] = new JObject
                {
                    ["currentLevel"] = qualityLevel,
                    ["levelName"] = qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "Unknown",
                    ["vSyncCount"] = QualitySettings.vSyncCount,
                    ["targetFrameRate"] = Application.targetFrameRate,
                    ["antiAliasing"] = QualitySettings.antiAliasing,
                    ["shadowQuality"] = QualitySettings.shadows.ToString(),
                    ["shadowResolution"] = QualitySettings.shadowResolution.ToString()
                },
                ["graphicsDevice"] = new JObject
                {
                    ["name"] = SystemInfo.graphicsDeviceName,
                    ["type"] = SystemInfo.graphicsDeviceType.ToString(),
                    ["memorySize"] = SystemInfo.graphicsMemorySize,
                    ["shaderLevel"] = SystemInfo.graphicsShaderLevel
                }
            };
        }

        /// <summary>
        /// Gets memory statistics.
        /// </summary>
        private static JObject GetMemoryStats(JObject p)
        {
            return new JObject
            {
                ["system"] = new JObject
                {
                    ["totalMemory"] = SystemInfo.systemMemorySize,
                    ["graphicsMemory"] = SystemInfo.graphicsMemorySize
                },
                ["unity"] = new JObject
                {
                    ["monoHeapSize"] = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong(),
                    ["monoUsedSize"] = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),
                    ["totalAllocatedMemory"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                    ["totalReservedMemory"] = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong(),
                    ["totalUnusedReservedMemory"] = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong()
                },
                ["textures"] = new JObject
                {
                    ["maxTextureSize"] = SystemInfo.maxTextureSize,
                    ["npotSupport"] = SystemInfo.npotSupport.ToString()
                }
            };
        }

        /// <summary>
        /// Gets counts of different object types in the scene.
        /// </summary>
        private static JObject GetObjectCounts(JObject p)
        {
            var counts = new JObject();

            // Count common component types
            counts["Transform"] = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["MeshRenderer"] = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["SkinnedMeshRenderer"] = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["MeshFilter"] = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["BoxCollider"] = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["SphereCollider"] = Object.FindObjectsByType<SphereCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["CapsuleCollider"] = Object.FindObjectsByType<CapsuleCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["MeshCollider"] = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["Rigidbody"] = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["Light"] = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["Camera"] = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["AudioSource"] = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["ParticleSystem"] = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["Canvas"] = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["Animator"] = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            counts["NavMeshAgent"] = Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            return new JObject
            {
                ["componentCounts"] = counts
            };
        }

        /// <summary>
        /// Gets statistics about project assets.
        /// </summary>
        private static JObject GetAssetStats(JObject p)
        {
            int materials = AssetDatabase.FindAssets("t:Material").Length;
            int textures = AssetDatabase.FindAssets("t:Texture").Length;
            int meshes = AssetDatabase.FindAssets("t:Mesh").Length;
            int prefabs = AssetDatabase.FindAssets("t:Prefab").Length;
            int scripts = AssetDatabase.FindAssets("t:Script").Length;
            int scenes = AssetDatabase.FindAssets("t:Scene").Length;
            int audioClips = AssetDatabase.FindAssets("t:AudioClip").Length;
            int animationClips = AssetDatabase.FindAssets("t:AnimationClip").Length;
            int shaders = AssetDatabase.FindAssets("t:Shader").Length;
            int fonts = AssetDatabase.FindAssets("t:Font").Length;

            return new JObject
            {
                ["materials"] = materials,
                ["textures"] = textures,
                ["meshes"] = meshes,
                ["prefabs"] = prefabs,
                ["scripts"] = scripts,
                ["scenes"] = scenes,
                ["audioClips"] = audioClips,
                ["animationClips"] = animationClips,
                ["shaders"] = shaders,
                ["fonts"] = fonts,
                ["totalAssets"] = materials + textures + meshes + prefabs + scripts + scenes + audioClips + animationClips + shaders + fonts
            };
        }

        /// <summary>
        /// Analyzes the scene and provides optimization suggestions.
        /// </summary>
        private static JObject AnalyzeScene(JObject p)
        {
            var warnings = new JArray();
            var suggestions = new JArray();

            // Get scene stats first
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            int totalObjects = 0;
            long totalVertices = 0;
            long totalTriangles = 0;
            int meshColliderCount = 0;
            int realtimeLightCount = 0;

            foreach (var root in rootObjects)
            {
                AnalyzeObjectsRecursive(root.transform, ref totalObjects, ref totalVertices, 
                    ref totalTriangles, ref meshColliderCount, ref realtimeLightCount);
            }

            // Generate warnings and suggestions
            if (totalObjects > 10000)
                warnings.Add($"High object count: {totalObjects} GameObjects may impact performance");
            
            if (totalVertices > 1000000)
                warnings.Add($"High vertex count: {totalVertices:N0} vertices in scene");

            if (meshColliderCount > 50)
                warnings.Add($"Many MeshColliders ({meshColliderCount}) - consider using primitive colliders");

            var realtimeLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(l => l.lightmapBakeType == LightmapBakeType.Realtime).Count();
            if (realtimeLights > 4)
                warnings.Add($"Many realtime lights ({realtimeLights}) may impact performance");

            // Check for missing references
            int missingRefs = 0;
            foreach (var root in rootObjects)
            {
                missingRefs += CountMissingReferences(root);
            }
            if (missingRefs > 0)
                warnings.Add($"Found {missingRefs} missing component/script references");

            // Suggestions
            if (totalTriangles > 500000)
                suggestions.Add("Consider using LOD (Level of Detail) for distant objects");
            
            if (Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 1)
                suggestions.Add("Multiple cameras active - ensure this is intentional");

            var staticObjects = rootObjects.SelectMany(r => r.GetComponentsInChildren<Transform>())
                .Count(t => t.gameObject.isStatic);
            if (staticObjects < totalObjects / 2)
                suggestions.Add($"Only {staticObjects}/{totalObjects} objects are static - mark static objects for batching");

            return new JObject
            {
                ["summary"] = new JObject
                {
                    ["totalObjects"] = totalObjects,
                    ["totalVertices"] = totalVertices,
                    ["totalTriangles"] = totalTriangles,
                    ["meshColliders"] = meshColliderCount,
                    ["realtimeLights"] = realtimeLightCount
                },
                ["warnings"] = warnings,
                ["warningCount"] = warnings.Count,
                ["suggestions"] = suggestions,
                ["suggestionCount"] = suggestions.Count
            };
        }

        private static void AnalyzeObjectsRecursive(Transform transform, ref int totalObjects,
            ref long totalVertices, ref long totalTriangles, ref int meshColliderCount,
            ref int realtimeLightCount)
        {
            totalObjects++;

            var mf = transform.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                totalVertices += mf.sharedMesh.vertexCount;
                totalTriangles += mf.sharedMesh.triangles.Length / 3;
            }

            if (transform.GetComponent<MeshCollider>() != null)
                meshColliderCount++;

            var light = transform.GetComponent<Light>();
            if (light != null && light.lightmapBakeType == LightmapBakeType.Realtime)
                realtimeLightCount++;

            foreach (Transform child in transform)
            {
                AnalyzeObjectsRecursive(child, ref totalObjects, ref totalVertices,
                    ref totalTriangles, ref meshColliderCount, ref realtimeLightCount);
            }
        }

        private static int CountMissingReferences(GameObject go)
        {
            int count = 0;
            var components = go.GetComponents<Component>();
            
            foreach (var component in components)
            {
                if (component == null)
                    count++;
            }

            foreach (Transform child in go.transform)
            {
                count += CountMissingReferences(child.gameObject);
            }

            return count;
        }
    }
}

