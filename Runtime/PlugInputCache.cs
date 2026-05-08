using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace PlugInputPack
{
    public class PlugInputCache
    {
        private readonly Dictionary<string, InputState>    _stateMap  = new();
        private readonly List<InputState>                  _stateList = new();
        private readonly Dictionary<string, InputAccessor> _accessors = new();

        private readonly List<(InputState state, bool pressed, bool released)> _flushResults = new();

        public void RegisterState(InputAction action)
        {
            if (action == null || _stateMap.ContainsKey(action.name)) return;
            var state = new InputState(action);
            _stateMap[action.name] = state;
            _stateList.Add(state);
        }

        public InputState GetState(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return null;
            _stateMap.TryGetValue(actionName, out var state);
            return state;
        }

        public InputAccessor GetAccessor(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return null;
            if (_accessors.TryGetValue(actionName, out var existing)) return existing;

            var state = GetState(actionName);
            if (state == null) return null;

            var accessor = new InputAccessor(state);
            _accessors[actionName] = accessor;
            return accessor;
        }

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

        public bool HasInput(string actionName) =>
            !string.IsNullOrEmpty(actionName) && _stateMap.ContainsKey(actionName);

        public List<InputState>    GetStates()     => _stateList;
        public IEnumerable<string> GetInputNames() => _stateMap.Keys;

        public string GetCacheStats() =>
            $"States: {_stateMap.Count}  Accessors: {_accessors.Count}";

        public void Dispose()
        {
            for (int i = 0; i < _stateList.Count; i++) _stateList[i].Dispose();
            foreach (var a in _accessors.Values) a.Dispose();
            _stateMap.Clear();
            _stateList.Clear();
            _accessors.Clear();
            _flushResults.Clear();
        }
    }
}
