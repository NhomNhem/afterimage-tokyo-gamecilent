using UnityEngine;
using VContainer;
using GlassRefrain.Camera;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Presentation;
using GlassRefrain.Targeting;
using NhemDangFugBixs.NhemLogging;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;

namespace GlassRefrain.Bootstrap {
    public class M0GameplayTickHandler : SerializedMonoBehaviour {
        [OdinSerialize] private M0PlayerLocomotionAdapter adapter;
        [OdinSerialize] private M0DirectPlayerInput directInput;
        [OdinSerialize] private CameraMovementBasisProvider cameraBasisProvider;
        [OdinSerialize] private M0CombatVisualFeedbackAdapter visualFeedbackAdapter;
        [OdinSerialize] private M0CombatDebugOverlayAdapter debugOverlayAdapter;
        [OdinSerialize] private M0AnimationPresentationAdapter animationPresentationAdapter;

        private M0PlayerLocomotion _locomotion;
        private M0TargetContext _targetContext;
        private M0CombatCore _combatCore;
        private M0EnemyIntentModel _enemyIntentModel;
        private M0EnemyIntentLoopDriver _enemyIntentLoopDriver;
        private M0InputRouter _inputRouter;
        private IM0MemoryState _memoryState;
        private M0MemoryVFXResponse _memoryVfxResponse;
        private MemoryInteractionService _memoryInteractionService;
        private INhemLogger _logger;

        private bool _warnedMissingBasis;
        private bool _warnedInvalidBasis;
#if GR_ENEMY_DEBUG
        private float _enemyLoopTickDebugTimer;
#endif

        private M0CombatSnapshot lastCombatSnapshot;
        private EnemyIntentSnapshot lastEnemyIntentSnapshot;
        private InputIntentSnapshot lastInputSnapshot;
        private TargetContextSnapshot lastTargetSnapshot;
        private readonly M1MemoryRevealFeedbackBridge _memoryRevealFeedbackBridge = new M1MemoryRevealFeedbackBridge();
        private readonly M1RuntimeMemoryLogPlaceholder _runtimeMemoryLogPlaceholder = new M1RuntimeMemoryLogPlaceholder();
        private bool _loggedAdapterMissing;
        private bool _dodgeDisplacementArmed;
        private Vector3 _encounterResetStartPosition;
        private Vector3 _encounterResetStartFacing;
        private readonly List<InputActionIntent> _triggeredInputActions = new List<InputActionIntent>(8);
        private bool _interactTriggeredThisFrame;

        public void SetVisualFeedbackAdapter(M0CombatVisualFeedbackAdapter adapter) {
            visualFeedbackAdapter = adapter;
        }

        public void SetDebugOverlayAdapter(M0CombatDebugOverlayAdapter adapter) {
            debugOverlayAdapter = adapter;
        }

        public void SetAnimationPresentationAdapter(M0AnimationPresentationAdapter adapter) {
            animationPresentationAdapter = adapter;
        }

