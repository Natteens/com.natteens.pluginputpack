using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace PlugInputPack
{
    /// <summary>
    /// Manages per-action InputState instances and their pooled InputAccessors.
    /// </summary>
    public class PlugInputCache
    {
        private readonly Dictionary<string, InputState> _stateMap  = new Dictionary<string, InputState>();
        private readonly List<InputState>               _stateList = new List<InputState>();

        private readonly Dictionary<string, InputAccessor> _accessors    = new Dictionary<string, InputAccessor>();
        private readonly Stack<InputAccessor>              _accessorPool = new Stack<InputAccessor>();

        // Reused each flush to report which actions had press/release this frame — zero alloc.
        private readonly List<(InputState state, bool pressed, bool released)> _flushResults
            = new List<(InputState, bool, bool)>();

        // ── Registration ──────────────────────────────────────────────────────────

        public void RegisterState(InputAction action)
        {
            if (action == null)
            {
                UnityEngine.Debug.LogWarning("[PlugInputCache] Attempted to register a null InputAction.");
                return;
            }

            if (!_stateMap.ContainsKey(action.name))
            {
                var state = new InputState(action);
                _stateMap[action.name] = state;
                _stateList.Add(state);
            }
        }

        // ── State access ──────────────────────────────────────────────────────────

        public InputState GetState(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return null;
            _stateMap.TryGetValue(actionName, out InputState state);
            return state;
        }

        // ── Accessor pool ─────────────────────────────────────────────────────────

        public InputAccessor GetAccessor(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return null;

            if (_accessors.TryGetValue(actionName, out InputAccessor existing))
                return existing;

            InputState state = GetState(actionName);
            if (state == null) return null;

            InputAccessor accessor = _accessorPool.Count > 0
                ? _accessorPool.Pop()
                : new InputAccessor(state);

            accessor.Initialize(state);
            _accessors[actionName] = accessor;
            return accessor;
        }

        public void ReturnAccessorToPool(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return;
            if (!_accessors.TryGetValue(actionName, out InputAccessor accessor)) return;

            _accessors.Remove(actionName);
            accessor.Reset();
            _accessorPool.Push(accessor);
        }

        // ── Per-frame flush (must be called from Update, not LateUpdate) ──────────

        /// <summary>
        /// Flushes pressed/released buffers for every registered state.
        /// Returns a list of states that had a press or release this frame,
        /// so PlugInputComponent can fire events after the flush — not before.
        /// The returned list is reused each call; do not store it.
        /// </summary>
        public List<(InputState state, bool pressed, bool released)> FlushStates()
        {
            _flushResults.Clear();
            for (int i = 0; i < _stateList.Count; i++)
            {
                var (pressed, released) = _stateList[i].Flush();
                if (pressed || released)
                    _flushResults.Add((_stateList[i], pressed, released));
            }
            return _flushResults;
        }

        // ── Queries ───────────────────────────────────────────────────────────────

        public bool HasInput(string actionName) =>
            !string.IsNullOrEmpty(actionName) && _stateMap.ContainsKey(actionName);

        /// <summary>
        /// Returns the internal state list for zero-alloc iteration.
        /// Callers must not add/remove elements.
        /// </summary>
        public List<InputState> GetStates() => _stateList;

        public IEnumerable<string> GetInputNames() => _stateMap.Keys;

        public string GetCacheStats() =>
            $"States: {_stateMap.Count}, Accessors: {_accessors.Count}, Pool: {_accessorPool.Count}";

        // ── Disposal ──────────────────────────────────────────────────────────────

        public void Dispose()
        {
            for (int i = 0; i < _stateList.Count; i++)
                _stateList[i].Dispose();

            foreach (InputAccessor a in _accessors.Values)
                a.Dispose();

            while (_accessorPool.Count > 0)
                _accessorPool.Pop().Dispose();

            _stateMap.Clear();
            _stateList.Clear();
            _accessors.Clear();
            _accessorPool.Clear();
            _flushResults.Clear();
        }
    }
}