using UnityEngine;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using NhemDangFugBixs.NhemLogging;

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
        private M0PlayerLocomotion _locomotion;
        private INhemLogger _logger;
        private bool _wasMoving;
        private bool _warnedMissingLocomotion;

        /// <summary>
        /// Sets the locomotion instance. Called by M0GameplayTickHandler after
        /// VContainer injection. Adapter only reads snapshots — never mutates truth.
        /// </summary>
        public void SetLocomotion(M0PlayerLocomotion loco) => _locomotion = loco;

        public void SetLogger(INhemLogger log) => _logger = log;

        private void Update() {
            if (_locomotion == null) {
#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
                if (!_warnedMissingLocomotion) {
                    _logger?.LogWarning("[M0Locomotion] Adapter has no locomotion instance; movement cannot be applied");
                    _warnedMissingLocomotion = true;
                }
#endif
                return;
            }
#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
            _warnedMissingLocomotion = false;
#endif
            ApplyLocomotionToTransform();
        }

        /// <summary>
        /// Reads locomotion movement snapshot and applies it to player transform.
        /// </summary>
        private void ApplyLocomotionToTransform() {
            LocomotionMovementSnapshot snapshot = _locomotion.GetMovementSnapshot();
            Vector3 before = transform.position;

            // Apply position to transform
            transform.position = snapshot.Position;

            // Apply facing rotation to transform
            // Create rotation that points forward in the facing direction
            if (snapshot.Facing.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(snapshot.Facing, Vector3.up);

#if GR_M0_PROTOTYPE || GR_INPUT_DEBUG
            Vector3 after = transform.position;
            bool moved = (after - before).sqrMagnitude > 0.0000001f;
            _wasMoving = moved;
#endif
        }

        /// <summary>
        /// Public access to movement snapshot for external systems.
        /// </summary>
        public LocomotionMovementSnapshot GetMovementSnapshot() => _locomotion.GetMovementSnapshot();
    }
}
