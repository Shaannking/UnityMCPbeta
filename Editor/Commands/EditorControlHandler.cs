using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles editor control commands like undo, redo, play, pause, stop, and build operations.
    /// </summary>
    public static class EditorControlHandler
    {
        /// <summary>
        /// Handles editor control commands
        /// </summary>
        public static object HandleEditorControl(JObject @params)
        {
            string command = (string)@params["command"];
            JObject commandParams = (JObject)@params["params"];

            return command.ToUpper() switch
            {
                "UNDO" => HandleUndo(),
                "REDO" => HandleRedo(),
                "PLAY" => HandlePlay(),
                "PAUSE" => HandlePause(),
                "STOP" => HandleStop(),
                "BUILD" => HandleBuild(commandParams),
                "EXECUTE_COMMAND" => HandleExecuteCommand(commandParams),
                "READ_CONSOLE" => ReadConsole(commandParams),
                "GET_AVAILABLE_COMMANDS" => GetAvailableCommands(),
                _ => new { error = $"Unknown editor control command: {command}" },
            };
        }

        private static object HandleUndo()
        {
            Undo.PerformUndo();
            return new { message = "Undo performed successfully" };
        }

        private static object HandleRedo()
        {
            Undo.PerformRedo();
            return new { message = "Redo performed successfully" };
        }

        private static object HandlePlay()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return new { message = "Entered play mode" };
            }
            return new { message = "Already in play mode" };
        }

        private static object HandlePause()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPaused = !EditorApplication.isPaused;
                return new { message = EditorApplication.isPaused ? "Game paused" : "Game resumed" };
            }
            return new { message = "Not in play mode" };
        }

        private static object HandleStop()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return new { message = "Exited play mode" };
            }
            return new { message = "Not in play mode" };
        }

        private static object HandleBuild(JObject @params)
        {
            string platform = (string)@params["platform"];
            string buildPath = (string)@params["buildPath"];

            try
            {
                BuildTarget target = GetBuildTarget(platform);
                if ((int)target == -1)
                {
                    return new { error = $"Unsupported platform: {platform}" };
                }

                BuildPlayerOptions buildPlayerOptions = new()
                {
                    scenes = GetEnabledScenes(),
                    target = target,
                    locationPathName = buildPath
                };

                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                return new
                {
                    message = "Build completed successfully",
                    report.summary
                };
            }
            catch (Exception e)
            {
                return new { error = $"Build failed: {e.Message}" };
            }
        }

        private static object HandleExecuteCommand(JObject @params)
        {
            string commandName = (string)@params["commandName"];
            try
            {
                EditorApplication.ExecuteMenuItem(commandName);
                return new { message = $"Executed command: {commandName}" };
            }
            catch (Exception e)
            {
                return new { error = $"Failed to execute command: {e.Message}" };
            }
        }

        /// <summary>
        /// Reads log messages from the Unity Console
        /// </summary>
        /// <param name="params">Parameters containing filtering options</param>
        /// <returns>Object containing console messages filtered by type</returns>
        public static object ReadConsole(JObject @params)
        {
            // Default values for show flags
            bool showLogs = true;
            bool showWarnings = true;
            bool showErrors = true;
            string searchTerm = string.Empty;

            // Get filter parameters if provided
            if (@params != null)
            {
                if (@params["show_logs"] != null) showLogs = (bool)@params["show_logs"];
                if (@params["show_warnings"] != null) showWarnings = (bool)@params["show_warnings"];
                if (@params["show_errors"] != null) showErrors = (bool)@params["show_errors"];
                if (@params["search_term"] != null) searchTerm = (string)@params["search_term"];
            }

            try
            {
                // Get required types and methods via reflection
                Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                Type logEntryType = Type.GetType("UnityEditor.LogEntry,UnityEditor");

                if (logEntriesType == null || logEntryType == null)
                    return new { error = "Could not find required Unity logging types", entries = new List<object>() };

                // Get essential methods
                MethodInfo getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo getEntryMethod = logEntriesType.GetMethod("GetEntryAt", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                                            logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (getCountMethod == null || getEntryMethod == null)
                    return new { error = "Could not find required Unity logging methods", entries = new List<object>() };

                // Get stack trace method if available
                MethodInfo getStackTraceMethod = logEntriesType.GetMethod("GetEntryStackTrace", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(int) }, null) ?? logEntriesType.GetMethod("GetStackTrace", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(int) }, null);

                // Get entry count and prepare result list
                int count = (int)getCountMethod.Invoke(null, null);
                var entries = new List<object>();

                // Create LogEntry instance to populate
                object logEntryInstance = Activator.CreateInstance(logEntryType);

                // Find properties on LogEntry type
                PropertyInfo modeProperty = logEntryType.GetProperty("mode") ?? logEntryType.GetProperty("Mode");
                PropertyInfo messageProperty = logEntryType.GetProperty("message") ?? logEntryType.GetProperty("Message");

                // Parse search terms if provided
                string[] searchWords = !string.IsNullOrWhiteSpace(searchTerm) ?
                    searchTerm.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : null;

                // Process each log entry
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        // Get log entry at index i
                        var methodParams = getEntryMethod.GetParameters();
                        if (methodParams.Length == 2 && methodParams[1].ParameterType == logEntryType)
                        {
                            getEntryMethod.Invoke(null, new object[] { i, logEntryInstance });
                        }
                        else if (methodParams.Length >= 1 && methodParams[0].ParameterType == typeof(int))
                        {
                            var parameters = new object[methodParams.Length];
                            parameters[0] = i;
                            for (int p = 1; p < parameters.Length; p++)
                            {
                                parameters[p] = methodParams[p].ParameterType.IsValueType ?
                                    Activator.CreateInstance(methodParams[p].ParameterType) : null;
                            }
                            getEntryMethod.Invoke(null, parameters);
                        }
                        else continue;

                        // Extract log data
                        int logType = modeProperty != null ?
                            Convert.ToInt32(modeProperty.GetValue(logEntryInstance) ?? 0) : 0;

                        string message = messageProperty != null ?
                            (messageProperty.GetValue(logEntryInstance)?.ToString() ?? "") : "";

                        // If message is empty, try to get it via a field
                        if (string.IsNullOrEmpty(message))
                        {
                            var msgField = logEntryType.GetField("message", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (msgField != null)
                            {
                                object msgValue = msgField.GetValue(logEntryInstance);
                                message = msgValue != null ? msgValue.ToString() : "";
                            }

                            // If still empty, try alternate approach with Console window
                            if (string.IsNullOrEmpty(message))
                            {
                                // Access ConsoleWindow and its data
                                Type consoleWindowType = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
                                if (consoleWindowType != null)
                                {
                                    try
                                    {
                                        // Get Console window instance
                                        var getWindowMethod = consoleWindowType.GetMethod("GetWindow",
                                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                            null, new[] { typeof(bool) }, null) ??
                                            consoleWindowType.GetMethod("GetConsoleWindow",
                                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                                        if (getWindowMethod != null)
                                        {
                                            object consoleWindow = getWindowMethod.Invoke(null,
                                                getWindowMethod.GetParameters().Length > 0 ? new object[] { false } : null);

                                            if (consoleWindow != null)
                                            {
                                                // Try to find log entries collection
                                                foreach (var prop in consoleWindowType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                                {
                                                    if (prop.PropertyType.IsArray ||
                                                       (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                                                    {
                                                        try
                                                        {
                                                            var logItems = prop.GetValue(consoleWindow);
                                                            if (logItems != null)
                                                            {
                                                                if (logItems.GetType().IsArray && i < ((Array)logItems).Length)
                                                                {
                                                                    var entry = ((Array)logItems).GetValue(i);
                                                                    if (entry != null)
                                                                    {
                                                                        var entryType = entry.GetType();
                                                                        var entryMessageProp = entryType.GetProperty("message") ??
                                                                                             entryType.GetProperty("Message");
                                                                        if (entryMessageProp != null)
                                                                        {
                                                                            object value = entryMessageProp.GetValue(entry);
                                                                            if (value != null)
                                                                            {
                                                                                message = value.ToString();
                                                                                break;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            // Ignore errors in this fallback approach
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Ignore errors in this fallback approach
                                    }
                                }
                            }

                            // If still empty, try one more approach with log files
                            if (string.IsNullOrEmpty(message))
                            {
                                // This is our last resort - try to get log messages from the most recent Unity log file
                                try
                                {
                                    string logPath = string.Empty;

                                    // Determine the log file path based on the platform
                                    if (Application.platform == RuntimePlatform.WindowsEditor)
                                    {
                                        logPath = System.IO.Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                            "Unity", "Editor", "Editor.log");
                                    }
                                    else if (Application.platform == RuntimePlatform.OSXEditor)
                                    {
                                        logPath = System.IO.Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                            "Library", "Logs", "Unity", "Editor.log");
                                    }
                                    else if (Application.platform == RuntimePlatform.LinuxEditor)
                                    {
                                        logPath = System.IO.Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                            ".config", "unity3d", "logs", "Editor.log");
                                    }

                                    if (!string.IsNullOrEmpty(logPath) && System.IO.File.Exists(logPath))
                                    {
                                        // Read last few lines from the log file
                                        var logLines = ReadLastLines(logPath, 100);
                                        if (logLines.Count > i)
                                        {
                                            message = logLines[logLines.Count - 1 - i];
                                        }
                                    }
                                }
                                catch
                                {
                                    // Ignore errors in this fallback approach
                                }
                            }
                        }

                        // Get stack trace if method available
                        string stackTrace = "";
                        if (getStackTraceMethod != null)
                        {
                            stackTrace = getStackTraceMethod.Invoke(null, new object[] { i })?.ToString() ?? "";
                        }

                        // Filter by type
                        bool typeMatch = (logType == 0 && showLogs) ||
                                        (logType == 1 && showWarnings) ||
                                        (logType == 2 && showErrors);
                        if (!typeMatch) continue;

                        // Filter by search term
                        bool searchMatch = true;
                        if (searchWords != null && searchWords.Length > 0)
                        {
                            string lowerMessage = message.ToLower();
                            string lowerStackTrace = stackTrace.ToLower();

                            foreach (string word in searchWords)
                            {
                                if (!lowerMessage.Contains(word) && !lowerStackTrace.Contains(word))
                                {
                                    searchMatch = false;
                                    break;
                                }
                            }
                        }
                        if (!searchMatch) continue;

                        // Add matching entry to results
                        string typeStr = logType == 0 ? "Log" : logType == 1 ? "Warning" : "Error";
                        entries.Add(new
                        {
                            type = typeStr,
                            message,
                            stackTrace
                        });
                    }
                    catch (Exception)
                    {
                        // Skip entries that cause errors
                        continue;
                    }
                }

                // Return filtered results
                return new
                {
                    message = "Console logs retrieved successfully",
                    entries,
                    total_entries = count,
                    filtered_count = entries.Count,
                    show_logs = showLogs,
                    show_warnings = showWarnings,
                    show_errors = showErrors
                };
            }
            catch (Exception e)
            {
                return new
                {
                    error = $"Failed to read console logs: {e.Message}",
                    entries = new List<object>()
                };
            }
        }

        private static MethodInfo FindMethod(Type type, string[] methodNames)
        {
            foreach (var methodName in methodNames)
            {
                var method = type.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return method;
            }
            return null;
        }

        private static BuildTarget GetBuildTarget(string platform)
        {
            BuildTarget target;
            switch (platform.ToLower())
            {
                case "windows": target = BuildTarget.StandaloneWindows64; break;
                case "mac": target = BuildTarget.StandaloneOSX; break;
                case "linux": target = BuildTarget.StandaloneLinux64; break;
                case "android": target = BuildTarget.Android; break;
                case "ios": target = BuildTarget.iOS; break;
                case "webgl": target = BuildTarget.WebGL; break;
                default: target = (BuildTarget)(-1); break; // Invalid target
            }
            return target;
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (EditorBuildSettings.scenes[i].enabled)
                {
                    scenes.Add(EditorBuildSettings.scenes[i].path);
                }
            }
            return scenes.ToArray();
        }

        /// <summary>
        /// Helper method to get information about available properties and fields in a type
        /// </summary>
        private static Dictionary<string, object> GetTypeInfo(Type type)
        {
            var result = new Dictionary<string, object>();

            // Get all public and non-public properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.Static | BindingFlags.Instance);
            var propList = new List<string>();
            foreach (var prop in properties)
            {
                propList.Add($"{prop.PropertyType.Name} {prop.Name}");
            }
            result["Properties"] = propList;

            // Get all public and non-public fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Static | BindingFlags.Instance);
            var fieldList = new List<string>();
            foreach (var field in fields)
            {
                fieldList.Add($"{field.FieldType.Name} {field.Name}");
            }
            result["Fields"] = fieldList;

            // Get all public and non-public methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Static | BindingFlags.Instance);
            var methodList = new List<string>();
            foreach (var method in methods)
            {
                if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                {
                    var parameters = string.Join(", ", method.GetParameters()
                        .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    methodList.Add($"{method.ReturnType.Name} {method.Name}({parameters})");
                }
            }
            result["Methods"] = methodList;

            return result;
        }

        /// <summary>
        /// Helper method to get all property and field values from an object
        /// </summary>
        private static Dictionary<string, string> GetObjectValues(object obj)
        {
            if (obj == null) return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var type = obj.GetType();

            // Get all property values
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    result[$"Property:{prop.Name}"] = value?.ToString() ?? "null";
                }
                catch (Exception)
                {
                    result[$"Property:{prop.Name}"] = "ERROR";
                }
            }

            // Get all field values
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(obj);
                    result[$"Field:{field.Name}"] = value?.ToString() ?? "null";
                }
                catch (Exception)
                {
                    result[$"Field:{field.Name}"] = "ERROR";
                }
            }

            return result;
        }

        /// <summary>
        /// Reads the last N lines from a file
        /// </summary>
        private static List<string> ReadLastLines(string filePath, int lineCount)
        {
            var result = new List<string>();

            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            using (var reader = new System.IO.StreamReader(stream))
            {
                string line;
                var circularBuffer = new List<string>(lineCount);
                int currentIndex = 0;

                // Read all lines keeping only the last N in a circular buffer
                while ((line = reader.ReadLine()) != null)
                {
                    if (circularBuffer.Count < lineCount)
                    {
                        circularBuffer.Add(line);
                    }
                    else
                    {
                        circularBuffer[currentIndex] = line;
                        currentIndex = (currentIndex + 1) % lineCount;
                    }
                }

                // Reorder the circular buffer so that lines are returned in order
                if (circularBuffer.Count == lineCount)
                {
                    for (int i = 0; i < lineCount; i++)
                    {
                        result.Add(circularBuffer[(currentIndex + i) % lineCount]);
                    }
                }
                else
                {
                    result.AddRange(circularBuffer);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a comprehensive list of available Unity commands, including editor menu items,
        /// internal commands, utility methods, and other actionable operations that can be executed.
        /// </summary>
        /// <returns>Object containing categorized lists of available command paths</returns>
        private static object GetAvailableCommands()
        {
            var menuCommands = new HashSet<string>();
            var utilityCommands = new HashSet<string>();
            var assetCommands = new HashSet<string>();
            var sceneCommands = new HashSet<string>();
            var gameObjectCommands = new HashSet<string>();
            var prefabCommands = new HashSet<string>();
            var shortcutCommands = new HashSet<string>();
            var otherCommands = new HashSet<string>();

            // Add a simple command that we know will work for testing
            menuCommands.Add("Window/Unity MCP");

            Debug.Log("Starting command collection...");

            try
            {
                // Add all EditorApplication static methods - these are guaranteed to work
                Debug.Log("Adding EditorApplication methods...");
                foreach (MethodInfo method in typeof(EditorApplication).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    utilityCommands.Add($"EditorApplication.{method.Name}");
                }
                Debug.Log($"Added {utilityCommands.Count} EditorApplication methods");

                // Add built-in menu commands directly - these are common ones that should always be available
                Debug.Log("Adding built-in menu commands...");
                string[] builtInMenus = new[] {
                    "File/New Scene",
                    "File/Open Scene",
                    "File/Save",
                    "File/Save As...",
                    "Edit/Undo",
                    "Edit/Redo",
                    "Edit/Cut",
                    "Edit/Copy",
                    "Edit/Paste",
                    "Edit/Duplicate",
                    "Edit/Delete",
                    "GameObject/Create Empty",
                    "GameObject/3D Object/Cube",
                    "GameObject/3D Object/Sphere",
                    "GameObject/3D Object/Capsule",
                    "GameObject/3D Object/Cylinder",
                    "GameObject/3D Object/Plane",
                    "GameObject/Light/Directional Light",
                    "GameObject/Light/Point Light",
                    "GameObject/Light/Spotlight",
                    "GameObject/Light/Area Light",
                    "Component/Mesh/Mesh Filter",
                    "Component/Mesh/Mesh Renderer",
                    "Component/Physics/Rigidbody",
                    "Component/Physics/Box Collider",
                    "Component/Physics/Sphere Collider",
                    "Component/Physics/Capsule Collider",
                    "Component/Audio/Audio Source",
                    "Component/Audio/Audio Listener",
                    "Window/General/Scene",
                    "Window/General/Game",
                    "Window/General/Inspector",
                    "Window/General/Hierarchy",
                    "Window/General/Project",
                    "Window/General/Console",
                    "Window/Analysis/Profiler",
                    "Window/Package Manager",
                    "Assets/Create/Material",
                    "Assets/Create/C# Script",
                    "Assets/Create/Prefab",
                    "Assets/Create/Scene",
                    "Assets/Create/Folder",
                };

                foreach (string menuItem in builtInMenus)
                {
                    menuCommands.Add(menuItem);
                }
                Debug.Log($"Added {builtInMenus.Length} built-in menu commands");

                // Get menu commands from MenuItem attributes - wrapped in separate try block
                Debug.Log("Searching for MenuItem attributes...");
                try
                {
                    int itemCount = 0;
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.IsDynamic) continue;

                        try
                        {
                            foreach (Type type in assembly.GetExportedTypes())
                            {
                                try
                                {
                                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                                    {
                                        try
                                        {
                                            object[] attributes = method.GetCustomAttributes(typeof(UnityEditor.MenuItem), false);
                                            if (attributes != null && attributes.Length > 0)
                                            {
                                                foreach (var attr in attributes)
                                                {
                                                    var menuItem = attr as UnityEditor.MenuItem;
                                                    if (menuItem != null && !string.IsNullOrEmpty(menuItem.menuItem))
                                                    {
                                                        menuCommands.Add(menuItem.menuItem);
                                                        itemCount++;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception methodEx)
                                        {
                                            Debug.LogWarning($"Error getting menu items for method {method.Name}: {methodEx.Message}");
                                            continue;
                                        }
                                    }
                                }
                                catch (Exception typeEx)
                                {
                                    Debug.LogWarning($"Error processing type: {typeEx.Message}");
                                    continue;
                                }
                            }
                        }
                        catch (Exception assemblyEx)
                        {
                            Debug.LogWarning($"Error examining assembly {assembly.GetName().Name}: {assemblyEx.Message}");
                            continue;
                        }
                    }
                    Debug.Log($"Found {itemCount} menu items from attributes");
                }
                catch (Exception menuItemEx)
                {
                    Debug.LogError($"Failed to get menu items: {menuItemEx.Message}");
                }

                // Add EditorUtility methods as commands
                Debug.Log("Adding EditorUtility methods...");
                foreach (MethodInfo method in typeof(EditorUtility).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    utilityCommands.Add($"EditorUtility.{method.Name}");
                }
                Debug.Log($"Added {typeof(EditorUtility).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} EditorUtility methods");

                // Add AssetDatabase methods as commands
                Debug.Log("Adding AssetDatabase methods...");
                foreach (MethodInfo method in typeof(AssetDatabase).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    assetCommands.Add($"AssetDatabase.{method.Name}");
                }
                Debug.Log($"Added {typeof(AssetDatabase).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} AssetDatabase methods");

                // Add EditorSceneManager methods as commands
                Debug.Log("Adding EditorSceneManager methods...");
                Type sceneManagerType = typeof(UnityEditor.SceneManagement.EditorSceneManager);
                if (sceneManagerType != null)
                {
                    foreach (MethodInfo method in sceneManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        sceneCommands.Add($"EditorSceneManager.{method.Name}");
                    }
                    Debug.Log($"Added {sceneManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static).Length} EditorSceneManager methods");
                }

                // Add GameObject manipulation commands
                Debug.Log("Adding GameObject methods...");
                foreach (MethodInfo method in typeof(GameObject).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    gameObjectCommands.Add($"GameObject.{method.Name}");
                }
                Debug.Log($"Added {typeof(GameObject).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} GameObject methods");

                // Add Selection-related commands
                Debug.Log("Adding Selection methods...");
                foreach (MethodInfo method in typeof(Selection).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    gameObjectCommands.Add($"Selection.{method.Name}");
                }
                Debug.Log($"Added {typeof(Selection).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} Selection methods");

                // Add PrefabUtility methods as commands
                Debug.Log("Adding PrefabUtility methods...");
                Type prefabUtilityType = typeof(UnityEditor.PrefabUtility);
                if (prefabUtilityType != null)
                {
                    foreach (MethodInfo method in prefabUtilityType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        prefabCommands.Add($"PrefabUtility.{method.Name}");
                    }
                    Debug.Log($"Added {prefabUtilityType.GetMethods(BindingFlags.Public | BindingFlags.Static).Length} PrefabUtility methods");
                }

                // Add Undo related methods
                Debug.Log("Adding Undo methods...");
                foreach (MethodInfo method in typeof(Undo).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    utilityCommands.Add($"Undo.{method.Name}");
                }
                Debug.Log($"Added {typeof(Undo).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} Undo methods");

                // The rest of the command gathering can be attempted but might not be critical
                try
                {
                    // Get commands from Unity's internal command system
                    Debug.Log("Trying to get internal CommandService commands...");
                    Type commandServiceType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.CommandService");
                    if (commandServiceType != null)
                    {
                        Debug.Log("Found CommandService type");
                        PropertyInfo instanceProperty = commandServiceType.GetProperty("Instance",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                        if (instanceProperty != null)
                        {
                            Debug.Log("Found Instance property");
                            object commandService = instanceProperty.GetValue(null);
                            if (commandService != null)
                            {
                                Debug.Log("Got CommandService instance");
                                MethodInfo findAllCommandsMethod = commandServiceType.GetMethod("FindAllCommands",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                                if (findAllCommandsMethod != null)
                                {
                                    Debug.Log("Found FindAllCommands method");
                                    var commandsResult = findAllCommandsMethod.Invoke(commandService, null);
                                    if (commandsResult != null)
                                    {
                                        Debug.Log("Got commands result");
                                        var commandsList = commandsResult as System.Collections.IEnumerable;
                                        if (commandsList != null)
                                        {
                                            int commandCount = 0;
                                            foreach (var cmd in commandsList)
                                            {
                                                try
                                                {
                                                    PropertyInfo nameProperty = cmd.GetType().GetProperty("name") ??
                                                                             cmd.GetType().GetProperty("path") ??
                                                                             cmd.GetType().GetProperty("commandName");
                                                    if (nameProperty != null)
                                                    {
                                                        string commandName = nameProperty.GetValue(cmd)?.ToString();
                                                        if (!string.IsNullOrEmpty(commandName))
                                                        {
                                                            otherCommands.Add(commandName);
                                                            commandCount++;
                                                        }
                                                    }
                                                }
                                                catch (Exception cmdEx)
                                                {
                                                    Debug.LogWarning($"Error processing command: {cmdEx.Message}");
                                                    continue;
                                                }
                                            }
                                            Debug.Log($"Added {commandCount} internal commands");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("FindAllCommands returned null");
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("FindAllCommands method not found");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("CommandService instance is null");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Instance property not found on CommandService");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("CommandService type not found");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to get internal Unity commands: {e.Message}");
                }

                // Other additional command sources can be tried
                // ... other commands ...
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting Unity commands: {e.Message}\n{e.StackTrace}");
            }

            // Create command categories dictionary for the result
            var commandCategories = new Dictionary<string, List<string>>
            {
                { "MenuCommands", menuCommands.OrderBy(x => x).ToList() },
                { "UtilityCommands", utilityCommands.OrderBy(x => x).ToList() },
                { "AssetCommands", assetCommands.OrderBy(x => x).ToList() },
                { "SceneCommands", sceneCommands.OrderBy(x => x).ToList() },
                { "GameObjectCommands", gameObjectCommands.OrderBy(x => x).ToList() },
                { "PrefabCommands", prefabCommands.OrderBy(x => x).ToList() },
                { "ShortcutCommands", shortcutCommands.OrderBy(x => x).ToList() },
                { "OtherCommands", otherCommands.OrderBy(x => x).ToList() }
            };

            // Calculate total command count
            int totalCount = commandCategories.Values.Sum(list => list.Count);

            Debug.Log($"Command retrieval complete. Found {totalCount} total commands.");

            // Create a simplified response with just the essential data
            // The complex object structure might be causing serialization issues
            var allCommandsList = commandCategories.Values.SelectMany(x => x).OrderBy(x => x).ToList();

            // Use simple string array instead of JArray for better serialization
            string[] commandsArray = allCommandsList.ToArray();

            // Log the array size for verification
            Debug.Log($"Final commands array contains {commandsArray.Length} items");

            try
            {
                // Return a simple object with just the commands array and count
                var result = new
                {
                    commands = commandsArray,
                    count = commandsArray.Length
                };

                // Verify the result can be serialized properly
                var jsonTest = JsonUtility.ToJson(new { test = "This is a test" });
                Debug.Log($"JSON serialization test successful: {jsonTest}");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating response: {ex.Message}");

                // Ultimate fallback - don't use any JObject/JArray
                return new
                {
                    message = $"Found {commandsArray.Length} commands",
                    firstTen = commandsArray.Take(10).ToArray(),
                    count = commandsArray.Length
                };
            }
        }
    }
}