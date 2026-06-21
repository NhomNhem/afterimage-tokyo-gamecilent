using System;
using GlassRefrain.Core;
using R3;

namespace GlassRefrain.Application;

public interface IPlayerStateMachine : IDisposable {
    Observable<PlayerStateSnapshot> StateChanges { get; }
    Observable<LocomotionStateSnapshot> LocomotionChanges { get; }
    PlayerStateSnapshot CurrentSnapshot { get; }
    void SetMovementLockedForTurn(bool isLocked, string source);
    void SetHasTargetFocus(bool hasTargetFocus);
    PlayerStateDebugSnapshot CreateDebugSnapshot();
}
