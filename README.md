<div align="center">
  <h1>SimpleCore</h1>
</div>

# About

SimpleCore is the foundational package of Simple Kit. It provides common building blocks used across other packages:

- Identifiers (numeric IDs, snowflake-style unique IDs, and hash IDs)
- Operation results (lightweight success/error values with optional data)
- Input utilities (Unity Input System helpers for display names, rebinding and duplicate detection)

*For requirements check .asmdef*

# Usage

## Identifiers

SimpleCore exposes several identifier types implemented as value types with lightweight equality/compare semantics and string formatting.

### Non-unique numeric identifiers

SimpleCore provides non-unique numeric identifiers ranging from 8 to 512 bits where the size is always a double of 
previous one (power of 2).

Example of such identifier is `ID128` based on `Unity.Mathematics.uint4` which usage example is provided below.

```csharp
using Systems.SimpleCore.Identifiers;
using Unity.Mathematics;

// Construct from raw value
ID128 id = new ID128(new uint4(0xDEADBEEF, 0xFEEDC0DE, 0x01234567, 0x89ABCDEF));

// Basic usage
bool created = id.IsCreated;            // true
string asText = id.ToString();          // hex string formatted as XXXX....-XXXX....

// Equality and ordering
ID128 other = new ID128(new uint4(1, 2, 3, 4));
bool areEqual = id.Equals(other);       // false
int ordering = id.CompareTo(other);     // IComparable implementation
```

### Snowflake128 (unique time-based identifier)

`Snowflake128` is a 128-bit unique identifier composed of a UTC timestamp and a local cyclic index.

```csharp
using Systems.SimpleCore.Identifiers;

// Create new unique ID
Snowflake128 uid = Snowflake128.New();

// Inspect
bool isCreated = uid.IsCreated;         // true
string text = uid.ToString();           // Ticks-CyclicIndex as hex
string debug = uid.GetDebugTooltipText();

// Compare and sort
Snowflake128 a = Snowflake128.New();
Snowflake128 b = Snowflake128.New();
bool same = a == b;                     // false (very likely)
int cmp = a.CompareTo(b);               // chronological ordering
```

### HashIdentifier (type-based 64-bit hash)

`HashIdentifier` is a deterministic per-process 64-bit hash for a type or instance type.

```csharp
using Systems.SimpleCore.Identifiers;

// From Type
HashIdentifier typeId = HashIdentifier.New(typeof(MyComponent));

// From instance (uses runtime type)
object obj = new MyComponent();
HashIdentifier runtimeTypeId = HashIdentifier.New(obj);

// Basic usage
bool isNonZero = runtimeTypeId.IsCreated;
string hex = runtimeTypeId.ToString();
```

## OperationResult

`OperationResult` is a compact success/error value that carries three codes: `systemCode`, `resultCode`, and an optional `userCode`. It can also be paired with data via `WithData<T>()`.

```csharp
using Systems.SimpleCore.Operations;

// Creating results
OperationResult ok = OperationResult.Success(OperationResult.GENERIC_SPACE, 0);
OperationResult error = OperationResult.Error(OperationResult.GENERIC_SPACE, OperationResult.ERROR_DENIED);

// Checking status
bool isOk = OperationResult.IsSuccess(in ok);       // true
bool isErr = OperationResult.IsError(in error);     // true

// Comparing
bool sameSystem = OperationResult.IsFromSystem(in ok, OperationResult.GENERIC_SPACE);
bool similar = OperationResult.AreSimilar(in error, in OperationResult.Error(OperationResult.GENERIC_SPACE, OperationResult.ERROR_DENIED));

// With data
var withData = ok.WithData(new Vector2Int(10, 20)); // OperationResult<Vector2Int>
OperationResult plain = withData;                   // implicit conversion
bool successBool = withData;                        // implicit bool (true when success)
Vector2Int data = (Vector2Int)withData;             // explicit data extraction
```

