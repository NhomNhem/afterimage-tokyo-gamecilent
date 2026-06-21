using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public readonly struct HitReactionAnimationRequest {
        public CombatCoreState CombatState { get; }
        public string SourceLabel { get; }

        public HitReactionAnimationRequest(CombatCoreState combatState, string sourceLabel) {
            CombatState = combatState;
            SourceLabel = sourceLabel ?? string.Empty;
        }
    }
}
