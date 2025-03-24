using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles UI-related commands for the MCP Server
    /// </summary>
    public static class UICommandHandler
    {
        /// <summary>
        /// Creates a UI Button in the scene
        /// </summary>
        public static object CreateUIButton(JObject @params)
        {
            string buttonName = (string)@params["name"] ?? "New Button";
            // Get position if provided
            Vector3 position = new Vector3(0, 0, 0);
            if (@params.ContainsKey("position"))
            {
                JArray posArray = (JArray)@params["position"];
                position = new Vector3((float)posArray[0], (float)posArray[1], (float)posArray[2]);
            }
            
            // First ensure we have a Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // Create a canvas if none exists
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Create button
            GameObject buttonObj = new GameObject(buttonName);
            buttonObj.transform.SetParent(canvas.transform, false);
            buttonObj.transform.localPosition = position;
            
            // Add button component and image
            Button button = buttonObj.AddComponent<Button>();
            Image image = buttonObj.AddComponent<Image>();
            button.targetGraphic = image;
            
            // Add text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = "Button";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            
            // Set text rect transform to fill button
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = new Vector2(0, 0);
            textRT.offsetMax = new Vector2(0, 0);
            
            // Set button size
            RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
            buttonRT.sizeDelta = new Vector2(160, 30);
            
            return new { 
                success = true, 
                message = $"Created UI Button: {buttonName}",
                buttonPath = GetGameObjectPath(buttonObj)
            };
        }
        
        /// <summary>
        /// Creates a UI Text element in the scene
        /// </summary>
        public static object CreateUIText(JObject @params)
        {
            string textName = (string)@params["name"] ?? "New Text";
            string content = (string)@params["content"] ?? "New Text";
            
            // Get position if provided
            Vector3 position = new Vector3(0, 0, 0);
            if (@params.ContainsKey("position"))
            {
                JArray posArray = (JArray)@params["position"];
                position = new Vector3((float)posArray[0], (float)posArray[1], (float)posArray[2]);
            }
            
            // First ensure we have a Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // Create a canvas if none exists
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Create text object
            GameObject textObj = new GameObject(textName);
            textObj.transform.SetParent(canvas.transform, false);
            textObj.transform.localPosition = position;
            
            // Add text component
            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            
            // Set text size
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.sizeDelta = new Vector2(200, 50);
            
            return new { 
                success = true, 
                message = $"Created UI Text: {textName}",
                textPath = GetGameObjectPath(textObj)
            };
        }
        
        // Helper to get GameObject path
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
