<div align="center">

# Plug Input Pack

A small Unity Input System layer for projects that need reusable input access without repeated setup code.

[![Release](https://img.shields.io/github/v/release/Natteens/com.natteens.pluginputpack?style=flat-square)](https://github.com/Natteens/com.natteens.pluginputpack/releases)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-000000?style=flat-square&logo=unity)](https://unity.com)
[![Input System](https://img.shields.io/badge/Input_System-1.6.1%2B-555555?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.6/manual/index.html)
[![License](https://img.shields.io/github/license/Natteens/com.natteens.pluginputpack?style=flat-square)](LICENSE)

</div>

Plug Input Pack turns an Input Action Asset into a reusable reader with typed values, frame-state helpers, device tracking, cursor control and generated action-name constants.

## Features

- Typed access through `input[InputNames.Move]`.
- Press and release handling for both `Update` and `FixedUpdate`.
- Automatic active-device tracking.
- Optional cursor and debug helpers.

## Installation

Add the package through `Window > Package Manager > Add package from git URL`:

```text
https://github.com/Natteens/com.natteens.pluginputpack.git
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.natteens.pluginputpack": "https://github.com/Natteens/com.natteens.pluginputpack.git"
  }
}
```

## Quick start

1. Create an Input Action Asset.
2. Create a `PlugInputReader` and assign that asset.
3. Add `PlugInputComponent` to a GameObject and assign the reader.
4. Save the action asset to generate `InputNames.cs`.

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

## Documentation

Setup, FixedUpdate handling, events, device tracking and cursor behavior are covered in [Documentation](Documentation~/index.md).

## License

MIT. See [LICENSE](LICENSE).