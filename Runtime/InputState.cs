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
        private bool _suspended;

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

            if (_suspended)
            {
                ClearTransientState();
                return;
            }

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

            if (_suspended)
            {
                ClearTransientState();
                return;
            }

            if (!_currentValue.IsActive() && _previousValue.IsActive())
            {
                _releasedThisFrameBuffer = true;
                _releasedPending         = true;
            }
        }

        public (bool pressed, bool released) Flush()
        {
            if (_suspended)
            {
                ClearTransientState();
                return (false, false);
            }

            _pressedThisFrame  = _pressedThisFrameBuffer;
            _releasedThisFrame = _releasedThisFrameBuffer;
            _pressedThisFrameBuffer  = false;
            _releasedThisFrameBuffer = false;
            return (_pressedThisFrame, _releasedThisFrame);
        }

        public bool TakePress()
        {
            if (_suspended || !_pressedPending) return false;
            _pressedPending = false;
            return true;
        }

        public bool TakeRelease()
        {
            if (_suspended || !_releasedPending) return false;
            _releasedPending = false;
            return true;
        }

        internal void SetSuspended(bool suspended)
        {
            if (_suspended == suspended) return;
            _suspended = suspended;
            ClearTransientState();
        }

        private void ClearTransientState()
        {
            _pressedThisFrame = false;
            _releasedThisFrame = false;
            _pressedThisFrameBuffer = false;
            _releasedThisFrameBuffer = false;
            _pressedPending = false;
            _releasedPending = false;
        }

        public void Dispose()
        {
            _action.performed -= OnActionPerformed;
            _action.canceled  -= OnActionCanceled;
        }

        public string Name      => _action.name;
        public string InputType => _inputType;

        internal string ActionMapName => _action.actionMap?.name ?? string.Empty;
        internal bool IsSuspended => _suspended;

        public bool IsPressed         => !_suspended && _currentValue.IsActive();
        public bool PressedThisFrame  => !_suspended && _pressedThisFrame;
        public bool ReleasedThisFrame => !_suspended && _releasedThisFrame;

        public bool    AsBool    => !_suspended && _currentValue.AsBool();
        public float   AsFloat   => _suspended ? 0f : _currentValue.AsFloat();
        public int     AsInt     => _suspended ? 0 : _currentValue.AsInt();
        public Vector2 AsVector2 => _suspended ? Vector2.zero : _currentValue.AsVector2();
        public Vector3 AsVector3 => _suspended ? Vector3.zero : _currentValue.AsVector3();

        public InputValue CurrentValue  => _suspended ? PlugInputProcessor.GetDefaultValue(_inputType) : _currentValue;
        public InputValue PreviousValue => _suspended ? PlugInputProcessor.GetDefaultValue(_inputType) : _previousValue;

        public string GetDebugString() => CurrentValue.ToString();
    }
}
