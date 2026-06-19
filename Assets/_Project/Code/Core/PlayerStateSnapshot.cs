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
    public Axis2 MovementDirection { get; }
    public Axis2 FacingDirection { get; }
    public Axis2 WorldVelocity { get; }

    public PlayerStateSnapshot(
        PlayerState resolvedState,
        CombatCoreState combatState,
        LocomotionState locomotionState,
        ActionLockContext actionLock,
        RecoveryContext recovery,
        bool hasTargetFocus,
        string stateDetail,
        CombatResolutionResult lastResolutionResult)
        : this(resolvedState, combatState, locomotionState, actionLock, recovery,
            hasTargetFocus, stateDetail, lastResolutionResult,
            new Axis2(0f, 0f), new Axis2(0f, 1f), new Axis2(0f, 0f)) { }

    public PlayerStateSnapshot(
        PlayerState resolvedState,
        CombatCoreState combatState,
        LocomotionState locomotionState,
        ActionLockContext actionLock,
        RecoveryContext recovery,
        bool hasTargetFocus,
        string stateDetail,
        CombatResolutionResult lastResolutionResult,
        Axis2 movementDirection,
        Axis2 facingDirection)
        : this(resolvedState, combatState, locomotionState, actionLock, recovery,
            hasTargetFocus, stateDetail, lastResolutionResult,
            movementDirection, facingDirection, new Axis2(0f, 0f)) { }

    public PlayerStateSnapshot(
        PlayerState resolvedState,
        CombatCoreState combatState,
        LocomotionState locomotionState,
        ActionLockContext actionLock,
        RecoveryContext recovery,
        bool hasTargetFocus,
        string stateDetail,
        CombatResolutionResult lastResolutionResult,
        Axis2 movementDirection,
        Axis2 facingDirection,
        Axis2 worldVelocity) {
        ResolvedState = resolvedState;
        CombatState = combatState;
        LocomotionState = locomotionState;
        ActionLock = actionLock;
        Recovery = recovery;
        HasTargetFocus = hasTargetFocus;
        StateDetail = stateDetail ?? string.Empty;
        LastResolutionResult = lastResolutionResult;
        MovementDirection = movementDirection;
        FacingDirection = facingDirection;
        WorldVelocity = worldVelocity;
    }
}
