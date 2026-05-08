using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

namespace PlugInputPack
{
    public class PlugInputComponent : MonoBehaviour
    {
        [SerializeField] private PlugInputReader inputReader;

        private PlugInputCache         _cache;
        private PlugInputDebugger      _debugger;
        private PlugInputVisualizer    _visualizer;
        private PlugInputDeviceManager _deviceManager;

        private bool           _cursorLocked;
        private CursorLockMode _originalCursorLockMode;
        private bool           _originalCursorVisible;
        private bool           _hasManualOverride;
        private bool           _inputEventsSubscribed;

        private readonly Dictionary<string, InputValue> _lastValues = new();

        public delegate void InputPerformedHandler(string actionName, InputValue value);
        public delegate void InputFloatHandler(string actionName, float value);
        public delegate void InputVector2Handler(string actionName, Vector2 value);
        public delegate void InputBoolHandler(string actionName, bool value);
        public delegate void DeviceChangedHandler(PlugInputDeviceManager.DeviceType previous, PlugInputDeviceManager.DeviceType current);

        public event InputPerformedHandler  OnInputPerformed;
        public event Action<string>         OnInputCanceled;
        public event Action<string>         OnInputPressed;
        public event Action<string>         OnInputReleased;
        public event InputFloatHandler      OnInputValueChanged;
        public event InputVector2Handler    OnInputVector2Changed;
        public event InputBoolHandler       OnInputStateChanged;
        public event Action                 OnInputSystemInitialized;
        public event Action                 OnInputSystemDestroyed;

        public event DeviceChangedHandler        OnDeviceChanged;
        public event Action<InputDevice>         OnDeviceConnected;
        public event Action<InputDevice>         OnDeviceDisconnected;

        public PlugInputDeviceManager            DeviceManager    => _deviceManager;
        public PlugInputDeviceManager.DeviceType CurrentDeviceType => _deviceManager?.CurrentDeviceType ?? PlugInputDeviceManager.DeviceType.Unknown;
        public string                            CurrentDeviceName => _deviceManager?.CurrentDeviceName ?? "None";
        public bool                              IsCursorLocked    => _cursorLocked;

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            _cache         = new PlugInputCache();
            _debugger      = new PlugInputDebugger();
            _visualizer    = new PlugInputVisualizer();
            _deviceManager = new PlugInputDeviceManager();

            _originalCursorLockMode = Cursor.lockState;
            _originalCursorVisible  = Cursor.visible;

            if (inputReader != null && inputReader.InputActionAsset != null)
                InitializeInputSystem();
            else
                Debug.LogWarning("[PlugInput] No Input Reader or Input Action Asset assigned.");
        }

        private void OnEnable()
        {
            var actionAsset = inputReader?.InputActionAsset;
            if (actionAsset == null) return;

            SubscribeInputEvents(actionAsset);
            actionAsset.Enable();
        }

        private void OnDisable()
        {
            var actionAsset = inputReader?.InputActionAsset;
            if (actionAsset == null) return;

            actionAsset.Disable();
            UnsubscribeInputEvents(actionAsset);
        }

        private void InitializeInputSystem()
        {
            var actionAsset = inputReader.InputActionAsset;
            if (actionAsset == null) { Debug.LogError("[PlugInput] InputActionAsset is null."); return; }

            _debugger.SetEnabled(inputReader.EnableDebug);
            _visualizer.Initialize(inputReader.EnableVisualDebug, inputReader.DebugHandleSize / 100f, inputReader.DebugHandleColor);
            _deviceManager.Initialize(inputReader.EnableDeviceManagement, inputReader.DeviceSwitchCooldown);

            SetupDeviceEvents();
            RegisterAllInputs(actionAsset);

            if (inputReader.LockCursorOnStart) LockCursor(); else UnlockCursor();

            OnInputSystemInitialized?.Invoke();
        }

