# Plug Input Pack

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![Input System](https://img.shields.io/badge/Input%20System-2.0.6%2B-green.svg)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/index.html)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**PlugInputPack** is a reusable Unity package that simplifies the setup and usage of the Unity Input System. With a clean, modular architecture and an intuitive API, it helps you avoid repetitive code and get your inputs running quickly in any project.

---

## Features

**Ease of Use**
- Simple API: `input["Move"]` to access any input
- Automatic conversions: Native support for Vector2, Vector3, float, bool, int
- Zero configuration: Works right after import
- ScriptableObject: Reusable configurations across projects

**Advanced Features**
- Built-in debug system: Detailed logs and real-time visualization
- Optimized cache + object pool: Better performance with smart memory management
- Advanced public events: 8 input events + 4 device events for maximum flexibility
- Robust validation: Safe error handling and invalid configuration checks

**Device Management**
- Automatic device detection: Detects Keyboard, Mouse, Gamepad, Touch, Joystick and XR Controllers
- Device switch events: React when the player switches input method
- Strict device isolation: Optionally filter inputs by active device only
- Allowed device list: Restrict which device types can be used
- Composite action support: Smart detection for actions like Look that may use mouse or gamepad simultaneously

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
        Vector2 movement = input["Move"];
        bool jump = input["Jump"];
        float lookX = input["LookX"];
        
        if (input["Jump"].Pressed)
        {
            // Runs only on the frame it was pressed
            Jump();
        }
        
        if (input["Move"].Bool)
        {
            // True while being held
            Move(input["Move"].Vector2);
        }
    }
    
    void Jump() => Debug.Log("Jumping!");
    void Move(Vector2 direction) => transform.Translate(direction * Time.deltaTime);
}
```

### Input Event System

```csharp
using UnityEngine;
using PlugInputPack;

public class InputEventHandler : MonoBehaviour
{
    void OnEnable()
    {
        PlugInputComponent.OnInputPerformed += HandleInputPerformed;
        PlugInputComponent.OnInputCanceled += HandleInputCanceled;
        PlugInputComponent.OnInputPressed += HandleInputPressed;
        PlugInputComponent.OnInputReleased += HandleInputReleased;
        PlugInputComponent.OnInputValueChanged += HandleFloatChange;
        PlugInputComponent.OnInputVector2Changed += HandleVector2Change;
        PlugInputComponent.OnInputStateChanged += HandleBoolChange;
        PlugInputComponent.OnInputSystemInitialized += HandleSystemInit;
        PlugInputComponent.OnInputSystemDestroyed += HandleSystemDestroy;
    }
    
    void OnDisable()
    {
        // Always remove listeners!
        PlugInputComponent.OnInputPerformed -= HandleInputPerformed;
        PlugInputComponent.OnInputCanceled -= HandleInputCanceled;
        PlugInputComponent.OnInputPressed -= HandleInputPressed;
        PlugInputComponent.OnInputReleased -= HandleInputReleased;
        PlugInputComponent.OnInputValueChanged -= HandleFloatChange;
        PlugInputComponent.OnInputVector2Changed -= HandleVector2Change;
        PlugInputComponent.OnInputStateChanged -= HandleBoolChange;
        PlugInputComponent.OnInputSystemInitialized -= HandleSystemInit;
        PlugInputComponent.OnInputSystemDestroyed -= HandleSystemDestroy;
    }
    
    void HandleInputPerformed(string actionName, object value)
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
    private PlugInputComponent input;

    void Start()
    {
        input = FindObjectOfType<PlugInputComponent>();
        Debug.Log($"Current device: {input.CurrentDeviceName} ({input.CurrentDeviceType})");
    }

    void OnEnable()
    {
        PlugInputComponent.OnDeviceChanged += HandleDeviceChanged;
        PlugInputComponent.OnDeviceConnected += HandleDeviceConnected;
        PlugInputComponent.OnDeviceDisconnected += HandleDeviceDisconnected;
        PlugInputComponent.OnDeviceFiltered += HandleDeviceFiltered;
    }

    void OnDisable()
    {
        PlugInputComponent.OnDeviceChanged -= HandleDeviceChanged;
        PlugInputComponent.OnDeviceConnected -= HandleDeviceConnected;
        PlugInputComponent.OnDeviceDisconnected -= HandleDeviceDisconnected;
        PlugInputComponent.OnDeviceFiltered -= HandleDeviceFiltered;
    }

    void HandleDeviceChanged(PlugInputDeviceManager.DeviceType previous, PlugInputDeviceManager.DeviceType current)
        => Debug.Log($"Device switched from {previous} to {current}");

