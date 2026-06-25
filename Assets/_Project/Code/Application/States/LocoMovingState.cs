using GlassRefrain.Locomotion;

namespace GlassRefrain.Application;

public sealed class LocoMovingState : IState {
    private readonly LocomotionCore _locomotion;
    public LocoMovingState(LocomotionCore locomotion) { _locomotion = locomotion; }
    public void Enter(StateContext ctx) { }
    public void Update(StateContext ctx) { }
    public void Exit(StateContext ctx) { }

    public PlayerStateType? EvaluateTransition(StateContext ctx) {
        if (ctx.Input.MoveDirection.sqrMagnitude <= 0.01f) return PlayerStateType.Idle;
        return null;
    }

    public const string Key = "Moving";
}
