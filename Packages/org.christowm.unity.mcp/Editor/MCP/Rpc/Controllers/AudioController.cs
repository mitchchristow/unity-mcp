using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing audio in Unity.
    /// </summary>
    public static class AudioController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.create_audio_source", CreateAudioSource);
            JsonRpcDispatcher.RegisterMethod("unity.set_audio_source_property", SetAudioSourceProperty);
            JsonRpcDispatcher.RegisterMethod("unity.get_audio_source_info", GetAudioSourceInfo);
            JsonRpcDispatcher.RegisterMethod("unity.play_audio", PlayAudio);
            JsonRpcDispatcher.RegisterMethod("unity.stop_audio", StopAudio);
            JsonRpcDispatcher.RegisterMethod("unity.list_audio_clips", ListAudioClips);
            JsonRpcDispatcher.RegisterMethod("unity.set_audio_clip", SetAudioClip);
            JsonRpcDispatcher.RegisterMethod("unity.get_audio_settings", GetAudioSettings);
        }

        /// <summary>
        /// Creates an AudioSource component on a GameObject.
        /// </summary>
        private static JObject CreateAudioSource(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var existingSource = go.GetComponent<AudioSource>();
            if (existingSource != null)
            {
                return new JObject
                {
                    ["ok"] = true,
                    ["audioSourceId"] = existingSource.GetInstanceID(),
                    ["alreadyExists"] = true
                };
            }

            var audioSource = Undo.AddComponent<AudioSource>(go);

            // Set initial properties if provided
            if (p["volume"] != null)
                audioSource.volume = p["volume"].Value<float>();
            if (p["pitch"] != null)
                audioSource.pitch = p["pitch"].Value<float>();
            if (p["loop"] != null)
                audioSource.loop = p["loop"].Value<bool>();
            if (p["playOnAwake"] != null)
                audioSource.playOnAwake = p["playOnAwake"].Value<bool>();
            if (p["spatialBlend"] != null)
                audioSource.spatialBlend = p["spatialBlend"].Value<float>();

            // Set audio clip if path provided
            if (p["clipPath"] != null)
            {
                string clipPath = p["clipPath"].ToString();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                {
                    audioSource.clip = clip;
                }
            }

            return new JObject
            {
                ["ok"] = true,
                ["audioSourceId"] = audioSource.GetInstanceID()
            };
        }

        /// <summary>
        /// Sets properties on an AudioSource.
        /// </summary>
        private static JObject SetAudioSourceProperty(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var audioSource = go.GetComponent<AudioSource>();
            if (audioSource == null)
                throw new System.Exception("GameObject does not have an AudioSource component");

            Undo.RecordObject(audioSource, "Set AudioSource Property via MCP");

            if (p["volume"] != null)
                audioSource.volume = p["volume"].Value<float>();
            if (p["pitch"] != null)
                audioSource.pitch = p["pitch"].Value<float>();
            if (p["loop"] != null)
                audioSource.loop = p["loop"].Value<bool>();
            if (p["playOnAwake"] != null)
                audioSource.playOnAwake = p["playOnAwake"].Value<bool>();
            if (p["spatialBlend"] != null)
                audioSource.spatialBlend = p["spatialBlend"].Value<float>();
            if (p["mute"] != null)
                audioSource.mute = p["mute"].Value<bool>();
            if (p["priority"] != null)
                audioSource.priority = p["priority"].Value<int>();
            if (p["minDistance"] != null)
                audioSource.minDistance = p["minDistance"].Value<float>();
            if (p["maxDistance"] != null)
                audioSource.maxDistance = p["maxDistance"].Value<float>();

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Gets information about an AudioSource.
        /// </summary>
        private static JObject GetAudioSourceInfo(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var audioSource = go.GetComponent<AudioSource>();
            if (audioSource == null)
                throw new System.Exception("GameObject does not have an AudioSource component");

            return new JObject
            {
                ["gameObject"] = go.name,
                ["clip"] = audioSource.clip?.name,
                ["clipPath"] = audioSource.clip != null ? AssetDatabase.GetAssetPath(audioSource.clip) : null,
                ["volume"] = audioSource.volume,
                ["pitch"] = audioSource.pitch,
                ["loop"] = audioSource.loop,
                ["playOnAwake"] = audioSource.playOnAwake,
                ["spatialBlend"] = audioSource.spatialBlend,
                ["mute"] = audioSource.mute,
                ["priority"] = audioSource.priority,
                ["minDistance"] = audioSource.minDistance,
                ["maxDistance"] = audioSource.maxDistance,
                ["isPlaying"] = audioSource.isPlaying,
                ["time"] = audioSource.time
            };
        }

        /// <summary>
        /// Plays audio on an AudioSource (only works in Play mode).
        /// </summary>
        private static JObject PlayAudio(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var audioSource = go.GetComponent<AudioSource>();
            if (audioSource == null)
                throw new System.Exception("GameObject does not have an AudioSource component");

            if (!Application.isPlaying)
            {
                return new JObject
                {
                    ["ok"] = false,
                    ["error"] = "Audio playback only works in Play mode"
                };
            }

            audioSource.Play();

            return new JObject
            {
                ["ok"] = true,
                ["clip"] = audioSource.clip?.name
            };
        }

        /// <summary>
        /// Stops audio on an AudioSource.
        /// </summary>
        private static JObject StopAudio(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var audioSource = go.GetComponent<AudioSource>();
            if (audioSource == null)
                throw new System.Exception("GameObject does not have an AudioSource component");

            audioSource.Stop();

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Lists all audio clips in the project.
        /// </summary>
        private static JObject ListAudioClips(JObject p)
        {
            string folder = p["folder"]?.ToString() ?? "Assets";
            int limit = p["limit"]?.Value<int>() ?? 50;
            
            var clips = new JArray();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            
            int count = 0;
            foreach (string guid in guids)
            {
                if (count >= limit) break;
                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                
                if (clip != null)
                {
                    clips.Add(new JObject
                    {
                        ["path"] = path,
                        ["name"] = clip.name,
                        ["length"] = clip.length,
                        ["channels"] = clip.channels,
                        ["frequency"] = clip.frequency,
                        ["samples"] = clip.samples,
                        ["loadType"] = clip.loadType.ToString()
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
        /// Sets the audio clip on an AudioSource.
        /// </summary>
        private static JObject SetAudioClip(JObject p)
        {
            int id = p["id"].Value<int>();
            string clipPath = p["clipPath"]?.ToString();
            
            if (string.IsNullOrEmpty(clipPath))
                throw new System.Exception("Clip path is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var audioSource = go.GetComponent<AudioSource>();
            if (audioSource == null)
                throw new System.Exception("GameObject does not have an AudioSource component");

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
                throw new System.Exception($"Audio clip not found at path: {clipPath}");

            Undo.RecordObject(audioSource, "Set Audio Clip via MCP");
            audioSource.clip = clip;

            return new JObject
            {
                ["ok"] = true,
                ["clip"] = clip.name
            };
        }

        /// <summary>
        /// Gets global audio settings.
        /// </summary>
        private static JObject GetAudioSettings(JObject p)
        {
            return new JObject
            {
                ["globalVolume"] = AudioListener.volume,
                ["pause"] = AudioListener.pause,
                ["speakerMode"] = AudioSettings.speakerMode.ToString(),
                ["dspBufferSize"] = AudioSettings.GetConfiguration().dspBufferSize,
                ["sampleRate"] = AudioSettings.outputSampleRate
            };
        }
    }
}

