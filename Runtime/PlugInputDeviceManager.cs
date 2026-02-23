using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlugInputPack
{
    /// <summary>
    /// Tracks which input device the player is currently using and handles device switching.
    ///
    /// Performance notes:
    ///   • No .ToLower() on the hot path — action name checks use OrdinalIgnoreCase
    ///     comparisons that don't allocate, or a pre-built HashSet populated at init.
    ///   • Enum.ToString() is never called on the hot path — a static lookup array is
    ///     used instead (zero alloc).
    ///   • GetDebugInfo() uses a cached StringBuilder instead of string += chaining.
    ///   • GetDevicesOfType() returns IReadOnlyList — no copy allocation.
    /// </summary>
    public class PlugInputDeviceManager
    {
        public enum DeviceType
        {
            Unknown,
            Keyboard,
            Mouse,
            Gamepad,
            Touch,
            Joystick,
            XRController
        }

        // Pre-built name table so Enum.ToString() is never called at runtime
        private static readonly string[] s_deviceTypeNames =
        {
            "Unknown", "Keyboard", "Mouse", "Gamepad", "Touch", "Joystick", "XRController"
        };

        private static string DeviceName(DeviceType t) => s_deviceTypeNames[(int)t];

        // ── State ─────────────────────────────────────────────────────────────────

        private DeviceType _currentDeviceType = DeviceType.Unknown;
        private InputDevice _currentDevice;

        private readonly Dictionary<DeviceType, List<InputDevice>> _devicesByType  = new Dictionary<DeviceType, List<InputDevice>>();
        private readonly HashSet<DeviceType>                       _allowedDevices = new HashSet<DeviceType>();
        private readonly Dictionary<string, DeviceType>            _actionDeviceMapping = new Dictionary<string, DeviceType>();

        // Per-device type cache keyed by InputDevice instance ID — avoids re-running
        // GetDeviceType (which may iterate allControls) on every single input event.
        private readonly Dictionary<int, DeviceType> _deviceTypeCache = new Dictionary<int, DeviceType>();

        private bool  _isEnabled;
        private bool  _strictIsolation;
        private float _deviceSwitchCooldown = 0.1f;
        private float _lastDeviceSwitchTime;

        // Composite-action (Look/Camera) activity tracking
        private readonly Dictionary<string, float>   _lastInputActivity = new Dictionary<string, float>();
        private readonly Dictionary<string, Vector2> _lastInputValues   = new Dictionary<string, Vector2>();
        private float _inputActivityThreshold = 0.1f;

        // Pre-built set of "look-like" action names (populated on Initialize from the asset)
        // so we never call .ToLower() or .Contains() on the hot path.
        private readonly HashSet<string> _lookActionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly StringBuilder _debugSb = new StringBuilder(256);

        // ── Events ────────────────────────────────────────────────────────────────

        public static event Action<DeviceType, DeviceType> OnDeviceChanged;
        public static event Action<InputDevice>            OnDeviceConnected;
        public static event Action<InputDevice>            OnDeviceDisconnected;
        public static event Action<DeviceType>             OnDeviceTypeFiltered;

        // ── Properties ────────────────────────────────────────────────────────────

        public DeviceType   CurrentDeviceType => _currentDeviceType;
        public InputDevice  CurrentDevice     => _currentDevice;
        public string       CurrentDeviceName => _currentDevice?.displayName ?? "None";
        public bool         IsEnabled         => _isEnabled;

        // ── Initialization ────────────────────────────────────────────────────────

        public void Initialize(bool enabled, bool strictIsolation, float switchCooldown, DeviceType[] allowedDevices)
        {
            _isEnabled            = enabled;
            _strictIsolation      = strictIsolation;
            _deviceSwitchCooldown = switchCooldown;

            if (!_isEnabled) return;

            SetAllowedDevices(allowedDevices);
            CategorizeDevices();
            DetectInitialDevice();

            InputSystem.onDeviceChange += OnInputDeviceChange;
        }

        /// <summary>
        /// Call this after the InputActionAsset is known so look-action names
        /// can be cached — avoids .ToLower() + .Contains() on every input event.
        /// </summary>
        public void CacheLookActionNames(UnityEngine.InputSystem.InputActionAsset asset)
        {
            _lookActionNames.Clear();
            if (asset == null) return;
            foreach (var map in asset.actionMaps)
                foreach (var action in map.actions)
                {
                    string n = action.name;
                    // Flag actions whose name contains "look" or "camera" (case-insensitive)
                    if (n.IndexOf("look",   StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("camera", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _lookActionNames.Add(n);
                    }
                }
        }

        public void SetAllowedDevices(DeviceType[] allowedDevices)
        {
            _allowedDevices.Clear();
            if (allowedDevices == null) return;
            foreach (DeviceType dt in allowedDevices)
                _allowedDevices.Add(dt);
        }

        // ── Device cataloguing ────────────────────────────────────────────────────

        private void CategorizeDevices()
        {
            _devicesByType.Clear();
            foreach (InputDevice device in InputSystem.devices)
            {
                DeviceType dt = GetDeviceType(device);
                if (!_devicesByType.TryGetValue(dt, out List<InputDevice> list))
                {
                    list = new List<InputDevice>();
                    _devicesByType[dt] = list;
                }
                list.Add(device);
            }
        }

        private void DetectInitialDevice()
        {
            if (_allowedDevices.Count > 0)
            {
                foreach (DeviceType allowed in _allowedDevices)
                {
                    if (_devicesByType.TryGetValue(allowed, out List<InputDevice> devices) && devices.Count > 0)
                    {
                        SwitchToDevice(allowed, devices[0]);
                        return;
                    }
                }
            }
            else if (InputSystem.devices.Count > 0)
            {
                InputDevice first = InputSystem.devices[0];
                SwitchToDevice(GetDeviceType(first), first);
            }
        }

        // ── Device change callbacks ───────────────────────────────────────────────

        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)   HandleDeviceAdded(device);
            else if (change == InputDeviceChange.Removed) HandleDeviceRemoved(device);
        }

        private void HandleDeviceAdded(InputDevice device)
        {
            DeviceType dt = GetDeviceType(device);
            if (!_devicesByType.TryGetValue(dt, out List<InputDevice> list))
            {
                list = new List<InputDevice>();
                _devicesByType[dt] = list;
            }
            list.Add(device);
            OnDeviceConnected?.Invoke(device);
        }

        private void HandleDeviceRemoved(InputDevice device)
        {
            DeviceType dt = GetDeviceType(device);
            if (_devicesByType.TryGetValue(dt, out List<InputDevice> list))
                list.Remove(device);

            _deviceTypeCache.Remove(device.deviceId); // invalidate cache entry

            if (_currentDevice == device)
                FindAlternativeDevice();

            OnDeviceDisconnected?.Invoke(device);
        }

        private void FindAlternativeDevice()
        {
            foreach (DeviceType allowed in _allowedDevices)
            {
                if (_devicesByType.TryGetValue(allowed, out List<InputDevice> devices) && devices.Count > 0)
                {
                    SwitchToDevice(allowed, devices[0]);
                    return;
                }
            }
            _currentDevice     = null;
            _currentDeviceType = DeviceType.Unknown;
        }

        // ── Device type classification ────────────────────────────────────────────

        /// <summary>
        /// Cached version of GetDeviceType — keyed by device.deviceId (int).
        /// Called on every input event; avoids iterating allControls more than once per device.
        /// </summary>
        private DeviceType GetDeviceTypeCached(InputDevice device)
        {
            if (device == null) return DeviceType.Unknown;
            int id = device.deviceId;
            if (_deviceTypeCache.TryGetValue(id, out DeviceType cached)) return cached;
            DeviceType computed = GetDeviceType(device);
            _deviceTypeCache[id] = computed;
            return computed;
        }

        private DeviceType GetDeviceType(InputDevice device)
        {
            if (device == null) return DeviceType.Unknown;

            switch (device)
            {
                case Keyboard:    return DeviceType.Keyboard;
                case Mouse:       return DeviceType.Mouse;
                case Gamepad:     return DeviceType.Gamepad;
                case Touchscreen: return DeviceType.Touch;
                case TrackedDevice: return DeviceType.XRController;
                case Joystick j:
                    return HasGamepadLikeControls(j) ? DeviceType.Gamepad : DeviceType.Joystick;
                default:
                    string cls = device.description.deviceClass ?? string.Empty;
                    if (cls.IndexOf("XR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cls.IndexOf("VR", StringComparison.OrdinalIgnoreCase) >= 0)
                        return DeviceType.XRController;
                    return HasGamepadLikeControls(device) ? DeviceType.Gamepad : DeviceType.Unknown;
            }
        }

        private static bool HasGamepadLikeControls(InputDevice device)
        {
            int buttons = 0, axes = 0;
            foreach (var control in device.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl) buttons++;
                else if (control is UnityEngine.InputSystem.Controls.AxisControl)  axes++;
                else if (control is UnityEngine.InputSystem.Controls.StickControl) axes += 2;
            }
            return buttons >= 4 && axes >= 4;
        }

        // ── Hot-path input processing ─────────────────────────────────────────────

        /// <summary>
        /// Called on every input performed/canceled callback.
        /// No string allocation — look-action check uses a pre-built HashSet.
        /// </summary>
        public void ProcessInputActivity(InputAction.CallbackContext context)
        {
            if (!_isEnabled) return;

            InputDevice device = context.control?.device;
            if (device == null) return;

            string actionName = context.action.name;
            DeviceType detectedType;

            // _lookActionNames was built at init — HashSet.Contains is O(1), no alloc
            if (_lookActionNames.Contains(actionName))
            {
                Vector2 value = context.ReadValue<Vector2>();
                detectedType = DetectActiveDeviceForLook(value);
            }
            else
            {
                detectedType = GetDeviceTypeCached(device);
            }

            if (!IsDeviceAllowed(detectedType))
            {
                OnDeviceTypeFiltered?.Invoke(detectedType);
                return;
            }

            if (detectedType != _currentDeviceType)
            {
                if (_devicesByType.TryGetValue(detectedType, out List<InputDevice> devices) && devices.Count > 0)
                {
                    SwitchToDevice(detectedType, devices[0]);
                }
                else if (device != null)
                {
                    if (!_devicesByType.TryGetValue(detectedType, out List<InputDevice> newList))
                    {
                        newList = new List<InputDevice>();
                        _devicesByType[detectedType] = newList;
                    }
                    newList.Add(device);
                    SwitchToDevice(detectedType, device);
                }
            }
        }

        /// <summary>
        /// Returns true if the input from context.control.device should be processed.
        /// No string allocation — look-action check uses the pre-built HashSet.
        /// </summary>
        public bool ShouldProcessInput(InputAction.CallbackContext context, string actionName)
        {
            if (!_isEnabled || !_strictIsolation) return true;

            InputDevice device = context.control?.device;
            if (device == null) return true;

            DeviceType inputType = GetDeviceTypeCached(device);

            if (!IsDeviceAllowed(inputType)) return false;

            // Look/camera composite actions always pass through; device is handled
            // in ProcessInputActivity which runs first.
            if (_lookActionNames.Contains(actionName)) return true;

            // Gamepad and Joystick are treated as the same family
            bool currentIsGamepadFamily = _currentDeviceType == DeviceType.Gamepad || _currentDeviceType == DeviceType.Joystick;
            bool inputIsGamepadFamily   = inputType == DeviceType.Gamepad || inputType == DeviceType.Joystick;
            if (currentIsGamepadFamily && inputIsGamepadFamily) return true;

            return inputType == _currentDeviceType;
        }

        // ── Look-action device detection ──────────────────────────────────────────

        private DeviceType DetectActiveDeviceForLook(Vector2 currentValue)
        {
            float now = Time.unscaledTime;

            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                if (delta.sqrMagnitude > _inputActivityThreshold * _inputActivityThreshold)
                {
                    _lastInputActivity["mouse"] = now;
                    return DeviceType.Mouse;
                }
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                if (stick.sqrMagnitude > _inputActivityThreshold * _inputActivityThreshold)
                {
                    _lastInputActivity["gamepad"] = now;
                    return DeviceType.Gamepad;
                }
            }

            foreach (Joystick joystick in Joystick.all)
            {
                if (joystick?.stick == null) continue;
                Vector2 stick = joystick.stick.ReadValue();
                if (stick.sqrMagnitude > _inputActivityThreshold * _inputActivityThreshold)
                {
                    _lastInputActivity["joystick"] = now;
                    return HasGamepadLikeControls(joystick) ? DeviceType.Gamepad : DeviceType.Joystick;
                }
            }

            // No fresh input — pick whichever device was active most recently
            float tMouse    = _lastInputActivity.TryGetValue("mouse",    out float m) ? m : 0f;
            float tGamepad  = _lastInputActivity.TryGetValue("gamepad",  out float g) ? g : 0f;
            float tJoystick = _lastInputActivity.TryGetValue("joystick", out float j) ? j : 0f;

            float most = Mathf.Max(tMouse, Mathf.Max(tGamepad, tJoystick));
            if (most > 0f && (now - most) < 1f)
            {
                if (most == tMouse)   return DeviceType.Mouse;
                if (most == tGamepad) return DeviceType.Gamepad;
                return DeviceType.Gamepad; // joystick treated as gamepad
            }

            return _currentDeviceType;
        }

        // ── Switching ─────────────────────────────────────────────────────────────

        private void SwitchToDevice(DeviceType deviceType, InputDevice device)
        {
            if (Time.unscaledTime - _lastDeviceSwitchTime < _deviceSwitchCooldown) return;

            DeviceType previous = _currentDeviceType;
            _currentDeviceType      = deviceType;
            _currentDevice          = device;
            _lastDeviceSwitchTime   = Time.unscaledTime;

            if (previous != deviceType)
                OnDeviceChanged?.Invoke(previous, deviceType);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public bool ForceDeviceType(DeviceType deviceType)
        {
            if (!IsDeviceAllowed(deviceType)) return false;
            if (_devicesByType.TryGetValue(deviceType, out List<InputDevice> devices) && devices.Count > 0)
            {
                SwitchToDevice(deviceType, devices[0]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the internal list for a device type — no copy allocation.
        /// Callers must not modify the returned list.
        /// </summary>
        public IReadOnlyList<InputDevice> GetDevicesOfType(DeviceType deviceType) =>
            _devicesByType.TryGetValue(deviceType, out List<InputDevice> devices)
                ? (IReadOnlyList<InputDevice>)devices
                : System.Array.Empty<InputDevice>();

        public void SetInputActivityThreshold(float threshold) =>
            _inputActivityThreshold = Mathf.Max(0.01f, threshold);

        private bool IsDeviceAllowed(DeviceType dt) =>
            _allowedDevices.Count == 0 || _allowedDevices.Contains(dt);

        /// <summary>
        /// Returns a debug info string. Uses a reused StringBuilder — no string += chaining.
        /// Only call from debug/editor paths.
        /// </summary>
        public string GetDebugInfo()
        {
            _debugSb.Clear();
            _debugSb.Append("Device: ").Append(CurrentDeviceName)
                    .Append(" (").Append(DeviceName(_currentDeviceType)).Append(")\n");
            _debugSb.Append("Strict Isolation: ").Append(_strictIsolation ? "On" : "Off").Append('\n');
            _debugSb.Append("Allowed Devices: ").Append(_allowedDevices.Count).Append('\n');
            _debugSb.Append("Device Types Seen: ").Append(_devicesByType.Count).Append('\n');
            _debugSb.Append("Activity Threshold: ").Append(_inputActivityThreshold.ToString("F3")).Append('\n');
            _debugSb.Append("Switch Cooldown: ").Append(_deviceSwitchCooldown.ToString("F2")).Append('s');
            return _debugSb.ToString();
        }

        // ── Disposal ──────────────────────────────────────────────────────────────

        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnInputDeviceChange;
            _devicesByType.Clear();
            _allowedDevices.Clear();
            _actionDeviceMapping.Clear();
            _lastInputActivity.Clear();
            _lastInputValues.Clear();
            _lookActionNames.Clear();
            _deviceTypeCache.Clear();

            OnDeviceChanged      = null;
            OnDeviceConnected    = null;
            OnDeviceDisconnected = null;
            OnDeviceTypeFiltered = null;
        }
    }
}