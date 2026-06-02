using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public readonly struct ParryAnimationRequest {
        public CombatCoreState CombatState { get; }
        public string SourceLabel { get; }

        public ParryAnimationRequest(CombatCoreState combatState, string sourceLabel) {
            CombatState = combatState;
            SourceLabel = sourceLabel ?? string.Empty;
        }
    }
}
