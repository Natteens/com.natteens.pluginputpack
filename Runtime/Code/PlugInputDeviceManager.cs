using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System;
using System.Collections.Generic;

namespace PlugInputPack
{
    /// <summary>
    /// Gerencia dispositivos de entrada e isolamento de inputs.
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
        
        private DeviceType _currentDeviceType = DeviceType.Unknown;
        private InputDevice _currentDevice;
        private readonly Dictionary<DeviceType, List<InputDevice>> _devicesByType = new Dictionary<DeviceType, List<InputDevice>>();
        private readonly HashSet<DeviceType> _allowedDevices = new HashSet<DeviceType>();
        private readonly Dictionary<string, DeviceType> _actionDeviceMapping = new Dictionary<string, DeviceType>();
        
        private bool _isEnabled = false;
        private bool _strictIsolation = false;
        private float _deviceSwitchCooldown = 0.1f;
        private float _lastDeviceSwitchTime = 0f;
        
        // Controle para ações compostas (como Look)
        private readonly Dictionary<string, float> _lastInputActivity = new Dictionary<string, float>();
        private readonly Dictionary<string, Vector2> _lastInputValues = new Dictionary<string, Vector2>();
        private float _inputActivityThreshold = 0.1f;
        
        public static event Action<DeviceType, DeviceType> OnDeviceChanged;
        public static event Action<InputDevice> OnDeviceConnected;
        public static event Action<InputDevice> OnDeviceDisconnected;
        public static event Action<DeviceType> OnDeviceTypeFiltered;
        
        /// <summary>
        /// Tipo de dispositivo atual
        /// </summary>
        public DeviceType CurrentDeviceType => _currentDeviceType;
        
        /// <summary>
        /// Dispositivo atual
        /// </summary>
        public InputDevice CurrentDevice => _currentDevice;
        
        /// <summary>
        /// Nome do dispositivo atual
        /// </summary>
        public string CurrentDeviceName => _currentDevice?.displayName ?? "Nenhum";
        
        /// <summary>
        /// Verifica se o sistema está habilitado
        /// </summary>
        public bool IsEnabled => _isEnabled;
        
        /// <summary>
        /// Inicializa o gerenciador de dispositivos
        /// </summary>
        public void Initialize(bool enabled, bool strictIsolation, float switchCooldown, DeviceType[] allowedDevices)
        {
            _isEnabled = enabled;
            _strictIsolation = strictIsolation;
            _deviceSwitchCooldown = switchCooldown;
            
            if (!_isEnabled)
                return;
                
            SetAllowedDevices(allowedDevices);
            CategorizeDevices();
            DetectInitialDevice();
            
            InputSystem.onDeviceChange += OnInputDeviceChange;
        }
        
        /// <summary>
        /// Define quais tipos de dispositivos são permitidos
        /// </summary>
        public void SetAllowedDevices(DeviceType[] allowedDevices)
        {
            _allowedDevices.Clear();
            if (allowedDevices != null)
            {
                foreach (var deviceType in allowedDevices)
                {
                    _allowedDevices.Add(deviceType);
                }
            }
        }
        
        /// <summary>
        /// Categoriza todos os dispositivos conectados
        /// </summary>
        private void CategorizeDevices()
        {
            _devicesByType.Clear();
            
            foreach (var device in InputSystem.devices)
            {
                DeviceType deviceType = GetDeviceType(device);
                
                if (!_devicesByType.ContainsKey(deviceType))
                {
                    _devicesByType[deviceType] = new List<InputDevice>();
                }
                
                _devicesByType[deviceType].Add(device);
            }
        }
        
        /// <summary>
        /// Detecta o dispositivo inicial baseado na entrada mais recente
        /// </summary>
        private void DetectInitialDevice()
        {
            if (_allowedDevices.Count > 0)
            {
                foreach (var allowedType in _allowedDevices)
                {
                    if (_devicesByType.TryGetValue(allowedType, out var devices) && devices.Count > 0)
                    {
                        SwitchToDevice(allowedType, devices[0]);
                        break;
                    }
                }
            }
            else if (InputSystem.devices.Count > 0)
            {
                var firstDevice = InputSystem.devices[0];
                SwitchToDevice(GetDeviceType(firstDevice), firstDevice);
            }
        }
        
