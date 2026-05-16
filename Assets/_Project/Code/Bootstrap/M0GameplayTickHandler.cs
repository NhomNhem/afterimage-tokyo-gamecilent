using UnityEngine;
using VContainer;
using GlassRefrain.Camera;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Presentation;
using GlassRefrain.Targeting;

namespace GlassRefrain.Bootstrap {
    public class M0GameplayTickHandler : MonoBehaviour {
        [SerializeField] private M0PlayerLocomotionAdapter adapter;
        [SerializeField] private M0DirectPlayerInput directInput;
        [SerializeField] private CameraMovementBasisProvider cameraBasisProvider;
        [SerializeField] private M0CombatVisualFeedbackAdapter visualFeedbackAdapter;
        [SerializeField] private M0CombatDebugOverlayAdapter debugOverlayAdapter;

        private M0PlayerLocomotion locomotion;
        private M0TargetContext targetContext;
        private M0CombatCore combatCore;
        private M0EnemyIntentModel enemyIntentModel;
        private M0InputRouter inputRouter;
        private bool warnedMissingBasis;

        private M0CombatSnapshot lastCombatSnapshot;
        private EnemyIntentSnapshot lastEnemyIntentSnapshot;
        private InputIntentSnapshot lastInputSnapshot;
        private TargetContextSnapshot lastTargetSnapshot;

        [Inject]
        private void Construct(M0PlayerLocomotion locomotion, M0TargetContext targetContext, M0CombatCore combatCore, M0EnemyIntentModel enemyIntentModel, M0InputRouter inputRouter) {
            this.locomotion = locomotion;
            this.targetContext = targetContext;
            this.combatCore = combatCore;
            this.enemyIntentModel = enemyIntentModel;
            this.inputRouter = inputRouter;
            combatCore.SetTargetContext(targetContext);
            if (adapter != null) {
                adapter.SetLocomotion(locomotion);
            }
            if (directInput != null) {
                directInput.SetLocomotion(locomotion);
                directInput.SetTargetContext(targetContext);
                directInput.SetCombatCore(combatCore);
            }

            // Subscribe to snapshot events for presentation adapters
            if (combatCore != null) {
                combatCore.SnapshotChanged += OnCombatSnapshotChanged;
                lastCombatSnapshot = combatCore.Snapshot;
            }
            if (enemyIntentModel != null) {
                enemyIntentModel.SnapshotChanged += OnEnemyIntentSnapshotChanged;
                lastEnemyIntentSnapshot = enemyIntentModel.Snapshot;
            }
            if (inputRouter != null) {
                inputRouter.SnapshotChanged += OnInputSnapshotChanged;
                lastInputSnapshot = inputRouter.Snapshot;
            }
            if (targetContext != null) {
                targetContext.SnapshotChanged += OnTargetSnapshotChanged;
                lastTargetSnapshot = targetContext.Snapshot;
            }

            // Warn if presentation adapters are not assigned in Inspector
            if (visualFeedbackAdapter == null) {
                Debug.LogWarning("[M0GameplayTickHandler] M0CombatVisualFeedbackAdapter not assigned in Inspector. Visual feedback will be disabled.");
            }
            if (debugOverlayAdapter == null) {
                Debug.LogWarning("[M0GameplayTickHandler] M0CombatDebugOverlayAdapter not assigned in Inspector. Debug overlay will be disabled.");
            }
        }

        private void OnDestroy() {
            if (combatCore != null) combatCore.SnapshotChanged -= OnCombatSnapshotChanged;
            if (enemyIntentModel != null) enemyIntentModel.SnapshotChanged -= OnEnemyIntentSnapshotChanged;
            if (inputRouter != null) inputRouter.SnapshotChanged -= OnInputSnapshotChanged;
            if (targetContext != null) targetContext.SnapshotChanged -= OnTargetSnapshotChanged;
        }

