#if UNITY_EDITOR
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityMcp.Editor.MCP.Rpc.Controllers
{
    /// <summary>
    /// Controller for tilemap operations in Unity.
    /// Handles tilemap creation, tile placement, and grid management.
    /// Perfect for 2D games like turn-based strategy.
    /// </summary>
    public static class TilemapController
    {
        public static void Register()
        {
            // Tilemap listing (for resource)
            JsonRpcDispatcher.RegisterMethod("unity.list_tilemaps", ListTilemaps);
            JsonRpcDispatcher.RegisterMethod("unity.list_tiles", ListTiles);
            
            // Tilemap creation and management
            JsonRpcDispatcher.RegisterMethod("unity.create_tilemap", CreateTilemap);
            JsonRpcDispatcher.RegisterMethod("unity.get_tilemap_info", GetTilemapInfo);
            
            // Tile operations
            JsonRpcDispatcher.RegisterMethod("unity.set_tile", SetTile);
            JsonRpcDispatcher.RegisterMethod("unity.get_tile", GetTile);
            JsonRpcDispatcher.RegisterMethod("unity.clear_tile", ClearTile);
            JsonRpcDispatcher.RegisterMethod("unity.fill_tiles", FillTiles);
            JsonRpcDispatcher.RegisterMethod("unity.box_fill_tiles", BoxFillTiles);
            JsonRpcDispatcher.RegisterMethod("unity.clear_all_tiles", ClearAllTiles);
            
            // Grid operations
            JsonRpcDispatcher.RegisterMethod("unity.cell_to_world", CellToWorld);
            JsonRpcDispatcher.RegisterMethod("unity.world_to_cell", WorldToCell);
            JsonRpcDispatcher.RegisterMethod("unity.get_tiles_in_bounds", GetTilesInBounds);
        }

        /// <summary>
        /// Lists all tilemaps in the scene.
        /// </summary>
        private static JObject ListTilemaps(JObject p)
        {
            var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var result = new JArray();

            foreach (var tm in tilemaps)
            {
                var bounds = tm.cellBounds;
                result.Add(new JObject
                {
                    ["id"] = tm.gameObject.GetInstanceID(),
                    ["name"] = tm.gameObject.name,
                    ["gridId"] = tm.layoutGrid?.gameObject.GetInstanceID(),
                    ["cellSize"] = new JObject
                    {
                        ["x"] = tm.cellSize.x,
                        ["y"] = tm.cellSize.y,
                        ["z"] = tm.cellSize.z
                    },
                    ["bounds"] = new JObject
                    {
                        ["xMin"] = bounds.xMin,
                        ["yMin"] = bounds.yMin,
                        ["xMax"] = bounds.xMax,
                        ["yMax"] = bounds.yMax,
                        ["size"] = new JObject
                        {
                            ["x"] = bounds.size.x,
                            ["y"] = bounds.size.y
                        }
                    },
                    ["tileCount"] = GetTileCount(tm),
                    ["sortingLayerName"] = tm.GetComponent<TilemapRenderer>()?.sortingLayerName,
                    ["sortingOrder"] = tm.GetComponent<TilemapRenderer>()?.sortingOrder ?? 0
                });
            }

            return new JObject
            {
                ["tilemaps"] = result,
                ["count"] = result.Count
            };
        }

        /// <summary>
        /// Lists all tile assets in the project.
        /// </summary>
        private static JObject ListTiles(JObject p)
        {
            string folder = p["folder"]?.Value<string>() ?? "Assets";
            int limit = p["limit"]?.Value<int>() ?? 100;

            var guids = AssetDatabase.FindAssets("t:TileBase", new[] { folder });
            var tiles = new JArray();
            int count = 0;

            foreach (var guid in guids)
            {
                if (count >= limit) break;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                
                if (tile != null)
                {
                    var tileInfo = new JObject
                    {
                        ["path"] = path,
                        ["name"] = tile.name,
                        ["type"] = tile.GetType().Name
                    };

                    // Add sprite info if it's a standard Tile
                    if (tile is Tile standardTile && standardTile.sprite != null)
                    {
                        tileInfo["sprite"] = standardTile.sprite.name;
                        tileInfo["spritePath"] = AssetDatabase.GetAssetPath(standardTile.sprite);
                    }

                    tiles.Add(tileInfo);
                    count++;
                }
            }

            return new JObject
            {
                ["tiles"] = tiles,
                ["count"] = tiles.Count,
                ["folder"] = folder
            };
        }

        /// <summary>
        /// Creates a new tilemap with a grid.
        /// </summary>
        private static JObject CreateTilemap(JObject p)
        {
            string name = p["name"]?.Value<string>() ?? "Tilemap";
            bool createGrid = p["createGrid"]?.Value<bool>() ?? true;
            int? parentId = p["parentId"]?.Value<int>();

            GameObject gridGo = null;
            Grid grid = null;

            if (createGrid)
            {
                string gridName = p["gridName"]?.Value<string>() ?? "Grid";
                gridGo = new GameObject(gridName);
                grid = gridGo.AddComponent<Grid>();

                // Set grid cell size
                if (p["cellSize"] != null)
                {
                    grid.cellSize = new Vector3(
                        p["cellSize"]["x"]?.Value<float>() ?? 1,
                        p["cellSize"]["y"]?.Value<float>() ?? 1,
                        p["cellSize"]["z"]?.Value<float>() ?? 0
                    );
                }

                // Set grid cell layout
                if (p["cellLayout"] != null)
                {
                    grid.cellLayout = p["cellLayout"].Value<string>() switch
                    {
                        "Rectangle" => GridLayout.CellLayout.Rectangle,
                        "Hexagon" => GridLayout.CellLayout.Hexagon,
                        "Isometric" => GridLayout.CellLayout.Isometric,
                        "IsometricZAsY" => GridLayout.CellLayout.IsometricZAsY,
                        _ => GridLayout.CellLayout.Rectangle
                    };
                }

                // Set parent if specified
                if (parentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(parentId.Value) as GameObject;
                    if (parent != null)
                    {
                        gridGo.transform.SetParent(parent.transform);
                    }
                }

                Undo.RegisterCreatedObjectUndo(gridGo, "Create Grid");
            }
            else if (parentId.HasValue)
            {
                gridGo = EditorUtility.InstanceIDToObject(parentId.Value) as GameObject;
                grid = gridGo?.GetComponentInParent<Grid>();
            }

            // Create tilemap
            var tilemapGo = new GameObject(name);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            var renderer = tilemapGo.AddComponent<TilemapRenderer>();

            // Parent to grid
            if (gridGo != null)
            {
                tilemapGo.transform.SetParent(gridGo.transform);
            }

            // Set sorting
            if (p["sortingLayerName"] != null)
                renderer.sortingLayerName = p["sortingLayerName"].Value<string>();
            if (p["sortingOrder"] != null)
                renderer.sortingOrder = p["sortingOrder"].Value<int>();

            Undo.RegisterCreatedObjectUndo(tilemapGo, "Create Tilemap");

            return new JObject
            {
                ["tilemapId"] = tilemapGo.GetInstanceID(),
                ["tilemapName"] = tilemapGo.name,
                ["gridId"] = gridGo?.GetInstanceID(),
                ["gridName"] = gridGo?.name
            };
        }

        /// <summary>
        /// Gets detailed information about a tilemap.
        /// </summary>
        private static JObject GetTilemapInfo(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            var renderer = go.GetComponent<TilemapRenderer>();
            var grid = tilemap.layoutGrid;
            var bounds = tilemap.cellBounds;

            return new JObject
            {
                ["id"] = id,
                ["name"] = go.name,
                ["tileCount"] = GetTileCount(tilemap),
                ["cellSize"] = new JObject
                {
                    ["x"] = tilemap.cellSize.x,
                    ["y"] = tilemap.cellSize.y,
                    ["z"] = tilemap.cellSize.z
                },
                ["bounds"] = new JObject
                {
                    ["xMin"] = bounds.xMin,
                    ["yMin"] = bounds.yMin,
                    ["zMin"] = bounds.zMin,
                    ["xMax"] = bounds.xMax,
                    ["yMax"] = bounds.yMax,
                    ["zMax"] = bounds.zMax
                },
                ["origin"] = new JObject
                {
                    ["x"] = tilemap.origin.x,
                    ["y"] = tilemap.origin.y,
                    ["z"] = tilemap.origin.z
                },
                ["grid"] = grid != null ? new JObject
                {
                    ["id"] = grid.gameObject.GetInstanceID(),
                    ["name"] = grid.gameObject.name,
                    ["cellLayout"] = grid.cellLayout.ToString(),
                    ["cellSize"] = new JObject
                    {
                        ["x"] = grid.cellSize.x,
                        ["y"] = grid.cellSize.y,
                        ["z"] = grid.cellSize.z
                    }
                } : null,
                ["renderer"] = renderer != null ? new JObject
                {
                    ["sortingLayerName"] = renderer.sortingLayerName,
                    ["sortingOrder"] = renderer.sortingOrder,
                    ["mode"] = renderer.mode.ToString()
                } : null
            };
        }

        /// <summary>
        /// Sets a tile at a specific cell position.
        /// </summary>
        private static JObject SetTile(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            string tilePath = p["tilePath"]?.Value<string>() ?? throw new System.ArgumentException("tilePath is required");
            
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
            if (tile == null)
                throw new System.ArgumentException($"Tile not found at path: {tilePath}");

            int x = p["x"]?.Value<int>() ?? 0;
            int y = p["y"]?.Value<int>() ?? 0;
            int z = p["z"]?.Value<int>() ?? 0;

            var position = new Vector3Int(x, y, z);
            
            Undo.RecordObject(tilemap, "Set Tile");
            tilemap.SetTile(position, tile);

            return new JObject
            {
                ["ok"] = true,
                ["position"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z },
                ["tileName"] = tile.name
            };
        }

        /// <summary>
        /// Gets the tile at a specific cell position.
        /// </summary>
        private static JObject GetTile(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            int x = p["x"]?.Value<int>() ?? 0;
            int y = p["y"]?.Value<int>() ?? 0;
            int z = p["z"]?.Value<int>() ?? 0;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            var position = new Vector3Int(x, y, z);
            var tile = tilemap.GetTile(position);

            if (tile == null)
            {
                return new JObject
                {
                    ["hasTile"] = false,
                    ["position"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z }
                };
            }

            return new JObject
            {
                ["hasTile"] = true,
                ["position"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z },
                ["tileName"] = tile.name,
                ["tilePath"] = AssetDatabase.GetAssetPath(tile),
                ["tileType"] = tile.GetType().Name
            };
        }

        /// <summary>
        /// Clears a tile at a specific cell position.
        /// </summary>
        private static JObject ClearTile(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            int x = p["x"]?.Value<int>() ?? 0;
            int y = p["y"]?.Value<int>() ?? 0;
            int z = p["z"]?.Value<int>() ?? 0;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            var position = new Vector3Int(x, y, z);
            
            Undo.RecordObject(tilemap, "Clear Tile");
            tilemap.SetTile(position, null);

            return new JObject
            {
                ["ok"] = true,
                ["position"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z }
            };
        }

        /// <summary>
        /// Fills a region with tiles.
        /// </summary>
        private static JObject FillTiles(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            string tilePath = p["tilePath"]?.Value<string>() ?? throw new System.ArgumentException("tilePath is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
            if (tile == null)
                throw new System.ArgumentException($"Tile not found at path: {tilePath}");

            int startX = p["startX"]?.Value<int>() ?? 0;
            int startY = p["startY"]?.Value<int>() ?? 0;
            int endX = p["endX"]?.Value<int>() ?? startX;
            int endY = p["endY"]?.Value<int>() ?? startY;
            int z = p["z"]?.Value<int>() ?? 0;

            Undo.RecordObject(tilemap, "Fill Tiles");

            int count = 0;
            for (int x = Mathf.Min(startX, endX); x <= Mathf.Max(startX, endX); x++)
            {
                for (int y = Mathf.Min(startY, endY); y <= Mathf.Max(startY, endY); y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, z), tile);
                    count++;
                }
            }

            return new JObject
            {
                ["ok"] = true,
                ["tilesPlaced"] = count,
                ["tileName"] = tile.name
            };
        }

        /// <summary>
        /// Box fills tiles using Unity's BoxFill method.
        /// </summary>
        private static JObject BoxFillTiles(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            string tilePath = p["tilePath"]?.Value<string>() ?? throw new System.ArgumentException("tilePath is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
            if (tile == null)
                throw new System.ArgumentException($"Tile not found at path: {tilePath}");

            int startX = p["startX"]?.Value<int>() ?? 0;
            int startY = p["startY"]?.Value<int>() ?? 0;
            int endX = p["endX"]?.Value<int>() ?? startX;
            int endY = p["endY"]?.Value<int>() ?? startY;

            Undo.RecordObject(tilemap, "Box Fill Tiles");
            tilemap.BoxFill(
                new Vector3Int(startX, startY, 0),
                tile,
                Mathf.Min(startX, endX),
                Mathf.Min(startY, endY),
                Mathf.Max(startX, endX),
                Mathf.Max(startY, endY)
            );

            return new JObject
            {
                ["ok"] = true,
                ["tileName"] = tile.name
            };
        }

        /// <summary>
        /// Clears all tiles from a tilemap.
        /// </summary>
        private static JObject ClearAllTiles(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            int tileCount = GetTileCount(tilemap);
            
            Undo.RecordObject(tilemap, "Clear All Tiles");
            tilemap.ClearAllTiles();

            return new JObject
            {
                ["ok"] = true,
                ["tilesCleared"] = tileCount
            };
        }

        /// <summary>
        /// Converts cell coordinates to world position.
        /// </summary>
        private static JObject CellToWorld(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            int x = p["x"]?.Value<int>() ?? 0;
            int y = p["y"]?.Value<int>() ?? 0;
            int z = p["z"]?.Value<int>() ?? 0;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            var grid = tilemap?.layoutGrid ?? go.GetComponent<Grid>();
            
            if (grid == null)
                throw new System.ArgumentException("No Grid found");

            var worldPos = grid.CellToWorld(new Vector3Int(x, y, z));

            return new JObject
            {
                ["cell"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z },
                ["world"] = new JObject { ["x"] = worldPos.x, ["y"] = worldPos.y, ["z"] = worldPos.z }
            };
        }

        /// <summary>
        /// Converts world position to cell coordinates.
        /// </summary>
        private static JObject WorldToCell(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            float x = p["x"]?.Value<float>() ?? 0;
            float y = p["y"]?.Value<float>() ?? 0;
            float z = p["z"]?.Value<float>() ?? 0;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            var grid = tilemap?.layoutGrid ?? go.GetComponent<Grid>();
            
            if (grid == null)
                throw new System.ArgumentException("No Grid found");

            var cellPos = grid.WorldToCell(new Vector3(x, y, z));

            return new JObject
            {
                ["world"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z },
                ["cell"] = new JObject { ["x"] = cellPos.x, ["y"] = cellPos.y, ["z"] = cellPos.z }
            };
        }

        /// <summary>
        /// Gets all tiles within bounds.
        /// </summary>
        private static JObject GetTilesInBounds(JObject p)
        {
            int id = p["id"]?.Value<int>() ?? throw new System.ArgumentException("id is required");
            int startX = p["startX"]?.Value<int>() ?? 0;
            int startY = p["startY"]?.Value<int>() ?? 0;
            int endX = p["endX"]?.Value<int>() ?? 10;
            int endY = p["endY"]?.Value<int>() ?? 10;
            int z = p["z"]?.Value<int>() ?? 0;

            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go == null)
                throw new System.ArgumentException($"GameObject not found: {id}");

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
                throw new System.ArgumentException("GameObject does not have a Tilemap component");

            var tiles = new JArray();
            
            for (int x = Mathf.Min(startX, endX); x <= Mathf.Max(startX, endX); x++)
            {
                for (int y = Mathf.Min(startY, endY); y <= Mathf.Max(startY, endY); y++)
                {
                    var pos = new Vector3Int(x, y, z);
                    var tile = tilemap.GetTile(pos);
                    
                    if (tile != null)
                    {
                        tiles.Add(new JObject
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["z"] = z,
                            ["tileName"] = tile.name,
                            ["tilePath"] = AssetDatabase.GetAssetPath(tile)
                        });
                    }
                }
            }

            return new JObject
            {
                ["tiles"] = tiles,
                ["count"] = tiles.Count,
                ["bounds"] = new JObject
                {
                    ["startX"] = startX,
                    ["startY"] = startY,
                    ["endX"] = endX,
                    ["endY"] = endY
                }
            };
        }

        // Helper to count tiles in a tilemap
        private static int GetTileCount(Tilemap tilemap)
        {
            int count = 0;
            var bounds = tilemap.cellBounds;
            
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
                    count++;
            }
            
            return count;
        }
    }
}
#endif

