#pragma warning disable CS0618 // NavMeshBuilder obsolete warning - Unity 6 still requires this for editor baking
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor.AI;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing navigation meshes in Unity.
    /// </summary>
    public static class NavMeshController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.bake_navmesh", BakeNavMesh);
            JsonRpcDispatcher.RegisterMethod("unity.clear_navmesh", ClearNavMesh);
            JsonRpcDispatcher.RegisterMethod("unity.get_navmesh_settings", GetNavMeshSettings);
            JsonRpcDispatcher.RegisterMethod("unity.add_navmesh_agent", AddNavMeshAgent);
            JsonRpcDispatcher.RegisterMethod("unity.set_navmesh_agent", SetNavMeshAgent);
            JsonRpcDispatcher.RegisterMethod("unity.get_navmesh_agent_info", GetNavMeshAgentInfo);
            JsonRpcDispatcher.RegisterMethod("unity.add_navmesh_obstacle", AddNavMeshObstacle);
            JsonRpcDispatcher.RegisterMethod("unity.set_navmesh_destination", SetNavMeshDestination);
            JsonRpcDispatcher.RegisterMethod("unity.list_navmesh_agents", ListNavMeshAgents);
            JsonRpcDispatcher.RegisterMethod("unity.calculate_path", CalculatePath);
        }

        /// <summary>
        /// Bakes the navigation mesh.
        /// </summary>
        private static JObject BakeNavMesh(JObject p)
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            return new JObject
            {
                ["ok"] = true,
                ["message"] = "NavMesh baked successfully"
            };
        }

        /// <summary>
        /// Clears the navigation mesh.
        /// </summary>
        private static JObject ClearNavMesh(JObject p)
        {
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();

            return new JObject
            {
                ["ok"] = true,
                ["message"] = "NavMesh cleared"
            };
        }

        /// <summary>
        /// Gets the current NavMesh settings.
        /// </summary>
        private static JObject GetNavMeshSettings(JObject p)
        {
            var settings = NavMesh.GetSettingsByIndex(0);

            return new JObject
            {
                ["agentRadius"] = settings.agentRadius,
                ["agentHeight"] = settings.agentHeight,
                ["agentSlope"] = settings.agentSlope,
                ["agentClimb"] = settings.agentClimb,
                ["agentTypeID"] = settings.agentTypeID
            };
        }

        /// <summary>
        /// Adds a NavMeshAgent component to a GameObject.
        /// </summary>
        private static JObject AddNavMeshAgent(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var existingAgent = go.GetComponent<NavMeshAgent>();
            if (existingAgent != null)
            {
                return new JObject
                {
                    ["ok"] = true,
                    ["alreadyExists"] = true,
                    ["agentId"] = existingAgent.GetInstanceID()
                };
            }

            var agent = Undo.AddComponent<NavMeshAgent>(go);

            // Set initial properties if provided
            if (p["speed"] != null)
                agent.speed = p["speed"].Value<float>();
            if (p["angularSpeed"] != null)
                agent.angularSpeed = p["angularSpeed"].Value<float>();
            if (p["acceleration"] != null)
                agent.acceleration = p["acceleration"].Value<float>();
            if (p["stoppingDistance"] != null)
                agent.stoppingDistance = p["stoppingDistance"].Value<float>();
            if (p["radius"] != null)
                agent.radius = p["radius"].Value<float>();
            if (p["height"] != null)
                agent.height = p["height"].Value<float>();

            return new JObject
            {
                ["ok"] = true,
                ["agentId"] = agent.GetInstanceID()
            };
        }

        /// <summary>
        /// Sets properties on a NavMeshAgent.
        /// </summary>
        private static JObject SetNavMeshAgent(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null)
                throw new System.Exception("GameObject does not have a NavMeshAgent component");

            Undo.RecordObject(agent, "Set NavMeshAgent via MCP");

            if (p["speed"] != null)
                agent.speed = p["speed"].Value<float>();
            if (p["angularSpeed"] != null)
                agent.angularSpeed = p["angularSpeed"].Value<float>();
            if (p["acceleration"] != null)
                agent.acceleration = p["acceleration"].Value<float>();
            if (p["stoppingDistance"] != null)
                agent.stoppingDistance = p["stoppingDistance"].Value<float>();
            if (p["radius"] != null)
                agent.radius = p["radius"].Value<float>();
            if (p["height"] != null)
                agent.height = p["height"].Value<float>();
            if (p["baseOffset"] != null)
                agent.baseOffset = p["baseOffset"].Value<float>();
            if (p["autoTraverseOffMeshLink"] != null)
                agent.autoTraverseOffMeshLink = p["autoTraverseOffMeshLink"].Value<bool>();
            if (p["autoBraking"] != null)
                agent.autoBraking = p["autoBraking"].Value<bool>();
            if (p["autoRepath"] != null)
                agent.autoRepath = p["autoRepath"].Value<bool>();

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Gets information about a NavMeshAgent.
        /// </summary>
        private static JObject GetNavMeshAgentInfo(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null)
                throw new System.Exception("GameObject does not have a NavMeshAgent component");

            return new JObject
            {
                ["name"] = go.name,
                ["id"] = go.GetInstanceID(),
                ["speed"] = agent.speed,
                ["angularSpeed"] = agent.angularSpeed,
                ["acceleration"] = agent.acceleration,
                ["stoppingDistance"] = agent.stoppingDistance,
                ["radius"] = agent.radius,
                ["height"] = agent.height,
                ["baseOffset"] = agent.baseOffset,
                ["isOnNavMesh"] = agent.isOnNavMesh,
                ["hasPath"] = agent.hasPath,
                ["pathPending"] = agent.pathPending,
                ["pathStatus"] = agent.pathStatus.ToString(),
                ["velocity"] = new JObject
                {
                    ["x"] = agent.velocity.x,
                    ["y"] = agent.velocity.y,
                    ["z"] = agent.velocity.z
                },
                ["destination"] = new JObject
                {
                    ["x"] = agent.destination.x,
                    ["y"] = agent.destination.y,
                    ["z"] = agent.destination.z
                },
                ["remainingDistance"] = agent.remainingDistance
            };
        }

        /// <summary>
        /// Adds a NavMeshObstacle component to a GameObject.
        /// </summary>
        private static JObject AddNavMeshObstacle(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var existingObstacle = go.GetComponent<NavMeshObstacle>();
            if (existingObstacle != null)
            {
                return new JObject
                {
                    ["ok"] = true,
                    ["alreadyExists"] = true
                };
            }

            var obstacle = Undo.AddComponent<NavMeshObstacle>(go);

            if (p["carve"] != null)
                obstacle.carving = p["carve"].Value<bool>();
            if (p["shape"] != null)
            {
                if (System.Enum.TryParse(p["shape"].ToString(), true, out NavMeshObstacleShape shape))
                {
                    obstacle.shape = shape;
                }
            }
            if (p["size"] != null)
            {
                obstacle.size = new Vector3(
                    p["size"]["x"]?.Value<float>() ?? 1,
                    p["size"]["y"]?.Value<float>() ?? 1,
                    p["size"]["z"]?.Value<float>() ?? 1
                );
            }

            return new JObject
            {
                ["ok"] = true,
                ["obstacleId"] = obstacle.GetInstanceID()
            };
        }

        /// <summary>
        /// Sets the destination for a NavMeshAgent (only works in Play mode).
        /// </summary>
        private static JObject SetNavMeshDestination(JObject p)
        {
            int id = p["id"].Value<int>();

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null)
                throw new System.Exception("GameObject does not have a NavMeshAgent component");

            if (!Application.isPlaying)
            {
                return new JObject
                {
                    ["ok"] = false,
                    ["error"] = "NavMesh navigation only works in Play mode"
                };
            }

            Vector3 destination = new Vector3(
                p["destination"]["x"]?.Value<float>() ?? 0,
                p["destination"]["y"]?.Value<float>() ?? 0,
                p["destination"]["z"]?.Value<float>() ?? 0
            );

            bool success = agent.SetDestination(destination);

            return new JObject
            {
                ["ok"] = success,
                ["destination"] = new JObject
                {
                    ["x"] = destination.x,
                    ["y"] = destination.y,
                    ["z"] = destination.z
                }
            };
        }

        /// <summary>
        /// Lists all NavMeshAgents in the scene.
        /// </summary>
        private static JObject ListNavMeshAgents(JObject p)
        {
            var agents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var result = new JArray();

            foreach (var agent in agents)
            {
                result.Add(new JObject
                {
                    ["id"] = agent.gameObject.GetInstanceID(),
                    ["name"] = agent.gameObject.name,
                    ["speed"] = agent.speed,
                    ["isOnNavMesh"] = agent.isOnNavMesh,
                    ["position"] = new JObject
                    {
                        ["x"] = agent.transform.position.x,
                        ["y"] = agent.transform.position.y,
                        ["z"] = agent.transform.position.z
                    }
                });
            }

            return new JObject
            {
                ["agents"] = result,
                ["count"] = result.Count
            };
        }

        /// <summary>
        /// Calculates a path between two points.
        /// </summary>
        private static JObject CalculatePath(JObject p)
        {
            Vector3 start = new Vector3(
                p["start"]["x"]?.Value<float>() ?? 0,
                p["start"]["y"]?.Value<float>() ?? 0,
                p["start"]["z"]?.Value<float>() ?? 0
            );

            Vector3 end = new Vector3(
                p["end"]["x"]?.Value<float>() ?? 0,
                p["end"]["y"]?.Value<float>() ?? 0,
                p["end"]["z"]?.Value<float>() ?? 0
            );

            var path = new NavMeshPath();
            bool success = NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);

            var corners = new JArray();
            foreach (var corner in path.corners)
            {
                corners.Add(new JObject
                {
                    ["x"] = corner.x,
                    ["y"] = corner.y,
                    ["z"] = corner.z
                });
            }

            return new JObject
            {
                ["success"] = success,
                ["status"] = path.status.ToString(),
                ["corners"] = corners,
                ["cornerCount"] = path.corners.Length
            };
        }
    }
}
#pragma warning restore CS0618

