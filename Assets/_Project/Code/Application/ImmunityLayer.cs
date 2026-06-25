using System;
using System.Collections.Generic;
using R3;

namespace GlassRefrain.Application;

public sealed class ImmunityLayer : IDisposable {
    private ImmunityFlags _activeFlags;
    private readonly List<IDisposable> _timers = new();

    public ImmunityFlags ActiveFlags => _activeFlags;

    public void GrantTimed(ImmunityFlags flags, float durationSeconds) {
        _activeFlags |= flags;

        var timer = Observable.Timer(TimeSpan.FromSeconds(durationSeconds))
            .Subscribe(_ => {
                _activeFlags &= ~flags;
            });

        _timers.Add(timer);
    }

    public bool IsImmuneTo(ImmunityFlags flag) {
        return (_activeFlags & flag) != 0;
    }

    public void RevokeAll() {
        _activeFlags = ImmunityFlags.None;
        foreach (var t in _timers) t.Dispose();
        _timers.Clear();
    }

    public void Dispose() {
        RevokeAll();
    }
}
