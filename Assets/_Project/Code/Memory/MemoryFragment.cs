using NhemDangFugBixs.NhemLogging;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Memory {
    public sealed class MemoryFragment : MonoBehaviour {
        [SerializeField] private MemoryFragmentDefinition definition;
        [SerializeField, Min(0.1f)] private float interactionRadius = 2.25f;

        private MemoryInteractionService _interactionService;
        private INhemLogger _logger;
        private bool _isRegistered;

        public MemoryFragmentDefinition Definition => definition;
        public float InteractionRadius => interactionRadius;

        public string FragmentId {
            get {
                if (definition != null && !string.IsNullOrEmpty(definition.StableId)) {
                    return definition.StableId;
                }

                return gameObject.name;
            }
        }

        [Inject]
        public void Construct(MemoryInteractionService interactionService, INhemLogger logger) {
            _interactionService = interactionService;
            _logger = logger;
        }

        private void OnEnable() {
            TryRegister("OnEnable");
        }

        private void Start() {
            TryRegister("Start");
        }

        private void OnDisable() {
            if (_interactionService == null || !_isRegistered) {
                return;
            }

            _interactionService.UnregisterFragment(this);
            _isRegistered = false;
        }

        public bool IsEligible(Vector3 playerPosition, out float distanceToPlayer) {
            distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
            return distanceToPlayer <= interactionRadius;
        }

        private void TryRegister(string source) {
            if (_isRegistered) {
                return;
            }

            if (_interactionService == null) {
                _logger?.LogWarning("[M1Memory] MemoryFragment registration skipped in " + source + ": interaction service missing.");
                return;
            }

            _interactionService.RegisterFragment(this);
            _isRegistered = true;
        }
    }
}
