# SimpleCore

A high-performance, lightweight foundational library for Unity projects. SimpleCore provides essential systems and utilities for game development, including identifiers, input handling, save/load mechanics, asset management, timing, and storage solutions.

## Features

- **Identifiers**: Type-safe, performant ID systems (8, 16, 32, 64, 128, 256, 512-bit variants, Snowflake128, HashIdentifier)
- **Input System**: Wrapper layer for Unity's InputSystem with rebinding support and input device management
- **Example UI helpers**: Shared runtime Unity UI panel builder used by package example scenes
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
- **Unity.ugui** - For shared runtime example UI helpers
- **Unity.ResourceManager** - Dependency of Addressables

### C# Features

- Requires `.NET Standard 2.1` or higher (C# 8.0+)
- Unsafe code allowed (ref structs and pointers used for performance)

## Usage Examples

### Identifiers

Use type-safe identifiers for objects, entities, or items:

```csharp
// Create identifiers of various sizes
ID32 itemId = new ID32(12345);
ID64 playerId = new ID64(9876543210);
Snowflake128 uniqueId = Snowflake128.New();

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

Manage input display names and rebinding:

```csharp
// Initialize input system once at startup
InputAPI.Initialize();

// Get the display name of the current binding for an action
[SerializeField] private InputActionReference jumpActionRef;

string displayName = InputAPI.GetBindingDisplayName(jumpActionRef);
Debug.Log($"Jump is bound to: {displayName}");

// Start interactive rebind (keyboard only)
bool rebindStarted = InputAPI.Rebind(jumpActionRef, InputDeviceType.Keyboard);
```

### Example Runtime UI

Package examples use `ExampleRuntimePanel` to create lightweight uGUI controls during Play Mode:

```csharp
ExampleRuntimePanel panel = ExampleRuntimePanel.Create(
    "Example",
    "Runtime controls for the package example.",
    new Vector2(32f, 0f));

Button runButton = panel.AddButton("Run Example");
runButton.onClick.AddListener(RunExample);
```

Use the overload with `Vector2 panelAnchoredPosition` when a scene needs more than one panel.

### Save/Load System

Implement saveable objects with custom file formats. Define a save file type and a class that implements `ISaveData<T>`:

```csharp
using System;

[Serializable]
public sealed class PlayerSaveFile : SaveFileBase
{
    public int level;
    public float health;
}

public sealed class PlayerData : ISaveData<PlayerSaveFile>
{
    public int Level { get; set; }
    public float Health { get; set; }

    public void CollectData() { /* optional pre-save preparation */ }

    public PlayerSaveFile BuildSaveFile()
        => new PlayerSaveFile { level = Level, health = Health };

    public void ParseSaveFile(PlayerSaveFile saveFile)
    {
        Level = saveFile.level;
        Health = saveFile.health;
    }
}

// Usage
PlayerData player = new PlayerData { Level = 5, Health = 100f };
PlayerSaveFile saveFile = player.SaveAs();
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
public sealed class SkillDatabase : AddressableDatabase<SkillDatabase, SkillScriptableObject>
{
    protected override string AddressableLabel => "Skills";
}

// Assets load lazily on first access; or preload async:
SkillDatabase.Instance.LoadAsync((entries) =>
{
    for (int skillIndex = 0; skillIndex < entries.Count; skillIndex++)
    {
        SkillScriptableObject skill = entries[skillIndex];
        Debug.Log($"Loaded skill: {skill.name}");
    }
});

// Query by exact concrete type (fast binary search)
FireSkill fire = SkillDatabase.GetExact<FireSkill>();

// Query by base/abstract type
SkillScriptableObject any = SkillDatabase.GetAny<SkillScriptableObject>();

// Get all assets of a type (returns a pooled read-only list)
using ROListAccess<SkillScriptableObject> all = SkillDatabase.GetAll<SkillScriptableObject>();
for (int skillIndex = 0; skillIndex < all.List.Count; skillIndex++)
{
    SkillScriptableObject skill = all.List[skillIndex];
    Debug.Log(skill.name);
}
```

### Operation Results

Use operation results for chainable error handling:

```csharp
// Create success result
OperationResult success = OperationResult.Success(
    systemCode: 1,
    resultCode: 0,
    userCode: 100
);

// Create error result
OperationResult error = OperationResult.Error(
    systemCode: 1,
    resultCode: 1,
    userCode: 200
);

// Check results
if (OperationResult.IsSuccess(success))
{
    Debug.Log("Operation succeeded");
}

// AreSimilar compares systemCode + resultCode, ignoring userCode.
// Useful for matching the same operation type fired from different callers.
OperationResult successA = OperationResult.Success(1, 0, userCode: 100);
OperationResult successB = OperationResult.Success(1, 0, userCode: 200);

if (OperationResult.AreSimilar(successA, successB))
{
    Debug.Log("Same operation type, different caller");
}
```

### Auto-Generation

Mark ScriptableObjects for automatic generation:

```csharp
[AutoCreate("Skills/My Skill", "Skills")]
public sealed class MySkill : ScriptableObject
{
    public string skillName;
    public float cooldown;
}

// File is automatically created in Assets/Generated/Skills/My Skill/ at build time
```
Concrete auto-created ScriptableObject types must live in a matching `.cs` file that Unity can resolve as a `MonoScript`. Editor automation skips types that cannot be serialized as stable Unity assets and logs an error instead of repeatedly creating broken files under `Assets/Generated/`.

### Math Extensions

Perform efficient vector rotations:

```csharp
using Systems.SimpleCore.Utility;

float2 vec = new float2(1, 0);
float2 rotatedVec = MathExtensions.Rotate(vec, math.PI / 4);

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
- **Examples/** - Runtime Unity UI helpers shared by package example scenes
- **Tests/EditMode/** - Editor-only Unity Test Framework coverage for core runtime APIs

### Key Patterns

- **Ref Structs**: Used for list access (ROListAccess, RWListAccess) to ensure efficient memory usage
- **Burst Compilation**: Identifier types are Burst-compiled for performance
- **Layout Optimization**: Explicit field layout used in operations and identifiers for compact memory usage
- **Static Instances**: Databases and timing systems use static singletons for global access

### Tests

SimpleCore includes an Editor-only test assembly at `Tests/EditMode/SimpleCore.Tests.asmdef`.
It references Unity Test Framework and has `includePlatforms` set to `Editor`, so the tests are not compiled into player builds.
Run them from Unity Test Runner under Edit Mode when validating package changes.

## License

See [LICENSE.md](LICENSE.md) for details.
