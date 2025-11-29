using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing the build pipeline in Unity.
    /// </summary>
    public static class BuildController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_build_settings", GetBuildSettings);
            JsonRpcDispatcher.RegisterMethod("unity.set_build_target", SetBuildTarget);
            JsonRpcDispatcher.RegisterMethod("unity.add_scene_to_build", AddSceneToBuild);
            JsonRpcDispatcher.RegisterMethod("unity.remove_scene_from_build", RemoveSceneFromBuild);
            JsonRpcDispatcher.RegisterMethod("unity.get_scenes_in_build", GetScenesInBuild);
            JsonRpcDispatcher.RegisterMethod("unity.build_player", BuildPlayer);
            JsonRpcDispatcher.RegisterMethod("unity.get_build_target_list", GetBuildTargetList);
        }

        /// <summary>
        /// Gets the current build settings.
        /// </summary>
        private static JObject GetBuildSettings(JObject p)
        {
            var scenes = new JArray();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                scenes.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled,
                    ["guid"] = scene.guid.ToString()
                });
            }

            return new JObject
            {
                ["activeBuildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["activeScriptCompilationDefines"] = new JArray(EditorUserBuildSettings.activeScriptCompilationDefines),
                ["development"] = EditorUserBuildSettings.development,
                ["allowDebugging"] = EditorUserBuildSettings.allowDebugging,
                ["buildAppBundle"] = EditorUserBuildSettings.buildAppBundle,
                ["scenes"] = scenes,
                ["sceneCount"] = scenes.Count
            };
        }

        /// <summary>
        /// Sets the active build target.
        /// </summary>
        private static JObject SetBuildTarget(JObject p)
        {
            string targetName = p["target"]?.ToString();
            string targetGroupName = p["targetGroup"]?.ToString();

            if (string.IsNullOrEmpty(targetName))
                throw new System.Exception("Target is required");

            if (!System.Enum.TryParse(targetName, true, out BuildTarget target))
                throw new System.Exception($"Invalid build target: {targetName}");

            BuildTargetGroup targetGroup = BuildTargetGroup.Unknown;
            if (!string.IsNullOrEmpty(targetGroupName))
            {
                if (!System.Enum.TryParse(targetGroupName, true, out targetGroup))
                    throw new System.Exception($"Invalid build target group: {targetGroupName}");
            }
            else
            {
                // Try to infer target group from target
                targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            }

            bool success = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);

            return new JObject
            {
                ["ok"] = success,
                ["target"] = target.ToString(),
                ["targetGroup"] = targetGroup.ToString()
            };
        }

        /// <summary>
        /// Adds a scene to the build settings.
        /// </summary>
        private static JObject AddSceneToBuild(JObject p)
        {
            string scenePath = p["path"]?.ToString();
            bool enabled = p["enabled"]?.Value<bool>() ?? true;

            if (string.IsNullOrEmpty(scenePath))
                throw new System.Exception("Scene path is required");

            // Check if scene exists
            if (!System.IO.File.Exists(scenePath))
                throw new System.Exception($"Scene not found: {scenePath}");

            var scenes = EditorBuildSettings.scenes.ToList();

            // Check if already in build
            var existingScene = scenes.FirstOrDefault(s => s.path == scenePath);
            if (existingScene != null)
            {
                // Update enabled state
                existingScene.enabled = enabled;
                EditorBuildSettings.scenes = scenes.ToArray();
                
                return new JObject
                {
                    ["ok"] = true,
                    ["path"] = scenePath,
                    ["alreadyExists"] = true,
                    ["enabled"] = enabled
                };
            }

            // Add new scene
            scenes.Add(new EditorBuildSettingsScene(scenePath, enabled));
            EditorBuildSettings.scenes = scenes.ToArray();

            return new JObject
            {
                ["ok"] = true,
                ["path"] = scenePath,
                ["enabled"] = enabled,
                ["index"] = scenes.Count - 1
            };
        }

        /// <summary>
        /// Removes a scene from the build settings.
        /// </summary>
        private static JObject RemoveSceneFromBuild(JObject p)
        {
            string scenePath = p["path"]?.ToString();
            int? index = p["index"]?.Value<int>();

            var scenes = EditorBuildSettings.scenes.ToList();

            if (!string.IsNullOrEmpty(scenePath))
            {
                scenes.RemoveAll(s => s.path == scenePath);
            }
            else if (index.HasValue)
            {
                if (index.Value >= 0 && index.Value < scenes.Count)
                {
                    scenes.RemoveAt(index.Value);
                }
                else
                {
                    throw new System.Exception($"Invalid scene index: {index.Value}");
                }
            }
            else
            {
                throw new System.Exception("Either path or index is required");
            }

            EditorBuildSettings.scenes = scenes.ToArray();

            return new JObject
            {
                ["ok"] = true,
                ["remainingScenes"] = scenes.Count
            };
        }

        /// <summary>
        /// Gets all scenes in the build settings.
        /// </summary>
        private static JObject GetScenesInBuild(JObject p)
        {
            var scenes = new JArray();
            int index = 0;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                scenes.Add(new JObject
                {
                    ["index"] = index++,
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled
                });
            }

            return new JObject
            {
                ["scenes"] = scenes,
                ["count"] = scenes.Count
            };
        }

        /// <summary>
        /// Builds the player.
        /// </summary>
        private static JObject BuildPlayer(JObject p)
        {
            string locationPath = p["locationPath"]?.ToString();
            string targetName = p["target"]?.ToString();
            bool development = p["development"]?.Value<bool>() ?? false;

            if (string.IsNullOrEmpty(locationPath))
                throw new System.Exception("Location path is required");

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            if (!string.IsNullOrEmpty(targetName))
            {
                if (!System.Enum.TryParse(targetName, true, out target))
                    throw new System.Exception($"Invalid build target: {targetName}");
            }

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new System.Exception("No scenes enabled in build settings");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPath,
                target = target,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            var result = new JObject
            {
                ["success"] = report.summary.result == BuildResult.Succeeded,
                ["result"] = report.summary.result.ToString(),
                ["outputPath"] = report.summary.outputPath,
                ["totalSize"] = report.summary.totalSize,
                ["totalTime"] = report.summary.totalTime.TotalSeconds,
                ["totalErrors"] = report.summary.totalErrors,
                ["totalWarnings"] = report.summary.totalWarnings
            };

            // Add errors if any
            if (report.summary.totalErrors > 0)
            {
                var errors = new JArray();
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                        {
                            errors.Add(message.content);
                        }
                    }
                }
                result["errors"] = errors;
            }

            return result;
        }

        /// <summary>
        /// Gets a list of available build targets.
        /// </summary>
        private static JObject GetBuildTargetList(JObject p)
        {
            var targets = new JArray();
            
            // Common build targets
            var commonTargets = new[]
            {
                BuildTarget.StandaloneWindows64,
                BuildTarget.StandaloneOSX,
                BuildTarget.StandaloneLinux64,
                BuildTarget.iOS,
                BuildTarget.Android,
                BuildTarget.WebGL,
                BuildTarget.PS4,
                BuildTarget.PS5,
                BuildTarget.XboxOne,
                BuildTarget.Switch
            };

            foreach (var target in commonTargets)
            {
                bool isSupported = BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target);
                targets.Add(new JObject
                {
                    ["name"] = target.ToString(),
                    ["supported"] = isSupported,
                    ["isActive"] = target == EditorUserBuildSettings.activeBuildTarget
                });
            }

            return new JObject
            {
                ["targets"] = targets,
                ["activeBuildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString()
            };
        }
    }
}

