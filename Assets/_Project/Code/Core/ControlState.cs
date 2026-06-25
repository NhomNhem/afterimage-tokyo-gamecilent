namespace GlassRefrain.Core;

public readonly struct ControlState {
    public bool CanMove { get; }
    public bool CanAttack { get; }
    public bool CanRotate { get; }

    public ControlState(bool canMove, bool canAttack, bool canRotate) {
        CanMove = canMove;
        CanAttack = canAttack;
        CanRotate = canRotate;
    }
}
