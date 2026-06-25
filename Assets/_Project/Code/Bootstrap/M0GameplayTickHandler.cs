using System.Collections.Generic;
using UnityEngine;
using VContainer;
using GlassRefrain.Application;
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

namespace GlassRefrain.Bootstrap {
    public class M0GameplayTickHandler : SerializedMonoBehaviour {
        [OdinSerialize, Required] private PlayerMover playerMover;
        [OdinSerialize, Required] private M0DirectPlayerInput directInput;
        [OdinSerialize, Required] private CameraMovementBasisProvider cameraBasisProvider;
        [OdinSerialize, Required] private M0CombatVisualFeedbackAdapter visualFeedbackAdapter;
        [OdinSerialize, Required] private M0CombatDebugOverlayAdapter debugOverlayAdapter;
        [OdinSerialize, Required] private AnimationFacade animationFacade;
        [OdinSerialize, Required] private Transform enemyTransform;

        private LocomotionCore _locomotion;
        private M0TargetContext _targetContext;
        private CombatCore _combatCore;
        private M0EnemyIntentModel _enemyIntentModel;
        private M0EnemyIntentLoopDriver _enemyIntentLoopDriver;
        private M0InputRouter _inputRouter;
        private IM0MemoryState _memoryState;
        private M0MemoryVFXResponse _memoryVfxResponse;
        private MemoryInteractionService _memoryInteractionService;
        private PlayerStateMachine _stateMachine;
        private SkillSlotResolver _skillResolver;
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

        private readonly M0MemoryInteractionTickBridge _memoryInteractionTickBridge = new M0MemoryInteractionTickBridge();
        private IM0CameraTargetProvider _targetProvider;

        private bool _loggedAdapterMissing;
        private Vector3 _encounterResetStartPosition;
        private Vector3 _encounterResetStartFacing;
        private readonly List<InputActionIntent> _triggeredInputActions = new List<InputActionIntent>(8);
        private bool _interactTriggeredThisFrame;
        private bool _isConstructed;
        private Vector2 _pendingDashDirection;

        public void SetVisualFeedbackAdapter(M0CombatVisualFeedbackAdapter adapter) => visualFeedbackAdapter = adapter;

        public void SetDebugOverlayAdapter(M0CombatDebugOverlayAdapter adapter) => debugOverlayAdapter = adapter;

        [Inject]
        public void Construct(LocomotionCore locomotion, M0TargetContext targetContext, CombatCore combatCore,
            M0EnemyIntentModel enemyIntentModel, M0EnemyIntentLoopDriver enemyIntentLoopDriver,
            M0InputRouter inputRouter, IM0MemoryState memoryState, M0MemoryVFXResponse memoryVfxResponse,
            MemoryInteractionService memoryInteractionService, INhemLogger logger,
            PlayerStateMachine stateMachine, IM0CameraTargetProvider targetProvider,
            SkillSlotResolver skillResolver) {
            _locomotion = locomotion;
            _targetContext = targetContext;
            _combatCore = combatCore;
            _enemyIntentModel = enemyIntentModel;
            _enemyIntentLoopDriver = enemyIntentLoopDriver;
            _inputRouter = inputRouter;
            _memoryState = memoryState;
            _memoryVfxResponse = memoryVfxResponse;
            _memoryInteractionService = memoryInteractionService;
            _stateMachine = stateMachine;
            _skillResolver = skillResolver;
            _targetProvider = targetProvider;
            _logger = logger;
            _isConstructed = true;
#if GR_ENEMY_DEBUG
            _logger?.Log($"[M0EnemyLoop] TickHandler received loopDriver={_enemyIntentLoopDriver != null}");
#endif

            combatCore.SetTargetContext(targetContext);

            if (directInput != null) {
                directInput.SetLogger(logger);
                directInput.SetInputRouter(inputRouter);
                directInput.DashRequested += OnDashRequested;
            }

            // Subscribe to snapshot events for presentation adapters
            combatCore.SnapshotChanged += OnCombatSnapshotChanged;
            lastCombatSnapshot = combatCore.Snapshot;


            enemyIntentModel.SnapshotChanged += OnEnemyIntentSnapshotChanged;
            lastEnemyIntentSnapshot = enemyIntentModel.Snapshot;

            inputRouter.SnapshotChanged += OnInputSnapshotChanged;
            lastInputSnapshot = inputRouter.Snapshot;

            targetContext.SnapshotChanged += OnTargetSnapshotChanged;
            lastTargetSnapshot = targetContext.Snapshot;
            _locomotion?.SetStrafeMode(lastTargetSnapshot.IsLockedOn);
            _targetProvider?.SetLockOn(lastTargetSnapshot.IsLockedOn);
            combatCore.RevealRequestEmitted += OnRevealRequestEmitted;

            if (playerMover != null) {
                _encounterResetStartPosition = playerMover.transform.position;
                _encounterResetStartFacing = playerMover.transform.forward.sqrMagnitude > 0.000001f
                    ? playerMover.transform.forward.normalized
                    : Vector3.forward;
            }
            else {
                var initialMovementSnapshot = locomotion.GetMovementSnapshot();
                _encounterResetStartPosition = initialMovementSnapshot.Position;
                _encounterResetStartFacing = initialMovementSnapshot.Facing.sqrMagnitude > 0.000001f
                    ? initialMovementSnapshot.Facing
                    : Vector3.forward;
            }

            /*
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
*/

            SyncDebugOverlayFromSnapshots();
        }

