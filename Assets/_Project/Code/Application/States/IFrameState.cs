namespace GlassRefrain.Application;

public sealed class IFrameState : IState {
    private readonly ImmunityLayer _immunity;
    private float _elapsed;
    private const float Duration = 0.5f;
    private const float EarlyCancelRatio = 0.6f;

    public IFrameState(ImmunityLayer immunity) { _immunity = immunity; }

    public void Enter(StateContext ctx) {
        _elapsed = 0f;
        _immunity.GrantTimed(ImmunityFlags.AllDamage | ImmunityFlags.AllCC, Duration);
    }

    public void Update(StateContext ctx) {
        _elapsed += ctx.DeltaTime;
    }

    public void Exit(StateContext ctx) { }

    public PlayerStateType? EvaluateTransition(StateContext ctx) {
        if (_elapsed >= Duration * EarlyCancelRatio && ctx.Input.AttackPressed)
            return PlayerStateType.Attacking;
        if (_elapsed >= Duration)
            return ctx.Input.MoveDirection.sqrMagnitude > 0.01f ? PlayerStateType.Moving : PlayerStateType.Idle;
        return null;
    }

    public const string Key = "IFrame";
}
