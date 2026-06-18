using UnityEngine;

namespace GlassRefrain.Presentation {
    [CreateAssetMenu(menuName = "Glass Refrain/M0/Player Animation Set", fileName = "M0PlayerAnimationSet")]
    public sealed class M0PlayerAnimationSet : ScriptableObject {
        [Header("Peace locomotion (Normal clips)")]
        [SerializeField] private M0AnimationClipTransition idle;
        [SerializeField] private M0AnimationClipTransition locomotion;
        [SerializeField] private M0AnimationClipTransition walk;

        [Header("Combat locomotion (Special clips)")]
        [SerializeField] private M0AnimationClipTransition combatIdle;
        [SerializeField] private M0AnimationClipTransition combatLocomotion;
        [SerializeField] private M0AnimationClipTransition combatWalk;

        [Header("Combat actions")]
        [SerializeField] private M0AnimationClipTransition lightAttack;
        [SerializeField] private M0AnimationClipTransition heavyAttack;
        [SerializeField] private M0AnimationClipTransition dodge;
        [SerializeField] private M0AnimationClipTransition dash;
        [SerializeField] private M0AnimationClipTransition parry;
        [SerializeField] private M0AnimationClipTransition counter;
        [SerializeField] private M0AnimationClipTransition hitReaction;
        [SerializeField] private M0AnimationClipTransition stun;

        [Header("Directional walks (FS Melee)")]
        [SerializeField] private M0AnimationClipTransition walkBack;
        [SerializeField] private M0AnimationClipTransition walkLeft;
        [SerializeField] private M0AnimationClipTransition walkRight;

        public M0AnimationClipTransition Idle => idle;
        public M0AnimationClipTransition Locomotion => locomotion;
        public M0AnimationClipTransition Walk => walk;
        public M0AnimationClipTransition CombatIdle => combatIdle;
        public M0AnimationClipTransition CombatLocomotion => combatLocomotion;
        public M0AnimationClipTransition CombatWalk => combatWalk;
        public M0AnimationClipTransition LightAttack => lightAttack;
        public M0AnimationClipTransition HeavyAttack => heavyAttack;
        public M0AnimationClipTransition Dodge => dodge;
        public M0AnimationClipTransition Dash => dash;
        public M0AnimationClipTransition Parry => parry;
        public M0AnimationClipTransition Counter => counter;
        public M0AnimationClipTransition HitReaction => hitReaction;
        public M0AnimationClipTransition Stun => stun;
        public M0AnimationClipTransition WalkBack => walkBack;
        public M0AnimationClipTransition WalkLeft => walkLeft;
        public M0AnimationClipTransition WalkRight => walkRight;
    }
}
