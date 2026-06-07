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
        private MemoryInteractionService _memoryInteractionService;
        private bool _hasLoggedMissingInteractAction;

        [Inject]
        public void Construct(INhemLogger logger, MemoryInteractionService memoryInteractionService) {
            _logger = logger;
            _memoryInteractionService = memoryInteractionService;
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

            var serviceSnapshot = _memoryInteractionService != null
                ? _memoryInteractionService.Snapshot
                : new MemoryInteractionSnapshot(string.Empty, false, false, MemoryInteractOutcome.None, "MemoryInteractionService unavailable");
            var colliderSnapshot = TryReadColliderSnapshot();
            LogProbeSnapshot(serviceSnapshot, colliderSnapshot);
        }

        private MemoryProbeColliderSnapshot TryReadColliderSnapshot() {
            if (rangeDetector == null) {
                rangeDetector = GetComponent<RangeDetector>();
                if (rangeDetector == null) {
                    _logger?.LogWarning("[M1MemoryDebug] Interact pressed but RangeDetector is missing; service eligibility remains authoritative.");
                    return MemoryProbeColliderSnapshot.Unavailable("RangeDetectorMissing");
                }
            }

            rangeDetector.Radius = fallbackRadius;
            if (!rangeDetector.Cast()) {
                return MemoryProbeColliderSnapshot.Unavailable("NoColliderHit");
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
                return MemoryProbeColliderSnapshot.Unavailable("NoMemoryFragmentCollider");
            }

            var layerName = LayerMask.LayerToName(selectedCollider.gameObject.layer);
            if (string.IsNullOrEmpty(layerName)) {
                layerName = selectedCollider.gameObject.layer.ToString();
            }

            float distance = Mathf.Sqrt(nearestSqrDistance);
            float allowedRadius = selectedFragment != null ? selectedFragment.InteractionRadius : fallbackRadius;
            bool withinRadius = distance <= allowedRadius;

            return new MemoryProbeColliderSnapshot(
                true,
                selectedCollider.gameObject.name,
                distance,
                layerName,
                withinRadius,
                string.Empty);
        }

        private void LogProbeSnapshot(MemoryInteractionSnapshot serviceSnapshot, MemoryProbeColliderSnapshot colliderSnapshot) {
            string serviceFragmentId = string.IsNullOrEmpty(serviceSnapshot.NearbyFragmentId)
                ? "None"
                : serviceSnapshot.NearbyFragmentId;
            string colliderHitName = string.IsNullOrEmpty(colliderSnapshot.HitName)
                ? "None"
                : colliderSnapshot.HitName;
            string colliderLayer = string.IsNullOrEmpty(colliderSnapshot.LayerName)
                ? "None"
                : colliderSnapshot.LayerName;
            string colliderReason = string.IsNullOrEmpty(colliderSnapshot.Reason)
                ? "None"
                : colliderSnapshot.Reason;

            _logger?.Log("[M1MemoryDebug] serviceEligible=" + serviceSnapshot.HasEligibleFragment +
                         " serviceFragmentId=" + serviceFragmentId +
                         " serviceOutcome=" + serviceSnapshot.LastOutcome +
                         " serviceReason=" + serviceSnapshot.LastReason +
                         " colliderAvailable=" + colliderSnapshot.Available +
                         " colliderHitName=" + colliderHitName +
                         " colliderDistance=" + colliderSnapshot.Distance.ToString("F2") +
                         " colliderLayer=" + colliderLayer +
                         " colliderWithinRadius=" + colliderSnapshot.WithinRadius +
                         " colliderReason=" + colliderReason);
        }

        private readonly struct MemoryProbeColliderSnapshot {
            public MemoryProbeColliderSnapshot(
                bool available,
                string hitName,
                float distance,
                string layerName,
                bool withinRadius,
                string reason) {
                Available = available;
                HitName = hitName ?? string.Empty;
                Distance = distance;
                LayerName = layerName ?? string.Empty;
                WithinRadius = withinRadius;
                Reason = reason ?? string.Empty;
            }

            public bool Available { get; }
            public string HitName { get; }
            public float Distance { get; }
            public string LayerName { get; }
            public bool WithinRadius { get; }
            public string Reason { get; }

            public static MemoryProbeColliderSnapshot Unavailable(string reason) {
                return new MemoryProbeColliderSnapshot(false, string.Empty, -1f, string.Empty, false, reason);
            }
        }
    }
}
