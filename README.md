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

### ID128 (non-unique numeric identifier)

`ID128` is a 128-bit value identifier based on `Unity.Mathematics.uint4`.

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

# Notes

- All identifiers are structs intended for high-performance comparisons and dictionary keys.
- `OperationResult` supports implicit boolean conversion for convenience in control flow. Prefer the explicit helper methods when clarity is needed.
- `InputAPI` assumes Unity Input System package is installed and actions/maps are configured.