        [Inject]
        public void Construct(M0PlayerLocomotion locomotion, M0TargetContext targetContext, M0CombatCore combatCore, M0EnemyIntentModel enemyIntentModel, M0EnemyIntentLoopDriver enemyIntentLoopDriver, M0InputRouter inputRouter, IM0MemoryState memoryState, M0MemoryVFXResponse memoryVfxResponse, MemoryInteractionService memoryInteractionService, INhemLogger logger) {
            _locomotion = locomotion;
            _targetContext = targetContext;
            _combatCore = combatCore;
            _enemyIntentModel = enemyIntentModel;
            _enemyIntentLoopDriver = enemyIntentLoopDriver;
            _inputRouter = inputRouter;
            _memoryState = memoryState;
            _memoryVfxResponse = memoryVfxResponse;
            _memoryInteractionService = memoryInteractionService;
            _logger = logger;
#if GR_ENEMY_DEBUG
            _logger?.Log($"[M0EnemyLoop] TickHandler received loopDriver={_enemyIntentLoopDriver != null}");
#endif

            combatCore.SetTargetContext(targetContext);
            if (adapter != null) {
                adapter.SetLocomotion(locomotion);
                adapter.SetLogger(logger);
                _loggedAdapterMissing = false;
            } else if (!_loggedAdapterMissing) {
                logger?.LogWarning("[M0Locomotion] Adapter reference missing on M0GameplayTickHandler; Player transform will not be updated.");
                _loggedAdapterMissing = true;
            }
            if (directInput != null) {
                directInput.SetLogger(logger);
                directInput.SetInputRouter(inputRouter);
            }

            // Subscribe to snapshot events for presentation adapters
            combatCore.SnapshotChanged += OnCombatSnapshotChanged;
            lastCombatSnapshot = combatCore.Snapshot;

            locomotion.SnapshotChanged += OnLocomotionSnapshotChanged;
            animationPresentationAdapter?.ObserveLocomotionSnapshot(locomotion.Snapshot);

            enemyIntentModel.SnapshotChanged += OnEnemyIntentSnapshotChanged;
            lastEnemyIntentSnapshot = enemyIntentModel.Snapshot;

            inputRouter.SnapshotChanged += OnInputSnapshotChanged;
            lastInputSnapshot = inputRouter.Snapshot;

            targetContext.SnapshotChanged += OnTargetSnapshotChanged;
            lastTargetSnapshot = targetContext.Snapshot;
            combatCore.RevealRequestEmitted += OnRevealRequestEmitted;

            if (adapter != null) {
                _encounterResetStartPosition = adapter.transform.position;
                _encounterResetStartFacing = adapter.transform.forward.sqrMagnitude > 0.000001f
                    ? adapter.transform.forward.normalized
                    : Vector3.forward;
            } else {
                var initialMovementSnapshot = locomotion.GetMovementSnapshot();
                _encounterResetStartPosition = initialMovementSnapshot.Position;
                _encounterResetStartFacing = initialMovementSnapshot.Facing.sqrMagnitude > 0.000001f
                    ? initialMovementSnapshot.Facing
                    : Vector3.forward;
            }

            // Warn if presentation adapters are not assigned in Inspector
            if (visualFeedbackAdapter == null) {
                logger?.LogWarning("[M0Presentation] Visual feedback adapter missing; skipping presentation update");
            }
            if (debugOverlayAdapter == null) {
                logger?.LogWarning("[M0Presentation] Debug overlay adapter missing; skipping presentation update");
            }
            if (animationPresentationAdapter == null) {
                logger?.LogWarning("[M0Animation] Animation presentation adapter missing; combat continues without animation presentation");
            }
#if GR_DEBUG_OVERLAY
            else {
                logger?.Log("[M0DebugOverlay] Adapter initialized");
            }
#endif

            SyncDebugOverlayFromSnapshots();
        }

        private void OnDestroy() {
            if (_combatCore != null) {
                _combatCore.SnapshotChanged -= OnCombatSnapshotChanged;
            }
            if (_locomotion != null) {
                _locomotion.SnapshotChanged -= OnLocomotionSnapshotChanged;
            }
            if (_enemyIntentModel != null) {
                _enemyIntentModel.SnapshotChanged -= OnEnemyIntentSnapshotChanged;
            }
            if (_inputRouter != null) {
                _inputRouter.SnapshotChanged -= OnInputSnapshotChanged;
            }
            if (_targetContext != null) {
                _targetContext.SnapshotChanged -= OnTargetSnapshotChanged;
            }
            if (_combatCore != null) {
                _combatCore.RevealRequestEmitted -= OnRevealRequestEmitted;
            }
        }