        private void RegisterAllInputs(InputActionAsset actionAsset)
        {
            int total = 0;
            foreach (var map in actionAsset.actionMaps)
                foreach (var action in map.actions)
                {
                    _cache.RegisterState(action);
                    _lastValues[action.name] = default;
                    total++;
                }

            SubscribeInputEvents(actionAsset);

            if (inputReader.EnableDebug)
                _debugger.LogReady(actionAsset.actionMaps.Count, total);
        }

        private void Update()
        {
            var flushResults = _cache.FlushStates();
            for (int i = 0; i < flushResults.Count; i++)
            {
                var (state, pressed, released) = flushResults[i];
                if (pressed)
                {
                    OnInputPressed?.Invoke(state.Name);
                    if (inputReader.EnableDebug) _debugger.LogInputActivity(state.Name, state.CurrentValue, true);
                }
                if (released)
                {
                    OnInputReleased?.Invoke(state.Name);
                    if (inputReader.EnableDebug) _debugger.LogInputActivity(state.Name, state.CurrentValue, false);
                }
            }
        }

        private void OnGUI()
        {
            if (inputReader != null && inputReader.EnableVisualDebug && _visualizer != null)
                _visualizer.DrawHandles(_cache);
        }

        private void OnDestroy()
        {
            Cursor.lockState = _originalCursorLockMode;
            Cursor.visible   = _originalCursorVisible;

            PlugInputDeviceManager.OnDeviceChanged      -= HandleDeviceChanged;
            PlugInputDeviceManager.OnDeviceConnected    -= HandleDeviceConnected;
            PlugInputDeviceManager.OnDeviceDisconnected -= HandleDeviceDisconnected;

            OnInputSystemDestroyed?.Invoke();

            UnsubscribeInputEvents(inputReader?.InputActionAsset);

            _lastValues?.Clear();
            _cache?.Dispose();
            _debugger?.Clear();
            _deviceManager?.Dispose();
        }

        private void SubscribeInputEvents(InputActionAsset actionAsset)
        {
            if (_inputEventsSubscribed || actionAsset == null) return;

            foreach (var map in actionAsset.actionMaps)
                foreach (var action in map.actions)
                {
                    action.performed += OnActionPerformed;
                    action.canceled  += OnActionCanceled;
                }

            _inputEventsSubscribed = true;
        }

        private void UnsubscribeInputEvents(InputActionAsset actionAsset)
        {
            if (!_inputEventsSubscribed || actionAsset == null) return;

            foreach (var map in actionAsset.actionMaps)
                foreach (var action in map.actions)
                {
                    action.performed -= OnActionPerformed;
                    action.canceled  -= OnActionCanceled;
                }

            _inputEventsSubscribed = false;
        }

        // ── Input callbacks ────────────────────────────────────────────────────────

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            _deviceManager.ProcessInputActivity(context);

            string actionName = context.action.name;
            var state = _cache.GetState(actionName);
            if (state == null) return;

            OnInputPerformed?.Invoke(actionName, state.CurrentValue);
            FireValueChangedEvents(actionName, state);
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            _deviceManager.ProcessInputActivity(context);

            string actionName = context.action.name;
            var state = _cache.GetState(actionName);
            if (state == null) return;

