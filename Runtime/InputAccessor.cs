using UnityEngine;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Provides a clean API to access a single input's current state.
    /// Reads directly from InputState's typed properties — zero boxing, zero allocation.
    /// Instances are pooled by PlugInputCache and reused across frames.
    /// </summary>
    public class InputAccessor : IDisposable
    {
        private InputState _state;
        private bool _isDisposed;

        public InputAccessor(InputState state)
        {
            Initialize(state);
        }

        public void Initialize(InputState state)
        {
            _state = state;
            _isDisposed = false;
        }

        public void Reset()
        {
            _state = null;
            _isDisposed = false;
        }

        // --- Identity ---

        public string Name      => _state?.Name      ?? string.Empty;
        public string InputType => _state?.InputType ?? string.Empty;
        public bool   IsValid   => !_isDisposed && _state != null;

        // --- Typed properties (no alloc) ---

        public Vector2 Vector2  { get { CheckDisposed(); return _state?.AsVector2 ?? Vector2.zero; } }
        public Vector3 Vector3  { get { CheckDisposed(); return _state?.AsVector3 ?? Vector3.zero; } }
        public float   Float    { get { CheckDisposed(); return _state?.AsFloat   ?? 0f; } }
        public bool    Bool     { get { CheckDisposed(); return _state?.AsBool    ?? false; } }
        public int     Int      { get { CheckDisposed(); return _state?.AsInt     ?? 0; } }

        // --- Frame states ---

        public bool Pressed   { get { CheckDisposed(); return _state?.PressedThisFrame  ?? false; } }
        public bool Released  { get { CheckDisposed(); return _state?.ReleasedThisFrame ?? false; } }
        public bool IsPressed { get { CheckDisposed(); return _state?.IsPressed         ?? false; } }

        // --- Implicit conversions (no alloc) ---

        public static implicit operator Vector2(InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? Vector2.zero : a._state.AsVector2;
        public static implicit operator Vector3(InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? Vector3.zero : a._state.AsVector3;
        public static implicit operator float  (InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? 0f : a._state.AsFloat;
        public static implicit operator bool   (InputAccessor a) => a is { _isDisposed: false, _state: { AsBool: true } };
        public static implicit operator int    (InputAccessor a) => (a == null || a._isDisposed || a._state == null) ? 0 : a._state.AsInt;

        // --- Debug (alloc acceptable — only called from debug paths) ---

        /// <summary>Returns the underlying InputValue for debug display. Avoid calling every frame in production.</summary>
        public InputValue RawValue { get { CheckDisposed(); return _state?.CurrentValue ?? default; } }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(InputAccessor));
        }

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