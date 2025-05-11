using UnityEngine;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Fornece acesso dinâmico e simplificado a um input específico.
    /// </summary>
    public class InputAccessor
    {
        private readonly InputState _state;
        
        /// <summary>
        /// Cria um novo acessor de input
        /// </summary>
        public InputAccessor(InputState state)
        {
            _state = state;
        }
        
        /// <summary>
        /// Nome do input
        /// </summary>
        public string Name => _state.Name;
        
        /// <summary>
        /// Conversão implícita para Vector2
        /// </summary>
        public static implicit operator Vector2(InputAccessor accessor)
        {
            return accessor?._state?.AsVector2 ?? Vector2.zero;
        }
        
        /// <summary>
        /// Conversão implícita para Vector3
        /// </summary>
        public static implicit operator Vector3(InputAccessor accessor)
        {
            return accessor?._state?.AsVector3 ?? Vector3.zero;
        }
        
        /// <summary>
        /// Conversão implícita para float
        /// </summary>
        public static implicit operator float(InputAccessor accessor)
        {
            return accessor?._state?.AsFloat ?? 0f;
        }
        
        /// <summary>
        /// Conversão implícita para bool
        /// </summary>
        public static implicit operator bool(InputAccessor accessor)
        {
            return accessor?._state?.AsBool ?? false;
        }
        
        /// <summary>
        /// Conversão implícita para int
        /// </summary>
        public static implicit operator int(InputAccessor accessor)
        {
            return accessor?._state?.AsInt ?? 0;
        }
        
        /// <summary>
        /// Obtém o valor como Vector2
        /// </summary>
        public Vector2 Vector2 => _state?.AsVector2 ?? Vector2.zero;
        
        /// <summary>
        /// Obtém o valor como Vector3
        /// </summary>
        public Vector3 Vector3 => _state?.AsVector3 ?? Vector3.zero;
        
        /// <summary>
        /// Obtém o valor como float
        /// </summary>
        public float Float => _state?.AsFloat ?? 0f;
        
        /// <summary>
        /// Obtém o valor como bool
        /// </summary>
        public bool Bool => _state?.AsBool ?? false;
        
        /// <summary>
        /// Obtém o valor como int
        /// </summary>
        public int Int => _state?.AsInt ?? 0;
        
        /// <summary>
        /// Verifica se foi pressionado neste frame
        /// </summary>
        public bool Pressed => _state?.PressedThisFrame ?? false;
        
        /// <summary>
        /// Verifica se foi solto neste frame
        /// </summary>
        public bool Released => _state?.ReleasedThisFrame ?? false;
        
        /// <summary>
        /// Verifica se está sendo pressionado
        /// </summary>
        public bool IsPressed => _state?.IsPressed ?? false;
        
        /// <summary>
        /// Valor bruto para debugging
        /// </summary>
        public object RawValue => _state?.RawValue;
        
        /// <summary>
        /// Tipo de input
        /// </summary>
        public string InputType => _state?.InputType;
    }
}