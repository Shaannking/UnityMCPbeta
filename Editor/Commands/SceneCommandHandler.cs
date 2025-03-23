using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles scene-related commands for the MCP Server
    /// </summary>
    public static class SceneCommandHandler
    {
        /// <summary>
        /// Gets information about the current scene
        /// </summary>
        /// <returns>Scene information including name and root objects</returns>
        public static object GetSceneInfo()
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects().Select(o => o.name).ToArray();
            return new { sceneName = scene.name, rootObjects };
        }

        /// <summary>
        /// Opens a specified scene in the Unity editor
        /// </summary>
        /// <param name="params">Parameters containing the scene path</param>
        /// <returns>Result of the operation</returns>
        public static object OpenScene(JObject @params)
        {
            try
            {
                string scenePath = (string)@params["scene_path"];
                if (string.IsNullOrEmpty(scenePath))
                    return new { success = false, error = "Scene path cannot be empty" };

                if (!System.IO.File.Exists(scenePath))
                    return new { success = false, error = $"Scene file not found: {scenePath}" };

                EditorSceneManager.OpenScene(scenePath);
                return new { success = true, message = $"Opened scene: {scenePath}" };
            }
            catch (Exception e)
            {
                return new { success = false, error = $"Failed to open scene: {e.Message}", stackTrace = e.StackTrace };
            }
        }

        /// <summary>
        /// Saves the current scene
        /// </summary>
        /// <returns>Result of the operation</returns>
        public static object SaveScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene);
                return new { success = true, message = $"Saved scene: {scene.path}" };
            }
            catch (Exception e)
            {
                return new { success = false, error = $"Failed to save scene: {e.Message}", stackTrace = e.StackTrace };
            }
        }

        /// <summary>
        /// Creates a new empty scene
        /// </summary>
        /// <param name="params">Parameters containing the new scene path</param>
        /// <returns>Result of the operation</returns>
        public static object NewScene(JObject @params)
        {
            try
            {
                string scenePath = (string)@params["scene_path"];
                if (string.IsNullOrEmpty(scenePath))
                    return new { success = false, error = "Scene path cannot be empty" };

                // Create new scene
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

                // Ensure the scene is loaded and active
                if (!scene.isLoaded)
                {
                    EditorSceneManager.LoadScene(scenePath);
                }

                // Save the scene
                EditorSceneManager.SaveScene(scene, scenePath);

                // Force a refresh of the scene view
                EditorApplication.ExecuteMenuItem("Window/General/Scene");

                return new { success = true, message = $"Created new scene at: {scenePath}" };
            }
            catch (Exception e)
            {
                return new { success = false, error = $"Failed to create new scene: {e.Message}", stackTrace = e.StackTrace };
            }
        }

        /// <summary>
        /// Changes to a different scene, optionally saving the current one
        /// </summary>
        /// <param name="params">Parameters containing the target scene path and save option</param>
        /// <returns>Result of the operation</returns>
        public static object ChangeScene(JObject @params)
        {
            try
            {
                string scenePath = (string)@params["scene_path"];
                bool saveCurrent = @params["save_current"]?.Value<bool>() ?? false;

                if (string.IsNullOrEmpty(scenePath))
                    return new { success = false, error = "Scene path cannot be empty" };

                if (!System.IO.File.Exists(scenePath))
                    return new { success = false, error = $"Scene file not found: {scenePath}" };

                // Save current scene if requested
                if (saveCurrent)
                {
                    var currentScene = SceneManager.GetActiveScene();
                    EditorSceneManager.SaveScene(currentScene);
                }

                // Open the new scene
                EditorSceneManager.OpenScene(scenePath);
                return new { success = true, message = $"Changed to scene: {scenePath}" };
            }
            catch (Exception e)
            {
                return new { success = false, error = $"Failed to change scene: {e.Message}", stackTrace = e.StackTrace };
            }
        }
    }
}