        private void Update() {
            float dt = Time.deltaTime;

            _interactTriggeredThisFrame = false;
            HandleInputRouting();

            if (cameraBasisProvider != null) {
                var basis = cameraBasisProvider.GetMovementBasis();
                _locomotion.SetCameraMovementBasis(basis);
                _warnedMissingBasis = false;
#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
                if (!basis.IsValid) {
                    if (!_warnedInvalidBasis) {
                        _logger?.LogWarning("[M0GameplayTickHandler] CameraMovementBasis is invalid; locomotion velocity will remain zero.");
                        _warnedInvalidBasis = true;
                    }
                } else {
                    _warnedInvalidBasis = false;
                }
#endif
            } else {
                if (!_warnedMissingBasis) {
                    _logger?.LogWarning("[M0GameplayTickHandler] CameraMovementBasisProvider not assigned. Camera-relative movement disabled.");
                    _warnedMissingBasis = true;
                }
            }

            _locomotion.ProcessMovementInput(dt);
            _locomotion.UpdatePosition(dt);

            // Tick order: enemy loop driver first, then enemy intent model
            if (_enemyIntentLoopDriver == null) {
                _logger?.LogWarning("[M0GameplayTickHandler] _enemyIntentLoopDriver is null in Update. Tick will not be called.");
            }
#if GR_ENEMY_DEBUG
            _enemyLoopTickDebugTimer += dt;
            if (_enemyLoopTickDebugTimer >= 1f) {
                _logger?.Log($"[M0EnemyLoop] TickHandler ticking loopDriver dt={dt:F3}");
                _enemyLoopTickDebugTimer = 0f;
            }
#endif
            _enemyIntentLoopDriver?.Tick(dt);
            _enemyIntentModel?.Tick(dt);

            // Story 1-6: Combat Core tick for time-based state management (CounterWindow duration expiry).
            _combatCore?.Tick(dt);

            if (_memoryInteractionService != null && _locomotion != null) {
                var playerPosition = _locomotion.GetMovementSnapshot().Position;
                _memoryInteractionService.Tick(playerPosition, _interactTriggeredThisFrame);
                var interactionSnapshot = _memoryInteractionService.Snapshot;
                debugOverlayAdapter?.UpdateInteractionPrompt(
                    interactionSnapshot.HasEligibleFragment,
                    interactionSnapshot.NearbyFragmentId);
                if (_memoryState != null) {
                    var memorySnapshot = _memoryState.Snapshot;
                    _memoryRevealFeedbackBridge.TryPlayAcceptedInteraction(
                        interactionSnapshot,
                        memorySnapshot,
                        _memoryVfxResponse);
                    _runtimeMemoryLogPlaceholder.TryAppendAcceptedInteraction(
                        interactionSnapshot,
                        memorySnapshot);
                    debugOverlayAdapter?.UpdateRuntimeMemoryLog(_runtimeMemoryLogPlaceholder.Entries);
                }
            } else {
                debugOverlayAdapter?.UpdateInteractionPrompt(false, string.Empty);
                debugOverlayAdapter?.UpdateRuntimeMemoryLog(_runtimeMemoryLogPlaceholder.Entries);
            }

            // Story 1-6: Recovery context forwarding — forwards combat recovery state to locomotion each frame.
            // M0PlayerLocomotion.SetRecoveryContext already handles IsRecovering == false as a no-op.
            if (_combatCore != null && _locomotion != null)
                _locomotion.SetRecoveryContext(_combatCore.Snapshot.Recovery);

            if (_memoryVfxResponse != null) {
                _memoryVfxResponse.Update(dt);
                debugOverlayAdapter?.UpdateMemoryRevealFeedback(_memoryVfxResponse.Snapshot);
            } else {
                debugOverlayAdapter?.UpdateMemoryRevealFeedback(null);
            }

            if (_memoryState != null && _memoryVfxResponse != null) {
                var memorySnapshot = _memoryState.Snapshot;
                var vfxState = _memoryVfxResponse.State;
                var transitionedToCooldown = false;

                if (memorySnapshot.Phase == MemoryRevealPhase.Responding &&
                    (vfxState == MemoryVFXResponseState.CoolingDown || vfxState == MemoryVFXResponseState.Idle)) {
                    _memoryState.AdvancePhase("Reveal playback complete");
                    memorySnapshot = _memoryState.Snapshot;
                    transitionedToCooldown = true;
                }

                if (memorySnapshot.Phase == MemoryRevealPhase.Cooldown &&
                    vfxState == MemoryVFXResponseState.Idle &&
                    !transitionedToCooldown) {
                    _memoryState.AdvancePhase("Reveal cooldown complete");
                }
            }
        }

        [ContextMenu("M0 Debug/Reset Encounter")]
        public void DebugResetEncounter() {
            ResetEncounterLifecycle("Debug reset requested");
        }

#if GR_M0_PROTOTYPE || GR_MEMORY_DEBUG
        [ContextMenu("M0 Debug/Trigger Memory Reveal Evidence")]
        public void DebugTriggerMemoryRevealEvidence() {
            if (_combatCore == null) {
                _logger?.LogWarning("[M0Memory] Debug reveal evidence trigger skipped: combat core unavailable");
                return;
            }

            _combatCore.DebugEmitCounterRevealEvidence("DebugCounterRevealEvidence");
            debugOverlayAdapter?.UpdateLastInputAction("DebugRevealEvidence");
#if GR_MEMORY_DEBUG || GR_M0_PROTOTYPE
            _logger?.Log("[M0Memory] Debug reveal evidence trigger requested");
#endif
        }
#endif