        private void OnDestroy() {
            if (!_isConstructed) return;

            if (directInput != null) {
                directInput.DashRequested -= OnDashRequested;
            }

            _combatCore.SnapshotChanged -= OnCombatSnapshotChanged;
            _combatCore.RevealRequestEmitted -= OnRevealRequestEmitted;

            _enemyIntentModel.SnapshotChanged -= OnEnemyIntentSnapshotChanged;

            _inputRouter.SnapshotChanged -= OnInputSnapshotChanged;

            _targetContext.SnapshotChanged -= OnTargetSnapshotChanged;
        }

        private void Update() {
            if (!_isConstructed) return;

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
                        _logger?.LogWarning(
                            "[M0GameplayTickHandler] CameraMovementBasis is invalid; locomotion velocity will remain zero.");
                        _warnedInvalidBasis = true;
                    }
                }
                else {
                    _warnedInvalidBasis = false;
                }
#endif
            }
            else {
                if (!_warnedMissingBasis) {
                    _logger?.LogWarning(
                        "[M0GameplayTickHandler] CameraMovementBasisProvider not assigned. Camera-relative movement disabled.");
                    _warnedMissingBasis = true;
                }
            }

            // Player aggregator handles locomotion + combat tick
            var inputSnapshot = _inputRouter?.Snapshot ?? default;

            if (_pendingDashDirection.sqrMagnitude > 0.01f && _locomotion != null && cameraBasisProvider != null) {
                var dir = _pendingDashDirection;
                int slot = dir.x < -0.5f ? SkillSlotResolver.SlotDashLeft
                    : dir.x > 0.5f ? SkillSlotResolver.SlotDashRight : SkillSlotResolver.SlotDashBack;
                if (_skillResolver.CanActivate(slot)) {
                    var basis = cameraBasisProvider.GetMovementBasis();
                    if (basis.IsValid) {
                        Vector3 cameraForward = new Vector3(basis.Forward.X, 0f, basis.Forward.Y).normalized;
                        Vector3 cameraRight = new Vector3(basis.Right.X, 0f, basis.Right.Y).normalized;
                        Vector3 dashDir;
                        if (dir.y < -0.5f) {
                            var prevFrame = _stateMachine?.Frame ?? default;
                            dashDir = prevFrame.Facing.sqrMagnitude > 0.001f ? -prevFrame.Facing : -cameraForward;
                        } else {
                            dashDir = (cameraRight * dir.x + cameraForward * dir.y).normalized;
                        }
                        if (_locomotion.TryBeginDashDisplacement(dashDir)) {
                            _skillResolver.MarkUsed(slot);
                            if (dir.x < -0.5f) animationFacade?.TriggerDashLeft();
                            else if (dir.x > 0.5f) animationFacade?.TriggerDashRight();
                            else if (dir.y < -0.5f) animationFacade?.TriggerDashBack();
                        }
                    }
                }
                _pendingDashDirection = Vector2.zero;
            }

            _stateMachine?.Tick(inputSnapshot, dt);
            var frame = _stateMachine?.Frame ?? default;

            // Feed positions to cross-scene camera target provider
            if (_targetProvider != null) {
                _targetProvider.SetPlayerPosition(frame.Position);
                _targetProvider.SetEnemyPosition(enemyTransform != null
                    ? (Vector3?)enemyTransform.position
                    : null);
            }

            // Tick order: enemy loop driver first, then enemy intent model
            if (_enemyIntentLoopDriver == null) {
                _logger?.LogWarning(
                    "[M0GameplayTickHandler] _enemyIntentLoopDriver is null in Update. Tick will not be called.");
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

            _memoryInteractionTickBridge.TickInteraction(
                _locomotion,
                _memoryInteractionService,
                _memoryState,
                _memoryVfxResponse,
                debugOverlayAdapter,
                _interactTriggeredThisFrame);

            _memoryInteractionTickBridge.TickRevealFeedback(
                dt,
                _memoryState,
                _memoryVfxResponse,
                debugOverlayAdapter);
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
            if (_targetContext == null || _combatCore == null || _locomotion == null ||
                _enemyIntentLoopDriver == null) {
                _logger?.LogWarning("[M0Encounter] Reset skipped: missing runtime dependency");
                return;
            }

            _targetContext.ResetForEncounter("Encounter reset release");
            _combatCore.ResetForEncounter("Encounter reset");
            _locomotion.ResetForEncounter(_encounterResetStartPosition, _encounterResetStartFacing);
            _enemyIntentLoopDriver.ResetForEncounter("Encounter reset");

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

        private void OnCombatSnapshotChanged(M0CombatSnapshot snapshot) {
            var previousState = lastCombatSnapshot.State;
            var currentState = snapshot.State;
            bool counterWindowOpened = !lastCombatSnapshot.CounterWindow.IsOpen && snapshot.CounterWindow.IsOpen;

            // Trigger visual feedback on state transitions
            if (visualFeedbackAdapter != null && previousState != currentState) {
                switch (currentState) {
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
                        visualFeedbackAdapter.TriggerDodgeFeedback();
                        break;
                    case CombatCoreState.DodgeActive:
                        visualFeedbackAdapter.TriggerDodgeFeedback();
                        break;
                    case CombatCoreState.CounterActive:
                        visualFeedbackAdapter.TriggerCounterFeedback();
                        break;
                }
            }

            if (visualFeedbackAdapter != null && counterWindowOpened) {
                visualFeedbackAdapter.TriggerCounterAvailableFeedback();
            }

            if (snapshot.LastResolutionResult.HitConfirmed && previousState == CombatCoreState.AttackActive &&
                currentState != CombatCoreState.AttackActive) {
                animationFacade?.ObserveEnemyHitReaction();
            }

            // Update debug overlay
            if (debugOverlayAdapter != null) {
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

        private void OnEnemyIntentSnapshotChanged(EnemyIntentSnapshot snapshot) {
            var previousState = lastEnemyIntentSnapshot.State;
            var currentState = snapshot.State;

            // Update enemy visual feedback based on intent state
            if (visualFeedbackAdapter != null) {
                switch (currentState) {
                    case EnemyIntentState.Idle:
                        visualFeedbackAdapter.ResetEnemyState();
                        break;
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

            animationFacade?.ObserveEnemyIntentSnapshot(snapshot);

            // Update debug overlay
            if (debugOverlayAdapter != null) {
                debugOverlayAdapter.UpdateEnemyIntentState(currentState.ToString());

/*#if GR_DEBUG_OVERLAY
                _logger?.Log($"[M0DebugOverlay] Snapshot update received combatState={lastCombatSnapshot.State} enemyState={currentState}");
#endif*/
            }

            lastEnemyIntentSnapshot = snapshot;
        }

        private void OnInputSnapshotChanged(InputIntentSnapshot snapshot) {
            // LastInput overlay is written when triggered intents are routed in HandleInputRouting().
            lastInputSnapshot = snapshot;
        }

        private void HandleInputRouting() {
            if (_inputRouter == null) return;

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
                        debugOverlayAdapter?.UpdateLastInputAction("LightAttack");
                        _combatCore?.ConsumeAttackIntent(CombatActionType.LightAttack);
                        break;
                    case InputActionIntent.HeavyAttack:
                        debugOverlayAdapter?.UpdateLastInputAction("HeavyAttack");
                        _combatCore?.ConsumeAttackIntent(CombatActionType.HeavyAttack);
                        break;
                    case InputActionIntent.Parry:
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

        private void OnDashRequested(Vector2 direction) {
            _pendingDashDirection = direction;
        }

        private void OnTargetSnapshotChanged(TargetContextSnapshot snapshot) {
            if (debugOverlayAdapter == null) return;

            // Update debug overlay with lock-on target
            var targetName = snapshot.IsLockedOn && !string.IsNullOrEmpty(snapshot.TargetId) ? "Enemy" : "None";
            debugOverlayAdapter.UpdateLockOnTarget(targetName);

#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
            if (lastTargetSnapshot.AcquireReason != snapshot.AcquireReason &&
                !string.IsNullOrEmpty(snapshot.AcquireReason)) {
                _logger?.Log("[M0Target] AcquireReason: " + snapshot.AcquireReason);
            }

            if (lastTargetSnapshot.InvalidReason != snapshot.InvalidReason &&
                !string.IsNullOrEmpty(snapshot.InvalidReason)) {
                _logger?.Log("[M0Target] InvalidReason: " + snapshot.InvalidReason);
            }

            if (lastTargetSnapshot.IsLockedOn != snapshot.IsLockedOn) {
                if (snapshot.IsLockedOn) {
                    _logger?.Log("[M0Target] LockOn acquired");
                }
                else {
                    _logger?.Log("[M0Target] LockOn released");
                }
            }
#endif

            lastTargetSnapshot = snapshot;
            _locomotion?.SetStrafeMode(snapshot.IsLockedOn);
            _targetProvider?.SetLockOn(snapshot.IsLockedOn);
        }

        private void OnRevealRequestEmitted(RevealRequestContext request) {
            _memoryInteractionTickBridge.HandleRevealRequest(
                request,
                _memoryState,
                _memoryVfxResponse,
                _logger);
        }

    }
}
