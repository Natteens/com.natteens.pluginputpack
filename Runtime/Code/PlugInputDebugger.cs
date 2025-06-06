using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Sistema de depuração aprimorado para o Plug Input Pack.
    /// </summary>
    public class PlugInputDebugger
    {
        [Flags]
        public enum DebugLevel
        {
            None = 0,
            Basic = 1,
            Detailed = 2,
            Performance = 4,
            All = Basic | Detailed | Performance
        }
        
        private bool _isEnabled = false;
        private DebugLevel _debugLevel = DebugLevel.Basic;
        private readonly StringBuilder _logBuffer = new StringBuilder(512);
        private readonly Dictionary<string, object> _lastLoggedValues = new Dictionary<string, object>();
        private readonly List<string> _activeInputs = new List<string>();
        private readonly Dictionary<string, float> _inputTimestamps = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _inputCounts = new Dictionary<string, int>();
        
        private readonly HashSet<string> _filteredInputs = new HashSet<string>();
        private float _minimumLogInterval = 0.1f;
        
        /// <summary>
        /// Habilita ou desabilita o debug
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                Clear(); 
            }
        }
        
        /// <summary>
        /// Registra atividade de input para debugging
        /// </summary>
        public void LogInputActivity(string actionName, object value, bool isPerformed)
        {
            if (!_isEnabled || _filteredInputs.Contains(actionName))
                return;
            
            bool isSignificantChange = IsSignificantChange(actionName, value);
            bool canLog = CanLogInput(actionName);
            
            if (isSignificantChange && canLog)
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
                UpdateInputStats(actionName, isPerformed);
            }
        }
        
        /// <summary>
        /// Determina se houve mudança significativa no valor
        /// </summary>
        private bool IsSignificantChange(string actionName, object value)
        {
            if (value == null)
                return false;
                
            if (!_lastLoggedValues.TryGetValue(actionName, out object lastValue))
                return true;
            
            if (lastValue == null)
                return true;
            
            Type valueType = value.GetType();
            Type lastValueType = lastValue.GetType();
            
            if (valueType != lastValueType)
                return true;
            
            switch (value)
            {
                case Vector2 v2Current when lastValue is Vector2 v2Last:
                    return Vector2.SqrMagnitude(v2Current - v2Last) > 0.01f;
                
                case Vector3 v3Current when lastValue is Vector3 v3Last:
                    return Vector3.SqrMagnitude(v3Current - v3Last) > 0.01f;
                
                case float fCurrent when lastValue is float fLast:
                    return Mathf.Abs(fCurrent - fLast) > 0.1f;
                
                case bool bCurrent when lastValue is bool bLast:
                    return bCurrent != bLast;
                
                case int iCurrent when lastValue is int iLast:
                    return iCurrent != iLast;
                
                default:
                    return !value.Equals(lastValue);
            }
        }
        
        /// <summary>
        /// Formata um valor para exibição
        /// </summary>
        private string FormatValue(object value)
        {
            switch (value)
            {
                case Vector2 v2:
                    return $"({v2.x:F2}, {v2.y:F2})";
                
                case Vector3 v3:
                    return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
                
                case float f:
                    return f.ToString("F2");
                
                case bool b:
                    return b.ToString();
                
                case null:
                    return "null";
                
                default:
                    return value.ToString();
            }
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
        /// Limpa todos os buffers
        /// </summary>
        public void Clear()
        {
            _lastLoggedValues.Clear();
            _activeInputs.Clear();
            _inputTimestamps.Clear();
            _inputCounts.Clear();
            _logBuffer.Clear();
        }
        
        /// <summary>
        /// Define o nível de debug 
        /// </summary>
        public void SetDebugLevel(DebugLevel level)
        {
            _debugLevel = level;
        }
        
        /// <summary>
        /// Adiciona um input ao filtro
        /// </summary>
        public void AddInputFilter(string inputName)
        {
            if (!string.IsNullOrEmpty(inputName))
            {
                _filteredInputs.Add(inputName);
            }
        }
        
        /// <summary>
        /// Remove um input do filtro
        /// </summary>
        public void RemoveInputFilter(string inputName)
        {
            _filteredInputs.Remove(inputName);
        }
        
        /// <summary>
        /// Limpa todos os filtros
        /// </summary>
        public void ClearFilters()
        {
            _filteredInputs.Clear();
        }
        
        /// <summary>
        /// Define o intervalo mínimo entre logs
        /// </summary>
        public void SetMinimumLogInterval(float interval)
        {
            _minimumLogInterval = Mathf.Max(0f, interval);
        }
        
        /// <summary>
        /// Versão aprimorada do log com mais informações
        /// </summary>
        public void LogInputActivityDetailed(string actionName, object value, bool isPerformed, string inputType = null)
        {
            if (!_isEnabled || _filteredInputs.Contains(actionName))
                return;
                
            if (!_debugLevel.HasFlag(DebugLevel.Detailed))
            {
                LogInputActivity(actionName, value, isPerformed);
                return;
            }
            
            bool isSignificantChange = IsSignificantChange(actionName, value);
            bool canLog = CanLogInput(actionName);
            
            if (isSignificantChange && canLog)
            {
                _logBuffer.Clear();
                _logBuffer.Append("[PlugInput] ");
                _logBuffer.Append(actionName);
                
                if (!string.IsNullOrEmpty(inputType))
                {
                    _logBuffer.Append($" ({inputType})");
                }
                
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
                
                if (_debugLevel.HasFlag(DebugLevel.Performance))
                {
                    float currentTime = Time.unscaledTime;
                    if (_inputTimestamps.TryGetValue(actionName, out float lastTime))
                    {
                        float delta = currentTime - lastTime;
                        _logBuffer.Append($" [Δt: {delta:F3}s]");
                    }
                    _inputTimestamps[actionName] = currentTime;
                    
                    if (_inputCounts.TryGetValue(actionName, out int count))
                    {
                        _logBuffer.Append($" [Count: {count + 1}]");
                    }
                }
                
                Debug.Log(_logBuffer.ToString());
                _lastLoggedValues[actionName] = value;
                UpdateInputStats(actionName, isPerformed);
            }
        }
        
        /// <summary>
        /// Gera estatísticas de performance
        /// </summary>
        public string GeneratePerformanceStats()
        {
            if (!_isEnabled || !_debugLevel.HasFlag(DebugLevel.Performance))
                return string.Empty;
                
            _logBuffer.Clear();
            _logBuffer.AppendLine("-- ESTATÍSTICAS DE PERFORMANCE --");
            _logBuffer.AppendLine($"Inputs Únicos: {_inputCounts.Count}");
            _logBuffer.AppendLine($"Inputs Ativos: {_activeInputs.Count}");
            _logBuffer.AppendLine($"Inputs Filtrados: {_filteredInputs.Count}");
            
            if (_inputCounts.Count > 0)
            {
                int totalEvents = 0;
                foreach (var count in _inputCounts.Values)
                {
                    totalEvents += count;
                }
                _logBuffer.AppendLine($"Total de Eventos: {totalEvents}");
            }
            
            return _logBuffer.ToString();
        }
        
        /// <summary>
        /// Obtém informações de debug do sistema
        /// </summary>
        public DebugInfo GetDebugInfo()
        {
            return new DebugInfo
            {
                IsEnabled = _isEnabled,
                DebugLevel = _debugLevel,
                ActiveInputsCount = _activeInputs.Count,
                FilteredInputsCount = _filteredInputs.Count,
                TotalEventsLogged = SumValues(_inputCounts),
                MinimumLogInterval = _minimumLogInterval
            };
        }
        
        /// <summary>
        /// Verifica se pode logar o input baseado no intervalo mínimo
        /// </summary>
        private bool CanLogInput(string actionName)
        {
            if (_minimumLogInterval <= 0f)
                return true;
                
            float currentTime = Time.unscaledTime;
            if (_inputTimestamps.TryGetValue(actionName, out float lastTime))
            {
                return (currentTime - lastTime) >= _minimumLogInterval;
            }
            return true;
        }
        
        /// <summary>
        /// Atualiza estatísticas do input
        /// </summary>
        private void UpdateInputStats(string actionName, bool isPerformed)
        {
            if (_debugLevel.HasFlag(DebugLevel.Performance))
            {
                if (isPerformed)
                {
                    _inputCounts[actionName] = GetValueOrDefault(_inputCounts, actionName, 0) + 1;
                }
                _inputTimestamps[actionName] = Time.unscaledTime;
            }
        }
        
        /// <summary>
        /// Obtém valor padrão do Dictionary
        /// </summary>
        private static TValue GetValueOrDefault<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Soma valores de um Dictionary
        /// </summary>
        private static int SumValues(Dictionary<string, int> dictionary)
        {
            int sum = 0;
            foreach (int value in dictionary.Values)
            {
                sum += value;
            }
            return sum;
        }
        
        /// <summary>
        /// Estrutura para informações de debug
        /// </summary>
        public struct DebugInfo
        {
            public bool IsEnabled;
            public DebugLevel DebugLevel;
            public int ActiveInputsCount;
            public int FilteredInputsCount;
            public int TotalEventsLogged;
            public float MinimumLogInterval;
        }
    }
}