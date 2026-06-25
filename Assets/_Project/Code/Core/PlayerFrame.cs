using UnityEngine;

namespace GlassRefrain.Core;

    public readonly struct PlayerFrame {
    public Vector3 Position { get; }
    public Vector3 Facing { get; }
    public Vector3 MoveVelocity { get; }
    public float MoveSpeed { get; }
    public bool IsGrounded { get; }

    public CombatActionType CurrentCombatAction { get; }
    public CombatCoreState CurrentCombatPhase { get; }
    public bool IsCounterWindowOpen { get; }
    public float CurrentHealth { get; }
    public float MaxHealth { get; }

    public bool CanMove { get; }
    public bool CanAttack { get; }
    public bool CanRotate { get; }

    public PlayerFrame(
        Vector3 position,
        Vector3 facing,
        Vector3 moveVelocity,
        float moveSpeed,
        bool isGrounded,
        CombatActionType currentCombatAction,
        CombatCoreState currentCombatPhase,
        bool isCounterWindowOpen,
        float currentHealth,
        float maxHealth,
        bool canMove,
        bool canAttack,
        bool canRotate) {
        Position = position;
        Facing = facing;
        MoveVelocity = moveVelocity;
        MoveSpeed = moveSpeed;
        IsGrounded = isGrounded;
        CurrentCombatAction = currentCombatAction;
        CurrentCombatPhase = currentCombatPhase;
        IsCounterWindowOpen = isCounterWindowOpen;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        CanMove = canMove;
        CanAttack = canAttack;
        CanRotate = canRotate;
    }
}
