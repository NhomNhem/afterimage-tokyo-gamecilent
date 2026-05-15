using UnityEngine;
using VContainer;
using GlassRefrain.Camera;
using GlassRefrain.Combat;
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
        private bool warnedMissingBasis;

        [Inject]
        private void Construct(M0PlayerLocomotion locomotion, M0TargetContext targetContext, M0CombatCore combatCore) {
            this.locomotion = locomotion;
            this.targetContext = targetContext;
            this.combatCore = combatCore;
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
        }
    }
}
