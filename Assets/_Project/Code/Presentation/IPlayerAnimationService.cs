using GlassRefrain.Core;
using UnityEngine;

namespace GlassRefrain.Presentation {
    public enum DashDirection {
        Forward = 0,
        Back = 1,
        Left = 2,
        Right = 3
    }

    public enum TurnDirection {
        Left,
        Right
    }

    public interface IPlayerAnimationService {
        void SetCombatMode(bool isCombatMode);
        void PlayNeutral();
        void PlayLocomotion(LocomotionStateSnapshot snapshot, Vector2 relativeMovementDirection);
        void PlayLocomotion(LocomotionState state, PlayerStateSnapshot fullSnapshot, Vector2 relativeMovementDirection);
        void PlayAttack(AttackAnimationRequest request);
        void PlayDodge(DodgeAnimationRequest request);
        void PlayDash(DashDirection direction);
        void PlayParry(ParryAnimationRequest request);
        void PlayCounter(CounterAnimationRequest request);
        void PlayHitReaction(HitReactionAnimationRequest request);
        void PlayStun();

        /// <summary>
        /// Play a 180° turn animation triggered by >130° input reversal (FS Melee pattern).
        /// </summary>
        void PlayTurn(TurnDirection direction);

        /// <summary>
        /// Set continuous locomotion blend parameters on the Animator.
        /// Called every frame during locomotion for smooth blend tree transitions.
        /// FS Melee pattern: animator.SetFloat with damping.
        /// </summary>
        void SetLocomotionParameters(float moveAmount, float strafeAmount, float rotationValue);
    }
}
