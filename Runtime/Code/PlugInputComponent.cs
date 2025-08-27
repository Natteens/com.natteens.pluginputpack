using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

namespace PlugInputPack
{
    /// <summary>
    /// Componente principal do Plug Input Pack
    /// </summary>
    public class PlugInputComponent : MonoBehaviour
    {
        [SerializeField] 
        private PlugInputReader inputReader;
        
        private PlugInputCache _cache;
        private PlugInputDebugger _debugger;
        private PlugInputVisualizer _visualizer;
        private PlugInputDeviceManager _deviceManager;
        
        public static event Action<string, object> OnInputPerformed;
        public static event Action<string> OnInputCanceled;
        public static event Action<string> OnInputPressed;   
        public static event Action<string> OnInputReleased;  
        public static event Action<string, float> OnInputValueChanged;
        public static event Action<string, Vector2> OnInputVector2Changed;
        public static event Action<string, bool> OnInputStateChanged;
        public static event Action OnInputSystemInitialized;
        public static event Action OnInputSystemDestroyed;
        
        public static event Action<PlugInputDeviceManager.DeviceType, PlugInputDeviceManager.DeviceType> OnDeviceChanged;
        public static event Action<InputDevice> OnDeviceConnected;
        public static event Action<InputDevice> OnDeviceDisconnected;
        public static event Action<PlugInputDeviceManager.DeviceType> OnDeviceFiltered;
        
        private Dictionary<string, object> _lastValues = new Dictionary<string, object>();
        private CursorLockMode _originalCursorLockMode;
        private bool _originalCursorVisible;
        
        /// <summary>
        /// Gerenciador de dispositivos
        /// </summary>
        public PlugInputDeviceManager DeviceManager => _deviceManager;
        
        /// <summary>
        /// Tipo de dispositivo atual
        /// </summary>
        public PlugInputDeviceManager.DeviceType CurrentDeviceType => _deviceManager?.CurrentDeviceType ?? PlugInputDeviceManager.DeviceType.Unknown;
        
        /// <summary>
        /// Nome do dispositivo atual
        /// </summary>
        public string CurrentDeviceName => _deviceManager?.CurrentDeviceName ?? "Nenhum";
        
        private void Awake()
        {
            _cache = new PlugInputCache();
            _debugger = new PlugInputDebugger();
            _visualizer = new PlugInputVisualizer();
            _deviceManager = new PlugInputDeviceManager();
            
            StoreCursorState();
            
            if (inputReader != null && inputReader.InputActionAsset != null)
            {
                InitializeInputSystem();
            }
            else
            {
                Debug.LogWarning("PlugInputPack: Input Reader ou Input Action Asset não configurado!");
            }
        }
        
        /// <summary>
        /// Armazena estado inicial do cursor
        /// </summary>
        private void StoreCursorState()
        {
            _originalCursorLockMode = Cursor.lockState;
            _originalCursorVisible = Cursor.visible;
        }
        
        /// <summary>
        /// Inicializa o sistema de input com as configurações do InputReader
        /// </summary>
        private void InitializeInputSystem()
        {
            var actionAsset = inputReader.InputActionAsset;
            
            if (actionAsset == null)
            {
                Debug.LogError("PlugInputPack: InputActionAsset não pode ser nulo!");
                return;
            }
            
            _debugger.SetEnabled(inputReader.EnableDebug);
            
            _visualizer.Initialize(
                inputReader.EnableVisualDebug, 
                inputReader.DebugHandleSize / 100f,
                inputReader.DebugHandleColor
            );
            
            _deviceManager.Initialize(
                inputReader.EnableDeviceManagement,
                inputReader.StrictDeviceIsolation,
                inputReader.DeviceSwitchCooldown,
                inputReader.AllowedDevices
            );
            
            SetupDeviceEvents();
            RegisterAllInputs(actionAsset);
            
            OnInputSystemInitialized?.Invoke();
        }
        
        /// <summary>
        /// Configura eventos de dispositivos
        /// </summary>
        private void SetupDeviceEvents()
        {
            PlugInputDeviceManager.OnDeviceChanged += HandleDeviceChanged;
            PlugInputDeviceManager.OnDeviceConnected += HandleDeviceConnected;
            PlugInputDeviceManager.OnDeviceDisconnected += HandleDeviceDisconnected;
            PlugInputDeviceManager.OnDeviceTypeFiltered += HandleDeviceFiltered;
        }
        
        /// <summary>
        /// Manipula mudança de dispositivo
        /// </summary>
        private void HandleDeviceChanged(PlugInputDeviceManager.DeviceType previous, PlugInputDeviceManager.DeviceType current)
        {
            if (inputReader.EnableDebug)
            {
                Debug.Log($"PlugInputPack: Dispositivo mudou de {previous} para {current}");
            }
            
            UpdateCursorBehavior(current);
            OnDeviceChanged?.Invoke(previous, current);
        }
        
