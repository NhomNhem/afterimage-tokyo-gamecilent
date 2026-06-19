#nullable enable
using System;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using R3;

namespace GlassRefrain.Application;

public sealed class PlayerStateResolver : IPlayerStateMachine {
    private readonly CombatStateMachine? _combatStateMachine;
    private readonly LocomotionStateMachine? _locomotionStateMachine;
    private readonly Subject<PlayerStateSnapshot> _stateSubject = new();
    private readonly IDisposable? _combatSubscription;
    private readonly IDisposable? _locomotionSubscription;

    private PlayerStateSnapshot _currentSnapshot;
    private bool _disposed;
    private MovementRestrictionContext _preTurnRestriction;
    private bool _hasTurnRestrictionSaved;
    private Subject<LocomotionStateSnapshot>? _emptyLocomotionSubject;

    public Observable<PlayerStateSnapshot> StateChanges => _stateSubject;
    public Observable<LocomotionStateSnapshot> LocomotionChanges =>
        _locomotionStateMachine != null
            ? _locomotionStateMachine.SnapshotChanges
            : (_emptyLocomotionSubject ??= new Subject<LocomotionStateSnapshot>());
    public PlayerStateSnapshot CurrentSnapshot => _currentSnapshot;

    public PlayerStateResolver(
        CombatStateMachine? combatStateMachine,
        LocomotionStateMachine? locomotionStateMachine) {
        _combatStateMachine = combatStateMachine;
        _locomotionStateMachine = locomotionStateMachine;

        if (combatStateMachine != null)
            _combatSubscription = combatStateMachine.StateChanges.Subscribe(_ => OnChildChanged());
        if (locomotionStateMachine != null)
            _locomotionSubscription = locomotionStateMachine.StateChanges.Subscribe(_ => OnChildChanged());

        _currentSnapshot = Resolve();
        _stateSubject.OnNext(_currentSnapshot);
    }

    private void OnChildChanged() {
        if (_disposed) return;

        var previousResolvedState = _currentSnapshot.ResolvedState;
        var previousCombatState = _currentSnapshot.CombatState;
        var snapshot = Resolve();

        if (snapshot.ResolvedState != previousResolvedState) {
            _currentSnapshot = snapshot;
            _stateSubject.OnNext(snapshot);
            OnResolvedStateChanged(previousResolvedState, snapshot.ResolvedState);
        } else if (snapshot.CombatState != previousCombatState) {
            _currentSnapshot = snapshot;
            _stateSubject.OnNext(snapshot);
        }
    }

    private PlayerStateSnapshot Resolve() {
        var resolvedState = ResolvePlayerState();
        var combatState = _combatStateMachine != null
            ? _combatStateMachine.CurrentCombatState
            : CombatCoreState.Disabled;
        var locomotionState = _locomotionStateMachine != null
            ? _locomotionStateMachine.CurrentLocomotionState
            : LocomotionState.Uninitialized;
        var actionLock = AggregateActionLock();
        var recovery = AggregateRecovery();
        var resolutionResult = _combatStateMachine != null
            ? _combatStateMachine.LastResolutionResult
            : new CombatResolutionResult(CombatActionType.Unknown, false, false, false, false, string.Empty,
                "No combat core");
        var detail = BuildStateDetail(resolvedState);
        var movementDirection = _locomotionStateMachine != null
            ? _locomotionStateMachine.CurrentMoveIntent
            : new Axis2(0f, 0f);
        var facingDirection = _locomotionStateMachine != null
            ? _locomotionStateMachine.CurrentFacingDirection
            : new Axis2(0f, 1f);

        return new PlayerStateSnapshot(
            resolvedState,
            combatState,
            locomotionState,
            actionLock,
            recovery,
            false,
            detail,
            resolutionResult,
            movementDirection,
            facingDirection);
    }

    private string BuildStateDetail(PlayerState resolved) {
        if (_combatStateMachine == null || !_combatStateMachine.HasCore)
            return "Locomotion only (degraded)";
        if (_locomotionStateMachine == null || !_locomotionStateMachine.HasLocomotion)
            return "Combat only (degraded)";
        return resolved + " | Combat: " + _combatStateMachine.CurrentCombatState +
               " | Locomotion: " + _locomotionStateMachine.CurrentLocomotionState;
    }