            OnInputCanceled?.Invoke(actionName);
            FireValueChangedEvents(actionName, state);
        }

        private void FireValueChangedEvents(string actionName, InputState state)
        {
            InputValue current = state.CurrentValue;
            if (_lastValues.TryGetValue(actionName, out InputValue last) && current.Equals(last)) return;

            _lastValues[actionName] = current;

            switch (current.Kind)
            {
                case InputValue.ValueKind.Float:   OnInputValueChanged?.Invoke(actionName, current.FloatVal); break;
                case InputValue.ValueKind.Vector2: OnInputVector2Changed?.Invoke(actionName, current.Vec2Val); break;
                case InputValue.ValueKind.Bool:    OnInputStateChanged?.Invoke(actionName, current.BoolVal); break;
            }
        }

        // ── Device ─────────────────────────────────────────────────────────────────

        private void SetupDeviceEvents()
        {
            PlugInputDeviceManager.OnDeviceChanged      += HandleDeviceChanged;
            PlugInputDeviceManager.OnDeviceConnected    += HandleDeviceConnected;
            PlugInputDeviceManager.OnDeviceDisconnected += HandleDeviceDisconnected;
        }

        private static readonly string[] s_deviceTypeNames =
            { "Unknown", "Keyboard", "Mouse", "Gamepad", "Touch", "Joystick", "XRController" };
        private static string DeviceName(PlugInputDeviceManager.DeviceType t) => s_deviceTypeNames[(int)t];

        private void HandleDeviceChanged(PlugInputDeviceManager.DeviceType previous, PlugInputDeviceManager.DeviceType current)
        {
            if (inputReader.EnableDebug) _debugger.LogDeviceChanged(DeviceName(previous), DeviceName(current));

            if (!_hasManualOverride && inputReader.AutoLockCursorOnGamepad)
            {
                bool isGamepadLike = current == PlugInputDeviceManager.DeviceType.Gamepad
                                  || current == PlugInputDeviceManager.DeviceType.Joystick
                                  || current == PlugInputDeviceManager.DeviceType.XRController;
                if (isGamepadLike) LockCursor(); else UnlockCursor();
            }

            OnDeviceChanged?.Invoke(previous, current);
        }

        private void HandleDeviceConnected(InputDevice device)
        {
            if (inputReader.EnableDebug) _debugger.LogDeviceEvent("Device connected", device.displayName);
            OnDeviceConnected?.Invoke(device);
        }

        private void HandleDeviceDisconnected(InputDevice device)
        {
            if (inputReader.EnableDebug) _debugger.LogDeviceEvent("Device disconnected", device.displayName);
            OnDeviceDisconnected?.Invoke(device);
        }

        public bool ForceDeviceType(PlugInputDeviceManager.DeviceType deviceType) =>
            _deviceManager?.ForceDeviceType(deviceType) ?? false;

        // ── Cursor ─────────────────────────────────────────────────────────────────

        public void LockCursor()
        {
            _cursorLocked = true; _hasManualOverride = true;
            Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
        }

        public void UnlockCursor()
        {
            _cursorLocked = false; _hasManualOverride = true;
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        }

        public void ResetCursorBehavior()
        {
            _hasManualOverride = false;
            if (inputReader.AutoLockCursorOnGamepad)
            {
                bool isGamepadLike = CurrentDeviceType == PlugInputDeviceManager.DeviceType.Gamepad
                                  || CurrentDeviceType == PlugInputDeviceManager.DeviceType.Joystick
                                  || CurrentDeviceType == PlugInputDeviceManager.DeviceType.XRController;
                if (isGamepadLike) LockCursor(); else UnlockCursor();
            }
            else { if (inputReader.LockCursorOnStart) LockCursor(); else UnlockCursor(); }
        }

        // ── API ────────────────────────────────────────────────────────────────────

        public InputAccessor this[string actionName]
        {
            get
            {
                if (string.IsNullOrEmpty(actionName)) { Debug.LogWarning("[PlugInput] Action name is null or empty."); return null; }
                return _cache.GetAccessor(actionName);
            }
        }

        public bool TryGetInput(string actionName, out InputAccessor accessor)
        {
            accessor = null;
            if (string.IsNullOrEmpty(actionName) || !_cache.HasInput(actionName)) return false;
            accessor = _cache.GetAccessor(actionName);
            return accessor != null;
        }

        public bool HasInput(string actionName) =>
            !string.IsNullOrEmpty(actionName) && _cache.HasInput(actionName);

        public IEnumerable<string> GetAllInputNames() => _cache.GetInputNames();
    }
}