        /// <summary>
        /// Atualiza comportamento do cursor baseado no dispositivo
        /// </summary>
        private void UpdateCursorBehavior(PlugInputDeviceManager.DeviceType deviceType)
        {
            if (deviceType == PlugInputDeviceManager.DeviceType.Gamepad)
            {
                if (inputReader.HideCursorOnGamepad)
                {
                    Cursor.visible = false;
                }
                
                if (inputReader.LockCursorOnGamepad)
                {
                    Cursor.lockState = inputReader.GamepadCursorLockMode;
                }
            }
            else
            {
                Cursor.visible = _originalCursorVisible;
                Cursor.lockState = _originalCursorLockMode;
            }
        }
        
        /// <summary>
        /// Manipula conexão de dispositivo
        /// </summary>
        private void HandleDeviceConnected(InputDevice device)
        {
            if (inputReader.EnableDebug)
            {
                Debug.Log($"PlugInputPack: Dispositivo conectado: {device.displayName}");
            }
            
            OnDeviceConnected?.Invoke(device);
        }
        
        /// <summary>
        /// Manipula desconexão de dispositivo
        /// </summary>
        private void HandleDeviceDisconnected(InputDevice device)
        {
            if (inputReader.EnableDebug)
            {
                Debug.Log($"PlugInputPack: Dispositivo desconectado: {device.displayName}");
            }
            
            OnDeviceDisconnected?.Invoke(device);
        }
        
        /// <summary>
        /// Manipula dispositivo filtrado
        /// </summary>
        private void HandleDeviceFiltered(PlugInputDeviceManager.DeviceType deviceType)
        {
            if (inputReader.EnableDebug)
            {
                Debug.Log($"PlugInputPack: Dispositivo {deviceType} foi filtrado (não permitido)");
            }
            
            OnDeviceFiltered?.Invoke(deviceType);
        }
        
        /// <summary>
        /// Registra todas as ações do Input System
        /// </summary>
        private void RegisterAllInputs(InputActionAsset actionAsset)
        {
            int totalActions = 0;
            
            foreach (var actionMap in actionAsset.actionMaps)
            {
                foreach (var action in actionMap.actions)
                {
                    action.performed += OnActionPerformed;
                    action.canceled += OnActionCanceled;
                    
                    _cache.RegisterState(action);
                    
                    _lastValues[action.name] = null;
                    
                    action.Enable();
                    totalActions++;
                }
            }
            
            if (inputReader.EnableDebug)
            {
                Debug.Log($"PlugInputPack: Sistema inicializado com {actionAsset.actionMaps.Count} mapas de ação e {totalActions} ações.");
            }
        }
        
        /// <summary>
        /// Callback quando uma ação é executada
        /// </summary>
        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            _deviceManager.ProcessInputActivity(context);
            
            if (!_deviceManager.ShouldProcessInput(context, context.action.name))
                return;
                
            string actionName = context.action.name;
            var state = _cache.GetState(actionName);
            
            if (state != null)
            {
                OnInputPerformed?.Invoke(actionName, state.RawValue);
                
                if (state.PressedThisFrame)
                {
                    OnInputPressed?.Invoke(actionName);
                }
                
                DetectAndFireValueChanges(actionName, state);
                
                if (inputReader.EnableDebug)
                {
                    _debugger.LogInputActivity(actionName, state.RawValue, true);
                }
            }
        }
        
        /// <summary>
        /// Callback quando uma ação é cancelada
        /// </summary>
        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            _deviceManager.ProcessInputActivity(context);
            
            if (!_deviceManager.ShouldProcessInput(context, context.action.name))
                return;
                
            string actionName = context.action.name;
            var state = _cache.GetState(actionName);
            
