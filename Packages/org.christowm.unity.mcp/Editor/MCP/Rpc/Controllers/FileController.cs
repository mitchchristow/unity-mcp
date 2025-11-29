using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for reading and writing files in the Unity project.
    /// </summary>
    public static class FileController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.read_file", ReadFile);
            JsonRpcDispatcher.RegisterMethod("unity.write_file", WriteFile);
            JsonRpcDispatcher.RegisterMethod("unity.file_exists", FileExists);
            JsonRpcDispatcher.RegisterMethod("unity.delete_file", DeleteFile);
            JsonRpcDispatcher.RegisterMethod("unity.list_directory", ListDirectory);
            JsonRpcDispatcher.RegisterMethod("unity.create_directory", CreateDirectory);
        }

        /// <summary>
        /// Reads the contents of a file in the project.
        /// </summary>
        private static JObject ReadFile(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("Path is required");

            // Ensure path is relative to project
            string fullPath = GetFullPath(path);
            
            if (!File.Exists(fullPath))
                throw new System.Exception($"File not found: {path}");

            // Check file size to prevent reading huge files
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > 1024 * 1024) // 1MB limit
            {
                return new JObject
                {
                    ["path"] = path,
                    ["error"] = "File too large (>1MB). Use a different method for large files.",
                    ["size"] = fileInfo.Length
                };
            }

            string content = File.ReadAllText(fullPath);
            
            return new JObject
            {
                ["path"] = path,
                ["content"] = content,
                ["size"] = fileInfo.Length,
                ["extension"] = fileInfo.Extension
            };
        }

        /// <summary>
        /// Writes content to a file in the project.
        /// </summary>
        private static JObject WriteFile(JObject p)
        {
            string path = p["path"]?.ToString();
            string content = p["content"]?.ToString();
            
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("Path is required");
            if (content == null)
                throw new System.Exception("Content is required");

            // Ensure path is relative to project and within Assets
            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
            {
                path = Path.Combine("Assets", path);
            }

            string fullPath = GetFullPath(path);
            
            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);
            
            // Refresh asset database if it's in Assets folder
            if (path.StartsWith("Assets"))
            {
                AssetDatabase.ImportAsset(path);
            }

            return new JObject
            {
                ["ok"] = true,
                ["path"] = path
            };
        }

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        private static JObject FileExists(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("Path is required");

            string fullPath = GetFullPath(path);
            bool exists = File.Exists(fullPath);
            bool isDirectory = Directory.Exists(fullPath);

            return new JObject
            {
                ["path"] = path,
                ["exists"] = exists || isDirectory,
                ["isFile"] = exists,
                ["isDirectory"] = isDirectory
            };
        }

        /// <summary>
        /// Deletes a file from the project.
        /// </summary>
        private static JObject DeleteFile(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("Path is required");

            // Safety check: only allow deleting from Assets folder
            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
                throw new System.Exception("Can only delete files within Assets folder");

            string fullPath = GetFullPath(path);
            
            if (!File.Exists(fullPath))
                throw new System.Exception($"File not found: {path}");

            // Use AssetDatabase for proper deletion
            bool success = AssetDatabase.DeleteAsset(path);
            
            if (!success)
            {
                // Fallback to direct deletion
                File.Delete(fullPath);
                AssetDatabase.Refresh();
            }

            return new JObject
            {
                ["ok"] = true,
                ["deleted"] = path
            };
        }

        /// <summary>
        /// Lists contents of a directory.
        /// </summary>
        private static JObject ListDirectory(JObject p)
        {
            string path = p["path"]?.ToString() ?? "Assets";
            bool recursive = p["recursive"]?.Value<bool>() ?? false;
            string filter = p["filter"]?.ToString(); // e.g., "*.cs", "*.prefab"

            string fullPath = GetFullPath(path);
            
            if (!Directory.Exists(fullPath))
                throw new System.Exception($"Directory not found: {path}");

            var files = new JArray();
            var directories = new JArray();

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string searchPattern = string.IsNullOrEmpty(filter) ? "*" : filter;

            // Get directories
            foreach (var dir in Directory.GetDirectories(fullPath, "*", SearchOption.TopDirectoryOnly))
            {
                var dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Name.StartsWith(".")) continue; // Skip hidden directories
                
                directories.Add(new JObject
                {
                    ["name"] = dirInfo.Name,
                    ["path"] = GetRelativePath(dir)
                });
            }

            // Get files
            foreach (var file in Directory.GetFiles(fullPath, searchPattern, searchOption))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Name.StartsWith(".")) continue; // Skip hidden files
                if (fileInfo.Extension == ".meta") continue; // Skip meta files
                
                files.Add(new JObject
                {
                    ["name"] = fileInfo.Name,
                    ["path"] = GetRelativePath(file),
                    ["size"] = fileInfo.Length,
                    ["extension"] = fileInfo.Extension
                });
            }

            return new JObject
            {
                ["path"] = path,
                ["directories"] = directories,
                ["files"] = files,
                ["directoryCount"] = directories.Count,
                ["fileCount"] = files.Count
            };
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        private static JObject CreateDirectory(JObject p)
        {
            string path = p["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                throw new System.Exception("Path is required");

            // Ensure path is within Assets
            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
            {
                path = Path.Combine("Assets", path);
            }

            string fullPath = GetFullPath(path);
            
            if (Directory.Exists(fullPath))
            {
                return new JObject
                {
                    ["ok"] = true,
                    ["path"] = path,
                    ["alreadyExists"] = true
                };
            }

            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();

            return new JObject
            {
                ["ok"] = true,
                ["path"] = path,
                ["created"] = true
            };
        }

        private static string GetFullPath(string relativePath)
        {
            // If already absolute, return as is
            if (Path.IsPathRooted(relativePath))
                return relativePath;
                
            // Get project root (parent of Assets folder)
            string projectPath = Application.dataPath.Replace("/Assets", "");
            return Path.Combine(projectPath, relativePath);
        }

        private static string GetRelativePath(string fullPath)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");
            if (fullPath.StartsWith(projectPath))
            {
                return fullPath.Substring(projectPath.Length + 1).Replace("\\", "/");
            }
            return fullPath;
        }
    }
}

