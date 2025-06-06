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
        
        public static event Action<string, object> OnInputPerformed;
        public static event Action<string> OnInputCanceled;
        public static event Action<string> OnInputPressed;   
        public static event Action<string> OnInputReleased;  
        public static event Action<string, float> OnInputValueChanged;
        public static event Action<string, Vector2> OnInputVector2Changed;
        public static event Action<string, bool> OnInputStateChanged;
        public static event Action OnInputSystemInitialized;
        public static event Action OnInputSystemDestroyed;
        
        private Dictionary<string, object> _lastValues = new Dictionary<string, object>();
        
        private void Awake()
        {
            _cache = new PlugInputCache();
            _debugger = new PlugInputDebugger();
            _visualizer = new PlugInputVisualizer();
            
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
            
            RegisterAllInputs(actionAsset);
            
            OnInputSystemInitialized?.Invoke();
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
            
            _lastValues?.Clear();
            _cache?.Dispose();
            _debugger?.Clear();
            
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
    }
}