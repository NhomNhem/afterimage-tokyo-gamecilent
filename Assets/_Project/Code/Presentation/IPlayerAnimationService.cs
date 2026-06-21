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
        Left90 = 0,
        Right90 = 1,
        Turn180 = 2
    }

    public interface IPlayerAnimationService {
        bool IsTurnActive { get; }
        System.Action<bool> TurnActiveChanged { get; set; }
        void SetCombatMode(bool isCombatMode);
        void PlayNeutral();
        void PlayLocomotion(LocomotionStateSnapshot snapshot, Vector2 relativeMovementDirection);
        void PlayLocomotion(LocomotionState state, PlayerStateSnapshot fullSnapshot, Vector2 relativeMovementDirection);
        void PlayTurn(TurnDirection direction);
        void PlayAttack(AttackAnimationRequest request);
        void PlayDodge(DodgeAnimationRequest request);
        void PlayDash(DashDirection direction);
        void PlayParry(ParryAnimationRequest request);
        void PlayCounter(CounterAnimationRequest request);
        void PlayHitReaction(HitReactionAnimationRequest request);
        void PlayStun();
    }
}
