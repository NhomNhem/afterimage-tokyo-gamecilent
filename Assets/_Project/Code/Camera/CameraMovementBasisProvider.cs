#nullable enable
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
        private UnityEngine.Camera? targetCamera;

        private CameraMovementBasisSnapshot currentBasis;
        private bool isValid = false;
        private const float MinProjectedSqrMagnitude = 0.000001f;

        private void Awake() {
            if (targetCamera == null) {
                targetCamera = UnityEngine.Camera.main;
            }
        }

        private void OnEnable() {
            if (targetCamera == null) {
                targetCamera = UnityEngine.Camera.main;
            }
            UpdateMovementBasis();
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

            // Project forward onto ground plane by zeroing Y component.
            // Guard both NaN and near-zero magnitude to avoid zero basis vectors.
            Vector3 projectedForwardRaw = new Vector3(forward.x, 0f, forward.z);
            Vector3 projectedForward = projectedForwardRaw.sqrMagnitude > MinProjectedSqrMagnitude
                ? projectedForwardRaw.normalized
                : Vector3.forward;
            if (float.IsNaN(projectedForward.x) || float.IsNaN(projectedForward.z) || projectedForward.sqrMagnitude <= MinProjectedSqrMagnitude) {
                projectedForward = Vector3.forward;
            }

            // Build a ground-plane orthonormal right axis from projected forward.
            // This keeps movement basis stable/readable even when camera roll introduces right-axis skew.
            Vector3 projectedRight = Vector3.Cross(Vector3.up, projectedForward);
            if (float.IsNaN(projectedRight.x) || float.IsNaN(projectedRight.z) || projectedRight.sqrMagnitude <= MinProjectedSqrMagnitude) {
                // Fallback to projected camera right if forward-based cross is degenerate.
                Vector3 projectedRightRaw = new Vector3(right.x, 0f, right.z);
                projectedRight = projectedRightRaw.sqrMagnitude > MinProjectedSqrMagnitude
                    ? projectedRightRaw.normalized
                    : Vector3.right;
            } else {
                projectedRight = projectedRight.normalized;
            }

            if (float.IsNaN(projectedRight.x) || float.IsNaN(projectedRight.z) || projectedRight.sqrMagnitude <= MinProjectedSqrMagnitude) {
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
            // Keep basis fresh for callers that tick in Update and cannot wait for LateUpdate.
            if (targetCamera == null || !targetCamera.isActiveAndEnabled) {
                targetCamera = UnityEngine.Camera.main;
            }
            UpdateMovementBasis();
            return currentBasis;
        }

        /// <summary>
        /// Whether the camera basis is valid and ready for consumption.
        /// </summary>
        public bool IsValid => isValid;
    }
}
