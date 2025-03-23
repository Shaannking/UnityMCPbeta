using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles material-related commands
    /// </summary>
    public static class MaterialCommandHandler
    {
        /// <summary>
        /// Sets or modifies a material on an object
        /// </summary>
        public static object SetMaterial(JObject @params)
        {
            string objectName = (string)@params["object_name"] ?? throw new System.Exception("Parameter 'object_name' is required.");
            var obj = GameObject.Find(objectName) ?? throw new System.Exception($"Object '{objectName}' not found.");
            var renderer = obj.GetComponent<Renderer>() ?? throw new System.Exception($"Object '{objectName}' has no renderer.");

            // Check if URP is being used
            bool isURP = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;

            Material material = null;
            string materialName = (string)@params["material_name"];
            bool createIfMissing = (bool)(@params["create_if_missing"] ?? true);
            string materialPath = null;

            // If material name is specified, try to find or create it
            if (!string.IsNullOrEmpty(materialName))
            {
                // Ensure Materials folder exists
                const string materialsFolder = "Assets/Materials";
                if (!Directory.Exists(materialsFolder))
                {
                    Directory.CreateDirectory(materialsFolder);
                }

                materialPath = $"{materialsFolder}/{materialName}.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null && createIfMissing)
                {
                    // Create new material with appropriate shader
                    material = new Material(isURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));
                    material.name = materialName;

                    // Save the material asset
                    AssetDatabase.CreateAsset(material, materialPath);
                    AssetDatabase.SaveAssets();
                }
                else if (material == null)
                {
                    throw new System.Exception($"Material '{materialName}' not found and create_if_missing is false.");
                }
            }
            else
            {
                // Create a temporary material if no name specified
                material = new Material(isURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));
            }

            // Apply color if specified
            if (@params.ContainsKey("color"))
            {
                var colorArray = (JArray)@params["color"];
                if (colorArray.Count < 3 || colorArray.Count > 4)
                    throw new System.Exception("Color must be an array of 3 (RGB) or 4 (RGBA) floats.");

                Color color = new(
                    (float)colorArray[0],
                    (float)colorArray[1],
                    (float)colorArray[2],
                    colorArray.Count > 3 ? (float)colorArray[3] : 1.0f
                );
                material.color = color;

                // If this is a saved material, make sure to save the color change
                if (!string.IsNullOrEmpty(materialPath))
                {
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssets();
                }
            }

            // Apply the material to the renderer
            renderer.material = material;

            return new { material_name = material.name, path = materialPath };
        }
    }
}