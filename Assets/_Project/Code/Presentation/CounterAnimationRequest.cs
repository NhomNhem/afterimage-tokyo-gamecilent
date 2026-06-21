using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public readonly struct CounterAnimationRequest {
        public CombatCoreState CombatState { get; }
        public string SourceLabel { get; }

        public CounterAnimationRequest(CombatCoreState combatState, string sourceLabel) {
            CombatState = combatState;
            SourceLabel = sourceLabel ?? string.Empty;
        }
    }
}
