using GlassRefrain.Core;
using UnityEngine;

namespace GlassRefrain.Presentation {
    public enum DashDirection {
        Forward = 0,
        Back = 1,
        Left = 2,
        Right = 3
    }

    public interface IPlayerAnimationService {
        void SetCombatMode(bool isCombatMode);
        void PlayNeutral();
        void PlayLocomotion(LocomotionStateSnapshot snapshot, Vector2 relativeMovementDirection);
        void PlayAttack(AttackAnimationRequest request);
        void PlayDodge(DodgeAnimationRequest request);
        void PlayDash(DashDirection direction);
        void PlayParry(ParryAnimationRequest request);
        void PlayCounter(CounterAnimationRequest request);
        void PlayHitReaction(HitReactionAnimationRequest request);
        void PlayStun();
        void PlayEnterCombat();
        void PlayExitCombat();
        void SetLocomotionParameters(float moveAmount, float strafeAmount, float rotationValue);
    }
}
