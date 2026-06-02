using NhemDangFugBixs.NhemLogging;
using RaycastPro.Detectors;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace GlassRefrain.Memory {
    public sealed class MemoryRaycastProProbe : MonoBehaviour {
        [SerializeField] private RangeDetector rangeDetector;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField, Min(0.01f)] private float fallbackRadius = 2.25f;

        private INhemLogger _logger;
        private bool _hasLoggedMissingInteractAction;
        [Inject]
        public void Construct(INhemLogger logger) {
            _logger = logger;
        }

        private void Update() {
            if (interactAction == null || interactAction.action == null) {
                if (!_hasLoggedMissingInteractAction) {
                    _logger?.LogWarning("[M1MemoryDebug] Interact InputActionReference is missing. Assign Gameplay/Interact action.");
                    _hasLoggedMissingInteractAction = true;
                }

                return;
            }

            bool interactPressed = interactAction.action.WasPressedThisFrame();
            if (!interactPressed) {
                return;
            }

            if (rangeDetector == null) {
                rangeDetector = GetComponent<RangeDetector>();
                if (rangeDetector == null) {
                    _logger?.LogWarning("[M1MemoryDebug] Interact pressed but RangeDetector is missing.");
                    return;
                }
            }

            rangeDetector.Radius = fallbackRadius;
            if (!rangeDetector.Cast()) {
                _logger?.Log("[M1MemoryDebug] hitName=None distance=-1.00 layer=None withinRadius=False");
                return;
            }

            Collider selectedCollider = null;
            MemoryFragment selectedFragment = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (var detectedCollider in rangeDetector.DetectedColliders) {
                if (detectedCollider == null) {
                    continue;
                }

                var fragment = detectedCollider.GetComponentInParent<MemoryFragment>();
                if (fragment == null) {
                    continue;
                }

                float sqrDistance = (detectedCollider.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                selectedCollider = detectedCollider;
                selectedFragment = fragment;
            }

            if (selectedCollider == null) {
                _logger?.Log("[M1MemoryDebug] hitName=None distance=-1.00 layer=None withinRadius=False");
                return;
            }

            var layerName = LayerMask.LayerToName(selectedCollider.gameObject.layer);
            if (string.IsNullOrEmpty(layerName)) {
                layerName = selectedCollider.gameObject.layer.ToString();
            }

            float distance = Mathf.Sqrt(nearestSqrDistance);
            float allowedRadius = selectedFragment != null ? selectedFragment.InteractionRadius : fallbackRadius;

            bool withinRadius = distance <= allowedRadius;
            _logger?.Log("[M1MemoryDebug] hitName=" + selectedCollider.gameObject.name +
                         " distance=" + distance.ToString("F2") +
                         " layer=" + layerName +
                         " withinRadius=" + withinRadius);
        }
    }
}
