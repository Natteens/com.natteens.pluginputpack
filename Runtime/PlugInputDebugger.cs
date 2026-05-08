using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace PlugInputPack
{
    public class PlugInputDebugger
    {
        private const string ColPrefix  = "<color=#5B9BD5><b>[PlugInput]</b></color>";
        private const string ColPerform = "<color=#6A9955>▲</color>";
        private const string ColCancel  = "<color=#F44747>▼</color>";

        private const string OpenAction  = "<color=#C8C8C8><b>";
        private const string OpenValue   = "<color=#4EC9B0>";
        private const string OpenDevice  = "<color=#DCDCAA>";
        private const string OpenSystem  = "<color=#9CDCFE>";
        private const string CloseColor  = "</color>";
        private const string CloseColorB = "</b></color>";

        private bool _isEnabled;

        private readonly StringBuilder              _sb             = new(256);
        private readonly Dictionary<string, InputValue> _lastValues = new();
        private readonly Dictionary<string, float>      _lastTimes  = new();
        private readonly HashSet<string>                _filtered   = new();

        private const float ContinuousAngleDeg       = 15f;
        private const float ContinuousMagnitude      = 0.15f;
        private const float ContinuousTimeThreshold  = 0.08f;

        public void SetEnabled(bool enabled) { _isEnabled = enabled; if (!enabled) Clear(); }

        public void LogInputActivity(string actionName, InputValue value, bool isPerformed)
        {
            if (!_isEnabled || _filtered.Contains(actionName)) return;
            if (!ShouldLog(actionName, value, isPerformed)) return;

            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ");
            _sb.Append(OpenAction).Append(actionName).Append(CloseColorB);
            _sb.Append("  <color=#555555>=</color>  ");
            _sb.Append(OpenValue).Append(FormatValue(value)).Append(CloseColor);
            _sb.Append("  ").Append(isPerformed ? ColPerform : ColCancel);
            Debug.Log(_sb.ToString());

            _lastValues[actionName] = value;
            _lastTimes[actionName]  = Time.unscaledTime;
        }

        public void LogReady(int maps, int actions)
        {
            if (!_isEnabled) return;
            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ").Append(OpenSystem)
               .Append("Ready — ").Append(maps).Append(" maps, ").Append(actions).Append(" actions")
               .Append(CloseColor);
            Debug.Log(_sb.ToString());
        }

        public void LogDeviceChanged(string previous, string current)
        {
            if (!_isEnabled) return;
            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ").Append(OpenSystem).Append("Device").Append(CloseColor)
               .Append("  ").Append(OpenDevice).Append(previous).Append(CloseColor)
               .Append("  <color=#555555>→</color>  ")
               .Append(OpenDevice).Append(current).Append(CloseColor);
            Debug.Log(_sb.ToString());
        }

        public void LogDeviceEvent(string verb, string deviceName)
        {
            if (!_isEnabled) return;
            _sb.Clear();
            _sb.Append(ColPrefix).Append("  ").Append(OpenSystem).Append(verb).Append(CloseColor)
               .Append("  ").Append(OpenDevice).Append(deviceName).Append(CloseColor);
            Debug.Log(_sb.ToString());
        }

        private bool ShouldLog(string actionName, InputValue value, bool isPerformed)
        {
            if (!_lastValues.TryGetValue(actionName, out InputValue last)) return true;

            if (value.Kind == InputValue.ValueKind.Bool || value.Kind == InputValue.ValueKind.Int)
                return !value.Equals(last);

            if (value.Kind == InputValue.ValueKind.Float)
                return Mathf.Abs(value.FloatVal - last.FloatVal) > 0.05f;

            if (value.Kind == InputValue.ValueKind.Vector2)
            {
                float t = Time.unscaledTime - (_lastTimes.TryGetValue(actionName, out float lt) ? lt : 0f);
                if (t < ContinuousTimeThreshold) return false;

                Vector2 curr = value.Vec2Val, prev = last.Vec2Val;
                if (Mathf.Abs(curr.magnitude - prev.magnitude) > ContinuousMagnitude) return true;
                if (curr.sqrMagnitude > 0.001f && prev.sqrMagnitude > 0.001f &&
                    Vector2.Angle(curr, prev) > ContinuousAngleDeg) return true;
                return (prev.sqrMagnitude < 0.001f) != (curr.sqrMagnitude < 0.001f);
            }

            if (value.Kind == InputValue.ValueKind.Vector3)
            {
                float t = Time.unscaledTime - (_lastTimes.TryGetValue(actionName, out float lt) ? lt : 0f);
                if (t < ContinuousTimeThreshold) return false;
                return (value.Vec3Val - last.Vec3Val).magnitude > ContinuousMagnitude;
            }

            return !value.Equals(last);
        }

        private static string FormatValue(InputValue v) => v.Kind switch
        {
            InputValue.ValueKind.Bool    => v.BoolVal ? "true" : "false",
            InputValue.ValueKind.Float   => v.FloatVal.ToString("F2"),
            InputValue.ValueKind.Int     => v.IntVal.ToString(),
            InputValue.ValueKind.Vector2 => $"({v.Vec2Val.x:F1}, {v.Vec2Val.y:F1})",
            InputValue.ValueKind.Vector3 => $"({v.Vec3Val.x:F1}, {v.Vec3Val.y:F1}, {v.Vec3Val.z:F1})",
            _                            => "?"
        };

        public void Clear() { _lastValues.Clear(); _lastTimes.Clear(); _filtered.Clear(); _sb.Clear(); }
        public void AddInputFilter(string name)    { if (!string.IsNullOrEmpty(name)) _filtered.Add(name); }
        public void RemoveInputFilter(string name) => _filtered.Remove(name);
    }
}
