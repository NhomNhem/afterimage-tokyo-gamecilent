using UnityEngine;
using Sirenix.OdinInspector;

namespace GlassRefrain.Locomotion {
    [CreateAssetMenu(
        fileName = "M0LocomotionConfig",
        menuName = "Glass Refrain/M0/Locomotion Config")]
    public sealed class M0LocomotionConfig : ScriptableObject {
        [BoxGroup("Locomotion Tuning")]
        [SerializeField, MinValue(0f)] private float moveSpeed = 5.0f;
        [BoxGroup("Locomotion Tuning")]
        [SerializeField, Range(0f, 0.99f)] private float inputDeadzone = 0.1f;
        [BoxGroup("Locomotion Tuning")]
        [SerializeField, MinValue(0f)] private float facingLerpSpeed = 8.0f;

        [BoxGroup("Dodge Tuning")]
        [SerializeField, MinValue(0f)] private float dodgeDistance = 1.5f;
        [BoxGroup("Dodge Tuning")]
        [SerializeField, MinValue(0f)] private float dodgeSpeed = 10.0f;
        [BoxGroup("Dodge Tuning")]
        [SerializeField, MinValue(0f)] private float dodgeDurationSeconds = 0.2f;

        public float MoveSpeed => moveSpeed;
        public float InputDeadzone => inputDeadzone;
        public float FacingLerpSpeed => facingLerpSpeed;
        public float DodgeDistance => dodgeDistance;
        public float DodgeSpeed => dodgeSpeed;
        public float DodgeDurationSeconds => dodgeDurationSeconds;

        public M0LocomotionSettings ToSettings() {
            return new M0LocomotionSettings(
                moveSpeed,
                inputDeadzone,
                facingLerpSpeed,
                dodgeDistance,
                dodgeSpeed,
                dodgeDurationSeconds);
        }
    }
}
