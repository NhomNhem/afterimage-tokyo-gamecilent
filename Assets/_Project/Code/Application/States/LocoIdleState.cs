using GlassRefrain.Locomotion;

namespace GlassRefrain.Application;

public sealed class LocoIdleState : IState {
    private readonly LocomotionCore _locomotion;
    public LocoIdleState(LocomotionCore locomotion) { _locomotion = locomotion; }
    public void Enter(StateContext ctx) { }
    public void Update(StateContext ctx) { }
    public void Exit(StateContext ctx) { }

    public PlayerStateType? EvaluateTransition(StateContext ctx) {
        if (ctx.Input.MoveDirection.sqrMagnitude > 0.01f) return PlayerStateType.Moving;
        return null;
    }

    public const string Key = "Idle";
}
