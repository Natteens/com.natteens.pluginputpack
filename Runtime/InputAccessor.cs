using UnityEngine;
using System;

namespace PlugInputPack
{
    public class InputAccessor : IDisposable
    {
        private InputState _state;
        private bool _isDisposed;

        public InputAccessor(InputState state) { _state = state; }

        public string Name      => _state?.Name      ?? string.Empty;
        public string InputType => _state?.InputType ?? string.Empty;
        public bool   IsValid   => !_isDisposed && _state != null;

        public Vector2 Vector2  { get { CheckDisposed(); return _state?.AsVector2 ?? Vector2.zero; } }
        public Vector3 Vector3  { get { CheckDisposed(); return _state?.AsVector3 ?? Vector3.zero; } }
        public float   Float    { get { CheckDisposed(); return _state?.AsFloat   ?? 0f; } }
        public bool    Bool     { get { CheckDisposed(); return _state?.AsBool    ?? false; } }
        public int     Int      { get { CheckDisposed(); return _state?.AsInt     ?? 0; } }

        /// <summary>True no frame em que o botão foi pressionado. Use em Update.</summary>
        public bool JustPressed  { get { CheckDisposed(); return _state?.PressedThisFrame  ?? false; } }

        /// <summary>True no frame em que o botão foi solto. Use em Update.</summary>
        public bool JustReleased { get { CheckDisposed(); return _state?.ReleasedThisFrame ?? false; } }

        /// <summary>True enquanto o botão está segurado.</summary>
        public bool IsHeld       { get { CheckDisposed(); return _state?.IsPressed         ?? false; } }

        /// <summary>Consome um press pendente. Retorna true uma vez por press. Use em FixedUpdate.</summary>
        public bool TakePress()   { CheckDisposed(); return _state?.TakePress()   ?? false; }

        /// <summary>Consome um release pendente. Retorna true uma vez por release. Use em FixedUpdate.</summary>
        public bool TakeRelease() { CheckDisposed(); return _state?.TakeRelease() ?? false; }

        public static implicit operator Vector2(InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? Vector2.zero : a._state.AsVector2;
        public static implicit operator Vector3(InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? Vector3.zero : a._state.AsVector3;
        public static implicit operator float  (InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? 0f : a._state.AsFloat;
        public static implicit operator bool   (InputAccessor a) => a is { _isDisposed: false, _state: { AsBool: true } };
        public static implicit operator int    (InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? 0 : a._state.AsInt;

        /// <summary>Apenas para debug — aloca string.</summary>
        public InputValue RawValue { get { CheckDisposed(); return _state?.CurrentValue ?? default; } }

        private void CheckDisposed() { if (_isDisposed) throw new ObjectDisposedException(nameof(InputAccessor)); }

        public void Dispose()
        {
            if (!_isDisposed) { _state = null; _isDisposed = true; }
        }
    }
}