        private void ResetEncounterLifecycle(string reason) {
            if (_targetContext == null || _combatCore == null || _locomotion == null || _enemyIntentLoopDriver == null) {
                _logger?.LogWarning("[M0Encounter] Reset skipped: missing runtime dependency");
                return;
            }

            _targetContext.ResetForEncounter("Encounter reset release");
            _combatCore.ResetForEncounter("Encounter reset");
            _locomotion.ResetForEncounter(_encounterResetStartPosition, _encounterResetStartFacing);
            _enemyIntentLoopDriver.ResetForEncounter("Encounter reset");

            _dodgeDisplacementArmed = false;
            debugOverlayAdapter?.UpdateLastInputAction("ResetEncounter");
            SyncDebugOverlayFromSnapshots();

#if GR_M0_PROTOTYPE || GR_COMBAT_DEBUG
            _logger?.Log("[M0Encounter] Reset complete: " + reason);
#endif
        }

        private void SyncDebugOverlayFromSnapshots() {
            if (debugOverlayAdapter == null) return;

            var combatSnapshot = _combatCore != null ? _combatCore.Snapshot : lastCombatSnapshot;
            var enemySnapshot = _enemyIntentModel != null ? _enemyIntentModel.Snapshot : lastEnemyIntentSnapshot;
            var targetSnapshot = _targetContext != null ? _targetContext.Snapshot : lastTargetSnapshot;

            debugOverlayAdapter.UpdateCombatState(combatSnapshot.State.ToString());
            debugOverlayAdapter.UpdateCounterWindowState(
                combatSnapshot.CounterWindow.IsOpen,
                combatSnapshot.CounterWindow.ElapsedSeconds,
                combatSnapshot.CounterWindow.DurationSeconds);
            debugOverlayAdapter.UpdateEnemyIntentState(enemySnapshot.State.ToString());
            debugOverlayAdapter.UpdateLockOnTarget(
                targetSnapshot.IsLockedOn && !string.IsNullOrEmpty(targetSnapshot.TargetId) ? "Enemy" : "None");
        }

        private void OnCombatSnapshotChanged(M0CombatSnapshot snapshot)
        {
            var previousState = lastCombatSnapshot.State;
            var currentState = snapshot.State;

            // Trigger visual feedback on state transitions
            if (visualFeedbackAdapter != null && previousState != currentState)
            {
                switch (currentState)
                {
                    case CombatCoreState.AttackStartup:
                    case CombatCoreState.AttackActive:
                        // Trigger visual feedback (simplified - in full implementation would distinguish Light vs Heavy)
                        visualFeedbackAdapter.TriggerLightAttackFeedback();
                        break;
                    case CombatCoreState.ParryStartup:
                    case CombatCoreState.ParryActive:
                        visualFeedbackAdapter.TriggerParryFeedback();
                        break;
                    case CombatCoreState.DodgeStartup:
                        _dodgeDisplacementArmed = true;
                        visualFeedbackAdapter.TriggerDodgeFeedback();
                        break;
                    case CombatCoreState.DodgeActive:
                        if (_dodgeDisplacementArmed && _locomotion != null) {
                            var before = _locomotion.GetMovementSnapshot().Position;
                            bool started = _locomotion.TryBeginDodgeDisplacement();
                            if (started) {
#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
                                _logger?.Log("[M0Locomotion] Dodge displacement started: before=("
                                             + before.x.ToString("F2") + "," + before.y.ToString("F2") + "," + before.z.ToString("F2") + ")");
#endif
                            }

                            _dodgeDisplacementArmed = false;
                        }

                        visualFeedbackAdapter.TriggerDodgeFeedback();
                        break;
                    case CombatCoreState.CounterActive:
                        visualFeedbackAdapter.TriggerCounterFeedback();
                        break;
                }
            }

            animationPresentationAdapter?.ObserveCombatSnapshot(snapshot);

            // Update debug overlay
            if (debugOverlayAdapter != null)
            {
                debugOverlayAdapter.UpdateCombatState(currentState.ToString());
                debugOverlayAdapter.UpdateCounterWindowState(
                    snapshot.CounterWindow.IsOpen,
                    snapshot.CounterWindow.ElapsedSeconds,
                    snapshot.CounterWindow.DurationSeconds
                );
/*#if GR_DEBUG_OVERLAY
                _logger?.Log($"[M0DebugOverlay] Snapshot update received combatState={currentState} enemyState={lastEnemyIntentSnapshot.State}");
#endif*/
            }

            lastCombatSnapshot = snapshot;
        }

