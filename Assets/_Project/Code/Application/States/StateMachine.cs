using System.Collections.Generic;
using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Application;

public sealed class StateMachine {
    private readonly Dictionary<string, IState> _states = new();
    private IState _currentState;
    private string _currentKey;
    private readonly string _ownerName;
    private readonly INhemLogger _logger;

    public string CurrentKey => _currentKey;
    public IState CurrentState => _currentState;

    public StateMachine(string ownerName, INhemLogger logger) {
        _ownerName = ownerName;
        _logger = logger;
        _currentKey = string.Empty;
    }

    public void AddState(string key, IState state) {
        _states[key] = state;
    }

    public void TransitionTo(string nextKey, StateContext ctx) {
        if (_currentKey == nextKey) return;
        _currentState?.Exit(ctx);
        if (!_states.TryGetValue(nextKey, out var nextState)) {
            _logger?.LogWarning($"[{_ownerName}SM] State '{nextKey}' not registered");
            return;
        }
        _currentKey = nextKey;
        _currentState = nextState;
        _currentState.Enter(ctx);
    }

    public void Update(StateContext ctx) {
        _currentState?.Update(ctx);
    }
}
