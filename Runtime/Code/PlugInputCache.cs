using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace PlugInputPack
{
    /// <summary>
    /// Gerencia o cache de estados e acessores de input.
    /// </summary>
    public class PlugInputCache
    {
        private readonly Dictionary<string, InputState> _states = new Dictionary<string, InputState>();
        
        private readonly Dictionary<string, InputAccessor> _accessors = new Dictionary<string, InputAccessor>();
        
        private readonly Stack<InputAccessor> _accessorPool = new Stack<InputAccessor>();
        
        /// <summary>
        /// Registra um estado de input
        /// </summary>
        public void RegisterState(InputAction action)
        {
            if (action == null)
            {
                UnityEngine.Debug.LogWarning("PlugInputCache: Tentativa de registrar InputAction nulo!");
                return;
            }

            if (!_states.ContainsKey(action.name))
            {
                _states[action.name] = new InputState(action);
            }
        }
        
        /// <summary>
        /// Obtém um estado de input
        /// </summary>
        public InputState GetState(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return null;
                
            if (_states.TryGetValue(actionName, out var state))
                return state;
                
            return null;
        }
        
        /// <summary>
        /// Obtém ou cria um accessor para um input
        /// </summary>
        public InputAccessor GetAccessor(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return null;
                
            if (_accessors.TryGetValue(actionName, out var accessor))
                return accessor;
                
            var state = GetState(actionName);
            if (state == null)
                return null;
                
            InputAccessor newAccessor;
            if (_accessorPool.Count > 0)
            {
                newAccessor = _accessorPool.Pop();
                newAccessor.Initialize(state); 
            }
            else
            {
                newAccessor = new InputAccessor(state);
            }
            
            _accessors[actionName] = newAccessor;
            return newAccessor;
        }
        
        /// <summary>
        /// Retorna um accessor para o pool para reutilização
        /// </summary>
        public void ReturnAccessorToPool(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return;
                
            if (_accessors.TryGetValue(actionName, out var accessor))
            {
                _accessors.Remove(actionName);
                accessor.Reset(); 
                _accessorPool.Push(accessor);
            }
        }
        
        /// <summary>
        /// Atualiza todos os estados
        /// </summary>
        public void UpdateStates()
        {
            foreach (var state in _states.Values)
            {
                state?.Update();
            }
        }
        
        /// <summary>
        /// Limpa todos os recursos
        /// </summary>
        public void Dispose()
        {
            foreach (var state in _states.Values)
            {
                state?.Dispose();
            }
            
            foreach (var accessor in _accessors.Values)
            {
                accessor?.Dispose();
            }
            
            while (_accessorPool.Count > 0)
            {
                var pooledAccessor = _accessorPool.Pop();
                pooledAccessor?.Dispose();
            }
            
            _states.Clear();
            _accessors.Clear();
            _accessorPool.Clear();
        }
        
        /// <summary>
        /// Verifica se um input existe
        /// </summary>
        public bool HasInput(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return false;
                
            return _states.ContainsKey(actionName);
        }
        
        /// <summary>
        /// Obtém todos os nomes de inputs
        /// </summary>
        public IEnumerable<string> GetInputNames()
        {
            return _states.Keys;
        }
        
        /// <summary>
        /// Obtém todos os estados de input
        /// </summary>
        public IEnumerable<InputState> GetStates()
        {
            return _states.Values;
        }
        
        /// <summary>
        /// Obtém estatísticas do cache para debug
        /// </summary>
        public string GetCacheStats()
        {
            return $"Estados: {_states.Count}, Accessors: {_accessors.Count}, Pool: {_accessorPool.Count}";
        }
    }
}