using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Armazena o estado atual de um input específico.
    /// </summary>
    public class InputState
    {
        private readonly InputAction _action;
        private readonly string _inputType;
        private object _currentValue;
        private object _previousValue;
        private bool _pressedThisFrame;
        private bool _releasedThisFrame;
        private bool _pressedThisFrameBuffer;
        private bool _releasedThisFrameBuffer;
        /// <summary>
        /// Cria um novo estado de input
        /// </summary>
        public InputState(InputAction action)
        {
            _action = action;
            _inputType = action.expectedControlType;
            
            // Configura callbacks
            _action.performed += OnActionPerformed;
            _action.canceled += OnActionCanceled;
            
            // Inicializa com valores padrão
            _currentValue = GetDefaultValue();
            _previousValue = _currentValue;
        }
        
        /// <summary>
        /// Chamado quando uma ação é executada
        /// </summary>
        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            _previousValue = _currentValue;
            _currentValue = PlugInputProcessor.ReadValue(context, _inputType);
            
            // Detecta se foi pressionado neste frame
            if (IsPressed && !WasPressed)
            {
                _pressedThisFrameBuffer = true;
            }
        }
        
        /// <summary>
        /// Chamado quando uma ação é cancelada
        /// </summary>
        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            _previousValue = _currentValue;
            _currentValue = GetDefaultValue();
            
            // Detecta se foi liberado neste frame
            if (!IsPressed && WasPressed)
            {
                _releasedThisFrameBuffer = true;
            }
        }
        
        /// <summary>
        /// Obtém o valor padrão para o tipo de input
        /// </summary>
        private object GetDefaultValue()
        {
            return PlugInputProcessor.GetDefaultValue(_inputType);
        }
        
        /// <summary>
        /// Atualiza o estado do frame
        /// </summary>
        public void Update()
        {
            // Aplica o buffer
            _pressedThisFrame = _pressedThisFrameBuffer;
            _releasedThisFrame = _releasedThisFrameBuffer;
            
            // Reset do buffer para próximo frame
            _pressedThisFrameBuffer = false;
            _releasedThisFrameBuffer = false;
        }
        
        /// <summary>
        /// Remove os callbacks ao destruir
        /// </summary>
        public void Dispose()
        {
            _action.performed -= OnActionPerformed;
            _action.canceled -= OnActionCanceled;
        }
        
        /// <summary>
        /// Nome da ação
        /// </summary>
        public string Name => _action.name;
        
        /// <summary>
        /// O tipo de input esperado para esta ação
        /// </summary>
        public string InputType => _inputType;
        
        /// <summary>
        /// Verifica se o input está pressionado
        /// </summary>
        public bool IsPressed => PlugInputProcessor.IsValueActive(_currentValue);
        
        /// <summary>
        /// Verifica se o input estava pressionado no frame anterior
        /// </summary>
        private bool WasPressed => PlugInputProcessor.IsValueActive(_previousValue);
        
        /// <summary>
        /// Verifica se o input foi pressionado neste frame
        /// </summary>
        public bool PressedThisFrame => _pressedThisFrame;
        
        /// <summary>
        /// Verifica se o input foi liberado neste frame
        /// </summary>
        public bool ReleasedThisFrame => _releasedThisFrame;
        
        /// <summary>
        /// Obtém o valor como Vector2
        /// </summary>
        public Vector2 AsVector2 => PlugInputProcessor.ConvertToVector2(_currentValue);
        
        /// <summary>
        /// Obtém o valor como Vector3
        /// </summary>
        public Vector3 AsVector3 => PlugInputProcessor.ConvertToVector3(_currentValue);
        
        /// <summary>
        /// Obtém o valor como float
        /// </summary>
        public float AsFloat => PlugInputProcessor.ConvertToFloat(_currentValue);
        
        /// <summary>
        /// Obtém o valor como bool
        /// </summary>
        public bool AsBool => PlugInputProcessor.ConvertToBool(_currentValue);
        
        /// <summary>
        /// Obtém o valor como int
        /// </summary>
        public int AsInt => PlugInputProcessor.ConvertToInt(_currentValue);
        
        /// <summary>
        /// Valor atual sem conversão
        /// </summary>
        public object RawValue => _currentValue;
        
        /// <summary>
        /// Valor anterior sem conversão
        /// </summary>
        public object PreviousValue => _previousValue;
    }
}