    void HandleDeviceConnected(InputDevice device)
        => Debug.Log($"Device connected: {device.displayName}");

    void HandleDeviceDisconnected(InputDevice device)
        => Debug.Log($"Device disconnected: {device.displayName}");

    void HandleDeviceFiltered(PlugInputDeviceManager.DeviceType deviceType)
        => Debug.Log($"Input from {deviceType} was ignored (not in allowed list)");
}
```

### Cursor Management

```csharp
using UnityEngine;
using PlugInputPack;

public class CursorHandler : MonoBehaviour
{
    private PlugInputComponent input;

    void Start()
    {
        input = FindObjectOfType<PlugInputComponent>();
    }

    void Update()
    {
        if (input["Pause"].Pressed)
        {
            if (input.IsCursorLocked)
                input.UnlockCursor();
            else
                input.LockCursor();
        }

        // Reset to automatic behavior defined in the PlugInputReader
        if (input["ResetCursor"].Pressed)
            input.ResetCursorBehavior();
    }
}
```

### Safe Validation

```csharp
if (input.HasInput("SpecialAction"))
{
    bool special = input["SpecialAction"];
}

if (input.TryGetInput("Move", out InputAccessor moveInput))
{
    Vector2 movement = moveInput.Vector2;
}

InputAccessor accessor = input["Jump"];
if (accessor != null && accessor.IsValid)
{
    bool jumped = accessor.Pressed;
}

foreach (string inputName in input.GetAllInputNames())
{
    Debug.Log($"Available input: {inputName}");
}
```

### Force Device Type

```csharp
bool success = input.ForceDeviceType(PlugInputDeviceManager.DeviceType.Gamepad);

if (success)
    Debug.Log("Now using Gamepad");
else
    Debug.Log("Gamepad not available");
```

---

## Full API

### InputAccessor Properties

```csharp
// Implicit conversions
Vector2 movement = input["Move"];
float axis = input["Horizontal"];
bool button = input["Jump"];

// Typed properties
input["Move"].Vector2
input["Move"].Vector3
input["Move"].Float
input["Move"].Bool
input["Move"].Int

// Frame states
input["Jump"].Pressed      // Pressed THIS frame
input["Jump"].Released     // Released THIS frame
input["Jump"].IsPressed    // Currently held

// Validity
input["Jump"].IsValid

// Debug info
input["Move"].RawValue
input["Move"].InputType    // e.g. "Vector2", "Button"
input["Move"].Name
```

### PlugInputComponent Methods & Properties

```csharp
// Main access
InputAccessor accessor = input["ActionName"];

// Validation
bool exists = input.HasInput("ActionName");
bool success = input.TryGetInput("ActionName", out InputAccessor accessor);
IEnumerable<string> allInputs = input.GetAllInputNames();

// Device info
PlugInputDeviceManager.DeviceType type = input.CurrentDeviceType;
string deviceName = input.CurrentDeviceName;
PlugInputDeviceManager manager = input.DeviceManager;
bool forced = input.ForceDeviceType(PlugInputDeviceManager.DeviceType.Gamepad);

// Cursor
bool locked = input.IsCursorLocked;
input.LockCursor();
input.UnlockCursor();
input.ResetCursorBehavior();
```

### Input Events

```csharp
OnInputPerformed(string actionName, object value)
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
OnDeviceFiltered(DeviceType deviceType)
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

## PlugInputReader Settings

Create via `Create > Scriptable Objects > Plug Input Pack > Input Reader`.

| Section | Field | Description |
|---|---|---|
| Main | Input Action Asset | Your Unity Input System asset |
| Debug | Enable Debug | Detailed console logs |
| Debug | Enable Visual Debug | Real-time on-screen overlay |
| Visual | Debug Handle Size | Scale of visual elements (1–300) |
| Visual | Debug Handle Color | Color of the overlay |
| Device Management | Enable Device Management | Automatic device detection |
| Device Management | Strict Device Isolation | Only process inputs from the active device |
| Device Management | Device Switch Cooldown | Minimum seconds between device switches (0–2s) |
| Device Management | Allowed Devices | Whitelist of accepted device types (empty = all) |
| Cursor | Lock Cursor On Start | Lock and hide cursor on scene start |
| Cursor | Auto Lock Cursor On Gamepad | Auto lock/unlock when switching to or from gamepad |

The visual debug overlay shows active inputs, real-time values, a direction indicator for Vector2 inputs, and inputs fade out shortly after becoming inactive.

---

## License

MIT License — see [LICENSE](LICENSE) for details.

## Author

[Natteens](https://github.com/Natteens) — natteens.social@gmail.com — [Issues](https://github.com/Natteens/com.natteens.pluginputpack/issues)