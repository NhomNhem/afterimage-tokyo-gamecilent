#nullable enable
using System;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using R3;

namespace GlassRefrain.Application;

public enum GroundState {
    Idle = 0,
    Moving = 1,
    Restricted = 2,
    Recovering = 3
}

public sealed class LocomotionStateMachine : IDisposable {
    private static readonly (LocomotionState State, int Priority)[] LocomotionPriorityTable = {
        (LocomotionState.Uninitialized, 0),
        (LocomotionState.Idle, 0),
        (LocomotionState.Moving, 1),
        (LocomotionState.Restricted, 0),
        (LocomotionState.Recovering, 0),
    };

    private readonly IM0PlayerLocomotion? _locomotion;
    private readonly Subject<GroundState> _stateSubject = new();
    private readonly Subject<LocomotionStateSnapshot> _snapshotSubject = new();
    private LocomotionStateSnapshot _latestSnapshot;
    private bool _disposed;

    public Observable<GroundState> StateChanges => _stateSubject;
    public Observable<LocomotionStateSnapshot> SnapshotChanges => _snapshotSubject;
    public LocomotionStateSnapshot LatestSnapshot => _latestSnapshot;
    public GroundState CurrentGroundState => LocomotionStateToGroundState(_latestSnapshot.State);
    public LocomotionState CurrentLocomotionState => _latestSnapshot.State;
    public int CurrentPriority => LookupPriority(_latestSnapshot.State);
    public MovementRestrictionContext MovementRestriction => _latestSnapshot.MovementRestriction;
    public RecoveryContext Recovery => _latestSnapshot.Recovery;
    public bool HasLocomotion => _locomotion != null;
    public Axis2 CurrentMoveIntent => _latestSnapshot.MoveIntent;
    public Axis2 CurrentFacingDirection {
        get {
            if (_locomotion == null) return new Axis2(0f, 1f);
            var facing = _locomotion.GetMovementSnapshot().Facing;
            return new Axis2(facing.x, facing.z);
        }
    }

    public LocomotionStateMachine(IM0PlayerLocomotion? locomotion) {
        _locomotion = locomotion;

        if (locomotion != null) {
            _latestSnapshot = locomotion.Snapshot;
            locomotion.SnapshotChanged += OnSnapshotChanged;
        } else {
            _latestSnapshot = CreateDefaultSnapshot();
        }

        _stateSubject.OnNext(CurrentGroundState);
        _snapshotSubject.OnNext(_latestSnapshot);
    }

    public bool TryBeginDodgeDisplacement() {
        return _locomotion?.TryBeginDodgeDisplacement() ?? false;
    }

    public MovementRestrictionContext GetCurrentRestriction() {
        return _latestSnapshot.MovementRestriction;
    }

    public void SetMovementRestriction(MovementRestrictionContext restriction) {
        _locomotion?.SetMovementRestriction(restriction);
    }

    public static GroundState LocomotionStateToGroundState(LocomotionState state) {
        switch (state) {
            case LocomotionState.Moving: return GroundState.Moving;
            case LocomotionState.Restricted: return GroundState.Restricted;
            case LocomotionState.Recovering: return GroundState.Recovering;
            default: return GroundState.Idle;
        }
    }

    private void OnSnapshotChanged(LocomotionStateSnapshot snapshot) {
        if (_disposed) return;
        _latestSnapshot = snapshot;
        _stateSubject.OnNext(CurrentGroundState);
        _snapshotSubject.OnNext(snapshot);
    }

    private static int LookupPriority(LocomotionState state) {
        for (var i = 0; i < LocomotionPriorityTable.Length; i++) {
            if (LocomotionPriorityTable[i].State == state)
                return LocomotionPriorityTable[i].Priority;
        }
        return 0;
    }

    private static LocomotionStateSnapshot CreateDefaultSnapshot() {
        return new LocomotionStateSnapshot(
            LocomotionState.Uninitialized,
            new Axis2(0f, 0f),
            false,
            new MovementRestrictionContext(true, true, 0f, string.Empty),
            new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty),
            new CameraMovementBasisSnapshot(new Axis2(0f, 1f), new Axis2(1f, 0f), false, "No locomotion"),
            "No locomotion (degraded)");
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        if (_locomotion != null)
            _locomotion.SnapshotChanged -= OnSnapshotChanged;
        _stateSubject.Dispose();
        _snapshotSubject.Dispose();
    }
}
