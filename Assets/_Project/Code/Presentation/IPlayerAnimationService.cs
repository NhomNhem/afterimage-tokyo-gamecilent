using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public interface IPlayerAnimationService {
    void SetCombatMode(bool isCombatMode);
    void PlayNeutral();
    void PlayLocomotion(LocomotionStateSnapshot snapshot);
    void PlayLocomotion(LocomotionState state, PlayerStateSnapshot fullSnapshot);
    void PlayAttack(AttackAnimationRequest request);
    void PlayDodge(DodgeAnimationRequest request);
    void PlayDash(DodgeAnimationRequest request);
    void PlayParry(ParryAnimationRequest request);
    void PlayCounter(AttackAnimationRequest request);
    void PlayHitReaction(AttackAnimationRequest request);
    void PlayStun();
}
}
