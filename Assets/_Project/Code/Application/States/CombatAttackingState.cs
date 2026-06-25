using GlassRefrain.Combat;
using GlassRefrain.Core;

namespace GlassRefrain.Application;

public sealed class CombatAttackingState : IState {
    private readonly CombatCore _combat;
    public CombatAttackingState(CombatCore combat) { _combat = combat; }
    public void Enter(StateContext ctx) { }
    public void Update(StateContext ctx) { }
    public void Exit(StateContext ctx) { }

    public PlayerStateType? EvaluateTransition(StateContext ctx) {
        if (ctx.CombatPhase == CombatCoreState.Neutral)
            return ctx.Input.MoveDirection.sqrMagnitude > 0.01f ? PlayerStateType.Moving : PlayerStateType.Idle;
        var next = MapCombatState(ctx.CombatPhase);
        return next == PlayerStateType.Attacking ? null : next;
    }

    private static PlayerStateType? MapCombatState(CombatCoreState ccs) {
        return ccs switch {
            CombatCoreState.DodgeStartup or CombatCoreState.DodgeActive
                or CombatCoreState.DodgeRecovery => PlayerStateType.Dodging,
            CombatCoreState.ParryStartup or CombatCoreState.ParryActive
                or CombatCoreState.ParryRecovery => PlayerStateType.Parrying,
            CombatCoreState.CounterWindow or CombatCoreState.CounterActive
                or CombatCoreState.RevealBeat => PlayerStateType.Countering,
            CombatCoreState.HitReact => PlayerStateType.Stunned,
            _ => null
        };
    }

    public const string Key = "Attacking";
}
