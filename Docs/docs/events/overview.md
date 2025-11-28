---
sidebar_position: 3
---

# WebSocket events

Connect to `ws://localhost:17891/mcp/events` to receive real-time updates.

## Event Format

```json
{
  "type": "event",
  "event": "EVENT_NAME",
  "data": { ... },
  "timestamp": "2023-10-01T12:00:00Z"
}
```

## Supported Events

### `log_message`
Emitted when a log is written to the Unity Console.

```json
{
  "type": "Log|Warning|Error",
  "message": "Hello World",
  "stackTrace": "..."
}
```

### `playmode_state_changed`
Emitted when entering or exiting play mode.

```json
{
  "isPlaying": true,
  "isPaused": false
}
```

### `scene_changed`
Emitted when the active scene changes.

```json
{
  "name": "NewScene",
  "path": "Assets/Scenes/NewScene.unity"
}
```
