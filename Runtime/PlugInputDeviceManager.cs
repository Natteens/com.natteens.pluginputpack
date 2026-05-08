using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlugInputPack
{
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

        private static readonly string[] s_names =
            { "Unknown", "Keyboard", "Mouse", "Gamepad", "Touch", "Joystick", "XRController" };
        private static string DeviceName(DeviceType t) => s_names[(int)t];

        // ── State ──────────────────────────────────────────────────────────────────

        private DeviceType  _currentDeviceType = DeviceType.Unknown;
        private InputDevice _currentDevice;

        private readonly Dictionary<DeviceType, List<InputDevice>> _devicesByType  = new();
        private readonly Dictionary<int, DeviceType>               _deviceTypeCache = new();

        private bool  _isEnabled;
        private float _deviceSwitchCooldown = 0.1f;
        private float _lastDeviceSwitchTime;

        private readonly StringBuilder _debugSb = new(256);

        // ── Events ─────────────────────────────────────────────────────────────────

        public static event Action<DeviceType, DeviceType> OnDeviceChanged;
        public static event Action<InputDevice>            OnDeviceConnected;
        public static event Action<InputDevice>            OnDeviceDisconnected;

        // ── Properties ─────────────────────────────────────────────────────────────

        public DeviceType  CurrentDeviceType => _currentDeviceType;
        public InputDevice CurrentDevice     => _currentDevice;
        public string      CurrentDeviceName => _currentDevice?.displayName ?? "None";
        public bool        IsEnabled         => _isEnabled;

        // ── Init ───────────────────────────────────────────────────────────────────

        public void Initialize(bool enabled, float switchCooldown)
        {
            _isEnabled            = enabled;
            _deviceSwitchCooldown = switchCooldown;
            if (!_isEnabled) return;

            CategorizeDevices();
            DetectInitialDevice();
            InputSystem.onDeviceChange += OnInputDeviceChange;
        }

        private void CategorizeDevices()
        {
            _devicesByType.Clear();
            foreach (var device in InputSystem.devices)
            {
                var dt = GetDeviceType(device);
                if (!_devicesByType.TryGetValue(dt, out var list)) { list = new(); _devicesByType[dt] = list; }
                list.Add(device);
            }
        }

        private void DetectInitialDevice()
        {
            if (InputSystem.devices.Count == 0) return;
            var first = InputSystem.devices[0];
            SwitchToDevice(GetDeviceType(first), first);
        }

        // ── Device change ──────────────────────────────────────────────────────────

        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if      (change == InputDeviceChange.Added)   HandleDeviceAdded(device);
            else if (change == InputDeviceChange.Removed) HandleDeviceRemoved(device);
        }

        private void HandleDeviceAdded(InputDevice device)
        {
            var dt = GetDeviceType(device);
            if (!_devicesByType.TryGetValue(dt, out var list)) { list = new(); _devicesByType[dt] = list; }
            list.Add(device);
            OnDeviceConnected?.Invoke(device);
        }

        private void HandleDeviceRemoved(InputDevice device)
        {
            var dt = GetDeviceType(device);
            if (_devicesByType.TryGetValue(dt, out var list)) list.Remove(device);
            _deviceTypeCache.Remove(device.deviceId);
            if (_currentDevice == device) FindAlternativeDevice();
            OnDeviceDisconnected?.Invoke(device);
        }

        private void FindAlternativeDevice()
        {
            foreach (var kv in _devicesByType)
            {
                if (kv.Value.Count > 0) { SwitchToDevice(kv.Key, kv.Value[0]); return; }
            }
            _currentDevice     = null;
            _currentDeviceType = DeviceType.Unknown;
        }

        // ── Type detection ─────────────────────────────────────────────────────────

        private DeviceType GetDeviceTypeCached(InputDevice device)
        {
            if (device == null) return DeviceType.Unknown;
            int id = device.deviceId;
            if (_deviceTypeCache.TryGetValue(id, out var cached)) return cached;
            var computed = GetDeviceType(device);
            _deviceTypeCache[id] = computed;
            return computed;
        }

        private DeviceType GetDeviceType(InputDevice device)
        {
            if (device == null) return DeviceType.Unknown;
            switch (device)
            {
                case Keyboard:      return DeviceType.Keyboard;
                case Mouse:         return DeviceType.Mouse;
                case Gamepad:       return DeviceType.Gamepad;
                case Touchscreen:   return DeviceType.Touch;
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
            foreach (var c in device.allControls)
            {
                if (c is UnityEngine.InputSystem.Controls.ButtonControl) buttons++;
                else if (c is UnityEngine.InputSystem.Controls.AxisControl) axes++;
                else if (c is UnityEngine.InputSystem.Controls.StickControl) axes += 2;
            }
            return buttons >= 4 && axes >= 4;
        }

        // ── Hot path ───────────────────────────────────────────────────────────────

        public void ProcessInputActivity(InputAction.CallbackContext context)
        {
            if (!_isEnabled) return;
            var device = context.control?.device;
            if (device == null) return;

            var detected = GetDeviceTypeCached(device);
            if (detected == _currentDeviceType) return;

            if (!_devicesByType.TryGetValue(detected, out var devices) || devices.Count == 0)
            {
                if (!_devicesByType.ContainsKey(detected)) _devicesByType[detected] = new();
                _devicesByType[detected].Add(device);
            }

            SwitchToDevice(detected, _devicesByType[detected][0]);
        }

        // ── Switching ──────────────────────────────────────────────────────────────

        private void SwitchToDevice(DeviceType deviceType, InputDevice device)
        {
            if (Time.unscaledTime - _lastDeviceSwitchTime < _deviceSwitchCooldown) return;
            var previous       = _currentDeviceType;
            _currentDeviceType = deviceType;
            _currentDevice     = device;
            _lastDeviceSwitchTime = Time.unscaledTime;
            if (previous != deviceType) OnDeviceChanged?.Invoke(previous, deviceType);
        }

        // ── API ────────────────────────────────────────────────────────────────────

        public bool ForceDeviceType(DeviceType deviceType)
        {
            if (_devicesByType.TryGetValue(deviceType, out var devices) && devices.Count > 0)
            {
                SwitchToDevice(deviceType, devices[0]);
                return true;
            }
            return false;
        }

        public IReadOnlyList<InputDevice> GetDevicesOfType(DeviceType deviceType) =>
            _devicesByType.TryGetValue(deviceType, out var devices)
                ? (IReadOnlyList<InputDevice>)devices
                : Array.Empty<InputDevice>();

        public string GetDebugInfo()
        {
            _debugSb.Clear();
            _debugSb.Append("Device: ").Append(CurrentDeviceName)
                    .Append(" (").Append(DeviceName(_currentDeviceType)).Append(")\n");
            _debugSb.Append("Switch Cooldown: ").Append(_deviceSwitchCooldown.ToString("F2")).Append("s\n");
            _debugSb.Append("Device Types Seen: ").Append(_devicesByType.Count);
            return _debugSb.ToString();
        }

        // ── Dispose ────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnInputDeviceChange;
            _devicesByType.Clear();
            _deviceTypeCache.Clear();
            OnDeviceChanged      = null;
            OnDeviceConnected    = null;
            OnDeviceDisconnected = null;
        }
    }
}
