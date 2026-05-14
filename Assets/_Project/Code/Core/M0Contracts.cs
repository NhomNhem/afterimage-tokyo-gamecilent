namespace GlassRefrain.Core
{
    public readonly struct Axis2
    {
        public float X { get; }
        public float Y { get; }

        public Axis2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public readonly struct Axis3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Axis3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public enum InputActionIntent
    {
        None = 0,
        Move = 1,
        Look = 2,
        LightAttack = 3,
        HeavyAttack = 4,
        Parry = 5,
        Dodge = 6,
        Counter = 7,
        LockOn = 8,
        ResetEncounter = 9,
        ToggleDebugOverlay = 10
    }

    public enum InputRoutingDisposition
    {
        Disabled = 0,
        Ignored = 1,
        Routed = 2,
        Rejected = 3
    }

    public readonly struct InputIntentSnapshot
    {
        public Axis2 Move { get; }
        public Axis2 Look { get; }
        public bool LightAttackPressed { get; }
        public bool HeavyAttackPressed { get; }
        public bool ParryPressed { get; }
        public bool DodgePressed { get; }
        public bool CounterPressed { get; }
        public bool LockOnPressed { get; }
        public bool ResetEncounterPressed { get; }
        public bool ToggleDebugOverlayPressed { get; }
        public bool InputEnabled { get; }

        public InputIntentSnapshot(
            Axis2 move,
            Axis2 look,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool parryPressed,
            bool dodgePressed,
            bool counterPressed,
            bool lockOnPressed,
            bool resetEncounterPressed,
            bool toggleDebugOverlayPressed,
            bool inputEnabled)
        {
            Move = move;
            Look = look;
            LightAttackPressed = lightAttackPressed;
            HeavyAttackPressed = heavyAttackPressed;
            ParryPressed = parryPressed;
            DodgePressed = dodgePressed;
            CounterPressed = counterPressed;
            LockOnPressed = lockOnPressed;
            ResetEncounterPressed = resetEncounterPressed;
            ToggleDebugOverlayPressed = toggleDebugOverlayPressed;
            InputEnabled = inputEnabled;
        }
    }

    public readonly struct InputRoutingResult
    {
        public InputActionIntent Intent { get; }
        public InputRoutingDisposition Disposition { get; }
        public string RoutedTo { get; }
        public string Reason { get; }
        public bool Accepted
        {
            get { return Disposition == InputRoutingDisposition.Routed; }
        }

        public InputRoutingResult(
            InputActionIntent intent,
            InputRoutingDisposition disposition,
            string routedTo,
            string reason)
        {
            Intent = intent;
            Disposition = disposition;
            RoutedTo = routedTo;
            Reason = reason;
        }
    }

    public interface IInputIntentSource
    {
        InputIntentSnapshot Snapshot { get; }
    }

    public enum CombatActionType
    {
        Unknown = 0,
        LightAttack = 1,
        HeavyAttack = 2,
        Parry = 3,
        Dodge = 4,
        Counter = 5
    }

    public readonly struct CombatActionRequest
    {
        public CombatActionType ActionType { get; }
        public string Source { get; }
        public string ContextLabel { get; }

        public CombatActionRequest(CombatActionType actionType, string source, string contextLabel)
        {
            ActionType = actionType;
            Source = source;
            ContextLabel = contextLabel;
        }
    }

    public readonly struct CombatActionRequestResult
    {
        public bool Accepted { get; }
        public string Reason { get; }
        public string StateLabel { get; }

        public CombatActionRequestResult(bool accepted, string reason, string stateLabel)
        {
            Accepted = accepted;
            Reason = reason;
            StateLabel = stateLabel;
        }
    }

    public readonly struct CombatResolutionResult
    {
        public bool Resolved { get; }
        public bool Successful { get; }
        public string Detail { get; }

        public CombatResolutionResult(bool resolved, bool successful, string detail)
        {
            Resolved = resolved;
            Successful = successful;
            Detail = detail;
        }
    }

    public readonly struct ActionLockContext
    {
        public bool IsLocked { get; }
        public string Source { get; }
        public string Reason { get; }

        public ActionLockContext(bool isLocked, string source, string reason)
        {
            IsLocked = isLocked;
            Source = source;
            Reason = reason;
        }
    }

    public enum RecoverySource
    {
        Unknown = 0,
        CombatCore = 1,
        PlayerLocomotion = 2,
        Health = 3,
        Encounter = 4
    }

    public readonly struct RecoveryContext
    {
        public RecoverySource Source { get; }
        public bool IsRecovering { get; }
        public float RemainingSeconds { get; }
        public string Detail { get; }

        public RecoveryContext(RecoverySource source, bool isRecovering, float remainingSeconds, string detail)
        {
            Source = source;
            IsRecovering = isRecovering;
            RemainingSeconds = remainingSeconds;
            Detail = detail;
        }
    }

    public readonly struct MovementRestrictionContext
    {
        public bool CanTranslate { get; }
        public bool CanRotate { get; }
        public float RestrictionStrength { get; }
        public string Source { get; }

        public MovementRestrictionContext(bool canTranslate, bool canRotate, float restrictionStrength, string source)
        {
            CanTranslate = canTranslate;
            CanRotate = canRotate;
            RestrictionStrength = restrictionStrength;
            Source = source;
        }
    }

    public enum LocomotionState
    {
        Uninitialized = 0,
        Idle = 1,
        Moving = 2,
        Restricted = 3,
        Recovering = 4
    }

    public readonly struct LocomotionStateSnapshot
    {
        public LocomotionState State { get; }
        public Axis2 MoveIntent { get; }
        public bool InputEnabled { get; }
        public MovementRestrictionContext MovementRestriction { get; }
        public RecoveryContext Recovery { get; }
        public CameraMovementBasisSnapshot CameraMovementBasis { get; }
        public string StateDetail { get; }
        public bool HasMoveIntent
        {
            get { return MoveIntent.X != 0f || MoveIntent.Y != 0f; }
        }
        public bool IsRestricted
        {
            get { return State == LocomotionState.Restricted; }
        }
        public bool IsRecovering
        {
            get { return State == LocomotionState.Recovering; }
        }

        public LocomotionStateSnapshot(
            LocomotionState state,
            Axis2 moveIntent,
            bool inputEnabled,
            MovementRestrictionContext movementRestriction,
            RecoveryContext recovery,
            CameraMovementBasisSnapshot cameraMovementBasis,
            string stateDetail)
        {
            State = state;
            MoveIntent = moveIntent;
            InputEnabled = inputEnabled;
            MovementRestriction = movementRestriction;
            Recovery = recovery;
            CameraMovementBasis = cameraMovementBasis;
            StateDetail = stateDetail ?? string.Empty;
        }
    }

    public readonly struct FacingContextSnapshot
    {
        public Axis2 FacingDirection { get; }
        public Axis2 TargetDirection { get; }
        public bool HasTarget { get; }

        public FacingContextSnapshot(Axis2 facingDirection, Axis2 targetDirection, bool hasTarget)
        {
            FacingDirection = facingDirection;
            TargetDirection = targetDirection;
            HasTarget = hasTarget;
        }
    }

    public readonly struct LocomotionTransitionRecord
    {
        public string FromState { get; }
        public string ToState { get; }
        public string Reason { get; }
        public float TimeSeconds { get; }

        public LocomotionTransitionRecord(string fromState, string toState, string reason, float timeSeconds)
        {
            FromState = fromState;
            ToState = toState;
            Reason = reason;
            TimeSeconds = timeSeconds;
        }
    }

    public readonly struct DodgeRequestContext
    {
        public bool Requested { get; }
        public string Source { get; }
        public bool IsCameraRelative { get; }

        public DodgeRequestContext(bool requested, string source, bool isCameraRelative)
        {
            Requested = requested;
            Source = source;
            IsCameraRelative = isCameraRelative;
        }
    }

    public readonly struct DodgePhaseContext
    {
        public string PhaseName { get; }
        public float RemainingSeconds { get; }

        public DodgePhaseContext(string phaseName, float remainingSeconds)
        {
            PhaseName = phaseName;
            RemainingSeconds = remainingSeconds;
        }
    }

    public readonly struct DodgeResultContext
    {
        public bool Accepted { get; }
        public bool Invulnerable { get; }
        public string Reason { get; }

        public DodgeResultContext(bool accepted, bool invulnerable, string reason)
        {
            Accepted = accepted;
            Invulnerable = invulnerable;
            Reason = reason;
        }
    }

    public readonly struct EnemyIntentSnapshot
    {
        public string EnemyId { get; }
        public string IntentLabel { get; }
        public bool IsTelegraphing { get; }
        public float RemainingSeconds { get; }

        public EnemyIntentSnapshot(string enemyId, string intentLabel, bool isTelegraphing, float remainingSeconds)
        {
            EnemyId = enemyId;
            IntentLabel = intentLabel;
            IsTelegraphing = isTelegraphing;
            RemainingSeconds = remainingSeconds;
        }
    }

    public readonly struct TelegraphStateSnapshot
    {
        public string TelegraphId { get; }
        public bool IsActive { get; }
        public float RemainingSeconds { get; }

        public TelegraphStateSnapshot(string telegraphId, bool isActive, float remainingSeconds)
        {
            TelegraphId = telegraphId;
            IsActive = isActive;
            RemainingSeconds = remainingSeconds;
        }
    }

    public readonly struct EnemyAttackTagSet
    {
        public string[] Tags { get; }

        public EnemyAttackTagSet(string[] tags)
        {
            Tags = tags;
        }
    }

    public readonly struct EnemyPunishWindowContext
    {
        public bool IsOpen { get; }
        public float RemainingSeconds { get; }
        public string Source { get; }

        public EnemyPunishWindowContext(bool isOpen, float remainingSeconds, string source)
        {
            IsOpen = isOpen;
            RemainingSeconds = remainingSeconds;
            Source = source;
        }
    }

    public enum TargetFocusState
    {
        Inactive = 0,
        AcquireRequested = 1,
        Focused = 2,
        Invalid = 3
    }

    public readonly struct TargetDirectionContext
    {
        public Axis2 Direction { get; }
        public bool HasDirection { get; }
        public string Label { get; }

        public TargetDirectionContext(Axis2 direction, bool hasDirection, string label)
        {
            Direction = direction;
            HasDirection = hasDirection;
            Label = label;
        }
    }

    public readonly struct TargetAcquireRequest
    {
        public string TargetId { get; }
        public string Source { get; }
        public string Reason { get; }

        public TargetAcquireRequest(string targetId, string source, string reason)
        {
            TargetId = targetId;
            Source = source;
            Reason = reason;
        }
    }

    public readonly struct TargetReleaseRequest
    {
        public TargetReleaseReason Reason { get; }
        public string Source { get; }
        public string Detail { get; }

        public TargetReleaseRequest(TargetReleaseReason reason, string source, string detail)
        {
            Reason = reason;
            Source = source;
            Detail = detail;
        }
    }

    public readonly struct TargetValidityContext
    {
        public string TargetId { get; }
        public bool IsValid { get; }
        public string Reason { get; }

        public TargetValidityContext(string targetId, bool isValid, string reason)
        {
            TargetId = targetId;
            IsValid = isValid;
            Reason = reason;
        }
    }

    public readonly struct TargetContextSnapshot
    {
        public TargetFocusState FocusState { get; }
        public string TargetId { get; }
        public bool IsLockedOn
        {
            get { return FocusState == TargetFocusState.Focused; }
        }
        public bool IsValid { get; }
        public TargetDirectionContext Direction { get; }
        public string AcquireReason { get; }
        public string ReleaseReason { get; }
        public string InvalidReason { get; }
        public bool HasTarget
        {
            get { return !string.IsNullOrEmpty(TargetId); }
        }

        public TargetContextSnapshot(
            TargetFocusState focusState,
            string targetId,
            bool isValid,
            TargetDirectionContext direction,
            string acquireReason,
            string releaseReason,
            string invalidReason)
        {
            FocusState = focusState;
            TargetId = targetId;
            IsValid = isValid;
            Direction = direction;
            AcquireReason = acquireReason;
            ReleaseReason = releaseReason;
            InvalidReason = invalidReason;
        }
    }

    public readonly struct TargetAcquireResult
    {
        public bool Accepted { get; }
        public string TargetId { get; }
        public string Reason { get; }

        public TargetAcquireResult(bool accepted, string targetId, string reason)
        {
            Accepted = accepted;
            TargetId = targetId;
            Reason = reason;
        }
    }

    public readonly struct TargetDebugSnapshot
    {
        public string Summary { get; }
        public System.Collections.Generic.IReadOnlyList<string> Details { get; }

        public TargetDebugSnapshot(string summary, System.Collections.Generic.IReadOnlyList<string> details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public enum TargetReleaseReason
    {
        Unknown = 0,
        Manual = 1,
        Invalid = 2,
        OutOfRange = 3,
        EncounterReset = 4,
        TargetLost = 5
    }

    public readonly struct CameraMovementBasisSnapshot
    {
        public Axis2 Forward { get; }
        public Axis2 Right { get; }
        public bool IsValid { get; }
        public string CameraModeLabel { get; }

        public CameraMovementBasisSnapshot(Axis2 forward, Axis2 right, bool isValid, string cameraModeLabel)
        {
            Forward = forward;
            Right = right;
            IsValid = isValid;
            CameraModeLabel = cameraModeLabel;
        }
    }

    public readonly struct DamageApplicationContext
    {
        public string SourceId { get; }
        public float Amount { get; }
        public string DamageType { get; }

        public DamageApplicationContext(string sourceId, float amount, string damageType)
        {
            SourceId = sourceId;
            Amount = amount;
            DamageType = damageType;
        }
    }

    public readonly struct HealthStateSnapshot
    {
        public float Current { get; }
        public float Max { get; }
        public bool IsAlive { get; }

        public HealthStateSnapshot(float current, float max, bool isAlive)
        {
            Current = current;
            Max = max;
            IsAlive = isAlive;
        }
    }

    public readonly struct HitReactionContext
    {
        public string SourceId { get; }
        public string ReactionLabel { get; }
        public float SuppressionSeconds { get; }

        public HitReactionContext(string sourceId, string reactionLabel, float suppressionSeconds)
        {
            SourceId = sourceId;
            ReactionLabel = reactionLabel;
            SuppressionSeconds = suppressionSeconds;
        }
    }

    public readonly struct DefeatStateContext
    {
        public bool IsDefeated { get; }
        public string Reason { get; }

        public DefeatStateContext(bool isDefeated, string reason)
        {
            IsDefeated = isDefeated;
            Reason = reason;
        }
    }

    public readonly struct RevealRequestContext
    {
        public string SourceId { get; }
        public string MemoryId { get; }
        public string ContextLabel { get; }

        public RevealRequestContext(string sourceId, string memoryId, string contextLabel)
        {
            SourceId = sourceId;
            MemoryId = memoryId;
            ContextLabel = contextLabel;
        }
    }

    public readonly struct RevealRequestResult
    {
        public bool Accepted { get; }
        public string Reason { get; }

        public RevealRequestResult(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason;
        }
    }

    public readonly struct MemoryStateSnapshot
    {
        public string MemoryId { get; }
        public bool IsRevealed { get; }
        public float CooldownSeconds { get; }

        public MemoryStateSnapshot(string memoryId, bool isRevealed, float cooldownSeconds)
        {
            MemoryId = memoryId;
            IsRevealed = isRevealed;
            CooldownSeconds = cooldownSeconds;
        }
    }

    public readonly struct MemoryResponseContext
    {
        public string MemoryId { get; }
        public string ResponseLabel { get; }
        public bool IsAccepted { get; }

        public MemoryResponseContext(string memoryId, string responseLabel, bool isAccepted)
        {
            MemoryId = memoryId;
            ResponseLabel = responseLabel;
            IsAccepted = isAccepted;
        }
    }

    public readonly struct EncounterStateSnapshot
    {
        public string EncounterId { get; }
        public bool IsActive { get; }
        public int ParticipantCount { get; }

        public EncounterStateSnapshot(string encounterId, bool isActive, int participantCount)
        {
            EncounterId = encounterId;
            IsActive = isActive;
            ParticipantCount = participantCount;
        }
    }

    public readonly struct EncounterStartContext
    {
        public string EncounterId { get; }
        public string Reason { get; }

        public EncounterStartContext(string encounterId, string reason)
        {
            EncounterId = encounterId;
            Reason = reason;
        }
    }

    public readonly struct EncounterEndContext
    {
        public string EncounterId { get; }
        public string EndReason { get; }

        public EncounterEndContext(string encounterId, string endReason)
        {
            EncounterId = encounterId;
            EndReason = endReason;
        }
    }

    public readonly struct EncounterResetContext
    {
        public string EncounterId { get; }
        public string ResetReason { get; }

        public EncounterResetContext(string encounterId, string resetReason)
        {
            EncounterId = encounterId;
            ResetReason = resetReason;
        }
    }

    public readonly struct CombatDebugSnapshot
    {
        public string Summary { get; }
        public string[] Details { get; }

        public CombatDebugSnapshot(string summary, string[] details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct LocomotionDebugSnapshot
    {
        public string Summary { get; }
        public System.Collections.Generic.IReadOnlyList<string> Details { get; }

        public LocomotionDebugSnapshot(string summary, System.Collections.Generic.IReadOnlyList<string> details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct EnemyIntentDebugSnapshot
    {
        public string Summary { get; }
        public string[] Details { get; }

        public EnemyIntentDebugSnapshot(string summary, string[] details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct HealthDebugSnapshot
    {
        public string Summary { get; }
        public string[] Details { get; }

        public HealthDebugSnapshot(string summary, string[] details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct MemoryDebugSnapshot
    {
        public string Summary { get; }
        public string[] Details { get; }

        public MemoryDebugSnapshot(string summary, string[] details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct CameraDebugSnapshot
    {
        public string Summary { get; }
        public string[] Details { get; }

        public CameraDebugSnapshot(string summary, string[] details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct EncounterDebugSnapshot
    {
        public string Summary { get; }
        public string[] Details { get; }

        public EncounterDebugSnapshot(string summary, string[] details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct InputDebugSnapshot
    {
        public string Summary { get; }
        public System.Collections.Generic.IReadOnlyList<string> Details { get; }

        public InputDebugSnapshot(string summary, System.Collections.Generic.IReadOnlyList<string> details)
        {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct DebugTransitionEvent
    {
        public string SystemName { get; }
        public string EventName { get; }
        public string Detail { get; }
        public bool Accepted { get; }

        public DebugTransitionEvent(string systemName, string eventName, string detail, bool accepted)
        {
            SystemName = systemName;
            EventName = eventName;
            Detail = detail;
            Accepted = accepted;
        }
    }
}
