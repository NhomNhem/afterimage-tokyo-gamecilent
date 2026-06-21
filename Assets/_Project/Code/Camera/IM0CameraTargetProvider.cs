using UnityEngine;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Pure C# cross-scene provider for camera target positions.
    /// Written by M0GameplayTickHandler, read by M0CombatCameraAdapter.
    /// Registered in ProjectRootLifetimeScope (parent scope) so both gameplay and camera scenes can access it.
    /// No Unity object references — safe for DI across scene boundaries.
    /// </summary>
    public interface IM0CameraTargetProvider {
        Vector3 PlayerPosition { get; }
        Vector3? EnemyPosition { get; }
        bool HasValidTarget { get; }

        void SetPlayerPosition(Vector3 position);
        void SetEnemyPosition(Vector3? position);
    }
}