        /// <summary>
        /// Callback para mudanças de dispositivos
        /// </summary>
        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    HandleDeviceAdded(device);
                    break;
                    
                case InputDeviceChange.Removed:
                    HandleDeviceRemoved(device);
                    break;
            }
        }
        
        /// <summary>
        /// Manipula adição de dispositivo
        /// </summary>
        private void HandleDeviceAdded(InputDevice device)
        {
            DeviceType deviceType = GetDeviceType(device);
            
            if (!_devicesByType.ContainsKey(deviceType))
            {
                _devicesByType[deviceType] = new List<InputDevice>();
            }
            
            _devicesByType[deviceType].Add(device);
            OnDeviceConnected?.Invoke(device);
        }
        
        /// <summary>
        /// Manipula remoção de dispositivo
        /// </summary>
        private void HandleDeviceRemoved(InputDevice device)
        {
            DeviceType deviceType = GetDeviceType(device);
            
            if (_devicesByType.TryGetValue(deviceType, out var devices))
            {
                devices.Remove(device);
            }
            
            if (_currentDevice == device)
            {
                FindAlternativeDevice();
            }
            
            OnDeviceDisconnected?.Invoke(device);
        }
        
        /// <summary>
        /// Procura um dispositivo alternativo quando o atual é removido
        /// </summary>
        private void FindAlternativeDevice()
        {
            foreach (var allowedType in _allowedDevices)
            {
                if (_devicesByType.TryGetValue(allowedType, out var devices) && devices.Count > 0)
                {
                    SwitchToDevice(allowedType, devices[0]);
                    return;
                }
            }
            
            _currentDevice = null;
            _currentDeviceType = DeviceType.Unknown;
        }
        
        /// <summary>
        /// Determina o tipo de um dispositivo
        /// </summary>
        private DeviceType GetDeviceType(InputDevice device)
        {
            switch (device)
            {
                case Keyboard:
                    return DeviceType.Keyboard;
                case Mouse:
                    return DeviceType.Mouse;
                case Gamepad:
                    return DeviceType.Gamepad;
                case Touchscreen:
                    return DeviceType.Touch;
                case Joystick:
                    // Trata controles genéricos como Gamepad se tiverem botões suficientes
                    if (HasGamepadLikeControls(device))
                        return DeviceType.Gamepad;
                    return DeviceType.Joystick;
                case TrackedDevice:
                    return DeviceType.XRController;
                default:
                    // Verifica se é algum tipo de controller
                    if (device.description.deviceClass.Contains("XR") || 
                        device.description.deviceClass.Contains("VR"))
                    {
                        return DeviceType.XRController;
                    }
                    
                    // Se tem características de gamepad, trata como tal
                    if (HasGamepadLikeControls(device))
                    {
                        return DeviceType.Gamepad;
                    }
                    
                    return DeviceType.Unknown;
            }
        }
        
        /// <summary>
        /// Verifica se o dispositivo tem controles similares a um gamepad
        /// </summary>
        private bool HasGamepadLikeControls(InputDevice device)
        {
            // Verifica se tem pelo menos 4 botões e 2 sticks/eixos
            int buttonCount = 0;
            int axisCount = 0;
            
            foreach (var control in device.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl)
                    buttonCount++;
                else if (control is UnityEngine.InputSystem.Controls.AxisControl)
                    axisCount++;
                else if (control is UnityEngine.InputSystem.Controls.StickControl)
                    axisCount += 2; // Stick conta como 2 eixos
            }
            
            // Se tem pelo menos 4 botões e 4 eixos, provavelmente é um gamepad
            return buttonCount >= 4 && axisCount >= 4;
        }
        
        /// <summary>
        /// Detecta qual dispositivo está sendo usado para ações compostas (como Look)
        /// </summary>
        private DeviceType DetectActiveDeviceForCompositeAction(InputAction.CallbackContext context, Vector2 currentValue)
        {
            string actionName = context.action.name;
            float currentTime = Time.unscaledTime;
            
            // Para ações de "Look", detecta qual dispositivo está realmente sendo usado
            if (actionName.ToLower().Contains("look") || actionName.ToLower().Contains("camera"))
            {
                return DetectLookDeviceActivity(currentValue, currentTime);
            }
            
            // Para outras ações compostas, usa a detecção padrão
            return GetDeviceType(context.control?.device);
        }
        
        /// <summary>
        /// Detecta especificamente qual dispositivo está sendo usado para Look
        /// </summary>
        private DeviceType DetectLookDeviceActivity(Vector2 currentValue, float currentTime)
        {
            // Verifica se há atividade significativa no mouse
            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                if (mouseDelta.magnitude > _inputActivityThreshold)
                {
                    _lastInputActivity["mouse"] = currentTime;
                    return DeviceType.Mouse;
                }
            }
            
            // Verifica se há atividade significativa no gamepad
            if (Gamepad.current != null)
            {
                Vector2 stickValue = Gamepad.current.rightStick.ReadValue();
                if (stickValue.magnitude > _inputActivityThreshold)
                {
                    _lastInputActivity["gamepad"] = currentTime;
                    return DeviceType.Gamepad;
                }
            }
            
            // Verifica joysticks genéricos
            foreach (var joystick in Joystick.all)
            {
                if (joystick != null && joystick.stick != null)
                {
                    Vector2 stickValue = joystick.stick.ReadValue();
                    if (stickValue.magnitude > _inputActivityThreshold)
                    {
                        _lastInputActivity["joystick"] = currentTime;
                        return HasGamepadLikeControls(joystick) ? DeviceType.Gamepad : DeviceType.Joystick;
                    }
                }
            }
            
            // Se não há atividade nova, usa o último dispositivo ativo
            float mouseLastActivity = _lastInputActivity.ContainsKey("mouse") ? _lastInputActivity["mouse"] : 0f;
            float gamepadLastActivity = _lastInputActivity.ContainsKey("gamepad") ? _lastInputActivity["gamepad"] : 0f;
            float joystickLastActivity = _lastInputActivity.ContainsKey("joystick") ? _lastInputActivity["joystick"] : 0f;
            
            float mostRecentTime = Mathf.Max(mouseLastActivity, gamepadLastActivity, joystickLastActivity);
            
            if (mostRecentTime > 0 && (currentTime - mostRecentTime) < 1f)
            {
                if (mostRecentTime == mouseLastActivity)
                    return DeviceType.Mouse;
                else if (mostRecentTime == gamepadLastActivity)
                    return DeviceType.Gamepad;
                else if (mostRecentTime == joystickLastActivity)
                    return DeviceType.Gamepad; // Trata joystick como gamepad
            }
            
            return _currentDeviceType;
        }
        
        /// <summary>
        /// Muda para um dispositivo específico
        /// </summary>
        private void SwitchToDevice(DeviceType deviceType, InputDevice device)
        {
            if (Time.unscaledTime - _lastDeviceSwitchTime < _deviceSwitchCooldown)
                return;
                
            var previousType = _currentDeviceType;
            _currentDeviceType = deviceType;
            _currentDevice = device;
            _lastDeviceSwitchTime = Time.unscaledTime;
            
            if (previousType != deviceType)
            {
                OnDeviceChanged?.Invoke(previousType, deviceType);
            }
        }
        
        /// <summary>
        /// Detecta mudança de dispositivo baseada em atividade de input
        /// </summary>
        public void ProcessInputActivity(InputAction.CallbackContext context)
        {
            if (!_isEnabled)
                return;
                
            var device = context.control?.device;
            if (device == null)
                return;
                
            DeviceType detectedType;
            
            // Para ações compostas como Look, usa detecção especial
            if (context.action.name.ToLower().Contains("look") || context.action.name.ToLower().Contains("camera"))
            {
                Vector2 value = context.ReadValue<Vector2>();
                detectedType = DetectActiveDeviceForCompositeAction(context, value);
            }
            else
            {
                detectedType = GetDeviceType(device);
            }
            
            if (!IsDeviceAllowed(detectedType))
            {
                OnDeviceTypeFiltered?.Invoke(detectedType);
                return;
            }
            
            if (detectedType != _currentDeviceType)
            {
                // Encontra um dispositivo do tipo detectado
                if (_devicesByType.TryGetValue(detectedType, out var devices) && devices.Count > 0)
                {
                    SwitchToDevice(detectedType, devices[0]);
                }
                else if (detectedType == DeviceType.Gamepad && device != null)
                {
                    // Se detectou como gamepad mas não tem na lista, adiciona
                    if (!_devicesByType.ContainsKey(DeviceType.Gamepad))
                    {
                        _devicesByType[DeviceType.Gamepad] = new List<InputDevice>();
                    }
                    _devicesByType[DeviceType.Gamepad].Add(device);
                    SwitchToDevice(detectedType, device);
                }
            }
        }
        
        /// <summary>
        /// Verifica se um input deve ser processado baseado no isolamento
        /// </summary>
        public bool ShouldProcessInput(InputAction.CallbackContext context, string actionName)
        {
            if (!_isEnabled)
                return true;
                
            if (!_strictIsolation)
                return true;
                
            var device = context.control?.device;
            if (device == null)
                return true;
                
            DeviceType inputDeviceType = GetDeviceType(device);
            
            if (!IsDeviceAllowed(inputDeviceType))
                return false;
            
            // Para ações compostas como Look, sempre processa mas filtra no ProcessInputActivity
            if (actionName.ToLower().Contains("look") || actionName.ToLower().Contains("camera"))
            {
                return true;
            }
            
            // Permite tanto Gamepad quanto Joystick se o atual for um deles
            if ((_currentDeviceType == DeviceType.Gamepad || _currentDeviceType == DeviceType.Joystick) &&
                (inputDeviceType == DeviceType.Gamepad || inputDeviceType == DeviceType.Joystick))
            {
                return true;
            }
            
            return inputDeviceType == _currentDeviceType;
        }
        
        /// <summary>
        /// Verifica se um tipo de dispositivo é permitido
        /// </summary>
        private bool IsDeviceAllowed(DeviceType deviceType)
        {
            return _allowedDevices.Count == 0 || _allowedDevices.Contains(deviceType);
        }
        
        /// <summary>
        /// Força a mudança para um tipo de dispositivo específico
        /// </summary>
        public bool ForceDeviceType(DeviceType deviceType)
        {
            if (!IsDeviceAllowed(deviceType))
                return false;
                
            if (_devicesByType.TryGetValue(deviceType, out var devices) && devices.Count > 0)
            {
                SwitchToDevice(deviceType, devices[0]);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Obtém todos os dispositivos de um tipo específico
        /// </summary>
        public List<InputDevice> GetDevicesOfType(DeviceType deviceType)
        {
            return _devicesByType.TryGetValue(deviceType, out var devices) ? 
                   new List<InputDevice>(devices) : new List<InputDevice>();
        }
        
        /// <summary>
        /// Define o threshold para detecção de atividade
        /// </summary>
        public void SetInputActivityThreshold(float threshold)
        {
            _inputActivityThreshold = Mathf.Max(0.01f, threshold);
        }
        
        /// <summary>
        /// Obtém informações de debug
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"Dispositivo Atual: {CurrentDeviceName} ({_currentDeviceType})\n";
            info += $"Isolamento: {(_strictIsolation ? "Ativo" : "Inativo")}\n";
            info += $"Dispositivos Permitidos: {_allowedDevices.Count}\n";
            info += $"Total de Tipos: {_devicesByType.Count}\n";
            info += $"Threshold de Atividade: {_inputActivityThreshold:F3}\n";
            info += $"Cooldown: {_deviceSwitchCooldown:F2}s";
            return info;
        }
        
        /// <summary>
        /// Limpa recursos
        /// </summary>
        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnInputDeviceChange;
            _devicesByType.Clear();
            _allowedDevices.Clear();
            _actionDeviceMapping.Clear();
            _lastInputActivity.Clear();
            _lastInputValues.Clear();
            
            OnDeviceChanged = null;
            OnDeviceConnected = null;
            OnDeviceDisconnected = null;
            OnDeviceTypeFiltered = null;
        }
    }
}