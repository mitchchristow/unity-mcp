using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for inspecting and managing components on GameObjects.
    /// Provides detailed read access to component properties.
    /// </summary>
    public static class ComponentInspectorController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.get_components", GetComponents);
            JsonRpcDispatcher.RegisterMethod("unity.get_component_properties", GetComponentProperties);
            JsonRpcDispatcher.RegisterMethod("unity.remove_component", RemoveComponent);
            JsonRpcDispatcher.RegisterMethod("unity.get_object_details", GetObjectDetails);
        }

        /// <summary>
        /// Gets all components on a GameObject.
        /// </summary>
        private static JObject GetComponents(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var components = new JArray();
            
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue; // Skip missing/broken components
                
                components.Add(new JObject
                {
                    ["type"] = comp.GetType().Name,
                    ["fullType"] = comp.GetType().FullName,
                    ["id"] = comp.GetInstanceID(),
                    ["enabled"] = IsComponentEnabled(comp)
                });
            }

            return new JObject
            {
                ["gameObject"] = go.name,
                ["components"] = components,
                ["count"] = components.Count
            };
        }

        /// <summary>
        /// Gets all public properties and fields of a specific component.
        /// </summary>
        private static JObject GetComponentProperties(JObject p)
        {
            int objectId = p["id"].Value<int>();
            string componentName = p["component"]?.ToString();
            
            if (string.IsNullOrEmpty(componentName))
                throw new System.Exception("Component name is required");

            var go = EditorUtility.InstanceIDToObject(objectId) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var component = go.GetComponent(componentName);
            if (component == null)
                throw new System.Exception($"Component '{componentName}' not found on object");

            var properties = new JArray();
            var type = component.GetType();

            // Get serialized properties (what shows in Inspector)
            var serializedObject = new SerializedObject(component);
            var prop = serializedObject.GetIterator();
            
            if (prop.NextVisible(true))
            {
                do
                {
                    var propInfo = new JObject
                    {
                        ["name"] = prop.name,
                        ["displayName"] = prop.displayName,
                        ["type"] = prop.propertyType.ToString(),
                        ["value"] = JToken.FromObject(GetSerializedPropertyValue(prop) ?? "null"),
                        ["editable"] = prop.editable
                    };
                    properties.Add(propInfo);
                }
                while (prop.NextVisible(false));
            }

            return new JObject
            {
                ["gameObject"] = go.name,
                ["component"] = componentName,
                ["properties"] = properties
            };
        }

        /// <summary>
        /// Removes a component from a GameObject.
        /// </summary>
        private static JObject RemoveComponent(JObject p)
        {
            int objectId = p["id"].Value<int>();
            string componentName = p["component"]?.ToString();
            
            if (string.IsNullOrEmpty(componentName))
                throw new System.Exception("Component name is required");

            var go = EditorUtility.InstanceIDToObject(objectId) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var component = go.GetComponent(componentName);
            if (component == null)
                throw new System.Exception($"Component '{componentName}' not found on object");

            // Don't allow removing Transform
            if (component is Transform)
                throw new System.Exception("Cannot remove Transform component");

            Undo.DestroyObjectImmediate(component);

            return new JObject
            {
                ["ok"] = true,
                ["removed"] = componentName
            };
        }

        /// <summary>
        /// Gets comprehensive details about a GameObject including all components and their properties.
        /// </summary>
        private static JObject GetObjectDetails(JObject p)
        {
            int id = p["id"].Value<int>();
            bool includeChildren = p["includeChildren"]?.Value<bool>() ?? false;
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var result = SerializeGameObjectDetails(go);
            
            if (includeChildren)
            {
                var children = new JArray();
                foreach (Transform child in go.transform)
                {
                    children.Add(SerializeGameObjectDetails(child.gameObject));
                }
                result["children"] = children;
            }

            return result;
        }

        private static JObject SerializeGameObjectDetails(GameObject go)
        {
            var components = new JArray();
            
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                
                var compInfo = new JObject
                {
                    ["type"] = comp.GetType().Name,
                    ["id"] = comp.GetInstanceID(),
                    ["enabled"] = IsComponentEnabled(comp)
                };

                // Add key properties for common components
                if (comp is Transform t)
                {
                    compInfo["position"] = new JObject { ["x"] = t.position.x, ["y"] = t.position.y, ["z"] = t.position.z };
                    compInfo["rotation"] = new JObject { ["x"] = t.eulerAngles.x, ["y"] = t.eulerAngles.y, ["z"] = t.eulerAngles.z };
                    compInfo["scale"] = new JObject { ["x"] = t.localScale.x, ["y"] = t.localScale.y, ["z"] = t.localScale.z };
                }
                else if (comp is Renderer r)
                {
                    compInfo["material"] = r.sharedMaterial?.name;
                    compInfo["enabled"] = r.enabled;
                }
                else if (comp is Collider c)
                {
                    compInfo["isTrigger"] = c.isTrigger;
                    compInfo["enabled"] = c.enabled;
                }
                else if (comp is Rigidbody rb)
                {
                    compInfo["mass"] = rb.mass;
                    compInfo["useGravity"] = rb.useGravity;
                    compInfo["isKinematic"] = rb.isKinematic;
                }
                else if (comp is Light l)
                {
                    compInfo["lightType"] = l.type.ToString();
                    compInfo["intensity"] = l.intensity;
                    compInfo["color"] = new JObject { ["r"] = l.color.r, ["g"] = l.color.g, ["b"] = l.color.b };
                }
                else if (comp is Camera cam)
                {
                    compInfo["fieldOfView"] = cam.fieldOfView;
                    compInfo["orthographic"] = cam.orthographic;
                    compInfo["nearClip"] = cam.nearClipPlane;
                    compInfo["farClip"] = cam.farClipPlane;
                }
                else if (comp is AudioSource audio)
                {
                    compInfo["clip"] = audio.clip?.name;
                    compInfo["volume"] = audio.volume;
                    compInfo["loop"] = audio.loop;
                }

                components.Add(compInfo);
            }

            return new JObject
            {
                ["id"] = go.GetInstanceID(),
                ["name"] = go.name,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["layerName"] = LayerMask.LayerToName(go.layer),
                ["active"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["isStatic"] = go.isStatic,
                ["components"] = components,
                ["childCount"] = go.transform.childCount
            };
        }

        private static bool IsComponentEnabled(Component comp)
        {
            if (comp is Behaviour b) return b.enabled;
            if (comp is Renderer r) return r.enabled;
            if (comp is Collider c) return c.enabled;
            return true; // Components without enabled property are always "enabled"
        }

        private static object GetSerializedPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    return new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
                case SerializedPropertyType.Vector2:
                    var v2 = prop.vector2Value;
                    return new JObject { ["x"] = v2.x, ["y"] = v2.y };
                case SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    return new JObject { ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z };
                case SerializedPropertyType.Vector4:
                    var v4 = prop.vector4Value;
                    return new JObject { ["x"] = v4.x, ["y"] = v4.y, ["z"] = v4.z, ["w"] = v4.w };
                case SerializedPropertyType.Quaternion:
                    var q = prop.quaternionValue.eulerAngles;
                    return new JObject { ["x"] = q.x, ["y"] = q.y, ["z"] = q.z };
                case SerializedPropertyType.Enum:
                    return prop.enumDisplayNames.Length > prop.enumValueIndex && prop.enumValueIndex >= 0
                        ? prop.enumDisplayNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue?.name;
                case SerializedPropertyType.LayerMask:
                    return prop.intValue;
                case SerializedPropertyType.Rect:
                    var r = prop.rectValue;
                    return new JObject { ["x"] = r.x, ["y"] = r.y, ["width"] = r.width, ["height"] = r.height };
                case SerializedPropertyType.Bounds:
                    var b = prop.boundsValue;
                    return new JObject 
                    { 
                        ["center"] = new JObject { ["x"] = b.center.x, ["y"] = b.center.y, ["z"] = b.center.z },
                        ["size"] = new JObject { ["x"] = b.size.x, ["y"] = b.size.y, ["z"] = b.size.z }
                    };
                default:
                    return $"[{prop.propertyType}]";
            }
        }
    }
}

