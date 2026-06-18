using System;
using GlassRefrain.Core;
using R3;

namespace GlassRefrain.Application;

public interface IPlayerStateMachine : IDisposable {
    Observable<PlayerStateSnapshot> StateChanges { get; }
    PlayerStateSnapshot CurrentSnapshot { get; }
    PlayerStateDebugSnapshot CreateDebugSnapshot();
}
