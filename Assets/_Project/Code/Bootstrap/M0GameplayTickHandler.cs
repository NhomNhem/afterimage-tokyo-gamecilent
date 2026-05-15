using UnityEngine;
using VContainer;
using GlassRefrain.Camera;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Targeting;

namespace GlassRefrain.Bootstrap {
    public class M0GameplayTickHandler : MonoBehaviour {
        [SerializeField] private M0PlayerLocomotionAdapter adapter;
        [SerializeField] private M0DirectPlayerInput directInput;
        [SerializeField] private CameraMovementBasisProvider cameraBasisProvider;

        private M0PlayerLocomotion locomotion;
        private M0TargetContext targetContext;
        private M0CombatCore combatCore;
        private M0EnemyIntentModel enemyIntentModel;
        private bool warnedMissingBasis;

        [Inject]
        private void Construct(M0PlayerLocomotion locomotion, M0TargetContext targetContext, M0CombatCore combatCore, M0EnemyIntentModel enemyIntentModel) {
            this.locomotion = locomotion;
            this.targetContext = targetContext;
            this.combatCore = combatCore;
            this.enemyIntentModel = enemyIntentModel;
            combatCore.SetTargetContext(targetContext);
            if (adapter != null) {
                adapter.SetLocomotion(locomotion);
            }
            if (directInput != null) {
                directInput.SetLocomotion(locomotion);
                directInput.SetTargetContext(targetContext);
                directInput.SetCombatCore(combatCore);
            }
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
    }
}
