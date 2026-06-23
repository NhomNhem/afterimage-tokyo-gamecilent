using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace GlassRefrain.Locomotion {
    [CreateAssetMenu(
        fileName = "M0LocomotionConfig",
        menuName = "Glass Refrain/M0/Locomotion Config")]
    public sealed class M0LocomotionConfig : SerializedScriptableObject {
        [BoxGroup("Locomotion Tuning")]
        [OdinSerialize, MinValue(0f)] private float moveSpeed = 5.0f;
        [OdinSerialize, Range(0f, 0.99f)] private float inputDeadzone = 0.1f;
        [OdinSerialize, MinValue(0f)] private float facingLerpSpeed = 8.0f;
        [OdinSerialize, MinValue(0f)] private float acceleration = 8.0f;
        [OdinSerialize, MinValue(0f)] private float deceleration = 6.0f;

        [BoxGroup("Rotation Tuning")]
        [OdinSerialize, MinValue(0f)] private float turnAngleThreshold = 130f;
        [OdinSerialize, MinValue(0.1f)] private float rotationSpeed = 4.0f;
        [OdinSerialize, MinValue(0.1f)] private float maxRotationSpeed = 8.0f;

        [BoxGroup("Dodge Tuning")]
        [OdinSerialize, MinValue(0f)] private float dodgeDistance = 1.5f;
        [OdinSerialize, MinValue(0f)] private float dodgeSpeed = 10.0f;
        [OdinSerialize, MinValue(0f)] private float dodgeDurationSeconds = 0.2f;

        public float MoveSpeed => moveSpeed;
        public float InputDeadzone => inputDeadzone;
        public float FacingLerpSpeed => facingLerpSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float TurnAngleThreshold => turnAngleThreshold;
        public float RotationSpeed => rotationSpeed;
        public float MaxRotationSpeed => maxRotationSpeed;
        public float DodgeDistance => dodgeDistance;
        public float DodgeSpeed => dodgeSpeed;
        public float DodgeDurationSeconds => dodgeDurationSeconds;

        public M0LocomotionSettings ToSettings() {
            return new M0LocomotionSettings(
                moveSpeed,
                inputDeadzone,
                facingLerpSpeed,
                acceleration,
                deceleration,
                dodgeDistance,
                dodgeSpeed,
                dodgeDurationSeconds,
                turnAngleThreshold,
                rotationSpeed,
                maxRotationSpeed);
        }
    }
}