        private void OnLocomotionSnapshotChanged(LocomotionStateSnapshot snapshot) {
            animationPresentationAdapter?.ObserveLocomotionSnapshot(snapshot);
        }

        private void OnEnemyIntentSnapshotChanged(EnemyIntentSnapshot snapshot)
        {
            var previousState = lastEnemyIntentSnapshot.State;
            var currentState = snapshot.State;

            // Update enemy visual feedback based on intent state
            if (visualFeedbackAdapter != null)
            {
                switch (currentState)
                {
                    case EnemyIntentState.Telegraph:
                        visualFeedbackAdapter.SetEnemyTelegraphState();
                        break;
                    case EnemyIntentState.Commit:
                        visualFeedbackAdapter.SetEnemyActiveState();
                        break;
                    case EnemyIntentState.Active:
                        visualFeedbackAdapter.SetEnemyActiveState();
                        break;
                    case EnemyIntentState.Recovery:
                        visualFeedbackAdapter.SetEnemyRecoveryState();
                        break;
                }
            }

            animationPresentationAdapter?.ObserveEnemyIntentSnapshot(snapshot);

            // Update debug overlay
            if (debugOverlayAdapter != null)
            {
                debugOverlayAdapter.UpdateEnemyIntentState(currentState.ToString());

/*#if GR_DEBUG_OVERLAY
                _logger?.Log($"[M0DebugOverlay] Snapshot update received combatState={lastCombatSnapshot.State} enemyState={currentState}");
#endif*/
            }

            lastEnemyIntentSnapshot = snapshot;
        }

        private void OnInputSnapshotChanged(InputIntentSnapshot snapshot)
        {
            // LastInput overlay is written when triggered intents are routed in HandleInputRouting().
            lastInputSnapshot = snapshot;
        }

        private void HandleInputRouting() {
            if (_inputRouter == null) return;

            var inputSnapshot = _inputRouter.Snapshot;
            if (_locomotion != null) {
                var locomotionIntent = new InputIntentSnapshot(
                    inputSnapshot.Move,
                    new Axis2(0f, 0f),
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    inputSnapshot.InputEnabled);

                _locomotion.ConsumeInputIntent(locomotionIntent);
            }

            _triggeredInputActions.Clear();
            _inputRouter.DrainTriggeredActions(_triggeredInputActions);
            if (_triggeredInputActions.Count == 0) return;

            var enemySnapshot = _enemyIntentModel != null ? _enemyIntentModel.Snapshot : default;
            for (var i = 0; i < _triggeredInputActions.Count; i++) {
                var actionIntent = _triggeredInputActions[i];
                switch (actionIntent) {
                    case InputActionIntent.LockOn:
#if GR_INPUT_DEBUG
                        _logger?.Log("[M0Input] LockOn pressed");
#endif
                        debugOverlayAdapter?.UpdateLastInputAction("LockOn");
                        if (_targetContext != null) {
                            var intent = new InputIntentSnapshot(
                                new Axis2(0f, 0f),
                                new Axis2(0f, 0f),
                                false, false, false, false, false, true, false, false,
                                true);
                            _targetContext.ConsumeInputIntent(intent);
                        }
                        break;
                    case InputActionIntent.LightAttack:
#if GR_INPUT_DEBUG
                        _logger?.Log("[M0Input] LightAttack pressed");
#endif
                        debugOverlayAdapter?.UpdateLastInputAction("LightAttack");
                        _combatCore?.ConsumeAttackIntent(CombatActionType.LightAttack);
                        break;
                    case InputActionIntent.HeavyAttack:
#if GR_INPUT_DEBUG
                        _logger?.Log("[M0Input] HeavyAttack pressed");
#endif
                        debugOverlayAdapter?.UpdateLastInputAction("HeavyAttack");
                        _combatCore?.ConsumeAttackIntent(CombatActionType.HeavyAttack);
                        break;
                    case InputActionIntent.Parry:
#if GR_INPUT_DEBUG
                        _logger?.Log("[M0Input] Parry pressed");
#endif
                        debugOverlayAdapter?.UpdateLastInputAction("Parry");
                        _combatCore?.ConsumeDefensiveIntent(CombatActionType.Parry, enemySnapshot);
                        break;
                    case InputActionIntent.Dodge:
#if GR_INPUT_DEBUG
                        _logger?.Log("[M0Input] Dodge pressed");
#endif
                        debugOverlayAdapter?.UpdateLastInputAction("Dodge");
                        _combatCore?.ConsumeDefensiveIntent(CombatActionType.Dodge, enemySnapshot);
                        break;
                    case InputActionIntent.Counter:
#if GR_INPUT_DEBUG
                        _logger?.Log("[M0Input] Counter pressed");
#endif
                        debugOverlayAdapter?.UpdateLastInputAction("Counter");
                        _combatCore?.ConsumeDefensiveIntent(CombatActionType.Counter, enemySnapshot);
                        break;
                    case InputActionIntent.Interact:
                        debugOverlayAdapter?.UpdateLastInputAction("Interact");
                        _interactTriggeredThisFrame = true;
                        break;
                    case InputActionIntent.ToggleDebugOverlay:
#if GR_DEBUG_OVERLAY
                        _logger?.Log("[M0DebugOverlay] Toggle requested");
#endif
                        if (debugOverlayAdapter != null) {
                            debugOverlayAdapter.UpdateLastInputAction("ToggleDebugOverlay");
                            debugOverlayAdapter.ToggleOverlay();
                        }
                        break;
                    case InputActionIntent.ResetEncounter:
                        debugOverlayAdapter?.UpdateLastInputAction("ResetEncounter");
                        ResetEncounterLifecycle("Input requested reset");
                        break;
                }
            }
        }

