using UnityEngine;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Pure C# cross-scene camera target provider.
    /// </summary>
    public sealed class M0CameraTargetProvider : IM0CameraTargetProvider {
        public Vector3 PlayerPosition { get; private set; }
        public Vector3? EnemyPosition { get; private set; }
        public bool HasValidTarget => EnemyPosition.HasValue;

        public void SetPlayerPosition(Vector3 position) {
            PlayerPosition = position;
        }

        public void SetEnemyPosition(Vector3? position) {
            EnemyPosition = position;
        }
    }
}
