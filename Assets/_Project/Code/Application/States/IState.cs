using GlassRefrain.Core;

namespace GlassRefrain.Application;

public interface IState {
    void Enter(StateContext ctx);
    void Update(StateContext ctx);
    void Exit(StateContext ctx);
    PlayerStateType? EvaluateTransition(StateContext ctx);
}

public readonly struct StateContext {
    public float DeltaTime { get; }
    public CombatCoreState CombatPhase { get; }
    public LocomotionState LocoState { get; }
    public PlayerInputSnapshot Input { get; }

    public StateContext(
        float deltaTime,
        CombatCoreState combatPhase,
        LocomotionState locoState,
        PlayerInputSnapshot input) {
        DeltaTime = deltaTime;
        CombatPhase = combatPhase;
        LocoState = locoState;
        Input = input;
    }
}
