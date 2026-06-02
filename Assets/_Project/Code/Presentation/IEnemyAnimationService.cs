namespace GlassRefrain.Presentation {
    public interface IEnemyAnimationService {
        void PlayIdle();
        void PlayIntent(EnemyIntentAnimationRequest request);
    }
}
