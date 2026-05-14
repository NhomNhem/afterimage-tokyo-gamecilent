namespace GlassRefrain.Core {
    public readonly struct Axis2 {
        public float X { get; }
        public float Y { get; }

        public Axis2(float x, float y) {
            X = x;
            Y = y;
        }
    }

    public readonly struct Axis3 {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Axis3(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public enum InputActionIntent {
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

    public enum InputRoutingDisposition {
        Disabled = 0,
        Ignored = 1,
        Routed = 2,
        Rejected = 3
    }

    public readonly struct InputIntentSnapshot {
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
            bool inputEnabled) {
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

    public readonly struct InputRoutingResult {
        public InputActionIntent Intent { get; }
        public InputRoutingDisposition Disposition { get; }
        public string RoutedTo { get; }
        public string Reason { get; }
        public bool Accepted => Disposition == InputRoutingDisposition.Routed;

        public InputRoutingResult(
            InputActionIntent intent,
            InputRoutingDisposition disposition,
            string routedTo,
            string reason) {
            Intent = intent;
            Disposition = disposition;
            RoutedTo = routedTo;
            Reason = reason;
        }
    }

    public interface IInputIntentSource {
        InputIntentSnapshot Snapshot { get; }
    }

    public enum CombatActionType {
        Unknown = 0,
        LightAttack = 1,
        HeavyAttack = 2,
        Dodge = 3,
        Parry = 4,
        Counter = 5
    }

    public enum CombatCoreState {
        Neutral = 0,
        AttackStartup = 1,
        AttackActive = 2,
        AttackRecovery = 3,
        DodgeStartup = 4,
        DodgeActive = 5,
        DodgeRecovery = 6,
        ParryStartup = 7,
        ParryActive = 8,
        ParryRecovery = 9,
        CounterWindow = 10,
        CounterActive = 11,
        HitReact = 12,
        RevealBeat = 13,
        Disabled = 14
    }

    public enum CombatActionResult {
        Accepted = 0,
        Rejected = 1,
        Ignored = 2
    }

    public enum CombatRequestSourceType {
        Unknown = 0,
        InputMapping = 1,
        CombatCore = 2,
        Encounter = 3,
        Memory = 4,
        TestHarness = 5
    }

    public readonly struct CombatActionRequest {
        public CombatActionType ActionType { get; }
        public float TimestampSeconds { get; }
        public CombatRequestSourceType SourceType { get; }
        public string Source { get; }
        public string ContextLabel { get; }

        public CombatActionRequest(
            CombatActionType actionType,
            float timestampSeconds,
            CombatRequestSourceType sourceType,
            string source,
            string contextLabel) {
            ActionType = actionType;
            TimestampSeconds = timestampSeconds;
            SourceType = sourceType;
            Source = source ?? string.Empty;
            ContextLabel = contextLabel ?? string.Empty;
        }

        public CombatActionRequest(CombatActionType actionType, string source, string contextLabel)
            : this(actionType, 0f, CombatRequestSourceType.Unknown, source, contextLabel) { }
    }

    public readonly struct CombatActionRequestResult {
        public CombatActionResult Result { get; }
        public bool Accepted { get; }
        public string Reason { get; }
        public string StateLabel { get; }

        public CombatActionRequestResult(CombatActionResult result, string reason, string stateLabel) {
            Result = result;
            Accepted = result == CombatActionResult.Accepted;
            Reason = reason ?? string.Empty;
            StateLabel = stateLabel ?? string.Empty;
        }

        public CombatActionRequestResult(bool accepted, string reason, string stateLabel)
            : this(accepted ? CombatActionResult.Accepted : CombatActionResult.Rejected, reason, stateLabel) { }
    }

    public readonly struct CombatResolutionResult {
        public CombatActionType ActionType { get; }
        public bool Resolved { get; }
        public bool Successful { get; }
        public bool HitConfirmed { get; }
        public bool TriggeredCounterWindow { get; }
        public string SourceLabel { get; }
        public string Detail { get; }

        public CombatResolutionResult(
            CombatActionType actionType,
            bool resolved,
            bool successful,
            bool hitConfirmed,
            bool triggeredCounterWindow,
            string sourceLabel,
            string detail) {
            ActionType = actionType;
            Resolved = resolved;
            Successful = successful;
            HitConfirmed = hitConfirmed;
            TriggeredCounterWindow = triggeredCounterWindow;
            SourceLabel = sourceLabel ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public CombatResolutionResult(bool resolved, bool successful, string detail)
            : this(CombatActionType.Unknown, resolved, successful, false, false, string.Empty, detail) { }
    }

    public readonly struct ActionLockContext {
        public bool LockActive { get; }
        public string LockSource { get; }
        public CombatCoreState RequestingState { get; }
        public bool IsLocked => LockActive;

        public string Source => LockSource;
        public string Reason => LockSource;

        public ActionLockContext(bool lockActive, string lockSource, CombatCoreState requestingState) {
            LockActive = lockActive;
            LockSource = lockSource ?? string.Empty;
            RequestingState = requestingState;
        }

        public ActionLockContext(bool isLocked, string source, string reason)
            : this(isLocked, string.IsNullOrEmpty(reason) ? source : reason, CombatCoreState.Neutral) { }
    }

    public enum RecoverySource {
        Unknown = 0,
        CombatCore = 1,
        PlayerLocomotion = 2,
        Health = 3,
        Encounter = 4
    }

    public readonly struct RecoveryContext {
        public bool RecoveryActive { get; }
        public string RecoverySourceLabel { get; }
        public CombatCoreState RequestingState { get; }
        public RecoverySource Source { get; }
        public bool IsRecovering => RecoveryActive;
        public float RemainingSeconds { get; }
        public string Detail => RecoverySourceLabel;

        public RecoveryContext(
            bool recoveryActive,
            string recoverySourceLabel,
            CombatCoreState requestingState,
            RecoverySource source,
            float remainingSeconds) {
            RecoveryActive = recoveryActive;
            RecoverySourceLabel = recoverySourceLabel ?? string.Empty;
            RequestingState = requestingState;
            Source = source;
            RemainingSeconds = remainingSeconds;
        }

        public RecoveryContext(RecoverySource source, bool isRecovering, float remainingSeconds, string detail)
            : this(isRecovering, detail, CombatCoreState.Neutral, source, remainingSeconds) { }
    }

    public readonly struct CounterWindowState {
        public bool IsOpen { get; }
        public string SourceTag { get; }
        public float ElapsedSeconds { get; }
        public float DurationSeconds { get; }
        public float RemainingSeconds => DurationSeconds - ElapsedSeconds;

        public CounterWindowState(bool isOpen, string sourceTag, float elapsedSeconds, float durationSeconds) {
            IsOpen = isOpen;
            SourceTag = sourceTag ?? string.Empty;
            ElapsedSeconds = elapsedSeconds;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct M0CombatSnapshot {
        public CombatCoreState State { get; }
        public CombatActionRequestResult LastActionResult { get; }
        public CombatResolutionResult LastResolutionResult { get; }
        public CounterWindowState CounterWindow { get; }
        public ActionLockContext ActionLock { get; }
        public RecoveryContext Recovery { get; }

        public M0CombatSnapshot(
            CombatCoreState state,
            CombatActionRequestResult lastActionResult,
            CombatResolutionResult lastResolutionResult,
            CounterWindowState counterWindow,
            ActionLockContext actionLock,
            RecoveryContext recovery) {
            State = state;
            LastActionResult = lastActionResult;
            LastResolutionResult = lastResolutionResult;
            CounterWindow = counterWindow;
            ActionLock = actionLock;
            Recovery = recovery;
        }
    }

    public readonly struct CombatStepResult {
        public bool Transitioned { get; }
        public CombatCoreState PreviousState { get; }
        public CombatCoreState CurrentState { get; }
        public string Reason { get; }

        public CombatStepResult(bool transitioned, CombatCoreState previousState, CombatCoreState currentState,
            string reason) {
            Transitioned = transitioned;
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason ?? string.Empty;
        }
    }

    public enum RevealRequestClassification {
        Unknown = 0,
        CounterConfirmed = 1,
        GenericHit = 2,
        FailedDodge = 3,
        FailedParry = 4,
        InvalidCounter = 5,
        PresentationOnly = 6
    }

    public readonly struct RevealRequestContext {
        public CombatRequestSourceType RequestSourceType { get; }
        public RevealRequestClassification Classification { get; }
        public string CombatResultSourceLabel { get; }
        public string SourceId { get; }
        public string MemoryId { get; }
        public string ContextLabel { get; }

        public RevealRequestContext(
            CombatRequestSourceType requestSourceType,
            string combatResultSourceLabel,
            string sourceId,
            string memoryId,
            string contextLabel,
            RevealRequestClassification classification) {
            RequestSourceType = requestSourceType;
            Classification = classification;
            CombatResultSourceLabel = combatResultSourceLabel ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            MemoryId = memoryId ?? string.Empty;
            ContextLabel = contextLabel ?? string.Empty;
        }

        public RevealRequestContext(
            CombatRequestSourceType requestSourceType,
            string combatResultSourceLabel,
            string sourceId,
            string memoryId,
            string contextLabel)
            : this(
                requestSourceType,
                combatResultSourceLabel,
                sourceId,
                memoryId,
                contextLabel,
                RevealRequestClassification.Unknown) { }

        public RevealRequestContext(string sourceId, string memoryId, string contextLabel)
            : this(
                CombatRequestSourceType.Unknown,
                string.Empty,
                sourceId,
                memoryId,
                contextLabel,
                RevealRequestClassification.Unknown) { }
    }

    public enum MemoryVFXResponseState {
        Idle = 0,
        Requested = 1,
        Playing = 2,
        CoolingDown = 3,
        Rejected = 4,
        Ignored = 5
    }

    public static class MemoryVFXResponseReasons {
        public const string GenericHit = "generic_hit";
        public const string FailedDodge = "failed_dodge";
        public const string FailedParry = "failed_parry";
        public const string PresentationOnly = "presentation_only";
        public const string InCooldown = "in_cooldown";
        public const string AlreadyPlaying = "already_playing";
        public const string NotAcceptedByMemoryState = "not_accepted_by_memory_state";
    }

    public interface IAcceptedMemoryRevealContext {
        string MemoryId { get; }
        RevealRequestContext RevealRequest { get; }
        RevealRequestResult RevealResult { get; }
        string SourceLabel { get; }
        string ContextLabel { get; }
    }

    public readonly struct AcceptedMemoryRevealContext : IAcceptedMemoryRevealContext {
        public string MemoryId { get; }
        public RevealRequestContext RevealRequest { get; }
        public RevealRequestResult RevealResult { get; }
        public string SourceLabel { get; }
        public string ContextLabel { get; }

        public AcceptedMemoryRevealContext(
            string memoryId,
            RevealRequestContext revealRequest,
            RevealRequestResult revealResult,
            string sourceLabel,
            string contextLabel) {
            MemoryId = memoryId ?? string.Empty;
            RevealRequest = revealRequest;
            RevealResult = revealResult;
            SourceLabel = sourceLabel ?? string.Empty;
            ContextLabel = contextLabel ?? string.Empty;
        }

        public AcceptedMemoryRevealContext(IAcceptedMemoryRevealContext source)
            : this(
                source == null ? string.Empty : source.MemoryId,
                source == null ? new RevealRequestContext(string.Empty, string.Empty, string.Empty) : source.RevealRequest,
                source == null ? new RevealRequestResult(false, string.Empty) : source.RevealResult,
                source == null ? string.Empty : source.SourceLabel,
                source == null ? string.Empty : source.ContextLabel) { }
    }

    public interface IMemoryVFXResponseSnapshot {
        MemoryVFXResponseState State { get; }
        IAcceptedMemoryRevealContext SourceAcceptedContext { get; }
        string RejectionReason { get; }
        float CooldownProgress { get; }
        string IntensityLabel { get; }
    }

    public readonly struct MemoryVFXResponseSnapshot : IMemoryVFXResponseSnapshot {
        private readonly AcceptedMemoryRevealContext? sourceAcceptedContext;

        public MemoryVFXResponseState State { get; }
        public IAcceptedMemoryRevealContext SourceAcceptedContext {
            get { return sourceAcceptedContext.HasValue ? (IAcceptedMemoryRevealContext)sourceAcceptedContext.Value : null; }
        }
        public string RejectionReason { get; }
        public float CooldownProgress { get; }
        public string IntensityLabel { get; }

        public MemoryVFXResponseSnapshot(
            MemoryVFXResponseState state,
            AcceptedMemoryRevealContext? sourceAcceptedContext,
            string rejectionReason,
            float cooldownProgress,
            string intensityLabel) {
            State = state;
            this.sourceAcceptedContext = sourceAcceptedContext;
            RejectionReason = rejectionReason ?? string.Empty;
            CooldownProgress = cooldownProgress;
            IntensityLabel = intensityLabel ?? string.Empty;
        }
    }

    public readonly struct MovementRestrictionContext {
        public bool CanTranslate { get; }
        public bool CanRotate { get; }
        public float RestrictionStrength { get; }
        public string Source { get; }

        public MovementRestrictionContext(bool canTranslate, bool canRotate, float restrictionStrength, string source) {
            CanTranslate = canTranslate;
            CanRotate = canRotate;
            RestrictionStrength = restrictionStrength;
            Source = source;
        }
    }

    public enum LocomotionState {
        Uninitialized = 0,
        Idle = 1,
        Moving = 2,
        Restricted = 3,
        Recovering = 4
    }

    public readonly struct LocomotionStateSnapshot {
        public LocomotionState State { get; }
        public Axis2 MoveIntent { get; }
        public bool InputEnabled { get; }
        public MovementRestrictionContext MovementRestriction { get; }
        public RecoveryContext Recovery { get; }
        public CameraMovementBasisSnapshot CameraMovementBasis { get; }
        public string StateDetail { get; }
        public bool HasMoveIntent => MoveIntent.X != 0f || MoveIntent.Y != 0f;

        public bool IsRestricted => State == LocomotionState.Restricted;
        public bool IsRecovering => State == LocomotionState.Recovering;

        public LocomotionStateSnapshot(
            LocomotionState state,
            Axis2 moveIntent,
            bool inputEnabled,
            MovementRestrictionContext movementRestriction,
            RecoveryContext recovery,
            CameraMovementBasisSnapshot cameraMovementBasis,
            string stateDetail) {
            State = state;
            MoveIntent = moveIntent;
            InputEnabled = inputEnabled;
            MovementRestriction = movementRestriction;
            Recovery = recovery;
            CameraMovementBasis = cameraMovementBasis;
            StateDetail = stateDetail ?? string.Empty;
        }
    }

    public readonly struct FacingContextSnapshot {
        public Axis2 FacingDirection { get; }
        public Axis2 TargetDirection { get; }
        public bool HasTarget { get; }

        public FacingContextSnapshot(Axis2 facingDirection, Axis2 targetDirection, bool hasTarget) {
            FacingDirection = facingDirection;
            TargetDirection = targetDirection;
            HasTarget = hasTarget;
        }
    }

    public readonly struct LocomotionTransitionRecord {
        public string FromState { get; }
        public string ToState { get; }
        public string Reason { get; }
        public float TimeSeconds { get; }

        public LocomotionTransitionRecord(string fromState, string toState, string reason, float timeSeconds) {
            FromState = fromState;
            ToState = toState;
            Reason = reason;
            TimeSeconds = timeSeconds;
        }
    }

    public readonly struct DodgeRequestContext {
        public bool Requested { get; }
        public string Source { get; }
        public bool IsCameraRelative { get; }

        public DodgeRequestContext(bool requested, string source, bool isCameraRelative) {
            Requested = requested;
            Source = source;
            IsCameraRelative = isCameraRelative;
        }
    }

    public readonly struct DodgePhaseContext {
        public string PhaseName { get; }
        public float RemainingSeconds { get; }

        public DodgePhaseContext(string phaseName, float remainingSeconds) {
            PhaseName = phaseName;
            RemainingSeconds = remainingSeconds;
        }
    }

    public readonly struct DodgeResultContext {
        public bool Accepted { get; }
        public bool Invulnerable { get; }
        public string Reason { get; }

        public DodgeResultContext(bool accepted, bool invulnerable, string reason) {
            Accepted = accepted;
            Invulnerable = invulnerable;
            Reason = reason;
        }
    }

    public enum EnemyIntentState {
        Idle = 0,
        Telegraph = 1,
        Commit = 2,
        Active = 3,
        Recovery = 4
    }

    public readonly struct EnemyAttackIntentContext {
        public string AttackId { get; }
        public string IntentLabel { get; }
        public float DurationSeconds { get; }
        public EnemyAttackTagSet AttackTags { get; }

        public EnemyAttackIntentContext(string attackId, string intentLabel, float durationSeconds,
            EnemyAttackTagSet attackTags) {
            AttackId = attackId ?? string.Empty;
            IntentLabel = intentLabel ?? string.Empty;
            DurationSeconds = durationSeconds;
            AttackTags = attackTags;
        }
    }

    public readonly struct EnemyIntentSnapshot {
        public EnemyIntentState State { get; }
        public string EnemyId { get; }
        public string IntentLabel { get; }
        public bool IsTelegraphing { get; }
        public float RemainingSeconds { get; }
        public TelegraphStateSnapshot Telegraph { get; }
        public EnemyAttackIntentContext AttackIntent { get; }
        public EnemyPunishWindowContext PunishWindow { get; }

        public EnemyIntentSnapshot(
            EnemyIntentState state,
            string enemyId,
            string intentLabel,
            bool isTelegraphing,
            float remainingSeconds,
            TelegraphStateSnapshot telegraph,
            EnemyAttackIntentContext attackIntent,
            EnemyPunishWindowContext punishWindow) {
            State = state;
            EnemyId = enemyId ?? string.Empty;
            IntentLabel = intentLabel ?? string.Empty;
            IsTelegraphing = isTelegraphing;
            RemainingSeconds = remainingSeconds;
            Telegraph = telegraph;
            AttackIntent = attackIntent;
            PunishWindow = punishWindow;
        }
    }

    public readonly struct TelegraphStateSnapshot {
        public string TelegraphId { get; }
        public bool IsActive { get; }
        public float RemainingSeconds { get; }

        public TelegraphStateSnapshot(string telegraphId, bool isActive, float remainingSeconds) {
            TelegraphId = telegraphId;
            IsActive = isActive;
            RemainingSeconds = remainingSeconds;
        }
    }

    public readonly struct EnemyAttackTagSet {
        public string[] Tags { get; }

        public EnemyAttackTagSet(string[] tags) {
            Tags = tags;
        }
    }

    public readonly struct EnemyPunishWindowContext {
        public bool IsOpen { get; }
        public float RemainingSeconds { get; }
        public string Source { get; }

        public EnemyPunishWindowContext(bool isOpen, float remainingSeconds, string source) {
            IsOpen = isOpen;
            RemainingSeconds = remainingSeconds;
            Source = source;
        }
    }

    public enum TargetFocusState {
        Inactive = 0,
        AcquireRequested = 1,
        Focused = 2,
        Invalid = 3
    }

    public readonly struct TargetDirectionContext {
        public Axis2 Direction { get; }
        public bool HasDirection { get; }
        public string Label { get; }

        public TargetDirectionContext(Axis2 direction, bool hasDirection, string label) {
            Direction = direction;
            HasDirection = hasDirection;
            Label = label;
        }
    }

    public readonly struct TargetAcquireRequest {
        public string TargetId { get; }
        public string Source { get; }
        public string Reason { get; }

        public TargetAcquireRequest(string targetId, string source, string reason) {
            TargetId = targetId;
            Source = source;
            Reason = reason;
        }
    }

    public readonly struct TargetReleaseRequest {
        public TargetReleaseReason Reason { get; }
        public string Source { get; }
        public string Detail { get; }

        public TargetReleaseRequest(TargetReleaseReason reason, string source, string detail) {
            Reason = reason;
            Source = source;
            Detail = detail;
        }
    }

    public readonly struct TargetValidityContext {
        public string TargetId { get; }
        public bool IsValid { get; }
        public string Reason { get; }

        public TargetValidityContext(string targetId, bool isValid, string reason) {
            TargetId = targetId;
            IsValid = isValid;
            Reason = reason;
        }
    }

    public readonly struct TargetContextSnapshot {
        public TargetFocusState FocusState { get; }
        public string TargetId { get; }
        public bool IsLockedOn => FocusState == TargetFocusState.Focused;
        public bool IsValid { get; }
        public TargetDirectionContext Direction { get; }
        public string AcquireReason { get; }
        public string ReleaseReason { get; }
        public string InvalidReason { get; }
        public bool HasTarget => !string.IsNullOrEmpty(TargetId);

        public TargetContextSnapshot(
            TargetFocusState focusState,
            string targetId,
            bool isValid,
            TargetDirectionContext direction,
            string acquireReason,
            string releaseReason,
            string invalidReason) {
            FocusState = focusState;
            TargetId = targetId;
            IsValid = isValid;
            Direction = direction;
            AcquireReason = acquireReason;
            ReleaseReason = releaseReason;
            InvalidReason = invalidReason;
        }
    }

    public readonly struct TargetAcquireResult {
        public bool Accepted { get; }
        public string TargetId { get; }
        public string Reason { get; }

        public TargetAcquireResult(bool accepted, string targetId, string reason) {
            Accepted = accepted;
            TargetId = targetId;
            Reason = reason;
        }
    }

    public readonly struct TargetDebugSnapshot {
        public string Summary { get; }
        public System.Collections.Generic.IReadOnlyList<string> Details { get; }

        public TargetDebugSnapshot(string summary, System.Collections.Generic.IReadOnlyList<string> details) {
            Summary = summary;
            Details = details;
        }
    }

    public enum TargetReleaseReason {
        Unknown = 0,
        Manual = 1,
        Invalid = 2,
        OutOfRange = 3,
        EncounterReset = 4,
        TargetLost = 5
    }

    public readonly struct CameraMovementBasisSnapshot {
        public Axis2 Forward { get; }
        public Axis2 Right { get; }
        public bool IsValid { get; }
        public string CameraModeLabel { get; }

        public CameraMovementBasisSnapshot(Axis2 forward, Axis2 right, bool isValid, string cameraModeLabel) {
            Forward = forward;
            Right = right;
            IsValid = isValid;
            CameraModeLabel = cameraModeLabel;
        }
    }

    public readonly struct DamageApplicationContext {
        public string SourceId { get; }
        public string TargetId { get; }
        public float Amount { get; }
        public string DamageType { get; }
        public string ContextLabel { get; }

        public DamageApplicationContext(string sourceId, string targetId, float amount, string damageType,
            string contextLabel) {
            SourceId = sourceId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Amount = amount;
            DamageType = damageType ?? string.Empty;
            ContextLabel = contextLabel ?? string.Empty;
        }

        public DamageApplicationContext(string sourceId, float amount, string damageType)
            : this(sourceId, string.Empty, amount, damageType, string.Empty) { }
    }

    public enum HealthState {
        Living = 0,
        Damaged = 1,
        Recovering = 2,
        Disabled = 3
    }

    public enum DamageApplicationResultType {
        Accepted = 0,
        Rejected = 1,
        Ignored = 2
    }

    public readonly struct DamageApplicationResult {
        public DamageApplicationResultType Result { get; }
        public bool Accepted { get; }
        public string Reason { get; }
        public float AppliedAmount { get; }

        public DamageApplicationResult(DamageApplicationResultType result, string reason, float appliedAmount) {
            Result = result;
            Accepted = result == DamageApplicationResultType.Accepted;
            Reason = reason ?? string.Empty;
            AppliedAmount = appliedAmount;
        }
    }

    public readonly struct HealthStateSnapshot {
        public HealthState State { get; }
        public float Current { get; }
        public float Max { get; }
        public bool IsAlive { get; }
        public DamageApplicationResult LastDamageResult { get; }
        public HitReactionContext HitReaction { get; }
        public DefeatStateContext Defeat { get; }

        public HealthStateSnapshot(
            HealthState state,
            float current,
            float max,
            bool isAlive,
            DamageApplicationResult lastDamageResult,
            HitReactionContext hitReaction,
            DefeatStateContext defeat) {
            State = state;
            Current = current;
            Max = max;
            IsAlive = isAlive;
            LastDamageResult = lastDamageResult;
            HitReaction = hitReaction;
            Defeat = defeat;
        }

        public HealthStateSnapshot(float current, float max, bool isAlive)
            : this(
                isAlive ? HealthState.Living : HealthState.Disabled,
                current,
                max,
                isAlive,
                new DamageApplicationResult(DamageApplicationResultType.Ignored, "No damage processed yet", 0f),
                new HitReactionContext(string.Empty, string.Empty, 0f),
                new DefeatStateContext(!isAlive, isAlive ? string.Empty : "Disabled")) { }
    }

    public readonly struct HitReactionContext {
        public string SourceId { get; }
        public string ReactionLabel { get; }
        public float SuppressionSeconds { get; }

        public HitReactionContext(string sourceId, string reactionLabel, float suppressionSeconds) {
            SourceId = sourceId;
            ReactionLabel = reactionLabel;
            SuppressionSeconds = suppressionSeconds;
        }
    }

    public readonly struct DefeatStateContext {
        public bool IsDefeated { get; }
        public string Reason { get; }

        public DefeatStateContext(bool isDefeated, string reason) {
            IsDefeated = isDefeated;
            Reason = reason;
        }
    }

    public enum RevealRequestDecision {
        Accepted = 0,
        Rejected = 1
    }

    public readonly struct RevealRequestResult {
        public RevealRequestDecision Decision { get; }

        public bool Accepted {
            get { return Decision == RevealRequestDecision.Accepted; }
        }

        public string Reason { get; }
        public string ResultContext { get; }
        public RevealRequestClassification RequestClassification { get; }
        public string MemoryId { get; }

        public RevealRequestResult(
            RevealRequestDecision decision,
            string reason,
            string resultContext,
            RevealRequestClassification requestClassification,
            string memoryId) {
            Decision = decision;
            Reason = reason ?? string.Empty;
            ResultContext = resultContext ?? string.Empty;
            RequestClassification = requestClassification;
            MemoryId = memoryId ?? string.Empty;
        }

        public RevealRequestResult(bool accepted, string reason)
            : this(
                accepted ? RevealRequestDecision.Accepted : RevealRequestDecision.Rejected,
                reason,
                string.Empty,
                RevealRequestClassification.Unknown,
                string.Empty) { }
    }

    public enum MemoryRevealPhase {
        Dormant = 0,
        Requested = 1,
        Accepted = 2,
        Rejected = 3,
        Responding = 4,
        Cooldown = 5
    }

    public readonly struct MemoryResponseContext {
        public string MemoryId { get; }
        public string ResponseLabel { get; }
        public bool ResponseActive { get; }
        public string Detail { get; }

        public bool IsAccepted {
            get { return ResponseActive; }
        }

        public MemoryResponseContext(string memoryId, string responseLabel, bool responseActive, string detail) {
            MemoryId = memoryId ?? string.Empty;
            ResponseLabel = responseLabel ?? string.Empty;
            ResponseActive = responseActive;
            Detail = detail ?? string.Empty;
        }

        public MemoryResponseContext(string memoryId, string responseLabel, bool isAccepted)
            : this(memoryId, responseLabel, isAccepted, string.Empty) { }
    }

    public readonly struct MemoryCooldownContext {
        public bool CooldownActive { get; }
        public float RemainingSeconds { get; }
        public string Reason { get; }

        public MemoryCooldownContext(bool cooldownActive, float remainingSeconds, string reason) {
            CooldownActive = cooldownActive;
            RemainingSeconds = remainingSeconds;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct MemoryStateSnapshot {
        public string MemoryId { get; }
        public MemoryRevealPhase Phase { get; }
        public RevealRequestContext LastRequest { get; }
        public RevealRequestResult LastResult { get; }
        public MemoryResponseContext Response { get; }
        public MemoryCooldownContext Cooldown { get; }

        public bool IsRevealed {
            get { return Phase == MemoryRevealPhase.Responding || Phase == MemoryRevealPhase.Cooldown; }
        }

        public float CooldownSeconds {
            get { return Cooldown.RemainingSeconds; }
        }

        public MemoryStateSnapshot(
            string memoryId,
            MemoryRevealPhase phase,
            RevealRequestContext lastRequest,
            RevealRequestResult lastResult,
            MemoryResponseContext response,
            MemoryCooldownContext cooldown) {
            MemoryId = memoryId ?? string.Empty;
            Phase = phase;
            LastRequest = lastRequest;
            LastResult = lastResult;
            Response = response;
            Cooldown = cooldown;
        }

        public MemoryStateSnapshot(string memoryId, bool isRevealed, float cooldownSeconds)
            : this(
                memoryId,
                isRevealed ? MemoryRevealPhase.Responding : MemoryRevealPhase.Dormant,
                new RevealRequestContext(string.Empty, memoryId, string.Empty),
                new RevealRequestResult(
                    isRevealed ? RevealRequestDecision.Accepted : RevealRequestDecision.Rejected,
                    isRevealed ? "Reveal active" : "Reveal dormant",
                    string.Empty,
                    RevealRequestClassification.Unknown,
                    memoryId),
                new MemoryResponseContext(memoryId, string.Empty, isRevealed, string.Empty),
                new MemoryCooldownContext(cooldownSeconds > 0f, cooldownSeconds, string.Empty)) { }
    }

    public readonly struct EncounterStateSnapshot {
        public string EncounterId { get; }
        public bool IsActive { get; }
        public int ParticipantCount { get; }

        public EncounterStateSnapshot(string encounterId, bool isActive, int participantCount) {
            EncounterId = encounterId;
            IsActive = isActive;
            ParticipantCount = participantCount;
        }
    }

    public readonly struct EncounterStartContext {
        public string EncounterId { get; }
        public string Reason { get; }

        public EncounterStartContext(string encounterId, string reason) {
            EncounterId = encounterId;
            Reason = reason;
        }
    }

    public readonly struct EncounterEndContext {
        public string EncounterId { get; }
        public string EndReason { get; }

        public EncounterEndContext(string encounterId, string endReason) {
            EncounterId = encounterId;
            EndReason = endReason;
        }
    }

    public readonly struct EncounterResetContext {
        public string EncounterId { get; }
        public string ResetReason { get; }

        public EncounterResetContext(string encounterId, string resetReason) {
            EncounterId = encounterId;
            ResetReason = resetReason;
        }
    }

    public enum EncounterLifecycleState {
        Uninitialized = 0,
        Preparing = 1,
        Ready = 2,
        Starting = 3,
        Active = 4,
        Completing = 5,
        Completed = 6,
        Failed = 7,
        Aborted = 8,
        Resetting = 9
    }

    public enum EncounterParticipantRole {
        Player = 0,
        Enemy = 1
    }

    public enum EncounterLifecycleRequestKind {
        Prepare = 0,
        Start = 1,
        Complete = 2,
        Fail = 3,
        Abort = 4,
        Reset = 5
    }

    public readonly struct EncounterParticipantRegistration {
        public string ParticipantId { get; }
        public string SourceLabel { get; }
        public string Reason { get; }

        public EncounterParticipantRegistration(string participantId, string sourceLabel, string reason) {
            ParticipantId = participantId ?? string.Empty;
            SourceLabel = sourceLabel ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct EncounterParticipantContext {
        public EncounterParticipantRole Role { get; }
        public string ParticipantId { get; }
        public string SourceLabel { get; }
        public bool IsRegistered { get; }
        public string Reason { get; }

        public EncounterParticipantContext(
            EncounterParticipantRole role,
            string participantId,
            string sourceLabel,
            bool isRegistered,
            string reason) {
            Role = role;
            ParticipantId = participantId ?? string.Empty;
            SourceLabel = sourceLabel ?? string.Empty;
            IsRegistered = isRegistered;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct EncounterReadinessBlocker {
        public string Code { get; }
        public string Reason { get; }

        public EncounterReadinessBlocker(string code, string reason) {
            Code = code ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct EncounterLifecycleRequest {
        public EncounterLifecycleRequestKind Kind { get; }
        public string Reason { get; }

        public EncounterLifecycleRequest(EncounterLifecycleRequestKind kind, string reason) {
            Kind = kind;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct EncounterLifecycleResult {
        public EncounterLifecycleRequestKind Kind { get; }
        public bool Accepted { get; }
        public EncounterLifecycleState PreviousState { get; }
        public EncounterLifecycleState CurrentState { get; }
        public string Reason { get; }

        public EncounterLifecycleResult(
            EncounterLifecycleRequestKind kind,
            bool accepted,
            EncounterLifecycleState previousState,
            EncounterLifecycleState currentState,
            string reason) {
            Kind = kind;
            Accepted = accepted;
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct EncounterObservationContext {
        public bool PlayerDefeated { get; }
        public bool EnemyDefeated { get; }
        public bool RevealAccepted { get; }
        public bool ManualResetRequested { get; }
        public bool ManualAbortRequested { get; }
        public string MemoryId { get; }
        public string Reason { get; }

        public EncounterObservationContext(
            bool playerDefeated,
            bool enemyDefeated,
            bool revealAccepted,
            bool manualResetRequested,
            bool manualAbortRequested,
            string memoryId,
            string reason) {
            PlayerDefeated = playerDefeated;
            EnemyDefeated = enemyDefeated;
            RevealAccepted = revealAccepted;
            ManualResetRequested = manualResetRequested;
            ManualAbortRequested = manualAbortRequested;
            MemoryId = memoryId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct EncounterLifecycleSnapshot {
        public string EncounterId { get; }
        public EncounterLifecycleState State { get; }
        public EncounterParticipantContext PlayerParticipant { get; }
        public EncounterParticipantContext EnemyParticipant { get; }
        public System.Collections.Generic.IReadOnlyList<EncounterReadinessBlocker> ReadinessBlockers { get; }
        public EncounterObservationContext Observation { get; }
        public EncounterLifecycleResult LastResult { get; }
        public string LastReason { get; }
        public float ElapsedSeconds { get; }
        public int ParticipantCount { get; }
        public bool IsReady => State == EncounterLifecycleState.Ready;
        public bool IsActive => State == EncounterLifecycleState.Active;

        public EncounterLifecycleSnapshot(
            string encounterId,
            EncounterLifecycleState state,
            EncounterParticipantContext playerParticipant,
            EncounterParticipantContext enemyParticipant,
            System.Collections.Generic.IReadOnlyList<EncounterReadinessBlocker> readinessBlockers,
            EncounterObservationContext observation,
            EncounterLifecycleResult lastResult,
            string lastReason,
            float elapsedSeconds) {
            EncounterId = encounterId ?? string.Empty;
            State = state;
            PlayerParticipant = playerParticipant;
            EnemyParticipant = enemyParticipant;
            ReadinessBlockers = readinessBlockers ?? new EncounterReadinessBlocker[0];
            Observation = observation;
            LastResult = lastResult;
            LastReason = lastReason ?? string.Empty;
            ElapsedSeconds = elapsedSeconds;
            ParticipantCount = (playerParticipant.IsRegistered ? 1 : 0) + (enemyParticipant.IsRegistered ? 1 : 0);
        }
    }

    public readonly struct CombatDebugSnapshot {
        public string Summary { get; }
        public string[] Details { get; }

        public CombatDebugSnapshot(string summary, string[] details) {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct LocomotionDebugSnapshot {
        public string Summary { get; }
        public System.Collections.Generic.IReadOnlyList<string> Details { get; }

        public LocomotionDebugSnapshot(string summary, System.Collections.Generic.IReadOnlyList<string> details) {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct EnemyIntentDebugSnapshot {
        public string Summary { get; }
        public string[] Details { get; }

        public EnemyIntentDebugSnapshot(string summary, string[] details) {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct HealthDebugSnapshot {
        public string Summary { get; }
        public string[] Details { get; }

        public HealthDebugSnapshot(string summary, string[] details) {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct MemoryDebugSnapshot {
        public string Summary { get; }
        public string[] Details { get; }

        public MemoryDebugSnapshot(string summary, string[] details) {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct CameraDebugSnapshot {
        public string Summary { get; }
        public string[] Details { get; }

        public CameraDebugSnapshot(string summary, string[] details) {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct EncounterDebugSnapshot {
        public string Summary { get; }
        public string[] Details { get; }

        public EncounterDebugSnapshot(string summary, string[] details) {
            Summary = summary;
            Details = details;
        }
    }

    public readonly struct InputDebugSnapshot {
        public string Summary { get; }
        public System.Collections.Generic.IReadOnlyList<string> Details { get; }

        public InputDebugSnapshot(string summary, System.Collections.Generic.IReadOnlyList<string> details) {
            Summary = summary;
            Details = details;
        }
    }

    public enum DebugOverlayChannelId {
        Input = 0,
        Locomotion = 1,
        TargetContext = 2,
        CombatCore = 3,
        EnemyIntent = 4,
        Health = 5,
        MemoryState = 6,
        MemoryVFXResponse = 7,
        EncounterFramework = 8
    }

    public interface IDebugOverlayChannelSnapshot {
        DebugOverlayChannelId ChannelId { get; }
        string ChannelLabel { get; }
        bool IsVisible { get; }
        string LastReason { get; }
        object SourceSnapshot { get; }
    }

    public readonly struct DebugOverlayChannelSnapshot<TSnapshot> : IDebugOverlayChannelSnapshot {
        public DebugOverlayChannelId ChannelId { get; }
        public string ChannelLabel { get; }
        public bool IsVisible { get; }
        public TSnapshot Snapshot { get; }
        public string LastReason { get; }
        public TSnapshot SourceSnapshot { get { return Snapshot; } }
        object IDebugOverlayChannelSnapshot.SourceSnapshot => Snapshot;

        public DebugOverlayChannelSnapshot(
            DebugOverlayChannelId channelId,
            string channelLabel,
            bool isVisible,
            TSnapshot snapshot,
            string lastReason) {
            ChannelId = channelId;
            ChannelLabel = channelLabel ?? string.Empty;
            IsVisible = isVisible;
            Snapshot = snapshot;
            LastReason = lastReason ?? string.Empty;
        }
    }

    public readonly struct DebugOverlayAggregateSnapshot {
        public DebugOverlayChannelSnapshot<InputIntentSnapshot> Input { get; }
        public DebugOverlayChannelSnapshot<LocomotionStateSnapshot> Locomotion { get; }
        public DebugOverlayChannelSnapshot<TargetContextSnapshot> TargetContext { get; }
        public DebugOverlayChannelSnapshot<M0CombatSnapshot> CombatCore { get; }
        public DebugOverlayChannelSnapshot<EnemyIntentSnapshot> EnemyIntent { get; }
        public DebugOverlayChannelSnapshot<HealthStateSnapshot> Health { get; }
        public DebugOverlayChannelSnapshot<MemoryStateSnapshot> MemoryState { get; }
        public DebugOverlayChannelSnapshot<IMemoryVFXResponseSnapshot> MemoryVFXResponse { get; }
        public DebugOverlayChannelSnapshot<EncounterLifecycleSnapshot> EncounterFramework { get; }
        public System.Collections.Generic.IReadOnlyList<IDebugOverlayChannelSnapshot> Channels { get; }
        public int ChannelCount { get; }

        public DebugOverlayAggregateSnapshot(
            DebugOverlayChannelSnapshot<InputIntentSnapshot> input,
            DebugOverlayChannelSnapshot<LocomotionStateSnapshot> locomotion,
            DebugOverlayChannelSnapshot<TargetContextSnapshot> targetContext,
            DebugOverlayChannelSnapshot<M0CombatSnapshot> combatCore,
            DebugOverlayChannelSnapshot<EnemyIntentSnapshot> enemyIntent,
            DebugOverlayChannelSnapshot<HealthStateSnapshot> health,
            DebugOverlayChannelSnapshot<MemoryStateSnapshot> memoryState,
            DebugOverlayChannelSnapshot<IMemoryVFXResponseSnapshot> memoryVfxResponse,
            DebugOverlayChannelSnapshot<EncounterLifecycleSnapshot> encounterFramework) {
            Input = input;
            Locomotion = locomotion;
            TargetContext = targetContext;
            CombatCore = combatCore;
            EnemyIntent = enemyIntent;
            Health = health;
            MemoryState = memoryState;
            MemoryVFXResponse = memoryVfxResponse;
            EncounterFramework = encounterFramework;
            Channels = new IDebugOverlayChannelSnapshot[] {
                input,
                locomotion,
                targetContext,
                combatCore,
                enemyIntent,
                health,
                memoryState,
                memoryVfxResponse,
                encounterFramework
            };
            ChannelCount = Channels.Count;
        }
    }

    public readonly struct DebugTransitionEvent {
        public string SystemName { get; }
        public string EventName { get; }
        public string Detail { get; }
        public bool Accepted { get; }

        public DebugTransitionEvent(string systemName, string eventName, string detail, bool accepted) {
            SystemName = systemName;
            EventName = eventName;
            Detail = detail;
            Accepted = accepted;
        }
    }
}
