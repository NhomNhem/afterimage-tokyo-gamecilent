using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public readonly struct EnemyIntentAnimationRequest {
        public EnemyIntentState IntentState { get; }
        public string EnemyId { get; }
        public string IntentLabel { get; }
        public string TelegraphId { get; }

        public EnemyIntentAnimationRequest(EnemyIntentState intentState, string enemyId, string intentLabel, string telegraphId) {
            IntentState = intentState;
            EnemyId = enemyId ?? string.Empty;
            IntentLabel = intentLabel ?? string.Empty;
            TelegraphId = telegraphId ?? string.Empty;
        }
    }
}
