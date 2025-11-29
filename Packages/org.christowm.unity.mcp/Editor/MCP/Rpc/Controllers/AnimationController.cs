using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing animations and animators in Unity.
    /// </summary>
    public static class AnimationController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_animator_info", GetAnimatorInfo);
            JsonRpcDispatcher.RegisterMethod("unity.set_animator_parameter", SetAnimatorParameter);
            JsonRpcDispatcher.RegisterMethod("unity.get_animator_parameters", GetAnimatorParameters);
            JsonRpcDispatcher.RegisterMethod("unity.play_animator_state", PlayAnimatorState);
            JsonRpcDispatcher.RegisterMethod("unity.list_animation_clips", ListAnimationClips);
            JsonRpcDispatcher.RegisterMethod("unity.get_animation_clip_info", GetAnimationClipInfo);
        }

        /// <summary>
        /// Gets information about an Animator component on a GameObject.
        /// </summary>
        private static JObject GetAnimatorInfo(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var animator = go.GetComponent<Animator>();
            if (animator == null)
                throw new System.Exception("GameObject does not have an Animator component");

            var result = new JObject
            {
                ["gameObject"] = go.name,
                ["hasController"] = animator.runtimeAnimatorController != null,
                ["controllerName"] = animator.runtimeAnimatorController?.name,
                ["applyRootMotion"] = animator.applyRootMotion,
                ["updateMode"] = animator.updateMode.ToString(),
                ["cullingMode"] = animator.cullingMode.ToString(),
                ["speed"] = animator.speed,
                ["isHuman"] = animator.isHuman,
                ["hasRootMotion"] = animator.hasRootMotion
            };

            // Get current state info if playing
            if (Application.isPlaying && animator.runtimeAnimatorController != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                result["currentState"] = new JObject
                {
                    ["normalizedTime"] = stateInfo.normalizedTime,
                    ["length"] = stateInfo.length,
                    ["speed"] = stateInfo.speed,
                    ["isLooping"] = stateInfo.loop
                };
            }

            return result;
        }

        /// <summary>
        /// Sets a parameter on an Animator.
        /// </summary>
        private static JObject SetAnimatorParameter(JObject p)
        {
            int id = p["id"].Value<int>();
            string paramName = p["name"]?.ToString();
            
            if (string.IsNullOrEmpty(paramName))
                throw new System.Exception("Parameter name is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var animator = go.GetComponent<Animator>();
            if (animator == null)
                throw new System.Exception("GameObject does not have an Animator component");

            // Determine parameter type and set value
            if (p["floatValue"] != null)
            {
                animator.SetFloat(paramName, p["floatValue"].Value<float>());
            }
            else if (p["intValue"] != null)
            {
                animator.SetInteger(paramName, p["intValue"].Value<int>());
            }
            else if (p["boolValue"] != null)
            {
                animator.SetBool(paramName, p["boolValue"].Value<bool>());
            }
            else if (p["trigger"] != null && p["trigger"].Value<bool>())
            {
                animator.SetTrigger(paramName);
            }
            else
            {
                throw new System.Exception("Must specify floatValue, intValue, boolValue, or trigger");
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Gets all parameters from an Animator.
        /// </summary>
        private static JObject GetAnimatorParameters(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var animator = go.GetComponent<Animator>();
            if (animator == null)
                throw new System.Exception("GameObject does not have an Animator component");

            var parameters = new JArray();
            
            foreach (var param in animator.parameters)
            {
                var paramInfo = new JObject
                {
                    ["name"] = param.name,
                    ["type"] = param.type.ToString()
                };

                // Get current value based on type
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        paramInfo["value"] = animator.GetFloat(param.name);
                        paramInfo["defaultValue"] = param.defaultFloat;
                        break;
                    case AnimatorControllerParameterType.Int:
                        paramInfo["value"] = animator.GetInteger(param.name);
                        paramInfo["defaultValue"] = param.defaultInt;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        paramInfo["value"] = animator.GetBool(param.name);
                        paramInfo["defaultValue"] = param.defaultBool;
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        paramInfo["value"] = "trigger";
                        break;
                }

                parameters.Add(paramInfo);
            }

            return new JObject
            {
                ["gameObject"] = go.name,
                ["parameters"] = parameters,
                ["count"] = parameters.Count
            };
        }

        /// <summary>
        /// Plays a specific animator state.
        /// </summary>
        private static JObject PlayAnimatorState(JObject p)
        {
            int id = p["id"].Value<int>();
            string stateName = p["stateName"]?.ToString();
            int layer = p["layer"]?.Value<int>() ?? 0;
            float normalizedTime = p["normalizedTime"]?.Value<float>() ?? 0f;
            
            if (string.IsNullOrEmpty(stateName))
                throw new System.Exception("State name is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var animator = go.GetComponent<Animator>();
            if (animator == null)
                throw new System.Exception("GameObject does not have an Animator component");

            animator.Play(stateName, layer, normalizedTime);

            return new JObject
            {
                ["ok"] = true,
                ["state"] = stateName,
                ["layer"] = layer
            };
        }

        /// <summary>
        /// Lists all animation clips in the project.
        /// </summary>
        private static JObject ListAnimationClips(JObject p)
        {
            string folder = p["folder"]?.ToString() ?? "Assets";
            int limit = p["limit"]?.Value<int>() ?? 50;
            
            var clips = new JArray();
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
            
            int count = 0;
            foreach (string guid in guids)
            {
                if (count >= limit) break;
                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                
                if (clip != null)
                {
                    clips.Add(new JObject
                    {
                        ["path"] = path,
                        ["name"] = clip.name,
                        ["length"] = clip.length,
                        ["frameRate"] = clip.frameRate,
                        ["isLooping"] = clip.isLooping,
                        ["legacy"] = clip.legacy
                    });
                    count++;
                }
            }

            return new JObject
            {
                ["clips"] = clips,
                ["count"] = count,
                ["totalFound"] = guids.Length,
                ["truncated"] = guids.Length > limit
            };
        }

        /// <summary>
        /// Gets detailed information about an animation clip.
        /// </summary>
        private static JObject GetAnimationClipInfo(JObject p)
        {
            string path = p["path"]?.ToString();
            
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("Path is required");

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
                throw new System.Exception($"Animation clip not found at path: {path}");

            var events = new JArray();
            foreach (var evt in clip.events)
            {
                events.Add(new JObject
                {
                    ["time"] = evt.time,
                    ["functionName"] = evt.functionName,
                    ["intParameter"] = evt.intParameter,
                    ["floatParameter"] = evt.floatParameter,
                    ["stringParameter"] = evt.stringParameter
                });
            }

            return new JObject
            {
                ["path"] = path,
                ["name"] = clip.name,
                ["length"] = clip.length,
                ["frameRate"] = clip.frameRate,
                ["isLooping"] = clip.isLooping,
                ["legacy"] = clip.legacy,
                ["wrapMode"] = clip.wrapMode.ToString(),
                ["hasMotionCurves"] = clip.hasMotionCurves,
                ["hasRootCurves"] = clip.hasRootCurves,
                ["events"] = events,
                ["eventCount"] = events.Count
            };
        }
    }
}

