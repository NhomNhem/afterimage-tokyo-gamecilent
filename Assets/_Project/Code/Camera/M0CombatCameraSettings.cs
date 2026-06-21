using UnityEngine;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Pure C# settings for M0 combat camera.
    /// Owned by config; injected into M0CombatCameraModel.
    /// </summary>
    public readonly struct M0CombatCameraSettings {
        public readonly float Distance;
        public readonly float MinDistance;
        public readonly float Height;
        public readonly float PivotHeight;

        public readonly float YawSensitivity;
        public readonly float PitchSensitivity;
        public readonly float MinPitch;
        public readonly float MaxPitch;

        public readonly float PositionSmoothTime;
        public readonly float RotationSmoothTime;

        public readonly float CollisionRadius;
        public readonly float CollisionSafetyMargin;
        public readonly LayerMask CollisionMask;

        public readonly float BaseFOV;
        public readonly float MaxFOVExpansion;
        public readonly float FOVSmoothTime;

        public readonly float ShakeDecay;
        public readonly float ShakeFrequency;

        public readonly float LockOnDistance;
        public readonly float LockOnHeight;

        public M0CombatCameraSettings(
            float distance,
            float minDistance,
            float height,
            float pivotHeight,
            float yawSensitivity,
            float pitchSensitivity,
            float minPitch,
            float maxPitch,
            float positionSmoothTime,
            float rotationSmoothTime,
            float collisionRadius,
            float collisionSafetyMargin,
            LayerMask collisionMask,
            float baseFOV,
            float maxFOVExpansion,
            float fovSmoothTime,
            float shakeDecay,
            float shakeFrequency,
            float lockOnDistance,
            float lockOnHeight) {
            Distance = distance;
            MinDistance = minDistance;
            Height = height;
            PivotHeight = pivotHeight;
            YawSensitivity = yawSensitivity;
            PitchSensitivity = pitchSensitivity;
            MinPitch = minPitch;
            MaxPitch = maxPitch;
            PositionSmoothTime = positionSmoothTime;
            RotationSmoothTime = rotationSmoothTime;
            CollisionRadius = collisionRadius;
            CollisionSafetyMargin = collisionSafetyMargin;
            CollisionMask = collisionMask;
            BaseFOV = baseFOV;
            MaxFOVExpansion = maxFOVExpansion;
            FOVSmoothTime = fovSmoothTime;
            ShakeDecay = shakeDecay;
            ShakeFrequency = shakeFrequency;
            LockOnDistance = lockOnDistance;
            LockOnHeight = lockOnHeight;
        }
    }
}
