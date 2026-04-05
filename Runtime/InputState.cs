using UnityEngine;
using UnityEngine.InputSystem;

namespace PlugInputPack
{
    /// <summary>
    /// Stores the current state of a single input action.
    /// Uses InputValue struct internally — no boxing, no heap allocation per frame.
    /// </summary>
    public class InputState
    {
        private readonly InputAction _action;
        private readonly string _inputType;

        private InputValue _currentValue;
        private InputValue _previousValue;

        private bool _pressedThisFrame;
        private bool _releasedThisFrame;
        private bool _pressedThisFrameBuffer;
        private bool _releasedThisFrameBuffer;

        // Persistent flags for FixedUpdate consumers.
        // Unlike PressedThisFrame (cleared next flush), these stay true until explicitly consumed.
        private bool _pressedPending;
        private bool _releasedPending;

        public InputState(InputAction action)
        {
            _action = action;
            _inputType = action.expectedControlType;
            _currentValue = PlugInputProcessor.GetDefaultValue(_inputType);
            _previousValue = _currentValue;

            _action.performed += OnActionPerformed;
            _action.canceled += OnActionCanceled;
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            _previousValue = _currentValue;
            _currentValue = PlugInputProcessor.ReadValue(context, _inputType);

            if (_currentValue.IsActive() && !_previousValue.IsActive())
            {
                _pressedThisFrameBuffer = true;
                _pressedPending = true;
            }
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            _previousValue = _currentValue;
            _currentValue = PlugInputProcessor.GetDefaultValue(_inputType);

            if (!_currentValue.IsActive() && _previousValue.IsActive())
            {
                _releasedThisFrameBuffer = true;
                _releasedPending = true;
            }
        }

        /// <summary>
        /// Flushes the per-frame pressed/released buffers.
        /// Must be called from Update (not LateUpdate) so that Pressed/Released
        /// are valid for the entire frame, including FixedUpdate calls within it.
        /// Returns whether a press or release occurred this frame so callers can fire events.
        /// </summary>
        public (bool pressed, bool released) Flush()
        {
            _pressedThisFrame = _pressedThisFrameBuffer;
            _releasedThisFrame = _releasedThisFrameBuffer;
            _pressedThisFrameBuffer = false;
            _releasedThisFrameBuffer = false;
            return (_pressedThisFrame, _releasedThisFrame);
        }

        /// <summary>
        /// Consumes the pending pressed flag. Returns true once per press event.
        /// Safe to call from FixedUpdate — survives until explicitly consumed.
        /// </summary>
        public bool ConsumePressedPending()
        {
            if (!_pressedPending) return false;
            _pressedPending = false;
            return true;
        }

        /// <summary>
        /// Consumes the pending released flag. Returns true once per release event.
        /// Safe to call from FixedUpdate — survives until explicitly consumed.
        /// </summary>
        public bool ConsumeReleasedPending()
        {
            if (!_releasedPending) return false;
            _releasedPending = false;
            return true;
        }

        public void Dispose()
        {
            _action.performed -= OnActionPerformed;
            _action.canceled  -= OnActionCanceled;
        }

        // --- Identity ---
        public string Name      => _action.name;
        public string InputType => _inputType;

        // --- State ---
        public bool IsPressed         => _currentValue.IsActive();
        public bool PressedThisFrame  => _pressedThisFrame;
        public bool ReleasedThisFrame => _releasedThisFrame;

        // --- Typed accessors (no alloc) ---
        public bool    AsBool    => _currentValue.AsBool();
        public float   AsFloat   => _currentValue.AsFloat();
        public int     AsInt     => _currentValue.AsInt();
        public Vector2 AsVector2 => _currentValue.AsVector2();
        public Vector3 AsVector3 => _currentValue.AsVector3();

        // --- Raw access for debug/events ---
        public InputValue CurrentValue  => _currentValue;
        public InputValue PreviousValue => _previousValue;

        /// <summary>
        /// Returns a formatted string for the debug overlay. Allocates — only call from debug paths.
        /// </summary>
        public string GetDebugString() => _currentValue.ToString();
    }
}