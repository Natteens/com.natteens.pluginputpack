using UnityEngine;
using UnityEngine.InputSystem;

namespace PlugInputPack
{
    [CreateAssetMenu(fileName = "New PlugInputReader", menuName = "Plug Input Pack/Input Reader")]
    public class PlugInputReader : ScriptableObject
    {
        [Header("Input")]
        [SerializeField, Tooltip("The Unity Input System asset that defines all input actions.")]
        private InputActionAsset inputActionAsset;

        [Header("Debug")]
        [SerializeField, Tooltip("Print input activity to the console.")]
        private bool enableDebug;

        [SerializeField, Tooltip("Show a real-time input overlay on screen.")]
        private bool enableVisualDebug;

        [SerializeField, Tooltip("Scale of the visual debug overlay elements."), Range(1f, 300f)]
        private float debugHandleSize = 100f;

        [SerializeField, Tooltip("Color used for the visual debug overlay.")]
        private Color debugHandleColor = Color.yellow;

        [Header("Device Management")]
        [SerializeField, Tooltip("Detect and track which input device the player is using.")]
        private bool enableDeviceManagement = true;

        [SerializeField, Tooltip("When enabled, only inputs from the currently active device are processed. Useful for preventing ghost inputs when switching between keyboard and gamepad.")]
        private bool strictDeviceIsolation;

        [SerializeField, Tooltip("Minimum time in seconds before the active device can change again. Prevents accidental flicker when two devices are used simultaneously."), Range(0f, 2f)]
        private float deviceSwitchCooldown = 0.1f;

        [SerializeField, Tooltip("Restrict input to specific device types. Leave empty to allow all devices.")]
        private PlugInputDeviceManager.DeviceType[] allowedDevices = new PlugInputDeviceManager.DeviceType[0];

        [Header("Cursor")]
        [SerializeField, Tooltip("Lock and hide the cursor when the scene starts.")]
        private bool lockCursorOnStart;

        [SerializeField, Tooltip("Automatically lock the cursor when a gamepad, joystick, or XR controller is detected, and unlock it when switching back to keyboard/mouse.")]
        private bool autoLockCursorOnGamepad = true;

        // --- Accessors ---

        public InputActionAsset InputActionAsset       => inputActionAsset;
        public bool  EnableDebug                       => enableDebug;
        public bool  EnableVisualDebug                 => enableVisualDebug;
        public float DebugHandleSize                   => debugHandleSize;
        public Color DebugHandleColor                  => debugHandleColor;
        public bool  EnableDeviceManagement            => enableDeviceManagement;
        public bool  StrictDeviceIsolation             => strictDeviceIsolation;
        public float DeviceSwitchCooldown              => deviceSwitchCooldown;
        public PlugInputDeviceManager.DeviceType[] AllowedDevices => allowedDevices;
        public bool  LockCursorOnStart                 => lockCursorOnStart;
        public bool  AutoLockCursorOnGamepad           => autoLockCursorOnGamepad;

        // --- Validation ---

        public bool IsValid()
        {
            if (inputActionAsset == null)
            {
                Debug.LogWarning($"[PlugInputReader] '{name}': Input Action Asset is not assigned.");
                return false;
            }
            if (inputActionAsset.actionMaps.Count == 0)
            {
                Debug.LogWarning($"[PlugInputReader] '{name}': Input Action Asset has no action maps.");
                return false;
            }
            return true;
        }

        public string GetDebugInfo()
        {
            if (!IsValid()) return "Invalid configuration";

            int totalActions = 0;
            foreach (var map in inputActionAsset.actionMaps)
                totalActions += map.actions.Count;

            string deviceInfo = enableDeviceManagement
                ? $", Devices: {(allowedDevices.Length > 0 ? allowedDevices.Length.ToString() : "All")}"
                : "";

            return $"Maps: {inputActionAsset.actionMaps.Count}, Actions: {totalActions}{deviceInfo}";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            debugHandleSize       = Mathf.Clamp(debugHandleSize, 1f, 300f);
            debugHandleColor.a    = Mathf.Clamp01(debugHandleColor.a);
            deviceSwitchCooldown  = Mathf.Clamp(deviceSwitchCooldown, 0f, 2f);
        }
#endif
    }
}