    private PlayerState ResolvePlayerState() {
        var combatPriority = _combatStateMachine?.CurrentPriority ?? 0;
        var locomotionPriority = _locomotionStateMachine?.CurrentPriority ?? 0;

        if (combatPriority >= locomotionPriority && _combatStateMachine != null) {
            return _combatStateMachine.CurrentMappedState;
        }

        return LocomotionStateToPlayerState(
            _locomotionStateMachine?.CurrentLocomotionState ?? LocomotionState.Uninitialized);
    }

    private static PlayerState LocomotionStateToPlayerState(LocomotionState state) {
        switch (state) {
            case LocomotionState.Moving: return PlayerState.Moving;
            default: return PlayerState.Idle;
        }
    }

    private ActionLockContext AggregateActionLock() {
        var combatLocked = _combatStateMachine != null && _combatStateMachine.ActionLock.IsLocked;
        var locomotionLocked = _locomotionStateMachine != null &&
                               !_locomotionStateMachine.MovementRestriction.CanTranslate;

        if (combatLocked)
            return new ActionLockContext(true, _combatStateMachine!.ActionLock.Source,
                _combatStateMachine.CurrentCombatState);

        if (locomotionLocked)
            return new ActionLockContext(true, _locomotionStateMachine!.MovementRestriction.Source,
                CombatCoreState.Neutral);

        return new ActionLockContext(false, string.Empty, CombatCoreState.Neutral);
    }

    private RecoveryContext AggregateRecovery() {
        var combatRecovering = _combatStateMachine != null && _combatStateMachine.Recovery.IsRecovering;
        var locomotionRecovering = _locomotionStateMachine != null && _locomotionStateMachine.Recovery.IsRecovering;

        if (combatRecovering)
            return _combatStateMachine!.Recovery;

        if (locomotionRecovering)
            return _locomotionStateMachine!.Recovery;

        return new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty);
    }

    private void OnResolvedStateChanged(PlayerState previous, PlayerState current) {
        if (current == PlayerState.Dodge && previous != PlayerState.Dodge)
            _locomotionStateMachine?.TryBeginDodgeDisplacement();
    }

    public void SetMovementLockedForTurn(bool isLocked, string source) {
        if (_locomotionStateMachine == null) return;

        if (isLocked) {
            _preTurnRestriction = _locomotionStateMachine.GetCurrentRestriction();
            _hasTurnRestrictionSaved = true;
            _locomotionStateMachine.SetMovementRestriction(new MovementRestrictionContext(
                canTranslate: false,
                canRotate: true,
                restrictionStrength: 1f,
                source: source ?? "TurnInPlace"));
            return;
        }

        if (_hasTurnRestrictionSaved) {
            _locomotionStateMachine.SetMovementRestriction(_preTurnRestriction);
            _hasTurnRestrictionSaved = false;
        }
    }

    public PlayerStateDebugSnapshot CreateDebugSnapshot() {
        var details = new string[] {
            "ResolvedState: " + _currentSnapshot.ResolvedState,
            "CombatState: " + _currentSnapshot.CombatState,
            "LocomotionState: " + _currentSnapshot.LocomotionState,
            "ActionLocked: " + _currentSnapshot.ActionLock.IsLocked + " | " + _currentSnapshot.ActionLock.Source,
            "Recovering: " + _currentSnapshot.Recovery.IsRecovering + " | " + _currentSnapshot.Recovery.Detail,
            "HasTargetFocus: " + _currentSnapshot.HasTargetFocus,
            "Detail: " + _currentSnapshot.StateDetail
        };
        return new PlayerStateDebugSnapshot("M0 PlayerState", Array.AsReadOnly(details));
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        _combatSubscription?.Dispose();
        _locomotionSubscription?.Dispose();
        _stateSubject.Dispose();
        _emptyLocomotionSubject?.Dispose();
    }
}
