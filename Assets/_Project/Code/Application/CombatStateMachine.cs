#nullable enable
using System;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using R3;

namespace GlassRefrain.Application;

public sealed class CombatStateMachine : IDisposable {
    private static readonly (CombatCoreState State, int Priority)[] CombatPriorityTable = {
        (CombatCoreState.Disabled, 9),
        (CombatCoreState.HitReact, 8),
        (CombatCoreState.RevealBeat, 7),
        (CombatCoreState.CounterActive, 6),
        (CombatCoreState.AttackStartup, 5),
        (CombatCoreState.AttackActive, 5),
        (CombatCoreState.AttackRecovery, 5),
        (CombatCoreState.CounterWindow, 5),
        (CombatCoreState.ParryStartup, 4),
        (CombatCoreState.ParryActive, 4),
        (CombatCoreState.ParryRecovery, 4),
        (CombatCoreState.DodgeStartup, 3),
        (CombatCoreState.DodgeActive, 3),
        (CombatCoreState.DodgeRecovery, 3),
        (CombatCoreState.Neutral, 0),
    };

    private readonly M0CombatCore? _combatCore;
    private readonly Subject<PlayerState> _stateSubject = new();
    private M0CombatSnapshot _latestSnapshot;
    private bool _disposed;

    public Observable<PlayerState> StateChanges => _stateSubject;
    public CombatCoreState CurrentCombatState => _latestSnapshot.State;
    public PlayerState CurrentMappedState => CombatStateToPlayerState(_latestSnapshot.State);
    public int CurrentPriority => LookupPriority(_latestSnapshot.State);
    public ActionLockContext ActionLock => _latestSnapshot.ActionLock;
    public RecoveryContext Recovery => _latestSnapshot.Recovery;
    public CombatResolutionResult LastResolutionResult => _latestSnapshot.LastResolutionResult;
    public bool HasCore => _combatCore != null;

    public CombatStateMachine(M0CombatCore? combatCore) {
        _combatCore = combatCore;

        if (combatCore != null) {
            _latestSnapshot = combatCore.Snapshot;
            combatCore.SnapshotChanged += OnSnapshotChanged;
        } else {
            _latestSnapshot = CreateDefaultSnapshot();
        }

        _stateSubject.OnNext(CurrentMappedState);
    }

    public static PlayerState CombatStateToPlayerState(CombatCoreState state) {
        switch (state) {
            case CombatCoreState.Disabled: return PlayerState.Disabled;
            case CombatCoreState.HitReact: return PlayerState.HitReaction;
            case CombatCoreState.RevealBeat: return PlayerState.RevealBeat;
            case CombatCoreState.CounterActive: return PlayerState.CounterActive;
            case CombatCoreState.AttackStartup:
            case CombatCoreState.AttackActive:
            case CombatCoreState.AttackRecovery:
            case CombatCoreState.CounterWindow: return PlayerState.Attack;
            case CombatCoreState.ParryStartup:
            case CombatCoreState.ParryActive:
            case CombatCoreState.ParryRecovery: return PlayerState.Parry;
            case CombatCoreState.DodgeStartup:
            case CombatCoreState.DodgeActive:
            case CombatCoreState.DodgeRecovery: return PlayerState.Dodge;
            default: return PlayerState.Idle;
        }
    }

    private void OnSnapshotChanged(M0CombatSnapshot snapshot) {
        if (_disposed) return;
        _latestSnapshot = snapshot;
        _stateSubject.OnNext(CurrentMappedState);
    }

    private static int LookupPriority(CombatCoreState state) {
        for (var i = 0; i < CombatPriorityTable.Length; i++) {
            if (CombatPriorityTable[i].State == state)
                return CombatPriorityTable[i].Priority;
        }
        return 0;
    }

    private static M0CombatSnapshot CreateDefaultSnapshot() {
        return new M0CombatSnapshot(
            CombatCoreState.Disabled,
            new CombatActionRequestResult(CombatActionResult.Ignored, "No combat core", "Disabled"),
            new CombatResolutionResult(CombatActionType.Unknown, false, false, false, false, string.Empty,
                "No combat core"),
            new CounterWindowState(false, string.Empty, 0f, 0f),
            new ActionLockContext(false, string.Empty, CombatCoreState.Disabled),
            new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty));
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        if (_combatCore != null)
            _combatCore.SnapshotChanged -= OnSnapshotChanged;
        _stateSubject.Dispose();
    }
}