        private void OnTargetSnapshotChanged(TargetContextSnapshot snapshot)
        {
            if (debugOverlayAdapter == null) return;

            // Update debug overlay with lock-on target
            var targetName = snapshot.IsLockedOn && !string.IsNullOrEmpty(snapshot.TargetId) ? "Enemy" : "None";
            debugOverlayAdapter.UpdateLockOnTarget(targetName);

#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
            if (lastTargetSnapshot.AcquireReason != snapshot.AcquireReason && !string.IsNullOrEmpty(snapshot.AcquireReason))
            {
                _logger?.Log("[M0Target] AcquireReason: " + snapshot.AcquireReason);
            }

            if (lastTargetSnapshot.InvalidReason != snapshot.InvalidReason && !string.IsNullOrEmpty(snapshot.InvalidReason))
            {
                _logger?.Log("[M0Target] InvalidReason: " + snapshot.InvalidReason);
            }

            if (lastTargetSnapshot.IsLockedOn != snapshot.IsLockedOn)
            {
                if (snapshot.IsLockedOn)
                {
                    _logger?.Log("[M0Target] LockOn acquired");
                }
                else
                {
                    _logger?.Log("[M0Target] LockOn released");
                }
            }
#endif

            lastTargetSnapshot = snapshot;
        }

        private void OnRevealRequestEmitted(RevealRequestContext request) {
            if (_memoryState == null || _memoryVfxResponse == null) {
                return;
            }

            _memoryState.IntakeRevealRequest(request);
            var evaluation = _memoryState.EvaluateRequestedReveal();
            if (!evaluation.Accepted) {
                _memoryVfxResponse.OnRejectRequest(MemoryVFXResponseReasons.NotAcceptedByMemoryState);
                return;
            }

            _memoryState.AdvancePhase("Reveal response accepted");

            var acceptedContext = new AcceptedMemoryRevealContext(
                _memoryState.Snapshot.MemoryId,
                request,
                evaluation,
                request.CombatResultSourceLabel,
                request.ContextLabel);

            _memoryVfxResponse.OnAcceptedReveal(acceptedContext);
            _memoryVfxResponse.OnPlaybackStarted();

#if GR_MEMORY_DEBUG || GR_M0_PROTOTYPE
            _logger?.Log("[M0Memory] Reveal accepted: source=" + request.CombatResultSourceLabel + " memoryId=" + _memoryState.Snapshot.MemoryId);
#endif
        }

    }
}