        private void Update() {
            if (locomotion == null) return;

            float dt = Time.deltaTime;

            if (cameraBasisProvider != null) {
                locomotion.SetCameraMovementBasis(cameraBasisProvider.GetMovementBasis());
                warnedMissingBasis = false;
            } else {
                if (!warnedMissingBasis) {
                    Debug.LogWarning("[M0GameplayTickHandler] CameraMovementBasisProvider not assigned. Camera-relative movement disabled.");
                    warnedMissingBasis = true;
                }
            }

            locomotion.ProcessMovementInput(dt);
            locomotion.UpdatePosition(dt);

            enemyIntentModel?.Tick(dt);

            // Story 1-6: Combat Core tick for time-based state management (CounterWindow duration expiry).
            combatCore?.Tick(dt);

            // Story 1-6: Defensive intent forwarding — reads pressed flags from input, passes
            // EnemyIntentSnapshot as value struct so Combat Core has no Enemy assembly dependency.
            if (directInput != null && combatCore != null && enemyIntentModel != null) {
                var enemySnapshot = enemyIntentModel.Snapshot;
                if (directInput.ParryPressedThisFrame)
                    combatCore.ConsumeDefensiveIntent(CombatActionType.Parry, enemySnapshot);
                if (directInput.DodgePressedThisFrame)
                    combatCore.ConsumeDefensiveIntent(CombatActionType.Dodge, enemySnapshot);
                if (directInput.CounterPressedThisFrame)
                    combatCore.ConsumeDefensiveIntent(CombatActionType.Counter, enemySnapshot);
            }

            // Story 1-6: Recovery context forwarding — forwards combat recovery state to locomotion each frame.
            // M0PlayerLocomotion.SetRecoveryContext already handles IsRecovering == false as a no-op.
            if (combatCore != null && locomotion != null)
                locomotion.SetRecoveryContext(combatCore.Snapshot.Recovery);
        }

        private void OnCombatSnapshotChanged(M0CombatSnapshot snapshot)
        {
            if (visualFeedbackAdapter == null) return;

            var previousState = lastCombatSnapshot.State;
            var currentState = snapshot.State;

            // Trigger visual feedback on state transitions
            if (previousState != currentState)
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
                    case CombatCoreState.DodgeActive:
                        visualFeedbackAdapter.TriggerDodgeFeedback();
                        break;
                    case CombatCoreState.CounterActive:
                        visualFeedbackAdapter.TriggerCounterFeedback();
                        break;
                }
            }

            // Update debug overlay
            if (debugOverlayAdapter != null)
            {
                debugOverlayAdapter.UpdateCombatState(currentState.ToString());
                debugOverlayAdapter.UpdateCounterWindowState(
                    snapshot.CounterWindow.IsOpen,
                    snapshot.CounterWindow.ElapsedSeconds,
                    snapshot.CounterWindow.DurationSeconds
                );
            }

            lastCombatSnapshot = snapshot;
        }

        private void OnEnemyIntentSnapshotChanged(EnemyIntentSnapshot snapshot)
        {
            if (visualFeedbackAdapter == null) return;

            var previousState = lastEnemyIntentSnapshot.State;
            var currentState = snapshot.State;

            // Update enemy visual feedback based on intent state
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

            // Update debug overlay
            if (debugOverlayAdapter != null)
            {
                debugOverlayAdapter.UpdateEnemyIntentState(currentState.ToString());
            }

            lastEnemyIntentSnapshot = snapshot;
        }

        private void OnInputSnapshotChanged(InputIntentSnapshot snapshot)
        {
            if (debugOverlayAdapter == null) return;

            // Update debug overlay with last input action
            // Simplified approach - in a full implementation you'd track the last pressed action
            debugOverlayAdapter.UpdateLastInputAction("Input Updated");

            lastInputSnapshot = snapshot;
        }

        private void OnTargetSnapshotChanged(TargetContextSnapshot snapshot)
        {
            if (debugOverlayAdapter == null) return;

            // Update debug overlay with lock-on target
            var targetName = snapshot.TargetId != null ? "Enemy" : "None";
            debugOverlayAdapter.UpdateLockOnTarget(targetName);

            lastTargetSnapshot = snapshot;
        }
    }
}
