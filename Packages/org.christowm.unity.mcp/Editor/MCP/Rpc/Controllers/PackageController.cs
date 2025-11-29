using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System.Threading.Tasks;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing Unity packages.
    /// </summary>
    public static class PackageController
    {
        private static ListRequest _listRequest;
        private static AddRequest _addRequest;
        private static RemoveRequest _removeRequest;
        private static SearchRequest _searchRequest;

        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.list_packages", ListPackages);
            JsonRpcDispatcher.RegisterMethod("unity.get_package_info", GetPackageInfo);
            JsonRpcDispatcher.RegisterMethod("unity.add_package", AddPackage);
            JsonRpcDispatcher.RegisterMethod("unity.remove_package", RemovePackage);
            JsonRpcDispatcher.RegisterMethod("unity.search_packages", SearchPackages);
        }

        /// <summary>
        /// Lists all installed packages.
        /// </summary>
        private static JObject ListPackages(JObject p)
        {
            _listRequest = Client.List(true); // Include dependencies
            
            // Wait for request to complete (synchronously for RPC)
            while (!_listRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_listRequest.Status == StatusCode.Failure)
            {
                throw new System.Exception($"Failed to list packages: {_listRequest.Error?.message}");
            }

            var packages = new JArray();
            foreach (var package in _listRequest.Result)
            {
                packages.Add(new JObject
                {
                    ["name"] = package.name,
                    ["displayName"] = package.displayName,
                    ["version"] = package.version,
                    ["description"] = package.description,
                    ["source"] = package.source.ToString(),
                    ["isDirectDependency"] = package.isDirectDependency
                });
            }

            return new JObject
            {
                ["packages"] = packages,
                ["count"] = packages.Count
            };
        }

        /// <summary>
        /// Gets detailed information about a specific package.
        /// </summary>
        private static JObject GetPackageInfo(JObject p)
        {
            string packageName = p["name"]?.ToString();
            
            if (string.IsNullOrEmpty(packageName))
                throw new System.Exception("Package name is required");

            _listRequest = Client.List(true);
            
            while (!_listRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_listRequest.Status == StatusCode.Failure)
            {
                throw new System.Exception($"Failed to get package info: {_listRequest.Error?.message}");
            }

            foreach (var package in _listRequest.Result)
            {
                if (package.name == packageName)
                {
                    var dependencies = new JArray();
                    if (package.dependencies != null)
                    {
                        foreach (var dep in package.dependencies)
                        {
                            dependencies.Add(new JObject
                            {
                                ["name"] = dep.name,
                                ["version"] = dep.version
                            });
                        }
                    }

                    var keywords = new JArray();
                    if (package.keywords != null)
                    {
                        foreach (var keyword in package.keywords)
                        {
                            keywords.Add(keyword);
                        }
                    }

                    return new JObject
                    {
                        ["name"] = package.name,
                        ["displayName"] = package.displayName,
                        ["version"] = package.version,
                        ["description"] = package.description,
                        ["source"] = package.source.ToString(),
                        ["category"] = package.category,
                        ["documentationUrl"] = package.documentationUrl,
                        ["changelogUrl"] = package.changelogUrl,
                        ["licensesUrl"] = package.licensesUrl,
                        ["author"] = package.author?.name,
                        ["dependencies"] = dependencies,
                        ["keywords"] = keywords,
                        ["isDirectDependency"] = package.isDirectDependency,
                        ["resolvedPath"] = package.resolvedPath
                    };
                }
            }

            throw new System.Exception($"Package not found: {packageName}");
        }

        /// <summary>
        /// Adds a package to the project.
        /// </summary>
        private static JObject AddPackage(JObject p)
        {
            string packageId = p["packageId"]?.ToString();
            
            if (string.IsNullOrEmpty(packageId))
                throw new System.Exception("Package ID is required (e.g., 'com.unity.inputsystem' or 'com.unity.inputsystem@1.0.0')");

            _addRequest = Client.Add(packageId);
            
            while (!_addRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_addRequest.Status == StatusCode.Failure)
            {
                throw new System.Exception($"Failed to add package: {_addRequest.Error?.message}");
            }

            var result = _addRequest.Result;
            return new JObject
            {
                ["ok"] = true,
                ["name"] = result.name,
                ["version"] = result.version,
                ["displayName"] = result.displayName
            };
        }

        /// <summary>
        /// Removes a package from the project.
        /// </summary>
        private static JObject RemovePackage(JObject p)
        {
            string packageName = p["name"]?.ToString();
            
            if (string.IsNullOrEmpty(packageName))
                throw new System.Exception("Package name is required");

            _removeRequest = Client.Remove(packageName);
            
            while (!_removeRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_removeRequest.Status == StatusCode.Failure)
            {
                throw new System.Exception($"Failed to remove package: {_removeRequest.Error?.message}");
            }

            return new JObject
            {
                ["ok"] = true,
                ["removed"] = packageName
            };
        }

        /// <summary>
        /// Searches for packages in the Unity registry.
        /// Uses SearchAll and filters client-side for fuzzy matching.
        /// </summary>
        private static JObject SearchPackages(JObject p)
        {
            string query = p["query"]?.ToString()?.ToLowerInvariant();
            
            // Always use SearchAll - Client.Search() only does exact name matching
            _searchRequest = Client.SearchAll();
            
            while (!_searchRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_searchRequest.Status == StatusCode.Failure)
            {
                throw new System.Exception($"Failed to search packages: {_searchRequest.Error?.message}");
            }

            var packages = new JArray();
            foreach (var package in _searchRequest.Result)
            {
                // If query is provided, filter results
                if (!string.IsNullOrEmpty(query))
                {
                    bool matches = 
                        (package.name?.ToLowerInvariant().Contains(query) ?? false) ||
                        (package.displayName?.ToLowerInvariant().Contains(query) ?? false) ||
                        (package.description?.ToLowerInvariant().Contains(query) ?? false);
                    
                    if (!matches) continue;
                }
                
                packages.Add(new JObject
                {
                    ["name"] = package.name,
                    ["displayName"] = package.displayName,
                    ["version"] = package.version,
                    ["description"] = package.description
                });
            }

            return new JObject
            {
                ["packages"] = packages,
                ["count"] = packages.Count,
                ["query"] = query ?? ""
            };
        }
    }
}

