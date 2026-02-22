using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Debug helper for PlugInputPack.
    /// Uses InputValue for last-value tracking — no boxing on the hot path.
    /// String formatting (alloc) only happens when a log is actually emitted.
    /// </summary>
    public class PlugInputDebugger
    {
        [Flags]
        public enum DebugLevel
        {
            None        = 0,
            Basic       = 1,
            Detailed    = 2,
            Performance = 4,
            All         = Basic | Detailed | Performance
        }

        private bool _isEnabled;
        private DebugLevel _debugLevel = DebugLevel.Basic;

        // StringBuilder is reused to avoid alloc on every log line
        private readonly StringBuilder _logBuffer = new StringBuilder(512);

        // InputValue instead of object — no boxing
        private readonly Dictionary<string, InputValue> _lastLoggedValues = new Dictionary<string, InputValue>();
        private readonly List<string>                   _activeInputs     = new List<string>();
        private readonly Dictionary<string, float>      _inputTimestamps  = new Dictionary<string, float>();
        private readonly Dictionary<string, int>        _inputCounts      = new Dictionary<string, int>();
        private readonly HashSet<string>                _filteredInputs   = new HashSet<string>();

        private float _minimumLogInterval = 0.1f;

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled) Clear();
        }

        /// <summary>
        /// Logs an input event. Only emits a Debug.Log when the value changed significantly.
        /// The string format (alloc) only happens when the log is actually written.
        /// </summary>
        public void LogInputActivity(string actionName, InputValue value, bool isPerformed)
        {
            if (!_isEnabled || _filteredInputs.Contains(actionName)) return;
            if (!IsSignificantChange(actionName, value))              return;
            if (!CanLogInput(actionName))                             return;

            // StringBuilder reuse — one alloc for the final string passed to Debug.Log
            _logBuffer.Clear();
            _logBuffer.Append("[PlugInput] ");
            _logBuffer.Append(actionName);
            _logBuffer.Append(" = ");
            _logBuffer.Append(value.ToString()); // alloc here is intentional — debug only

            if (isPerformed)
            {
                _logBuffer.Append(" (Performed)");
                if (!_activeInputs.Contains(actionName)) _activeInputs.Add(actionName);
            }
            else
            {
                _logBuffer.Append(" (Canceled)");
                _activeInputs.Remove(actionName);
            }

            Debug.Log(_logBuffer.ToString());
            _lastLoggedValues[actionName] = value;
            UpdateInputStats(actionName, isPerformed);
        }

        private bool IsSignificantChange(string actionName, InputValue value)
        {
            if (!_lastLoggedValues.TryGetValue(actionName, out InputValue last))
                return true;

            // InputValue.Equals is a struct comparison — zero alloc
            return !value.Equals(last);
        }

        private bool CanLogInput(string actionName)
        {
            if (_minimumLogInterval <= 0f) return true;
            float currentTime = Time.unscaledTime;
            if (_inputTimestamps.TryGetValue(actionName, out float lastTime))
                return (currentTime - lastTime) >= _minimumLogInterval;
            return true;
        }

        private void UpdateInputStats(string actionName, bool isPerformed)
        {
            if (!_debugLevel.HasFlag(DebugLevel.Performance)) return;
            if (isPerformed)
                _inputCounts[actionName] = (_inputCounts.TryGetValue(actionName, out int c) ? c : 0) + 1;
            _inputTimestamps[actionName] = Time.unscaledTime;
        }

        public void Clear()
        {
            _lastLoggedValues.Clear();
            _activeInputs.Clear();
            _inputTimestamps.Clear();
            _inputCounts.Clear();
            _logBuffer.Clear();
        }

        public void SetDebugLevel(DebugLevel level) => _debugLevel = level;
        public void SetMinimumLogInterval(float interval) => _minimumLogInterval = Mathf.Max(0f, interval);
        public void AddInputFilter(string inputName)    { if (!string.IsNullOrEmpty(inputName)) _filteredInputs.Add(inputName); }
        public void RemoveInputFilter(string inputName) => _filteredInputs.Remove(inputName);
        public void ClearFilters()                      => _filteredInputs.Clear();

        public struct DebugInfo
        {
            public bool IsEnabled;
            public DebugLevel DebugLevel;
            public int ActiveInputsCount;
            public int FilteredInputsCount;
            public int TotalEventsLogged;
            public float MinimumLogInterval;
        }

        public DebugInfo GetDebugInfo()
        {
            int total = 0;
            foreach (int v in _inputCounts.Values) total += v;
            return new DebugInfo
            {
                IsEnabled           = _isEnabled,
                DebugLevel          = _debugLevel,
                ActiveInputsCount   = _activeInputs.Count,
                FilteredInputsCount = _filteredInputs.Count,
                TotalEventsLogged   = total,
                MinimumLogInterval  = _minimumLogInterval
            };
        }
    }
}