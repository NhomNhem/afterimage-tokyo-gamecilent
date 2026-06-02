using GlassRefrain.Core;

namespace GlassRefrain.Presentation {
    public readonly struct AttackAnimationRequest {
        public CombatActionType AttackType { get; }
        public CombatCoreState CombatState { get; }
        public string SourceLabel { get; }

        public AttackAnimationRequest(CombatActionType attackType, CombatCoreState combatState, string sourceLabel) {
            AttackType = attackType;
            CombatState = combatState;
            SourceLabel = sourceLabel ?? string.Empty;
        }
    }
}
