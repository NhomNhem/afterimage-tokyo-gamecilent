using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public interface IPlayerAnimationService {
        void PlayNeutral();
        void PlayLocomotion(LocomotionStateSnapshot snapshot);
        void PlayAttack(AttackAnimationRequest request);
        void PlayDodge(DodgeAnimationRequest request);
        void PlayParry(ParryAnimationRequest request);
        void PlayCounter(AttackAnimationRequest request);
    }
}
