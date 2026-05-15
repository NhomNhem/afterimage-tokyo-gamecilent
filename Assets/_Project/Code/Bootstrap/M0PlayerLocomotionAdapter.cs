using UnityEngine;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// M0PlayerLocomotionAdapter — MonoBehaviour adapter for camera-relative locomotion.
    /// 
    /// Responsibility (Story 1-2):
    /// - Receives movement snapshots from M0PlayerLocomotion
    /// - Applies position and rotation to player GameObject
    /// - Does NOT consume input (that happens in gameplay composition)
    /// - Does NOT own movement truth (M0PlayerLocomotion owns it)
    /// 
    /// Scope:
    /// - Story 1-2: Position/rotation application only
    /// - Story 1-11: Animator parameter binding (deferred)
    /// - No combat integration (Story 1-4 and beyond)
    /// 
    /// Note: locomotion is set by M0GameplayTickHandler via VContainer injection.
    /// This is explicit composition — the adapter does not own the locomotion instance.
    /// </summary>
    public class M0PlayerLocomotionAdapter : MonoBehaviour {
        /// <summary>
        /// Current locomotion instance. Set by M0GameplayTickHandler via VContainer
        /// during M0 bootstrap. Never owns movement truth.
        /// </summary>
        private M0PlayerLocomotion locomotion;

        /// <summary>
        /// Sets the locomotion instance. Called by M0GameplayTickHandler after
        /// VContainer injection. Adapter only reads snapshots — never mutates truth.
        /// </summary>
        public void SetLocomotion(M0PlayerLocomotion loco) {
            locomotion = loco;
        }

        private void Update() {
            if (locomotion == null) return;
            ApplyLocomotionToTransform();
        }

        /// <summary>
        /// Reads locomotion movement snapshot and applies it to player transform.
        /// </summary>
        private void ApplyLocomotionToTransform() {
            LocomotionMovementSnapshot snapshot = locomotion.GetMovementSnapshot();

            // Apply position to transform
            transform.position = snapshot.Position;

            // Apply facing rotation to transform
            // Create rotation that points forward in the facing direction
            if (snapshot.Facing.sqrMagnitude > 0.001f) {
                transform.rotation = Quaternion.LookRotation(snapshot.Facing, Vector3.up);
            }
        }

        /// <summary>
        /// Public access to movement snapshot for external systems.
        /// </summary>
        public LocomotionMovementSnapshot GetMovementSnapshot() {
            if (locomotion == null) {
                Debug.LogError("[M0PlayerLocomotionAdapter] locomotion is null — was SetLocomotion() called before first Update?");
                return default;
            }
            return locomotion.GetMovementSnapshot();
        }
    }
}

