using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for managing UI elements in Unity.
    /// </summary>
    public static class UIController
    {
        public static void Register()
        {
            JsonRpcDispatcher.RegisterMethod("unity.create_canvas", CreateCanvas);
            JsonRpcDispatcher.RegisterMethod("unity.create_ui_element", CreateUIElement);
            JsonRpcDispatcher.RegisterMethod("unity.set_ui_text", SetUIText);
            JsonRpcDispatcher.RegisterMethod("unity.set_ui_image", SetUIImage);
            JsonRpcDispatcher.RegisterMethod("unity.set_rect_transform", SetRectTransform);
            JsonRpcDispatcher.RegisterMethod("unity.get_ui_info", GetUIInfo);
            JsonRpcDispatcher.RegisterMethod("unity.list_ui_elements", ListUIElements);
        }

        /// <summary>
        /// Creates a new Canvas with EventSystem.
        /// </summary>
        private static JObject CreateCanvas(JObject p)
        {
            string name = p["name"]?.ToString() ?? "Canvas";
            string renderMode = p["renderMode"]?.ToString() ?? "ScreenSpaceOverlay";
            
            // Create Canvas
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            
            // Set render mode
            if (System.Enum.TryParse(renderMode, true, out RenderMode mode))
            {
                canvas.renderMode = mode;
            }
            
            // Add CanvasScaler
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Add GraphicRaycaster
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create EventSystem if none exists
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem via MCP");
            }
            
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas via MCP");

            return new JObject
            {
                ["id"] = canvasGO.GetInstanceID(),
                ["canvasId"] = canvas.GetInstanceID()
            };
        }

        /// <summary>
        /// Creates a UI element (Button, Text, Image, Panel, etc.).
        /// </summary>
        private static JObject CreateUIElement(JObject p)
        {
            string elementType = p["type"]?.ToString();
            int parentId = p["parentId"]?.Value<int>() ?? 0;
            string name = p["name"]?.ToString();
            
            if (string.IsNullOrEmpty(elementType))
                throw new System.Exception("Element type is required");

            // Find parent (should be a Canvas or UI element)
            Transform parent = null;
            if (parentId != 0)
            {
                var parentGO = EditorUtility.InstanceIDToObject(parentId) as GameObject;
                if (parentGO != null)
                {
                    parent = parentGO.transform;
                }
            }
            
            // If no parent specified, find or create a Canvas
            if (parent == null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    // Create a canvas first
                    var createResult = CreateCanvas(new JObject());
                    var canvasGO = EditorUtility.InstanceIDToObject(createResult["id"].Value<int>()) as GameObject;
                    parent = canvasGO.transform;
                }
                else
                {
                    parent = canvas.transform;
                }
            }

            GameObject element = null;
            
            switch (elementType.ToLower())
            {
                case "panel":
                    element = CreatePanel(name ?? "Panel", parent);
                    break;
                case "button":
                    element = CreateButton(name ?? "Button", parent);
                    break;
                case "text":
                    element = CreateText(name ?? "Text", parent);
                    break;
                case "image":
                    element = CreateImage(name ?? "Image", parent);
                    break;
                case "rawimage":
                    element = CreateRawImage(name ?? "RawImage", parent);
                    break;
                case "inputfield":
                    element = CreateInputField(name ?? "InputField", parent);
                    break;
                case "slider":
                    element = CreateSlider(name ?? "Slider", parent);
                    break;
                case "toggle":
                    element = CreateToggle(name ?? "Toggle", parent);
                    break;
                case "dropdown":
                    element = CreateDropdown(name ?? "Dropdown", parent);
                    break;
                case "scrollview":
                    element = CreateScrollView(name ?? "ScrollView", parent);
                    break;
                default:
                    throw new System.Exception($"Unknown UI element type: {elementType}");
            }

            // Set position if provided
            var rectTransform = element.GetComponent<RectTransform>();
            if (p["position"] != null)
            {
                rectTransform.anchoredPosition = new Vector2(
                    p["position"]["x"]?.Value<float>() ?? 0,
                    p["position"]["y"]?.Value<float>() ?? 0
                );
            }
            
            // Set size if provided
            if (p["size"] != null)
            {
                rectTransform.sizeDelta = new Vector2(
                    p["size"]["width"]?.Value<float>() ?? rectTransform.sizeDelta.x,
                    p["size"]["height"]?.Value<float>() ?? rectTransform.sizeDelta.y
                );
            }

            Undo.RegisterCreatedObjectUndo(element, $"Create {elementType} via MCP");

            return new JObject
            {
                ["id"] = element.GetInstanceID(),
                ["type"] = elementType,
                ["name"] = element.name
            };
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.5f);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200);
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);
            
            // Add text child
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = "Button";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return go;
        }

        private static GameObject CreateText(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = "New Text";
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private static GameObject CreateRawImage(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RawImage>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private static GameObject CreateInputField(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            var inputField = go.AddComponent<InputField>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);
            
            // Add text child
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<Text>();
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.supportRichText = false;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -7);
            
            inputField.textComponent = text;
            
            return go;
        }

        private static GameObject CreateSlider(string name, Transform parent)
        {
            // Use Unity's default creation
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var slider = go.AddComponent<Slider>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 20);
            return go;
        }

        private static GameObject CreateToggle(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var toggle = go.AddComponent<Toggle>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 20);
            return go;
        }

        private static GameObject CreateDropdown(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);
            return go;
        }

        private static GameObject CreateScrollView(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            var scrollRect = go.AddComponent<ScrollRect>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200);
            return go;
        }

        /// <summary>
        /// Sets text on a UI Text component.
        /// </summary>
        private static JObject SetUIText(JObject p)
        {
            int id = p["id"].Value<int>();
            string text = p["text"]?.ToString();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var textComponent = go.GetComponent<Text>();
            if (textComponent == null)
                throw new System.Exception("GameObject does not have a Text component");

            Undo.RecordObject(textComponent, "Set UI Text via MCP");
            
            if (text != null)
                textComponent.text = text;
            if (p["fontSize"] != null)
                textComponent.fontSize = p["fontSize"].Value<int>();
            if (p["color"] != null)
            {
                textComponent.color = new Color(
                    p["color"]["r"]?.Value<float>() ?? 0,
                    p["color"]["g"]?.Value<float>() ?? 0,
                    p["color"]["b"]?.Value<float>() ?? 0,
                    p["color"]["a"]?.Value<float>() ?? 1
                );
            }
            if (p["alignment"] != null)
            {
                if (System.Enum.TryParse(p["alignment"].ToString(), true, out TextAnchor anchor))
                {
                    textComponent.alignment = anchor;
                }
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Sets image properties on a UI Image component.
        /// </summary>
        private static JObject SetUIImage(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var image = go.GetComponent<Image>();
            if (image == null)
                throw new System.Exception("GameObject does not have an Image component");

            Undo.RecordObject(image, "Set UI Image via MCP");
            
            if (p["color"] != null)
            {
                image.color = new Color(
                    p["color"]["r"]?.Value<float>() ?? 1,
                    p["color"]["g"]?.Value<float>() ?? 1,
                    p["color"]["b"]?.Value<float>() ?? 1,
                    p["color"]["a"]?.Value<float>() ?? 1
                );
            }
            
            if (p["spritePath"] != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p["spritePath"].ToString());
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }

            if (p["fillAmount"] != null)
            {
                image.fillAmount = p["fillAmount"].Value<float>();
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Sets RectTransform properties.
        /// </summary>
        private static JObject SetRectTransform(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
                throw new System.Exception("GameObject does not have a RectTransform component");

            Undo.RecordObject(rect, "Set RectTransform via MCP");
            
            if (p["anchoredPosition"] != null)
            {
                rect.anchoredPosition = new Vector2(
                    p["anchoredPosition"]["x"]?.Value<float>() ?? rect.anchoredPosition.x,
                    p["anchoredPosition"]["y"]?.Value<float>() ?? rect.anchoredPosition.y
                );
            }
            
            if (p["sizeDelta"] != null)
            {
                rect.sizeDelta = new Vector2(
                    p["sizeDelta"]["width"]?.Value<float>() ?? rect.sizeDelta.x,
                    p["sizeDelta"]["height"]?.Value<float>() ?? rect.sizeDelta.y
                );
            }
            
            if (p["anchorMin"] != null)
            {
                rect.anchorMin = new Vector2(
                    p["anchorMin"]["x"]?.Value<float>() ?? rect.anchorMin.x,
                    p["anchorMin"]["y"]?.Value<float>() ?? rect.anchorMin.y
                );
            }
            
            if (p["anchorMax"] != null)
            {
                rect.anchorMax = new Vector2(
                    p["anchorMax"]["x"]?.Value<float>() ?? rect.anchorMax.x,
                    p["anchorMax"]["y"]?.Value<float>() ?? rect.anchorMax.y
                );
            }
            
            if (p["pivot"] != null)
            {
                rect.pivot = new Vector2(
                    p["pivot"]["x"]?.Value<float>() ?? rect.pivot.x,
                    p["pivot"]["y"]?.Value<float>() ?? rect.pivot.y
                );
            }

            return new JObject { ["ok"] = true };
        }

        /// <summary>
        /// Gets UI information about a GameObject.
        /// </summary>
        private static JObject GetUIInfo(JObject p)
        {
            int id = p["id"].Value<int>();
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.Exception("GameObject not found");

            var result = new JObject
            {
                ["name"] = go.name,
                ["id"] = go.GetInstanceID()
            };

            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                result["rectTransform"] = new JObject
                {
                    ["anchoredPosition"] = new JObject { ["x"] = rect.anchoredPosition.x, ["y"] = rect.anchoredPosition.y },
                    ["sizeDelta"] = new JObject { ["width"] = rect.sizeDelta.x, ["height"] = rect.sizeDelta.y },
                    ["anchorMin"] = new JObject { ["x"] = rect.anchorMin.x, ["y"] = rect.anchorMin.y },
                    ["anchorMax"] = new JObject { ["x"] = rect.anchorMax.x, ["y"] = rect.anchorMax.y },
                    ["pivot"] = new JObject { ["x"] = rect.pivot.x, ["y"] = rect.pivot.y }
                };
            }

            var text = go.GetComponent<Text>();
            if (text != null)
            {
                result["text"] = new JObject
                {
                    ["content"] = text.text,
                    ["fontSize"] = text.fontSize,
                    ["alignment"] = text.alignment.ToString()
                };
            }

            var image = go.GetComponent<Image>();
            if (image != null)
            {
                result["image"] = new JObject
                {
                    ["color"] = new JObject { ["r"] = image.color.r, ["g"] = image.color.g, ["b"] = image.color.b, ["a"] = image.color.a },
                    ["sprite"] = image.sprite?.name,
                    ["fillAmount"] = image.fillAmount
                };
            }

            return result;
        }

        /// <summary>
        /// Lists all UI elements in the scene.
        /// </summary>
        private static JObject ListUIElements(JObject p)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var result = new JArray();

            foreach (var canvas in canvases)
            {
                var canvasInfo = new JObject
                {
                    ["id"] = canvas.gameObject.GetInstanceID(),
                    ["name"] = canvas.gameObject.name,
                    ["renderMode"] = canvas.renderMode.ToString(),
                    ["children"] = new JArray()
                };

                foreach (Transform child in canvas.transform)
                {
                    CollectUIChildren(child, (JArray)canvasInfo["children"]);
                }

                result.Add(canvasInfo);
            }

            return new JObject
            {
                ["canvases"] = result,
                ["count"] = canvases.Length
            };
        }

        private static void CollectUIChildren(Transform parent, JArray list)
        {
            var info = new JObject
            {
                ["id"] = parent.gameObject.GetInstanceID(),
                ["name"] = parent.gameObject.name
            };

            // Identify UI component type
            if (parent.GetComponent<Button>() != null) info["type"] = "Button";
            else if (parent.GetComponent<Text>() != null) info["type"] = "Text";
            else if (parent.GetComponent<InputField>() != null) info["type"] = "InputField";
            else if (parent.GetComponent<Image>() != null) info["type"] = "Image";
            else if (parent.GetComponent<RawImage>() != null) info["type"] = "RawImage";
            else if (parent.GetComponent<Slider>() != null) info["type"] = "Slider";
            else if (parent.GetComponent<Toggle>() != null) info["type"] = "Toggle";
            else if (parent.GetComponent<ScrollRect>() != null) info["type"] = "ScrollView";
            else info["type"] = "Panel";

            list.Add(info);
        }
    }
}

