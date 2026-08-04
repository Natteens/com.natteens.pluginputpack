# Plug Input Pack documentation

Plug Input Pack wraps the Unity Input System with a reusable reader, typed accessors, device tracking and cursor management.

## Setup

1. Create an Input Action Asset through `Create > Input Actions`.
2. Create a `PlugInputReader` through `Create > Scriptable Objects > Plug Input Pack > Input Reader`.
3. Assign the Input Action Asset to the reader.
4. Add `PlugInputComponent` to a GameObject and assign the reader.
5. Save the `.inputactions` asset to generate `InputNames.cs`.

## Reading actions

```csharp
using PlugInputPack;
using UnityEngine;

public sealed class PlayerInputExample : MonoBehaviour
{
    [SerializeField] private PlugInputComponent input;

    private void Update()
    {
        Vector2 movement = input[InputNames.Move];

        if (input[InputNames.Jump].JustPressed)
        {
            Debug.Log("Jump");
        }
    }
}
```

`InputAccessor` exposes typed values such as `Vector2`, `Vector3`, `Float`, `Bool` and `Int`, together with `JustPressed`, `JustReleased` and `IsHeld` frame states.

## FixedUpdate

Use the consuming methods when physics code must receive a press or release exactly once:

```csharp
private void FixedUpdate()
{
    if (input[InputNames.Jump].TakePress())
    {
        ApplyJumpForce();
    }
}
```

## Validation

```csharp
if (input.TryGetInput(InputNames.Move, out InputAccessor move))
{
    Vector2 direction = move.Vector2;
}
```

Use `HasInput`, `TryGetInput` and `IsValid` when actions may be optional or configured by another project.

## Events

`PlugInputComponent` exposes instance events for action activity, value changes, lifecycle and device changes. Subscribe in `OnEnable` and unsubscribe in `OnDisable` using the component reference owned by the scene.

## Device tracking

The device manager can identify keyboard, mouse, gamepad, touch, joystick and XR controllers. It also exposes active-device changes and optional switch cooldown behavior.

## Cursor behavior

The reader can lock the cursor on startup, react to gamepad switching, or leave cursor state under manual control.

```csharp
input.LockCursor();
input.UnlockCursor();
input.ResetCursorBehavior();
```

## Generated action names

`InputNames.cs` contains constants generated from the assigned Input Action Asset. Use those constants instead of handwritten action-name strings. The file is rewritten when action names change.

## Debugging

The reader can enable console activity logs and an in-game overlay. The overlay is intended for development and should be disabled when it is not needed.

## Notes

- Read frame states from `Update`.
- Use `TakePress` and `TakeRelease` from `FixedUpdate`.
- Avoid repeatedly requesting allocation-heavy debug values in hot paths.
- Treat the configured `PlugInputComponent` as the owner of subscriptions and runtime input state.