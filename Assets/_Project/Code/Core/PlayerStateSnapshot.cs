namespace GlassRefrain.Core;

public readonly struct PlayerStateSnapshot {
    public PlayerState ResolvedState { get; }
    public CombatCoreState CombatState { get; }
    public LocomotionState LocomotionState { get; }
    public ActionLockContext ActionLock { get; }
    public RecoveryContext Recovery { get; }
    public bool HasTargetFocus { get; }
    public string StateDetail { get; }
    public CombatResolutionResult LastResolutionResult { get; }

    public PlayerStateSnapshot(
        PlayerState resolvedState,
        CombatCoreState combatState,
        LocomotionState locomotionState,
        ActionLockContext actionLock,
        RecoveryContext recovery,
        bool hasTargetFocus,
        string stateDetail,
        CombatResolutionResult lastResolutionResult) {
        ResolvedState = resolvedState;
        CombatState = combatState;
        LocomotionState = locomotionState;
        ActionLock = actionLock;
        Recovery = recovery;
        HasTargetFocus = hasTargetFocus;
        StateDetail = stateDetail ?? string.Empty;
        LastResolutionResult = lastResolutionResult;
    }
}
