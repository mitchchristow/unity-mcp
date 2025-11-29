#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for scripting assistance - script creation, compilation status, and API info.
    /// </summary>
    public static class ScriptingController
    {
        public static void Register()
        {
            // Script creation
            JsonRpcDispatcher.RegisterMethod("unity.create_script", CreateScript);
            JsonRpcDispatcher.RegisterMethod("unity.get_script_templates", GetScriptTemplates);
            
            // Compilation status
            JsonRpcDispatcher.RegisterMethod("unity.get_compilation_errors", GetCompilationErrors);
            JsonRpcDispatcher.RegisterMethod("unity.get_compilation_warnings", GetCompilationWarnings);
            JsonRpcDispatcher.RegisterMethod("unity.is_compiling", IsCompiling);
            
            // Component API info
            JsonRpcDispatcher.RegisterMethod("unity.get_component_api", GetComponentApi);
            JsonRpcDispatcher.RegisterMethod("unity.list_component_types", ListComponentTypes);
        }

        /// <summary>
        /// Creates a new C# script from a template.
        /// </summary>
        private static JObject CreateScript(JObject p)
        {
            string path = p["path"]?.Value<string>() ?? throw new ArgumentException("path is required");
            string className = p["className"]?.Value<string>() ?? Path.GetFileNameWithoutExtension(path);
            string template = p["template"]?.Value<string>() ?? "monobehaviour";
            string namespaceName = p["namespace"]?.Value<string>();
            string customContent = p["content"]?.Value<string>();

            // Ensure path starts with Assets/
            if (!path.StartsWith("Assets/"))
            {
                path = "Assets/" + path;
            }

            // Ensure .cs extension
            if (!path.EndsWith(".cs"))
            {
                path += ".cs";
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Generate content based on template
            string content;
            if (!string.IsNullOrEmpty(customContent))
            {
                content = customContent;
            }
            else
            {
                content = GenerateScriptContent(template, className, namespaceName);
            }

            // Write the file
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();

            return new JObject
            {
                ["ok"] = true,
                ["path"] = path,
                ["className"] = className,
                ["template"] = template
            };
        }

        /// <summary>
        /// Gets available script templates.
        /// </summary>
        private static JObject GetScriptTemplates(JObject p)
        {
            var templates = new JArray
            {
                new JObject
                {
                    ["name"] = "monobehaviour",
                    ["description"] = "Standard MonoBehaviour with Start and Update methods",
                    ["baseClass"] = "MonoBehaviour"
                },
                new JObject
                {
                    ["name"] = "monobehaviour_empty",
                    ["description"] = "Empty MonoBehaviour class",
                    ["baseClass"] = "MonoBehaviour"
                },
                new JObject
                {
                    ["name"] = "scriptableobject",
                    ["description"] = "ScriptableObject for data assets",
                    ["baseClass"] = "ScriptableObject"
                },
                new JObject
                {
                    ["name"] = "editor",
                    ["description"] = "Custom Editor for Inspector customization",
                    ["baseClass"] = "Editor"
                },
                new JObject
                {
                    ["name"] = "editorwindow",
                    ["description"] = "Custom Editor Window",
                    ["baseClass"] = "EditorWindow"
                },
                new JObject
                {
                    ["name"] = "interface",
                    ["description"] = "C# Interface",
                    ["baseClass"] = null
                },
                new JObject
                {
                    ["name"] = "enum",
                    ["description"] = "C# Enum",
                    ["baseClass"] = null
                },
                new JObject
                {
                    ["name"] = "struct",
                    ["description"] = "C# Struct (Serializable)",
                    ["baseClass"] = null
                },
                new JObject
                {
                    ["name"] = "class",
                    ["description"] = "Plain C# class",
                    ["baseClass"] = null
                },
                new JObject
                {
                    ["name"] = "singleton",
                    ["description"] = "MonoBehaviour Singleton pattern",
                    ["baseClass"] = "MonoBehaviour"
                },
                new JObject
                {
                    ["name"] = "statemachine",
                    ["description"] = "Basic state machine pattern",
                    ["baseClass"] = "MonoBehaviour"
                }
            };

            return new JObject
            {
                ["templates"] = templates,
                ["count"] = templates.Count
            };
        }

        /// <summary>
        /// Gets current compilation errors from console logs.
        /// </summary>
        private static JObject GetCompilationErrors(JObject p)
        {
            var errors = new JArray();

            // Use reflection to access LogEntries (internal Unity API)
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntriesType != null)
            {
                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                var startMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                var endMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                if (getCountMethod != null && startMethod != null && endMethod != null)
                {
                    startMethod.Invoke(null, null);
                    try
                    {
                        int count = (int)getCountMethod.Invoke(null, null);
                        
                        // Get LogEntry type for reading entries
                        var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                        
                        for (int i = 0; i < count && i < 500; i++) // Limit to 500 entries
                        {
                            var entry = Activator.CreateInstance(logEntryType);
                            if (getEntryMethod != null)
                            {
                                getEntryMethod.Invoke(null, new object[] { i, entry });
                                
                                var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
                                var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                                var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
                                var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
                                
                                int mode = (int)modeField.GetValue(entry);
                                string message = (string)messageField.GetValue(entry);
                                
                                // Mode flags: Error = 1, Warning = 2, Log = 4
                                // ScriptCompileError = 256, ScriptCompileWarning = 512
                                bool isError = (mode & 1) != 0 || (mode & 256) != 0;
                                
                                if (isError && message != null)
                                {
                                    string file = fileField?.GetValue(entry) as string ?? "";
                                    int line = lineField != null ? (int)lineField.GetValue(entry) : 0;
                                    
                                    // Parse file:line from message if not provided
                                    if (string.IsNullOrEmpty(file) && message.Contains(": error"))
                                    {
                                        var match = Regex.Match(message, @"(.+\.cs)\((\d+),(\d+)\):");
                                        if (match.Success)
                                        {
                                            file = match.Groups[1].Value;
                                            line = int.Parse(match.Groups[2].Value);
                                        }
                                    }
                                    
                                    errors.Add(new JObject
                                    {
                                        ["message"] = message,
                                        ["file"] = file,
                                        ["line"] = line
                                    });
                                }
                            }
                        }
                    }
                    finally
                    {
                        endMethod.Invoke(null, null);
                    }
                }
            }

            return new JObject
            {
                ["errors"] = errors,
                ["count"] = errors.Count,
                ["isCompiling"] = EditorApplication.isCompiling
            };
        }

        /// <summary>
        /// Gets current compilation warnings from console logs.
        /// </summary>
        private static JObject GetCompilationWarnings(JObject p)
        {
            var warnings = new JArray();

            // Use reflection to access LogEntries (internal Unity API)
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntriesType != null)
            {
                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                var startMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                var endMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                if (getCountMethod != null && startMethod != null && endMethod != null)
                {
                    startMethod.Invoke(null, null);
                    try
                    {
                        int count = (int)getCountMethod.Invoke(null, null);
                        
                        var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                        
                        for (int i = 0; i < count && i < 500; i++)
                        {
                            var entry = Activator.CreateInstance(logEntryType);
                            if (getEntryMethod != null)
                            {
                                getEntryMethod.Invoke(null, new object[] { i, entry });
                                
                                var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
                                var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                                var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
                                var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
                                
                                int mode = (int)modeField.GetValue(entry);
                                string message = (string)messageField.GetValue(entry);
                                
                                // Mode flags: Warning = 2, ScriptCompileWarning = 512
                                bool isWarning = (mode & 2) != 0 || (mode & 512) != 0;
                                bool isError = (mode & 1) != 0 || (mode & 256) != 0;
                                
                                if (isWarning && !isError && message != null)
                                {
                                    string file = fileField?.GetValue(entry) as string ?? "";
                                    int line = lineField != null ? (int)lineField.GetValue(entry) : 0;
                                    
                                    // Parse file:line from message if not provided
                                    if (string.IsNullOrEmpty(file) && message.Contains(": warning"))
                                    {
                                        var match = Regex.Match(message, @"(.+\.cs)\((\d+),(\d+)\):");
                                        if (match.Success)
                                        {
                                            file = match.Groups[1].Value;
                                            line = int.Parse(match.Groups[2].Value);
                                        }
                                    }
                                    
                                    warnings.Add(new JObject
                                    {
                                        ["message"] = message,
                                        ["file"] = file,
                                        ["line"] = line
                                    });
                                }
                            }
                        }
                    }
                    finally
                    {
                        endMethod.Invoke(null, null);
                    }
                }
            }

            return new JObject
            {
                ["warnings"] = warnings,
                ["count"] = warnings.Count,
                ["isCompiling"] = EditorApplication.isCompiling
            };
        }

        /// <summary>
        /// Checks if Unity is currently compiling.
        /// </summary>
        private static JObject IsCompiling(JObject p)
        {
            return new JObject
            {
                ["isCompiling"] = EditorApplication.isCompiling,
                ["isUpdating"] = EditorApplication.isUpdating
            };
        }

        /// <summary>
        /// Gets API information for a component type.
        /// </summary>
        private static JObject GetComponentApi(JObject p)
        {
            string typeName = p["type"]?.Value<string>() ?? throw new ArgumentException("type is required");

            // Try to find the type
            Type componentType = null;
            
            // Search in common assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                componentType = assembly.GetType(typeName) ?? 
                               assembly.GetType("UnityEngine." + typeName) ??
                               assembly.GetType("UnityEngine.UI." + typeName);
                if (componentType != null) break;
            }

            if (componentType == null)
            {
                throw new ArgumentException($"Type not found: {typeName}");
            }

            // Get public properties
            var properties = new JArray();
            foreach (var prop in componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead)
                {
                    properties.Add(new JObject
                    {
                        ["name"] = prop.Name,
                        ["type"] = GetFriendlyTypeName(prop.PropertyType),
                        ["canWrite"] = prop.CanWrite,
                        ["canRead"] = prop.CanRead
                    });
                }
            }

            // Get public fields
            var fields = new JArray();
            foreach (var field in componentType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                fields.Add(new JObject
                {
                    ["name"] = field.Name,
                    ["type"] = GetFriendlyTypeName(field.FieldType),
                    ["isReadOnly"] = field.IsInitOnly
                });
            }

            // Get public methods (excluding property accessors and inherited Object methods)
            var methods = new JArray();
            var excludeMethods = new HashSet<string> { "Equals", "GetHashCode", "GetType", "ToString", "GetInstanceID" };
            
            foreach (var method in componentType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!method.IsSpecialName && !excludeMethods.Contains(method.Name))
                {
                    var parameters = new JArray();
                    foreach (var param in method.GetParameters())
                    {
                        parameters.Add(new JObject
                        {
                            ["name"] = param.Name,
                            ["type"] = GetFriendlyTypeName(param.ParameterType),
                            ["isOptional"] = param.IsOptional,
                            ["defaultValue"] = param.HasDefaultValue ? param.DefaultValue?.ToString() : null
                        });
                    }

                    methods.Add(new JObject
                    {
                        ["name"] = method.Name,
                        ["returnType"] = GetFriendlyTypeName(method.ReturnType),
                        ["parameters"] = parameters
                    });
                }
            }

            // Get events
            var events = new JArray();
            foreach (var evt in componentType.GetEvents(BindingFlags.Public | BindingFlags.Instance))
            {
                events.Add(new JObject
                {
                    ["name"] = evt.Name,
                    ["handlerType"] = GetFriendlyTypeName(evt.EventHandlerType)
                });
            }

            return new JObject
            {
                ["type"] = componentType.FullName,
                ["baseType"] = componentType.BaseType?.Name,
                ["isComponent"] = typeof(Component).IsAssignableFrom(componentType),
                ["isMonoBehaviour"] = typeof(MonoBehaviour).IsAssignableFrom(componentType),
                ["properties"] = properties,
                ["fields"] = fields,
                ["methods"] = methods,
                ["events"] = events
            };
        }

        /// <summary>
        /// Lists common component types.
        /// </summary>
        private static JObject ListComponentTypes(JObject p)
        {
            string category = p["category"]?.Value<string>() ?? "all";

            var types = new JArray();

            // Common Unity components organized by category
            var componentCategories = new Dictionary<string, string[]>
            {
                ["transform"] = new[] { "Transform", "RectTransform" },
                ["rendering"] = new[] { "MeshRenderer", "SpriteRenderer", "LineRenderer", "TrailRenderer", "ParticleSystemRenderer", "SkinnedMeshRenderer", "Camera", "Light" },
                ["physics"] = new[] { "Rigidbody", "BoxCollider", "SphereCollider", "CapsuleCollider", "MeshCollider", "CharacterController" },
                ["physics2d"] = new[] { "Rigidbody2D", "BoxCollider2D", "CircleCollider2D", "CapsuleCollider2D", "PolygonCollider2D", "EdgeCollider2D" },
                ["audio"] = new[] { "AudioSource", "AudioListener", "AudioReverbZone" },
                ["ui"] = new[] { "Canvas", "CanvasScaler", "GraphicRaycaster", "Image", "RawImage", "Text", "Button", "Toggle", "Slider", "Scrollbar", "Dropdown", "InputField", "ScrollRect" },
                ["animation"] = new[] { "Animator", "Animation" },
                ["navigation"] = new[] { "NavMeshAgent", "NavMeshObstacle", "OffMeshLink" },
                ["effects"] = new[] { "ParticleSystem", "Projector", "LensFlare" },
                ["layout"] = new[] { "LayoutElement", "ContentSizeFitter", "AspectRatioFitter", "HorizontalLayoutGroup", "VerticalLayoutGroup", "GridLayoutGroup" },
                ["tilemap"] = new[] { "Tilemap", "TilemapRenderer", "TilemapCollider2D" }
            };

            if (category == "all" || category == "categories")
            {
                // Return all categories
                foreach (var cat in componentCategories)
                {
                    types.Add(new JObject
                    {
                        ["category"] = cat.Key,
                        ["types"] = new JArray(cat.Value)
                    });
                }
            }
            else if (componentCategories.ContainsKey(category))
            {
                foreach (var typeName in componentCategories[category])
                {
                    types.Add(typeName);
                }
            }

            return new JObject
            {
                ["category"] = category,
                ["types"] = types
            };
        }

        // Helper methods

        private static string GenerateScriptContent(string template, string className, string namespaceName)
        {
            var sb = new StringBuilder();
            bool hasNamespace = !string.IsNullOrEmpty(namespaceName);
            string indent = hasNamespace ? "    " : "";

            // Add using statements based on template
            switch (template.ToLower())
            {
                case "monobehaviour":
                case "monobehaviour_empty":
                case "singleton":
                case "statemachine":
                    sb.AppendLine("using UnityEngine;");
                    break;
                case "scriptableobject":
                    sb.AppendLine("using UnityEngine;");
                    break;
                case "editor":
                    sb.AppendLine("using UnityEngine;");
                    sb.AppendLine("using UnityEditor;");
                    break;
                case "editorwindow":
                    sb.AppendLine("using UnityEngine;");
                    sb.AppendLine("using UnityEditor;");
                    break;
                default:
                    sb.AppendLine("using System;");
                    break;
            }

            sb.AppendLine();

            // Namespace start
            if (hasNamespace)
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            // Generate class content based on template
            switch (template.ToLower())
            {
                case "monobehaviour":
                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    void Start()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        ");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void Update()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        ");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "monobehaviour_empty":
                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    ");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "scriptableobject":
                    sb.AppendLine($"{indent}[CreateAssetMenu(fileName = \"{className}\", menuName = \"ScriptableObjects/{className}\")]");
                    sb.AppendLine($"{indent}public class {className} : ScriptableObject");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    ");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "editor":
                    sb.AppendLine($"{indent}[CustomEditor(typeof(MonoBehaviour))] // TODO: Change MonoBehaviour to your target type");
                    sb.AppendLine($"{indent}public class {className} : Editor");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    public override void OnInspectorGUI()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        base.OnInspectorGUI();");
                    sb.AppendLine($"{indent}        ");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "editorwindow":
                    sb.AppendLine($"{indent}public class {className} : EditorWindow");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    [MenuItem(\"Window/{className}\")]");
                    sb.AppendLine($"{indent}    public static void ShowWindow()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        GetWindow<{className}>(\"{className}\");");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void OnGUI()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        ");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "interface":
                    sb.AppendLine($"{indent}public interface {className}");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    ");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "enum":
                    sb.AppendLine($"{indent}public enum {className}");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    None,");
                    sb.AppendLine($"{indent}    // Add more values here");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "struct":
                    sb.AppendLine($"{indent}[System.Serializable]");
                    sb.AppendLine($"{indent}public struct {className}");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    ");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "class":
                    sb.AppendLine($"{indent}[System.Serializable]");
                    sb.AppendLine($"{indent}public class {className}");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    ");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "singleton":
                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    public static {className} Instance {{ get; private set; }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void Awake()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        if (Instance != null && Instance != this)");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            Destroy(gameObject);");
                    sb.AppendLine($"{indent}            return;");
                    sb.AppendLine($"{indent}        }}");
                    sb.AppendLine($"{indent}        Instance = this;");
                    sb.AppendLine($"{indent}        DontDestroyOnLoad(gameObject);");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "statemachine":
                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    public enum State");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        Idle,");
                    sb.AppendLine($"{indent}        // Add more states here");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    public State CurrentState {{ get; private set; }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    public void ChangeState(State newState)");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        if (CurrentState == newState) return;");
                    sb.AppendLine($"{indent}        ");
                    sb.AppendLine($"{indent}        OnExitState(CurrentState);");
                    sb.AppendLine($"{indent}        CurrentState = newState;");
                    sb.AppendLine($"{indent}        OnEnterState(newState);");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void OnEnterState(State state)");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        switch (state)");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            case State.Idle:");
                    sb.AppendLine($"{indent}                break;");
                    sb.AppendLine($"{indent}        }}");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void OnExitState(State state)");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        switch (state)");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            case State.Idle:");
                    sb.AppendLine($"{indent}                break;");
                    sb.AppendLine($"{indent}        }}");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void Update()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        UpdateState(CurrentState);");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void UpdateState(State state)");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        switch (state)");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            case State.Idle:");
                    sb.AppendLine($"{indent}                break;");
                    sb.AppendLine($"{indent}        }}");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine($"{indent}}}");
                    break;

                default:
                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    ");
                    sb.AppendLine($"{indent}}}");
                    break;
            }

            // Namespace end
            if (hasNamespace)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type == null) return "void";
            if (type == typeof(void)) return "void";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(Vector4)) return "Vector4";
            if (type == typeof(Quaternion)) return "Quaternion";
            if (type == typeof(Color)) return "Color";
            if (type == typeof(Color32)) return "Color32";
            if (type == typeof(GameObject)) return "GameObject";
            if (type == typeof(Transform)) return "Transform";

            if (type.IsGenericType)
            {
                var genericName = type.Name.Split('`')[0];
                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericName}<{genericArgs}>";
            }

            if (type.IsArray)
            {
                return $"{GetFriendlyTypeName(type.GetElementType())}[]";
            }

            return type.Name;
        }
    }
}
#endif

