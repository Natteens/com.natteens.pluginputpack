using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Gerencia o cache de estados e acessores de input.
    /// </summary>
    public class PlugInputCache
    {
        // Cache de estados de inputs
        private readonly Dictionary<string, InputState> _states = new Dictionary<string, InputState>();
        
        // Cache de acessores para evitar alocação de memória
        private readonly Dictionary<string, InputAccessor> _accessors = new Dictionary<string, InputAccessor>();
        
        // Pool de objetos acessores para reutilização
        private readonly Stack<InputAccessor> _accessorPool = new Stack<InputAccessor>();
        
        /// <summary>
        /// Registra um estado de input
        /// </summary>
        public void RegisterState(InputAction action)
        {
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
            if (_states.TryGetValue(actionName, out var state))
                return state;
                
            return null;
        }
        
        /// <summary>
        /// Obtém ou cria um accessor para um input
        /// </summary>
        public InputAccessor GetAccessor(string actionName)
        {
            // Retorna do cache se existir
            if (_accessors.TryGetValue(actionName, out var accessor))
                return accessor;
                
            // Obtém o estado para o input
            var state = GetState(actionName);
            if (state == null)
                return null;
                
            // Cria ou obtém um accessor do pool
            InputAccessor newAccessor;
            if (_accessorPool.Count > 0)
            {
                // Reutiliza um accessor do pool
                newAccessor = _accessorPool.Pop();
                // Aqui precisaríamos de uma forma de reinicializar o accessor com o novo estado
                // Como o InputAccessor é imutável nesta implementação, vamos criar um novo
                newAccessor = new InputAccessor(state);
            }
            else
            {
                // Cria um novo accessor
                newAccessor = new InputAccessor(state);
            }
            
            // Armazena no cache
            _accessors[actionName] = newAccessor;
            return newAccessor;
        }
        
        /// <summary>
        /// Atualiza todos os estados
        /// </summary>
        public void UpdateStates()
        {
            foreach (var state in _states.Values)
            {
                state.Update();
            }
        }
        
        /// <summary>
        /// Limpa todos os recursos
        /// </summary>
        public void Dispose()
        {
            foreach (var state in _states.Values)
            {
                state.Dispose();
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
    }
}