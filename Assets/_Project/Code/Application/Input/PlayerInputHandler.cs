using GlassRefrain.Core;

namespace GlassRefrain.Application;

public sealed class PlayerInputHandler {
    public PlayerInputSnapshot BuildSnapshot(InputIntentSnapshot intent) {
        return new PlayerInputSnapshot(
            new UnityEngine.Vector2(intent.Move.X, intent.Move.Y),
            intent.LightAttackPressed || intent.HeavyAttackPressed,
            intent.DodgePressed,
            intent.ParryPressed,
            intent.CounterPressed,
            intent.LockOnPressed,
            intent.DodgePressed,
            intent.InputEnabled);
    }
}
