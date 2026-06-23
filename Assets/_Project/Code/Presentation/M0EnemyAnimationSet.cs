using UnityEngine;

namespace GlassRefrain.Presentation {
    [CreateAssetMenu(menuName = "Glass Refrain/M0/Enemy Animation Set", fileName = "M0EnemyAnimationSet")]
    public sealed class M0EnemyAnimationSet : ScriptableObject {
        [SerializeField] private M0AnimationClipTransition idle;
        [SerializeField] private M0AnimationClipTransition telegraph;
        [SerializeField] private M0AnimationClipTransition active;
        [SerializeField] private M0AnimationClipTransition recovery;
        [SerializeField] private M0AnimationClipTransition hitReaction;

        public M0AnimationClipTransition Idle => idle;
        public M0AnimationClipTransition Telegraph => telegraph;
        public M0AnimationClipTransition Active => active;
        public M0AnimationClipTransition Recovery => recovery;
        public M0AnimationClipTransition HitReaction => hitReaction;
    }
}
