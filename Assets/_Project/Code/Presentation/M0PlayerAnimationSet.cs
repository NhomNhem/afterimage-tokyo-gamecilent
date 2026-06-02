using UnityEngine;

namespace GlassRefrain.Presentation {
    [CreateAssetMenu(menuName = "Glass Refrain/M0/Player Animation Set", fileName = "M0PlayerAnimationSet")]
    public sealed class M0PlayerAnimationSet : ScriptableObject {
        [SerializeField] private M0AnimationClipTransition idle;
        [SerializeField] private M0AnimationClipTransition locomotion;
        [SerializeField] private M0AnimationClipTransition lightAttack;
        [SerializeField] private M0AnimationClipTransition heavyAttack;
        [SerializeField] private M0AnimationClipTransition dodge;
        [SerializeField] private M0AnimationClipTransition parry;
        [SerializeField] private M0AnimationClipTransition counter;

        public M0AnimationClipTransition Idle => idle;
        public M0AnimationClipTransition Locomotion => locomotion;
        public M0AnimationClipTransition LightAttack => lightAttack;
        public M0AnimationClipTransition HeavyAttack => heavyAttack;
        public M0AnimationClipTransition Dodge => dodge;
        public M0AnimationClipTransition Parry => parry;
        public M0AnimationClipTransition Counter => counter;
    }
}
