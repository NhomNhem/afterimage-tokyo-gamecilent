using Animancer;
using UnityEngine;

namespace GlassRefrain.Presentation {
    [CreateAssetMenu(menuName = "Glass Refrain/M0/Player Animation Library", fileName = "PlayerAnimLibrary")]
    public sealed class PlayerAnimLibrary : ScriptableObject {
        [Header("Peace locomotion")]
        public ClipTransition Idle;
        public ClipTransition Locomotion;
        public ClipTransition Walk;

        [Header("Combat locomotion")]
        public ClipTransition CombatIdle;
        public ClipTransition CombatLocomotion;
        public ClipTransition CombatWalk;

        [Header("Combat enter/exit")]
        public ClipTransition CombatEnter;
        public ClipTransition CombatExit;

        [Header("Combat actions")]
        public ClipTransition LightAttack;
        public ClipTransition HeavyAttack;
        public ClipTransition AttackWindup;
        public ClipTransition AttackRecovery;

        [Header("Dodge / Dash")]
        public ClipTransition Dodge;
        public ClipTransition DodgeStartup;
        public ClipTransition DodgeActive;
        public ClipTransition DodgeRecovery;
        public ClipTransition Dash;
        public ClipTransition DashBack;
        public ClipTransition DashLeft;
        public ClipTransition DashRight;

        [Header("Parry")]
        public ClipTransition Parry;
        public ClipTransition ParryStartup;
        public ClipTransition ParryActive;
        public ClipTransition ParryRecovery;

        [Header("Counter")]
        public ClipTransition Counter;
        public ClipTransition CounterStartup;
        public ClipTransition CounterActive;
        public ClipTransition CounterRecovery;

        [Header("Reactions")]
        public ClipTransition HitReaction;
        public ClipTransition HitReaction2;
        public ClipTransition Stun;
        public ClipTransition Jump;

        [Header("Directional")]
        public ClipTransition WalkBack;
        public ClipTransition WalkLeft;
        public ClipTransition WalkRight;

        [Header("Turn")]
        public ClipTransition TurnLeft;
        public ClipTransition TurnRight;

        public bool HasClip(ClipTransition t) => t != null && t.Clip != null;
    }
}
