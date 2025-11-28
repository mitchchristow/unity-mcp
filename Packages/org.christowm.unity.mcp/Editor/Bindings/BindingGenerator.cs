using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Bindings
{
    public static class BindingGenerator
    {
        [MenuItem("Tools/MCP/Generate Type Bindings")]
        public static void GenerateBindings()
        {
            var bindings = new Dictionary<string, object>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("UnityEngine") || a.GetName().Name.StartsWith("Assembly-CSharp"));

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(Component).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        bindings[type.FullName] = GenerateTypeInfo(type);
                    }
                }
            }

            string path = Path.Combine(Directory.GetCurrentDirectory(), "unity-bindings.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(bindings, Formatting.Indented));
            Debug.Log($"[MCP] Generated bindings for {bindings.Count} types at {path}");
        }

        private static object GenerateTypeInfo(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => new { name = p.Name, type = p.PropertyType.Name });

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => new { name = f.Name, type = f.FieldType.Name });

            return new
            {
                assembly = type.Assembly.GetName().Name,
                properties = properties,
                fields = fields
            };
        }
    }
}