Tip: Packages like SimpleSkills define domain-specific helpers (e.g., `SkillOperations.Permitted()`/`Denied()`) on top of `OperationResult`. You can do the same for your domain by wrapping `OperationResult.Success/Error` with your own static helper methods.

## Input API

`InputAPI` provides helpers atop the Unity Input System for binding labels, device validation, rebinding flows and duplicate detection.

### Initialization

```csharp
using Systems.SimpleCore.Input;

// Call once on startup (e.g., in a bootstrap MonoBehaviour Awake)
InputAPI.Initialize();
```

### Getting binding display names

```csharp
using Systems.SimpleCore.Input;
using Systems.SimpleCore.Input.Enums;
using UnityEngine.InputSystem;

public string GetJumpBindingName(InputActionReference jump)
{
    // Auto-picks bindings for allowed devices; joins multiple device bindings with a separator
    return jump.GetBindingDisplayName(preferShortNames: true, ignoreOverrides: false, allowedDevices: InputDeviceType.All);
}

public string GetFirstGamepadBindingName(InputActionReference action)
{
    // Retrieve display name of the first gamepad binding only
    int index;
    if (InputAPI.GetBindingFromAction(action.action, InputDeviceType.Gamepad, ignoreOverrides: false, out index))
        return action.GetBindingDisplayName(index, preferShortNames: true, ignoreOverrides: false, allowedDevices: InputDeviceType.Gamepad);
    return string.Empty;
}
```

### Rebinding an action

The API supports rebinding by default binding, by name (binding id as string `Guid`), or by explicit binding index. Events are exposed globally for UI feedback.

```csharp
using Systems.SimpleCore.Input;
using Systems.SimpleCore.Input.Enums;
using UnityEngine.InputSystem;

// Global UI hooks (optional)
InputAPI.OnBindingChangeStartedGlobalEvent += info => Debug.Log($"Rebind started for {info.action.name}");
InputAPI.OnBindingChangeCompletedGlobalEvent += info => Debug.Log($"Rebind completed: {info.newEffectivePath}");
InputAPI.OnBindingDuplicateFoundGlobalEvent += info => Debug.LogWarning("Duplicate binding detected. Reverted.");
InputAPI.OnBindingChangeCancelledGlobalEvent += info => Debug.Log("Rebind cancelled");
InputAPI.OnBindingResetGlobalEvent += info => Debug.Log("Binding reset to default");

// Rebind the only/default binding (asserts action has a single binding)
bool started = myActionReference.Rebind(InputDeviceType.Keyboard);

// Rebind by binding name (binding ID string)
bool byName = myActionReference.Rebind(bindingName: someBindingIdString, allowedDevices: InputDeviceType.Gamepad);

// Rebind by explicit binding index
bool byIndex = myActionReference.Rebind(bindingIndex: 2, allowedDevices: InputDeviceType.Gamepad);
```

### Resetting to defaults and duplicate checks

```csharp
using Systems.SimpleCore.Input;
using Systems.SimpleCore.Input.Enums;

// Reset a specific binding by name or index
bool resetByName = myActionReference.ResetToDefault(bindingName: someBindingIdString, allowedDevices: InputDeviceType.All);
bool resetByIndex = myActionReference.ResetToDefault(bindingIndex: 0, allowedDevices: InputDeviceType.Keyboard);

// Detect duplicates for a binding before committing UI changes
bool hasDuplicate = myActionReference.SearchForDuplicate(bindingName: someBindingIdString, allCompositeParts: true);
```

### Utilities

```csharp
using Systems.SimpleCore.Input;
using Systems.SimpleCore.Input.Enums;
using UnityEngine.InputSystem;

// Find action and binding index by names
InputAction action;
int bindingIndex;
bool found = myInputActionsAsset.GetActionAndBinding("Gameplay/Jump", someBindingIdString, out action, out bindingIndex);

// Validate a device for a given binding slot
bool isValid = action.IsValidDevice(bindingIndex, InputDeviceType.Keyboard);
```

