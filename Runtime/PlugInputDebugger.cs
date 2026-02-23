using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Debug logger for PlugInputPack.
    /// Emits rich-text colored logs and suppresses redundant entries for continuous inputs
    /// (mouse position, stick axes) so the console doesn't flood with near-identical values.
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

        // ── Rich-text color palette ──────────────────────────────────────────────
        private const string ColPrefix  = "<color=#5B9BD5><b>[PlugInput]</b></color>";
        private const string ColPerform = "<color=#6A9955>▲</color>";
        private const string ColCancel  = "<color=#F44747>▼</color>";

        // ── State ────────────────────────────────────────────────────────────────
        private bool       _isEnabled;
        private DebugLevel _debugLevel = DebugLevel.Basic;

        // Reused to avoid alloc on every formatted line
        private readonly StringBuilder _sb = new StringBuilder(256);

        // Last logged value per action — used to detect meaningful changes
        private readonly Dictionary<string, InputValue> _lastValues     = new Dictionary<string, InputValue>();
        private readonly Dictionary<string, float>      _lastTimestamps = new Dictionary<string, float>();
        private readonly Dictionary<string, int>        _counts         = new Dictionary<string, int>();
        private readonly HashSet<string>                _filtered       = new HashSet<string>();
        private readonly List<string>                   _active         = new List<string>();

        // Continuous inputs (Vector2 axes, floats) throttle: minimum angular/magnitude
        // change required before a new log is emitted.  Prevents flooding from mouse
        // delta or stick axes changing by 1–2 units every frame.
        private const float ContinuousAngleThresholdDeg  = 15f;   // direction must rotate ≥15° OR
        private const float ContinuousMagnitudeThreshold = 0.15f; // magnitude must shift ≥0.15
        private const float ContinuousTimeThreshold      = 0.08f; // …but at most every 80ms anyway

        // ────────────────────────────────────────────────────────────────────────

        public void SetEnabled(bool enabled) { _isEnabled = enabled; if (!enabled) Clear(); }

        // ── Rich-text color wrappers — no AppendFormat, no params object[] alloc ──
        // Each method wraps its string arg directly with Append calls.
        private void AppendColored(string openTag, string closeTag, string value)
        {
            _sb.Append(openTag);
            _sb.Append(value);
            _sb.Append(closeTag);
        }

        private const string OpenAction  = "<color=#C8C8C8><b>";
        private const string OpenValue   = "<color=#4EC9B0>";
        private const string OpenDevice  = "<color=#DCDCAA>";
        private const string OpenSystem  = "<color=#9CDCFE>";
        private const string CloseColor  = "</color>";
        private const string CloseColorB = "</b></color>";

        // ── Main entry point ─────────────────────────────────────────────────────

        /// <summary>
        /// Called from PlugInputComponent on every performed/canceled callback.
        /// Decides whether to emit a log based on how much the value changed.
        /// </summary>
        public void LogInputActivity(string actionName, InputValue value, bool isPerformed)
        {
            if (!_isEnabled || _filtered.Contains(actionName)) return;
            if (!ShouldLog(actionName, value, isPerformed))    return;

            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ");
            AppendColored(OpenAction, CloseColorB, actionName);
            _sb.Append("  <color=#555555>=</color>  ");
            AppendColored(OpenValue, CloseColor, FormatValue(value));
            _sb.Append("  ");
            _sb.Append(isPerformed ? ColPerform : ColCancel);

            Debug.Log(_sb.ToString());

            _lastValues[actionName]     = value;
            _lastTimestamps[actionName] = Time.unscaledTime;

            if (_debugLevel.HasFlag(DebugLevel.Performance))
                _counts[actionName] = (_counts.TryGetValue(actionName, out int c) ? c : 0) + 1;

            if (isPerformed) { if (!_active.Contains(actionName)) _active.Add(actionName); }
            else _active.Remove(actionName);
        }

        // ── System-level log helpers ─────────────────────────────────────────────

        public void LogReady(int maps, int actions)
        {
            if (!_isEnabled) return;
            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ");
            _sb.Append(OpenSystem).Append("Ready — ").Append(maps).Append(" maps, ").Append(actions).Append(" actions").Append(CloseColor);
            Debug.Log(_sb.ToString());
        }

        public void LogDeviceChanged(string previous, string current)
        {
            if (!_isEnabled) return;
            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ");
            _sb.Append(OpenSystem).Append("Device").Append(CloseColor);
            _sb.Append("  ");
            AppendColored(OpenDevice, CloseColor, previous);
            _sb.Append("  <color=#555555>→</color>  ");
            AppendColored(OpenDevice, CloseColor, current);
            Debug.Log(_sb.ToString());
        }

        public void LogDeviceEvent(string verb, string deviceName)
        {
            if (!_isEnabled) return;
            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ");
            _sb.Append(OpenSystem).Append(verb).Append(CloseColor);
            _sb.Append("  ");
            AppendColored(OpenDevice, CloseColor, deviceName);
            Debug.Log(_sb.ToString());
        }

        // ── Change detection ─────────────────────────────────────────────────────

        private bool ShouldLog(string actionName, InputValue value, bool isPerformed)
        {
            if (!_lastValues.TryGetValue(actionName, out InputValue last))
                return true; // first event for this action — always log

            // Bool and int: log on any change
            if (value.Kind == InputValue.ValueKind.Bool || value.Kind == InputValue.ValueKind.Int)
                return !value.Equals(last);

            // Float: log if change > small deadband
            if (value.Kind == InputValue.ValueKind.Float)
                return Mathf.Abs(value.FloatVal - last.FloatVal) > 0.05f;

            // Vector2 / Vector3 (continuous axes like mouse delta, stick):
            // Suppress if the direction hasn't rotated much AND magnitude hasn't changed much.
            // This prevents flooding on small mouse movements while still capturing
            // meaningful direction changes (e.g. flick left → right).
            if (value.Kind == InputValue.ValueKind.Vector2)
            {
                float timeSinceLast = Time.unscaledTime - (_lastTimestamps.TryGetValue(actionName, out float t) ? t : 0f);
                if (timeSinceLast < ContinuousTimeThreshold) return false;

                Vector2 curr = value.Vec2Val;
                Vector2 prev = last.Vec2Val;

                float magDelta = Mathf.Abs(curr.magnitude - prev.magnitude);
                if (magDelta > ContinuousMagnitudeThreshold) return true;

                if (curr.sqrMagnitude > 0.001f && prev.sqrMagnitude > 0.001f)
                {
                    float angle = Vector2.Angle(curr, prev);
                    if (angle > ContinuousAngleThresholdDeg) return true;
                }

                // Transitioning to/from zero is always significant
                bool wasZero = prev.sqrMagnitude < 0.001f;
                bool isZero  = curr.sqrMagnitude < 0.001f;
                return wasZero != isZero;
            }

            if (value.Kind == InputValue.ValueKind.Vector3)
            {
                float timeSinceLast = Time.unscaledTime - (_lastTimestamps.TryGetValue(actionName, out float t) ? t : 0f);
                if (timeSinceLast < ContinuousTimeThreshold) return false;
                return (value.Vec3Val - last.Vec3Val).magnitude > ContinuousMagnitudeThreshold;
            }

            return !value.Equals(last);
        }

        // ── Value formatting ─────────────────────────────────────────────────────

        private static string FormatValue(InputValue v)
        {
            switch (v.Kind)
            {
                case InputValue.ValueKind.Bool:    return v.BoolVal ? "true" : "false";
                case InputValue.ValueKind.Float:   return v.FloatVal.ToString("F2");
                case InputValue.ValueKind.Int:     return v.IntVal.ToString();
                case InputValue.ValueKind.Vector2: return $"({v.Vec2Val.x:F1}, {v.Vec2Val.y:F1})";
                case InputValue.ValueKind.Vector3: return $"({v.Vec3Val.x:F1}, {v.Vec3Val.y:F1}, {v.Vec3Val.z:F1})";
                default:                           return "?";
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void Clear()
        {
            _lastValues.Clear();
            _lastTimestamps.Clear();
            _counts.Clear();
            _active.Clear();
            _sb.Clear();
        }

        public void SetDebugLevel(DebugLevel level) => _debugLevel = level;
        public void AddInputFilter(string name)    { if (!string.IsNullOrEmpty(name)) _filtered.Add(name); }
        public void RemoveInputFilter(string name) => _filtered.Remove(name);
        public void ClearFilters()                 => _filtered.Clear();

        public struct DebugInfo
        {
            public bool       IsEnabled;
            public DebugLevel Level;
            public int        ActiveCount;
            public int        FilteredCount;
            public int        TotalLogged;
        }

        public DebugInfo GetDebugInfo()
        {
            int total = 0;
            foreach (int v in _counts.Values) total += v;
            return new DebugInfo
            {
                IsEnabled    = _isEnabled,
                Level        = _debugLevel,
                ActiveCount  = _active.Count,
                FilteredCount = _filtered.Count,
                TotalLogged  = total
            };
        }
    }
}