using UnityEngine;

namespace GlassRefrain.Application;

public readonly struct PlayerInputSnapshot {
    public Vector2 MoveDirection { get; }
    public bool AttackPressed { get; }
    public bool DodgePressed { get; }
    public bool ParryPressed { get; }
    public bool CounterPressed { get; }
    public bool TabPressed { get; }
    public bool RetreatPressed { get; }
    public bool InputEnabled { get; }

    public PlayerInputSnapshot(
        Vector2 moveDirection,
        bool attackPressed,
        bool dodgePressed,
        bool parryPressed,
        bool counterPressed,
        bool tabPressed,
        bool retreatPressed,
        bool inputEnabled) {
        MoveDirection = moveDirection;
        AttackPressed = attackPressed;
        DodgePressed = dodgePressed;
        ParryPressed = parryPressed;
        CounterPressed = counterPressed;
        TabPressed = tabPressed;
        RetreatPressed = retreatPressed;
        InputEnabled = inputEnabled;
    }
}
