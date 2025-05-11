using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace PlugInputPack
{
    /// <summary>
    /// Sistema de depuração para o Plug Input Pack.
    /// </summary>
    public class PlugInputDebugger
    {
        private bool _isEnabled = false;
        private readonly StringBuilder _logBuffer = new StringBuilder(512);
        private readonly Dictionary<string, object> _lastLoggedValues = new Dictionary<string, object>();
        private readonly List<string> _activeInputs = new List<string>();
        
        /// <summary>
        /// Habilita ou desabilita o debug
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
        
        /// <summary>
        /// Registra atividade de input para debugging
        /// </summary>
        public void LogInputActivity(string actionName, object value, bool isPerformed)
        {
            if (!_isEnabled)
                return;
            
            bool isSignificantChange = IsSignificantChange(actionName, value);
            
            if (isSignificantChange)
            {
                _logBuffer.Clear();
                _logBuffer.Append("[PlugInput] ");
                _logBuffer.Append(actionName);
                _logBuffer.Append(" = ");
                _logBuffer.Append(FormatValue(value));
                
                if (isPerformed)
                {
                    _logBuffer.Append(" (Performed)");
                    if (!_activeInputs.Contains(actionName))
                        _activeInputs.Add(actionName);
                }
                else
                {
                    _logBuffer.Append(" (Canceled)");
                    _activeInputs.Remove(actionName);
                }
                
                Debug.Log(_logBuffer.ToString());
                _lastLoggedValues[actionName] = value;
            }
        }
        
        /// <summary>
        /// Determina se houve mudança significativa no valor
        /// </summary>
        private bool IsSignificantChange(string actionName, object value)
        {
            if (!_lastLoggedValues.TryGetValue(actionName, out object lastValue))
                return true;
            
            // Verificações específicas por tipo
            if (value is Vector2 v2Current && lastValue is Vector2 v2Last)
                return Vector2.Distance(v2Current, v2Last) > 0.1f;
            
            if (value is Vector3 v3Current && lastValue is Vector3 v3Last)
                return Vector3.Distance(v3Current, v3Last) > 0.1f;
            
            if (value is float fCurrent && lastValue is float fLast)
                return Mathf.Abs(fCurrent - fLast) > 0.1f;
            
            return !value.Equals(lastValue);
        }
        
        /// <summary>
        /// Formata um valor para exibição
        /// </summary>
        private string FormatValue(object value)
        {
            if (value is Vector2 v2)
                return $"({v2.x:F2}, {v2.y:F2})";
            
            if (value is Vector3 v3)
                return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
            
            if (value is float f)
                return f.ToString("F2");
            
            return value.ToString();
        }
        
        /// <summary>
        /// Gera informação de debug para exibição na tela
        /// </summary>
        public string GenerateDebugText()
        {
            if (!_isEnabled || _activeInputs.Count == 0)
                return string.Empty;
                
            _logBuffer.Clear();
            _logBuffer.AppendLine("-- INPUTS ATIVOS --");
            
            foreach (string input in _activeInputs)
            {
                _logBuffer.Append(input);
                
                if (_lastLoggedValues.TryGetValue(input, out object value))
                {
                    _logBuffer.Append(": ");
                    _logBuffer.Append(FormatValue(value));
                }
                
                _logBuffer.AppendLine();
            }
            
            return _logBuffer.ToString();
        }
        
        /// <summary>
        /// Limpa os dados de debug
        /// </summary>
        public void Clear()
        {
            _lastLoggedValues.Clear();
            _activeInputs.Clear();
        }
    }
}