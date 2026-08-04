<div align="center">

# Plug Input Pack

**Configure the Unity Input System once, then read actions like ordinary game data.**

A compact input layer with typed values, frame-state helpers, device tracking and generated action
names for Unity projects that do not need another gameplay framework.

[![Release](https://img.shields.io/github/v/release/Natteens/com.natteens.pluginputpack?sort=semver&label=release&style=flat-square)](https://github.com/Natteens/com.natteens.pluginputpack/releases)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-000000?style=flat-square&logo=unity)](https://unity.com)
[![Input System](https://img.shields.io/badge/Input_System-1.6.1%2B-555555?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.6/manual/index.html)
[![License](https://img.shields.io/github/license/Natteens/com.natteens.pluginputpack?style=flat-square)](./LICENSE)

[Why Plug Input Pack?](#why-plug-input-pack) · [Installation](#installation) · [Quick Start](#quick-start) · [Documentation](#documentation)

</div>

---

## Why Plug Input Pack?

The Unity Input System is flexible, but the same adapter code tends to reappear around it: looking
up actions, exposing values, distinguishing a held input from a fresh press, tracking the active
device and keeping physics reads consistent.

Plug Input Pack keeps that layer small and reusable. The action asset remains the source of truth;
the package turns it into a reader that gameplay code can query without repeating the setup in every
controller.

<table>
<tr>
<td width="50%"><strong>Generated action names</strong><br><sub>Save the action asset and use generated constants instead of scattering string keys through gameplay code.</sub></td>
<td width="50%"><strong>Typed reads</strong><br><sub>Read vectors, buttons and other action values through one compact access pattern.</sub></td>
</tr>
<tr>
<td width="50%"><strong>Frame-state helpers</strong><br><sub>Separate held, pressed and released states across both Update and FixedUpdate.</sub></td>
<td width="50%"><strong>Device awareness</strong><br><sub>Track the active control scheme and centralize cursor or debug behavior when the project needs it.</sub></td>
</tr>
</table>

## Installation

Requires Unity **2022.3** or newer. The compatible Input System dependency is declared by the
package.

In the Package Manager, choose **Add package from git URL** and paste:

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

Pin the dependency to a release tag when reproducible installs matter.

## Quick Start

Create an Input Action Asset, assign it to a `PlugInputReader`, then place a
`PlugInputComponent` in the scene. Saving the action asset generates `InputNames.cs`.

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

The component owns input lifecycle. Gameplay code only reads the state it needs.

## Documentation

The complete workflow lives in [Documentation](./Documentation~/index.md), including:

- action asset and reader setup;
- Update versus FixedUpdate behavior;
- callbacks and active-device tracking;
- cursor handling, generated names and troubleshooting.

See the [changelog](./CHANGELOG.md) for release history.

## License

MIT. See [LICENSE](./LICENSE).