            if (state != null)
            {
                OnInputCanceled?.Invoke(actionName);
                
                if (state.ReleasedThisFrame)
                {
                    OnInputReleased?.Invoke(actionName);
                }
                
                DetectAndFireValueChanges(actionName, state);
                
                if (inputReader.EnableDebug)
                {
                    _debugger.LogInputActivity(actionName, state.RawValue, false);
                }
            }
        }
        
        /// <summary>
        /// Detecta mudanças de valor e dispara eventos específicos
        /// </summary>
        private void DetectAndFireValueChanges(string actionName, InputState state)
        {
            object currentValue = state.RawValue;
            object lastValue = _lastValues.ContainsKey(actionName) ? _lastValues[actionName] : null;
            
            if (!ValuesAreEqual(currentValue, lastValue))
            {
                if (currentValue is float floatValue)
                {
                    OnInputValueChanged?.Invoke(actionName, floatValue);
                }
                else if (currentValue is Vector2 vector2Value)
                {
                    OnInputVector2Changed?.Invoke(actionName, vector2Value);
                }
                else if (currentValue is bool boolValue)
                {
                    OnInputStateChanged?.Invoke(actionName, boolValue);
                }
                
                _lastValues[actionName] = currentValue;
            }
        }
        
        /// <summary>
        /// Compara valores de forma inteligente
        /// </summary>
        private bool ValuesAreEqual(object current, object last)
        {
            if (current == null && last == null) return true;
            if (current == null || last == null) return false;
            
            if (current is float cf && last is float lf)
                return Mathf.Abs(cf - lf) < 0.001f;
            
            if (current is Vector2 cv2 && last is Vector2 lv2)
                return Vector2.Distance(cv2, lv2) < 0.001f;
            
            if (current is Vector3 cv3 && last is Vector3 lv3)
                return Vector3.Distance(cv3, lv3) < 0.001f;
            
            return current.Equals(last);
        }
        
        /// <summary>
        /// Força mudança para um tipo de dispositivo específico
        /// </summary>
        public bool ForceDeviceType(PlugInputDeviceManager.DeviceType deviceType)
        {
            return _deviceManager?.ForceDeviceType(deviceType) ?? false;
        }
        
        /// <summary>
        /// Acessa um input pelo nome usando a sintaxe input["Action"]
        /// </summary>
        public InputAccessor this[string actionName]
        {
            get
            {
                if (string.IsNullOrEmpty(actionName))
                {
                    Debug.LogWarning("PlugInputPack: Nome da ação está vazio ou nulo!");
                    return null;
                }
                
                return _cache.GetAccessor(actionName);
            }
        }
        
        /// <summary>
        /// Tenta obter um input de forma segura
        /// </summary>
        public bool TryGetInput(string actionName, out InputAccessor accessor)
        {
            accessor = null;
            
            if (string.IsNullOrEmpty(actionName))
                return false;
                
            if (!_cache.HasInput(actionName))
                return false;
                
            accessor = _cache.GetAccessor(actionName);
            return accessor != null;
        }
        
        /// <summary>
        /// Verifica se um input existe
        /// </summary>
        public bool HasInput(string actionName)
        {
            return !string.IsNullOrEmpty(actionName) && _cache.HasInput(actionName);
        }
        
        /// <summary>
        /// Obtém lista de todos os inputs disponíveis
        /// </summary>
        public IEnumerable<string> GetAllInputNames()
        {
            return _cache.GetInputNames();
        }
        
        /// <summary>
        /// Atualiza os estados e visualização
        /// </summary>
        private void LateUpdate()
        {
            _cache?.UpdateStates();
        }
        
        /// <summary>
        /// Desenha visualizadores na tela
        /// </summary>
        private void OnGUI()
        {
            if (inputReader != null && inputReader.EnableVisualDebug && _visualizer != null)
            {
                _visualizer.DrawHandles(_cache);
            }
        }
        
        /// <summary>
        /// Limpa recursos ao destruir
        /// </summary>
        private void OnDestroy()
        {
            RestoreCursorState();
            
            PlugInputDeviceManager.OnDeviceChanged -= HandleDeviceChanged;
            PlugInputDeviceManager.OnDeviceConnected -= HandleDeviceConnected;
            PlugInputDeviceManager.OnDeviceDisconnected -= HandleDeviceDisconnected;
            PlugInputDeviceManager.OnDeviceTypeFiltered -= HandleDeviceFiltered;
            
            OnInputSystemDestroyed?.Invoke();
            OnInputPerformed = null;
            OnInputCanceled = null;
            OnInputPressed = null;
            OnInputReleased = null;
            OnInputValueChanged = null;
            OnInputVector2Changed = null;
            OnInputStateChanged = null;
            OnInputSystemInitialized = null;
            OnInputSystemDestroyed = null;
            OnDeviceChanged = null;
            OnDeviceConnected = null;
            OnDeviceDisconnected = null;
            OnDeviceFiltered = null;
            
            _lastValues?.Clear();
            _cache?.Dispose();
            _debugger?.Clear();
            _deviceManager?.Dispose();
            
            if (inputReader?.InputActionAsset != null)
            {
                foreach (var actionMap in inputReader.InputActionAsset.actionMaps)
                {
                    foreach (var action in actionMap.actions)
                    {
                        action.performed -= OnActionPerformed;
                        action.canceled -= OnActionCanceled;
                    }
                }
            }
        }
        
        /// <summary>
        /// Restaura estado original do cursor
        /// </summary>
        private void RestoreCursorState()
        {
            Cursor.lockState = _originalCursorLockMode;
            Cursor.visible = _originalCursorVisible;
        }
    }
}