using UnityEngine;
using UnityEngine.InputSystem;

namespace PlugInputPack
{
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

        private bool _pressedPending;
        private bool _releasedPending;

        public InputState(InputAction action)
        {
            _action    = action;
            _inputType = action.expectedControlType;
            _currentValue  = PlugInputProcessor.GetDefaultValue(_inputType);
            _previousValue = _currentValue;

            _action.performed += OnActionPerformed;
            _action.canceled  += OnActionCanceled;
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            _previousValue = _currentValue;
            _currentValue  = PlugInputProcessor.ReadValue(context, _inputType);

            if (_currentValue.IsActive() && !_previousValue.IsActive())
            {
                _pressedThisFrameBuffer = true;
                _pressedPending         = true;
            }
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            _previousValue = _currentValue;
            _currentValue  = PlugInputProcessor.GetDefaultValue(_inputType);

            if (!_currentValue.IsActive() && _previousValue.IsActive())
            {
                _releasedThisFrameBuffer = true;
                _releasedPending         = true;
            }
        }

        public (bool pressed, bool released) Flush()
        {
            _pressedThisFrame  = _pressedThisFrameBuffer;
            _releasedThisFrame = _releasedThisFrameBuffer;
            _pressedThisFrameBuffer  = false;
            _releasedThisFrameBuffer = false;
            return (_pressedThisFrame, _releasedThisFrame);
        }

        public bool TakePress()
        {
            if (!_pressedPending) return false;
            _pressedPending = false;
            return true;
        }

        public bool TakeRelease()
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

        public string Name      => _action.name;
        public string InputType => _inputType;

        public bool IsPressed         => _currentValue.IsActive();
        public bool PressedThisFrame  => _pressedThisFrame;
        public bool ReleasedThisFrame => _releasedThisFrame;

        public bool    AsBool    => _currentValue.AsBool();
        public float   AsFloat   => _currentValue.AsFloat();
        public int     AsInt     => _currentValue.AsInt();
        public Vector2 AsVector2 => _currentValue.AsVector2();
        public Vector3 AsVector3 => _currentValue.AsVector3();

        public InputValue CurrentValue  => _currentValue;
        public InputValue PreviousValue => _previousValue;

        public string GetDebugString() => _currentValue.ToString();
    }
}
