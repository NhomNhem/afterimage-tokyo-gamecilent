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

        [Header("Attack phase clips (optional — falls back to main attack clip)")]
        [SerializeField] private M0AnimationClipTransition attackWindup;
        [SerializeField] private M0AnimationClipTransition attackRecovery;

        [SerializeField] private M0AnimationClipTransition dodge;
        [SerializeField] private M0AnimationClipTransition dash;
        [SerializeField] private M0AnimationClipTransition dashBack;
        [SerializeField] private M0AnimationClipTransition dashLeft;
        [SerializeField] private M0AnimationClipTransition dashRight;
        [SerializeField] private M0AnimationClipTransition parry;
        [SerializeField] private M0AnimationClipTransition counter;
        [SerializeField] private M0AnimationClipTransition hitReaction;
        [SerializeField] private M0AnimationClipTransition hitReaction2;
        [SerializeField] private M0AnimationClipTransition stun;

        [Header("Directional walks (FS Melee)")]
        [SerializeField] private M0AnimationClipTransition walkBack;
        [SerializeField] private M0AnimationClipTransition walkLeft;
        [SerializeField] private M0AnimationClipTransition walkRight;

        [Header("Turn in place (FS Melee hard pivot)")]
        [SerializeField] private M0AnimationClipTransition turn180;
        [SerializeField] private M0AnimationClipTransition turnLeft90;
        [SerializeField] private M0AnimationClipTransition turnRight90;

        public M0AnimationClipTransition Idle => idle;
        public M0AnimationClipTransition Locomotion => locomotion;
        public M0AnimationClipTransition Walk => walk;
        public M0AnimationClipTransition CombatIdle => combatIdle;
        public M0AnimationClipTransition CombatLocomotion => combatLocomotion;
        public M0AnimationClipTransition CombatWalk => combatWalk;
        public M0AnimationClipTransition LightAttack => lightAttack;
        public M0AnimationClipTransition HeavyAttack => heavyAttack;
        public M0AnimationClipTransition AttackWindup => attackWindup;
        public M0AnimationClipTransition AttackRecovery => attackRecovery;
        public M0AnimationClipTransition Dodge => dodge;
        public M0AnimationClipTransition Dash => dash;
        public M0AnimationClipTransition DashBack => dashBack;
        public M0AnimationClipTransition DashLeft => dashLeft;
        public M0AnimationClipTransition DashRight => dashRight;
        public M0AnimationClipTransition Parry => parry;
        public M0AnimationClipTransition Counter => counter;
        public M0AnimationClipTransition HitReaction => hitReaction;
        public M0AnimationClipTransition HitReaction2 => hitReaction2;
        public M0AnimationClipTransition Stun => stun;
        public M0AnimationClipTransition WalkBack => walkBack;
        public M0AnimationClipTransition WalkLeft => walkLeft;
        public M0AnimationClipTransition WalkRight => walkRight;
        public M0AnimationClipTransition Turn180 => turn180;
        public M0AnimationClipTransition TurnLeft90 => turnLeft90;
        public M0AnimationClipTransition TurnRight90 => turnRight90;
    }
}
