using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace PlugInputPack
{
    /// <summary>
    /// Componente principal do Plug Input Pack, oferecendo acesso super simplificado aos inputs.
    /// </summary>
    public class PlugInputComponent : MonoBehaviour
    {
        [SerializeField] 
        private PlugInputReader inputReader;
        
        // Componentes internos
        private PlugInputCache _cache;
        private PlugInputDebugger _debugger;
        private PlugInputVisualizer _visualizer;
        
        private void Awake()
        {
            // Inicializa componentes
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
            
            // Configura debug
            _debugger.SetEnabled(inputReader.EnableDebug);
            
            // Configura visualizador
            _visualizer.Initialize(
                inputReader.EnableVisualDebug, 
                inputReader.DebugHandleSize, 
                inputReader.DebugHandleColor
            );
            
            // Registra todas as ações
            RegisterAllInputs(actionAsset);
        }
        
        /// <summary>
        /// Registra todas as ações do Input System
        /// </summary>
        private void RegisterAllInputs(InputActionAsset actionAsset)
        {
            foreach (var actionMap in actionAsset.actionMaps)
            {
                foreach (var action in actionMap.actions)
                {
                    // Configura callbacks
                    action.performed += OnActionPerformed;
                    action.canceled += OnActionCanceled;
                    
                    // Registra no cache
                    _cache.RegisterState(action);
                    
                    // Habilita a ação
                    action.Enable();
                }
            }
            
            if (inputReader.EnableDebug)
            {
                Debug.Log($"PlugInputPack: Sistema inicializado com {actionAsset.actionMaps.Count} mapas de ação.");
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
                // Debug
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
                // Debug
                if (inputReader.EnableDebug)
                {
                    _debugger.LogInputActivity(actionName, state.RawValue, false);
                }
            }
        }
        
        /// <summary>
        /// Acessa um input pelo nome usando a sintaxe input["Action"]
        /// </summary>
        public InputAccessor this[string actionName]
        {
            get
            {
                return _cache.GetAccessor(actionName);
            }
        }
        
        /// <summary>
        /// Acessa um input pelo nome usando a sintaxe input("Action")
        /// Implementação do operador de invocação para uma sintaxe mais natural
        /// </summary>
        public InputAccessor this[string actionName, bool dummy = false] 
        => this[actionName];
        
        /// <summary>
        /// Atualiza os estados e visualização
        /// </summary>
        private void LateUpdate()
        {
            _cache.UpdateStates();
        }
        
        /// <summary>
        /// Desenha visualizadores na tela
        /// </summary>
        private void OnGUI()
        {
            if (inputReader != null && inputReader.EnableVisualDebug)
            {
                // Desenha apenas a nova interface minimalista
                _visualizer.DrawHandles(_cache);
            }
        }
        
        /// <summary>
        /// Limpa recursos ao destruir
        /// </summary>
        private void OnDestroy()
        {
            if (_cache != null)
            {
                _cache.Dispose();
            }
            
            if (_debugger != null)
            {
                _debugger.Clear();
            }
        }
    }
}