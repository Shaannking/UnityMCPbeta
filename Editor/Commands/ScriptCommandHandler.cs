using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles script-related commands for Unity
    /// </summary>
    public static class ScriptCommandHandler
    {
        /// <summary>
        /// Views the contents of a Unity script file
        /// </summary>
        public static object ViewScript(JObject @params)
        {
            string scriptPath = (string)@params["script_path"] ?? throw new Exception("Parameter 'script_path' is required.");
            bool requireExists = (bool?)@params["require_exists"] ?? true;

            // Handle path correctly to avoid double "Assets" folder issue
            string relativePath;
            if (scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                // If path already starts with Assets/, remove it for local path operations
                relativePath = scriptPath.Substring(7);
            }
            else
            {
                relativePath = scriptPath;
            }

            string fullPath = Path.Combine(Application.dataPath, relativePath);

            if (!File.Exists(fullPath))
            {
                if (requireExists)
                {
                    throw new Exception($"Script file not found: {scriptPath}");
                }
                else
                {
                    return new { exists = false, message = $"Script file not found: {scriptPath}" };
                }
            }

            return new { exists = true, content = File.ReadAllText(fullPath) };
        }

        /// <summary>
        /// Ensures the Scripts folder exists in the project
        /// </summary>
        private static void EnsureScriptsFolderExists()
        {
            // Never create an "Assets" folder as it's the project root
            // Instead create "Scripts" within the existing Assets folder
            string scriptsFolderPath = Path.Combine(Application.dataPath, "Scripts");
            if (!Directory.Exists(scriptsFolderPath))
            {
                Directory.CreateDirectory(scriptsFolderPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Creates a new Unity script file in the specified folder
        /// </summary>
        public static object CreateScript(JObject @params)
        {
            string scriptName = (string)@params["script_name"] ?? throw new Exception("Parameter 'script_name' is required.");
            string scriptType = (string)@params["script_type"] ?? "MonoBehaviour";
            string namespaceName = (string)@params["namespace"];
            string template = (string)@params["template"];
            string scriptFolder = (string)@params["script_folder"];
            string content = (string)@params["content"];
            bool overwrite = (bool?)@params["overwrite"] ?? false;

            // Ensure script name ends with .cs
            if (!scriptName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                scriptName += ".cs";

            // Make sure scriptName doesn't contain path separators - extract base name
            scriptName = Path.GetFileName(scriptName);

            // Determine the script path
            string scriptPath;

            // Handle the script folder parameter
            if (string.IsNullOrEmpty(scriptFolder))
            {
                // Default to Scripts folder within Assets
                scriptPath = "Scripts";
                EnsureScriptsFolderExists();
            }
            else
            {
                // Use provided folder path
                scriptPath = scriptFolder;

                // If scriptFolder starts with "Assets/", remove it for local path operations
                if (scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    scriptPath = scriptPath.Substring(7);
                }
            }

            // Create the full directory path, avoiding Assets/Assets issue
            string folderPath = Path.Combine(Application.dataPath, scriptPath);

            // Create directory if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    Directory.CreateDirectory(folderPath);
                    AssetDatabase.Refresh();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create directory '{scriptPath}': {ex.Message}");
                }
            }

            // Check if script already exists
            string fullFilePath = Path.Combine(folderPath, scriptName);
            if (File.Exists(fullFilePath) && !overwrite)
            {
                throw new Exception($"Script file '{scriptName}' already exists in '{scriptPath}' and overwrite is not enabled.");
            }

            try
            {
                // If content is provided, use it directly
                if (!string.IsNullOrEmpty(content))
                {
                    // Create the script file with provided content
                    File.WriteAllText(fullFilePath, content);
                }
                else
                {
                    // Otherwise generate content based on template and parameters
                    StringBuilder contentBuilder = new();

                    // Add using directives
                    contentBuilder.AppendLine("using UnityEngine;");
                    contentBuilder.AppendLine();

                    // Add namespace if specified
                    if (!string.IsNullOrEmpty(namespaceName))
                    {
                        contentBuilder.AppendLine($"namespace {namespaceName}");
                        contentBuilder.AppendLine("{");
                    }

                    // Add class definition with indent based on namespace
                    string indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
                    contentBuilder.AppendLine($"{indent}public class {Path.GetFileNameWithoutExtension(scriptName)} : {scriptType}");
                    contentBuilder.AppendLine($"{indent}{{");

                    // Add default Unity methods based on script type
                    if (scriptType == "MonoBehaviour")
                    {
                        contentBuilder.AppendLine($"{indent}    private void Start()");
                        contentBuilder.AppendLine($"{indent}    {{");
                        contentBuilder.AppendLine($"{indent}        // Initialize your component here");
                        contentBuilder.AppendLine($"{indent}    }}");
                        contentBuilder.AppendLine();
                        contentBuilder.AppendLine($"{indent}    private void Update()");
                        contentBuilder.AppendLine($"{indent}    {{");
                        contentBuilder.AppendLine($"{indent}        // Update your component here");
                        contentBuilder.AppendLine($"{indent}    }}");
                    }
                    else if (scriptType == "ScriptableObject")
                    {
                        contentBuilder.AppendLine($"{indent}    private void OnEnable()");
                        contentBuilder.AppendLine($"{indent}    {{");
                        contentBuilder.AppendLine($"{indent}        // Initialize your ScriptableObject here");
                        contentBuilder.AppendLine($"{indent}    }}");
                    }

                    // Close class
                    contentBuilder.AppendLine($"{indent}}}");

                    // Close namespace if specified
                    if (!string.IsNullOrEmpty(namespaceName))
                    {
                        contentBuilder.AppendLine("}");
                    }

                    // Write the generated content to file
                    File.WriteAllText(fullFilePath, contentBuilder.ToString());
                }

                // Refresh the AssetDatabase to recognize the new script
                AssetDatabase.Refresh();

                // Return the relative path for easier reference
                string relativePath = scriptPath.Replace('\\', '/');
                if (!relativePath.StartsWith("Assets/"))
                {
                    relativePath = $"Assets/{relativePath}";
                }

                return new
                {
                    message = $"Created script: {Path.Combine(relativePath, scriptName).Replace('\\', '/')}",
                    script_path = Path.Combine(relativePath, scriptName).Replace('\\', '/')
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create script: {ex.Message}\n{ex.StackTrace}");
                throw new Exception($"Failed to create script '{scriptName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the contents of an existing Unity script
        /// </summary>
        public static object UpdateScript(JObject @params)
        {
            string scriptPath = (string)@params["script_path"] ?? throw new Exception("Parameter 'script_path' is required.");
            string content = (string)@params["content"] ?? throw new Exception("Parameter 'content' is required.");
            bool createIfMissing = (bool?)@params["create_if_missing"] ?? false;
            bool createFolderIfMissing = (bool?)@params["create_folder_if_missing"] ?? false;

            // Handle path correctly to avoid double "Assets" folder
            string relativePath;
            if (scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                // If path already starts with Assets/, remove it for local path operations
                relativePath = scriptPath.Substring(7);
            }
            else
            {
                relativePath = scriptPath;
            }

            string fullPath = Path.Combine(Application.dataPath, relativePath);
            string directory = Path.GetDirectoryName(fullPath);

            // Debug the paths to help diagnose issues


            // Check if file exists, create if requested
            if (!File.Exists(fullPath))
            {
                if (createIfMissing)
                {
                    // Create the directory if requested and needed
                    if (!Directory.Exists(directory) && createFolderIfMissing)
                    {
                        Directory.CreateDirectory(directory);
                    }
                    else if (!Directory.Exists(directory))
                    {
                        throw new Exception($"Directory does not exist: {Path.GetDirectoryName(scriptPath)}");
                    }

                    // Create the file with content
                    File.WriteAllText(fullPath, content);
                    AssetDatabase.Refresh();
                    return new { message = $"Created script: {scriptPath}" };
                }
                else
                {
                    throw new Exception($"Script file not found: {scriptPath}");
                }
            }

            // Update existing script
            File.WriteAllText(fullPath, content);

            // Refresh the AssetDatabase
            AssetDatabase.Refresh();

            return new { message = $"Updated script: {scriptPath}" };
        }

        /// <summary>
        /// Lists all script files in a specified folder
        /// </summary>
        public static object ListScripts(JObject @params)
        {
            string folderPath = (string)@params["folder_path"] ?? "Assets";

            // Special handling for "Assets" path since it's already the root
            string fullPath;
            if (folderPath.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = Application.dataPath;
            }
            else if (folderPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                // Remove "Assets/" from the path since Application.dataPath already points to it
                string relativePath = folderPath.Substring(7);
                fullPath = Path.Combine(Application.dataPath, relativePath);
            }
            else
            {
                // Assume it's a relative path from Assets
                fullPath = Path.Combine(Application.dataPath, folderPath);
            }

            if (!Directory.Exists(fullPath))
                throw new Exception($"Folder not found: {folderPath}");

            string[] scripts = Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories)
                .Select(path => path.Replace(Application.dataPath, "Assets"))
                .ToArray();

            return new { scripts };
        }

        /// <summary>
        /// Attaches a script component to a GameObject
        /// </summary>
        public static object AttachScript(JObject @params)
        {
            string objectName = (string)@params["object_name"] ?? throw new Exception("Parameter 'object_name' is required.");
            string scriptName = (string)@params["script_name"] ?? throw new Exception("Parameter 'script_name' is required.");
            string scriptPath = (string)@params["script_path"]; // Optional

            // Find the target object
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
                throw new Exception($"Object '{objectName}' not found in scene.");

            // Ensure script name ends with .cs
            if (!scriptName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                scriptName += ".cs";

            // Remove the path from the scriptName if it contains path separators
            string scriptFileName = Path.GetFileName(scriptName);
            string scriptNameWithoutExtension = Path.GetFileNameWithoutExtension(scriptFileName);

            // Find the script asset
            string[] guids;

            if (!string.IsNullOrEmpty(scriptPath))
            {
                // If a specific path is provided, try that first
                if (File.Exists(Path.Combine(Application.dataPath, scriptPath.Replace("Assets/", ""))))
                {
                    // Use the direct path if it exists
                    MonoScript scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    if (scriptAsset != null)
                    {
                        Type scriptType = scriptAsset.GetClass();
                        if (scriptType != null)
                        {
                            try
                            {
                                // Try to add the component
                                Component component = targetObject.AddComponent(scriptType);
                                if (component != null)
                                {
                                    return new
                                    {
                                        message = $"Successfully attached script '{scriptFileName}' to object '{objectName}'",
                                        component_type = scriptType.Name
                                    };
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Error attaching script component: {ex.Message}");
                                throw new Exception($"Failed to add component: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Use the file name for searching if direct path didn't work
            guids = AssetDatabase.FindAssets(scriptNameWithoutExtension + " t:script");

            if (guids.Length == 0)
            {
                // Try a broader search if exact match fails
                guids = AssetDatabase.FindAssets(scriptNameWithoutExtension);

                if (guids.Length == 0)
                    throw new Exception($"Script '{scriptFileName}' not found in project.");
            }

            // Check each potential script until we find one that can be attached
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Filter to only consider .cs files
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Double check the file name to avoid false matches
                string foundFileName = Path.GetFileName(path);
                if (!string.Equals(foundFileName, scriptFileName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(Path.GetFileNameWithoutExtension(foundFileName), scriptNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                MonoScript scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (scriptAsset == null)
                    continue;

                Type scriptType = scriptAsset.GetClass();
                if (scriptType == null || !typeof(MonoBehaviour).IsAssignableFrom(scriptType))
                    continue;

                try
                {
                    // Check if component is already attached
                    if (targetObject.GetComponent(scriptType) != null)
                    {
                        return new
                        {
                            message = $"Script '{scriptNameWithoutExtension}' is already attached to object '{objectName}'",
                            component_type = scriptType.Name
                        };
                    }

                    // Add the component
                    Component component = targetObject.AddComponent(scriptType);
                    if (component != null)
                    {
                        return new
                        {
                            message = $"Successfully attached script '{scriptFileName}' to object '{objectName}'",
                            component_type = scriptType.Name,
                            script_path = path
                        };
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error attaching script '{path}': {ex.Message}");
                    // Continue trying other matches instead of failing immediately
                }
            }

            // If we've tried all possibilities and nothing worked
            throw new Exception($"Could not attach script '{scriptFileName}' to object '{objectName}'. No valid script found or component creation failed.");
        }
    }
}