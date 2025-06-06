using UnityEngine;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Fornece acesso dinâmico e simplificado a um input específico.
    /// </summary>
    public class InputAccessor : IDisposable
    {
        private InputState _state;
        private bool _isDisposed = false;
        
        /// <summary>
        /// Cria um novo acessor de input
        /// </summary>
        public InputAccessor(InputState state)
        {
            Initialize(state);
        }
        
        /// <summary>
        /// Inicializa ou reinicializa o accessor com um novo estado
        /// </summary>
        public void Initialize(InputState state)
        {
            _state = state;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Reseta o accessor para reutilização no pool
        /// </summary>
        public void Reset()
        {
            _state = null;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Nome do input
        /// </summary>
        public string Name => _state?.Name ?? string.Empty;
        
        /// <summary>
        /// Conversão implícita para Vector2
        /// </summary>
        public static implicit operator Vector2(InputAccessor accessor)
        {
            if (accessor == null || accessor._isDisposed || accessor._state == null)
                return Vector2.zero;
            return accessor._state.AsVector2;
        }
        
        /// <summary>
        /// Conversão implícita para Vector3
        /// </summary>
        public static implicit operator Vector3(InputAccessor accessor)
        {
            if (accessor == null || accessor._isDisposed || accessor._state == null)
                return Vector3.zero;
            return accessor._state.AsVector3;
        }
        
        /// <summary>
        /// Conversão implícita para float
        /// </summary>
        public static implicit operator float(InputAccessor accessor)
        {
            if (accessor == null || accessor._isDisposed || accessor._state == null)
                return 0f;
            return accessor._state.AsFloat;
        }
        
        /// <summary>
        /// Conversão implícita para bool
        /// </summary>
        public static implicit operator bool(InputAccessor accessor)
        {
            if (accessor == null || accessor._isDisposed || accessor._state == null)
                return false;
            return accessor._state.AsBool;
        }
        
        /// <summary>
        /// Conversão implícita para int
        /// </summary>
        public static implicit operator int(InputAccessor accessor)
        {
            if (accessor == null || accessor._isDisposed || accessor._state == null)
                return 0;
            return accessor._state.AsInt;
        }
        
        /// <summary>
        /// Obtém o valor como Vector2
        /// </summary>
        public Vector2 Vector2 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.AsVector2 ?? Vector2.zero;
            }
        }
        
        /// <summary>
        /// Obtém o valor como Vector3
        /// </summary>
        public Vector3 Vector3 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.AsVector3 ?? Vector3.zero;
            }
        }
        
        /// <summary>
        /// Obtém o valor como float
        /// </summary>
        public float Float 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.AsFloat ?? 0f;
            }
        }
        
        /// <summary>
        /// Obtém o valor como bool
        /// </summary>
        public bool Bool 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.AsBool ?? false;
            }
        }
        
        /// <summary>
        /// Obtém o valor como int
        /// </summary>
        public int Int 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.AsInt ?? 0;
            }
        }
        
        /// <summary>
        /// Verifica se foi pressionado neste frame
        /// </summary>
        public bool Pressed 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.PressedThisFrame ?? false;
            }
        }
        
        /// <summary>
        /// Verifica se foi solto neste frame
        /// </summary>
        public bool Released 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.ReleasedThisFrame ?? false;
            }
        }
        
        /// <summary>
        /// Verifica se está sendo pressionado
        /// </summary>
        public bool IsPressed 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.IsPressed ?? false;
            }
        }
        
        /// <summary>
        /// Valor bruto para debugging
        /// </summary>
        public object RawValue 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.RawValue;
            }
        }
        
        /// <summary>
        /// Tipo de input
        /// </summary>
        public string InputType 
        { 
            get
            {
                ThrowIfDisposed();
                return _state?.InputType ?? string.Empty;
            }
        }
        
        /// <summary>
        /// Verifica se o accessor é válido
        /// </summary>
        public bool IsValid => !_isDisposed && _state != null;
        
        /// <summary>
        /// Lança exceção se o objeto foi disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(InputAccessor));
        }
        
        /// <summary>
        /// Dispose do accessor
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _state = null;
                _isDisposed = true;
            }
        }
    }
}