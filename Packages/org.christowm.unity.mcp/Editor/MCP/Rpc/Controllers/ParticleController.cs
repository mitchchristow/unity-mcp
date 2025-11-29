using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing particle systems in Unity.
    /// </summary>
    public static class ParticleController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.create_particle_system", CreateParticleSystem);
            JsonRpcDispatcher.RegisterMethod("unity.get_particle_system_info", GetParticleSystemInfo);
            JsonRpcDispatcher.RegisterMethod("unity.set_particle_main", SetParticleMain);
            JsonRpcDispatcher.RegisterMethod("unity.set_particle_emission", SetParticleEmission);
            JsonRpcDispatcher.RegisterMethod("unity.set_particle_shape", SetParticleShape);
            JsonRpcDispatcher.RegisterMethod("unity.play_particle_system", PlayParticleSystem);
            JsonRpcDispatcher.RegisterMethod("unity.stop_particle_system", StopParticleSystem);
            JsonRpcDispatcher.RegisterMethod("unity.list_particle_systems", ListParticleSystems);
        }

        /// <summary>
        /// Creates a new particle system.
        /// </summary>
        private static JObject CreateParticleSystem(JObject p)
        {
            string name = p["name"]?.ToString() ?? "Particle System";

            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();

            // Set position if provided
            if (p["position"] != null)
            {
                go.transform.position = new Vector3(
                    p["position"]["x"]?.Value<float>() ?? 0,
                    p["position"]["y"]?.Value<float>() ?? 0,
                    p["position"]["z"]?.Value<float>() ?? 0
                );
            }

            // Set parent if provided
            if (p["parentId"] != null)
            {
                var parent = EditorUtility.InstanceIDToObject(p["parentId"].Value<int>()) as GameObject;
                if (parent != null)
                {
                    go.transform.SetParent(parent.transform, true);
                }
            }

            // Configure main module with defaults or provided values
            var main = ps.main;
            if (p["duration"] != null)
                main.duration = p["duration"].Value<float>();
            if (p["startLifetime"] != null)
                main.startLifetime = p["startLifetime"].Value<float>();
            if (p["startSpeed"] != null)
                main.startSpeed = p["startSpeed"].Value<float>();
            if (p["startSize"] != null)
                main.startSize = p["startSize"].Value<float>();
            if (p["maxParticles"] != null)
                main.maxParticles = p["maxParticles"].Value<int>();
            if (p["loop"] != null)
                main.loop = p["loop"].Value<bool>();
            if (p["startColor"] != null)
            {
                main.startColor = new Color(
                    p["startColor"]["r"]?.Value<float>() ?? 1,
                    p["startColor"]["g"]?.Value<float>() ?? 1,
                    p["startColor"]["b"]?.Value<float>() ?? 1,
                    p["startColor"]["a"]?.Value<float>() ?? 1
                );
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Particle System via MCP");

            return new JObject
            {
                ["id"] = go.GetInstanceID(),
                ["name"] = go.name
            };
        }

        /// <summary>
        /// Gets information about a particle system.
        /// </summary>
        private static JObject GetParticleSystemInfo(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                throw new System.Exception("GameObject does not have a ParticleSystem component");

            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;

            return new JObject
            {
                ["name"] = go.name,
                ["id"] = go.GetInstanceID(),
                ["isPlaying"] = ps.isPlaying,
                ["isPaused"] = ps.isPaused,
                ["isStopped"] = ps.isStopped,
                ["particleCount"] = ps.particleCount,
                ["main"] = new JObject
                {
                    ["duration"] = main.duration,
                    ["loop"] = main.loop,
                    ["startLifetime"] = main.startLifetime.constant,
                    ["startSpeed"] = main.startSpeed.constant,
                    ["startSize"] = main.startSize.constant,
                    ["maxParticles"] = main.maxParticles,
                    ["simulationSpace"] = main.simulationSpace.ToString(),
                    ["playOnAwake"] = main.playOnAwake
                },
                ["emission"] = new JObject
                {
                    ["enabled"] = emission.enabled,
                    ["rateOverTime"] = emission.rateOverTime.constant
                },
                ["shape"] = new JObject
                {
                    ["enabled"] = shape.enabled,
                    ["shapeType"] = shape.shapeType.ToString(),
                    ["radius"] = shape.radius,
                    ["angle"] = shape.angle
                }
            };
        }

        /// <summary>
        /// Sets properties on the main module.
        /// </summary>
        private static JObject SetParticleMain(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                throw new System.Exception("GameObject does not have a ParticleSystem component");

            Undo.RecordObject(ps, "Set Particle Main via MCP");

            var main = ps.main;

            if (p["duration"] != null)
                main.duration = p["duration"].Value<float>();
            if (p["loop"] != null)
                main.loop = p["loop"].Value<bool>();
            if (p["startLifetime"] != null)
                main.startLifetime = p["startLifetime"].Value<float>();
            if (p["startSpeed"] != null)
                main.startSpeed = p["startSpeed"].Value<float>();
            if (p["startSize"] != null)
                main.startSize = p["startSize"].Value<float>();
            if (p["maxParticles"] != null)
                main.maxParticles = p["maxParticles"].Value<int>();
            if (p["gravityModifier"] != null)
                main.gravityModifier = p["gravityModifier"].Value<float>();
            if (p["simulationSpeed"] != null)
                main.simulationSpeed = p["simulationSpeed"].Value<float>();
            if (p["playOnAwake"] != null)
                main.playOnAwake = p["playOnAwake"].Value<bool>();
            if (p["startColor"] != null)
            {
                main.startColor = new Color(
                    p["startColor"]["r"]?.Value<float>() ?? 1,
                    p["startColor"]["g"]?.Value<float>() ?? 1,
                    p["startColor"]["b"]?.Value<float>() ?? 1,
                    p["startColor"]["a"]?.Value<float>() ?? 1
                );
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Sets properties on the emission module.
        /// </summary>
        private static JObject SetParticleEmission(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                throw new System.Exception("GameObject does not have a ParticleSystem component");

            Undo.RecordObject(ps, "Set Particle Emission via MCP");

            var emission = ps.emission;

            if (p["enabled"] != null)
                emission.enabled = p["enabled"].Value<bool>();
            if (p["rateOverTime"] != null)
                emission.rateOverTime = p["rateOverTime"].Value<float>();
            if (p["rateOverDistance"] != null)
                emission.rateOverDistance = p["rateOverDistance"].Value<float>();

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Sets properties on the shape module.
        /// </summary>
        private static JObject SetParticleShape(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                throw new System.Exception("GameObject does not have a ParticleSystem component");

            Undo.RecordObject(ps, "Set Particle Shape via MCP");

            var shape = ps.shape;

            if (p["enabled"] != null)
                shape.enabled = p["enabled"].Value<bool>();
            if (p["shapeType"] != null)
            {
                if (System.Enum.TryParse(p["shapeType"].ToString(), true, out ParticleSystemShapeType shapeType))
                {
                    shape.shapeType = shapeType;
                }
            }
            if (p["radius"] != null)
                shape.radius = p["radius"].Value<float>();
            if (p["angle"] != null)
                shape.angle = p["angle"].Value<float>();
            if (p["arc"] != null)
                shape.arc = p["arc"].Value<float>();

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Plays a particle system.
        /// </summary>
        private static JObject PlayParticleSystem(JObject p)
        {
            int id = p["id"].Value<int>();
            bool withChildren = p["withChildren"]?.Value<bool>() ?? true;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                throw new System.Exception("GameObject does not have a ParticleSystem component");

            ps.Play(withChildren);

            return new JObject
            {
                ["ok"] = true,
                ["isPlaying"] = ps.isPlaying
            };
        }

        /// <summary>
        /// Stops a particle system.
        /// </summary>
        private static JObject StopParticleSystem(JObject p)
        {
            int id = p["id"].Value<int>();
            bool withChildren = p["withChildren"]?.Value<bool>() ?? true;
            bool clear = p["clear"]?.Value<bool>() ?? false;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                throw new System.Exception("GameObject does not have a ParticleSystem component");

            ps.Stop(withChildren, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Lists all particle systems in the scene.
        /// </summary>
        private static JObject ListParticleSystems(JObject p)
        {
            var systems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var result = new JArray();

            foreach (var ps in systems)
            {
                result.Add(new JObject
                {
                    ["id"] = ps.gameObject.GetInstanceID(),
                    ["name"] = ps.gameObject.name,
                    ["isPlaying"] = ps.isPlaying,
                    ["particleCount"] = ps.particleCount,
                    ["position"] = new JObject
                    {
                        ["x"] = ps.transform.position.x,
                        ["y"] = ps.transform.position.y,
                        ["z"] = ps.transform.position.z
                    }
                });
            }

            return new JObject
            {
                ["particleSystems"] = result,
                ["count"] = result.Count
            };
        }
    }
}

