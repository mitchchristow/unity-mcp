---
sidebar_position: 1
---

# Real-time Events

The Unity MCP Server broadcasts real-time events via WebSocket, allowing AI agents to react to changes in the Unity Editor without polling.

## How It Works

```
┌─────────────┐                    ┌─────────────┐                    ┌─────────────┐
│   Unity     │  ── WebSocket ──►  │   Gateway   │  ── Resources ──►  │   IDE/AI    │
│   Editor    │     (events)       │  (Node.js)  │   (event buffer)   │  (Cursor)   │
└─────────────┘                    └─────────────┘                    └─────────────┘
```

1. **Unity** detects changes (selection, play mode, console logs, etc.)
2. **EventBroadcaster** sends events via WebSocket (port 17891)
3. **Gateway** receives events and stores them in a buffer
4. **AI** reads events via the `unity://events/recent` resource

## Available Events

| Event | Description | When Fired |
|-------|-------------|------------|
| `scene.object_created` | A new GameObject was created | Hierarchy change |
| `scene.object_deleted` | A GameObject was deleted | Hierarchy change |
| `scene.selection_changed` | Editor selection changed | User selects objects |
| `scene.opened` | A scene was opened | Scene load |
| `scene.closed` | A scene was closed | Scene unload |
| `scene.saved` | A scene was saved | File save |
| `playmode.changed` | Play mode state changed | Play/Stop/Pause |
| `console.log` | Message logged to console | Debug.Log, errors, etc. |
| `scripts.compilation_started` | Script compilation began | Code changes |
| `scripts.compilation_finished` | Script compilation ended | Compilation complete |
| `editor.undo_redo` | Undo or redo performed | Ctrl+Z / Ctrl+Y |

## Event Payloads

### scene.object_created

```json
{
  "event": "scene.object_created",
  "data": {
    "id": -12345,
    "name": "MyCube",
    "path": "Environment/MyCube",
    "parentId": -67890
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### scene.object_deleted

```json
{
  "event": "scene.object_deleted",
  "data": {
    "id": -12345
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### scene.selection_changed

```json
{
  "event": "scene.selection_changed",
  "data": {
    "count": 2,
    "objects": [
      { "id": -12345, "name": "Player", "path": "Characters/Player" },
      { "id": -67890, "name": "Enemy", "path": "Characters/Enemy" }
    ]
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### playmode.changed

```json
{
  "event": "playmode.changed",
  "data": {
    "state": "playing",
    "isPlaying": true,
    "isPaused": false
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

States: `edit`, `exiting_edit`, `playing`, `exiting_play`

### console.log

```json
{
  "event": "console.log",
  "data": {
    "message": "Player spawned at position (0, 1, 0)",
    "stackTrace": "",
    "type": "info"
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

Types: `info`, `warning`, `error`, `exception`, `assert`

### scripts.compilation_started / finished

```json
{
  "event": "scripts.compilation_started",
  "data": {
    "timestamp": "2024-01-15T10:30:00.000Z"
  }
}
```

## Accessing Events

### Via MCP Resources

The gateway buffers the last 100 events. Access them via resources:

| Resource | Description |
|----------|-------------|
| `unity://events/recent` | Recent events from the buffer |
| `unity://events/types` | List of all event types with payload schemas |
| `unity://events/status` | WebSocket connection status |

### Via WebSocket (Direct)

Connect directly to Unity's WebSocket server:

```javascript
const ws = new WebSocket("ws://localhost:17891/mcp/events");

ws.on("message", (data) => {
  const event = JSON.parse(data);
  console.log(`Event: ${event.event}`, event.data);
});
```

### Via RPC Methods

```json
{
  "jsonrpc": "2.0",
  "method": "unity.get_recent_events",
  "params": { "limit": 50 },
  "id": 1
}
```

## Use Cases

### 1. Reactive AI Behavior

The AI can monitor events to provide contextual assistance:

> **AI sees**: `scene.object_created` for "Enemy" prefab
> **AI suggests**: "I see you added an Enemy. Would you like me to add a health component and patrol behavior?"

### 2. Error Monitoring

The AI can watch for console errors:

> **AI sees**: `console.log` with type "error"
> **AI responds**: "I noticed a NullReferenceException in PlayerController.cs line 42. Let me help you fix that."

### 3. Compilation Feedback

The AI knows when scripts are compiling:

> **AI sees**: `scripts.compilation_started`
> **AI says**: "Compiling scripts... I'll wait for compilation to finish before making more changes."

### 4. Selection Context

The AI knows what you're working on:

> **AI sees**: `scene.selection_changed` with "MainCamera"
> **AI provides**: Camera-specific suggestions and tools

## Configuration

The event system is enabled by default. Events are:

- **Buffered**: Last 100 events kept in gateway memory
- **Non-blocking**: Event broadcasting doesn't slow down Unity
- **Lightweight**: Minimal payload sizes

## Troubleshooting

### Events Not Appearing

1. Check WebSocket connection: Read `unity://events/status`
2. Verify Unity is running with MCP server started
3. Check Unity Console for `[MCP Events] Event broadcaster initialized`

### WebSocket Disconnected

The gateway auto-reconnects every 5 seconds. Check:
- Unity is running
- Port 17891 is not blocked
- No firewall issues
