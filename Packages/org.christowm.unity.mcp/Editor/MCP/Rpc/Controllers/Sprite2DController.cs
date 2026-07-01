#if UNITY_EDITOR
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor.MCP;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for 2D sprite operations in Unity.
    /// Handles sprite assets, sprite renderers, and 2D-specific functionality.
    /// </summary>
    public static class Sprite2DController
    {
        public static void Register()
        {
            // Sprite listing (for resource)
            JsonRpcDispatcher.RegisterMethod("unity.list_sprites", ListSprites);
            JsonRpcDispatcher.RegisterMethod("unity.get_sprite_info", GetSpriteInfo);
            
            // Sprite renderer operations
            JsonRpcDispatcher.RegisterMethod("unity.create_sprite_object", CreateSpriteObject);
            JsonRpcDispatcher.RegisterMethod("unity.set_sprite", SetSprite);
            JsonRpcDispatcher.RegisterMethod("unity.set_sprite_renderer_property", SetSpriteRendererProperty);
            JsonRpcDispatcher.RegisterMethod("unity.get_sprite_renderer_info", GetSpriteRendererInfo);
            
            // 2D collider operations
            JsonRpcDispatcher.RegisterMethod("unity.add_2d_collider", Add2DCollider);
            JsonRpcDispatcher.RegisterMethod("unity.add_rigidbody_2d", AddRigidbody2D);
            JsonRpcDispatcher.RegisterMethod("unity.set_rigidbody_2d_property", SetRigidbody2DProperty);
            
            // Sorting layers
            JsonRpcDispatcher.RegisterMethod("unity.set_sorting_layer", SetSortingLayer);
        }

        /// <summary>
        /// Lists all sprite assets in the project.
        /// </summary>
        private static JObject ListSprites(JObject p)
        {
            string folder = p["folder"]?.Value<string>() ?? "Assets";
            int limit = p["limit"]?.Value<int>() ?? 100;

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            var sprites = new JArray();
            int count = 0;

            foreach (var guid in guids)
            {
                if (count >= limit) break;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                
                if (sprite != null)
                {
                    sprites.Add(new JObject
                    {
                        ["path"] = path,
                        ["name"] = sprite.name,
                        ["textureWidth"] = sprite.texture?.width ?? 0,
                        ["textureHeight"] = sprite.texture?.height ?? 0,
                        ["pixelsPerUnit"] = sprite.pixelsPerUnit,
                        ["pivot"] = new JObject
                        {
                            ["x"] = sprite.pivot.x,
                            ["y"] = sprite.pivot.y
                        },
                        ["rect"] = new JObject
                        {
                            ["x"] = sprite.rect.x,
                            ["y"] = sprite.rect.y,
                            ["width"] = sprite.rect.width,
                            ["height"] = sprite.rect.height
                        }
                    });
                    count++;
                }
            }

            return new JObject
            {
                ["sprites"] = sprites,
                ["count"] = sprites.Count,
                ["folder"] = folder
            };
        }

        /// <summary>
        /// Gets detailed information about a specific sprite.
        /// </summary>
        private static JObject GetSpriteInfo(JObject p)
        {
            string path = p["path"]?.Value<string>();
            if (string.IsNullOrEmpty(path))
                throw new System.ArgumentException("path is required");

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new System.ArgumentException($"Sprite not found at path: {path}");

            return new JObject
            {
                ["path"] = path,
                ["name"] = sprite.name,
                ["texture"] = sprite.texture?.name,
                ["textureWidth"] = sprite.texture?.width ?? 0,
                ["textureHeight"] = sprite.texture?.height ?? 0,
                ["pixelsPerUnit"] = sprite.pixelsPerUnit,
                ["pivot"] = new JObject
                {
                    ["x"] = sprite.pivot.x,
                    ["y"] = sprite.pivot.y
                },
                ["rect"] = new JObject
                {
                    ["x"] = sprite.rect.x,
                    ["y"] = sprite.rect.y,
                    ["width"] = sprite.rect.width,
                    ["height"] = sprite.rect.height
                },
                ["border"] = new JObject
                {
                    ["x"] = sprite.border.x,
                    ["y"] = sprite.border.y,
                    ["z"] = sprite.border.z,
                    ["w"] = sprite.border.w
                },
                ["packed"] = sprite.packed
            };
        }

        /// <summary>
        /// Creates a new GameObject with a SpriteRenderer component.
        /// </summary>
        private static JObject CreateSpriteObject(JObject p)
        {
            string name = p["name"]?.Value<string>() ?? "New Sprite";
            string spritePath = p["spritePath"]?.Value<string>();
            var parentIdToken = p["parentId"];
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();

            // Set sprite if path provided
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    sr.sprite = sprite;
                }
            }

            // Set parent if specified
            if (parentIdToken != null)
            {
                var parent = McpObjectReference.ToGameObject(parentIdToken) as GameObject;
                if (parent != null)
                {
                    go.transform.SetParent(parent.transform);
                }
            }

            // Set position if specified
            if (p["position"] != null)
            {
                go.transform.position = new Vector3(
                    p["position"]["x"]?.Value<float>() ?? 0,
                    p["position"]["y"]?.Value<float>() ?? 0,
                    p["position"]["z"]?.Value<float>() ?? 0
                );
            }

            // Set sorting layer and order
            if (p["sortingLayer"] != null)
            {
                sr.sortingLayerName = p["sortingLayer"].Value<string>();
            }
            if (p["sortingOrder"] != null)
            {
                sr.sortingOrder = p["sortingOrder"].Value<int>();
            }

            // Set color if specified
            if (p["color"] != null)
            {
                sr.color = new Color(
                    p["color"]["r"]?.Value<float>() ?? 1,
                    p["color"]["g"]?.Value<float>() ?? 1,
                    p["color"]["b"]?.Value<float>() ?? 1,
                    p["color"]["a"]?.Value<float>() ?? 1
                );
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Sprite Object");

            return new JObject
            {
                ["id"] = McpObjectReference.ToJToken(go),
                ["name"] = go.name,
                ["hasSprite"] = sr.sprite != null
            };
        }

        /// <summary>
        /// Sets the sprite on a SpriteRenderer.
        /// </summary>
        private static JObject SetSprite(JObject p)
        {
            var idToken = p["id"];
            string spritePath = p["spritePath"]?.Value<string>() ?? throw new System.ArgumentException("spritePath is required");

            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {idToken}");

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                throw new System.ArgumentException("GameObject does not have a SpriteRenderer component");

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                throw new System.ArgumentException($"Sprite not found at path: {spritePath}");

            Undo.RecordObject(sr, "Set Sprite");
            sr.sprite = sprite;

            return new JObject
            {
                ["ok"] = true,
                ["spriteName"] = sprite.name
            };
        }

        /// <summary>
        /// Sets properties on a SpriteRenderer.
        /// </summary>
        private static JObject SetSpriteRendererProperty(JObject p)
        {
            var idToken = p["id"];
            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {idToken}");

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                throw new System.ArgumentException("GameObject does not have a SpriteRenderer component");

            Undo.RecordObject(sr, "Set SpriteRenderer Property");

            if (p["color"] != null)
            {
                sr.color = new Color(
                    p["color"]["r"]?.Value<float>() ?? sr.color.r,
                    p["color"]["g"]?.Value<float>() ?? sr.color.g,
                    p["color"]["b"]?.Value<float>() ?? sr.color.b,
                    p["color"]["a"]?.Value<float>() ?? sr.color.a
                );
            }

            if (p["flipX"] != null) sr.flipX = p["flipX"].Value<bool>();
            if (p["flipY"] != null) sr.flipY = p["flipY"].Value<bool>();
            if (p["sortingLayerName"] != null) sr.sortingLayerName = p["sortingLayerName"].Value<string>();
            if (p["sortingOrder"] != null) sr.sortingOrder = p["sortingOrder"].Value<int>();
            if (p["drawMode"] != null)
            {
                sr.drawMode = p["drawMode"].Value<string>() switch
                {
                    "Simple" => SpriteDrawMode.Simple,
                    "Sliced" => SpriteDrawMode.Sliced,
                    "Tiled" => SpriteDrawMode.Tiled,
                    _ => sr.drawMode
                };
            }
            if (p["maskInteraction"] != null)
            {
                sr.maskInteraction = p["maskInteraction"].Value<string>() switch
                {
                    "None" => SpriteMaskInteraction.None,
                    "VisibleInsideMask" => SpriteMaskInteraction.VisibleInsideMask,
                    "VisibleOutsideMask" => SpriteMaskInteraction.VisibleOutsideMask,
                    _ => sr.maskInteraction
                };
            }

            return new JObject
            {
                ["ok"] = true
            };
        }

        /// <summary>
        /// Gets information about a SpriteRenderer.
        /// </summary>
        private static JObject GetSpriteRendererInfo(JObject p)
        {
            var idToken = p["id"];
            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {idToken}");

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                throw new System.ArgumentException("GameObject does not have a SpriteRenderer component");

            return new JObject
            {
                ["id"] = McpObjectReference.ToJToken(go),
                ["name"] = go.name,
                ["sprite"] = sr.sprite?.name,
                ["spritePath"] = sr.sprite != null ? AssetDatabase.GetAssetPath(sr.sprite) : null,
                ["color"] = new JObject
                {
                    ["r"] = sr.color.r,
                    ["g"] = sr.color.g,
                    ["b"] = sr.color.b,
                    ["a"] = sr.color.a
                },
                ["flipX"] = sr.flipX,
                ["flipY"] = sr.flipY,
                ["sortingLayerName"] = sr.sortingLayerName,
                ["sortingLayerID"] = sr.sortingLayerID,
                ["sortingOrder"] = sr.sortingOrder,
                ["drawMode"] = sr.drawMode.ToString(),
                ["maskInteraction"] = sr.maskInteraction.ToString(),
                ["bounds"] = new JObject
                {
                    ["center"] = new JObject { ["x"] = sr.bounds.center.x, ["y"] = sr.bounds.center.y, ["z"] = sr.bounds.center.z },
                    ["size"] = new JObject { ["x"] = sr.bounds.size.x, ["y"] = sr.bounds.size.y, ["z"] = sr.bounds.size.z }
                }
            };
        }

        /// <summary>
        /// Adds a 2D collider to a GameObject.
        /// </summary>
        private static JObject Add2DCollider(JObject p)
        {
            var idToken = p["id"];
            string type = p["type"]?.Value<string>() ?? "Box";

            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {idToken}");

            Collider2D collider = type.ToLower() switch
            {
                "box" => go.AddComponent<BoxCollider2D>(),
                "circle" => go.AddComponent<CircleCollider2D>(),
                "capsule" => go.AddComponent<CapsuleCollider2D>(),
                "polygon" => go.AddComponent<PolygonCollider2D>(),
                "edge" => go.AddComponent<EdgeCollider2D>(),
                "composite" => go.AddComponent<CompositeCollider2D>(),
                _ => throw new System.ArgumentException($"Unknown collider type: {type}")
            };

            Undo.RegisterCreatedObjectUndo(collider, "Add 2D Collider");

            // Set common properties
            if (p["isTrigger"] != null) collider.isTrigger = p["isTrigger"].Value<bool>();
            if (p["offset"] != null)
            {
                collider.offset = new Vector2(
                    p["offset"]["x"]?.Value<float>() ?? 0,
                    p["offset"]["y"]?.Value<float>() ?? 0
                );
            }

            // Type-specific properties
            if (collider is BoxCollider2D box && p["size"] != null)
            {
                box.size = new Vector2(
                    p["size"]["x"]?.Value<float>() ?? 1,
                    p["size"]["y"]?.Value<float>() ?? 1
                );
            }
            else if (collider is CircleCollider2D circle && p["radius"] != null)
            {
                circle.radius = p["radius"].Value<float>();
            }
            else if (collider is CapsuleCollider2D capsule)
            {
                if (p["size"] != null)
                {
                    capsule.size = new Vector2(
                        p["size"]["x"]?.Value<float>() ?? 1,
                        p["size"]["y"]?.Value<float>() ?? 1
                    );
                }
                if (p["direction"] != null)
                {
                    capsule.direction = p["direction"].Value<string>() == "Horizontal" 
                        ? CapsuleDirection2D.Horizontal 
                        : CapsuleDirection2D.Vertical;
                }
            }

            return new JObject
            {
                ["ok"] = true,
                ["colliderType"] = type,
                ["componentId"] = McpObjectReference.ToJToken(collider)
            };
        }

        /// <summary>
        /// Adds a Rigidbody2D to a GameObject.
        /// </summary>
        private static JObject AddRigidbody2D(JObject p)
        {
            var idToken = p["id"];
            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {idToken}");

            var rb = go.AddComponent<Rigidbody2D>();
            Undo.RegisterCreatedObjectUndo(rb, "Add Rigidbody2D");

            // Set properties
            if (p["bodyType"] != null)
            {
                rb.bodyType = p["bodyType"].Value<string>() switch
                {
                    "Dynamic" => RigidbodyType2D.Dynamic,
                    "Kinematic" => RigidbodyType2D.Kinematic,
                    "Static" => RigidbodyType2D.Static,
                    _ => rb.bodyType
                };
            }
            if (p["mass"] != null) rb.mass = p["mass"].Value<float>();
            if (p["linearDamping"] != null) rb.linearDamping = p["linearDamping"].Value<float>();
            if (p["angularDamping"] != null) rb.angularDamping = p["angularDamping"].Value<float>();
            if (p["gravityScale"] != null) rb.gravityScale = p["gravityScale"].Value<float>();
            if (p["freezeRotation"] != null) rb.freezeRotation = p["freezeRotation"].Value<bool>();
            if (p["simulated"] != null) rb.simulated = p["simulated"].Value<bool>();

            return new JObject
            {
                ["ok"] = true,
                ["componentId"] = McpObjectReference.ToJToken(rb)
            };
        }

        /// <summary>
        /// Sets properties on a Rigidbody2D.
        /// </summary>
        private static JObject SetRigidbody2DProperty(JObject p)
        {
            var idToken = p["id"];
            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {idToken}");

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
                throw new System.ArgumentException("GameObject does not have a Rigidbody2D component");

            Undo.RecordObject(rb, "Set Rigidbody2D Property");

            if (p["bodyType"] != null)
            {
                rb.bodyType = p["bodyType"].Value<string>() switch
                {
                    "Dynamic" => RigidbodyType2D.Dynamic,
                    "Kinematic" => RigidbodyType2D.Kinematic,
                    "Static" => RigidbodyType2D.Static,
                    _ => rb.bodyType
                };
            }
            if (p["mass"] != null) rb.mass = p["mass"].Value<float>();
            if (p["linearDamping"] != null) rb.linearDamping = p["linearDamping"].Value<float>();
            if (p["angularDamping"] != null) rb.angularDamping = p["angularDamping"].Value<float>();
            if (p["gravityScale"] != null) rb.gravityScale = p["gravityScale"].Value<float>();
            if (p["freezeRotation"] != null) rb.freezeRotation = p["freezeRotation"].Value<bool>();
            if (p["simulated"] != null) rb.simulated = p["simulated"].Value<bool>();

            return new JObject
            {
                ["ok"] = true
            };
        }

        /// <summary>
        /// Sets the sorting layer on a renderer.
        /// </summary>
        private static JObject SetSortingLayer(JObject p)
        {
            var idToken = p["id"];
            string layerName = p["layerName"]?.Value<string>();
            int? order = p["order"]?.Value<int>();

            var go = McpObjectReference.ToObject(idToken) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {idToken}");

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                throw new System.ArgumentException("GameObject does not have a Renderer component");

            Undo.RecordObject(renderer, "Set Sorting Layer");

            if (!string.IsNullOrEmpty(layerName))
                renderer.sortingLayerName = layerName;
            if (order.HasValue)
                renderer.sortingOrder = order.Value;

            return new JObject
            {
                ["ok"] = true,
                ["sortingLayerName"] = renderer.sortingLayerName,
                ["sortingOrder"] = renderer.sortingOrder
            };
        }
    }
}
#endif

