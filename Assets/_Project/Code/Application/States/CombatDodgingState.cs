using GlassRefrain.Combat;
using GlassRefrain.Core;

namespace GlassRefrain.Application;

public sealed class CombatDodgingState : IState {
    private readonly CombatCore _combat;
    public CombatDodgingState(CombatCore combat) { _combat = combat; }
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
            CombatCoreState.ParryStartup or CombatCoreState.ParryActive
                or CombatCoreState.ParryRecovery => PlayerStateType.Parrying,
            CombatCoreState.CounterWindow or CombatCoreState.CounterActive
                or CombatCoreState.RevealBeat => PlayerStateType.Countering,
            CombatCoreState.HitReact => PlayerStateType.Stunned,
            _ => null
        };
    }

    public const string Key = "Dodging";
}
