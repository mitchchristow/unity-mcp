---
sidebar_position: 1
---

# Scripting Assistance

The Unity MCP Server provides powerful scripting assistance for creating C# scripts, monitoring compilation, and exploring Unity's component APIs.

## Features

- **Script Creation**: Generate scripts from templates
- **Compilation Monitoring**: Track errors and warnings in real-time
- **API Exploration**: Discover component properties and methods

## Creating Scripts

Use the `unity_create_script` tool to generate scripts from templates:

### Available Templates

| Template | Description | Use Case |
|----------|-------------|----------|
| `monobehaviour` | Standard MonoBehaviour | Game objects with Update loop |
| `monobehaviour_empty` | Empty MonoBehaviour | Minimal starting point |
| `scriptableobject` | ScriptableObject | Data assets, configurations |
| `editor` | Custom Editor | Inspector customization |
| `editorwindow` | Editor Window | Custom tool windows |
| `singleton` | Singleton pattern | Managers (GameManager, AudioManager) |
| `statemachine` | State machine | AI, player states |
| `interface` | C# Interface | Contracts, abstraction |
| `enum` | C# Enum | State definitions, types |
| `struct` | Serializable Struct | Data containers |
| `class` | Plain C# class | Utilities, data |

### Examples

**Create a MonoBehaviour**:
```json
{
  "path": "Assets/Scripts/Player/PlayerController.cs",
  "className": "PlayerController",
  "template": "monobehaviour",
  "namespace": "MyGame.Player"
}
```

**Create a Singleton GameManager**:
```json
{
  "path": "Assets/Scripts/Managers/GameManager.cs",
  "className": "GameManager",
  "template": "singleton",
  "namespace": "MyGame"
}
```

**Create a ScriptableObject for game data**:
```json
{
  "path": "Assets/Scripts/Data/UnitStats.cs",
  "className": "UnitStats",
  "template": "scriptableobject",
  "namespace": "MyGame.Data"
}
```

**Create a State Machine for AI**:
```json
{
  "path": "Assets/Scripts/AI/EnemyAI.cs",
  "className": "EnemyAI",
  "template": "statemachine"
}
```

**Create with custom content**:
```json
{
  "path": "Assets/Scripts/Utils/MathHelper.cs",
  "content": "using UnityEngine;\n\npublic static class MathHelper\n{\n    public static float Remap(float value, float from1, float to1, float from2, float to2)\n    {\n        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;\n    }\n}"
}
```

## Monitoring Compilation

### Resources

| Resource | Description |
|----------|-------------|
| `unity://scripts/errors` | Current compilation errors |
| `unity://scripts/warnings` | Current compilation warnings |
| `unity://scripts/status` | Whether Unity is compiling |
| `unity://scripts/templates` | Available script templates |

### Example: Check for Errors

Read `unity://scripts/errors` to get:

```json
{
  "errors": [
    {
      "message": "The name 'foo' does not exist in the current context",
      "file": "Assets/Scripts/Test.cs",
      "line": 15,
      "column": 9
    }
  ],
  "count": 1,
  "isCompiling": false
}
```

### AI Workflow

The AI can use these resources to:

1. **Check before running**: "Let me verify there are no compilation errors..."
2. **Wait for compilation**: "Scripts are compiling, I'll wait..."
3. **Offer fixes**: "I see an error on line 15. Let me fix that..."

## Exploring Component APIs

Use `unity_get_component_api` to discover what properties and methods a component has:

### Example: Rigidbody API

```json
{
  "type": "Rigidbody"
}
```

Returns:
```json
{
  "type": "UnityEngine.Rigidbody",
  "baseType": "Component",
  "isComponent": true,
  "properties": [
    { "name": "mass", "type": "float", "canWrite": true },
    { "name": "velocity", "type": "Vector3", "canWrite": true },
    { "name": "angularVelocity", "type": "Vector3", "canWrite": true },
    { "name": "useGravity", "type": "bool", "canWrite": true }
  ],
  "methods": [
    { "name": "AddForce", "returnType": "void", "parameters": [...] },
    { "name": "MovePosition", "returnType": "void", "parameters": [...] }
  ]
}
```

### List Component Types by Category

Read `unity://components/types` to get components organized by category:

```json
{
  "category": "all",
  "types": [
    { "category": "physics", "types": ["Rigidbody", "BoxCollider", "SphereCollider", ...] },
    { "category": "physics2d", "types": ["Rigidbody2D", "BoxCollider2D", ...] },
    { "category": "rendering", "types": ["MeshRenderer", "SpriteRenderer", ...] },
    { "category": "ui", "types": ["Canvas", "Image", "Button", ...] },
    { "category": "audio", "types": ["AudioSource", "AudioListener", ...] }
  ]
}
```

## Use Cases for Turn-Based Strategy

### 1. Create Unit Script

```
"Create a Unit script with health, attack power, and movement range properties"
```

AI creates:
```csharp
public class Unit : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private int attackPower = 10;
    [SerializeField] private int movementRange = 3;
    
    // ... getters and methods
}
```

### 2. Create Turn Manager

```
"Create a TurnManager singleton to handle turn order"
```

AI uses `singleton` template and customizes it.

### 3. Create Unit Data

```
"Create a ScriptableObject for unit type definitions"
```

AI uses `scriptableobject` template:
```csharp
[CreateAssetMenu(fileName = "UnitType", menuName = "ScriptableObjects/UnitType")]
public class UnitType : ScriptableObject
{
    public string unitName;
    public int baseHealth;
    public int baseAttack;
    public int movementRange;
    public Sprite icon;
}
```

### 4. Create State Machine for AI

```
"Create a state machine for enemy AI with Idle, Patrol, and Attack states"
```

AI uses `statemachine` template and adds the specific states.

## Best Practices

1. **Use namespaces**: Organize code with namespaces matching your folder structure
2. **Use ScriptableObjects for data**: Keep game balance data separate from code
3. **Check compilation**: Always verify no errors before testing
4. **Use appropriate templates**: Singletons for managers, state machines for AI

## Template Details

### Singleton Pattern

The `singleton` template includes:
- Static Instance property
- Null check in Awake
- DontDestroyOnLoad for persistence

### State Machine Pattern

The `statemachine` template includes:
- State enum
- OnEnterState / OnExitState hooks
- UpdateState for per-frame logic
- ChangeState method with transitions

