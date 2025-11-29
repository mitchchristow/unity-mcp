---
sidebar_position: 1
---

# 2D Game Development

The Unity MCP Server includes comprehensive support for 2D game development, perfect for creating turn-based strategy games, platformers, and other 2D projects.

## Features

- **Sprite Management**: Create and configure sprite objects
- **Tilemaps**: Create grids, place tiles, fill regions
- **2D Physics**: Rigidbody2D, Collider2D, raycasting, overlap queries
- **Sorting Layers**: Control render order for 2D graphics

## Sprite Operations

Use the consolidated `unity_sprite` tool for all sprite operations:

### Create a Sprite Object

```json
{
  "action": "create",
  "name": "Player",
  "spritePath": "Assets/Sprites/player.png",
  "position": { "x": 0, "y": 0, "z": 0 },
  "sortingLayerName": "Characters",
  "sortingOrder": 10
}
```

### Set Sprite on Existing Object

```json
{
  "action": "set_sprite",
  "id": -12345,
  "spritePath": "Assets/Sprites/player_walk.png"
}
```

### Modify Sprite Properties

```json
{
  "action": "set_property",
  "id": -12345,
  "color": { "r": 1, "g": 0.5, "b": 0.5, "a": 1 },
  "flipX": true,
  "sortingOrder": 15
}
```

## Tilemap Operations

Use the consolidated `unity_tilemap` tool for grid-based games:

### Create a Tilemap

```json
{
  "action": "create",
  "name": "Ground",
  "createGrid": true,
  "cellLayout": "Rectangle",
  "cellSize": { "x": 1, "y": 1 },
  "sortingLayerName": "Background",
  "sortingOrder": 0
}
```

For hexagonal strategy games:

```json
{
  "action": "create",
  "name": "HexMap",
  "createGrid": true,
  "cellLayout": "Hexagon",
  "cellSize": { "x": 1, "y": 1 }
}
```

### Place a Tile

```json
{
  "action": "set_tile",
  "id": -12345,
  "tilePath": "Assets/Tiles/grass.asset",
  "x": 5,
  "y": 3
}
```

### Fill a Region

```json
{
  "action": "fill",
  "id": -12345,
  "tilePath": "Assets/Tiles/water.asset",
  "startX": 0,
  "startY": 0,
  "endX": 10,
  "endY": 10
}
```

### Clear All Tiles

```json
{
  "action": "clear_all",
  "id": -12345
}
```

## 2D Physics

### Add Physics Components

Use `unity_physics_2d_body` to add physics:

```json
{
  "action": "add_collider",
  "id": -12345,
  "colliderType": "Box",
  "size": { "x": 1, "y": 1 },
  "isTrigger": false
}
```

```json
{
  "action": "add_rigidbody",
  "id": -12345,
  "bodyType": "Dynamic",
  "mass": 1,
  "gravityScale": 1
}
```

For turn-based games, use Kinematic bodies:

```json
{
  "action": "add_rigidbody",
  "id": -12345,
  "bodyType": "Kinematic",
  "gravityScale": 0
}
```

### Physics Queries

Use `unity_physics_2d_query` for detection:

**Raycast** (for line-of-sight, targeting):

```json
{
  "query": "raycast",
  "origin": { "x": 0, "y": 0 },
  "direction": { "x": 1, "y": 0 },
  "distance": 10
}
```

**Overlap Circle** (for area effects, unit detection):

```json
{
  "query": "overlap_circle",
  "origin": { "x": 5, "y": 5 },
  "radius": 3
}
```

**Overlap Box** (for selection rectangles):

```json
{
  "query": "overlap_box",
  "origin": { "x": 5, "y": 5 },
  "size": { "x": 4, "y": 4 },
  "angle": 0
}
```

## Available Resources

| Resource | Description |
|----------|-------------|
| `unity://sprites` | List all sprite assets |
| `unity://tilemaps` | List all tilemaps in scene |
| `unity://tiles` | List all tile assets |
| `unity://2d/physics` | 2D physics settings |

## Turn-Based Strategy Example

Here's a workflow for creating a basic strategy game grid:

1. **Create the tilemap**:

```
unity_tilemap action=create name="GameBoard" cellLayout="Rectangle"
```

2. **Fill with grass tiles**:

```
unity_tilemap action=fill id=-12345 tilePath="Assets/Tiles/grass.asset" startX=0 startY=0 endX=15 endY=15
```

3. **Add water tiles for obstacles**:

```
unity_tilemap action=fill id=-12345 tilePath="Assets/Tiles/water.asset" startX=5 startY=5 endX=7 endY=7
```

4. **Create a unit**:

```
unity_sprite action=create name="Unit_Infantry" spritePath="Assets/Sprites/infantry.png" position={"x":0,"y":0}
```

5. **Add unit collider for click detection**:

```
unity_physics_2d_body action=add_collider id=-67890 colliderType="Circle" radius=0.5 isTrigger=true
```

## Grid Layouts

The tilemap supports multiple grid layouts:

| Layout | Use Case |
|--------|----------|
| `Rectangle` | Standard grid games, chess-like |
| `Hexagon` | Hex-based strategy (Civilization-style) |
| `Isometric` | Isometric view games |
| `IsometricZAsY` | Isometric with depth sorting |

## Best Practices

1. **Sorting Layers**: Use separate sorting layers for:
   - Background (terrain)
   - Units
   - Effects
   - UI

2. **Triggers vs Colliders**: Use triggers (`isTrigger: true`) for:
   - Click detection
   - Area-of-effect zones
   - Movement boundaries

3. **Kinematic Bodies**: For turn-based games, use Kinematic rigidbodies to control unit movement precisely without physics interference.

4. **Tile Assets**: Create tile assets in Unity first, then reference them by path.

