using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Editor.Demo
{
    public static class DemoSceneGenerator
    {
        [MenuItem("Tools/MCP/Create Demo Scene", priority = 20)]
        public static void CreateDemoScene()
        {
            string scenePath = "Assets/DemoScene.unity";

            // Check if scene already exists
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                if (!EditorUtility.DisplayDialog("Demo Scene Exists", 
                    $"Scene at {scenePath} already exists. Overwrite?", "Yes", "No"))
                {
                    return;
                }
            }

            // Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // 1. Create Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.transform.position = new Vector3(0, 5, -10);
            camObj.transform.LookAt(Vector3.zero);
            camObj.AddComponent<AudioListener>();

            // 2. Create Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // 3. Create Ground Plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5, 1, 5); // 50x50 units
            
            // Create a simple material for the ground if possible, or just leave default
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                groundRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                groundRenderer.sharedMaterial.color = new Color(0.3f, 0.3f, 0.3f); // Dark gray
            }

            // 4. Create a "Welcome" Cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Welcome Cube";
            cube.transform.position = new Vector3(0, 0.5f, 0);
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                cubeRenderer.sharedMaterial.color = new Color(0.2f, 0.6f, 1.0f); // Blue
            }

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[MCP] Demo Scene created at {scenePath}");
            
            // Refresh Asset Database
            AssetDatabase.Refresh();
        }
    }
}
