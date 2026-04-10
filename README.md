# SimpleCore

A high-performance, lightweight foundational library for Unity projects. SimpleCore provides essential systems and utilities for game development, including identifiers, input handling, save/load mechanics, asset management, timing, and storage solutions.

## Features

- **Identifiers**: Type-safe, performant ID systems (8, 16, 32, 64, 128, 256, 512-bit variants, Snowflake128, HashIdentifier)
- **Input System**: Wrapper layer for Unity's InputSystem with rebinding support and input device management
- **Save/Load System**: Generic save file abstraction supporting multiple file formats with upgrade/downgrade transitions
- **Asset Storage**: Addressable asset databases with lazy-loading and ID-based lookups
- **Tick System**: Global timing system for frame-rate independent updates
- **Operations**: Lightweight operation result type for error handling and status reporting
- **Automation**: Attributes for auto-generating and registering ScriptableObjects at build time
- **Utilities**: Math extensions for vector rotation and other common operations

## Requirements

### Dependencies

- **Unity** 2022.1+
- **Unity.Addressables** - For asset management and addressable asset loading
- **Unity.Burst** - Performance optimization for identifier types
- **Unity.Collections** - For NativeCollections support
- **Unity.Mathematics** - For high-performance math operations
- **Unity.InputSystem** - For input handling
- **Unity.ResourceManager** - Dependency of Addressables

### C# Features

- Requires `.NET Standard 2.1` or higher (C# 8.0+)
- Unsafe code allowed (ref structs and pointers used for performance)

## Usage Examples

### Identifiers

Use type-safe identifiers for objects, entities, or items:

```csharp
// Create identifiers of various sizes
var itemId = new ID32(12345);
var playerId = new ID64(9876543210);
var uniqueId = new Snowflake128();

// Check if an identifier was created
if (itemId.IsCreated)
{
    uint value = itemId.Value;
    Debug.Log($"Item: {itemId}");
}

// Compare identifiers
bool isSame = itemId.Equals(new ID32(12345));
```

### Input System

Manage keyboard, gamepad, and input rebinding:

```csharp
// Initialize input system
InputAPI.Initialize();

// Get input state
var inputInfo = InputAPI.GetInputInfo("Jump");
if (inputInfo.IsPressed)
{
    // Handle jump action
}

// Rebind input
InputAPI.OnBindingChangeCompleted += (info) =>
{
    Debug.Log($"Binding changed: {info.ActionName}");
};

InputAPI.StartRebind("Jump", allowedDevices: new[] { InputDeviceType.Keyboard });
```

### Save/Load System

Implement saveable objects with custom file formats:

```csharp
public class PlayerData : ISaveData<JsonSaveFile>
{
    public int Level { get; set; }
    public float Health { get; set; }

    public void CollectData()
    {
        // Gather data before saving
    }

    public JsonSaveFile BuildSaveFile()
    {
        // Create save file from collected data
        return new JsonSaveFile { /* ... */ };
    }

    public void ParseSaveFile(JsonSaveFile saveFile)
    {
        // Load data from file
    }
}

// Usage
var player = new PlayerData();
var saveFile = player.SaveAs();
player.LoadAs(saveFile);
```

### Tick System

Use the global tick system for fixed-timestep updates:

```csharp
// Register a handler that fires every frame
TickSystem.RegisterHandler((deltaTime) =>
{
    Debug.Log($"Tick: {deltaTime}s");
});

// Or use fixed intervals (turn-based games)
TickSystem.Instance.TickInterval = 0.5f;

// Control time
TickSystem.Instance.CanTimePass = false; // Pause updates
TickSystem.Instance.AutomaticTick = false; // Manual tick control
```

### Asset Databases

Create type-safe databases of addressable assets:

```csharp
public class SkillDatabase : AddressableDatabase<SkillDatabase, SkillScriptableObject>
{
    protected override string AddressableLabel => "Skills";
}

// Usage
SkillDatabase.Instance.LoadAsync((entries) =>
{
    foreach (var entry in entries)
    {
        Debug.Log($"Loaded skill: {entry.Asset.name}");
    }
});

// Get specific asset
var skill = SkillDatabase.Instance.Get(new ID32(skillId));
```

### Operation Results

Use operation results for chainable error handling:

```csharp
// Create success result
var success = OperationResult.Success(
    systemCode: 1,
    resultCode: 0,
    userCode: 100
);

// Create error result
var error = OperationResult.Error(
    systemCode: 1,
    resultCode: 1,
    userCode: 200
);

// Check results
if (OperationResult.IsSuccess(success))
{
    Debug.Log("Operation succeeded");
}

if (OperationResult.AreSimilar(success, error))
{
    Debug.Log("Same operation, different result");
}
```

### Auto-Generation

Mark ScriptableObjects for automatic generation:

```csharp
[AutoCreate("Skills/My Skill", "Skills")]
public class MySkill : ScriptableObject
{
    public string skillName;
    public float cooldown;
}

// File is automatically created in Assets/Generated/Skills/ at build time
```

### Math Extensions

Perform efficient vector rotations:

```csharp
using Systems.SimpleCore.Utility;

float2 vec = new float2(1, 0);
float rotatedVec = MathExtensions.Rotate(vec, math.PI / 4);

float3 vec3 = new float3(1, 0, 0);
float3 rotated = MathExtensions.Rotate(vec3, new float3(0, 1, 0), math.PI / 2);
```

## Architecture

### Module Structure

- **Automation/** - Attributes and editor tools for code generation
- **Editor/** - Editor-only utilities and post-processors
- **Identifiers/** - ID types and unique identifier implementations
- **Input/** - Input system wrapper and rebinding utilities
- **Operations/** - Operation result types for status/error handling
- **Saving/** - Save/load interfaces and file abstractions
- **Storage/** - Addressable databases and list access structures
- **Timing/** - Global tick system for updates
- **Utility/** - Helper functions and extensions

### Key Patterns

- **Ref Structs**: Used for list access (ROListAccess, RWListAccess) to ensure efficient memory usage
- **Burst Compilation**: Identifier types are Burst-compiled for performance
- **Layout Optimization**: Explicit field layout used in operations and identifiers for compact memory usage
- **Static Instances**: Databases and timing systems use static singletons for global access

## License

See [LICENSE.md](LICENSE.md) for details.
