using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public readonly struct DodgeAnimationRequest {
        public CombatCoreState CombatState { get; }
        public string SourceLabel { get; }

        public DodgeAnimationRequest(CombatCoreState combatState, string sourceLabel) {
            CombatState = combatState;
            SourceLabel = sourceLabel ?? string.Empty;
        }
    }
}
