using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles asset-related commands for the MCP Server
    /// </summary>
    public static class AssetCommandHandler
    {
        /// <summary>
        /// Imports an asset into the project
        /// </summary>
        public static object ImportAsset(JObject @params)
        {
            try
            {
                string sourcePath = (string)@params["source_path"];
                string targetPath = (string)@params["target_path"];

                if (string.IsNullOrEmpty(sourcePath))
                    return new { success = false, error = "Source path cannot be empty" };

                if (string.IsNullOrEmpty(targetPath))
                    return new { success = false, error = "Target path cannot be empty" };

                if (!File.Exists(sourcePath))
                    return new { success = false, error = $"Source file not found: {sourcePath}" };

                // Ensure target directory exists
                string targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Copy file to target location
                File.Copy(sourcePath, targetPath, true);
                AssetDatabase.Refresh();

                return new
                {
                    success = true,
                    message = $"Successfully imported asset to {targetPath}",
                    path = targetPath
                };
            }
            catch (System.Exception e)
            {
                return new
                {
                    success = false,
                    error = $"Failed to import asset: {e.Message}",
                    stackTrace = e.StackTrace
                };
            }
        }

        /// <summary>
        /// Instantiates a prefab in the current scene
        /// </summary>
        public static object InstantiatePrefab(JObject @params)
        {
            try
            {
                string prefabPath = (string)@params["prefab_path"];

                if (string.IsNullOrEmpty(prefabPath))
                    return new { success = false, error = "Prefab path cannot be empty" };

                Vector3 position = new(
                    (float)@params["position_x"],
                    (float)@params["position_y"],
                    (float)@params["position_z"]
                );
                Vector3 rotation = new(
                    (float)@params["rotation_x"],
                    (float)@params["rotation_y"],
                    (float)@params["rotation_z"]
                );

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    return new { success = false, error = $"Prefab not found at path: {prefabPath}" };
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null)
                {
                    return new { success = false, error = $"Failed to instantiate prefab: {prefabPath}" };
                }

                instance.transform.position = position;
                instance.transform.rotation = Quaternion.Euler(rotation);

                return new
                {
                    success = true,
                    message = "Successfully instantiated prefab",
                    instance_name = instance.name
                };
            }
            catch (System.Exception e)
            {
                return new
                {
                    success = false,
                    error = $"Failed to instantiate prefab: {e.Message}",
                    stackTrace = e.StackTrace
                };
            }
        }

        /// <summary>
        /// Creates a new prefab from a GameObject in the scene
        /// </summary>
        public static object CreatePrefab(JObject @params)
        {
            try
            {
                string objectName = (string)@params["object_name"];
                string prefabPath = (string)@params["prefab_path"];

                if (string.IsNullOrEmpty(objectName))
                    return new { success = false, error = "GameObject name cannot be empty" };

                if (string.IsNullOrEmpty(prefabPath))
                    return new { success = false, error = "Prefab path cannot be empty" };

                // Ensure prefab has .prefab extension
                if (!prefabPath.ToLower().EndsWith(".prefab"))
                    prefabPath = $"{prefabPath}.prefab";

                GameObject sourceObject = GameObject.Find(objectName);
                if (sourceObject == null)
                {
                    return new { success = false, error = $"GameObject not found in scene: {objectName}" };
                }

                // Ensure target directory exists
                string targetDir = Path.GetDirectoryName(prefabPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sourceObject, prefabPath);
                if (prefab == null)
                {
                    return new { success = false, error = "Failed to create prefab. Verify the path is writable." };
                }

                return new
                {
                    success = true,
                    message = $"Successfully created prefab at {prefabPath}",
                    path = prefabPath
                };
            }
            catch (System.Exception e)
            {
                return new
                {
                    success = false,
                    error = $"Failed to create prefab: {e.Message}",
                    stackTrace = e.StackTrace,
                    sourceInfo = $"Object: {@params["object_name"]}, Path: {@params["prefab_path"]}"
                };
            }
        }

        /// <summary>
        /// Applies changes from a prefab instance back to the original prefab asset
        /// </summary>
        public static object ApplyPrefab(JObject @params)
        {
            string objectName = (string)@params["object_name"];

            GameObject instance = GameObject.Find(objectName);
            if (instance == null)
            {
                return new { error = $"GameObject not found in scene: {objectName}" };
            }

            Object prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (prefabAsset == null)
            {
                return new { error = "Selected object is not a prefab instance" };
            }

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            return new { message = "Successfully applied changes to prefab asset" };
        }

        /// <summary>
        /// Gets a list of assets in the project, optionally filtered by type
        /// </summary>
        public static object GetAssetList(JObject @params)
        {
            string type = (string)@params["type"];
            string searchPattern = (string)@params["search_pattern"] ?? "*";
            string folder = (string)@params["folder"] ?? "Assets";

            var guids = AssetDatabase.FindAssets(searchPattern, new[] { folder });
            var assets = new List<object>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                // Skip if type filter is specified and doesn't match
                if (!string.IsNullOrEmpty(type) && assetType?.Name != type)
                    continue;

                assets.Add(new
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    path,
                    type = assetType?.Name ?? "Unknown",
                    guid
                });
            }

            return new { assets };
        }
    }
}