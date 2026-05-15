using UnityEngine;
using GlassRefrain.Core;

namespace GlassRefrain.Camera {
    /// <summary>
    /// CameraMovementBasisProvider — provides read-only camera movement basis snapshot.
    /// 
    /// Responsibility:
    /// - Observes camera transform
    /// - Projects camera forward and right onto the ground plane (Y=0)
    /// - Exposes basis via CameraMovementBasisSnapshot (read-only)
    /// 
    /// Scope:
    /// - Story 1-2: Camera basis provision only
    /// - Locomotion reads this basis to calculate world-projected movement direction
    /// - Camera does NOT mutate locomotion state
    /// </summary>
    public class CameraMovementBasisProvider : MonoBehaviour {
        [SerializeField]
        private UnityEngine.Camera targetCamera;

        private CameraMovementBasisSnapshot currentBasis;
        private bool isValid = false;

        private void OnEnable() {
            if (targetCamera == null) {
                targetCamera = UnityEngine.Camera.main;
            }
        }

        private void LateUpdate() {
            // Auto-refresh camera if it becomes null or inactive (handles scene reloads, camera destruction)
            if (targetCamera == null || !targetCamera.isActiveAndEnabled) {
                targetCamera = UnityEngine.Camera.main;
            }
            UpdateMovementBasis();
        }

        private void UpdateMovementBasis() {
            if (targetCamera == null) {
                isValid = false;
                currentBasis = new CameraMovementBasisSnapshot(
                    new Axis2(0f, 1f),
                    new Axis2(1f, 0f),
                    false,
                    "Camera not found");
                return;
            }

            // Project camera forward and right onto ground plane (Y=0)
            Vector3 forward = targetCamera.transform.forward;
            Vector3 right = targetCamera.transform.right;

            // Project forward onto ground plane by zeroing Y component, then normalize
            Vector3 projectedForward = new Vector3(forward.x, 0f, forward.z).normalized;
            if (float.IsNaN(projectedForward.x) || float.IsNaN(projectedForward.z)) {
                projectedForward = Vector3.forward;
            }

            // Project right onto ground plane by zeroing Y component, then normalize
            Vector3 projectedRight = new Vector3(right.x, 0f, right.z).normalized;
            if (float.IsNaN(projectedRight.x) || float.IsNaN(projectedRight.z)) {
                projectedRight = Vector3.right;
            }

            // Create axis2 snapshot (X, Z plane mapping)
            Axis2 forwardAxis = new Axis2(projectedForward.x, projectedForward.z);
            Axis2 rightAxis = new Axis2(projectedRight.x, projectedRight.z);

            isValid = true;
            currentBasis = new CameraMovementBasisSnapshot(
                forwardAxis,
                rightAxis,
                true,
                "Active");
        }

        /// <summary>
        /// Returns the current movement basis snapshot for locomotion to consume.
        /// </summary>
        public CameraMovementBasisSnapshot GetMovementBasis() {
            return currentBasis;
        }

        /// <summary>
        /// Whether the camera basis is valid and ready for consumption.
        /// </summary>
        public bool IsValid => isValid;
    }
}
