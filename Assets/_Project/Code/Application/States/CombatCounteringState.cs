using GlassRefrain.Combat;
using GlassRefrain.Core;

namespace GlassRefrain.Application;

public sealed class CombatCounteringState : IState {
    private readonly CombatCore _combat;
    public CombatCounteringState(CombatCore combat) { _combat = combat; }
    public void Enter(StateContext ctx) { }
    public void Update(StateContext ctx) { }
    public void Exit(StateContext ctx) { }

    public PlayerStateType? EvaluateTransition(StateContext ctx) {
        if (ctx.CombatPhase == CombatCoreState.Neutral)
            return ctx.Input.MoveDirection.sqrMagnitude > 0.01f ? PlayerStateType.Moving : PlayerStateType.Idle;
        return MapNonSelfCombatState(ctx.CombatPhase);
    }

    private static PlayerStateType? MapNonSelfCombatState(CombatCoreState ccs) {
        return ccs switch {
            CombatCoreState.AttackStartup or CombatCoreState.AttackActive
                or CombatCoreState.AttackRecovery => PlayerStateType.Attacking,
            CombatCoreState.DodgeStartup or CombatCoreState.DodgeActive
                or CombatCoreState.DodgeRecovery => PlayerStateType.Dodging,
            CombatCoreState.ParryStartup or CombatCoreState.ParryActive
                or CombatCoreState.ParryRecovery => PlayerStateType.Parrying,
            CombatCoreState.HitReact => PlayerStateType.Stunned,
            _ => null
        };
    }

    public const string Key = "Countering";
}
