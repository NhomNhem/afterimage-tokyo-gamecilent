using UnityEngine;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Read-only snapshot of camera output state.
    /// Consumed by M0CombatCameraAdapter to apply transform + FOV.
    /// </summary>
    public readonly struct M0CombatCameraSnapshot {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float FOV;
        public readonly bool IsLockOn;

        public M0CombatCameraSnapshot(Vector3 position, Quaternion rotation, float fov, bool isLockOn) {
            Position = position;
            Rotation = rotation;
            FOV = fov;
            IsLockOn = isLockOn;
        }
    }
}
