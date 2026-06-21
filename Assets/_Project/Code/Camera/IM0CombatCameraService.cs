using UnityEngine;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Read-only camera service interface for M0 combat camera.
    /// Consumed by M0CombatCameraAdapter (Presentation) and M0GameplayTickHandler (tick driver).
    /// </summary>
    public interface IM0CombatCameraService {
        /// <summary>Current camera output snapshot.</summary>
        M0CombatCameraSnapshot Snapshot { get; }

        /// <summary>Whether lock-on framing is active.</summary>
        bool IsLockOn { get; }

        /// <summary>Apply look input (mouse delta / right stick).</summary>
        void ApplyLook(Vector2 lookInput);

        /// <summary>Set lock-on mode.</summary>
        void SetLockOn(bool enabled);

        /// <summary>Toggle lock-on mode.</summary>
        void ToggleLockOn();

        /// <summary>Set player and enemy targets.</summary>
        void SetTargets(Vector3 playerPosition, Vector3? enemyPosition);

        /// <summary>Advance the camera model by one tick.</summary>
        void Tick(float playerSpeed, float deltaTime);

        /// <summary>Trigger camera shake.</summary>
        void AddShake(float intensity);
    }
}
