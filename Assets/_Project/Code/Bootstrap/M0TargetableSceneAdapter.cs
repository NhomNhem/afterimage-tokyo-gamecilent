using UnityEngine;
using VContainer;
using GlassRefrain.Core;
using GlassRefrain.Targeting;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// M0 prototype-only scene adapter for a static targetable placeholder.
    /// Implements ITargetable and self-registers with ITargetableRegistry via VContainer injection.
    /// Use only for PlayMode verification of Story 1-4 hit/whiff resolution.
    /// Not for production: no health, no AI, no animation, no hit reaction.
    /// </summary>
    public sealed class M0TargetableSceneAdapter : MonoBehaviour, ITargetable {
        [SerializeField] private string targetId = "enemy-m0-placeholder";

        private ITargetableRegistry registry;

        public string TargetId => targetId;
        public bool IsTargetable => gameObject.activeInHierarchy;

        [Inject]
        public void Construct(ITargetableRegistry targetableRegistry) {
            registry = targetableRegistry;
        }

        private void Start() {
            if (registry != null) {
                registry.Register(this);
            }
        }

        private void OnDestroy() {
            if (registry != null) {
                registry.Unregister(this);
                registry = null;
            }
        }

        public Axis2 GetPosition() {
            var pos = transform.position;
            return new Axis2(pos.x, pos.z);
        }
    }
}