# Automatic objects and Addressables 

## Auto-creating ScriptableObject(s)

Mark your `ScriptableObject` type with `AutoCreateAttribute` to have an asset auto-created under `Assets/Generated/{Path}/{TypeName}.asset` on editor domain reload. The created asset is also marked Addressable in the group named after `Path` and given the optional `Label`.

```csharp
using Systems.SimpleCore.Automation.Attributes;
using UnityEngine;

[AutoCreate("Core/Databases", label: "Core")] // Assets/Generated/Core/Databases/MyConfig.asset
public sealed class MyConfig : ScriptableObject
{
    public int someValue = 10;
}
```

Notes:
- Auto-creation runs on load via an editor initializer and will create the folder if missing.
- The asset is re-used if it already exists with the same type.
- Auto-created assets are protected from moving/deleting by an editor postprocessor. Keep them under `Assets/Generated/`.

## Auto-registering Addressables

There are two automatic pathways:

- ScriptableObjects marked with `AutoCreateAttribute` are automatically registered as Addressables in the group specified by `Path` and get the optional `Label`.
- Prefabs that contain a component type marked with `AutoAddressableObjectAttribute` will be registered as Addressables when the prefab is saved.

```csharp
using Systems.SimpleCore.Automation.Attributes;
using UnityEngine;

[AutoAddressableObject(path: "Gameplay/Prefabs", label: "Gameplay")] 
public sealed class LootMarker : MonoBehaviour { }
```

When you save a prefab containing `LootMarker`, the editor will add or move the prefab to the `Gameplay/Prefabs` Addressables group and assign the `Gameplay` label. If the group does not exist, it will be created (you may need to add schemas to the new group in Addressables settings).

Warning: if Addressable group does not exist it will be created automatically, but it won't be compiled as you need to set-up schema manually - this is done to ensure user sets up desired mode (either remote or local storage).

## AddressableDatabase

`AddressableDatabase<TSelf, TUnityObject, TLoadType>` provides a simple, lazy-loaded registry of Addressable items by label with fast retrieval APIs.

- Implement a concrete database and specify the Addressables label to load.
- Use the two-type form for plain assets (`TLoadType` = `TUnityObject`), or the three-type form to load `GameObject` prefabs that contain a component of `TUnityObject`.

```csharp
using Systems.SimpleCore.Storage;
using UnityEngine;

// ScriptableObject assets labeled "Core"
public sealed class ConfigDatabase : AddressableDatabase<ConfigDatabase, MyConfig>
{
    protected override string AddressableLabel => "Core";
}

// Prefabs labeled "Gameplay" that contain LootMarker component
public sealed class LootDatabase : AddressableDatabase<LootDatabase, LootMarker, GameObject>
{
    protected override string AddressableLabel => "Gameplay";
}

// Usage (lazy, synchronous load on first access)
MyConfig config = ConfigDatabase.GetExact<MyConfig>();
var allLootMarkers = LootDatabase.GetAll<LootMarker>();
int count = ConfigDatabase.Count;
float progress = LootDatabase.LoadProgress;
```

APIs:
- `GetExact<T>()`: fast lookup by concrete type.
- `GetAbstract<T>()` / `GetAbstractUnsafe<T>()`: first item assignable to `T` (interfaces/abstract).
- `GetAll<T>()` / `GetAllUnsafe<T>()`: all items assignable to `T`.
- `Count`, `LoadProgress` for basic stats.

Note: Prefer `GetExact<T>` whenever possible as it's using binary tree to reduce search time compared to `GetAbstract<T>` which scans every single object.

Note: It's recommended to pre-load database using `StartLoading` method as database will wait to be loaded every time it's accessed and loading may take a while when a lot of objects are present (e.g. database of all items in game). Pre-loading is asynchronous and preferred toward on-spot loading which is fully-synchronous to ensure deterministic datbase results.

You can even access loading progress of specific database using `LoadProgress` property when it's loaded in background to display nice progress bar.