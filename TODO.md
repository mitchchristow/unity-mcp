# Unity MCP Server - Future Improvements

This document tracks potential enhancements for future development.

## 🚀 Planned (In Progress)

_No items currently in progress._

---

## 📋 Backlog

### Batch Operations
**Priority**: Medium  
**Effort**: Medium

Allow multiple operations in a single call to reduce round-trips.

```javascript
// Example: Create multiple objects in one call
{
  "action": "batch",
  "operations": [
    { "tool": "unity_create_primitive", "args": { "type": "Cube", "name": "Wall1" } },
    { "tool": "unity_create_primitive", "args": { "type": "Cube", "name": "Wall2" } },
    { "tool": "unity_set_transform", "args": { "id": "$0", "position": { "x": 0 } } }
  ]
}
```

**Benefits**:
- Faster bulk operations
- Atomic transactions (all succeed or all fail)
- Reference previous results with `$0`, `$1`, etc.

---

### Resource Subscriptions
**Priority**: Medium  
**Effort**: High

Subscribe to resource changes instead of polling.

```javascript
// Client subscribes
{ "method": "resources/subscribe", "uri": "unity://selection" }

// Server pushes updates automatically
{ "type": "resource_update", "uri": "unity://selection", "data": {...} }
```

**Benefits**:
- Real-time updates without polling
- Reduced server load
- Better responsiveness

**Note**: Partially covered by WebSocket events, but this would be more granular.

---

### Automatic Undo Groups for AI Operations
**Priority**: Low  
**Effort**: Low

Automatically wrap AI operations in undo groups so "Undo" reverts the entire AI action.

```javascript
// Gateway automatically wraps tool sequences
await callUnity("unity.begin_undo_group", { name: "AI: Create Player Character" });
// ... AI performs multiple operations ...
await callUnity("unity.end_undo_group");
```

**Benefits**:
- Single Ctrl+Z undoes entire AI workflow
- Better user experience
- Cleaner undo history

**Implementation**: Track AI "sessions" in gateway and auto-wrap.

---

### Response Caching
**Priority**: Low  
**Effort**: Low

Cache resource responses that don't change frequently.

**Candidates for caching**:
- `unity://tags` - Rarely changes
- `unity://layers` - Rarely changes
- `unity://build/targets` - Static
- `unity://scripts/templates` - Static
- `unity://components/types` - Static

**Implementation**:
- Add TTL (time-to-live) to cached responses
- Invalidate on relevant events (e.g., script compilation)

---

### Input Validation
**Priority**: Low  
**Effort**: Medium

Validate tool inputs in the gateway before sending to Unity.

**Benefits**:
- Faster error feedback
- Better error messages
- Reduced Unity load

**Implementation**:
- Use JSON Schema validation
- Add custom validators for Unity-specific types (Vector3, paths, etc.)

---

### Authentication/Authorization
**Priority**: Low (local-only)  
**Effort**: High

Add optional authentication for remote access scenarios.

**Use Cases**:
- Remote development
- Team collaboration
- CI/CD integration

**Note**: Not needed for local development. Consider if expanding to remote scenarios.

---

## ✅ Completed

- [x] Core Tools (80 tools)
- [x] MCP Resources (42 resources)
- [x] Real-time Events via WebSocket
- [x] 2D Game Development Support
- [x] Scripting Assistance
- [x] Tool consolidation (under 80 limit)
- [x] Background execution optimization
- [x] WebSocket server (raw TCP implementation)
- [x] **MCP Prompts** (8 workflow templates)
  - Pre-defined prompt templates that guide the AI through complex multi-step workflows
  - Implemented prompts:
    - `create_2d_character` - Sprite, physics, movement script
    - `setup_turn_based_system` - Units, turns, actions
    - `create_grid_map` - Grid-based map with tilemap and pathfinding
    - `create_ui_menu` - Canvas, buttons, navigation
    - `setup_unit_stats` - ScriptableObject-based stats system
    - `create_audio_manager` - Singleton audio manager
    - `optimize_scene` - Scene analysis and optimization recommendations
    - `setup_save_system` - JSON-based save/load system
  - Full implementation in `gateway/index.js` with `generatePromptContent()` function
  - Registered with `ListPromptsRequestSchema` and `GetPromptRequestSchema` handlers
- [x] **Progress Notifications** (Real-time operation tracking)
  - Real-time progress updates for long-running operations via WebSocket events
  - `ProgressTracker.cs` - Tracks operation progress with start/update/complete/fail lifecycle
  - `ProgressController.cs` - RPC endpoints for querying progress (`unity.get_progress`, `unity.get_all_progress`)
  - `unity://progress` resource - Access current progress state
  - WebSocket `operation.progress` events broadcast automatically
  - Use cases: Build progress, NavMesh baking, large asset imports, package installation

---

## 💡 Ideas (Not Yet Prioritized)

### Unity Test Runner Integration
- Run unit tests from AI
- Get test results as resource
- Auto-fix failing tests

### Visual Scripting Support
- Create/modify visual scripts
- For users who prefer node-based logic

### Prefab Variant Management
- Create/modify prefab variants
- Override tracking

### Scene Templates
- Save/load scene configurations
- Quick scene setup for common patterns

### Asset Store Integration
- Search Asset Store
- Import free assets

### Collaboration Features
- Lock objects being edited
- Change notifications to team members

---

## Contributing

When implementing a backlog item:
1. Move it to "Planned" section
2. Update status
3. Create branch: `feature/item-name`
4. Update documentation
5. Test thoroughly
6. PR with description


