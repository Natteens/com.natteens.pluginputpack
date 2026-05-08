# Plug Input Pack

[![Unity Version](https://img.shields.io/badge/Unity-6000.1%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![Input System](https://img.shields.io/badge/Input%20System-2.2.0%2B-green.svg)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/index.html)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**PlugInputPack** is a reusable Unity package that simplifies the setup and usage of the Unity Input System. With a clean, modular architecture and an intuitive API, it helps you avoid repetitive code and get your inputs running quickly in any project.

---

## Features

**Ease of Use**
- Simple API: `input["Move"]` or `input[InputNames.Move]` to access any input
- Automatic conversions: Native support for Vector2, Vector3, float, bool, int
- Zero configuration: Works right after import
- ScriptableObject: Reusable configurations across projects
- Auto-generated `InputNames.cs`: IntelliSense-friendly string constants, regenerated automatically when your `.inputactions` changes

**Advanced Features**
- Built-in debug system: Detailed console logs and real-time screen overlay
- Advanced public events: 8 input events + 3 device events for maximum flexibility
- Robust validation: Safe error handling and invalid configuration checks

**Device Management**
- Automatic device detection: Detects Keyboard, Mouse, Gamepad, Touch, Joystick and XR Controllers
- Device switch events: React when the player switches input method
- Switch cooldown: Configurable minimum time before the active device can change

**Cursor Management**
- Auto lock on start: Optionally lock and hide the cursor at startup
- Auto lock on gamepad: Automatically lock/unlock cursor when switching to/from gamepad
- Manual control: `LockCursor()` and `UnlockCursor()` for runtime control

**Compatibility**
- Unity 2022.3+: Full support for LTS versions
- Cross-platform: PC, Console, Mobile, WebGL
- Input System 1.4.0+: Native integration with Unity's modern input system

---

## Installation

**Method 1: Package Manager (Recommended)**

1. Open the Package Manager (`Window > Package Manager`)
2. Click the `+` button in the top left corner
3. Select `Add package from git URL...`
4. Paste the URL:
```
https://github.com/Natteens/com.natteens.pluginputpack.git
```
5. Click `Add`

**Method 2: manifest.json**

Add to your `Packages/manifest.json`:
```json
{
   "dependencies": {
      "com.natteens.pluginputpack": "https://github.com/Natteens/com.natteens.pluginputpack.git"
   }
}
```

---

## Quick Start

### Initial Setup

1. Create an Input Action Asset via `Create > Input Actions` and configure your actions (Move, Jump, Attack, etc.)
2. Create a PlugInputReader via `Create > Scriptable Objects > Plug Input Pack > Input Reader`, drag your Input Action Asset into the field and configure debug, device and cursor options as needed
3. Add the `PlugInputComponent` to a GameObject and drag the created PlugInputReader into it
4. Save your `.inputactions` asset — `InputNames.cs` will be generated automatically next to your PlugInputReader

### Using in Scripts

```csharp
using UnityEngine;
using PlugInputPack;

public class PlayerController : MonoBehaviour
{
    private PlugInputComponent input;

    void Start()
    {
        input = FindObjectOfType<PlugInputComponent>();
    }

    void Update()
    {
        // Direct reading with automatic conversion
        Vector2 movement = input[InputNames.Move];
        bool jump = input[InputNames.Jump];

        // JustPressed: true only on the frame it was pressed — use in Update
        if (input[InputNames.Jump].JustPressed)
            Jump();

        // IsHeld: true while the button is held down
        if (input[InputNames.Move].IsHeld)
            Move(input[InputNames.Move].Vector2);
    }

    void Jump() => Debug.Log("Jumping!");
    void Move(Vector2 direction) => transform.Translate(direction * Time.deltaTime);
}
```

### FixedUpdate Input

Use `TakePress()` / `TakeRelease()` when reading input from `FixedUpdate`. These consume the event and return `true` exactly once per press, regardless of how many `FixedUpdate` calls occur in the same frame.

```csharp
void FixedUpdate()
{
    if (input[InputNames.Jump].TakePress())
        ApplyJumpForce();
}
```

### Input Event System

Events are instance-based — subscribe via a reference to the `PlugInputComponent`.

```csharp
using UnityEngine;
using PlugInputPack;

public class InputEventHandler : MonoBehaviour
{
    [SerializeField] private PlugInputComponent input;

    void OnEnable()
    {
        input.OnInputPerformed        += HandleInputPerformed;
        input.OnInputCanceled         += HandleInputCanceled;
        input.OnInputPressed          += HandleInputPressed;
        input.OnInputReleased         += HandleInputReleased;
        input.OnInputValueChanged     += HandleFloatChange;
        input.OnInputVector2Changed   += HandleVector2Change;
        input.OnInputStateChanged     += HandleBoolChange;
        input.OnInputSystemInitialized += HandleSystemInit;
        input.OnInputSystemDestroyed  += HandleSystemDestroy;
    }

    void OnDisable()
    {
        input.OnInputPerformed        -= HandleInputPerformed;
        input.OnInputCanceled         -= HandleInputCanceled;
        input.OnInputPressed          -= HandleInputPressed;
        input.OnInputReleased         -= HandleInputReleased;
        input.OnInputValueChanged     -= HandleFloatChange;
        input.OnInputVector2Changed   -= HandleVector2Change;
        input.OnInputStateChanged     -= HandleBoolChange;
        input.OnInputSystemInitialized -= HandleSystemInit;
        input.OnInputSystemDestroyed  -= HandleSystemDestroy;
    }

    void HandleInputPerformed(string actionName, InputValue value)
        => Debug.Log($"Input {actionName} performed: {value}");

    void HandleInputPressed(string actionName)
        => Debug.Log($"Pressed: {actionName}");

    void HandleInputReleased(string actionName)
        => Debug.Log($"Released: {actionName}");

    void HandleFloatChange(string actionName, float value)
        => Debug.Log($"Float changed {actionName}: {value}");

    void HandleVector2Change(string actionName, Vector2 value)
        => Debug.Log($"Vector2 changed {actionName}: {value}");

    void HandleBoolChange(string actionName, bool value)
        => Debug.Log($"Bool changed {actionName}: {value}");

    void HandleSystemInit()
        => Debug.Log("Input system initialized!");

    void HandleSystemDestroy()
        => Debug.Log("Input system destroyed!");
}
```

### Device Events

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using PlugInputPack;

public class DeviceHandler : MonoBehaviour
{
    [SerializeField] private PlugInputComponent input;

    void Start() =>
        Debug.Log($"Current device: {input.CurrentDeviceName} ({input.CurrentDeviceType})");

    void OnEnable()
    {
        input.OnDeviceChanged      += HandleDeviceChanged;
        input.OnDeviceConnected    += HandleDeviceConnected;
        input.OnDeviceDisconnected += HandleDeviceDisconnected;
    }

    void OnDisable()
    {
        input.OnDeviceChanged      -= HandleDeviceChanged;
        input.OnDeviceConnected    -= HandleDeviceConnected;
        input.OnDeviceDisconnected -= HandleDeviceDisconnected;
    }

    void HandleDeviceChanged(PlugInputDeviceManager.DeviceType previous, PlugInputDeviceManager.DeviceType current)
        => Debug.Log($"Device switched from {previous} to {current}");

    void HandleDeviceConnected(InputDevice device)
        => Debug.Log($"Device connected: {device.displayName}");

    void HandleDeviceDisconnected(InputDevice device)
        => Debug.Log($"Device disconnected: {device.displayName}");
}
```

### Cursor Management

```csharp
void Update()
{
    if (input[InputNames.Pause].JustPressed)
    {
        if (input.IsCursorLocked) input.UnlockCursor();
        else                      input.LockCursor();
    }

    // Reset to automatic behavior defined in the PlugInputReader
    if (input[InputNames.ResetCursor].JustPressed)
        input.ResetCursorBehavior();
}
```

### Safe Validation

```csharp
if (input.HasInput(InputNames.SpecialAction))
{
    bool special = input[InputNames.SpecialAction];
}

if (input.TryGetInput(InputNames.Move, out InputAccessor moveInput))
{
    Vector2 movement = moveInput.Vector2;
}

InputAccessor accessor = input[InputNames.Jump];
if (accessor != null && accessor.IsValid)
{
    bool jumped = accessor.JustPressed;
}

foreach (string name in input.GetAllInputNames())
    Debug.Log($"Available input: {name}");
```

---

## Full API

### InputAccessor Properties

```csharp
// Implicit conversions
Vector2 movement = input[InputNames.Move];
float   axis     = input[InputNames.Horizontal];
bool    button   = input[InputNames.Jump];

// Typed properties
input[InputNames.Move].Vector2
input[InputNames.Move].Vector3
input[InputNames.Move].Float
input[InputNames.Move].Bool
input[InputNames.Move].Int

// Frame states
input[InputNames.Jump].JustPressed   // true on the frame it was pressed — use in Update
input[InputNames.Jump].JustReleased  // true on the frame it was released — use in Update
input[InputNames.Jump].IsHeld        // true while held down

// FixedUpdate-safe consume methods
// Return true exactly once per event, regardless of how many FixedUpdate calls occur in the frame
input[InputNames.Jump].TakePress()
input[InputNames.Jump].TakeRelease()

// Validity
input[InputNames.Jump].IsValid

// Debug info (allocates — avoid calling every frame)
input[InputNames.Move].RawValue
input[InputNames.Move].InputType   // e.g. "Vector2", "Button"
input[InputNames.Move].Name
```

### PlugInputComponent Methods & Properties

```csharp
// Main access
InputAccessor accessor = input[InputNames.ActionName];

// Validation
bool                   exists    = input.HasInput(InputNames.ActionName);
bool                   success   = input.TryGetInput(InputNames.ActionName, out InputAccessor accessor);
IEnumerable<string>    allInputs = input.GetAllInputNames();

// Device info
PlugInputDeviceManager.DeviceType type       = input.CurrentDeviceType;
string                            deviceName = input.CurrentDeviceName;
PlugInputDeviceManager            manager    = input.DeviceManager;
bool                              forced     = input.ForceDeviceType(PlugInputDeviceManager.DeviceType.Gamepad);

// Cursor
bool locked = input.IsCursorLocked;
input.LockCursor();
input.UnlockCursor();
input.ResetCursorBehavior();
```

### Input Events

```csharp
OnInputPerformed(string actionName, InputValue value)
OnInputCanceled(string actionName)
OnInputPressed(string actionName)
OnInputReleased(string actionName)
OnInputValueChanged(string actionName, float value)
OnInputVector2Changed(string actionName, Vector2 value)
OnInputStateChanged(string actionName, bool value)
OnInputSystemInitialized()
OnInputSystemDestroyed()
```

### Device Events

```csharp
OnDeviceChanged(DeviceType previous, DeviceType current)
OnDeviceConnected(InputDevice device)
OnDeviceDisconnected(InputDevice device)
```

### DeviceType Enum

```csharp
PlugInputDeviceManager.DeviceType.Unknown
PlugInputDeviceManager.DeviceType.Keyboard
PlugInputDeviceManager.DeviceType.Mouse
PlugInputDeviceManager.DeviceType.Gamepad
PlugInputDeviceManager.DeviceType.Touch
PlugInputDeviceManager.DeviceType.Joystick
PlugInputDeviceManager.DeviceType.XRController
```

---

## InputNames — IntelliSense for Action Names

When you save your `.inputactions` asset, `InputNames.cs` is generated automatically next to your `PlugInputReader`. It contains a `const string` for every action, so you get IntelliSense autocomplete and never mistype an action name.

```csharp
// Generated automatically — do not edit manually
namespace PlugInputPack
{
    public static class InputNames
    {
        // Player
        public const string Move   = "Move";
        public const string Jump   = "Jump";
        public const string Attack = "Attack";
        // ...
    }
}
```

The file is only rewritten when action **names** change. Editing bindings without renaming actions does not trigger a domain reload.

---

## PlugInputReader Settings

Create via `Create > Scriptable Objects > Plug Input Pack > Input Reader`.

| Section | Field | Description |
|---|---|---|
| Main | Input Action Asset | Your Unity Input System asset |
| Debug | Console Logs | Print input activity to the Unity console |
| Debug | Screen Overlay | Real-time on-screen overlay during Play Mode |
| Device Management | Enable | Automatic device detection and tracking |
| Device Management | Switch Cooldown | Minimum seconds between device switches (0–2s) |
| Cursor | Lock On Start | Lock and hide cursor on scene start |
| Cursor | Auto Lock On Gamepad | Auto lock/unlock when switching to or from gamepad |

The visual debug overlay shows active inputs, real-time values, a direction indicator for Vector2 inputs, and inputs fade out shortly after becoming inactive.

---

## License

MIT License — see [LICENSE](LICENSE) for details.

## Author

[Natteens](https://github.com/Natteens) — natteens.social@gmail.com — [Issues](https://github.com/Natteens/com.natteens.pluginputpack/issues)